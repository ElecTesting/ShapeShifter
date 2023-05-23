// See https://aka.ms/new-console-template for more information


//D:\EsriData\Bradford\02_Renamed   -- the big one
//D:\\EsriData\\Mundesley           -- the small one

using System.Drawing.Imaging;

var shapeFile = ShapeShifter.ShapeShifter.MergeAllShapeFiles(@"D:\EsriData\Bradford\02_Renamed");
var image = ShapeRender.ShapeRender.RenderShapeFile(shapeFile);

#pragma warning disable CA1416 // Validate platform compatibility
image.Save(@"D:\temp\test.png", ImageFormat.Png);
#pragma warning restore CA1416 // Validate platform compatibility

