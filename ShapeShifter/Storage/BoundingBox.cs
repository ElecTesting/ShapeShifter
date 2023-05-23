using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class BoundingBox
    {
        public double Xmin { get; set; }
        public double Ymin { get; set; }
        public double Xmax { get; set; }
        public double Ymax { get; set; }
        public double Zmin { get; set; }
        public double Zmax { get; set; }
        public double Mmin { get; set; }
        public double Mmax { get; set; }
    }
}
