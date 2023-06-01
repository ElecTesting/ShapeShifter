using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeViewer
{
    public class FileItem
    {
        public string FilePath { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public override string ToString()
        {
            if (File.Exists(FilePath))
            {
                return Path.GetFileName(FilePath);
            }
            else
            {
                return "File not found";
            }
        }
    }
}
