
using PV_analysis.Informations;
using System;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    internal abstract class Capacitor : Component
    {
        //限制条件
        protected static readonly double margin = 0.1; //裕量
        protected static readonly int numberMax = 20; //最大器件数

        //器件参数
        protected int device; //电容编号
        protected int numberSeriesConnected; //串联数量
        protected int numberParallelConnected; //并联数量

        //设计条件
        protected double capacitor; //电容值
        protected double voltageMax; //电压最大值
        protected double currentRMSMax; //电流有效值最大值

        //电路参数
        protected double currentRMS; //电容电流有效值
        protected double[,] currentRMSForEvaluation = new double[5, 7]; //电容电流有效值（用于评估）

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类电容数量</param>
        public Capacitor(int number)
        {
            this.number = number;
        }

        /// <summary>
        /// 获取电容型号
        /// </summary>
        /// <returns>型号</returns>
        public string GetDeviceType()
        {
            return Data.CapacitorList[device].Type;
        }

        /// <summary>
        /// 设置电容型号
        /// </summary>
        /// <returns>型号</returns>
        protected void SetDeviceType(string type)
        {
            for (int i = 0; i < Data.CapacitorList.Count; i++)
            {
                if (type.Equals(Data.CapacitorList[i].Type))
                {
                    device = i;
                    return;
                }
            }
            device = -1;
        }

        /// <summary>
        /// 获取设计方案的配置信息标题
        /// </summary>
        /// <returns>配置信息标题</returns>
        public override string[] GetConfigTitles()
        {
            string[] data = { "同类器件数量", "型号", "串联数", "并联数" };
            return data;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            string[] data = { number.ToString(), GetDeviceType(), numberSeriesConnected.ToString(), numberParallelConnected.ToString() };
            return data;
        }

        /// <summary>
        /// 获取损耗分布
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public override List<Info> GetLossBreakdown()
        {
            List<Info> list = new List<Info>
            {
                new Info(Name, Math.Round(PowerLoss, 2))
            };
            return list;
        }

        /// <summary>
        /// 获取成本分布
        /// </summary>
        /// <returns>成本分布信息</returns>
        public override List<Info> GetCostBreakdown()
        {
            List<Info> list = new List<Info>
            {
                new Info(Name, Math.Round(Cost, 2))
            };
            return list;
        }

        /// <summary>
        /// 获取体积分布
        /// </summary>
        /// <returns>体积分布信息</returns>
        public override List<Info> GetVolumeBreakdown()
        {
            List<Info> list = new List<Info>
            {
                new Info(Name, Math.Round(Volume, 2))
            };
            return list;
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public override void Load(string[] configs, ref int index)
        {
            number = int.Parse(configs[index++]);
            SetDeviceType(configs[index++]);
            numberSeriesConnected = int.Parse(configs[index++]);
            numberParallelConnected = int.Parse(configs[index++]);
        }

        /// <summary>
        /// 设置设计条件
        /// </summary>
        /// <param name="capacitor">电压最大值</param>
        /// <param name="voltageMax">电容值</param>
        /// <param name="currentRMSMax">电流有效值最大值</param>
        public void SetConditions(double capacitor, double voltageMax, double currentRMSMax)
        {
            this.capacitor = capacitor;
            this.voltageMax = voltageMax;
            this.currentRMSMax = currentRMSMax;
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="currentRMS">电容电流有效值</param>
        public void AddEvalParameters(int m, int n, double currentRMS)
        {
            currentRMSForEvaluation[m, n] = currentRMS;
        }

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        protected override void SelectParameters(int m, int n)
        {
            currentRMS = currentRMSForEvaluation[m, n];
        }

        /// <summary>
        /// 设置电路参数
        /// </summary>
        /// <param name="currentRMS">电容电流有效值</param>
        public void SetParameters(double currentRMS)
        {
            this.currentRMS = currentRMS;
        }

        /// <summary>
        /// 验证电容的编号、电压、容值、电流是否满足要求
        /// </summary>
        /// <returns>验证结果，true为满足</returns>
        public bool Validate()
        {
            //验证编号是否合法
            if (device < 0 || device >= Data.CapacitorList.Count)
            {
                return false;
            }

            //验证器件是否可用
            if (!Data.CapacitorList[device].Available)
            {
                return false;
            }

            //验证当前参数组的参数是否已经给定
            if (voltageMax == 0 || capacitor == 0)
            {
                return false;
            }

            //验证电压电流应力是否满足
            if (Data.CapacitorList[device].Math_Un * (1 - margin) * numberSeriesConnected < voltageMax
                || Data.CapacitorList[device].Math_Irms * (1 - margin) * numberParallelConnected < currentRMSMax)
            {
                return false;
            }

            //容量过剩检查
            if (isCheckExcess)
            {
                if (Data.CapacitorList[device].Math_Un * (1 - margin) * numberSeriesConnected > voltageMax * (1 + excess)
                    || Data.CapacitorList[device].Math_Irms * (1 - margin) * numberParallelConnected > currentRMSMax * (1 + excess)
                    )
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 计算损耗
        /// </summary>
        public override void CalcPowerLoss()
        {
            double ESR = Data.CapacitorList[device].Math_ESR * 1e-3; //获取等效串联电阻
            powerLoss = Math.Pow(currentRMS, 2) * ESR * numberSeriesConnected / numberParallelConnected;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        protected override void CalcCost()
        {
            cost = numberSeriesConnected * numberParallelConnected * Data.CapacitorList[device].Price;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        protected override void CalcVolume()
        {
            volume = numberSeriesConnected * numberParallelConnected * Data.CapacitorList[device].Volume;
        }
    }
}
