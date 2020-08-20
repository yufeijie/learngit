using PV_analysis.Topologys;
using System;

namespace PV_analysis.Converters
{
    internal class DCACConverter : Converter
    {
        /// <summary>
        /// 直流侧输入电压（若设置此值，则按照直流侧设计）
        /// </summary>
        public double Math_Vin { get; set; }

        /// <summary>
        /// 并网电压
        /// </summary>
        public double Math_Vg { get; }

        /// <summary>
        /// 整体输出电压（并网相电压）
        /// </summary>
        public double Math_Vo { get; }

        /// <summary>
        /// 工频
        /// </summary>
        public double Math_fg { get; }

        /// <summary>
        /// 功率因数角
        /// </summary>
        public double Math_phi { get; }

        /// <summary>
        /// 幅度调制比
        /// </summary>
        public double Math_Ma { get; set; } = 0.95;

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
        /// 初始化
        /// </summary>
        /// <param name="Psys">系统功率</param>
        /// <param name="Vo">整体输出电压</param>
        /// <param name="fg">工频</param>
        /// <param name="phi">功率因数角</param>
        public DCACConverter(double Psys, double Vg, double fg, double phi)
        {
            Math_Psys = Psys;
            Math_Vg = Vg;
            Math_Vo = Vg / Math.Sqrt(3);
            Math_fg = fg;
            Math_phi = phi;
            PhaseNum = 3;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            string[] data = { Number.ToString(), Math_fs.ToString(), Math_Ma.ToString(), Modulation, Topology.GetType().Name };
            return data;
        }

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>配置信息</returns>
        protected override string[] GetConditionTitles()
        {
            string[] conditionTitles = new string[]
            {
                "Total power",
                "Grid voltage",
                "Grid frequency(Hz)",
                "Power factor angle(rad)",
                "Number range",
                "Topology range",
                "Modulation range",
                "Frequency range(kHz)"
            };
            return conditionTitles;
        }

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>配置信息</returns>
        protected override string[] GetConditions()
        {
            string[] conditions = new string[]
            {
                Math_Psys.ToString(),
                Math_Vg.ToString(),
                Math_fg.ToString(),
                Math_phi.ToString(),
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
        public void Optimize()
        {
            foreach (int n in NumberRange) //模块数变化
            {
                Number = n;
                foreach (double fs in FrequencyRange) //谐振频率变化
                {
                    Math_fs = fs;
                    foreach (string mo in ModulationRange) //调制方式变化
                    {
                        Modulation = mo;
                        foreach (string tp in TopologyRange) //拓扑变化
                        {
                            Math_Vin = 0;
                            CreateTopology(tp);
                            Console.WriteLine("Now topology=" + tp + ", modulation=" + mo + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                            Design();
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
        public override void Load(string[] configs, int index)
        {
            Number = int.Parse(configs[index++]);
            Math_fs = double.Parse(configs[index++]);
            Math_Ma = double.Parse(configs[index++]);
            Modulation = configs[index++];
            CreateTopology(configs[index++]);
            Topology.Load(configs, index);
        }

    }
}
