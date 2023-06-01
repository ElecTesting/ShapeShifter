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

        public ObservableCollection<ShapeSummary> _shapeEntities { get; set; } = new ObservableCollection<ShapeSummary>();

        public MainWindow()
        {
            InitializeComponent();

            _metersPerPixel = 1000;
            
            _windowX = 0.5;
            _windowY = 0.5;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var openFolder = new FolderBrowserDialog()
            {
                Title = "Select a folder containing ESRI files",
                AllowMultiSelect = false
            };

            var result = openFolder.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var path = openFolder.SelectedFolder;
                var shapeFileList = Directory.GetFiles(path, "*.shp");
                if (shapeFileList.Length == 0)
                {
                    // uh oh
                    MessageBox.Show("No shape files found in this folder");
                }
                else
                {
                    _shapeManager = new ShapeManager(path);
                    _folder = path;
                    ShowStats();

                    GetFileItems(_shapeManager.Summary);

                    _windowX = 0.5;
                    _windowY = 0.5;
                    _fileLoaded = true;
                    ZoomSlider.Value = 1;
                    SetNewArea();
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
            WidthText.Text = _shapeManager.Width.ToString();
            HeightText.Text = _shapeManager.Height.ToString();
            //_windowX = _shapeManager.Width / 2;
            //_windowY = _shapeManager.Height / 2;
        }

        private void SetNewArea()
        {
            if (!_fileLoaded)
            {
                return;
            }

            if (!_rendering)
            {
                _rendering = true;
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

                var exclusionList = new List<string>();

                foreach (var item in _shapeEntities)
                {
                    if (!item.IsSelected)
                    {
                        exclusionList.Add(item.FilePath);
                    }
                }

                var itemCount = _shapeManager.SetArea(box, exclusionList);
                ItemsArea.Text = itemCount.ToString();

                var areaOnly = _shapeManager.GetArea();

                //ImageDump.Stretch = Stretch.Uniform;
                //ImageDump.StretchDirection = StretchDirection.Both;

                //var shapeTest = ShapeShifter.ShapeShifter.MergeAllShapeFiles(_folder);
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
                _rendering = false;
            }
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

            MapX.Text = $"{panMove.BoxX:0}";
            MapY.Text = $"{panMove.BoxY:0}";

            MapLeft.Text = $"{left:0}";
            MapRight.Text = $"{right:0}";
            MapTop.Text = $"{top:0}";
            MapBottom.Text = $"{bottom:0}";

            MapWidth.Text = $"{panMove.AreaWidth:0}";
            MapHeight.Text = $"{panMove.AreaHeight:0}";
        }

        private void _mapViewGrid_Zoom(object sender, PanZoomRefreshEventArgs e)
        {
            var pan = (ZoomBorder)sender;
            _metersPerPixel = pan.AreaScale * 1000 < 1 ? 1 : pan.AreaScale * 1000;
            _mapViewGrid_Refresh(sender, e);
        }

        private void Button_MapExport(object sender, RoutedEventArgs e)
        {
            var totalItems = _shapeManager.Summary.Sum(s => s.ItemCount);

            if (totalItems > 500000)
            {
                if (MessageBox.Show("Large map, are you sure?") == MessageBoxResult.Cancel)
                {
                    return;
                }
                
            }

            var saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                var width = (int)_mapViewGrid.RenderSize.Width;
                var height = (int)_mapViewGrid.RenderSize.Height;

                var totalBox = new BoundingBox()
                {
                    Xmax = _shapeManager.Xmax,
                    Xmin = _shapeManager.Xmin, 
                    Ymax = _shapeManager.Ymax,
                    Ymin = _shapeManager.Ymin
                };

                _shapeManager.SetArea(totalBox);
                var shapeFile = _shapeManager.GetArea();

                var saveImage = ShapeRender.ShapeRender.RenderShapeFile(shapeFile, 10000, 10000, false);

                saveImage.Save(saveFileDialog.FileName, ImageFormat.Png);
            }
        }

        private void Map_MouseMove(object sender, MouseEventArgs e)
        {
            if (_shapeManager != null)
            {
                // set box pan limits and sizes
                var aspectY = _mapViewGrid.RenderSize.Height / _mapViewGrid.RenderSize.Width;

                var x = (e.GetPosition((IInputElement)sender).X / _mapViewGrid.RenderSize.Width) - 0.5;
                var y = -((e.GetPosition((IInputElement)sender).Y / _mapViewGrid.RenderSize.Height) - 0.5);
                x = (_windowX * _shapeManager.Width) + (x * _metersPerPixel);
                y = (_windowY * _shapeManager.Height) + (y * _metersPerPixel * aspectY);
                x = _shapeManager.Xmin + x;
                y = _shapeManager.Ymax - y;
                _pointPosX.Text = $"{x:0}";
                _pointPosY.Text = $"{y:0}";
            }
        }
    }
}

