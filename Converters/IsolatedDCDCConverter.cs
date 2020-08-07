using PV_analysis.Components;
using PV_analysis.Topologys;
using System;

namespace PV_analysis.Converters
{
    internal class IsolatedDCDCConverter : Converter
    {
        /// <summary>
        /// 输入电压
        /// </summary>
        public double Math_Vin { get; }

        /// <summary>
        /// 输出电压
        /// </summary>
        public double Math_Vo { get; }

        /// <summary>
        /// 品质因数
        /// </summary>
        public double Math_Q { get; }

        /// <summary>
        /// 谐振频率
        /// </summary>
        public double Math_fr { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="math_Psys">系统功率</param>
        /// <param name="math_Vin">输入电压</param>
        /// <param name="math_Vo">输出电压</param>
        /// <param name="math_Q">品质因数</param>
        public IsolatedDCDCConverter(double Psys, double Vin, double Vo, double Q)
        {
            Math_Psys = Psys;
            Math_Vin = Vin;
            Math_Vo = Vo;
            Math_Q = Q;
            PhaseNum = 3;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public string[] GetConfigs()
        {
            string[] data = { Number.ToString(), (Math_fr / 1e3).ToString(), Math_Q.ToString(), Topology.GetType().Name };
            return data;
        }

        /// <summary>
        /// 创建拓扑
        /// </summary>
        /// <param name="name">拓扑名</param>
        public void CreateTopology(string name)
        {
            switch (name)
            {
                case "SRC":
                    Topology = new SRC(this);
                    break;
                default:
                    Topology = null;
                    break;
            }
        }

        /// <summary>
        /// 自动设计，整合设计结果（不会覆盖之前的设计结果）
        /// </summary>
        public void Design()
        {
            Topology.Design();
            foreach (Component[] components in Topology.ComponentGroups)
            {
                //检查该组器件是否都有设计结果
                bool ok = true;
                foreach (Component component in components)
                {
                    if (component.DesignList.Size == 0)
                    {
                        Console.WriteLine(component.GetType().Name + " design Failed");
                        ok = false;
                        break;
                    }
                }
                if (!ok) { continue; }

                //如果所有器件都有设计方案，则组合并记录
                ComponentDesignList designCombinationList = new ComponentDesignList();
                foreach (Component component in components) //组合各个器件的设计方案
                {
                    designCombinationList.Combine(component.DesignList);
                }
                ConverterDesignList newDesignList = new ConverterDesignList();
                newDesignList.Transfer(designCombinationList, Math_Psys, Number, GetConfigs()); //转化为变换器设计
                ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                AllDesignList.Merge(newDesignList); //记录所有设计
            }
        }
    }
}
