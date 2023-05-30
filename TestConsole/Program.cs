﻿// See https://aka.ms/new-console-template for more information


//D:\EsriData\Bradford\02_Renamed   -- the big one
//D:\\EsriData\\Mundesley           -- the small one

using ShapeShifter;
using ShapeShifter.Storage;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;

var dbasefile = @"D:\EsriData\Bradford\02_Renamed\AR.dbf";
//var dbasefile = @"D:\EsriData\Mundesley\MUNDSLEY_Buildings Or Structure Text_text.dbf";


// dbase reader testing
using (var dbaseReader = new DBaseReader.DBaseReader(dbasefile))
{
    while (dbaseReader.HasRows)
    {
        var test = dbaseReader.GetInt32("FEATCODE");
        //Console.WriteLine($"{test}");
        //Console.WriteLine($"{dbaseReader.RowNumber}");
        dbaseReader.NextResult();
    }
}



// shape shifter testing
//var shapeManager = new ShapeManager(@"D:\EsriData\Bradford\02_Renamed");
//
//var area = new BoundingBox()
//{
//   Xmin = 400000,
//   Xmax = 410000,
//   Ymin = 430000,
//   Ymax = 440000
//};

//var getCount = shapeManager.SetArea(area);
//Console.WriteLine($"Count: {getCount}");

//var shapeFile = ShapeShifter.ShapeShifter.MergeAllShapeFiles(@"D:\EsriData\Bradford\02_Renamed");
//var image = ShapeRender.ShapeRender.RenderShapeFile(shapeFile, 0.25);

#pragma warning disable CA1416 // Validate platform compatibility
//image.Save(@"D:\temp\test.png", ImageFormat.Png);
#pragma warning restore CA1416 // Validate platform compatibility

