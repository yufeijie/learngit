using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis
{
    internal class Item
    {
        public string Name { get; set; }
        public double Value { get; set; }

        public Item(string name, double value)
        {
            Name = name;
            Value = value;
        }
    }
}
