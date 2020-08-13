using PV_analysis.Components;
using PV_analysis.Topologys;
using System;

namespace PV_analysis.Converters
{
    /// <summary>
    /// DC/DC变换器类
    /// </summary>
    internal class DCDCConverter : Converter
    {
        /// <summary>
        /// 输入电压最小值
        /// </summary>
        public double Math_Vin_min { get; }

        /// <summary>
        /// 输入电压最大值
        /// </summary>
        public double Math_Vin_max { get; }

        /// <summary>
        /// 输出电压
        /// </summary>
        public double Math_Vo { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="Psys">系统功率</param>
        /// <param name="Vin_min">最小输入电压</param>
        /// <param name="Vin_max">最大输入电压</param>
        /// <param name="Vo">输出电压</param>
        public DCDCConverter(double Psys, double Vin_min, double Vin_max, double Vo)
        {
            Math_Psys = Psys;
            Math_Vin_min = Vin_min;
            Math_Vin_max = Vin_max;
            Math_Vo = Vo;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            string[] data = { Number.ToString(), (Math_fs / 1e3).ToString(), Topology.GetType().Name };
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
                case "ThreeLevelBoost":
                    Topology = new ThreeLevelBoost(this);
                    break;
                case "TwoLevelBoost":
                    Topology = new TwoLevelBoost(this);
                    break;
                case "InterleavedBoost":
                    Topology = new InterleavedBoost(this);
                    break;
                default:
                    Topology = null;
                    break;
            }
        }
    }
}
