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

namespace ShapeViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ShapeManager _shapeManager;
        private double _windowRatioY;
        private string _folder;
        private double _meters;
        private System.Windows.Shapes.Rectangle _viewWindow;
        private bool _rendering = false;

        public MainWindow()
        {
            InitializeComponent();

            TextAreaXmin.Text = "400000";
            TextAreaXmax.Text = "410000";
            TextAreaYmin.Text = "430000";
            TextAreaYmax.Text = "440000";

            _meters = 1.0;
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
                    SetViewWindow();
                }
            }
        }

        private const int _windowSize = 200;

        // MouseLeftButtonDown="WinDown" MouseMove="WinMove" MouseLeftButtonUp="WinUp" 

        private void SetViewWindow()
        {
            var viewHeightPercent = _shapeManager.Height / _shapeManager.Width;
            var newHeight = _windowSize * viewHeightPercent;
            
            var viewPortSize = _meters / _shapeManager.Width * _windowSize;

            _canvas.Width = _windowSize;
            _canvas.Height = newHeight;
            _canvas.Background = System.Windows.Media.Brushes.LightGray;
            _canvas.Children.Clear();

            // how do you programatically add controls to a WPF window?
            _viewWindow = new System.Windows.Shapes.Rectangle();
            _viewWindow.Width = viewPortSize;
            _viewWindow.Height = viewPortSize;
            _viewWindow.Fill = new SolidColorBrush(Colors.RoyalBlue);
            // assign handlers
            _viewWindow.MouseDown += ViewWindow_MouseDown;
            _viewWindow.MouseMove += ViewWindow_MouseMove;
            _viewWindow.MouseUp += ViewWindow_MouseUp;
            
            Canvas.SetLeft(_viewWindow, (_canvas.Width * _windowX) - _viewWindow.Width/2);
            Canvas.SetTop(_viewWindow, (_canvas.Height * _windowY) - _viewWindow.Height/2);

            _canvas.Children.Add(_viewWindow);
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
            UpdatePos();
        }

        private void UpdatePos()
        {
            PosX.Text = $"{_windowX * _shapeManager.Width}";
            PosY.Text = $"{_windowY * _shapeManager.Height}";
        }

        private void SetNewArea()
        {
            if (!_rendering)
            {
                _rendering = true;

                var xmin = _shapeManager.Xmin + (_windowX * _shapeManager.Width) - (_meters / 2);
                var xmax = xmin + _meters;
                var ymin = _shapeManager.Ymin + (_windowY * _shapeManager.Height) - (_meters / 2);
                var ymax = ymin + _meters;

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

                ImageDump.Stretch = Stretch.Uniform;
                ImageDump.StretchDirection = StretchDirection.Both;

                //var shapeTest = ShapeShifter.ShapeShifter.MergeAllShapeFiles(_folder);
                var testImage = ShapeRender.ShapeRender.RenderShapeFile(areaOnly, (int)ImageDump.Width, (int)ImageDump.Height);

                using (MemoryStream memory = new MemoryStream())
                {
                    testImage.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    ImageDump.Source = bitmapImage;
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

        private void ViewWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _drag = true;
            // save start point of dragging
            _startPoint = Mouse.GetPosition(_canvas);
        }

        private void ViewWindow_MouseMove(object sender, MouseEventArgs e)
        {
            // if dragging, then adjust rectangle position based on mouse movement
            if (_drag)
            {
                var draggedRectangle = (System.Windows.Shapes.Rectangle)sender;
                var newPoint = Mouse.GetPosition(_canvas);

                double left = Canvas.GetLeft(draggedRectangle);
                double top = Canvas.GetTop(draggedRectangle);

                if (left >= 0 && left <= _canvas.Width - draggedRectangle.Width)
                {
                    Canvas.SetLeft(draggedRectangle, left + (newPoint.X - _startPoint.X));
                    _startPoint.X = newPoint.X;
                }

                if (top >= 0 && top <= _canvas.Height - draggedRectangle.Height)
                {
                    Canvas.SetTop(draggedRectangle, top + (newPoint.Y - _startPoint.Y));
                    _startPoint.Y = newPoint.Y; 
                }

                _windowX = ((Canvas.GetLeft(draggedRectangle) + (draggedRectangle.Width / 2)) / _canvas.Width);
                _windowY = ((Canvas.GetTop(draggedRectangle) + (draggedRectangle.Height / 2)) / _canvas.Height);
                PosX.Text = _windowX.ToString();
                PosY.Text = _windowY.ToString();
                SetNewArea();

                //_startPoint = newPoint;
            }
        }
        
        private void ViewWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // stop dragging
            var draggedRectangle = (System.Windows.Shapes.Rectangle)sender;
            SetNewArea();
            _drag = false;
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _meters = e.NewValue * 1000 < 1 ? 1 : e.NewValue * 1000;
            ZoomVal.Text = string.Format("{0:0.00}", _meters);
            SetViewWindow();
            SetNewArea();
        }

        private void Button_GetArea(object sender, RoutedEventArgs e)
        {

        }
    }
}
