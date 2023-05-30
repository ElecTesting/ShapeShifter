using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBaseReader
{
    public class DBaseField
    {
        public string Name { get; set; } = "";
        public FieldType Type { get; set; }
        public int Address { get; set; }    
        public int Length { get; set; }
        public byte DecimalCount { get; set; }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
