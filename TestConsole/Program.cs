// See https://aka.ms/new-console-template for more information


//D:\EsriData\Bradford\02_Renamed   -- the big one
//D:\\EsriData\\Mundesley           -- the small one

using ShapeShifter;
using ShapeShifter.Storage;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;

//var dbasefile = @"D:\EsriData\Bradford\02_Renamed\AR.dbf";
//var dbasefile = @"D:\EsriData\Mundesley\MUNDSLEY_Buildings Or Structure Text_text.dbf";


// dbase reader testing
//using (var dbaseReader = new DBaseReader.DBaseReader(dbasefile))
//{
//    while (dbaseReader.HasRows)
//    {
//        var test = dbaseReader.GetInt32("FEATCODE");
//        //Console.WriteLine($"{test}");
//        //Console.WriteLine($"{dbaseReader.RowNumber}");
//        dbaseReader.NextResult();
//    }
//}


var shapeFolder = @"D:\Mapping\Oxfordshire\MiltonRAB";
// shape shifter testing
var shapeManager = new ShapeManager(shapeFolder);

var mapName = Path.GetFileName(shapeFolder);

var area = new BoundingBox()
{
   Xmin = shapeManager.Xmin,
   Xmax = shapeManager.Xmax,
   Ymin = shapeManager.Ymin,
   Ymax = shapeManager.Ymax
};
var itemCount = shapeManager.SetArea(area);
var shapeFile = shapeManager.GetArea();

//var getCount = shapeManager.SetArea(area);
//Console.WriteLine($"Count: {getCount}");

//var shapeFile = ShapeShifter.ShapeShifter.MergeAllShapeFiles(@"D:\EsriData\Bradford\02_Renamed");
var image = ShapeRender.ShapeRender.RenderShapeFile(shapeFile, (int)shapeManager.Width/4, (int)shapeManager.Height/4, false);

#pragma warning disable CA1416 // Validate platform compatibility
var mapFile = Path.Combine(@"D:\temp", $"{mapName}.png");

image.Save(mapFile, ImageFormat.Png);
#pragma warning restore CA1416 // Validate platform compatibility

