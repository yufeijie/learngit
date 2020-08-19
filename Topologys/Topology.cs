using PV_analysis.Components;
using PV_analysis.Converters;
using System.Runtime.ConstrainedExecution;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// 拓扑抽象类，用于描述拓扑的共有特征
    /// </summary>
    internal abstract class Topology
    {
        protected Component[][] componentGroups; //可行元器件组合
        protected Component[] components; //可行元件组合中出现的所有元器件（待设计的元器件）

        //电路参数
        protected double math_Pfull; //满载功率
        protected double math_P; //功率

        /// <summary>
        /// 可行元器件组合
        /// </summary>
        public Component[][] ComponentGroups { get { return componentGroups; } }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public void Load(string[] configs, int index)
        {
            int n = int.Parse(configs[index++]);
            foreach (Component component in componentGroups[n])
            {
                component.Load(configs, ref index);
            }
        }

        /// <summary>
        /// 准备设计所需的参数，包括：计算电路参数，设定元器件参数
        /// </summary>
        public abstract void Prepare();

        /// <summary>
        /// 自动设计，得到每个器件的设计方案
        /// </summary>
        public void Design()
        {
            Prepare();
            foreach (Component component in components)
            {
                component.Design();
            }
        }
    }
}
