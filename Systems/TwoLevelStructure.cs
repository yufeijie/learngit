using PV_analysis.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Systems
{
    internal class TwoLevelStructure : Structure
    {
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
                "DCAC input voltage",
                "DCAC power factor angle(rad)",
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
                DCAC_Vin_def.ToString(),
                Math_phi.ToString(),
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
            foreach (int j in DCAC_numberRange) //目前只考虑一拖一
            {
                //隔离DC/DC变换器设计
                Console.WriteLine("-------------------------");
                Console.WriteLine("Isolated DC/DC converters design...");
                isolatedDCDC = new IsolatedDCDCConverter(Math_Psys, Math_Vpv_min, Math_Vpv_max, DCAC_Vin_def, IsolatedDCDC_Q)
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

                //逆变器设计
                Console.WriteLine("-------------------------");
                Console.WriteLine("Inverters design...");
                DCAC = new DCACConverter(Math_Psys, Math_Vo, Math_fg, Math_phi)
                {
                    NumberRange = new int[] { j },
                    TopologyRange = DCAC_topologyRange,
                    ModulationRange = DCAC_modulationRange,
                    FrequencyRange = DCAC_frequencyRange,
                    Math_Vin_def = DCAC_Vin_def
                };
                DCAC.Optimize();
                if (DCAC.AllDesignList.Size <= 0)
                {
                    continue;
                }

                //整合得到最终结果
                Console.WriteLine("-------------------------");
                Console.WriteLine("Inv num=" + j + ", Combining...");
                ConverterDesignList newDesignList = new ConverterDesignList();
                newDesignList.Combine(isolatedDCDC.ParetoDesignList);
                newDesignList.Combine(DCAC.ParetoDesignList);
                newDesignList.Transfer(new string[] { });
                ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                AllDesignList.Merge(newDesignList); //记录所有设计
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
