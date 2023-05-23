using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapePolyLine
    {
        public BoundingBox Box { get; set; } = new BoundingBox();
        public List<BasePoloygon> PolyLines { get; set; } = new List<BasePoloygon>();
    }
}
