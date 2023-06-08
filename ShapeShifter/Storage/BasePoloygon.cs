using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace ShapeShifter.Storage
{
    public class BasePoloygon
    {
        public List<ShapePoint> Points = new List<ShapePoint>();

        public bool PointInPoly(ShapePoint p)
        {
            bool inside = false;
            for (int i = 0, j = Points.Count - 1; i < Points.Count; j = i++)
            {
                if ((Points[i].Y > p.Y) != (Points[j].Y > p.Y) 
                    &&    p.X < (Points[j].X - Points[i].X) * (p.Y - Points[i].Y) / (Points[j].Y - Points[i].Y) + Points[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public ShapePoint GetCentroid()
        {
            double accumulatedArea = 0;
            double centerX = 0;
            double centerY = 0;

            for (int i = 0, j = Points.Count - 1; i < Points.Count; j = i++)
            {
                double temp = Points[i].X * Points[j].Y - Points[j].X * Points[i].Y;
                accumulatedArea += temp;
                centerX += (Points[i].X + Points[j].X) * temp;
                centerY += (Points[i].Y + Points[j].Y) * temp;
            }

            if (Math.Abs(accumulatedArea) < 1E-7f)
            {
                return new ShapePoint()
                {
                    X = 0,
                    Y = 0
                };  // Avoid division by zero
            }

            accumulatedArea *= 3;
            return new ShapePoint()
            {
                X = centerX / accumulatedArea,
                Y = centerY / accumulatedArea
            };
        }


        public void StraightSkeleton(double spacing)
        {
            var resultingPath = new List<ShapePoint>();
            var N = Points.Count;
            double mi, mi1, li, li1, ri, ri1, si, si1, Xi1, Yi1;

            for (int i = 0; i < N; i++)
            {
                mi = (Points[(i + 1) % N].Y - Points[i].Y) / (Points[(i + 1) % N].X - Points[i].X);
                mi1 = (Points[(i + 2) % N].Y - Points[(i + 1) % N].Y) / (Points[(i + 2) % N].X - Points[(i + 1) % N].X);
                li = Math.Sqrt(Math.Pow(Points[(i + 1) % N].X - Points[i].X, 2) + Math.Pow(Points[(i + 1) % N].Y - Points[i].Y, 2));
                li1 = Math.Sqrt(Math.Pow(Points[(i + 2) % N].X - Points[(i + 1) % N].X, 2) + Math.Pow(Points[(i + 2) % N].Y - Points[(i + 1) % N].Y, 2));
                ri = Points[i].X + spacing * (Points[(i + 1) % N].Y - Points[i].Y) / li;
                ri1 = Points[(i + 1) % N].X + spacing * (Points[(i + 2) % N].Y - Points[(i + 1) % N].Y) / li1;
                si = Points[i].Y - spacing * (Points[(i + 1) % N].X - Points[i].X) / li;
                si1 = Points[(i + 1) % N].Y - spacing * (Points[(i + 2) % N].X - Points[(i + 1) % N].X) / li1;
                Xi1 = (mi1 * ri1 - mi * ri + si - si1) / (mi1 - mi);
                Yi1 = (mi * mi1 * (ri1 - ri) + mi1 * si - mi * si1) / (mi1 - mi);

                // Correction for vertical lines
                if (Points[(i + 1) % N].X - Points[i % N].X == 0)
                {
                    Xi1 = Points[(i + 1) % N].X + spacing * (Points[(i + 1) % N].Y - Points[i % N].Y) / Math.Abs(Points[(i + 1) % N].Y - Points[i % N].Y);
                    Yi1 = mi1 * Xi1 - mi1 * ri1 + si1;
                }
                if (Points[(i + 2) % N].X - Points[(i + 1) % N].X == 0)
                {
                    Xi1 = Points[(i + 2) % N].X + spacing * (Points[(i + 2) % N].Y - Points[(i + 1) % N].Y) / Math.Abs(Points[(i + 2) % N].Y - Points[(i + 1) % N].Y);
                    Yi1 = mi * Xi1 - mi * ri + si;
                }

                resultingPath.Add(new ShapePoint()
                {   X = Xi1, 
                    Y = Yi1 
                });
            }
            Points = resultingPath;
        }

        public void ScaleDistance(double factor)
        {
            var centroid = GetCentroid();

            foreach (var point in Points)
            {
                var newDistance = ShapeShifter.GetDistance(centroid.X, centroid.Y, point.X, point.Y) * factor;
                //var angle = ShapeShifter.AngleToRadians(Math.Atan2(centroid.Y - point.Y, centroid.X - point.X));
                var angle = Math.Atan2(centroid.Y - point.Y, centroid.X - point.X);

                var newX = centroid.X + (newDistance * Math.Cos(angle));
                var newY = centroid.Y + (newDistance * Math.Sin(angle));

                point.X = newX;
                point.Y = newY;
            }
        }
        
        public void Scale(double factor)
        {
            var centroid = GetCentroid();

            foreach (var point in Points)
            {
                point.X = centroid.X + ((point.X - centroid.X) * factor);
                point.Y = centroid.Y + ((point.Y - centroid.Y) * factor);
            }
        }
    }
}
