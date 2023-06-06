using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapeRegion
    {
        public int RecordId { get; set; }
        public string Name { get; set; } = "";
        public BoundingBox Box { get; set; } = new BoundingBox();

        public override string ToString()
        {
            return $"{Name} - {RecordId}";
        }
    }
}
