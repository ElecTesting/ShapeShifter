using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapePolyLine : ShapeObject
    {
        public List<BasePoloygon> PolyLines { get; set; } = new List<BasePoloygon>();
    }
}
