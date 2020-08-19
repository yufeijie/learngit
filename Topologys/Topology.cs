using PV_analysis.Components;
using PV_analysis.Converters;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// 拓扑抽象类，用于描述拓扑的共有特征
    /// </summary>
    internal abstract class Topology
    {
        protected Component[][] componentGroups; //可行元器件组合
        protected Component[] components; //可行元件组合中出现的所有元器件（待设计的元器件）

        /// <summary>
        /// 可行元器件组合
        /// </summary>
        public Component[][] ComponentGroups { get { return componentGroups; } }

        /// <summary>
        /// 自动设计，得到每个器件的设计方案
        /// </summary>
        public abstract void Design();
    }
}
