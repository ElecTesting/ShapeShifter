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

        public ShapeCache OverlayCache { get; set; } = new ShapeCache();

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

        /* cut region
         * takes a shape region
         * grabs items within the bounding box
         * then checks if the intersect with the regions polygon
         */
        public List<ShapeCache> CutRegion(ShapeRegion region, List<string> exclusions)
        {
            var cutCache = new List<ShapeCache>();  

            // get the poly from the shape data
            var shapeRecord = ShapeShifter.GetSingleRecord(OverlayCache, region.RecordId);
            var regionPoly = shapeRecord.PolygonOverlays[0].Polygons[0];

            // set the area to the bounding box of the region polygon
            var boxCount = SetArea(region.Box, exclusions);
            var areaList = GetAreaList(exclusions);

            Parallel.ForEach(areaList, shapeFile =>
            {
                shapeFile.CutRegion(regionPoly);
                if (shapeFile.CutRecords.Count > 0)
                {
                    cutCache.Add(CutShapeToCache(shapeFile));
                }
            });

            //foreach (var shapeFile in areaList)
            //{
            //    shapeFile.CutRegion(regionPoly);
            //    if (shapeFile.CutRecords.Count > 0)
            //    {
            //        cutCache.Add(CutShapeToCache(shapeFile));
            //    }
            //}

            return cutCache;
        }

        /* takes the region cut shape file and outputs a cache file
         */
        private ShapeCache CutShapeToCache(ShapeFile shapeFile)
        {
            var cache = new ShapeCache()
            {
                FilePath = shapeFile.FilePath,
                DbfPath = Path.ChangeExtension(shapeFile.FilePath, ".dbf"),
                Overlay = false
            };

            var box = new BoundingBox();

            var sourceCache = _cache.Where(x => x.FilePath == shapeFile.FilePath).First();

            Parallel.ForEach(sourceCache.Items, item =>
                {
                if (shapeFile.CutRecords.Contains(item.RecordId))
                {
                    cache.Items.Add(item);
                }
            });
            //foreach (var item in sourceCache.Items)
            //{
            //    if (shapeFile.CutRecords.Contains(item.RecordId))
            //    {
            //        cache.Items.Add(item);
            //    }
            //}

            //foreach (var recordId in shapeFile.CutRecords)
            //{
            //    cache.Items.Add(sourceCache.Items.Where(x => x.RecordId == recordId).First());
            //}

            cache.BoundingBox = new BoundingBoxHeader()
            {
                Xmin = cache.Items.Min(x => x.Box.Xmin),
                Xmax = cache.Items.Max(x => x.Box.Xmax),
                Ymin = cache.Items.Min(x => x.Box.Ymin),
                Ymax = cache.Items.Max(x => x.Box.Ymax)
            };

            return cache;
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

        /* GetArea
         * returns a shape object containing the area set by SetArea
        */
        public List<ShapeFile> GetAreaList(List<string> exclusions)
        {
            var shapeFiles = new List<ShapeFile>();

            if (_area.Count() == 0)
            {
                return shapeFiles;
            }

            foreach (var cache in _cache)
            {
                // if the file exists in the exclusion list then skip it
                if (exclusions.Contains(cache.FilePath))
                {
                    continue;
                }

                var shapeFile = new ShapeFile()
                {
                    FilePath = cache.FilePath
                };

                ShapeShifter.CacheToShape(cache, shapeFile);
                if (shapeFile.TotalObjects > 0)
                {
                    shapeFiles.Add(shapeFile);
                }
            }

            return shapeFiles;
        }

        /* CrossRef
         * takes a cache file and intersetcs it with the total area of the current map
         */
        public void CrossRef(ShapeCache shapeCache)
        {
            _cache.RemoveAll(c => c.Overlay == true);

            OverlayCache = shapeCache;

            var fullMapArea = new BoundingBox()
            {
                Xmin = Xmin,
                Xmax = Xmax,
                Ymin = Ymin,
                Ymax = Ymax,
            };
            OverlayCache.Overlay = true;

            Random rnd = new Random(1);
            OverlayCache.Items = OverlayCache.Items.Where(x => x.Box.Intersects(fullMapArea)).ToList();
            foreach (var item in OverlayCache.Items)
            {
                item.FeatureColor = Color.FromArgb(255, rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));
            }

            OverlayCache.BoundingBox = new BoundingBoxHeader()
            {
                Xmax = Xmax,
                Xmin = Xmin,
                Ymax = Ymax,
                Ymin = Ymin
            };

            //_cache.Add(OverlayCache);
        }

        public List<ShapeRegion> GetOverlayHits()
        {
            var overlayHits = new List<ShapeRegion>();

            if (OverlayCache != null)
            {
                using (var dbf = new DBaseReader.DBaseReader(OverlayCache.DbfPath))
                {
                    var nameOrdinal = dbf.GetOrdinal("NAME");

                    foreach (var item in OverlayCache.Items)
                    {
                        var name = "DBase field name not found";

                        if (nameOrdinal > -1)
                        {
                            dbf.GotoRow(item.RecordId - 1);
                            name = dbf.GetString("NAME");
                        }

                        var hit = new ShapeRegion()
                        {
                            Name = name,
                            RecordId = item.RecordId,
                            Box = item.Box
                        };

                        overlayHits.Add(hit);
                    }
                }
            }

            return overlayHits;
        }
    }
}
