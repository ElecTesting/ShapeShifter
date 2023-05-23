using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapeMultiPoint
    {
        public BoundingBox Box { get; set; } = new BoundingBox();
        public List<ShapePoint> Points { get; set; } = new List<ShapePoint>();
    }
}
