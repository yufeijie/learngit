using PV_analysis.Converters;
using PV_analysis.Informations;
using System;
using System.Collections.Generic;

namespace PV_analysis.Structures
{
    /// <summary>
    /// 两级架构
    /// </summary>
    internal class TwoLevelStructure : Structure
    {
        /// <summary>
        /// 获取类型名
        /// </summary>
        /// <returns>类型名</returns>
        public override string GetTypeName()
        {
            return "两级架构";
        }

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>设计条件标题</returns>
        public override string[] GetConditionTitles()
        {
            string[] conditionTitles =
            {
                "评估对象",
                "总功率",
                "光伏MPPT电压最小值",
                "光伏MPPT电压最大值",
                "并网电压",
                "并网频率(Hz)",
                "DCAC最小电压调制比",
                "DCAC最大电压调制比",
                "DCAC功率因数角(rad)",
                "逆变直流侧电压范围",
                "隔离DCDC副边个数范围",
                "隔离DCDC模块数范围",
                "隔离DCDC拓扑范围",
                "隔离DCDC开关频率范围(kHz)",
                "隔离DCDC品质因数范围",
                "隔离DCDC电感比范围",
                "DCAC拓扑范围",
                "DCAC调制方式范围",
                "DCAC开关频率范围(kHz)"
            };
            return conditionTitles;
        }

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>设计条件</returns>
        public override string[] GetConditions()
        {
            string[] conditions =
            {
                GetType().Name,
                Math_Psys.ToString(),
                Math_Vpv_min.ToString(),
                Math_Vpv_max.ToString(),
                Math_Vg.ToString(),
                Math_fg.ToString(),
                DCAC_Ma_min.ToString(),
                DCAC_Ma_max.ToString(),
                DCAC_φ.ToString(),
                Function.DoubleArrayToString(Math_VinvRange),
                Function.IntArrayToString(IsolatedDCDC_secondaryRange),
                Function.IntArrayToString(IsolatedDCDC_numberRange),
                Function.StringArrayToString(IsolatedDCDC_topologyRange),
                Function.DoubleArrayToString(IsolatedDCDC_frequencyRange, 1e-3),
                Function.DoubleArrayToString(IsolatedDCDC_Math_Q_Range),
                Function.DoubleArrayToString(IsolatedDCDC_Math_k_Range),
                Function.StringArrayToString(DCAC_topologyRange),
                Function.StringArrayToString(DCAC_modulationRange),
                Function.DoubleArrayToString(DCAC_frequencyRange, 1e-3)
            };
            return conditions;
        }

        /// <summary>
        /// 获取设计参数信息
        /// </summary>
        /// <returns>设计参数信息</returns>
        public override List<Info> GetConfigInfo()
        {
            List<Info> list = new List<Info>
            {
                new Info("架构", Name),
                new Info("逆变直流侧电压", DCAC_Vinv.ToString() + "V")
            };
            return list;
        }
        
        /// <summary>
        /// 获取手动设计信息
        /// </summary>
        /// <returns>手动设计信息</returns>
        public override List<(MainForm.ControlType, string)> GetManualInfo()
        {
            List<(MainForm.ControlType, string)> list = new List<(MainForm.ControlType, string)>()
            {
                (MainForm.ControlType.Text, "系统总功率"),
                (MainForm.ControlType.Text, "光伏MPPT最小电压"),
                (MainForm.ControlType.Text, "光伏MPPT最大电压"),
                (MainForm.ControlType.Text, "并网电压"),
                (MainForm.ControlType.Text, "逆变直流侧电压"),
            };
            return list;
        }

        /// <summary>
        /// 复制当前架构，保留设计条件
        /// </summary>
        /// <returns>复制结果</returns>
        public override Equipment Clone()
        {
            return new TwoLevelStructure()
            {
                Name = Name,
                Math_Psys = Math_Psys,
                Math_Vpv_min = Math_Vpv_min,
                Math_Vpv_max = Math_Vpv_max,
                Math_Vg = Math_Vg,
                Math_Vo = Math_Vo,
                Math_fg = Math_fg,
                DCAC_Ma_min = DCAC_Ma_min,
                DCAC_Ma_max = DCAC_Ma_max,
                DCAC_φ = DCAC_φ
            };
        }

