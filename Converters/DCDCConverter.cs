using PV_analysis.Components;
using PV_analysis.Informations;
using PV_analysis.Topologys;
using System.Collections.Generic;

namespace PV_analysis.Converters
{
    /// <summary>
    /// DC/DC变换器类
    /// </summary>
    internal class DCDCConverter : Converter
    {
        /// <summary>
        /// 获取类型名
        /// </summary>
        /// <returns>类型名</returns>
        public override string GetTypeName()
        {
            return "前级DC/DC变换单元";
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
        /// <returns>设计条件标题</returns>
        public override string[] GetConditionTitles()
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
        /// <returns>设计条件</returns>
        public override string[] GetConditions()
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
                Function.DoubleArrayToString(FrequencyRange, 1e-3)
            };
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
                new Info("模块数", Number),
                new Info("开关频率", (Math_fs / 1e3).ToString("f1") + "kHz"),
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
                (MainForm.ControlType.Text, "模块数"),
                (MainForm.ControlType.Text, "开关频率"),
            };
            return list;
        }

        /// <summary>
        /// 复制当前变换器，保留设计条件
        /// </summary>
        /// <returns>复制结果</returns>
        public override Equipment Clone()
        {
            return new DCDCConverter()
            {
                Name = Name,
                PhaseNum = PhaseNum,
                Math_Psys = Math_Psys,
                Math_Vin_min = Math_Vin_min,
                Math_Vin_max = Math_Vin_max,
                IsInputVoltageVariation = IsInputVoltageVariation,
                Math_Vo = Math_Vo
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
        /// 根据给定的条件进行优化设计
        /// </summary>
        public override void Optimize(MainForm form, double progressMin, double progressMax)
        {
            double progress = progressMin;
            double dp = (progressMax - progressMin) / NumberRange.Length / FrequencyRange.Length / TopologyRange.Length;
            foreach (int n in NumberRange) //模块数变化
            {
                Number = n;
                foreach (double fs in FrequencyRange) //开关频率变化
                {
                    Math_fs = fs;
                    foreach (string tp in TopologyRange) //拓扑变化
                    {
                        CreateTopology(tp);
                        form.PrintDetails(2, "Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                        Design(form);
                        progress += dp;
                        form.Estimate_Result_ProgressBar_Set(progress);
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
            EfficiencyEval = double.Parse(configs[index++]);
            Volume = double.Parse(configs[index++]);
            Cost = double.Parse(configs[index++]);
            Number = int.Parse(configs[index++]);
            Math_fs = double.Parse(configs[index++]);
            CreateTopology(configs[index++]);
            Topology.Load(configs, ref index);
        }
    }
}
