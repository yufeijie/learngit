using PV_analysis.Components;
using PV_analysis.Topologys;

namespace PV_analysis.Converters
{
    /// <summary>
    /// DC/DC变换器类
    /// </summary>
    internal class DCDCConverter : Converter
    {
        //---可选参数---
        private readonly double diodeCurrentDropRate = 125e6; //二极管电流下降速率(A/s)

        //---给定参数---
        private double math_Vin_min; //输入电压最小值
        private double math_Vin_max; //输入电压最大值
        private double math_Vo; //输出电压

        /// <summary>
        /// 拓扑
        /// </summary>
        public Topology Topology { get; set; }

        /// <summary>
        /// 输入电压最小值
        /// </summary>
        public double Math_Vin_min { get { return math_Vin_min; } }

        /// <summary>
        /// 输入电压最大值
        /// </summary>
        public double Math_Vin_max { get { return math_Vin_max; } }

        /// <summary>
        /// 输出电压
        /// </summary>
        public double Math_Vo { get { return math_Vo; } }

        /// <summary>
        /// Pareto最优设计方案
        /// </summary>
        public ConverterDesignList ParetoDesignList { get; } = new ConverterDesignList();

        /// <summary>
        /// 所有设计方案
        /// </summary>
        public ConverterDesignList AllDesignList { get; } = new ConverterDesignList() { IsAll = true };

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="math_Psys">系统功率</param>
        /// <param name="math_Vin_min">最小输入电压</param>
        /// <param name="math_Vin_max">最大输入电压</param>
        /// <param name="math_Vo">输出电压</param>
        /// <param name="numberRange">可用模块数</param>
        /// <param name="topologyRange">可用拓扑</param>
        /// <param name="frequencyRange">可用开关频率</param>
        public DCDCConverter(double Psys, double Vin_min, double Vin_max, double Vo)
        {
            math_Psys = Psys;
            math_Vin_min = Vin_min;
            math_Vin_max = Vin_max;
            math_Vo = Vo;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public string[] GetConfigs()
        {
            double P = math_Psys / number;
            string[] data = { number.ToString(), "ThreeLevelBoost", (math_fs / 1e3).ToString() };
            return data;
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
                newDesignList.Transfer(designCombinationList, math_Psys, number, GetConfigs()); //转化为变换器设计
                ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                AllDesignList.Merge(newDesignList); //记录所有设计
            }
        }
    }
}
