
using PV_analysis.Informations;
using System;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    /// <summary>
    /// 电容抽象类
    /// </summary>
    internal abstract class Capacitor : Component
    {
        //器件参数
        protected int[] device; //电容编号
        protected int seriesConnectedNumber; //串联数量
        protected int parallelConnectedNumber; //并联数量

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
        /// 获取电容编号
        /// </summary>
        /// <returns>编号</returns>
        protected int GetDeviceId(string type)
        {
            for (int i = 0; i < Data.CapacitorList.Count; i++)
            {
                if (type.Equals(Data.CapacitorList[i].Type))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取电容型号
        /// </summary>
        /// <returns>型号</returns>
        public string GetDeviceType(int id)
        {
            return Data.CapacitorList[id].Type;
        }

        /// <summary>
        /// 获取设计方案的配置信息标题
        /// </summary>
        /// <returns>配置信息标题</returns>
        public override string[] GetConfigTitles()
        {
            List<string> data = new List<string>();
            data.Add("同类器件数量");
            data.Add("不同型号个数");
            for (int i = 1; i <= device.Length; i++)
            {
                data.Add("型号"+i);
            }
            data.Add("串联数");
            data.Add("并联数");
            return data.ToArray();
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            List<string> data = new List<string>();
            data.Add(number.ToString());
            data.Add(device.Length.ToString());
            for (int i = 0; i < device.Length; i++)
            {
                data.Add(GetDeviceType(device[i]));
            }
            data.Add(seriesConnectedNumber.ToString());
            data.Add(parallelConnectedNumber.ToString());
            return data.ToArray();
        }

        /// <summary>
        /// 获取手动设计信息
        /// </summary>
        /// <returns>手动设计信息</returns>
        public override List<(MainForm.ControlType, string)> GetManualInfo()
        {
            List<(MainForm.ControlType, string)> list = new List<(MainForm.ControlType, string)>()
            {
                (MainForm.ControlType.Capacitor, "型号"),
                (MainForm.ControlType.Text, "串联数"),
                (MainForm.ControlType.Text, "并联数"),
            };
            return list;
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
            int n = int.Parse(configs[index++]);
            device = new int[n];
            for (int i = 0; i < n; i++)
            {
                device[i] = GetDeviceId(configs[index++]);
            }
            seriesConnectedNumber = int.Parse(configs[index++]);
            parallelConnectedNumber = int.Parse(configs[index++]);
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
            //验证当前参数组的参数是否已经给定
            if (voltageMax == 0 || capacitor == 0)
            {
                return false;
            }

            double Un = -1;
            double Irms = 0;
            foreach (int id in device)
            {
                //验证编号是否合法
                if (id < 0 || id >= Data.CapacitorList.Count)
                {
                    return false;
                }

                //验证器件是否可用
                if (!Data.CapacitorList[id].Available)
                {
                    return false;
                }

                //验证不同型号的耐压是否相同
                if (Un != -1 && Un != Data.CapacitorList[id].Math_Un)
                {
                    return false;
                }
                Un = Data.CapacitorList[id].Math_Un;
                Irms += Data.CapacitorList[id].Math_Irms;
            }

            double kv = Properties.Settings.Default.电容电压裕量;
            double ki = Properties.Settings.Default.电容电流裕量;
            //验证电压电流应力是否满足
            if (Un * (1 - kv) * seriesConnectedNumber < voltageMax
                || Irms * (1 - ki) * parallelConnectedNumber < currentRMSMax)
            {
                return false;
            }

            //容量过剩检查
            if (Configuration.CAN_CHECK_CAPACITOR_EXCESS)
            {
                if (Un * (1 - kv) * seriesConnectedNumber > voltageMax * (1 + Configuration.CAPACITOR_VOLTAGE_EXCESS_RATIO)
                    || Irms * (1 - ki) * parallelConnectedNumber > currentRMSMax * (1 + Configuration.CAPACITOR_CURRENT_EXCESS_RATIO)
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
            double G = 0;
            foreach (int id in device)
            {
                G += 1 / Data.CapacitorList[id].Math_ESR;
            }
            double ESR = 1 / G * 1e-3; //获取等效串联电阻
            powerLoss = Math.Pow(currentRMS, 2) * ESR * seriesConnectedNumber / parallelConnectedNumber;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        protected override void CalcCost()
        {
            double c = 0;
            foreach (int id in device)
            {
                c += Data.CapacitorList[id].Price;
            }
            cost = seriesConnectedNumber * parallelConnectedNumber * c;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        protected override void CalcVolume()
        {
            double v = 0;
            foreach (int id in device)
            {
                v += Data.CapacitorList[id].Volume;
            }
            volume = seriesConnectedNumber * parallelConnectedNumber * v;
        }
    }
}
