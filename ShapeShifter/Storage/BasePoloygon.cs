using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
