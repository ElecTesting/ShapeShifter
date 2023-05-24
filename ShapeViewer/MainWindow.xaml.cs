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

        public MainWindow()
        {
            InitializeComponent();

            TextAreaXmin.Text = "400000";
            TextAreaXmax.Text = "410000";
            TextAreaYmin.Text = "430000";
            TextAreaYmax.Text = "440000";
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
            _windowX = _shapeManager.Width / 2;
            _windowY = _shapeManager.Height / 2;
            UpdatePos();
        }

        private void UpdatePos()
        {
            PosX.Text = _windowX.ToString();
            PosY.Text = _windowY.ToString();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var xmin = _shapeManager.Xmin + (_shapeManager.Width / 2) - (_meters/2);
            var xmax = xmin + _meters;
            var ymin = _shapeManager.Ymin + (_shapeManager.Height / 2) - (_meters / 2);
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
            areaOnly.BoundingBox = new BoundingBoxHeader()
            {
                Xmin = _shapeManager.Xmin,
                Xmax = _shapeManager.Xmax,
                Ymin = _shapeManager.Ymin,
                Ymax = _shapeManager.Ymax
            };
            
            ImageDump.Stretch = Stretch.Uniform;
            ImageDump.StretchDirection = StretchDirection.Both;

            //var shapeTest = ShapeShifter.ShapeShifter.MergeAllShapeFiles(_folder);
            var testImage = ShapeRender.ShapeRender.RenderShapeFile(areaOnly, 0.1);

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

            
        }

        private bool isDragging = false;
        private System.Windows.Point offset;
        private double _windowX;
        private double _windowY;

        private void WinDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            offset = e.GetPosition(draggableRectangle);
            draggableRectangle.CaptureMouse();
        }

        private void WinMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                System.Windows.Point currentPos = e.GetPosition(this);
                _windowX += currentPos.X - offset.X;
                _windowY += currentPos.Y - offset.Y;
                _windowX = _windowX < 0 ? 0 : _windowX;
                _windowX = _windowX > _shapeManager.Width ? _shapeManager.Width : _windowX;
                _windowY = _windowY < 0 ? 0 : _windowY;
                _windowY = _windowY > _shapeManager.Height ? _shapeManager.Height : _windowY;
                UpdatePos();
            }
        }

        private void WinUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            draggableRectangle.ReleaseMouseCapture();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _meters = e.NewValue * 1000;
            ZoomVal.Text = string.Format("{0:0.00}", _meters);   
        }
    }
}
