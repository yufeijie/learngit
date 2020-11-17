using PV_analysis.Converters;
using System;

namespace PV_analysis.Structures
{
    internal class ThreeLevelStructure : Structure
    {
        public DCDCConverter DCDC { get; private set; }
        public IsolatedDCDCConverter IsolatedDCDC { get; private set; }
        public DCACConverter DCAC { get; private set; }

        /// <summary>
        /// 获取架构名
        /// </summary>
        /// <returns>架构名</returns>
        public override string GetCategory()
        {
            return "三级架构";
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
                "隔离DCDC品质因数",
                "DCAC最小电压调制比",
                "DCAC最大电压调制比",
                "DCAC功率因数角(rad)",
                "母线电压范围",
                "逆变直流侧电压范围",
                "DCDC模块数范围",
                "DCDC拓扑范围",
                "DCDC频率范围(kHz)",
                "隔离DCDC副边个数范围",
                "隔离DCDC模块数范围",
                "隔离DCDC拓扑范围",
                "隔离DCDC谐振频率范围(kHz)",
                "DCAC拓扑范围",
                "DCAC调制方式范围",
                "DCAC频率范围(kHz)"
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
                IsolatedDCDC_Q.ToString(),
                DCAC_Ma_min.ToString(),
                DCAC_Ma_max.ToString(),
                DCAC_φ.ToString(),
                Function.DoubleArrayToString(Math_VbusRange),
                Function.DoubleArrayToString(Math_VinvRange),
                Function.IntArrayToString(DCDC_numberRange),
                Function.StringArrayToString(DCDC_topologyRange),
                Function.DoubleArrayToString(DCDC_frequencyRange, 1e-3),
                Function.IntArrayToString(IsolatedDCDC_secondaryRange),
                Function.IntArrayToString(IsolatedDCDC_numberRange),
                Function.StringArrayToString(IsolatedDCDC_topologyRange),
                Function.DoubleArrayToString(IsolatedDCDC_resonanceFrequencyRange, 1e-3),
                Function.StringArrayToString(DCAC_topologyRange),
                Function.StringArrayToString(DCAC_modulationRange),
                Function.DoubleArrayToString(DCAC_frequencyRange, 1e-3)
            };
            return conditions;
        }

