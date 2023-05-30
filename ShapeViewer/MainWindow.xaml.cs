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

        public MainWindow()
        {
            InitializeComponent();

            _metersPerPixel = 1.0;
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

                    _windowX = 0.5;
                    _windowY = 0.5;
                    _fileLoaded = true;
                    ZoomSlider_ValueChanged(null, new RoutedPropertyChangedEventArgs<double>(0,1));
                    SetNewArea();
                }
            }
        }

        private void ShowStats()
        {
            XminView.Text = _shapeManager.Xmin.ToString();
            XmaxView.Text = _shapeManager.Xmax.ToString();
            YminView.Text = _shapeManager.Ymin.ToString();
            YmaxView.Text = _shapeManager.Ymax.ToString();
            Items.Text = _shapeManager.RecordCount.ToString();
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

                TextAreaXmin.Text = box.Xmin.ToString();
                TextAreaXmax.Text = box.Xmax.ToString();
                TextAreaYmin.Text = box.Ymin.ToString();
                TextAreaYmax.Text = box.Ymax.ToString();

                var temp = _shapeManager.SetArea(box);
                ItemsArea.Text = temp.ToString();

                var areaOnly = _shapeManager.GetArea();

                //ImageDump.Stretch = Stretch.Uniform;
                //ImageDump.StretchDirection = StretchDirection.Both;

                //var shapeTest = ShapeShifter.ShapeShifter.MergeAllShapeFiles(_folder);
                var width = (int)_mapViewGrid.RenderSize.Width;
                var height = (int)_mapViewGrid.RenderSize.Height;
                var testImage = ShapeRender.ShapeRender.RenderShapeFile(areaOnly, width, height);
                
                using (MemoryStream memory = new MemoryStream())
                {
                    testImage.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    //ImageDump.Source = bitmapImage;
                    ((System.Windows.Controls.Image)_mapViewGrid.Child).Source = bitmapImage;
                    //_mapViewGrid.Fill = new ImageBrush(bitmapImage);
                }
                _rendering = false;
            }
        }

        private void Button_SetArea(object sender, RoutedEventArgs e)
        {
            SetNewArea();
        }

        private bool _drag = false;
        private System.Windows.Point _startPoint;
        private double _windowX;
        private double _windowY;
        private double _posMetersX;

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _metersPerPixel = e.NewValue * 1000 < 1 ? 1 : e.NewValue * 1000;
            ZoomVal.Text = string.Format("{0:0.00}", _metersPerPixel);
            SetNewArea();
        }

        private void Button_GetArea(object sender, RoutedEventArgs e)
        {

        }

        private void _mapViewGrid_Refresh(object sender, PanZoomRefreshEventArgs e)
        {
            var currentX = _windowX * _shapeManager.Width;
            var scaleX = _metersPerPixel / _mapViewGrid.RenderSize.Width;
            currentX += (e.Pan.X * scaleX);
            var distFractionX = currentX / _shapeManager.Width;
            _windowX = distFractionX;

            var aspectY = _mapViewGrid.RenderSize.Height / _mapViewGrid.RenderSize.Width;

            var currentY = _windowY * _shapeManager.Height;
            var scaleY = _metersPerPixel / _mapViewGrid.RenderSize.Height;
            currentY += (e.Pan.Y * scaleY) * aspectY;
            var distFractionY = currentY / _shapeManager.Height;
            _windowY = distFractionY;
            
            _mapViewGrid.Reset();
            SetNewArea();
        }
    }
}
