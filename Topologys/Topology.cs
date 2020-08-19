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
        protected int groupIndex; //元器件组合序号

        //电路参数
        protected double math_Pfull; //满载功率
        protected double math_P; //功率

        /// <summary>
        /// 可行元器件组合
        /// </summary>
        public Component[][] ComponentGroups { get { return componentGroups; } }

        /// <summary>
        /// 可行元件组合中出现的所有元器件（待设计的元器件）
        /// </summary>
        public Component[] Components { get { return components; } }

        /// <summary>
        /// 元器件组合序号
        /// </summary>
        public int GroupIndex { get { return groupIndex; } }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public void Load(string[] configs, int index)
        {
            groupIndex = int.Parse(configs[index++]);
            foreach (Component component in componentGroups[groupIndex])
            {
                component.Load(configs, ref index);
            }
        }

        /// <summary>
        /// 准备评估所需的电路参数
        /// </summary>
        public abstract void Prepare();

        /// <summary>
		/// 计算相应负载下的电路参数
		/// </summary>
		/// <param name="load">负载</param>
		public abstract void Calc(double load);
    }
}
