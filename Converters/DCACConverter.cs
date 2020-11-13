using PV_analysis.Topologys;
using System;

namespace PV_analysis.Converters
{
    internal class DCACConverter : Converter
    {
        /// <summary>
        /// 并网电压
        /// </summary>
        public double Math_Vg { get; set; }

        /// <summary>
        /// 工频
        /// </summary>
        public double Math_fg { get; set; }

        /// <summary>
        /// 功率因数角
        /// </summary>
        public double Math_φ { get; set; }

        /// <summary>
        /// 最小幅度调制比
        /// </summary>
        public double Math_Ma_min { get; set; }

        /// <summary>
        /// 最大幅度调制比
        /// </summary>
        public double Math_Ma_max { get; set; }

        /// <summary>
        /// 调制方式
        /// </summary>
        public string Modulation { get; set; }

        /// <summary>
        /// 模块数范围
        /// </summary>
        public int[] NumberRange { get; set; }

        /// <summary>
        /// 拓扑范围
        /// </summary>
        public string[] TopologyRange { get; set; }

        /// <summary>
        /// 调制方式范围
        /// </summary>
        public string[] ModulationRange { get; set; }

        /// <summary>
        /// 开关频率范围
        /// </summary>
        public double[] FrequencyRange { get; set; }

        /// <summary>
        /// 获取变换单元名
        /// </summary>
        /// <returns>变换单元名</returns>
        public override string GetCategory()
        {
            return "逆变单元";
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            string[] data = { Number.ToString(), Math_fs.ToString(), Modulation, Topology.GetType().Name };
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
                "直流侧电压",
                "并网电压",
                "并网频率(Hz)",
                "最小幅度调制比",
                "最大幅度调制比",
                "功率因数角(rad)",
                "模块数范围",
                "拓扑范围",
                "调制方式范围",
                "频率范围(kHz)"
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
                Math_Vin.ToString(),
                Math_Vg.ToString(),
                Math_fg.ToString(),
                Math_Ma_min.ToString(),
                Math_Ma_max.ToString(),
                Math_φ.ToString(),
                Function.IntArrayToString(NumberRange),
                Function.StringArrayToString(TopologyRange),
                Function.StringArrayToString(ModulationRange),
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
                case "CHB":
                    Topology = new CHB(this);
                    break;
                default:
                    Topology = null;
                    break;
            }
        }

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public override void Optimize(MainForm form)
        {
            foreach (int n in NumberRange) //模块数变化
            {
                Number = n;
                //电压调制比检查
                double Ma = Math_Vo * Math.Sqrt(2) / (n * Math_Vin);
                if (Ma > Math_Ma_min && Ma < Math_Ma_max)
                {
                    foreach (double fs in FrequencyRange) //谐振频率变化
                    {
                        Math_fs = fs;
                        foreach (string mo in ModulationRange) //调制方式变化
                        {
                            Modulation = mo;
                            foreach (string tp in TopologyRange) //拓扑变化
                            {
                                CreateTopology(tp);
                                form.PrintDetails("Now topology=" + tp + ", modulation=" + mo + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                                Design(form);
                            }
                        }
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
            Modulation = configs[index++];
            CreateTopology(configs[index++]);
            Topology.Load(configs, ref index);
        }
    }
}
