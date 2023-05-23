using ShapeShifter.Storage;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Security.AccessControl;

namespace ShapeShifter
{
    public static class ShapeShifter
    {
        /* Collect all shape files and returns a single ShapeFile object
         * 
         */
        public static ShapeFile MergeAllShapeFiles(string path)
        {
            var shapeFiles = ProcessAllShapeFiles(path);

            var shapeFile = new ShapeFile();
            shapeFile.BoundingBox = shapeFiles[0].BoundingBox;

            foreach (var shapeSource in shapeFiles)
            {
                BoundingBoxMinMax(shapeSource.BoundingBox, shapeFile.BoundingBox);
                shapeFile.PolyLines.AddRange(shapeSource.PolyLines);
                shapeFile.Polygons.AddRange(shapeSource.Polygons);
                shapeFile.Points.AddRange(shapeSource.Points);
            }

            return shapeFile;
        }

        /* Collect all shape files and returns a list of shape file objects
         * 
         */
        public static List<ShapeFile> ProcessAllShapeFiles(string path)
        {
            var shapeFiles = new List<ShapeFile>();

            foreach (var fileName in Directory.GetFiles(path, "*.shp"))
            {
                //Console.WriteLine($"{Path.GetFileName(fileName)}");
                shapeFiles.Add(ProcessShapeFile(fileName));
            }

            return shapeFiles;
        }

        /* Process Shape file from disk
         * 
         */
        public static ShapeFile ProcessShapeFile(string fileName)
        {
            var nameOnly = Path.GetFileNameWithoutExtension(fileName);

            using (var reader = new BinReader(new FileStream(fileName, FileMode.Open, FileAccess.Read)))
            {
                return ProcessShapeFile(reader, nameOnly);
            }
        }

        /* Process Shape file from reader
         * 
         */

        public static ShapeFile ProcessShapeFile(BinReader reader, string name)
        {
            var shapeFile = new ShapeFile()
            {
                Name = name,
                FileCode = reader.ReadInt32BE(),
                Unused1 = reader.ReadInt32BE(),
                Unused2 = reader.ReadInt32BE(),
                Unused3 = reader.ReadInt32BE(),
                Unused4 = reader.ReadInt32BE(),
                Unused5 = reader.ReadInt32BE(),
                FileLength = reader.ReadInt32BE() * 2
            };

            if (shapeFile.FileLength != reader.Length)
            {
                throw new Exception($"File {name} has incorrect length");
            }

            shapeFile.Version = reader.ReadInt32();
            shapeFile.ShapeType = (ShapeType)reader.ReadInt32();
            shapeFile.BoundingBox = new BoundingBox()
            {
                Xmin = reader.ReadDouble(),
                Ymin = reader.ReadDouble(),
                Xmax = reader.ReadDouble(),
                Ymax = reader.ReadDouble(),
                Zmin = reader.ReadDouble(),
                Zmax = reader.ReadDouble(),
                Mmin = reader.ReadDouble(),
                Mmax = reader.ReadDouble()
            };

            ProcessRecords(reader, shapeFile);

            if (reader.Position != shapeFile.FileLength)
            {
                throw new Exception("Not at end of file!");
            }

            return shapeFile;
        }

        /* ProcessRecords
         * 
         * continues to walk through the reader records from the shape file
         * and adding them to the appropriate list.
         * 
         */
        private static void ProcessRecords(BinReader reader, ShapeFile shapeFile)
        {
            var recordIdCheck = 1;

            while (reader.Position < reader.Length)
            {
                var recordId = reader.ReadInt32BE();
                if (recordId != recordIdCheck)
                {
                    throw new Exception("Error walking file, record id mismatch");
                }

                var recordLength = reader.ReadInt32BE() * 2;

                var currentPos = reader.Position;
                var nextRecord = currentPos + recordLength;

                var shapeType = (ShapeType)reader.ReadInt32();

                switch (shapeType)
                {
                    // empty record
                    case ShapeType.NullShape:
                        break;
                    case ShapeType.Point:
                        shapeFile.Points.Add(ReadPointRecord(reader));
                        break;
                    case ShapeType.PolyLine:
                        shapeFile.PolyLines.Add(ReadPolyLine(reader));
                        break;
                    case ShapeType.Polygon:
                        shapeFile.Polygons.Add(ReadPolyGon(reader));
                        break;
                    default:
                        Console.WriteLine($"Unsupported record type {shapeType} - skipping {recordLength} bytes");
                        reader.Move(recordLength - 4);
                        break;
                };

                if (reader.Position != nextRecord) 
                {
                    throw new Exception("Incorrect position");
                }

                recordIdCheck++;
            }
        }

