using PV_analysis.Components;
using PV_analysis.Converters;
using PV_analysis.Informations;
using PV_analysis.Topologys;
using System;
using System.Collections.Generic;

namespace PV_analysis.Structures
{
    internal abstract class Structure : Equipment
    {
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
        /// 判断评估对象是否为架构
        /// </summary>
        /// <returns>判断结果</returns>
        public override bool IsStructure() { return true; }

        /// <summary>
        /// 获取总损耗分布（变换器）
        /// </summary>
        /// <returns>总损耗分布信息</returns>
        public override List<Info> GetTotalLossBreakdown()
        {
            List<Info> list = new List<Info>();
            foreach (Converter converter in Converters)
            {
                list.Add(new Info(converter.GetTypeName(), Math.Round(converter.PowerLoss, 2)));
            }
            return list;
            
        }

        /// <summary>
        /// 获取损耗分布（变换器）
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public List<Info> GetLossBreakdown()
        {
            return GetTotalLossBreakdown();
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
                list.Add(new Info(converter.GetTypeName(), Math.Round(converter.Cost / 1e4, 2)));
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
                list.Add(new Info(converter.GetTypeName(), Math.Round(converter.Volume, 2)));
            }
            return list;
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        public override void Save()
        {
            Save(GetType().Name);
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="name">文件名</param>
        public override void Save(string name)
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
        public override void Save(string path, string name)
        {
            string[] conditionTitles = GetConditionTitles();
            string[] conditions = GetConditions();
            Data.Save(path, name + "_Pareto", conditionTitles, conditions, ParetoDesignList);
            Data.Save(path, name + "_all", conditionTitles, conditions, AllDesignList);
        }

        /// <summary>
        /// 评估，得到中国效率、体积、成本
        /// </summary>
        public override void Evaluate()
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
        public override void Operate(double load, double Vin)
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
