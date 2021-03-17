using System.Collections.Generic;

namespace PV_analysis
{
    /// <summary>
    /// 接口-器件设计方案数据
    /// </summary>
    internal interface IComponentDesignData
    {
        double PowerLoss { get; }
        double Volume { get; }
        double Cost { get; }
        string[] Configs { get; }
    }
}
