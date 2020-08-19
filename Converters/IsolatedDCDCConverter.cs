using PV_analysis.Topologys;

namespace PV_analysis.Converters
{
    internal class IsolatedDCDCConverter : Converter
    {
        /// <summary>
        /// 输入电压
        /// </summary>
        public double Math_Vin { get; }

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
        /// <param name="Psys">系统功率</param>
        /// <param name="Vin">输入电压</param>
        /// <param name="Vo">输出电压</param>
        /// <param name="Q">品质因数</param>
        public IsolatedDCDCConverter(double Psys, double Vin, double Vo, double Q)
        {
            Math_Psys = Psys;
            Math_Vin = Vin;
            Math_Vo = Vo;
            Math_Q = Q;
            PhaseNum = 3;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="Psys">系统功率</param>
        /// <param name="Vin_min">输入电压最小值</param>
        /// <param name="Vin_max">输入电压最大值</param>
        /// <param name="Vo">输出电压</param>
        /// <param name="Q">品质因数</param>
        public IsolatedDCDCConverter(double Psys, double Vin_min, double Vin_max, double Vo, double Q)
        {
            Math_Psys = Psys;
            Math_Vin_min = Vin_min;
            Math_Vin_max = Vin_max;
            Math_Vo = Vo;
            Math_Q = Q;
            PhaseNum = 3;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            string[] data = { Number.ToString(), Math_fr.ToString(), Topology.GetType().Name };
            return data;
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public override void Load(string[] configs, int index)
        {
            Number = int.Parse(configs[index++]);
            Math_fr = double.Parse(configs[index++]);
            CreateTopology(configs[index++]);
            Topology.Load(configs, index);
        }

        /// <summary>
        /// 创建拓扑（此前需保证变换器的参数已配置好）
        /// </summary>
        /// <param name="name">拓扑名</param>
        public void CreateTopology(string name)
        {
            switch (name)
            {
                case "SRC":
                    Topology = new SRC(this);
                    break;
                case "DTCSRC":
                    Topology = new DTCSRC(this);
                    break;
                default:
                    Topology = null;
                    break;
            }
        }
    }
}
