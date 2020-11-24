using PV_analysis.Components;
using PV_analysis.Converters;
using PV_analysis.Informations;
using PV_analysis.Topologys;
using System;
using System.Collections.Generic;

namespace PV_analysis.Structures
{
    internal abstract class Structure : EvaluationObject
    {
        /// <summary>
        /// 架构名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 包含的变换器
        /// </summary>
        public Converter[] Converters { get; protected set; }

        public DCDCConverter DCDC { get; protected set; }
        public IsolatedDCDCConverter IsolatedDCDC { get; protected set; }
        public DCACConverter DCAC { get; protected set; }

        //---整体参数---
        /// <summary>
        /// 架构总功率
        /// </summary>
        public double Math_Psys { get; set; }

        /// <summary>
        /// 光伏MPPT电压最小值
        /// </summary>
        public double Math_Vpv_min { get; set; }

        /// <summary>
        /// 光伏MPPT电压最大值
        /// </summary>
        public double Math_Vpv_max { get; set; }

        /// <summary>
        /// 并网电压（线电压）
        /// </summary>
        public double Math_Vg { get; set; }

        /// <summary>
        /// 输出电压（并网相电压）
        /// </summary>
        public double Math_Vo { get; set; }

        /// <summary>
        /// 并网频率
        /// </summary>
        public double Math_fg { get; set; }

        /// <summary>
        /// 母线电压
        /// </summary>
        public double Math_Vbus { get; set; }

        /// <summary>
        /// 逆变直流侧电压
        /// </summary>
        public double DCAC_Vinv { get; set; }

        /// <summary>
        /// 母线电压范围
        /// </summary>
        public double[] Math_VbusRange { get; set; }

        /// <summary>
        /// 逆变直流侧电压范围
        /// </summary>
        public double[] Math_VinvRange { get; set; }

        //---DC/DC参数---
        /// <summary>
        /// DCDC可用模块数序列
        /// </summary>
        public int[] DCDC_numberRange { get; set; }

        /// <summary>
        /// DCDC可用拓扑序列
        /// </summary>
        public string[] DCDC_topologyRange { get; set; }

        /// <summary>
        /// DCDC可用开关频率序列
        /// </summary>
        public double[] DCDC_frequencyRange { get; set; }

        //---隔离DC/DC参数---
        /// <summary>
        /// 隔离DCDC品质因数预设值
        /// </summary>
        public double IsolatedDCDC_Q { get; set; }

        /// <summary>
        /// 隔离DCDC可用副边个数序列
        /// </summary>
        public int[] IsolatedDCDC_secondaryRange { get; set; }

        /// <summary>
        /// 隔离DCDC可用模块数序列
        /// </summary>
        public int[] IsolatedDCDC_numberRange { get; set; }

        /// <summary>
        /// 隔离DCDC可用拓扑序列
        /// </summary>
        public string[] IsolatedDCDC_topologyRange { get; set; }

        /// <summary>
        /// 隔离DCDC可用谐振频率序列
        /// </summary>
        public double[] IsolatedDCDC_resonanceFrequencyRange { get; set; }

        //---DC/AC参数---
        /// <summary>
        /// DCAC最小电压调制比
        /// </summary>
        public double DCAC_Ma_min { get; set; }

        /// <summary>
        /// DCAC最大电压调制比
        /// </summary>
        public double DCAC_Ma_max { get; set; }

        /// <summary>
        /// DCAC功率因数角(rad)
        /// </summary>
        public double DCAC_φ { get; set; }

        /// <summary>
        /// DCAC可用拓扑序列
        /// </summary>
        public string[] DCAC_topologyRange { get; set; }

        /// <summary>
        /// DCAC可用调制方式序列
        /// </summary>
        public string[] DCAC_modulationRange { get; set; }

        /// <summary>
        /// DCAC可用开关频率序列
        /// </summary>
        public double[] DCAC_frequencyRange { get; set; }

        /// <summary>
        /// 获取架构类型
        /// </summary>
        /// <returns>架构类型</returns>
        public abstract string GetCategory();

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
        //public abstract List<Info> GetManualInfo();

        /// <summary>
        /// 获取设计参数信息
        /// </summary>
        /// <returns>获取设计参数信息</returns>
        public abstract List<Info> GetConfigInfo();

        /// <summary>
        /// 获取损耗分布（变换器）
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public List<Info> GetLossBreakdown()
        {
            List<Info> list = new List<Info>();
            foreach (Converter converter in Converters)
            {
                list.Add(new Info(converter.GetCategory(), Math.Round(converter.PowerLoss, 2)));
            }
            return list;
        }

        /// <summary>
        /// 获取成本分布（变换器）
        /// </summary>
        /// <returns>成本分布信息</returns>
        public List<Info> GetCostBreakdown()
        {
            List<Info> list = new List<Info>();
            foreach (Converter converter in Converters)
            {
                list.Add(new Info(converter.GetCategory(), Math.Round(converter.Cost / 1e4, 2)));
            }
            return list;
        }

        /// <summary>
        /// 获取体积分布（变换器）
        /// </summary>
        /// <returns>体积分布信息</returns>
        public List<Info> GetVolumeBreakdown()
        {
            List<Info> list = new List<Info>();
            foreach (Converter converter in Converters)
            {
                list.Add(new Info(converter.GetCategory(), Math.Round(converter.Volume, 2)));
            }
            return list;
        }

        /// <summary>
        /// 复制当前架构，保留设计条件
        /// </summary>
        /// <returns>复制结果</returns>
        public abstract Structure Clone();

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public abstract void Optimize(MainForm form, double progressMin, double progressMax);

        /// <summary>
        /// 保存设计结果
        /// </summary>
        public void Save()
        {
            Save(GetType().Name);
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="name">文件名</param>
        public void Save(string name)
        {
            string[] conditionTitles = GetConditionTitles();
            string[] conditions = GetConditions();
            Data.Save(name + "_Pareto", conditionTitles, conditions, ParetoDesignList);
            Data.Save(name + "_all", conditionTitles, conditions, AllDesignList);
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="name">文件名</param>
        public void Save(string path, string name)
        {
            string[] conditionTitles = GetConditionTitles();
            string[] conditions = GetConditions();
            Data.Save(path, name + "_Pareto", conditionTitles, conditions, ParetoDesignList);
            Data.Save(path, name + "_all", conditionTitles, conditions, AllDesignList);
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public abstract void Load(string[] configs, ref int index);

        /// <summary>
        /// 评估，得到中国效率、体积、成本
        /// </summary>
        public void Evaluate()
        {
            EfficiencyCGC = 1;
            Cost = 0;
            Volume = 0;
            foreach (Converter converter in Converters)
            {
                converter.Evaluate();
                EfficiencyCGC += converter.EfficiencyCGC - 1;
                Cost += converter.Cost;
                Volume += converter.Volume;
            }
        }

        /// <summary>
        /// 模拟变换器运行，得到相应负载下的效率
        /// </summary>
        /// <param name="load">负载</param>
        public void Operate(double load, double Vin)
        {
            PowerLoss = 0;
            foreach (Converter converter in Converters)
            {
                converter.Operate(load, Vin);
                PowerLoss += converter.PowerLoss;
                
            }
            Efficiency = 1 - PowerLoss / (Math_Psys * load);
        }
    }
}
