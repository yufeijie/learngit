﻿using System;

namespace PV_analysis.Components
{
    internal class Capacitor : IComponent
    {
        //限制条件
        private double margin = 0.1; //裕量
        private int numberMax = 20; //最大器件数

        //基本参数
        private int number; //同类电容数量

        //器件参数
        private int device; //电容编号
        private int numberSeriesConnected; //串联数量
        private int numberParallelConnected; //并联数量

        //设计结果
        private ComponentDesignList designList = new ComponentDesignList();

        //设计条件
        private double capacitor; //电容值
        private double voltageMax; //电压最大值
        private double currentRMSMax; //电流有效值最大值

        //电路参数
        private double currentRMS; //电容电流有效值
        private double[,] currentRMSForEvaluation; //电容电流有效值（用于评估）	

        //损耗参数（同类中一个开关器件的损耗）
        private double powerLoss; //单个电容损耗
        private double powerLossEvaluation; //单个电容损耗评估值

        //成本参数（同类中一个开关器件的损耗）
        private double cost; //单个电容成本

        //体积参数（同类中一个开关器件的损耗）
        private double volume; //单个电容体积(dm^3)

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
        public String GetDeviceType()
        {
            return Data.CapacitorList[device].Type;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns></returns>
        public String[] GetConfigs()
        {
            String[] data = { "Filter", GetDeviceType(), numberSeriesConnected.ToString(), numberParallelConnected.ToString() };
            return data;
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
        /// 设置电路参数（用于评估）
        /// </summary>
        /// <param name="currentRMS">电容电流有效值（用于评估）	</param>
        public void SetEvalParameters(double[,] currentRMS)
        {
            currentRMSForEvaluation = currentRMS;
        }

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        public void SelectParameters(int m, int n)
        {
            currentRMS = currentRMSForEvaluation[m, n];
        }

        /// <summary>
        /// 自动设计
        /// </summary>
        public void Design()
        {
            for (int i = 0; i < Data.CapacitorList.Count; i++) //搜寻库中所有电容型号
            {
                device = i; //选用当前型号电容
                int numberSeriesConnectedMin = (int)Math.Ceiling(voltageMax / (Data.CapacitorList[device].Math_Un) * (1 - margin));
                int numberParallelConnectedMin = (int)Math.Ceiling(currentRMSMax / (Data.CapacitorList[device].Math_Irms) * (1 - margin));
                for (int M = numberSeriesConnectedMin; M <= numberMax; M++)
                {
                    for (int N = numberParallelConnectedMin; M * N <= numberMax; N++)
                    {
                        numberSeriesConnected = M;
                        numberParallelConnected = N;
                        if (Validate()) //验证该电容是否可用
                        {
                            Evaluate();
                            CalcVolume();
                            CalcCost();
                            designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 损耗评估
        /// </summary>
        private void Evaluate()
        {
            for (int m = 0; m < Config.CGC_VOLTAGE_RATIO.Length; m++) //对不同输入电压进行计算
            {
                for (int n = 0; n < Config.CGC_POWER_RATIO.Length; n++) //对不同功率点进行计算
                {
                    SelectParameters(m, n);
                    powerLossEvaluation += powerLoss * Config.CGC_POWER_WEIGHT[n] / Config.CGC_POWER_RATIO[n]; //计算损耗评估值
                }
            }
            powerLossEvaluation /= Config.CGC_VOLTAGE_RATIO.Length;
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
            else
            {
                //验证当前参数组的参数是否已经给定
                if (voltageMax == 0 || capacitor == 0)
                {
                    return false;
                }
                else
                {
                    if (Data.CapacitorList[device].Math_Un * (1 - margin) * numberSeriesConnected < voltageMax
                        || Data.CapacitorList[device].Math_C * numberParallelConnected / numberSeriesConnected < capacitor * 1e6
                        || Data.CapacitorList[device].Math_Irms * (1 - margin) * numberParallelConnected < currentRMSMax)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        public void CalcCost()
        {
            cost = numberSeriesConnected * numberParallelConnected * Data.CapacitorList[device].Price;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        public void CalcVolume()
        {
            volume = numberSeriesConnected * numberParallelConnected * Data.CapacitorList[device].Volume;
        }

        /// <summary>
        /// 计算损耗
        /// </summary>
        public void CalcPowerLoss()
        {
            double ESR = Data.CapacitorList[device].Math_ESR * 1e-3; //获取等效串联电阻
            powerLoss = Math.Pow(currentRMS, 2) * ESR * numberSeriesConnected / numberParallelConnected;
        }
    }
}
