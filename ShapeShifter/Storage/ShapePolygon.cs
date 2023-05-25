using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapePolygon : ShapeObject
    {
        public Color Color { get; set; } = Color.White;
        public List<BasePoloygon> Polygons { get; set; } = new List<BasePoloygon>();
    }
}
