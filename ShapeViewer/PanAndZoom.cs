using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace ShapeViewer
{
    public class PanZoomRefreshEventArgs : EventArgs
    {
        public Vector Pan { get; set; }

        public PanZoomRefreshEventArgs()
        {
        }
    }

    public class ZoomBorder : Border
    {
        public double AreaWidth { get; set; }  
        public double AreaHeight { get; set; }
        public double AreaScale { get; set; }
        public double BoxWidth { get; set; }   
        public double BoxHeight { get; set; }
        public double BoxOriginX { get; set; }   
        public double BoxOriginY { get; set; }
        public double BoxX { get; set; }
        public double BoxY { get; set; }


        private UIElement _child = new UIElement();
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
                this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                  child_PreviewMouseRightButtonDown);
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
                tt.Y = 0.0;
            }
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_child != null)
            {
                var st = GetScaleTransform(_child);
                var tt = GetTranslateTransform(_child);

                double zoom = e.Delta > 0 ? .2 : -.2;
                
                //if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                //    return;
                if (!(e.Delta > 0) && (AreaScale > 0 || AreaScale < 1))
                    return;

                Point relative = e.GetPosition(_child);
                double absoluteX;
                double absoluteY;

                absoluteX = relative.X * st.ScaleX + tt.X;
                absoluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                tt.X = absoluteX - relative.X * st.ScaleX;
                tt.Y = absoluteY - relative.Y * st.ScaleY;

                var newPos = new Point(tt.X, tt.Y);

                AreaScale += zoom;
                var pan = new PanZoomRefreshEventArgs()
                {
                    Pan = VectorClamp(newPos - e.GetPosition(this))
                };
                OnZoom(pan);
            }
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_child != null)
            {
                var tt = GetTranslateTransform(_child);
                _start = e.GetPosition(this);
                _origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                _child.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_child != null)
            {
                _child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
                var pan = new PanZoomRefreshEventArgs()
                {
                    Pan = VectorClamp(_start - e.GetPosition(this))
                };
                OnRefresh(pan);
            }
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Reset();
        }

        private Vector VectorClamp(Vector v)
        {
            var vScale = new Vector(v.X * AreaScale, v.Y * AreaScale);

            var left = BoxOriginX - (BoxWidth / 2) + vScale.X;
            var right = BoxOriginX + (BoxWidth / 2) + vScale.X;
            var top = BoxOriginY + (BoxHeight / 2) - vScale.Y;
            var bottom = BoxOriginY - (BoxHeight / 2) - vScale.Y;

            if (left < 0)
            {
                v.X -= (left / AreaScale);
            }

            if (right > AreaWidth)
            {
                v.X -= (right - AreaWidth) / AreaScale;
            }
            
            if (bottom < 0) 
            { 
                v.Y += (bottom / AreaScale); 
            }
            
            if (top > AreaHeight)
            {
                v.Y += (top - AreaHeight) / AreaScale; 
            }
            
            BoxX = BoxOriginX + v.X;
            BoxY = BoxOriginY - v.Y;

            return v;
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (_child != null)
            {
                if (_child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(_child);
                    Vector v = VectorClamp(_start - e.GetPosition(this));

                    tt.X = _origin.X - v.X;
                    tt.Y = _origin.Y - v.Y;

                    var pan = new PanZoomRefreshEventArgs()
                    {
                        Pan = v
                    };
                    OnMove(pan);
                }
            }
        }

        #endregion
    }
}
