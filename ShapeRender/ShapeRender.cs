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
            //var width = (shapeFile.BoundingBox.Xmax - shapeFile.BoundingBox.Xmin) * scale;
            //var height = (shapeFile.BoundingBox.Ymax - shapeFile.BoundingBox.Ymin) * scale;
            var scale = width / (shapeFile.BoundingBox.Xmax - shapeFile.BoundingBox.Xmin);

            var image = new Bitmap((int)width, (int)height);
            using (var graphics = Graphics.FromImage(image))
            {
                // fill it with white
                graphics.FillRegion(Brushes.White, new Region(new Rectangle(0, 0, (int)width, (int)height)));

                // do the lines
                foreach (var item in shapeFile.PolyLines)
                {
                    foreach (var polyLine in item.PolyLines)
                    {
                        for (var p = 0; p < polyLine.Points.Count - 1; p++)
                        {
                            DrawLine(graphics, polyLine.Points[p], polyLine.Points[p + 1], shapeFile.BoundingBox, scale);
                        }
                    }
                }

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

                // points, which are text
                if (renderText)
                {
                    foreach (var item in shapeFile.Points)
                    {
                        if (!string.IsNullOrWhiteSpace(item.TextString))
                        {
                            DrawTextPoint(graphics, item, shapeFile.BoundingBox, scale);
                        }
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

        private static void DrawLine(Graphics graphics, ShapePoint point1, ShapePoint point2, BoundingBoxHeader box, double scale)
        {
            var pen = new Pen(new SolidBrush(Color.Black), 1);
            var p1 = new PointF((float)((point1.X - box.Xmin) * scale), (float)((box.Ymax - point1.Y) * scale));
            var p2 = new PointF((float)((point2.X - box.Xmin) * scale), (float)((box.Ymax - point2.Y) * scale));
            graphics.DrawLine(pen, p1, p2);
        }

        private static void DrawTextPoint(Graphics graphics, ShapePoint point, BoundingBoxHeader box, double scale)
        {
            var p1 = new PointF((float)((point.X - box.Xmin) * scale), (float)((box.Ymax - point.Y) * scale));
            graphics.DrawString(point.TextString, new Font("Courier New", 10), Brushes.Black, p1.X, p1.Y);
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}