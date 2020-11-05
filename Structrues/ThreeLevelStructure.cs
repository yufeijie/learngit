using PV_analysis.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public override string GetName()
        {
            return "三级架构";
        }

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>配置信息</returns>
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
                "DCAC电压调制比",
                "DCAC功率因数角(rad)",
                "母线电压范围",
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
        /// <returns>配置信息</returns>
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
                DCAC_Ma.ToString(),
                DCAC_phi.ToString(),
                Function.DoubleArrayToString(Math_VbusRange),
                Function.IntArrayToString(DCDC_numberRange),
                Function.StringArrayToString(DCDC_topologyRange),
                Function.DoubleArrayToString(DCDC_frequencyRange),
                Function.IntArrayToString(IsolatedDCDC_secondaryRange),
                Function.IntArrayToString(IsolatedDCDC_numberRange),
                Function.StringArrayToString(IsolatedDCDC_topologyRange),
                Function.DoubleArrayToString(IsolatedDCDC_resonanceFrequencyRange),
                Function.StringArrayToString(DCAC_topologyRange),
                Function.StringArrayToString(DCAC_modulationRange),
                Function.DoubleArrayToString(DCAC_frequencyRange)
            };
            return conditions;
        }

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public override void Optimize(MainForm form)
        {
            foreach (double Vbus in Math_VbusRange) //母线电压变化
            {
                form.PrintDetails("Now DC bus voltage = " + Vbus + ":");
                //前级DC/DC变换器设计
                form.PrintDetails("-------------------------");
                form.PrintDetails("Front-stage DC/DC converters design...");
                DCDC = new DCDCConverter(Math_Psys, Math_Vpv_min, Math_Vpv_max, Vbus)
                {
                    NumberRange = DCDC_numberRange,
                    TopologyRange = DCDC_topologyRange,
                    FrequencyRange = DCDC_frequencyRange
                };
                DCDC.Optimize(form);
                if (DCDC.AllDesignList.Size <= 0)
                {
                    continue;
                }
                foreach (int No in IsolatedDCDC_secondaryRange) //一拖N
                {
                    foreach (int n in IsolatedDCDC_numberRange)
                    {
                        //逆变器设计
                        form.PrintDetails("-------------------------");
                        form.PrintDetails("Inverters design...");
                        DCAC = new DCACConverter(Math_Psys, Math_Vg, Math_fg, DCAC_phi)
                        {
                            Math_Ma = DCAC_Ma,
                            NumberRange = new int[] { n * No },
                            TopologyRange = DCAC_topologyRange,
                            ModulationRange = DCAC_modulationRange,
                            FrequencyRange = DCAC_frequencyRange
                        };
                        DCAC.Optimize(form);
                        if (DCAC.AllDesignList.Size <= 0)
                        {
                            continue;
                        }

                        //隔离DC/DC变换器设计
                        form.PrintDetails("-------------------------");
                        form.PrintDetails("Isolated DC/DC converters design...");
                        IsolatedDCDC = new IsolatedDCDCConverter(Math_Psys, Vbus, DCAC.Math_Vin, IsolatedDCDC_Q)
                        {
                            SecondaryRange = new int[] { No },
                            NumberRange = new int[] { n },
                            TopologyRange = IsolatedDCDC_topologyRange,
                            FrequencyRange = IsolatedDCDC_resonanceFrequencyRange
                        };
                        IsolatedDCDC.Optimize(form);
                        if (IsolatedDCDC.AllDesignList.Size <= 0)
                        {
                            continue;
                        }

                        //整合得到最终结果
                        form.PrintDetails("-------------------------");
                        form.PrintDetails("Iso num=" + n + ", Iso sec=" + No + ", DC bus voltage=" + Vbus + ", Combining...");
                        ConverterDesignList newDesignList = new ConverterDesignList();
                        newDesignList.Combine(DCDC.ParetoDesignList);
                        newDesignList.Combine(IsolatedDCDC.ParetoDesignList);
                        newDesignList.Combine(DCAC.ParetoDesignList);
                        newDesignList.Transfer(new string[] { Vbus.ToString(), DCAC.Math_Vin.ToString() });
                        ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                        AllDesignList.Merge(newDesignList); //记录所有设计
                    }
                    form.PrintDetails("=========================");
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
            double DCAC_Vin = double.Parse(configs[index++]);
            DCDC = new DCDCConverter(Math_Psys, Math_Vpv_min, Math_Vpv_max, Math_Vbus);
            DCDC.Load(configs, ref index);
            IsolatedDCDC = new IsolatedDCDCConverter(Math_Psys, Math_Vbus, DCAC_Vin, IsolatedDCDC_Q);
            IsolatedDCDC.Load(configs, ref index);
            DCAC = new DCACConverter(Math_Psys, Math_Vg, Math_fg, DCAC_phi);
            DCAC.Load(configs, ref index);
            Converters = new Converter[] { DCDC, IsolatedDCDC, DCAC };
        }
    }
}
