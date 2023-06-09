﻿using System;
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
            Parallel.ForEach(Points, point =>
            {
                if (regionPoly.PointInPoly(point))
                {
                    lock (CutRecords)
                    {
                        CutRecords.Add(point.RecordId);
                    }
                }
            }); 
        }

        private void CutMultiPoints(BasePoloygon regionPoly)
        {
            Parallel.ForEach(MultiPoints, multiPoint =>
            {
                Parallel.ForEach(multiPoint.Points, point =>
                {
                    if (regionPoly.PointInPoly(point))
                    {
                        lock (CutRecords)
                        {
                            CutRecords.Add(multiPoint.RecordId);
                        }
                    }
                });
            });
        }

        private void CutPolygons(BasePoloygon regionPoly)
        {
            Parallel.ForEach(Polygons, polygon =>
            {
                foreach (var poly in polygon.Polygons)
                {
                    Parallel.ForEach(poly.Points, point =>
                    {
                        if (regionPoly.PointInPoly(point))
                        {
                            lock (CutRecords)
                            {
                                CutRecords.Add(polygon.RecordId);
                            }
                        }
                    });
                }
            });
        }

        private void CutPolyLines(BasePoloygon regionPoly)
        {
            Parallel.ForEach(PolyLines, polyLine =>
            {
                foreach (var poly in polyLine.PolyLines)
                {
                    Parallel.ForEach(poly.Points, point =>
                    {
                        if (regionPoly.PointInPoly(point))
                        {
                            lock (CutRecords)
                            {
                                CutRecords.Add(polyLine.RecordId);
                            }
                        }
                    });
                }
            });
        }

    }
}
