using PV_analysis.Informations;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    /// <summary>
    /// 元器件，设计统一
    /// </summary>
    internal abstract class Component
    {
        //特殊参数
        protected bool frequencyVariable = false; //开关频率是否变化（默认不变）

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
        /// 名称
        /// </summary>
        public string Name { get; set; }

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
        /// 获取设计方案的配置信息标题
        /// </summary>
        /// <returns>配置信息标题</returns>
        public abstract string[] GetConfigTitles();

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public abstract string[] GetConfigs();

        /// <summary>
        /// 获取设计方案的配置信息（包括标题）
        /// </summary>
        /// <returns>配置信息</returns>
        public List<Info> GetConfigInfo()
        {
            List<Info> list = new List<Info>();
            string[] titles = GetConfigTitles();
            string[] configs = GetConfigs();
            for (int i = 0; i < titles.Length; i++)
            {
                list.Add(new Info(titles[i], configs[i]));
            }
            return list;
        }

        /// <summary>
        /// 获取手动设计信息
        /// </summary>
        /// <returns>手动设计信息</returns>
        public abstract List<(MainForm.ControlType, string)> GetManualInfo();

        /// <summary>
        /// 获取损耗分布
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public abstract List<Info> GetLossBreakdown();

        /// <summary>
        /// 获取成本分布
        /// </summary>
        /// <returns>成本分布信息</returns>
        public abstract List<Info> GetCostBreakdown();

        /// <summary>
        /// 获取体积分布
        /// </summary>
        /// <returns>体积分布信息</returns>
        public abstract List<Info> GetVolumeBreakdown();

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public abstract void Load(string[] configs, ref int index);

        /// <summary>
        /// 自动设计，得到设计方案
        /// </summary>
        public abstract void Design();

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        protected abstract void SelectParameters(int m, int n);

        /// <summary>
        /// 评估，得到效率、体积、成本
        /// </summary>
        public void Evaluate()
        {
            int m = Configuration.voltageRatio.Length;
            int n = Configuration.powerRatio.Length;

            if (!VoltageVariable) //输入电压不变
            {
                m = 1;
            }

            powerLossEvaluation = 0;
            for (int i = 0; i < m; i++) //对不同输入电压进行计算
            {
                for (int j = n - 1; j >= 0; j--) //对不同功率点进行计算
                {
                    SelectParameters(i, j); //设置对应条件下的电路参数
                    CalcPowerLoss(); //计算对应条件下的损耗
                    if (PowerVariable)
                    {
                        powerLossEvaluation += powerLoss * Configuration.powerWeight[j] / Configuration.powerRatio[j]; //计算损耗评估值
                    }
                    else //若负载不变，则只评估满载
                    {
                        powerLossEvaluation = powerLoss;
                        break;
                    }
                }
            }
            powerLossEvaluation /= m;

            CalcVolume();
            CalcCost();
        }

        /// <summary>
        /// 计算损耗
        /// </summary>
        public abstract void CalcPowerLoss();

        /// <summary>
        /// 计算体积
        /// </summary>
        protected abstract void CalcVolume();

        /// <summary>
        /// 计算成本
        /// </summary>
        protected abstract void CalcCost();
    }
}
