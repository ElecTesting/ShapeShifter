﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapeObject
    {
        public int RecordId { get; set; } = -1;
        public BoundingBox Box { get; set; } = new BoundingBox();
        public string TextString { get; set; } = "";
        public double TextAngle { get; set; } = 0;
        public Color Color { get; set; } = Color.Black;
        public string Anchor { get; set; } = "";
    }
}
