using ShapeShifter.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter
{
    public class ShapeManager
    {
        private List<ShapeCache> _cache;
        private List<ShapeCache> _area;

        public double Xmin { get; set; }
        public double Xmax { get; set; }
        public double Ymin { get; set; }
        public double Ymax { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public int FileCount
        {
            get
            {
                return _cache.Count;
            }
        }

        public int RecordCount
        {
            get
            {
                return _cache.Sum(x => x.Items.Count);
            }
        }

        /* instansiate the ShapeManager class with a path to a folder containing shapefiles 
         * and set min / max and height / width properties
         */
        public ShapeManager(string path)
        {
            _cache = ShapeShifter.CreateShapeCacheFromFolder(path);
            _area = new List<ShapeCache>();

            Xmin = _cache.Min(x => x.BoundingBox.Xmin);
            Xmax = _cache.Max(x => x.BoundingBox.Xmax);   
            Ymin = _cache.Min(x => x.BoundingBox.Ymin);   
            Ymax = _cache.Max(x => x.BoundingBox.Ymax);
            Width = Xmax - Xmin;
            Height = Ymax - Ymin;
        }

        /* sets the area required from the cache files 
         */
        public int SetArea(BoundingBox area)
        {
            _area = new List<ShapeCache>();

            var count = 0;

            foreach (var cache in _cache)
            {
                if (cache.BoundingBox.Intersects(area))
                {
                    var thisCache = new ShapeCache();
                    _area.Add(thisCache);

                    foreach (var item in cache.Items)
                    {
                        if (item.Box.Intersects(area))
                        {
                            thisCache.Items.Add(item);
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        public void GetArea()
        {
            if (_area.Count() == 0)
            {
                throw new Exception("No area set");
            }

        }
    }
}
