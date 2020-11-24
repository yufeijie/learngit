using PV_analysis.Informations;
using System;

namespace PV_analysis
{
    /// <summary>
    /// 评估对象类，用于抽象评估对象共有方法和属性
    /// </summary>
    internal abstract class EvaluationObject
    {
        /// <summary>
        /// 中国效率
        /// </summary>
        public double EfficiencyCGC { get; protected set; }

        /// <summary>
        /// 损耗评估值
        /// </summary>
        public double Math_Peval { get; protected set; }

        /// <summary>
        /// 损耗
        /// </summary>
        public double PowerLoss { get; protected set; }

        /// <summary>
        /// 成本
        /// </summary>
        public double Cost { get; protected set; }

        /// <summary>
        /// 体积
        /// </summary>
        public double Volume { get; protected set; }

        /// <summary>
        /// 效率
        /// </summary>
        public double Efficiency { get; protected set; }

        /// <summary>
        /// Pareto最优设计方案
        /// </summary>
        public ConverterDesignList ParetoDesignList { get; } = new ConverterDesignList();

        /// <summary>
        /// 所有设计方案
        /// </summary>
        public ConverterDesignList AllDesignList { get; } = new ConverterDesignList { IsAll = true };

        /// <summary>
        /// 获取性能表现信息
        /// </summary>
        /// <returns>性能表现信息</returns>
        public InfoList GetPerformanceInfo()
        {
            InfoList list = new InfoList("性能表现");
            list.Add(new Info("中国效率", (EfficiencyCGC * 100).ToString("f2") + "%"));
            list.Add(new Info("成本", (Cost / 1e4).ToString("f2") + "万元"));
            list.Add(new Info("体积", Volume.ToString("f2") + "dm^3"));
            return list;
        }

    }
}
