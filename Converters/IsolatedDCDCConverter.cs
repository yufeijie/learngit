using PV_analysis.Informations;
using PV_analysis.Topologys;
using System.Collections.Generic;

namespace PV_analysis.Converters
{
    internal class IsolatedDCDCConverter : Converter
    {
        /// <summary>
        /// 副边个数
        /// </summary>
        public int Math_No { get; set; }

        /// <summary>
        /// 品质因数
        /// </summary>
        public double Math_Q { get; set; }

        /// <summary>
        /// 电感比
        /// </summary>
        public double Math_k { get; set; }

        /// <summary>
        /// 副边个数范围
        /// </summary>
        public int[] Math_No_Range { get; set; }

        /// <summary>
        /// 品质因数范围
        /// </summary>
        public double[] Math_Q_Range { get; set; }

        /// <summary>
        /// 电感比范围
        /// </summary>
        public double[] Math_k_Range { get; set; }

        /// <summary>
        /// 获取类型名
        /// </summary>
        /// <returns>类型名</returns>
        public override string GetTypeName()
        {
            return "隔离DC/DC变换单元";
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            string[] data = { Math_No.ToString(), Number.ToString(), Math_fs.ToString(), Math_Q.ToString(), Math_k.ToString(), Topology.GetType().Name };
            return data;
        }

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>设计条件标题</returns>
        public override string[] GetConditionTitles()
        {
            string[] conditionTitles;
            if (IsInputVoltageVariation)
            {
                conditionTitles = new string[]
                {
                    "评估对象",
                    "总功率",
                    "输入电压最小值",
                    "输入电压最大值",
                    "输出电压",
                    "副边个数范围",
                    "模块数范围",
                    "拓扑范围",
                    "开关频率范围(kHz)",
                    "品质因数范围",
                    "电感比范围",
                };
            }
            else
            {
                conditionTitles = new string[]
                {
                    "评估对象",
                    "总功率",
                    "输入电压",
                    "输出电压",
                    "副边个数范围",
                    "模块数范围",
                    "拓扑范围",
                    "开关频率范围(kHz)",
                    "品质因数范围",
                    "电感比范围",
                };
            }
            return conditionTitles;
        }

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>设计条件</returns>
        public override string[] GetConditions()
        {
            string[] conditions;
            if (IsInputVoltageVariation)
            {
                conditions = new string[]
                {
                    GetType().Name + "_TwoStage",
                    Math_Psys.ToString(),
                    Math_Vin_min.ToString(),
                    Math_Vin_max.ToString(),
                    Math_Vo.ToString(),
                    Function.IntArrayToString(Math_No_Range),
                    Function.IntArrayToString(NumberRange),
                    Function.StringArrayToString(TopologyRange),
                    Function.DoubleArrayToString(FrequencyRange, 1e-3),
                    Function.DoubleArrayToString(Math_Q_Range),
                    Function.DoubleArrayToString(Math_k_Range),
                };
            }
            else
            {
                conditions = new string[]
                {
                    GetType().Name,
                    Math_Psys.ToString(),
                    Math_Vin.ToString(),
                    Math_Vo.ToString(),
                    Function.IntArrayToString(Math_No_Range),
                    Function.IntArrayToString(NumberRange),
                    Function.StringArrayToString(TopologyRange),
                    Function.DoubleArrayToString(FrequencyRange, 1e-3),
                    Function.DoubleArrayToString(Math_Q_Range),
                    Function.DoubleArrayToString(Math_k_Range),
                };
            }
            return conditions;
        }

        /// <summary>
        /// 获取配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override List<Info> GetConfigInfo()
        {
            List<Info> list = new List<Info>
            {                
                new Info("副边个数", Math_No),
                new Info("模块数", Number),
                new Info("开关频率", (Math_fs / 1e3).ToString("f1") + "kHz"),
                new Info("品质因数", Math_Q),
                new Info("电感比", Math_k),
                new Info("拓扑", Topology.GetName())
            };
            return list;
        }
        
        /// <summary>
        /// 获取手动设计信息
        /// </summary>
        /// <returns>手动设计信息</returns>
        public List<(MainForm.ControlType, string)> GetManualInfo()
        {
            List<(MainForm.ControlType, string)> list = new List<(MainForm.ControlType, string)>()
            {                
                (MainForm.ControlType.Text, "副边个数"),
                (MainForm.ControlType.Text, "模块数"),
                (MainForm.ControlType.Text, "开关频率"),
                (MainForm.ControlType.Text, "品质因数"),
                (MainForm.ControlType.Text, "电感比"),
            };
            return list;
        }

        /// <summary>
        /// 复制当前变换器，保留设计条件
        /// </summary>
        /// <returns>复制结果</returns>
        public override Equipment Clone()
        {
            return new IsolatedDCDCConverter()
            {
                Name = Name,
                PhaseNum = PhaseNum,
                Math_Psys = Math_Psys,
                Math_Vin_min = Math_Vin_min,
                Math_Vin_max = Math_Vin_max,
                Math_Vin = Math_Vin,
                IsInputVoltageVariation = IsInputVoltageVariation,
                Math_Vo = Math_Vo,
            };
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
                case "LLC":
                    Topology = new LLC(this);
                    break;
                case "TL_LLC":
                    Topology = new TL_LLC(this);
                    break;
                case "HB_TL_LLC":
                    Topology = new HB_TL_LLC(this);
                    break;
                case "DTCSRC":
                    Topology = new DTCSRC(this);
                    break;
                default:
                    Topology = null;
                    break;
            }
        }

        /// <summary>
        /// 根据给定的条件进行优化设计
        /// </summary>
        public override void Optimize(MainForm form, double progressMin, double progressMax)
        {
            double progress = progressMin;
            double dp = (progressMax - progressMin) / NumberRange.Length / FrequencyRange.Length / Math_No_Range.Length / TopologyRange.Length;
            foreach (int n in NumberRange) //模块数变化
            {
                Number = n;
                foreach (double fs in FrequencyRange) //开关频率变化
                {
                    Math_fs = fs;
                    foreach (int No in Math_No_Range) //副边个数变化
                    {
                        Math_No = No;
                        foreach (double Q in Math_Q_Range) //品质因数变化
                        {
                            Math_Q = Q;
                            if (Math_k_Range == null || Math_k_Range.Length == 0)
                            {
                                Math_k_Range = new double[] { 0 };
                            }
                            foreach (double k in Math_k_Range) //电感比变化
                            {
                                Math_k = k;
                                foreach (string tp in TopologyRange) //拓扑变化
                                {
                                    if (Math_k <= 0 && (tp.Equals("HB_TL_LLC") || tp.Equals("LLC")))
                                    {
                                        break;
                                    }
                                    CreateTopology(tp);
                                    form.PrintDetails(2, "Now topology=" + tp + ", No=" + No + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz, Q=" + Q + ", k=" + k);
                                    Design(form);
                                    progress += dp;
                                    form.Estimate_Result_ProgressBar_Set(progress);
                                }
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
            Math_No = int.Parse(configs[index++]);
            Number = int.Parse(configs[index++]);
            Math_fs = double.Parse(configs[index++]);
            Math_Q = double.Parse(configs[index++]);
            Math_k = double.Parse(configs[index++]);
            CreateTopology(configs[index++]);
            Topology.Load(configs, ref index);
        }
    }
}
