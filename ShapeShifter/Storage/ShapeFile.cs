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
        public List<ShapePolygon> PolygonOverlays { get; set; } = new List<ShapePolygon>();

        public List<int> CutRecords { get; set; } = new List<int>();


        public int TotalObjects
        {
            get
            {
                return Points.Count + MultiPoints.Count + PolyLines.Count + Polygons.Count;
            }
        }

        public void CutRegion(BasePoloygon regionPoly)
        {
            CutRecords = new List<int>();

            CutPoints(regionPoly);
            CutMultiPoints(regionPoly);
            CutPolygons(regionPoly);
            CutPolyLines(regionPoly);
        }

        private void CutPoints(BasePoloygon regionPoly)
        {
            foreach (var point in Points)
            {
                if (regionPoly.PointInPoly(point))
                {
                    CutRecords.Add(point.RecordId);
                }
            }
        }

        private void CutMultiPoints(BasePoloygon regionPoly)
        {
            foreach (var multiPoint in MultiPoints)
            {
                foreach (var point in multiPoint.Points)
                {
                    if (regionPoly.PointInPoly(point))
                    {
                        CutRecords.Add(multiPoint.RecordId);
                        break;
                    }
                }
            }
        }

        private void CutPolygons(BasePoloygon regionPoly)
        {
            foreach (var polygon in Polygons)
            {
                foreach (var poly in polygon.Polygons)
                {
                    foreach (var point in poly.Points)
                    {
                        if (regionPoly.PointInPoly(point))
                        {
                            CutRecords.Add(polygon.RecordId);
                            break;
                        }
                    }
                }
            }
        }

        private void CutPolyLines(BasePoloygon regionPoly)
        {
            foreach (var polyLine in PolyLines)
            {
                foreach (var poly in polyLine.PolyLines)
                {
                    foreach (var point in poly.Points)
                    {
                        if (regionPoly.PointInPoly(point))
                        {
                            CutRecords.Add(polyLine.RecordId);
                            break;
                        }
                    }
                }
            }
        }

    }
}
