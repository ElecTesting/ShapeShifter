using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapePolygon
    {
        public BoundingBox Box { get; set; } = new BoundingBox();
        public List<BasePoloygon> Polygons { get; set; } = new List<BasePoloygon>();
    }
}
