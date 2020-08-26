using PV_analysis.Components;
using PV_analysis.Converters;
using PV_analysis.Topologys;
using System.Collections.Generic;

namespace PV_analysis.Structures
{
    internal abstract class Structure
    {
        /// <summary>
        /// 包含的变换器
        /// </summary>
        public Converter[] Converters { get; protected set; }

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
        /// 母线电压范围
        /// </summary>
        public double[] Math_VbusRange { get; set; }

        //---隔离DC/DC参数---
        /// <summary>
        /// 隔离DCDC品质因数预设值
        /// </summary>
        public double IsolatedDCDC_Q { get; set; }

        /// <summary>
        /// DCAC直流侧电压预设值
        /// </summary>
        public double DCAC_Vin_def { get; set; }

        /// <summary>
        /// DCAC功率因数角(rad)
        /// </summary>
        public double Math_phi { get; set; }

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
        /// DCAC可用模块数序列，隔离DCDC与此同
        /// </summary>
        public int[] DCAC_numberRange { get; set; }

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
        /// 中国效率
        /// </summary>
        public double EfficiencyCGC { get; protected set; }

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
        /// 获取拓扑名
        /// </summary>
        /// <returns>拓扑名</returns>
        public abstract string GetName();

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>配置信息</returns>
        public abstract string[] GetConditionTitles();

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>配置信息</returns>
        public abstract string[] GetConditions();

        /// <summary>
        /// 获取损耗分布（变换器）
        /// </summary>
        public List<Item> GetLossBreakdown()
        {
            List<Item> lossList = new List<Item>();
            foreach (Converter converter in Converters)
            {
                lossList.Add(new Item(converter.GetType().Name, converter.PowerLoss));
            }
            return lossList;
        }

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public abstract void Optimize();

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
