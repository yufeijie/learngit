using System.Collections.Generic;

namespace PV_analysis
{
    /// <summary>
    /// 接口-变换器设计方案数据
    /// </summary>
    internal interface IConverterDesignData
    {
        double Efficiency { get; }
        double Volume { get; }
        double Cost { get; }
        string[] Configs { get; }
    }
}
