using NPOI.SS.Formula.Functions;
using PV_analysis.Components;
using PV_analysis.Topologys;
using System;
using System.Collections.Generic;

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

        //散热器参数（单个模块）
        private double costHeatsink;
        private double volumeHeatsink;
        private double costDSP;

        /// <summary>
        /// 系统功率
        /// </summary>
        public double Math_Psys { get; set; }

        /// <summary>
        /// 模块功率
        /// </summary>
        public double Math_P { get; set; }

        /// <summary>
        /// 输入电压
        /// </summary>
        public double Math_Vin { get; set; }

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
        public double EfficiencyCGC { get; protected set; }

        /// <summary>
        /// 损耗评估值
        /// </summary>
        public double Math_Peval { get; protected set; }

        /// <summary>
        /// 损耗
        /// </summary>
        public double PowerLoss { get; protected set; }

        /// <summary>
        /// 成本
        /// </summary>
        public double Cost { get; protected set; }

        /// <summary>
        /// 体积
        /// </summary>
        public double Volume { get; protected set; }

        /// <summary>
        /// 效率
        /// </summary>
        public double Efficiency { get; protected set; }

        /// <summary>
        /// Pareto最优设计方案
        /// </summary>
        public ConverterDesignList ParetoDesignList { get; } = new ConverterDesignList();

        /// <summary>
        /// 所有设计方案
        /// </summary>
        public ConverterDesignList AllDesignList { get; } = new ConverterDesignList { IsAll = true };

        /// <summary>
        /// 获取变换单元名
        /// </summary>
        /// <returns>变换单元名</returns>
        public abstract string GetName();

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
        /// 获取损耗分布（元器件）
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public List<Item> GetLossBreakdown()
        {
            List<Item> list = new List<Item>();
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                list.AddRange(component.GetLossBreakdown());
            }            
            return list;
        }

        /// <summary>
        /// 获取成本分布（元器件）
        /// </summary>
        /// <returns>成本分布信息</returns>
        public List<Item> GetCostBreakdown()
        {
            List<Item> listAll = new List<Item>();
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                listAll.AddRange(component.GetCostBreakdown());
            }
            //整合驱动成本
            List<Item> list = new List<Item>();
            double costDriver = 0;
            foreach (Item item in listAll)
            {
                if (item.Name != null && item.Name.Equals("驱动"))
                {
                    costDriver += item.Value;
                }
                else
                {
                    list.Add(item);
                }
            }
            list.Add(new Item("驱动&控制芯片", Math.Round(costDriver + costDSP, 2)));
            list.Add(new Item("散热器", Math.Round(costHeatsink, 2)));
            return list;
        }

        /// <summary>
        /// 获取体积分布（元器件）
        /// </summary>
        /// <returns>体积分布信息</returns>
        public List<Item> GetVolumeBreakdown()
        {
            List<Item> list = new List<Item>();
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                list.AddRange(component.GetVolumeBreakdown());
            }
            list.Add(new Item("散热器", Math.Round(volumeHeatsink, 2)));
            return list;
        }

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public abstract void Optimize();

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
                    if (component.Name != null)
                    {
                        Console.WriteLine(component.Name + "设计失败！");
                    }
                    else
                    {
                        Console.WriteLine(component.GetType().Name + "设计失败！");
                    }
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
                    if (component.GetType().BaseType.Name.Equals("Semiconductor"))
                    {
                        designCombinationList.Combine(component.DesignList);
                    }
                }
                designCombinationList.DesignAuxComponent();
                foreach (Component component in components) //组合各个器件的设计方案
                {
                    if (!component.GetType().BaseType.Name.Equals("Semiconductor"))
                    {
                        designCombinationList.Combine(component.DesignList);
                    }
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
            Save(GetType().Name);
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="name">文件名</param>
        public void Save(string name)
        {
            string[] conditionTitles = GetConditionTitles();
            string[] conditions = GetConditions();
            Data.Save(name + "_Pareto", conditionTitles, conditions, ParetoDesignList);
            Data.Save(name + "_all", conditionTitles, conditions, AllDesignList);
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="name">文件名</param>
        public void Save(string path, string name)
        {
            string[] conditionTitles = GetConditionTitles();
            string[] conditions = GetConditions();
            Data.Save(path, name + "_Pareto", conditionTitles, conditions, ParetoDesignList);
            Data.Save(path, name + "_all", conditionTitles, conditions, AllDesignList);
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public abstract void Load(string[] configs, ref int index);

        /// <summary>
        /// 评估，得到中国效率、体积、成本
        /// </summary>
        public void Evaluate()
        {
            Topology.Prepare();
            Math_Peval = 0;
            Cost = 0;
            Volume = 0;
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                if (component.GetType().BaseType.Name.Equals("Semiconductor"))
                {
                    component.Evaluate(); //此时调用的是父类的方法
                    Math_Peval += component.Math_Peval;
                    Cost += component.Cost;
                    Volume += component.Volume;
                }
            }
            DesignAuxComponent();
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                if (!component.GetType().BaseType.Name.Equals("Semiconductor"))
                {
                    component.Evaluate();
                    Math_Peval += component.Math_Peval;
                    Cost += component.Cost;
                    Volume += component.Volume;
                }
            }
            EfficiencyCGC = 1 - Math_Peval * Number * PhaseNum / Math_Psys;
            Cost *= Number * PhaseNum;
            Volume *= Number * PhaseNum;
        }
        
        /// <summary>
        /// 模拟变换器运行，得到相应负载下的效率
        /// </summary>
        /// <param name="load">负载</param>
        public void Operate(double load)
        {
            Math_P = Math_Psys / PhaseNum / Number * load;
            Topology.Calc();
            PowerLoss = 0;
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                component.CalcPowerLoss();
                PowerLoss += component.PowerLoss;
            }
            PowerLoss *= Number * PhaseNum;
            Efficiency = 1 - PowerLoss / (Math_Psys * load);
        }

        /// <summary>
        /// 模拟变换器运行，得到相应负载、输入电压下的效率
        /// </summary>
        /// <param name="load">负载</param>
        public void Operate(double load, double Vin)
        {
            Math_Vin = Vin;
            Operate(load);
        }

        /// <summary>
        /// 设计散热器
        /// 设计DSP
        /// </summary>
        public void DesignAuxComponent()
        {
            //评估散热器
            double Rh = (Config.MAX_HEATSINK_TEMPERATURE - Config.AMBIENT_TEMPERATURE) / Math_Peval; //此处应采用损耗最大值
            volumeHeatsink = 1 / (Config.CSPI * Rh);
            costHeatsink = volumeHeatsink * Config.HEATSINK_UNIT_PRICE;
            Volume += volumeHeatsink;
            Cost += costHeatsink;

            //评估DSP
            costDSP = 157.296; //每个变换器模块用一个DSP，型号：TMS320F28335PGFA TI 100 Mouser FIXM
            Cost += costDSP;
        }
    }
}
