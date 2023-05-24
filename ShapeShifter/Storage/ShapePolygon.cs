using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapePolygon : ShapeObject
    {
        public List<BasePoloygon> Polygons { get; set; } = new List<BasePoloygon>();
    }
}
