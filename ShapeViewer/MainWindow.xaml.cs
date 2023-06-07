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

namespace ShapeViewer
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private ShapeManager _shapeManager;
        private double _windowRatioY;
        private string _folder;
        private double _metersPerPixel;
        private System.Windows.Shapes.Rectangle _viewWindow;
        private bool _rendering = false;
        private bool _fileLoaded = false;
        private bool _hideMouseTip = false;

        private Bitmap _overviewImage;
        private Bitmap _overlayImage;

        private string _shapeFolder = "";
        private string _exportFolder = "";
        private string _cutFolder = "";

        public ObservableCollection<ShapeSummary> _shapeEntities { get; set; } = new ObservableCollection<ShapeSummary>();

        private string _osMapFolder;

        public MainWindow()
        {
            InitializeComponent();

            _metersPerPixel = 1000;

            _windowX = 0.5;
            _windowY = 0.5;

            _osMapFolder = @"D:\_OS_\BoundaryData\";
            LoadOSMaps();
        }

        private void LoadOSMaps()
        {
            foreach (var filename in Directory.GetFiles(_osMapFolder, "*.shp"))
            {
                var osItem = new ShapeSummary()
                {
                    FileName = filename,
                    FilePath = filename
                };
                _osMaps.Items.Add(osItem);
            }
        }
            
        private void Button_Click(object sender, RoutedEventArgs e)
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
                    _overviewImage = null;
                    _overlayImage = null;

                    _shapeManager = new ShapeManager(path);
                    _folder = path;
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
            XminView.Text = $"{_shapeManager.Xmin:0}";
            XmaxView.Text = $"{_shapeManager.Xmax:0}";
            YminView.Text = $"{_shapeManager.Ymin:0}";
            YmaxView.Text = $"{_shapeManager.Ymax:0}";
            ItemCount.Text = _shapeManager.RecordCount.ToString();
            WidthText.Text = $"{_shapeManager.Width:0}";
            HeightText.Text = $"{_shapeManager.Height:0}";
            //_windowX = _shapeManager.Width / 2;
            //_windowY = _shapeManager.Height / 2;
        }

        private void SetNewArea()
        {
            if (!_fileLoaded)
            {
                return;
            }

            //var metersX = _mapViewGrid.RenderSize.Width / _shapeManager.Width * _metersPerPixel;
            //var metersY = _mapViewGrid.RenderSize.Height / _shapeManager.Height * _metersPerPixel;

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

            TextAreaXmin.Text = $"{box.Xmin:0}";
            TextAreaXmax.Text = $"{box.Xmax:0}";
            TextAreaYmin.Text = $"{box.Ymin:0}";
            TextAreaYmax.Text = $"{box.Ymax:0}";

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
            _mapViewGrid.AreaScale = _metersPerPixel / 1000;
        }

        private void SetOverview()
        {
            if (_shapeManager == null)
            {
                return;
            }

            if (_overviewImage == null)
            {

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
            }
        }

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
                _mapOverViewGrid.Reset();
            }

        }

        private Bitmap SetImageOpacity(Bitmap image, float opacity)
        {
            //create a Bitmap the size of the image provided  
            Bitmap bmp = new Bitmap(image.Width, image.Height);

            //create a graphics object from the image  
            using (Graphics gfx = Graphics.FromImage(bmp))
            {

                //create a color matrix object  
                ColorMatrix matrix = new ColorMatrix();

                //set the opacity  
                matrix.Matrix33 = opacity;

                //create image attributes  
                ImageAttributes attributes = new ImageAttributes();

                //set the color(opacity) of the image  
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
            _metersPerPixel = e.NewValue * 1000 < 1 ? 1 : e.NewValue * 1000;
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
            _metersPerPixel = pan.AreaScale * 1000 < 1 ? 1 : pan.AreaScale * 1000;
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
                y = _shapeManager.Ymax - y;
                //_pointPosX.Text = $"{x:0}";
                //_pointPosY.Text = $"{y:0}";

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
                                    if (thisOverlay.RecordId == comboItem.RecordId)
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
            if (_osMaps.SelectedIndex == -1)
                return;

            if (_shapeManager == null)
                return; 

            ApplyOverlay();
        }

        private void ApplyOverlay()
        {
            var osMap = (ShapeSummary)_osMaps.SelectedItem;

            var crossRef = ShapeShifter.ShapeShifter.CreateShapeCacheFromFile(osMap.FilePath);

            _shapeManager.CrossRef(crossRef);

            OverlayHitsText.Text = $"{_shapeManager.OverlayCache.Items.Count}";
            var hitList = _shapeManager.GetOverlayHits();
            _osHits.Items.Clear();
            foreach (var hit in hitList)
            {
                _osHits.Items.Add(hit);
            }

            GetOverlayImage();
            CompositeOverview();
            //SetNewArea();

        }

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

            var baseFolder = openFolder.SelectedFolder;

            foreach (ShapeRegion region in _osHits.Items)
            {
                var thisFolder = System.IO.Path.Combine(baseFolder, region.Name);

                if (!Directory.Exists(thisFolder))
                {
                    Directory.CreateDirectory(thisFolder);
                }

                // get the selected region record
                var cutList = _shapeManager.CutRegion(region, GetExclusionList());

                // no we need to build a new set of files in a folder...
                foreach (var cache in cutList)
                {
                    ShapeShifter.ShapeShifter.FileSlicer(cache, thisFolder);
                }
            }

            MessageBox.Show("Done");
        }

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

            var fileList = Directory.GetFiles(openFolder.SelectedFolder, "*.shp");
            if (fileList.Length > 0)
            {
                var youSure = MessageBox.Show("Files exist in this folder, continue?", "Are you sure?", MessageBoxButton.OKCancel);
                if (youSure == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // get the selected region record
            var region = (ShapeRegion)_osHits.SelectedItem;

            var cutList = _shapeManager.GetCacheArea(region.Box, GetExclusionList());
            if (cutList.Count > 0)
            {
                var thisFolder = System.IO.Path.Combine(openFolder.SelectedFolder, region.Name);

                if (!Directory.Exists(thisFolder))
                {
                    Directory.CreateDirectory(thisFolder);
                }

                // no we need to build a new set of files in a folder...
                foreach (var cache in cutList)
                {
                    ShapeShifter.ShapeShifter.FileSlicer(cache, thisFolder);
                }
            }
            MessageBox.Show("Done");
        }

        private void QuickCut_Click(object sender, RoutedEventArgs e)
        {
            QuickCut();
        }



        private void QuickCut()
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

            var baseFolder = openFolder.SelectedFolder;

            foreach (ShapeRegion region in _osHits.Items)
            {
                var cutList = _shapeManager.GetCacheArea(region.Box, GetExclusionList());
                if (cutList.Count > 0)
                {
                    var thisFolder = System.IO.Path.Combine(baseFolder, region.Name);

                    if (!Directory.Exists(thisFolder))
                    {
                        Directory.CreateDirectory(thisFolder);
                    }

                    // no we need to build a new set of files in a folder...
                    foreach (var cache in cutList)
                    {
                        ShapeShifter.ShapeShifter.FileSlicer(cache, thisFolder);
                    }
                }
            }

            MessageBox.Show("Done");
        }

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

            // get the selected region record
            var region = (ShapeRegion)_osHits.SelectedItem;
            var cutList = _shapeManager.CutRegion(region, GetExclusionList());
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

            MessageBox.Show("Done");
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
    }
}

