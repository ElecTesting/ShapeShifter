using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapeCacheItem
    {
        public int RecordId { get; set; }
        public long FileOffset { get; set; } 
        public BoundingBox Box { get; set; } = new BoundingBox();
        public int FeatCode { get; set; }
        public Color FeatureColor { get; set; } = Color.Black;
    }
}
