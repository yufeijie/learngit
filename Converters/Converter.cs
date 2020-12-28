using PV_analysis.Components;
using PV_analysis.Informations;
using PV_analysis.Topologys;
using System;
using System.Collections.Generic;

namespace PV_analysis.Converters
{
    /// <summary>
    /// 变换器抽象类，用于描述变换器的共有特征
    /// </summary>
    internal abstract class Converter : Equipment
    {
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
        /// 输入电压最小值
        /// </summary>
        public double Math_Vin_min { get; set; }

        /// <summary>
        /// 输入电压最大值
        /// </summary>
        public double Math_Vin_max { get; set; }

        /// <summary>
        /// 是否对不同输入电压进行评估
        /// </summary>
        public bool IsInputVoltageVariation { get; set; }

        /// <summary>
        /// 输出电压
        /// 对于逆变单元为总输出电压，其他变换单元为单个模块输出电压
        /// </summary>
        public double Math_Vo { get; set; }

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
        public int PhaseNum { get; set; }

        /// <summary>
        /// 拓扑
        /// </summary>
        public Topology Topology { get; protected set; }

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
        /// 判断评估对象是否为架构
        /// </summary>
        /// <returns>判断结果</returns>
        public override bool IsStructure() { return false; }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public abstract string[] GetConfigs();

        /// <summary>
        /// 获取总损耗分布（元器件）
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public override List<Info> GetTotalLossBreakdown()
        {
            List<Info> list = new List<Info>();
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                List<Info> comList = component.GetLossBreakdown();
                for (int i = 0; i < comList.Count; i++)
                {
                    comList[i].Content = (double)comList[i].Content * Number;
                }
                list.AddRange(comList);
            }
            return list;
        }

        /// <summary>
        /// 获取模块损耗分布（元器件）
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public List<Info> GetModuleLossBreakdown()
        {
            List<Info> list = new List<Info>();
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
        public List<Info> GetCostBreakdown()
        {
            List<Info> list = new List<Info>();
            double costDriver = 0;
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                List<Info> comList = component.GetCostBreakdown();
                for (int i = 0; i < comList.Count; i++)
                {
                    if (comList[i].Title != null && comList[i].Title.Equals("驱动"))
                    {
                        costDriver += (double)comList[i].Content;
                    }
                    else
                    {
                        list.Add(comList[i]);
                    }
                }
            }
            list.Add(new Info("驱动&控制芯片", Math.Round(costDriver + costDSP, 2)));
            list.Add(new Info("散热器", Math.Round(costHeatsink, 2)));
            return list;
        }

        /// <summary>
        /// 获取体积分布（元器件）
        /// </summary>
        /// <returns>体积分布信息</returns>
        public List<Info> GetVolumeBreakdown()
        {
            List<Info> list = new List<Info>();
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                list.AddRange(component.GetVolumeBreakdown());
            }
            list.Add(new Info("散热器", Math.Round(volumeHeatsink, 2)));
            return list;
        }

        /// <summary>
        /// 自动设计，整合设计结果（不会覆盖之前的设计结果）
        /// </summary>
        public void Design(MainForm form)
        {
            Topology.Prepare();
            foreach (Component component in Topology.Components)
            {
                component.Design();
                //若没有设计结果，则设计失败
                if (component.DesignList.Size == 0)
                {
                    if (component.Name != null)
                    {
                        form.PrintDetails(1, component.Name + "设计失败！");
                    }
                    else
                    {
                        form.PrintDetails(1, component.GetType().Name + "设计失败！");
                    }                    
                }
            }

            for(int i = 0; i < Topology.ComponentGroups.Length; i++) //用于记录当前元器件组合的序号
            {
                Component[] components = Topology.ComponentGroups[i];
                //设计结果检查
                bool check = true;
                foreach (Component component in components)
                {
                    if (component.DesignList.Size == 0)
                    {
                        check = false;
                        break;
                    }
                }
                if (!check)
                {
                    continue;
                }

                //组合并记录
                ComponentDesignList designCombinationList = new ComponentDesignList();
                foreach (Component component in components) //组合各个器件的设计方案
                {
                    if (component.GetType().BaseType.Name.Equals("Semiconductor"))
                    {
                        designCombinationList.Combine(component.DesignList);
                        //Console.WriteLine(component.Name);
                        //IComponentDesignData[] data = component.DesignList.GetData();
                        //foreach(IComponentDesignData design in data)
                        //{
                        //    Console.Write(design.PowerLoss + " ");
                        //    Console.Write(design.Cost + " ");
                        //    Console.Write(design.Volume + ", ");
                        //    foreach (string config in design.Configs)
                        //    {
                        //        Console.Write(config + " ");
                        //    }
                        //    Console.WriteLine();
                        //}
                    }
                }
                designCombinationList.DesignAuxComponent();
                foreach (Component component in components) //组合各个器件的设计方案
                {
                    if (!component.GetType().BaseType.Name.Equals("Semiconductor"))
                    {
                        designCombinationList.Combine(component.DesignList);
                        //Console.WriteLine(component.Name);
                        //IComponentDesignData[] data = component.DesignList.GetData();
                        //foreach (IComponentDesignData design in data)
                        //{
                        //    Console.Write(design.PowerLoss + " ");
                        //    Console.Write(design.Cost + " ");
                        //    Console.Write(design.Volume + ", ");
                        //    foreach (string config in design.Configs)
                        //    {
                        //        Console.Write(config + " ");
                        //    }
                        //    Console.WriteLine();
                        //}
                    }
                }
                //TODO 控制芯片、散热器设计
                ConverterDesignList newDesignList = new ConverterDesignList();
                newDesignList.Transfer(designCombinationList, Math_Psys, Number, PhaseNum, i, GetConfigs()); //转化为变换器设计
                ParetoDesignList.Merge(newDesignList); //记录Pareto最优设计
                AllDesignList.Merge(newDesignList); //记录所有设计
            }
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        public override void Save()
        {
            Save(GetType().Name);
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="name">文件名</param>
        public override void Save(string name)
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
        public override void Save(string path, string name)
        {
            string[] conditionTitles = GetConditionTitles();
            string[] conditions = GetConditions();
            Data.Save(path, name + "_Pareto", conditionTitles, conditions, ParetoDesignList);
            Data.Save(path, name + "_all", conditionTitles, conditions, AllDesignList);
        }

        /// <summary>
        /// 评估，得到效率、体积、成本
        /// </summary>
        public override void Evaluate()
        {
            Topology.Prepare();
            Math_Peval = 0;
            Cost = 0;
            Volume = 0;
            foreach (Component component in Topology.ComponentGroups[Topology.GroupIndex])
            {
                if (component.GetType().BaseType.Name.Equals("Semiconductor"))
                {
                    component.Evaluate(); //此时调用的是父类的方法（不会调用Semiconductor中new的Evaluate方法）
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
            EfficiencyEval = 1 - Math_Peval * Number * PhaseNum / Math_Psys;
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
        public override void Operate(double load, double Vin)
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
            double Rh = (Configuration.MAX_HEATSINK_TEMPERATURE - Configuration.AMBIENT_TEMPERATURE) / Math_Peval; //此处应采用损耗最大值
            volumeHeatsink = 1 / (Configuration.CSPI * Rh);
            costHeatsink = volumeHeatsink * Configuration.HEATSINK_UNIT_PRICE;
            Volume += volumeHeatsink;
            Cost += costHeatsink;

            //评估DSP
            costDSP = Configuration.DSP_PRICE; //每个变换器模块用一个DSP
            Cost += costDSP;
        }
    }
}