        /* reads the two doubles from the point record
         */
        private static ShapePoint ReadPointRecord(BinReader reader)
        {
            return new ShapePoint()
            {
                X = reader.ReadDouble(),
                Y = reader.ReadDouble()
            };
        }

        /* reads poly line record and returns object
         */
        private static ShapePolyLine ReadPolyLine(BinReader reader)
        {
            var polyLine = new ShapePolyLine()
            {
                Box = ReadBoundingBox(reader)
            };

            polyLine.PolyLines = ReadPoloygons(reader);
            return polyLine;
        }

        /* reads poly line record and returns object
         */
        private static ShapePolygon ReadPolyGon(BinReader reader)
        {
            var polyLine = new ShapePolygon()
            {
                Box = ReadBoundingBox(reader)
            };

            polyLine.Polygons = ReadPoloygons(reader);
            return polyLine;
        }

        private static List<BasePoloygon> ReadPoloygons(BinReader reader)
        {
            var polygons = new List<BasePoloygon>();

            var numParts = reader.ReadInt32();
            var totalPoints = reader.ReadInt32();

            var parts = new int[numParts + 1];

            // read part sizes into array
            var partId = 0;
            while (partId < numParts)
            {
                parts[partId] = reader.ReadInt32();
                partId++;
            }

            // add end of point stream to array
            parts[partId] = totalPoints;
            /*
            if (parts.Length > 2)
            {
                throw new Exception("TEST");
            }*/

            var pointOffset = reader.Position;

            // create polygons for each part
            partId = 0;
            while (partId < numParts)
            {
                var startPos = parts[partId];
                var endPos = parts[partId + 1];

                if (reader.Position != (startPos*16) + pointOffset)
                {
                    throw new Exception("TEST");
                }

                var polygon = new BasePoloygon();

                while (startPos < endPos)
                {
                    polygon.Points.Add(ReadPointRecord(reader));
                    startPos++;
                }
                polygons.Add(polygon);
                partId++;
            }

            return polygons;
        }

        /* Reads a bounding box from record which contains min/max x/y
         */
        private static BoundingBox ReadBoundingBox(BinReader reader)
        {
            return new BoundingBox()
            {
                Xmin = reader.ReadDouble(),
                Ymin = reader.ReadDouble(),
                Xmax = reader.ReadDouble(),     
                Ymax = reader.ReadDouble()
            };
        }

        /* Merge shape files
         * 
         * source = data to be copied from
         * dest = destination to push data into
         * 
         */
        private static void BoundingBoxMinMax(BoundingBox source, BoundingBox dest)
        {
            dest.Xmin = MinDouble(source.Xmin, dest.Xmin);
            dest.Ymin = MinDouble(source.Ymin, dest.Ymin);
            dest.Zmin = MinDouble(source.Zmin, dest.Zmin);
            dest.Mmin = MinDouble(source.Mmin, dest.Mmin);

            dest.Xmax = MaxDouble(source.Xmax, dest.Xmax);
            dest.Ymax = MaxDouble(source.Ymax, dest.Ymax);
            dest.Zmax = MaxDouble(source.Zmax, dest.Zmax);
            dest.Mmax = MaxDouble(source.Mmax, dest.Mmax);
        }

        /* MinDouble
         * 
         * input a / b
         * output minimum of a / b
         * 
         */
        private static double MinDouble(double a, double b)
        {
            if (a == 0)
                return b;
            if (b == 0) 
                return a;

            return a < b ? a : b;
        }

        /* MaxDouble
         * 
         * input a / b
         * output minimum of a / b
         * 
         */
        private static double MaxDouble(double a, double b)
        {
            if (a == 0)
                return b;
            if (b == 0)
                return a;

            return a > b ? a : b;
        }
    }
}