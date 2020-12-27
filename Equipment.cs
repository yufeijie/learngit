using PV_analysis.Informations;
using System;
using System.Collections.Generic;

namespace PV_analysis
{
    /// <summary>
    /// 装置类，用于抽象架构和变换单元共有的方法和属性
    /// </summary>
    internal abstract class Equipment
    {
        /// <summary>
        /// 装置名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 效率评估值
        /// </summary>
        public double EfficiencyEval { get; protected set; }

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
        /// 判断装置是否为架构
        /// </summary>
        /// <returns>判断结果</returns>
        public abstract bool IsStructure();

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>设计条件标题</returns>
        public abstract string[] GetConditionTitles();

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>设计条件</returns>
        public abstract string[] GetConditions();

        ///// <summary>
        ///// 获取手动设计信息
        ///// </summary>
        ///// <returns>手动设计信息</returns>
        //public abstract List<(MainForm.ContorlType, string)> GetManualInfo();

        /// <summary>
        /// 获取设计参数信息
        /// </summary>
        /// <returns>获取设计参数信息</returns>
        public abstract List<Info> GetConfigInfo();

        /// <summary>
        /// 获取类型名
        /// </summary>
        /// <returns>类型名</returns>
        public abstract string GetTypeName();

        /// <summary>
        /// 获取性能表现信息
        /// </summary>
        /// <returns>性能表现信息</returns>
        public List<Info> GetPerformanceInfo()
        {
            List<Info> list = new List<Info>
            {
                new Info(Configuration.effciencyText, (EfficiencyEval * 100).ToString("f2") + "%"),
                new Info("成本", (Cost / 1e4).ToString("f2") + "万元"),
                new Info("体积", Volume.ToString("f2") + "dm^3")
            };
            return list;
        }

        /// <summary>
        /// 获取总损耗分布
        /// </summary>
        /// <returns>总损耗分布信息</returns>
        public abstract List<Info> GetTotalLossBreakdown();

        /// <summary>
        /// 根据给定的条件进行优化设计
        /// </summary>
        public abstract void Optimize(MainForm form, double progressMin, double progressMax);

        /// <summary>
        /// 复制当前装置，保留设计条件
        /// </summary>
        /// <returns>复制结果</returns>
        public abstract Equipment Clone();

        /// <summary>
        /// 评估，得到效率、体积、成本
        /// </summary>
        public abstract void Evaluate();

        /// <summary>
        /// 模拟装置运行，得到相应负载下的效率
        /// </summary>
        /// <param name="load">负载</param>
        public abstract void Operate(double load, double Vin);

        /// <summary>
        /// 保存设计结果
        /// </summary>
        public abstract void Save();

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="name">文件名</param>
        public abstract void Save(string name);

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="name">文件名</param>
        public abstract void Save(string path, string name);

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public abstract void Load(string[] configs, ref int index);

    }
}