        /// <summary>
        /// 复制当前架构，保留设计条件
        /// </summary>
        /// <returns>复制结果</returns>
        public override Structure Clone()
        {
            return new ThreeLevelStructure()
            {
                Name = Name,
                Math_Psys = Math_Psys,
                Math_Vpv_min = Math_Vpv_min,
                Math_Vpv_max = Math_Vpv_max,
                Math_Vg = Math_Vg,
                Math_Vo = Math_Vo,
                Math_fg = Math_fg,
                IsolatedDCDC_Q = IsolatedDCDC_Q,
                DCAC_Ma_min = DCAC_Ma_min,
                DCAC_Ma_max = DCAC_Ma_max,
                DCAC_φ = DCAC_φ
            };
        }

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public override void Optimize(MainForm form, double progressMin, double progressMax)
        {
            double progress = progressMin;
            double dp = (progressMax - progressMin) / Math_VbusRange.Length;
            foreach (double Vbus in Math_VbusRange) //母线电压变化
            {
                form.PrintDetails(3, "Now DC bus voltage = " + Vbus + ":");
                //前级DC/DC变换器设计
                form.PrintDetails(3, "-------------------------");
                form.PrintDetails(3, "Front-stage DC/DC converters design...");
                DCDC = new DCDCConverter()
                {
                    PhaseNum = 1,
                    Math_Psys = Math_Psys,
                    Math_Vin_min = Math_Vpv_min,
                    Math_Vin_max = Math_Vpv_max,
                    IsInputVoltageVariation = true,
                    Math_Vo = Vbus,
                    NumberRange = DCDC_numberRange,
                    TopologyRange = DCDC_topologyRange,
                    FrequencyRange = DCDC_frequencyRange
                };
                DCDC.Optimize(form, progress, progress + dp * 0.2);
                progress += dp * 0.2;
                if (DCDC.AllDesignList.Size <= 0)
                {
                    progress += dp * 0.8;
                    form.Estimate_Result_ProgressBar_Set(progress);
                    continue;
                }
                double dp2 = (dp * 0.8) / Math_VinvRange.Length / IsolatedDCDC_secondaryRange.Length / IsolatedDCDC_numberRange.Length;
                foreach (double Vinv in Math_VinvRange) //逆变直流侧电压变化
                {
                    form.PrintDetails(3, "Now Inv DC voltage = " + Vinv + ":");
                    form.PrintDetails(3, "-------------------------");
                    foreach (int No in IsolatedDCDC_secondaryRange) //一拖N
                    {
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
                                NumberRange = new int[] { n * No },
                                TopologyRange = DCAC_topologyRange,
                                ModulationRange = DCAC_modulationRange,
                                FrequencyRange = DCAC_frequencyRange
                            };
                            DCAC.Optimize(form, progress, progress + dp2 * 0.3);
                            progress += dp2 * 0.3;
                            if (DCAC.AllDesignList.Size <= 0)
                            {
                                progress += dp2 * 0.7;
                                continue;
                            }

                            //隔离DC/DC变换器设计
                            form.PrintDetails(3, "-------------------------");
                            form.PrintDetails(3, "Isolated DC/DC converters design...");
                            IsolatedDCDC = new IsolatedDCDCConverter()
                            {
                                PhaseNum = 3,
                                Math_Psys = Math_Psys,
                                Math_Vin = Vbus,
                                IsInputVoltageVariation = false,
                                Math_Vo = Vinv,
                                Math_Q = IsolatedDCDC_Q,
                                SecondaryRange = new int[] { No },
                                NumberRange = new int[] { n },
                                TopologyRange = IsolatedDCDC_topologyRange,
                                FrequencyRange = IsolatedDCDC_resonanceFrequencyRange
                            };
                            IsolatedDCDC.Optimize(form, progress, progress + dp2 * 0.3);
                            progress += dp2 * 0.3;
                            if (IsolatedDCDC.AllDesignList.Size <= 0)
                            {
                                progress += dp2 * 0.4;
                                continue;
                            }

                            //整合得到最终结果
                            form.PrintDetails(3, "-------------------------");
                            form.PrintDetails(3, "Iso num=" + n + ", Iso sec=" + No + ", DC bus voltage=" + Vbus + ", Combining...");
                            ConverterDesignList newDesignList = new ConverterDesignList();
                            newDesignList.Combine(DCDC.ParetoDesignList);
                            newDesignList.Combine(IsolatedDCDC.ParetoDesignList);
                            newDesignList.Combine(DCAC.ParetoDesignList);
                            newDesignList.Transfer(new string[] { Vbus.ToString(), DCAC.Math_Vin.ToString() });
                            ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                            AllDesignList.Merge(newDesignList); //记录所有设计
                            progress += dp2 * 0.4;
                            form.Estimate_Result_ProgressBar_Set(progress);
                        }
                        form.PrintDetails(3, "=========================");
                    }
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
            EfficiencyCGC = double.Parse(configs[index++]);
            Volume = double.Parse(configs[index++]);
            Cost = double.Parse(configs[index++]);
            Math_Vbus = double.Parse(configs[index++]);
            DCAC_Vinv = double.Parse(configs[index++]);
            DCDC = new DCDCConverter()
            {
                PhaseNum = 1,
                Math_Psys = Math_Psys,
                Math_Vin_min = Math_Vpv_min,
                Math_Vin_max = Math_Vpv_max,
                IsInputVoltageVariation = true,
                Math_Vo = Math_Vbus,
                NumberRange = DCDC_numberRange,
                TopologyRange = DCDC_topologyRange,
                FrequencyRange = DCDC_frequencyRange
            };
            DCDC.Load(configs, ref index);
            IsolatedDCDC = new IsolatedDCDCConverter()
            {
                PhaseNum = 3,
                Math_Psys = Math_Psys,
                Math_Vin = Math_Vbus,
                IsInputVoltageVariation = false,
                Math_Vo = DCAC_Vinv,
                Math_Q = IsolatedDCDC_Q
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
                Math_φ = DCAC_φ,
            };
            DCAC.Load(configs, ref index);
            Converters = new Converter[] { DCDC, IsolatedDCDC, DCAC };
        }
    }
}
