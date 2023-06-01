using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeShifter.Storage
{
    public class ShapeSummary
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public int ItemCount { get; set; } = 0;
        
        public string Display 
        { 
            get
            {
                return $"{Path.GetFileNameWithoutExtension(FileName)} ({ItemCount})";
            }
        }

        public bool IsSelected { get; set; } = true;

        public override string ToString()
        {
            return FilePath;
        }
    }
}
