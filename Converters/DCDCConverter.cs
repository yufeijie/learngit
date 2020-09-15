﻿using PV_analysis.Topologys;
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
        /// 模块数范围
        /// </summary>
        public int[] NumberRange { get; set; }

        /// <summary>
        /// 拓扑范围
        /// </summary>
        public string[] TopologyRange { get; set; }

        /// <summary>
        /// 开关频率范围
        /// </summary>
        public double[] FrequencyRange { get; set; }

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
            string[] data = { Number.ToString(), Math_fs.ToString(), Topology.GetType().Name };
            return data;
        }

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>配置信息</returns>
        protected override string[] GetConditionTitles()
        {
            string[] conditionTitles =
            {
                "评估对象",
                "总功率",
                "输入电压最小值",
                "输入电压最大值",
                "输出电压",
                "模块数范围",
                "拓扑范围",
                "谐振频率范围(kHz)"
            };
            return conditionTitles;
        }

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>配置信息</returns>
        protected override string[] GetConditions()
        {
            string[] conditions =
            {
                GetType().Name,
                Math_Psys.ToString(),
                Math_Vin_min.ToString(),
                Math_Vin_max.ToString(),
                Math_Vo.ToString(),
                Function.IntArrayToString(NumberRange),
                Function.StringArrayToString(TopologyRange),
                Function.DoubleArrayToString(FrequencyRange)
            };
            return conditions;
        }

        /// <summary>
        /// 创建拓扑（此前需保证变换器的参数已配置好）
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

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public override void Optimize()
        {
            foreach (int n in NumberRange) //模块数变化
            {
                Number = n;
                foreach (double fs in FrequencyRange) //开关频率变化
                {
                    Math_fs = fs;
                    foreach (string tp in TopologyRange) //拓扑变化
                    {
                        CreateTopology(tp);
                        Console.WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                        Design();
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
            Number = int.Parse(configs[index++]);
            Math_fs = double.Parse(configs[index++]);
            CreateTopology(configs[index++]);
            Topology.Load(configs, ref index);
        }
    }
}
