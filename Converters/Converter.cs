using PV_analysis.Components;
using PV_analysis.Topologys;
using System;

namespace PV_analysis.Converters
{
    /// <summary>
    /// 变换器抽象类，用于描述变换器的共有特征
    /// </summary>
    internal abstract class Converter
    {
        //优化与评估
        protected bool isEvaluatedAtDiffInputVoltage = true; //是否对不同输入电压进行评估
        protected static readonly bool isRecordResult = true; //是否记录单级变换器评估结果

        //---基本参数---
        protected string name = null; //变换器名
        protected short stage = 0; //第几级变换器

        /// <summary>
        /// 系统功率
        /// </summary>
        public double Math_Psys { get; set; }

        /// <summary>
        /// 开关频率
        /// </summary>
        public double Math_fs { get; set; }

        /// <summary>
        /// 模块数
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// 相数(单相or三相)
        /// </summary>
        public int PhaseNum { get; set; } = 1;

        /// <summary>
        /// 拓扑
        /// </summary>
        public Topology Topology { get; set; }

        /// <summary>
        /// 中国效率
        /// </summary>
        public double EfficiencyCGC { get; private set; }

        /// <summary>
        /// 成本
        /// </summary>
        public double Cost { get; private set; }

        /// <summary>
        /// 体积
        /// </summary>
        public double Volume { get; private set; }

        /// <summary>
        /// 效率
        /// </summary>
        public double Efficiency { get; private set; }

        /// <summary>
        /// Pareto最优设计方案
        /// </summary>
        public ConverterDesignList ParetoDesignList { get; } = new ConverterDesignList();

        /// <summary>
        /// 所有设计方案
        /// </summary>
        public ConverterDesignList AllDesignList { get; } = new ConverterDesignList { IsAll = true };

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public abstract string[] GetConfigs();

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>配置信息</returns>
        protected abstract string[] GetConditionTitles();

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>配置信息</returns>
        protected abstract string[] GetConditions();

        /// <summary>
        /// 自动设计，整合设计结果（不会覆盖之前的设计结果）
        /// </summary>
        public void Design()
        {
            Topology.Prepare();
            foreach (Component component in Topology.Components)
            {
                component.Design();
                //若没有设计结果，则设计失败，退出
                if (component.DesignList.Size == 0)
                {
                    Console.WriteLine(component.GetType().Name + " design Failed");
                    break;
                }
            }
            int n = 0; //用于记录当前元器件组合的序号
            foreach (Component[] components in Topology.ComponentGroups)
            {
                //组合并记录
                ComponentDesignList designCombinationList = new ComponentDesignList();
                foreach (Component component in components) //组合各个器件的设计方案
                {
                    designCombinationList.Combine(component.DesignList);
                }
                //TODO 控制芯片、散热器设计
                ConverterDesignList newDesignList = new ConverterDesignList();
                newDesignList.Transfer(designCombinationList, Math_Psys, Number, PhaseNum, n, GetConfigs()); //转化为变换器设计
                ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                AllDesignList.Merge(newDesignList); //记录所有设计
                n++;
            }
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        public void Save()
        {
            string[] conditionTitles = GetConditionTitles();
            string[] conditions = GetConditions();
            Data.Save(GetType().Name + "_Pareto", conditionTitles, conditions, ParetoDesignList);
            Data.Save(GetType().Name + "_all", conditionTitles, conditions, AllDesignList);
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public abstract void Load(string[] configs, int index);

        /// <summary>
        /// 评估，得到中国效率、体积、成本
        /// </summary>
        public void Evaluate()
        {
            Topology.Prepare();
            double Pevel = 0;
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                component.Evaluate();
                Pevel += component.Math_Peval;
                Cost += component.Cost;
                Volume += component.Volume;
            }
            EfficiencyCGC = 1 - Pevel * Number * PhaseNum / Math_Psys;
            Cost *= Number * PhaseNum;
            Volume *= Number * PhaseNum;
        }

        /// <summary>
        /// 模拟变换器运行，得到相应负载下的效率
        /// </summary>
        /// <param name="load">负载</param>
        public void Operate(double load = 1.00)
        {
            Topology.Calc(load);
            double Ploss = 0;
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                component.CalcPowerLoss();
                Ploss += component.PowerLoss;
            }
            Efficiency = 1 - Ploss * Number * PhaseNum / Math_Psys;
        }
    }
}
