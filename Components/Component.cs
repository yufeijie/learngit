using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Components
{
    internal abstract class Component
    {
        public double PowerLoss { get; protected set; }
        public double Cost { get; protected set; }
        public double Volume { get; protected set; }
    }
}