        /// <summary>
        /// 根据给定的条件进行优化设计
        /// </summary>
        public override void Optimize(MainForm form, double progressMin, double progressMax)
        {
            double progress = progressMin;
            double dp = (progressMax - progressMin) / Math_VinvRange.Length / IsolatedDCDC_secondaryRange.Length / IsolatedDCDC_numberRange.Length;
            foreach (double Vinv in Math_VinvRange) //逆变直流侧电压变化
            {
                form.PrintDetails(3, "Now Inv DC voltage = " + Vinv + ":");
                form.PrintDetails(3, "-------------------------");
                foreach (int n in IsolatedDCDC_numberRange)
                {
                    //逆变器设计
                    form.PrintDetails(3, "-------------------------");
                    form.PrintDetails(3, "Inverters design...");
                    DCAC = new DCACConverter()
                    {
                        PhaseNum = 3,
                        Math_Psys = Math_Psys,
                        Math_Vin = Vinv,
                        Math_Vg = Math_Vg,
                        Math_Vo = Math_Vg / Math.Sqrt(3),
                        Math_fg = Math_fg,
                        Math_Ma_min = DCAC_Ma_min,
                        Math_Ma_max = DCAC_Ma_max,
                        Math_φ = DCAC_φ,
                        NumberRange = new int[] { n },
                        TopologyRange = DCAC_topologyRange,
                        ModulationRange = DCAC_modulationRange,
                        FrequencyRange = DCAC_frequencyRange,
                    };
                    DCAC.Optimize(form, progress, progress + dp * 0.3);
                    progress += dp * 0.3;
                    if (DCAC.AllDesignList.Size <= 0)
                    {
                        progress += dp * 0.7;
                        continue;
                    }

                    //隔离DC/DC变换器设计
                    form.PrintDetails(3, "-------------------------");
                    form.PrintDetails(3, "Isolated DC/DC converters design...");
                    IsolatedDCDC = new IsolatedDCDCConverter()
                    {
                        PhaseNum = 3,
                        Math_Psys = Math_Psys,
                        Math_Vin_min = Math_Vpv_min,
                        Math_Vin_max = Math_Vpv_max,
                        IsInputVoltageVariation = true,
                        Math_Vo = Vinv,
                        Math_No_Range = IsolatedDCDC_secondaryRange,
                        NumberRange = new int[] { n },
                        TopologyRange = IsolatedDCDC_topologyRange,
                        FrequencyRange = IsolatedDCDC_frequencyRange,
                        Math_Q_Range = IsolatedDCDC_Math_Q_Range,
                        Math_k_Range = IsolatedDCDC_Math_k_Range,
                    };
                    IsolatedDCDC.Optimize(form, progress, progress + dp * 0.3);
                    progress += dp * 0.3;
                    if (IsolatedDCDC.AllDesignList.Size <= 0)
                    {
                        progress += dp * 0.4;
                        continue;
                    }

                    //整合得到最终结果
                    form.PrintDetails(3, "-------------------------");
                    form.PrintDetails(3, "Inv num=" + n + ", Combining...");
                    ConverterDesignList newDesignList = new ConverterDesignList();
                    newDesignList.Combine(IsolatedDCDC.ParetoDesignList);
                    newDesignList.Combine(DCAC.ParetoDesignList);
                    newDesignList.Transfer(new string[] { DCAC.Math_Vin.ToString() });
                    ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                    AllDesignList.Merge(newDesignList); //记录所有设计
                    progress += dp * 0.4;
                    form.Estimate_Result_ProgressBar_Set(progress);
                }
            }
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public override void Load(string[] configs, ref int index)
        {
            EfficiencyEval = double.Parse(configs[index++]);
            Volume = double.Parse(configs[index++]);
            Cost = double.Parse(configs[index++]);
            DCAC_Vinv = double.Parse(configs[index++]);
            IsolatedDCDC = new IsolatedDCDCConverter()
            {
                PhaseNum = 3,
                Math_Psys = Math_Psys,
                Math_Vin_min = Math_Vpv_min,
                Math_Vin_max = Math_Vpv_max,
                IsInputVoltageVariation = true,
                Math_Vo = DCAC_Vinv
            };
            IsolatedDCDC.Load(configs, ref index);
            DCAC = new DCACConverter()
            {
                PhaseNum = 3,
                Math_Psys = Math_Psys,
                Math_Vin = DCAC_Vinv,
                Math_Vg = Math_Vg,
                Math_Vo = Math_Vg / Math.Sqrt(3),
                Math_fg = Math_fg,
                Math_Ma_min = DCAC_Ma_min,
                Math_Ma_max = DCAC_Ma_max,
                Math_φ = DCAC_φ
            };
            DCAC.Load(configs, ref index);
            Converters = new Converter[] { IsolatedDCDC, DCAC };
        }
    }
}
