using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapeCache
    {
        public string FilePath { get; set; } = "";
        public string DbfPath { get; set; } = "";
        public List<ShapeCacheItem> Items { get; set; } = new List<ShapeCacheItem>();
        public BoundingBoxHeader BoundingBox { get; set; } = new BoundingBoxHeader();
    }
}
