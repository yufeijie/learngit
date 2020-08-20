using PV_analysis.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Systems
{
    internal class ThreeLevelStructure : Structure
    {
        private DCDCConverter DCDC;
        private IsolatedDCDCConverter isolatedDCDC;
        private DCACConverter DCAC;

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConditionTitles()
        {
            string[] conditionTitles =
            {
                "Total power",
                "PV min voltage",
                "PV max voltage",
                "Grid voltage",
                "Grid frequency(Hz)",
                "Isolated DCDC quality factor default",
                "DCAC power factor angle(rad)",
                "DC bus voltage range",
                "DCDC number range",
                "DCDC topology range",
                "DCDC frequency range(kHz)",
                "Isolated DCDC topology range",
                "Isolated DCDC resonance frequency range(kHz)",
                "DCAC number range",
                "DCAC topology range",
                "DCAC modulation range",
                "DCAC frequency range(kHz)"
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
                Math_Psys.ToString(),
                Math_Vpv_min.ToString(),
                Math_Vpv_max.ToString(),
                Math_Vg.ToString(),
                Math_fg.ToString(),
                IsolatedDCDC_Q.ToString(),
                Math_phi.ToString(),
                Function.DoubleArrayToString(Math_VbusRange),
                Function.IntArrayToString(DCDC_numberRange),
                Function.StringArrayToString(DCDC_topologyRange),
                Function.DoubleArrayToString(DCDC_frequencyRange),
                Function.StringArrayToString(IsolatedDCDC_topologyRange),
                Function.DoubleArrayToString(IsolatedDCDC_resonanceFrequencyRange),
                Function.IntArrayToString(DCAC_numberRange),
                Function.StringArrayToString(DCAC_topologyRange),
                Function.StringArrayToString(DCAC_modulationRange),
                Function.DoubleArrayToString(DCAC_frequencyRange)
            };
            return conditions;
        }

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public override void Optimize()
        {
            foreach (double Vbus in Math_VbusRange) //母线电压变化
            {
                Console.WriteLine("Now DC bus voltage = " + Vbus + ":");
                //前级DC/DC变换器设计
                Console.WriteLine("-------------------------");
                Console.WriteLine("Front-stage DC/DC converters design...");
                DCDC = new DCDCConverter(Math_Psys, Math_Vpv_min, Math_Vpv_max, Vbus)
                {
                    NumberRange = DCDC_numberRange,
                    TopologyRange = DCDC_topologyRange,
                    FrequencyRange = DCDC_frequencyRange
                };
                DCDC.Optimize();
                if (DCDC.AllDesignList.Size <= 0)
                {
                    continue;
                }
                foreach (int j in DCAC_numberRange) //目前只考虑一拖一
                {
                    //逆变器设计
                    Console.WriteLine("-------------------------");
                    Console.WriteLine("Inverters design...");
                    DCAC = new DCACConverter(Math_Psys, Math_Vo, Math_fg, Math_phi)
                    {
                        NumberRange = new int[] { j },
                        TopologyRange = DCAC_topologyRange,
                        ModulationRange = DCAC_modulationRange,
                        FrequencyRange = DCAC_frequencyRange,
                        Math_Vin_def = 0
                    };
                    DCAC.Optimize();
                    if (DCAC.AllDesignList.Size <= 0)
                    {
                        continue;
                    }

                    //隔离DC/DC变换器设计
                    Console.WriteLine("-------------------------");
                    Console.WriteLine("Isolated DC/DC converters design...");
                    isolatedDCDC = new IsolatedDCDCConverter(Math_Psys, Vbus, DCAC.Math_Vin, IsolatedDCDC_Q)
                    {
                        NumberRange = new int[] { j },
                        TopologyRange = IsolatedDCDC_topologyRange,
                        FrequencyRange = IsolatedDCDC_resonanceFrequencyRange
                    };
                    isolatedDCDC.Optimize();
                    if (isolatedDCDC.AllDesignList.Size <= 0)
                    {
                        continue;
                    }

                    //整合得到最终结果
                    Console.WriteLine("-------------------------");
                    Console.WriteLine("Inv num=" + j + ", DC bus voltage=" + Vbus + ", Combining...");
                    ConverterDesignList newDesignList = new ConverterDesignList();
                    newDesignList.Combine(DCDC.ParetoDesignList);
                    newDesignList.Combine(isolatedDCDC.ParetoDesignList);
                    newDesignList.Combine(DCAC.ParetoDesignList);
                    newDesignList.Transfer(new string[] { Vbus.ToString(), DCAC.Math_Vin.ToString() });
                    ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                    AllDesignList.Merge(newDesignList); //记录所有设计
                }
                Console.WriteLine("=========================");
            }
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public override void Load(string[] configs, int index)
        {
            //Number = int.Parse(configs[index++]);
            //Math_fs = double.Parse(configs[index++]);
            //CreateTopology(configs[index++]);
            //Topology.Load(configs, index);
        }
    }
}
