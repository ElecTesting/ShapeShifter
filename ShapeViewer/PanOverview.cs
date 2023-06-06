using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ShapeViewer
{
    public class ZoomBorderOverview : Border
    {
        //public double AreaWidth { get; set; }
        //public double AreaHeight { get; set; }
        //public double AreaScale { get; set; }
        //public double BoxWidth { get; set; }
        //public double BoxHeight { get; set; }
        //public double BoxOriginX { get; set; }
        //public double BoxOriginY { get; set; }
        //public double BoxX { get; set; }
        //public double BoxY { get; set; }

        public double OffsetY { get; set; } = 0;

        private UIElement _child = null;
        private Point _origin;
        private Point _start;

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public event PanEventHandler Refresh;
        public event PanEventHandler Move;
        public event PanEventHandler Zoom;

        public delegate void PanEventHandler(object? sender, PanZoomRefreshEventArgs e);

        protected virtual void OnRefresh(PanZoomRefreshEventArgs e)
        {
            Refresh?.Invoke(this, e);
        }

        protected virtual void OnMove(PanZoomRefreshEventArgs e)
        {
            Move?.Invoke(this, e);
        }

        protected virtual void OnZoom(PanZoomRefreshEventArgs e)
        {
            Zoom?.Invoke(this, e);
        }
        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this._child = element;
            if (_child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                _child.RenderTransform = group;
                _child.RenderTransformOrigin = new Point(0.0, 0.0);
                //this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
                //this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(child_PreviewMouseRightButtonDown);
            }
        }

        public void Reset()
        {
            if (_child != null)
            {
                // reset zoom
                var st = GetScaleTransform(_child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(_child);
                tt.X = 0.0;
                tt.Y = OffsetY;
            }
        }

        #region Child Events

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                if (_child != null)
                {
                    var tt = GetTranslateTransform(_child);
                    _start = e.GetPosition(this);
                    _origin = new Point(0, tt.Y);
                    this.Cursor = Cursors.Hand;
                    _child.CaptureMouse();
                }
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_child != null)
            {
                _child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        //void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    this.Reset();
        //}

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (_child != null)
            {
                if (_child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(_child);
                    Vector v = (_start - e.GetPosition(this));

                    tt.X = 0;
                    var yresult = _origin.Y - v.Y;
                    if (yresult > 0)
                    {
                        yresult = 0;
                    }

                    var thisHeight = Height;
                    var parentWindow = (Grid)Parent;
                    var distance = thisHeight - parentWindow.ActualHeight;

                    if (yresult < -distance)
                    {
                        yresult = -distance;
                    }

                    tt.Y = yresult;
                    OffsetY = yresult;
                }
            }
        }

        #endregion
    }
}
