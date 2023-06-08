using ShapeShifter.Storage;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Data.Common;
using System.Net;
using System.Drawing;
using System.Reflection.PortableExecutable;

namespace ShapeShifter
{
    public static class ShapeShifter
    {
        public const int ZoomFactor = 1000;

        // TODO: Move to config
        private static List<MapFeature> Features = JsonConvert.DeserializeObject<List<MapFeature>>(File.ReadAllText("FeatureCodes.json"));


        private static string[] _featureCodeNames = new string[]
        {
            "FEATCODE",
            "featureCod"
        };

        private static string[] _textStringNames = new string[]
        {
            "TEXTSTRING",
            "textString",
            "ADDRESS",
            "JUNCTIONNU",
            "NAME",
            "Text",
            "TEXT",
            "TEXTSTRI0",
            "TXT_STRING",
            "WARD_NAME"
        };

        private static string[] _anchorNames = new string[]
        {
            "ANCHOR"
        };


        private static string[] _angleNames = new string[]
        {
            "TEXTANGLE"
        };

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
                shapeFiles.Add(ProcessShapeFile(fileName));
            }

            return shapeFiles;
        }

        /* Process a single shape file from disk
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
            var shapeFile = ProcessShapeHeader(reader, name);

            ProcessRecords(reader, shapeFile);

            if (reader.Position != shapeFile.FileLength)
            {
                throw new Exception("Not at end of file!");
            }

            return shapeFile;
        }

        /* Create shape cache from a file path
         */
        public static ShapeCache CreateShapeCacheFromFile(string filePath)
        {
            using (var reader = new BinReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)))
            {
                return CreateShapeCache(reader, filePath);
            }
        }

        /* Create list of shape cache froma folder path
        */
        public static List<ShapeCache> CreateShapeCacheFromFolder(string filePath)
        {
            var shapeCaches = new List<ShapeCache>();
            var files = Directory.GetFiles(filePath, "*.shp");

            foreach (var filename in files)
            {
                var cache = CreateShapeCacheFromFile(filename);
                if (cache.BoundingBox.Xmin > 400
                    && cache.BoundingBox.Xmax > 400
                    && cache.BoundingBox.Ymin > 400
                    && cache.BoundingBox.Ymax > 400)
                {
                    shapeCaches.Add(cache);
                }
            }

            return shapeCaches;
        }



        /* Create shape cache file from reader
         */
        public static ShapeCache CreateShapeCache(BinReader reader, string filePath)
        {
            var shapeFile = ProcessShapeHeader(reader, filePath);
            var shapeCache = new ShapeCache()
            {
                BoundingBox = shapeFile.BoundingBox,
                FilePath = filePath,
                DbfPath = Path.ChangeExtension(filePath, ".dbf")
            };

            ProcessShapeCacheRecords(reader, shapeCache);

            if (reader.Position != shapeFile.FileLength)
            {
                throw new Exception("Not at end of file!");
            }

            return shapeCache;
        }


        private static int GetOrdinalFromList(DBaseReader.DBaseReader dbf, string[] names)
        {
            var ordinal = 0;
            foreach (var featName in names)
            {
                if (dbf.Columns.Where(c => c.Name == featName).FirstOrDefault() != null)
                {
                    return dbf.Columns.IndexOf(dbf.Columns.Where(c => c.Name == featName).First());
                }
            }
            return -1;
        }

        /* Process a shape cache file
         * 
         * walks the shape file addint records to the cache with bounding boxes only
         */
        private static void ProcessShapeCacheRecords(BinReader reader, ShapeCache shapeCache)
        {
            var cacheBox = shapeCache.BoundingBox.GetBox;

            var recordIdCheck = 1;
            var dbaseFile = Path.ChangeExtension(shapeCache.FilePath, ".dbf");

            using (var dbf = new DBaseReader.DBaseReader(dbaseFile))
            {
                var featCodeOrdinal = GetOrdinalFromList(dbf, _featureCodeNames);

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

                    var cacheItem = new ShapeCacheItem()
                    {
                        RecordId = recordId,
                        FileOffset = currentPos
                    };

                    if (featCodeOrdinal >= 0)
                    {
                        var featCode = dbf.GetInt32(featCodeOrdinal);
                        var feat = Features.Find(x => x.FeatCode == featCode);
                        if (feat != null)
                        {
                            cacheItem.FeatureColor = feat.RGBColor;
                        }
                    }

                    switch (shapeType)
                    {
                        // empty record
                        case ShapeType.NullShape:
                            break;
                        case ShapeType.Point:
                            //shapeFile.Points.Add(ReadPointRecord(reader));
                            cacheItem.Box = ReadPointRecordAsBox(reader);
                            break;
                        case ShapeType.PolyLine:
                        case ShapeType.Polygon:
                        case ShapeType.PolyLineZ:
                            cacheItem.Box = ReadBoundingBox(reader);
                            break;
                        default:
                            //Console.WriteLine($"Unsupported record type {shapeType} - skipping {recordLength} bytes");
                            reader.Move(recordLength - 4);
                            break;
                    };

                    if (cacheItem.Box.Xmin > 400
                        && cacheItem.Box.Xmax > 400
                        && cacheItem.Box.Ymax > 400
                        && cacheItem.Box.Ymax > 400)
                    {
                        shapeCache.Items.Add(cacheItem);
                    }

                    reader.BaseStream.Position = nextRecord;

                    recordIdCheck++;

                    dbf.NextResult();
                }
            }
        }

        /* Process shape file header record
         * 
         */
        private static ShapeFile ProcessShapeHeader(BinReader reader, string filePath)
        {
            var shapeFile = new ShapeFile()
            {
                FileCode = reader.ReadInt32BE(),
                Unused1 = reader.ReadInt32BE(),
                Unused2 = reader.ReadInt32BE(),
                Unused3 = reader.ReadInt32BE(),
                Unused4 = reader.ReadInt32BE(),
                Unused5 = reader.ReadInt32BE(),
                FileLength = reader.ReadInt32BE() * 2,
                FilePath = filePath
            };

            if (shapeFile.FileLength != reader.Length)
            {
                throw new Exception($"File {filePath} has incorrect length");
            }

            shapeFile.Version = reader.ReadInt32();
            shapeFile.ShapeType = (ShapeType)reader.ReadInt32();
            shapeFile.BoundingBox = new BoundingBoxHeader()
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

            return shapeFile;
        }

        /* ProcessRecords
         * 
         * continues to walk through the reader and pulls records from the shape file
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

                reader.BaseStream.Position = nextRecord;

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

        /* reads the two doubles from the point record
 */
        private static BoundingBox ReadPointRecordAsBox(BinReader reader)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();

            return new BoundingBox()
            {
                Xmin = x,
                Ymin = y,
                Xmax = x,
                Ymax = y
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

        /* reads poly line Z record and returns object, ignores Z values
         *
         */
        private static ShapePolyLine ReadPolyLineZ(BinReader reader)
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

            // create polygons for each part
            partId = 0;
            while (partId < numParts)
            {
                var startPos = parts[partId];
                var endPos = parts[partId + 1];

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

        /* Convert bounding box
         * 
         * this should be replaced with a standard class for both
         */
        private static void BoundingBoxMinMax(BoundingBoxHeader source, BoundingBoxHeader dest)
        {
            dest.Xmin = MinDouble(source.Xmin, dest.Xmin);
            dest.Ymin = MinDouble(source.Ymin, dest.Ymin);
            //dest.Zmin = MinDouble(source.Zmin, dest.Zmin);
            //dest.Mmin = MinDouble(source.Mmin, dest.Mmin);

            dest.Xmax = MaxDouble(source.Xmax, dest.Xmax);
            dest.Ymax = MaxDouble(source.Ymax, dest.Ymax);
            //dest.Zmax = MaxDouble(source.Zmax, dest.Zmax);
            //dest.Mmax = MaxDouble(source.Mmax, dest.Mmax);
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

        /* Generate area based Shape File
         * 
         * Creates a shape file based on the area cache provided
         * 
         */
        public static ShapeFile CreateShapeFileFromCache(List<ShapeCache> caches, BoundingBox areaBox)
        {
            var shapeFile = new ShapeFile();

            foreach (var cache in caches)
            {
                CacheToShape(cache, shapeFile);
            }

            shapeFile.BoundingBox = new BoundingBoxHeader()
            {
                Xmin = areaBox.Xmin,
                Ymin = areaBox.Ymin,
                Xmax = areaBox.Xmax,
                Ymax = areaBox.Ymax
            };

            return shapeFile;
        }


        private static int ColumnToOrdinal(DBaseReader.DBaseReader dbf, string columnName)
        {
            if (dbf.Columns.Where(c => c.Name == columnName).FirstOrDefault() != null)
            {
                return dbf.Columns.IndexOf(dbf.Columns.Where(c => c.Name == columnName).First());
            }
            return -1;
        }

        public static ShapeFile GetSingleRecord(ShapeCache cache, int recordId)
        {
            var shapeFile = new ShapeFile();
            var item = cache.Items.Where(i => i.RecordId == recordId).FirstOrDefault();
            if (item == null)
            {
                return shapeFile;
            }

            using (var reader = new BinReader(new FileStream(cache.FilePath, FileMode.Open, FileAccess.Read)))
            {
                // move to the correct position in the shape binday file
                reader.Goto(item.FileOffset);
                var shapeType = (ShapeType)reader.ReadInt32();

                switch (shapeType)
                {
                    // empty record
                    case ShapeType.NullShape:
                        break;
                    case ShapeType.Point:
                        var point = ReadPointRecord(reader);
                        point.RecordId = item.RecordId;
                        shapeFile.Points.Add(point);
                        break;
                    case ShapeType.PolyLine:
                        var line = ReadPolyLine(reader);
                        line.RecordId = item.RecordId;
                        shapeFile.PolyLines.Add(line);
                        break;
                    case ShapeType.Polygon:
                        var poly = ReadPolyGon(reader);
                        poly.RecordId = item.RecordId;
                        shapeFile.PolygonOverlays.Add(poly);
                        break;
                    case ShapeType.PolyLineZ:
                        var polyLine = ReadPolyLineZ(reader);
                        polyLine.RecordId = item.RecordId;
                        shapeFile.PolyLines.Add(polyLine);
                        break;
                    default:
                        break;
                };
            }

            return shapeFile;
        }

        /* CacheToShape
         * 
         * takes a pre-trimmed cache file and converts it to a shape file
         * adding the additional meta data from the dbf files
         */
        public static void CacheToShape(ShapeCache cache, ShapeFile shapeFile)
        {
            using (var reader = new BinReader(new FileStream(cache.FilePath, FileMode.Open, FileAccess.Read)))
            {
                using (var dbf = new DBaseReader.DBaseReader(cache.DbfPath))
                {
                    var textStringOrdinal = GetOrdinalFromList(dbf, _textStringNames);
                    var anchorOrdinal = GetOrdinalFromList(dbf, _anchorNames);
                    var angleOrdrinal = GetOrdinalFromList(dbf, _angleNames);

                    foreach (var item in cache.Items)
                    {
                        // move to the correct position in the shape binday file
                        reader.Goto(item.FileOffset);

                        var textString = "";
                        var anchor = "";
                        double angle = 0;
                        if (textStringOrdinal >= 0)
                        {
                            // move dbf to correct record
                            dbf.GotoRow(item.RecordId - 1);
                            textString = dbf.GetString(textStringOrdinal);

                            if (anchorOrdinal >= 0)
                            {
                                anchor = dbf.GetString(anchorOrdinal);
                            }

                            if (angleOrdrinal >= 0)
                            {
                                angle = dbf.GetDouble(angleOrdrinal);
                            }
                        }

                        var shapeType = (ShapeType)reader.ReadInt32();

                        switch (shapeType)
                        {
                            // empty record
                            case ShapeType.NullShape:
                                break;
                            case ShapeType.Point:
                                var point = ReadPointRecord(reader);
                                point.RecordId = item.RecordId; 
                                point.TextString = textString;
                                point.Anchor = anchor;
                                point.Color = item.FeatureColor;
                                point.TextAngle = angle;
                                shapeFile.Points.Add(point);
                                break;
                            case ShapeType.PolyLine:
                                var line = ReadPolyLine(reader);
                                line.RecordId = item.RecordId;
                                line.Color = item.FeatureColor;
                                if (!string.IsNullOrEmpty(textString))
                                {
                                    line.TextString = textString;
                                    line.Anchor = anchor;
                                    line.TextAngle = angle;
                                }
                                shapeFile.PolyLines.Add(line);
                                break;
                            case ShapeType.Polygon:
                                var poly = ReadPolyGon(reader);
                                poly.RecordId = item.RecordId;
                                poly.Color = item.FeatureColor;
                                if (cache.Overlay)
                                {
                                    poly.Color = Color.FromArgb(50, item.FeatureColor);
                                    shapeFile.PolygonOverlays.Add(poly);
                                }
                                else
                                {
                                    shapeFile.Polygons.Add(poly);
                                }
                                break;
                            case ShapeType.PolyLineZ:
                                var polyLine = ReadPolyLineZ(reader);
                                polyLine.RecordId = item.RecordId;
                                polyLine.Color = item.FeatureColor;
                                shapeFile.PolyLines.Add(polyLine);
                                break;
                            default:
                                break;
                        };
                    }
                }
            }
        }

        public static double AngleToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        // TODO: maybe replace this!
        public static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
        }

        private const long SHAPEFILE_HEADER_LENGTH = 100;
        private const long SHAPEFILE_FILELENGTH = 24;
        private const long SHAPEFILE_BOX = 36;

        private const long DBF_HEADER_LENGTH = 32;
        private const long DBF_RECORD_COUNT = 4;
        private const long DBF_RECORD_SIZE = 10;
        private const long DBF_RECORD_DESC_SIZE = 32;

        /* FileSlicer
         * uses a cache object to slice up the original shape and dbf files
         * into smaller files that only contain the features that are within
         */
        public static void FileSlicer(ShapeCache shapeCache, string outputPath)
        {
            // new file names
            var shapeOutput = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(shapeCache.FilePath) + ".shp");
            var dbfOutput = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(shapeCache.FilePath) + ".dbf");

            // first do the shape file
            using (var reader = new BinReader(new FileStream(shapeCache.FilePath, FileMode.Open, FileAccess.Read)))
            {
                using (var writer = new BinWriter(new FileStream(shapeOutput, FileMode.Create, FileAccess.Write)))
                {
                    // write header
                    writer.Write(reader.ReadBytes((int)SHAPEFILE_HEADER_LENGTH));

                    var newRecordId = 1;

                    // loop
                    // jump to record, modify record id, write record
                    foreach (var item in shapeCache.Items)
                    {
                        // the offest in the cache is after the record id and record length
                        // we have a new recordid so we only need to get the length
                        reader.Goto(item.FileOffset - 4); 

                        writer.WriteBE(newRecordId);
                        
                        var recordLength = reader.ReadInt32BE();
                        writer.WriteBE(recordLength);
                        writer.Write(reader.ReadBytes(recordLength * 2));
                        newRecordId++;
                    }

                    // fix header data length
                    var totalLength = writer.Position;

                    // set file length
                    writer.Goto(SHAPEFILE_FILELENGTH);
                    var newLength = (int)totalLength / 2;
                    writer.WriteBE(newLength);

                    writer.Goto(SHAPEFILE_BOX);
                    writer.Write(shapeCache.BoundingBox.Xmin);
                    writer.Write(shapeCache.BoundingBox.Ymin);
                    writer.Write(shapeCache.BoundingBox.Xmax);
                    writer.Write(shapeCache.BoundingBox.Ymax);
                }
            }

            // now do the dbf file
            // first do the shape file
            using (var reader = new BinReader(new FileStream(shapeCache.DbfPath, FileMode.Open, FileAccess.Read)))
            {
                using (var writer = new BinWriter(new FileStream(dbfOutput, FileMode.Create, FileAccess.Write)))
                {
                    long dataStart = 0;

                    // get size of record
                    reader.Goto(DBF_RECORD_SIZE);
                    var recordSize = reader.ReadInt16();

                    // write header
                    reader.Goto(0);
                    writer.Write(reader.ReadBytes((int)DBF_HEADER_LENGTH));

                    // copy field descriptors
                    while (true)
                    {
                        var fieldDesc = reader.ReadBytes((int)DBF_RECORD_DESC_SIZE);

                        if (fieldDesc[0] == 0x0D)
                        {
                            // end of field descriptors
                            writer.Write(fieldDesc[0]);
                            
                            // move to start of data
                            reader.Move(-DBF_RECORD_DESC_SIZE + 1);
                            dataStart = reader.Position;
                            break;
                        }
                        else
                        {
                            writer.Write(fieldDesc);
                        }
                    }

                    // now copy the records
                    foreach (var item in shapeCache.Items)
                    {
                        // the offest in the cache is after the record id and record length
                        // we have a new recordid so we only need to get the length
                        var recordId = item.RecordId - 1;
                        var recordOffset = dataStart + (recordId * recordSize);
                        reader.Goto(recordOffset);
                        writer.Write(reader.ReadBytes(recordSize));
                    }

                    // end of file marker
                    writer.Write((byte)0x1A);

                    // fix record count
                    writer.Goto(DBF_RECORD_COUNT);
                    writer.Write(shapeCache.Items.Count);
                }
            }
        }
    }
}