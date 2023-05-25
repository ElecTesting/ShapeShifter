using ShapeShifter.Storage;
using System.Drawing;
using static System.Formats.Asn1.AsnWriter;
#pragma warning disable CA1416 // Validate platform compatibility

namespace ShapeRender
{
    public class ShapeRender
    {
        public static Bitmap RenderShapeFile(ShapeFile shapeFile, double scale)
        {
            var width = (shapeFile.BoundingBox.Xmax - shapeFile.BoundingBox.Xmin) * scale;
            var height = (shapeFile.BoundingBox.Ymax - shapeFile.BoundingBox.Ymin) * scale;
            
            var image = new Bitmap((int)width, (int)height);
            using (var graphics = Graphics.FromImage(image))
            {
                // fill it with white
                graphics.FillRegion(Brushes.White, new Region(new Rectangle(0, 0, (int)width, (int)height)));

                foreach (var item in shapeFile.PolyLines)
                {
                    foreach (var polyLine in item.PolyLines)
                    {
                        for (var p = 0; p < polyLine.Points.Count-1; p++)
                        {
                            DrawLine(graphics, polyLine.Points[p], polyLine.Points[p + 1], shapeFile.BoundingBox, scale);
                        }
                    }
                }

                // do some lines
                foreach (var item in shapeFile.Polygons)
                {
                    foreach (var polyLine in item.Polygons)
                    {
                        for (var p = 0; p < polyLine.Points.Count - 1; p++)
                        {
                            DrawLine(graphics, polyLine.Points[p], polyLine.Points[p + 1], shapeFile.BoundingBox, scale);
                        }
                    }
                }

                // do some points
                foreach (var item in shapeFile.Points)
                {
                    DrawPoint(graphics, item.X, item.Y, shapeFile.BoundingBox, scale);
                }
            }

            return image;
        }

        public static Bitmap RenderShapeFile(ShapeFile shapeFile, int width, int height)
        {
            //var width = (shapeFile.BoundingBox.Xmax - shapeFile.BoundingBox.Xmin) * scale;
            //var height = (shapeFile.BoundingBox.Ymax - shapeFile.BoundingBox.Ymin) * scale;
            var scale = width / (shapeFile.BoundingBox.Xmax - shapeFile.BoundingBox.Xmin);

            var image = new Bitmap((int)width, (int)height);
            using (var graphics = Graphics.FromImage(image))
            {
                // fill it with white
                graphics.FillRegion(Brushes.White, new Region(new Rectangle(0, 0, (int)width, (int)height)));

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

                // do some lines
                foreach (var item in shapeFile.Polygons)
                {
                    foreach (var polyLine in item.Polygons)
                    {
                        for (var p = 0; p < polyLine.Points.Count - 1; p++)
                        {
                            DrawLine(graphics, polyLine.Points[p], polyLine.Points[p + 1], shapeFile.BoundingBox, scale);
                        }
                    }
                }

                // do some points
                foreach (var item in shapeFile.Points)
                {
                    DrawPoint(graphics, item.X, item.Y, shapeFile.BoundingBox, scale);
                }
            }

            return image;
        }

        private static void DrawLine(Graphics graphics, ShapePoint point1, ShapePoint point2, BoundingBoxHeader box, double scale)
        {
            var pen = new Pen(new SolidBrush(Color.Black), 1);
            var p1 = new PointF((float)((point1.X - box.Xmin) * scale), (float)((point1.Y - box.Ymin) * scale));
            var p2 = new PointF((float)((point2.X - box.Xmin) * scale), (float)((point2.Y - box.Ymin) * scale));
            graphics.DrawLine(pen, p1, p2);
        }

        private static void DrawPoint(Graphics graphics, double x, double y, BoundingBoxHeader box, double scale)
        {
            var pen = new Pen(new SolidBrush(Color.Black), 1);
            var p1 = new PointF((float)((x - box.Xmin) * scale), (float)((y - box.Ymin) * scale));
            graphics.DrawRectangle(pen, p1.X, p1.Y, 1, 1);
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}