using FolderBrowserEx;
using Microsoft.Win32;
using ShapeShifter;
using ShapeShifter.Storage;
using System;
using System.Collections.Generic;
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
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var box = new BoundingBox()
            {
                Xmin = Convert.ToDouble(TextAreaXmin.Text),
                Xmax = Convert.ToDouble(TextAreaXmax.Text),
                Ymin = Convert.ToDouble(TextAreaYmin.Text),
                Ymax = Convert.ToDouble(TextAreaYmax.Text)
            };

            var temp = _shapeManager.SetArea(box);

            ItemsArea.Text = temp.ToString();
        }
    }
}
