using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class MapFeature
    {
        public string Feature { get; set; } = "";
        public string FeatType { get; set; } = "";
        public int FeatCode { get; set; }
        public int Red { get; set; }
        public int Green { get; set; }  
        public int Blue { get; set; }   

        public Color RGBColor
        {
            get 
            {
                return Color.FromArgb(Red, Green, Blue);
            }
        }
    }
}
