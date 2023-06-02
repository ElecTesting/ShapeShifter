using ShapeShifter.Storage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter
{
    public class ShapeManager
    {
        private List<ShapeCache> _cache;
        private List<ShapeCache> _area;
        private BoundingBox _areaBox;

        public double Xmin { get; set; }
        public double Xmax { get; set; }
        public double Ymin { get; set; }
        public double Ymax { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public List<ShapeSummary> Summary { get; set; } = new List<ShapeSummary>();

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
         * 
         * adds a list of all shape files found to a summary which can be used as a toggle
         */
        public ShapeManager(string path)
        {
            _cache = ShapeShifter.CreateShapeCacheFromFolder(path);
            _area = new List<ShapeCache>();

            foreach (var cache in _cache)
            {
                Summary.Add(new ShapeSummary()
                {
                    FilePath = cache.FilePath,
                    FileName = Path.GetFileName(cache.FilePath),
                    ItemCount = cache.Items.Count,
                });
            }

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
            return SetArea(area, new List<string>());
        }

        public int SetArea(BoundingBox area, List<string> exclusions)
        {
            _area = new List<ShapeCache>();
            _areaBox = area;

            var count = 0;

            foreach (var cache in _cache)
            {
                // if the file exists in the exclusion list then skip it
                if (exclusions.Contains(cache.FilePath))
                {
                    continue;
                }

                if (cache.BoundingBox.Intersects(area))
                {
                    var thisCache = new ShapeCache()
                    {
                        FilePath = cache.FilePath,
                        DbfPath = cache.DbfPath,
                        Overlay = cache.Overlay
                    };

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

        /* GetArea
         * returns a shape object containing the area set by SetArea
         */
        public ShapeFile GetArea()
        {
            if (_area.Count() == 0)
            {
                //throw new Exception("No area set");
            }

            return ShapeShifter.CreateShapeFileFromCache(_area, _areaBox);
        }

        /* CrossRef
         * takes a cache file and intersetcs it with the total area of the current map
         */
        public void CrossRef(ShapeCache shapeCache)
        {
            var fullMapArea = new BoundingBox()
            {
                Xmin = Xmin,
                Xmax = Xmax,
                Ymin = Ymin,
                Ymax = Ymax,
            };
            shapeCache.Overlay = true;

            Random rnd = new Random(1);
            shapeCache.Items = shapeCache.Items.Where(x => x.Box.Intersects(fullMapArea)).ToList();
            foreach (var item in shapeCache.Items)
            {
                item.FeatureColor = Color.FromArgb(30, rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));
            }

            shapeCache.BoundingBox = new BoundingBoxHeader()
            {
                Xmax = Xmax,
                Xmin = Xmin,
                Ymax = Ymax,
                Ymin = Ymin
            };

            _cache.Add(shapeCache);
        }


    }
}
