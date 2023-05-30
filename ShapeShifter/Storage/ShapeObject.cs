using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapeObject
    {
        public BoundingBox Box { get; set; } = new BoundingBox();
        public string TextString { get; set; } = "";
        public double TextAngle { get; set; } = 0;
    }
}
