using FolderBrowserEx;
using Microsoft.Win32;
using ShapeShifter;
using ShapeShifter.Storage;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Bson;
using System.Windows.Media.Media3D;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Printing;
using System.Windows.Threading;
using System.Configuration;
using System.Runtime.Intrinsics.X86;

namespace ShapeViewer
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private ShapeManager _shapeManager;

        private double _metersPerPixel;

        private bool _fileLoaded = false;
        private bool _hideMouseTip = false;

        private Bitmap? _overviewImage;
        private Bitmap? _overlayImage;

        private string _shapeFolder = "";
        private string _exportFolder = "";
        private string _cutFolder = "";

        private double _heightBackup = 0;

        private const double KmMi = 0.621371;

        private Config _config = new Config();

        public ObservableCollection<ShapeSummary> _shapeEntities { get; set; } = new ObservableCollection<ShapeSummary>();

        public MainWindow()
        {
            InitializeComponent();

            _metersPerPixel = ShapeShifter.ShapeShifter.ZoomFactor;

            _windowX = 0.5;
            _windowY = 0.5;

            var json = File.ReadAllText("config.json");
            _config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(json);
            LoadOSMaps();
        }

        /* function to get the .sho files from the configured os maps folder path and
         * add them to the drop down list
         */
        private void LoadOSMaps()
        {
            foreach (var filename in Directory.GetFiles(_config.OSMapPath, "*.shp"))
            {
                var osItem = new ShapeSummary()
                {
                    FileName = filename,
                    FilePath = filename
                };
                _osMaps.Items.Add(osItem);
            }
        }
            
        private void OpenShapeFolder_Click(object sender, RoutedEventArgs e)
        {
            var openFolder = new FolderBrowserDialog()
            {
                Title = "Select a folder containing ESRI files",
                AllowMultiSelect = false
            };

            if (!string.IsNullOrEmpty(_shapeFolder))
            {
                openFolder.InitialFolder = _shapeFolder;
            }


            var result = openFolder.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = openFolder.SelectedFolder;

                _shapeFolder = path;

                var shapeFileList = Directory.GetFiles(path, "*.shp");
                if (shapeFileList.Length == 0)
                {
                    // uh oh
                    MessageBox.Show("No shape files found in this folder");
                }
                else
                {
                    var procWindow = GetProcessWindow();

                    _overviewImage = null;
                    _overlayImage = null;

                    _shapeManager = new ShapeManager(path);
                    ShowStats();

                    GetFileItems(_shapeManager.Summary);

                    _windowX = 0.5;
                    _windowY = 0.5;
                    _fileLoaded = true;
                    ZoomSlider.Value = 1;
                    _tabs.SelectedIndex = 0;
                    SetNewArea();
                    _mapOverViewGrid.OffsetY = 0;
                    _mapOverViewGrid.Reset();
                    _txtTitle.Text = System.IO.Path.GetFileNameWithoutExtension(path);

                    CloseProcessWindow(procWindow);
                }
            }
        }

        private void GetFileItems(List<ShapeSummary> summary)
        {
            _shapeEntities.Clear();

            foreach (var item in summary)
            {
                _shapeEntities.Add(item);       
            }

            DataContext = this;
        }

        /* event handler for checkbox change
         * triggers a re-render of the map excluding the unchecked layers
         */
        private void ShapeItem_CheckChange(object sender, RoutedEventArgs e)
        {
            SetNewArea();
        }

        private void ShowStats()
        {
            XminView.Text = $"{_shapeManager.Xmin:0.00}";
            XmaxView.Text = $"{_shapeManager.Xmax:0.00}";
            YminView.Text = $"{_shapeManager.Ymin:0.00}";
            YmaxView.Text = $"{_shapeManager.Ymax:0.00}";
            ItemCount.Text = _shapeManager.RecordCount.ToString();
            WidthText.Text = $"{_shapeManager.Width:0.00}";
            HeightText.Text = $"{_shapeManager.Height:0.00}";
            var area = _shapeManager.Width * _shapeManager.Height / 1000;
            MapKm.Text = $"{area:0.00}";
            MapMi.Text = $"{area * 0.386102:0.00}";
        }

        /* set a new viewing area for the detail tab based on _windowX and _windowY
         * when area is determined, render the map
         */
        private void SetNewArea()
        {
            if (!_fileLoaded)
            {
                return;
            }

            var metersX = _metersPerPixel;
            var scaleY = _mapViewGrid.RenderSize.Height / _mapViewGrid.RenderSize.Width;
            var metersY = _metersPerPixel * scaleY;


            var xmin = _shapeManager.Xmin + (_windowX * _shapeManager.Width) - (metersX / 2);
            var xmax = xmin + metersX;
            var ymin = _shapeManager.Ymin + (_windowY * _shapeManager.Height) - (metersY / 2);
            var ymax = ymin + metersY;

            var box = new BoundingBox()
            {
                Xmin = xmin,
                Xmax = xmax,
                Ymin = ymin,
                Ymax = ymax
            };

            TextAreaXmin.Text = $"{box.Xmin:0.00}";
            TextAreaXmax.Text = $"{box.Xmax:0.00}";
            TextAreaYmin.Text = $"{box.Ymin:0.00}";
            TextAreaYmax.Text = $"{box.Ymax:0.00}";

            var exclusionList = GetExclusionList();
            var itemCount = _shapeManager.SetArea(box, exclusionList);
            ItemsArea.Text = itemCount.ToString();
            var areaOnly = _shapeManager.GetArea();

            var width = (int)_mapViewGrid.RenderSize.Width;
            var height = (int)_mapViewGrid.RenderSize.Height;
            bool renderText = _metersPerPixel < 300 ? true : false;

            var testImage = ShapeRender.ShapeRender.RenderShapeFile(areaOnly, width, height, renderText);
                
            using (MemoryStream memory = new MemoryStream())
            {
                testImage.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                ((System.Windows.Controls.Image)_mapViewGrid.Child).Source = bitmapImage;
            }

            // set box pan limits and sizes
            var aspectY = _mapViewGrid.RenderSize.Height / _mapViewGrid.RenderSize.Width;

            _mapViewGrid.AreaWidth = _shapeManager.Width;
            _mapViewGrid.AreaHeight = _shapeManager.Height;
            _mapViewGrid.BoxWidth = _metersPerPixel;
            _mapViewGrid.BoxHeight = _metersPerPixel * aspectY;
            _mapViewGrid.BoxOriginX = _windowX * _shapeManager.Width;
            _mapViewGrid.BoxOriginY = _windowY * _shapeManager.Height;
            _mapViewGrid.AreaScale = _metersPerPixel / ShapeShifter.ShapeShifter.ZoomFactor;
        }

        /* SetOverview
         * creates an image of the entire map which is then stored in _overviewImage if
         * the image doesn't already exist.
         * It then calls CompositeOverview to combine an overlay if it exists
         */
        private void SetOverview()
        {
            if (_shapeManager == null)
            {
                return;
            }

            if (_overviewImage == null)
            {
                var procWindow = GetProcessWindow();

                var box = new BoundingBox()
                {
                    Xmin = _shapeManager.Xmin,
                    Xmax = _shapeManager.Xmax,
                    Ymin = _shapeManager.Ymin,
                    Ymax = _shapeManager.Ymax
                };

                var exclusionList = GetExclusionList();
                _shapeManager.SetArea(box, exclusionList);
                var areaOnly = _shapeManager.GetArea();

                var width = (int)_mapViewGrid.RenderSize.Width;
                var aspectY = _shapeManager.Height / _shapeManager.Width;
                var height = (int)(_mapViewGrid.RenderSize.Width * aspectY);

                _overviewImage = ShapeRender.ShapeRender.RenderShapeFile(areaOnly, width, height, false);

                CompositeOverview();

                CloseProcessWindow(procWindow);
            }
        }

        /* CompositeOverview
         * combines the overview image with the overlay image if it exists
         * and then renders the result to the overview tab
         */
        private void CompositeOverview()
        {
            if (_overviewImage == null)
            {
                return;
            }

            Bitmap resultBitmap = new Bitmap(_overviewImage.Width, _overviewImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(resultBitmap))
            { 
                graphics.Clear(System.Drawing.Color.Transparent);
                var alpha1 = SetImageOpacity(_overviewImage, 1);
                graphics.DrawImage(_overviewImage, 0, 0);

                if (_overlayImage != null)
                {
                    var alpha2 = SetImageOpacity(_overlayImage, 0.5f);
                    graphics.DrawImage(alpha2, 0, 0);
                }

                using (MemoryStream memory = new MemoryStream())
                {
                    resultBitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    ((System.Windows.Controls.Image)_mapOverViewGrid.Child).Source = bitmapImage;
                }

                _mapOverViewGrid.Height = resultBitmap.Height;
                
                
                if (resultBitmap.Height <= (int)_mapViewGrid.RenderSize.Height)
                {
                    _mapOverViewGrid.Enabled = false;
                }
                else
                {
                    _mapOverViewGrid.Enabled = true;
                }

                _mapOverViewGrid.Reset();
            }
        }

        /* SetImageOpacity
         * forces opacity on an image
         */
        private Bitmap SetImageOpacity(Bitmap image, float opacity)
        {
            //create a Bitmap the size of the image provided  
            Bitmap bmp = new Bitmap(image.Width, image.Height);

            //create a graphics object from the image  
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                //create a color matrix object as set opactiy
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = opacity;
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                //now draw the image  
                gfx.DrawImage(image, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        private List<string> GetExclusionList()
        {
            var exclusionList = new List<string>();

            foreach (var item in _shapeEntities)
            {
                if (!item.IsSelected)
                {
                    exclusionList.Add(item.FilePath);
                }
            }
            return exclusionList;
        }

        private void Button_SetArea(object sender, RoutedEventArgs e)
        {
            SetNewArea();
        }

        private double _windowX;
        private double _windowY;

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _metersPerPixel = e.NewValue * ShapeShifter.ShapeShifter.ZoomFactor < 1 ? 1 : e.NewValue * ShapeShifter.ShapeShifter.ZoomFactor;
            ZoomVal.Text = $"{_metersPerPixel:0}";
            SetNewArea();
        }

        private void _mapViewGrid_Refresh(object sender, PanZoomRefreshEventArgs e)
        {
            if (_shapeManager == null)
            {
                return;
            }

            _hideMouseTip = false;

            var currentX = _windowX * _shapeManager.Width;
            var scaleX = _metersPerPixel / _mapViewGrid.RenderSize.Width;
            currentX += (e.Pan.X * scaleX);
            var distFractionX = currentX / _shapeManager.Width;
            _windowX = distFractionX;

            var aspectY = _mapViewGrid.RenderSize.Height / _mapViewGrid.RenderSize.Width;

            var currentY = _windowY * _shapeManager.Height;
            var scaleY = _metersPerPixel / _mapViewGrid.RenderSize.Height;
            currentY -= (e.Pan.Y * scaleY) * aspectY;
            var distFractionY = currentY / _shapeManager.Height;
            _windowY = distFractionY;
            
            _mapViewGrid.Reset();
            
            SetNewArea();
        }

        private void _mapViewGrid_Move(object sender, PanZoomRefreshEventArgs e)
        {
            var panMove = (ZoomBorder)sender;
            var left = panMove.BoxX - (_metersPerPixel / 2);
            var right = panMove.BoxX + (panMove.BoxWidth / 2);
            var top = panMove.BoxY - (panMove.BoxHeight / 2);
            var bottom = panMove.BoxY + (panMove.BoxHeight / 2);
            /*
            MapX.Text = $"{panMove.BoxX:0}";
            MapY.Text = $"{panMove.BoxY:0}";

            MapLeft.Text = $"{left:0}";
            MapRight.Text = $"{right:0}";
            MapTop.Text = $"{top:0}";
            MapBottom.Text = $"{bottom:0}";

            MapWidth.Text = $"{panMove.AreaWidth:0}";
            MapHeight.Text = $"{panMove.AreaHeight:0}";
            */
        }

        private void _mapViewGrid_Zoom(object sender, PanZoomRefreshEventArgs e)
        {
            var pan = (ZoomBorder)sender;
            _metersPerPixel = pan.AreaScale * ShapeShifter.ShapeShifter.ZoomFactor < 1 ? 1 : pan.AreaScale * ShapeShifter.ShapeShifter.ZoomFactor;
            _mapViewGrid_Refresh(sender, e);
        }

        private void Button_MapExport(object sender, RoutedEventArgs e)
        {
            if (_shapeManager == null)
            {
                return;
            }

            var totalItems = _shapeManager.Summary.Sum(s => s.ItemCount);

            if (totalItems > 500000)
            {
                if (MessageBox.Show("Large map, are you sure?",  "", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    return;
                }
                
            }

            var saveFileDialog = new SaveFileDialog()
            {
                Filter = "PNG Image|*.png",
                Title = "Save map image",
                FileName = "map.png"
            };

            if (!string.IsNullOrEmpty(_exportFolder))
            {
                saveFileDialog.InitialDirectory = _exportFolder;
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                _exportFolder = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);

                var width = (int)_mapViewGrid.RenderSize.Width;
                var height = (int)_mapViewGrid.RenderSize.Height;

                var totalBox = new BoundingBox()
                {
                    Xmax = _shapeManager.Xmax,
                    Xmin = _shapeManager.Xmin, 
                    Ymax = _shapeManager.Ymax,
                    Ymin = _shapeManager.Ymin
                };

                var exclusionList = GetExclusionList();

                _shapeManager.SetArea(totalBox, exclusionList);
                
                var shapeFile = _shapeManager.GetArea();

                var imageWidht = 10000;
                var imageHeight = (int)(10000 * (_shapeManager.Height / _shapeManager.Width));

                var saveImage = ShapeRender.ShapeRender.RenderShapeFile(shapeFile, imageWidht, imageHeight, false);

                saveImage.Save(saveFileDialog.FileName, ImageFormat.Png);

                MessageBox.Show("Map export complete");
            }
        }

        /* Map mouse move
         * 
         * updatges the tool tip for X / Y values
         * In all honesty this turned into a bit of a mess which I
         * don't fully understand and had to re-hack the Y result to
         * make it correct
         */
        private void Map_MouseMove(object sender, MouseEventArgs e)
        {
            if (_shapeManager != null)
            {
                // set box pan limits and sizes
                var aspectY = _mapViewGrid.RenderSize.Height / _mapViewGrid.RenderSize.Width;

                var mouseX = e.GetPosition((IInputElement)sender).X;
                var mouseY = e.GetPosition((IInputElement)sender).Y;

                var x = (mouseX / _mapViewGrid.RenderSize.Width) - 0.5;
                var y = (mouseY / _mapViewGrid.RenderSize.Height) - 0.5;
                x = (_windowX * _shapeManager.Width) + (x * _metersPerPixel);
                y = (_windowY * _shapeManager.Height) - (y * _metersPerPixel * aspectY);

                x = _shapeManager.Xmin + x;
                y = _shapeManager.Ymax - (_shapeManager.Width - y);

                if (!_detailToolTip.IsOpen) 
                { 
                    _detailToolTip.IsOpen = true; 
                }

                _detailToolTip.HorizontalOffset = mouseX + 20;
                _detailToolTip.VerticalOffset = mouseY;
                if (!_hideMouseTip)
                {
                    var textBlock = (TextBlock)_detailToolTip.Child;
                    textBlock.Text = $" X: {x:0.00} \n Y: {y:0.00} ";
                }
            }
        }

        /* Overview mouse over
         * 
         * updates the tool tip with X / Y position
         * this one is much cleaner than the other
         */
        private void Overview_MouseMove(object sender, MouseEventArgs e)
        {
            if (_overviewImage == null)
                return;

            if (_shapeManager != null)
            {
                var mouseX = e.GetPosition((IInputElement)sender).X;
                var mouseY = e.GetPosition((IInputElement)sender).Y;

                // get map position from control
                var mapPos = OverviewToMapPos(sender, e);

                if (!_overviewToolTip.IsOpen)
                {
                    _overviewToolTip.IsOpen = true;
                }
                _detailToolTip.IsOpen = false;

                _overviewToolTip.HorizontalOffset = mouseX + 20;
                _overviewToolTip.VerticalOffset = mouseY;

                if (!_hideMouseTip)
                {
                    var textBlock = (TextBlock)_overviewToolTip.Child;
                    textBlock.Text = $" X: {mapPos.X:0.00} \n Y: {mapPos.Y:0.00} ";
                }
            }
        }

        /* Uses a mouse event to get the map position from the overview control 
         */
        private ShapePoint OverviewToMapPos(object sender, MouseEventArgs e)
        {
            // set box pan limits and sizes
            var aspectY = _mapViewGrid.RenderSize.Height / _mapViewGrid.RenderSize.Width;

            var mouseX = e.GetPosition((IInputElement)sender).X;
            var mouseY = e.GetPosition((IInputElement)sender).Y;

            var scrollDiff = _mapOverViewGrid.OffsetY;
            var x = (mouseX / _overviewImage.Width) * _shapeManager.Width;
            var y = ((mouseY - scrollDiff) / _overviewImage.Height) * _shapeManager.Height;

            x = _shapeManager.Xmin + x;
            y = _shapeManager.Ymax - y;

            return new ShapePoint()
            {
                X = x,
                Y = y   
            };
        }

        private void Overview_MouseLeave(object sender, MouseEventArgs e)
        {
            _overviewToolTip.IsOpen = false;
        }

        private void Map_MouseLeave(object sender, MouseEventArgs e)
        {
            _detailToolTip.IsOpen = false;
        }

        private void Map_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _hideMouseTip = true;
        }

        private void Overview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                var mapPos = OverviewToMapPos(sender, e);
                FindPointInOverlay(mapPos);
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _hideMouseTip = true;
                }

                if (e.RightButton == MouseButtonState.Pressed)
                {
                    OverviewToDetail(sender, e);
                }
            }
        }
        /* Find Point in Overlay
         * 
         * using a combination of the mouse and map scroll position,
         * create a point on the map. With point determined, check
         * all overlay regions to see if that point is within them.
         * First overlay found then picks that single region from
         * the drop down and generates a map with only that selected.
         */
        private void FindPointInOverlay(ShapePoint point)
        {
            if (_shapeManager.OverlayCache.Items.Count == 0)
                return;

            // convert the point into a box
            var box = new BoundingBox()
            {
                Xmax = point.X,
                Xmin = point.X,
                Ymax = point.Y,
                Ymin = point.Y
            };

            var records = new List<ShapeCacheItem>();

            var overlays = _shapeManager.GetOverlayHits();

            //check if it intersects with any of the overlay boxes
            foreach (var item in _shapeManager.OverlayCache.Items)
            {
                if (item.Box.Intersects(box))
                {
                    var shapeRecord = ShapeShifter.ShapeShifter.GetSingleRecord(_shapeManager.OverlayCache, item.RecordId);
                    foreach (var poly in shapeRecord.PolygonOverlays)
                    {
                        foreach (var basePoly in poly.Polygons)
                        {
                            if (basePoly.PointInPoly(point))
                            {
                                var thisOverlay = overlays.Where(x => x.RecordId == item.RecordId).FirstOrDefault();
                                foreach (ShapeRegion comboItem in _osHits.Items)
                                {
                                    if (thisOverlay?.RecordId == comboItem.RecordId)
                                    {
                                        _osHits.SelectedItem = comboItem;
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // if we get here, we didn't find a hit
            _osHits.SelectedItem = null;
        }

        private void OverviewToDetail(object sender, MouseButtonEventArgs e)
        {
            if (_overviewImage == null)
                return;

            _overviewToolTip.IsOpen = false;
            _hideMouseTip = false;

            var mouseX = e.GetPosition((IInputElement)sender).X;
            var mouseY = e.GetPosition((IInputElement)sender).Y;

            var scrollDiff = _mapOverViewGrid.OffsetY;
            var x = (mouseX / _overviewImage.Width);
            var y = 1 - ((mouseY - scrollDiff) / _overviewImage.Height);
            _windowX = x;
            _windowY = y;
            SetNewArea();
            _tabs.SelectedIndex = 0;

        }

        private void Map_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _hideMouseTip = true;
        }

        private void ApplyOverlay_Click(object sender, RoutedEventArgs e)
        {
            // safety checks
            if (_osMaps.SelectedIndex == -1)
                return;

            if (_shapeManager == null)
                return; 

            ApplyOverlay();
        }

        /* Apply Overlay
         * 
         * Using the selected overlay from the drop down, it determines which
         * regions fall withing the currently loaded map. These are then
         * added to a secondary drop down for user selection.
         * 
         * It then generates a new overlay map and composites it with the
         * overview.
         */
        private void ApplyOverlay()
        {
            var osMap = (ShapeSummary)_osMaps.SelectedItem;

            var crossRef = ShapeShifter.ShapeShifter.CreateShapeCacheFromFile(osMap.FilePath);

            _shapeManager.CrossRef(crossRef);

            OverlayHitsText.Text = $"{_shapeManager.OverlayCache.Items.Count}";
            
            var hitList = _shapeManager.GetOverlayHits();
            hitList.Sort((x, y) => x.Name.CompareTo(y.Name));

            _osHits.Items.Clear();
            foreach (var hit in hitList)
            {
                _osHits.Items.Add(hit);
            }

            GetOverlayImage();
            CompositeOverview();
        }

        /* Get Overlay Image
         * 
         * Generates an overlay image for a specific recordId within
         * the overlay cache. If the recordId is not passed in
         * then it defaults to -1 which generates the image for all 
         * regions.
         * 
         * The regions are scaled based on the slider.
         */
        private void GetOverlayImage(int recordId = -1)
        {
            if (_shapeManager == null)
            {
                return;
            }

            if (_osMaps.SelectedItem == null)
            {
                return;
            }

            var box = new BoundingBox()
            {
                Xmax = _shapeManager.Xmax,
                Xmin = _shapeManager.Xmin,
                Ymin = _shapeManager.Ymin,
                Ymax = _shapeManager.Ymax
            };

            var tempList = new List<ShapeCache>();

            if (recordId == -1)
            {
                tempList.Add(_shapeManager.OverlayCache);
            }
            else
            {
                var temp = new ShapeCache();
                temp.FilePath = _shapeManager.OverlayCache.FilePath;
                temp.DbfPath =  _shapeManager.OverlayCache.DbfPath;
                temp.BoundingBox = _shapeManager.OverlayCache.BoundingBox;
                temp.Overlay = _shapeManager.OverlayCache.Overlay;
                temp.Items.Add(_shapeManager.OverlayCache.Items.Where(x => x.RecordId == recordId).FirstOrDefault());
                tempList.Add(temp);
            }

            var overlayShape = ShapeShifter.ShapeShifter.CreateShapeFileFromCache(tempList, box);

            var width = (int)_mapViewGrid.RenderSize.Width;

            var aspectY = _shapeManager.Height / _shapeManager.Width;

            var height = (int)(_mapViewGrid.RenderSize.Width * aspectY);

            _overlayImage = ShapeRender.ShapeRender.RenderShapeFile(overlayShape, width, height, false);
        }
        

        private void _tabOverview_Selected(object sender, RoutedEventArgs e)
        {
            SetOverview();
        }


        private void CutRegion_Click(object sender, RoutedEventArgs e)
        {
            if (_osHits.SelectedItem == null)
            {
                return;
            }

            CutRegion();
        }

        private void CalcRegion_Click(object sender, RoutedEventArgs e)
        {
            if (_osHits.SelectedItem == null)
            {
                return;
            }

            CalcRegion();
        }


        private void CutRegionAll_Click(object sender, RoutedEventArgs e)
        {
            CutRegionAll();
        }

        private void CutBox_Click(object sender, RoutedEventArgs e)
        {
            if (_osHits.SelectedItem == null)
            {
                return;
            }

            CutBox();
        }

        /* Cut Region All
         * 
         * Same as CutRegion except it processes all regions which have been
         * applied to the current map.
         */
        private void CutRegionAll()
        {
            var openFolder = new FolderBrowserDialog()
            {
                Title = "Select a folder destination for cut ESRI folders",
                AllowMultiSelect = false
            };

            if (!string.IsNullOrEmpty(_cutFolder))
            {
                openFolder.InitialFolder = _cutFolder;
            }

            var result = openFolder.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            var procWindow = GetProcessWindow();

            var baseFolder = openFolder.SelectedFolder;

            var count = 1;

            foreach (ShapeRegion region in _osHits.Items)
            {
                var thisFolder = System.IO.Path.Combine(baseFolder, region.Name);

                if (!Directory.Exists(thisFolder))
                {
                    Directory.CreateDirectory(thisFolder);
                }

                UpdateProcessWindow(procWindow, $"{count} of {_osHits.Items.Count}");

                // get the selected region record
                var cutList = _shapeManager.CutRegion(region, GetExclusionList(), 1);

                // no we need to build a new set of files in a folder...
                foreach (var cache in cutList)
                {
                    ShapeShifter.ShapeShifter.FileSlicer(cache, thisFolder);
                }

                count++;
            }

            CloseProcessWindow(procWindow);
        }

        /* Cut Box
         * 
         * Produces a new set of shape files based on the current
         * selected regions bounding box.
         * The box is scaled to the size of the slider.
         */
        private void CutBox()
        {
            var openFolder = new FolderBrowserDialog()
            {
                Title = "Select a folder destination for cut ESRI files",
                AllowMultiSelect = false
            };

            if (!string.IsNullOrEmpty(_cutFolder))
            {
                openFolder.InitialFolder = _cutFolder;
            }

            var result = openFolder.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            // check the folder is empty
            var fileList = Directory.GetFiles(openFolder.SelectedFolder, "*.shp");
            if (fileList.Length > 0)
            {
                var youSure = MessageBox.Show("Files exist in this folder, continue?", "Are you sure?", MessageBoxButton.OKCancel);
                if (youSure == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            var procWindow = GetProcessWindow();

            // get the selected region record
            // and scale it to the slider value
            var region = (ShapeRegion)_osHits.SelectedItem;
            var scale = _regionScaleSlider.Value / 100;
            region.Box.Scale(scale);

            // cut it out
            var cutList = _shapeManager.GetCacheArea(region.Box, GetExclusionList());
            if (cutList.Count > 0)
            {
                // build the new set of files
                foreach (var cache in cutList)
                {
                    ShapeShifter.ShapeShifter.FileSlicer(cache, openFolder.SelectedFolder);
                }
            }

            CloseProcessWindow(procWindow);
        }

        private void BoxCutAll_Click(object sender, RoutedEventArgs e)
        {
            CutBoxAll();
        }


        /* Cut Box All
         * 
         * Same as the CutBox function except it operates on all
         * regions which have been applied to the current map.
         */
        private void CutBoxAll()
        {
            var openFolder = new FolderBrowserDialog()
            {
                Title = "Select a folder destination for cut ESRI folders",
                AllowMultiSelect = false
            };

            if (!string.IsNullOrEmpty(_cutFolder))
            {
                openFolder.InitialFolder = _cutFolder;
            }

            var result = openFolder.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            var procWindow = GetProcessWindow();

            var baseFolder = openFolder.SelectedFolder;

            var count = 1;

            var scale = _regionScaleSlider.Value / 100;

            foreach (ShapeRegion region in _osHits.Items)
            {
                region.Box.Scale(scale);

                var cutList = _shapeManager.GetCacheArea(region.Box, GetExclusionList());
                if (cutList.Count > 0)
                {
                    var thisFolder = System.IO.Path.Combine(baseFolder, region.Name);

                    if (!Directory.Exists(thisFolder))
                    {
                        Directory.CreateDirectory(thisFolder);
                    }

                    var updateMessage = $"{count} of {_osHits.Items.Count}";

                    UpdateProcessWindow(procWindow, updateMessage);

                    // no we need to build a new set of files in a folder...
                    foreach (var cache in cutList)
                    {
                        ShapeShifter.ShapeShifter.FileSlicer(cache, thisFolder);
                    }

                    count++;
                }
            }

            CloseProcessWindow(procWindow);
        }


        /* Calc Box
         * 
         * Cuts the current regions bounding box to determine resulting size
         * and display those values on the form.  The box is scaleld to the
         * scale slider.
         */
        private void CalcBox()
        {
            // get the selected region record
            // and scale it to the slider value
            var region = (ShapeRegion)_osHits.SelectedItem;
            var scale = _regionScaleSlider.Value / 100;
            region.Box.Scale(scale);

            // cut it out
            var cutList = _shapeManager.GetCacheArea(region.Box, GetExclusionList());

            var Xmin = cutList.Min(c => c.BoundingBox.Xmin);
            var Ymin = cutList.Min(c => c.BoundingBox.Ymin);
            var Xmax = cutList.Max(c => c.BoundingBox.Xmax);
            var Ymax = cutList.Max(c => c.BoundingBox.Ymax);
            var width = Xmax - Xmin;
            var height = Ymax - Ymin;
            var area = width * height / 1000;

            TextRegionXmin_Copy.Text = $"{Xmin:0.00}";
            TextRegionYmin_Copy.Text = $"{Ymin:0.00}";
            TextRegionXmax_Copy.Text = $"{Xmax:0.00}";
            TextRegionYmax_Copy.Text = $"{Ymax:0.00}";
            TextRegionCount.Text = $"{cutList.Sum(c => c.Items.Count)}";
            TextRegionWidth.Text = $"{width:0.00}";
            TextRegionHeight.Text = $"{height:0.00}";
            TextRegionKm.Text = $"{area:0.00}";
            TextRegionMi.Text = $"{area * KmMi:0.00}";
        }

        /* Calc Region
         * 
         * Cuts the current region to determine resulting size
         * and display those values on the form.
         */
        private void CalcRegion()
        {
            var procWindow = GetProcessWindow();

            // get the selected region record
            var region = (ShapeRegion)_osHits.SelectedItem;
            var cutList = _shapeManager.CutRegion(region, GetExclusionList(), 1);
            if (cutList.Count == 0)
            {
                CloseProcessWindow(procWindow);
                MessageBox.Show("Nothing found in this region.");
                return;
            }

            var Xmin = cutList.Min(c => c.BoundingBox.Xmin);
            var Ymin = cutList.Min(c => c.BoundingBox.Ymin);
            var Xmax = cutList.Max(c => c.BoundingBox.Xmax);   
            var Ymax = cutList.Max(c => c.BoundingBox.Ymax);
            var width = Xmax - Xmin;
            var height = Ymax - Ymin;
            var area = width * height / 1000;

            TextRegionXmin_Copy.Text = $"{Xmin:0.00}";
            TextRegionYmin_Copy.Text = $"{Ymin:0.00}";
            TextRegionXmax_Copy.Text = $"{Xmax:0.00}";
            TextRegionYmax_Copy.Text = $"{Ymax:0.00}";
            TextRegionCount.Text = $"{cutList.Sum(c => c.Items.Count)}";
            TextRegionWidth.Text = $"{width:0.00}";
            TextRegionHeight.Text = $"{height:0.00}";
            TextRegionKm.Text = $"{area:0.00}";
            TextRegionMi.Text = $"{area * KmMi:0.00}";

            CloseProcessWindow(procWindow);
        }

        /* Cut Region
         * 
         * cuts and exports a new shape folder based on the polygon 
         * of the currently selected region.
         * Scaling is not implemented here as the polygon shape
         * can potentially miss things.
         */
        private void CutRegion()
        {
            var openFolder = new FolderBrowserDialog()
            {
                Title = "Select a folder destination for cut ESRI files",
                AllowMultiSelect = false
            };

            if (!string.IsNullOrEmpty(_cutFolder))
            {
                openFolder.InitialFolder = _cutFolder;
            }

            var result = openFolder.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            var fileList = Directory.GetFiles(openFolder.SelectedFolder, "*.shp");
            if (fileList.Length > 0)
            {
                var youSure = MessageBox.Show("Files exist in this folder, continue?", "Are you sure?", MessageBoxButton.OKCancel);
                if (youSure == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            
            var procWindow = GetProcessWindow();

            // get the selected region record
            var region = (ShapeRegion)_osHits.SelectedItem;
            var cutList = _shapeManager.CutRegion(region, GetExclusionList(), 1);
            if (cutList.Count == 0)
            {
                MessageBox.Show("Nothing found in this region.");
                return;
            }

            // no we need to build a new set of files in a folder...
            foreach (var cache in cutList)
            {
                ShapeShifter.ShapeShifter.FileSlicer(cache, openFolder.SelectedFolder);
            }

            CloseProcessWindow(procWindow);
        }

        private void Button_RefreshOveriew(object sender, RoutedEventArgs e)
        {
            if (_shapeManager == null)
            {
                return;
            }

            _overviewImage = null;
            SetOverview();
        }

        private void Overview_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _hideMouseTip = false;
        }

        /* event for region selection change.
         * 
         * This event is fired when the user selects a region from the list.
         * It will then load the overlay image for that region and composite
         * it with the overview image.
         */
        private void _osHits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = (System.Windows.Controls.ComboBox)sender;
            if (combo.SelectedItem != null)
            {
                var item = (ShapeRegion)combo.SelectedItem;
                GetOverlayImage(item.RecordId);
                CompositeOverview();
            }
        }

        /* Get Process Window
         * 
         * Simple function which instansiates a new processing window to
         * feedback to the user that stuff is happening.
         */
        private System.Windows.Window GetProcessWindow()
        {
            var window = new System.Windows.Window()
            {
                Width = 300,
                Height = 150,
                Title = "Processing..",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            window.Content = new TextBlock()
            {
                Text = "Please wait..",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10)
            };

            window.Owner = this;
            window.Show();
            this.IsEnabled = false;
            DoEvents();
            return window;
        }

        /* Update Process Window
         * 
         * Takes a window and string and updates the current text.
         */
        private void UpdateProcessWindow(System.Windows.Window window, string message)
        {
            window.Content = new TextBlock()
            {
                Text = message,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10)
            };
            this.UpdateLayout();
            DoEvents();
        }

        private void CloseProcessWindow(System.Windows.Window window)
        {
            this.IsEnabled = true;
            window.Close();
        }

        /* Do Events
         * 
         * Hacky method to ensure the UI gets updated when code is blocking.
         */
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(
                    delegate (object f)
                    {
                        ((DispatcherFrame)f).Continue = false;
                        return null;
                    }), frame);
            Dispatcher.PushFrame(frame);
        }

        private void _regionScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_regionScaleText != null)
            {
                var slider = (Slider)sender;
                _regionScaleText.Text = $"Cut Scale: {slider.Value:0.00}%";

                if (_overlayImage != null)
                {
                    var recordId = -1;

                    if (_osHits.SelectedItem != null)
                    {
                        var item = (ShapeRegion)_osHits.SelectedItem;
                        recordId = item.RecordId;
                    }
                    
                    GetOverlayImage(recordId);
                    CompositeOverview();
                }
            }
        }

        private void CalcBox_Click(object sender, RoutedEventArgs e)
        {
            if (_osHits.SelectedItem == null)
            {
                return;
            }

            CalcBox();
        }
    }
}

