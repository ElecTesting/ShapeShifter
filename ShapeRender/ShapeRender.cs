using ShapeShifter.Storage;
using System.Drawing;
using static System.Formats.Asn1.AsnWriter;
#pragma warning disable CA1416 // Validate platform compatibility

namespace ShapeRender
{
    public class ShapeRender
    {
        public static Bitmap RenderShapeFile(ShapeFile shapeFile, int width, int height, bool renderText = true)
        {
            var scale = width / (shapeFile.BoundingBox.Xmax - shapeFile.BoundingBox.Xmin);

            var image = new Bitmap((int)width, (int)height);
            using (var graphics = Graphics.FromImage(image))
            {
                // fill it with white
                graphics.FillRegion(Brushes.White, new Region(new Rectangle(0, 0, (int)width, (int)height)));

                // do the polys
                foreach (var item in shapeFile.Polygons)
                {
                    foreach (var poly in item.Polygons)
                    {
                        if (item.Color == Color.Black)
                        {
                            item.Color = Color.White;
                        }
                        DrawPoly(graphics, poly, shapeFile.BoundingBox, scale, item.Color);
                    }
                }

                // do the lines
                foreach (var item in shapeFile.PolyLines)
                {
                    if (string.IsNullOrEmpty(item.TextString))
                    {
                        foreach (var polyLine in item.PolyLines)
                        {
                            for (var p = 0; p < polyLine.Points.Count - 1; p++)
                            {
                                DrawLine(graphics, polyLine.Points[p], polyLine.Points[p + 1], shapeFile.BoundingBox, scale, item.Color);
                            }
                        }
                    }
                    else
                    {
                        if (renderText)
                        {
                            foreach (var polyLine in item.PolyLines)
                            {
                                DrawText(graphics, polyLine.Points[0].X, polyLine.Points[0].Y, shapeFile.BoundingBox, scale, item.TextString, item.Anchor, item.TextAngle);
                            }
                        }
                    }
                }


                // points, which are text
                if (renderText)
                {
                    foreach (var item in shapeFile.Points)
                    {
                        if (!string.IsNullOrWhiteSpace(item.TextString))
                        {
                            DrawTextPoint(graphics, item, shapeFile.BoundingBox, scale, item.Anchor, item.TextAngle);
                        }
                    }
                }

                // do the poly overlays
                foreach (var item in shapeFile.PolygonOverlays)
                {
                    foreach (var poly in item.Polygons)
                    {
                        if (item.Color == Color.Black)
                        {
                            item.Color = Color.White;
                        }
                        DrawPolyOverlay(graphics, poly, shapeFile.BoundingBox, scale, item.Color);
                    }
                }
            }
            
            return image;
        }

        private static void DrawPoly(Graphics graphics, BasePoloygon poly, BoundingBoxHeader box, double scale, Color color)
        {
            var pen = new Pen(new SolidBrush(Color.Black), 1);
            var brush = new SolidBrush(color);

            var newPoints = new List<PointF>();

            foreach (var point in poly.Points)
            {
                newPoints.Add(new PointF((float)((point.X - box.Xmin) * scale), (float)((box.Ymax - point.Y) * scale)));
            }
            graphics.FillPolygon(brush, newPoints.ToArray());
            graphics.DrawPolygon(pen, newPoints.ToArray());
        }

        private static void DrawPolyOverlay(Graphics graphics, BasePoloygon poly, BoundingBoxHeader box, double scale, Color color)
        {
            var newColor = Color.FromArgb(80, color);
            var brush = new SolidBrush(newColor);

            var newPoints = new List<PointF>();

            foreach (var point in poly.Points)
            {
                newPoints.Add(new PointF((float)((point.X - box.Xmin) * scale), (float)((box.Ymax - point.Y) * scale)));
            }
            graphics.FillPolygon(brush, newPoints.ToArray());
        }


        private static void DrawLine(Graphics graphics, ShapePoint point1, ShapePoint point2, BoundingBoxHeader box, double scale, Color color)
        {
            var pen = new Pen(new SolidBrush(color), 1);
            var p1 = new PointF((float)((point1.X - box.Xmin) * scale), (float)((box.Ymax - point1.Y) * scale));
            var p2 = new PointF((float)((point2.X - box.Xmin) * scale), (float)((box.Ymax - point2.Y) * scale));
            graphics.DrawLine(pen, p1, p2);
        }

        private static void DrawText(Graphics graphics, double x, double y, BoundingBoxHeader box, double scale, string text, string anchor, double angle)
        {
            var p1 = new PointF((float)((x - box.Xmin) * scale), (float)((box.Ymax - y) * scale));
            float width = 0;
            float height = 0;

            if (!string.IsNullOrEmpty(anchor))
            {
                var textSize = graphics.MeasureString(text, new Font("Courier New", 10));
                width = textSize.Width;
                height = textSize.Height;
                //var textSize = graphics.MeasureString(text, new Font("Courier New", 10), new PointF(p1.X, p1.Y), StringFormat.GenericTypographic, out width, out height);
            }
            
            // anchor positioning
            // gdi draws from a NW anchor point
            // so we adjust from there
            switch (anchor)
            {
                case "SW":
                    p1.Y -= height;
                    break;
                case "S":
                    p1.Y -= height;
                    p1.X -= width / 2;
                    break;
                case "N":
                    p1.X -= width / 2;
                    break;
                case "NE":
                    p1.X -= width;
                    break;
                case "":
                    break;
                default:
                    break;
            }

            //graphics.TranslateTransform(p1.X, p1.Y);   
            //graphics.RotateTransform((float)angle);
            graphics.DrawString(text, new Font("Courier New", 10), Brushes.Black, p1.X, p1.Y);
            //graphics.RotateTransform(0);
            //graphics.TranslateTransform(0, 0);
        }

        private static void DrawTextPoint(Graphics graphics, ShapePoint point, BoundingBoxHeader box, double scale, string anchor, double angle)
        {
            DrawText(graphics, point.X, point.Y, box, scale, point.TextString, anchor, angle);
            //var p1 = new PointF((float)((point.X - box.Xmin) * scale), (float)((box.Ymax - point.Y) * scale));
            //graphics.DrawString(point.TextString, new Font("Courier New", 10), Brushes.Black, p1.X, p1.Y);
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}