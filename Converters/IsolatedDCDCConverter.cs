using PV_analysis.Topologys;
using System;

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
        /// 谐振频率
        /// </summary>
        public double Math_fr { get; set; }

        /// <summary>
        /// 副边个数范围
        /// </summary>
        public int[] SecondaryRange { get; set; }

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
        /// 获取变换单元名
        /// </summary>
        /// <returns>变换单元名</returns>
        public override string GetCategory()
        {
            return "隔离DC/DC变换单元";
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            string[] data = { Math_No.ToString(), Number.ToString(), Math_fr.ToString(), Topology.GetType().Name };
            return data;
        }

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>配置信息</returns>
        protected override string[] GetConditionTitles()
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
                    "品质因数",
                    "副边个数范围",
                    "模块数范围",
                    "拓扑范围",
                    "谐振频率范围(kHz)"
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
                    "品质因数",
                    "副边个数范围",
                    "模块数范围",
                    "拓扑范围",
                    "谐振频率范围(kHz)"
                };
            }
            return conditionTitles;
        }

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>配置信息</returns>
        protected override string[] GetConditions()
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
                    Math_Q.ToString(),
                    Function.IntArrayToString(SecondaryRange),
                    Function.IntArrayToString(NumberRange),
                    Function.StringArrayToString(TopologyRange),
                    Function.DoubleArrayToString(FrequencyRange)
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
                    Math_Q.ToString(),
                    Function.IntArrayToString(SecondaryRange),
                    Function.IntArrayToString(NumberRange),
                    Function.StringArrayToString(TopologyRange),
                    Function.DoubleArrayToString(FrequencyRange)
                };
            }
            return conditions;
        }

        /// <summary>
        /// 复制当前变换器，保留设计条件
        /// </summary>
        /// <returns>复制结果</returns>
        public override Converter Clone()
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
                Math_Q = Math_Q
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
                case "DTCSRC":
                    Topology = new DTCSRC(this);
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
                foreach (double fr in FrequencyRange) //谐振频率变化
                {
                    Math_fr = fr;
                    foreach (int No in SecondaryRange) //副边个数变化
                    {
                        Math_No = No;
                        foreach (string tp in TopologyRange) //拓扑变化
                        {
                            CreateTopology(tp);
                            if (tp.Equals("SRC")) //目前多输出仅支持SRC
                            {
                                form.PrintDetails("Now topology=" + tp + ", No=" + No + ", n=" + n + ", fs=" + string.Format("{0:N1}", fr / 1e3) + "kHz");
                                Design(form);
                            }
                            else
                            {
                                if (No == 1)
                                {
                                    form.PrintDetails("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fr / 1e3) + "kHz");
                                    Design(form);
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
            Math_fr = double.Parse(configs[index++]);
            CreateTopology(configs[index++]);
            Topology.Load(configs, ref index);
        }
    }
}
