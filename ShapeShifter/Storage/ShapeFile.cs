using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapeFile
    {
        // taken from the shape filename
        public string Name { get; set; } = "";

        // taken from binary header data
        public int FileCode { get; set; }
        public int Unused1 { get; set; }
        public int Unused2 { get; set; }
        public int Unused3 { get; set; }
        public int Unused4 { get; set; }
        public int Unused5 { get; set; }
        public int FileLength { get; set; }
        public int Version { get; set; }
        public ShapeType ShapeType { get; set; }
        public BoundingBoxHeader BoundingBox { get; set; } = new BoundingBoxHeader();
        public string FilePath { get; set; } = "";  

        // diseminated from the records
        public List<ShapePoint> Points { get; set; } = new List<ShapePoint>();
        public List<ShapeMultiPoint> MultiPoints { get; set; } = new List<ShapeMultiPoint>();
        public List<ShapePolyLine> PolyLines { get; set; } = new List<ShapePolyLine>();
        public List<ShapePolygon> Polygons { get; set; } = new List<ShapePolygon>();

        public List<ShapeObject> Objects { get; set; } = new List<ShapeObject>();
    }
}
