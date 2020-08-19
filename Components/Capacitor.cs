using System;

namespace PV_analysis.Components
{
    internal class Capacitor : Component
    {
        //限制条件
        private static readonly bool isCheckExcess = false; //是否检查过剩容量
        private static readonly double excess = 1; //允许过剩容量
        private double margin = 0.1; //裕量
        private int numberMax = 20; //最大器件数

        //器件参数
        private int device; //电容编号
        private int numberSeriesConnected; //串联数量
        private int numberParallelConnected; //并联数量

        //设计条件
        private double capacitor; //电容值
        private double voltageMax; //电压最大值
        private double currentRMSMax; //电流有效值最大值

        //电路参数
        private double currentRMS; //电容电流有效值
        private double[,] currentRMSForEvaluation = new double[5, 7]; //电容电流有效值（用于评估）

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
        private void SetDeviceType(string type)
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
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public string[] GetConfigs()
        {
            string[] data = { number.ToString(), GetDeviceType(), numberSeriesConnected.ToString(), numberParallelConnected.ToString() };
            return data;
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
        /// <param name="currentRMS">电容电流有效值（用于评估）	</param>
        public void AddEvalParameters(int m, int n, double currentRMS)
        {
            currentRMSForEvaluation[m, n] = currentRMS;
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
        public override void Design()
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
                            M = numberMax; //对于同种电容，只允许一个可行设计方案
                            N = numberMax;
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
            int m = Config.CGC_VOLTAGE_RATIO.Length;
            int n = Config.CGC_POWER_RATIO.Length;

            if (!VoltageVariable) //输入电压不变
            {
                m = 1;
            }

            for (int i = 0; i < m; i++) //对不同输入电压进行计算
            {
                for (int j = n - 1; j >= 0; j--) //对不同功率点进行计算
                {
                    SelectParameters(i, j); //设置对应条件下的电路参数
                    CalcPowerLoss(); //计算对应条件下的损耗
                    if (PowerVariable)
                    {
                        powerLossEvaluation += powerLoss * Config.CGC_POWER_WEIGHT[j] / Config.CGC_POWER_RATIO[j]; //计算损耗评估值
                    }
                    else //若负载不变，则只评估满载
                    {
                        powerLossEvaluation = powerLoss;
                        break;
                    }
                }
            }
            powerLossEvaluation /= m;
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

            //验证电压电流应力、容值是否满足
            if (Data.CapacitorList[device].Math_Un * (1 - margin) * numberSeriesConnected < voltageMax
                || Data.CapacitorList[device].Math_C * numberParallelConnected / numberSeriesConnected < capacitor * 1e6
                || Data.CapacitorList[device].Math_Irms * (1 - margin) * numberParallelConnected < currentRMSMax)
            {
                return false;
            }

            //容量过剩检查
            if (isCheckExcess && (Data.CapacitorList[device].Math_Un * (1 - margin) * numberSeriesConnected > voltageMax * (1 + excess)
                || Data.CapacitorList[device].Math_Irms * (1 - margin) * numberParallelConnected > currentRMSMax * (1 + excess)))
            {
                return false;
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
