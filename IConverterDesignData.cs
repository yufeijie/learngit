using System.Collections.Generic;

namespace PV_analysis
{
    internal interface IConverterDesignData
    {
        double Efficiency { get; }
        double Volume { get; }
        double Cost { get; }
        string[] Configs { get; }
    }
}
