using PV_analysis.Topologys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Converters
{
    internal class DCACConverter : Converter
    {
        /// <summary>
        /// 直流侧输入电压（若设置此值，则按照直流侧设计）
        /// </summary>
        public double Math_Vin { get; set; } = 0;

        /// <summary>
        /// 整体输出电压
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
        /// 初始化
        /// </summary>
        /// <param name="Psys">系统功率</param>
        /// <param name="Vo">整体输出电压</param>
        /// <param name="fg">工频</param>
        /// <param name="phi">功率因数角</param>
        public DCACConverter(double Psys, double Vo, double fg, double phi)
        {
            Math_Psys = Psys;
            Math_Vo = Vo;
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
            string[] data = { Number.ToString(), (Math_fs / 1e3).ToString(), Math_Ma.ToString(), Topology.GetType().Name };
            return data;
        }

        /// <summary>
        /// 创建拓扑（实际上为调制方式不同）
        /// </summary>
        /// <param name="name">拓扑名</param>
        public void CreateTopology(string name)
        {
            switch (name)
            {
                case "CHB_PSPWM":
                    Topology = new CHB_PSPWM(this);
                    break;
                default:
                    Topology = null;
                    break;
            }
        }
    }
}
