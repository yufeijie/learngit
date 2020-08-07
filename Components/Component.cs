using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Components
{
    internal abstract class Component
    {
        //基本参数
        protected int number; //同类器件数量

        //损耗参数（同类器件中其中一个的损耗）
        protected double powerLoss; //单个器件损耗(W)
        protected double powerLossEvaluation; //单个器件损耗评估值(W)

        //成本参数（同类器件中其中一个的损耗）
        protected double cost; //单个器件成本(RMB)

        //体积参数（同类器件中其中一个的损耗）
        protected double volume; //单个器件体积(dm^3)

        //设计结果
        protected ComponentDesignList designList = new ComponentDesignList(); //TODO 封装

        /// <summary>
        /// 评估时，输入电压是否变化
        /// </summary>
        public bool VoltageVariable { get; set; } = true;

        /// <summary>
        /// 评估时，负载是否变化
        /// </summary>
        public bool PowerVariable { get; set; } = true;

        /// <summary>
        /// 损耗评估值
        /// </summary>
        public double Math_Peval { get { return number * powerLossEvaluation; } }

        /// <summary>
        /// 总损耗
        /// </summary>
        public double PowerLoss { get { return number * powerLoss; } }

        /// <summary>
        /// 总成本
        /// </summary>
        public double Cost { get { return number * cost; } }

        /// <summary>
        /// 总体积
        /// </summary>
        public double Volume { get { return number * volume; } }

        /// <summary>
        /// 设计结果
        /// </summary>
        public ComponentDesignList DesignList { get { return designList; } }

        /// <summary>
        /// 自动设计，得到设计方案
        /// </summary>
        public abstract void Design();
    }
}
