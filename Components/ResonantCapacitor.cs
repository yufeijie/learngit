using System;
using System.Collections.Generic;
using static PV_analysis.ComponentDesignList;

namespace PV_analysis.Components
{
    /// <summary>
    /// 谐振电容
    /// </summary>
    internal class ResonantCapacitor : Capacitor
    {
        List<int> deviceGroup;
        int[] designDevice;
        int designS;
        int designP;
        double designC;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类电容数量</param>
        public ResonantCapacitor(int number) : base(number) { }

        /// <summary>
        /// 自动设计
        /// </summary>
        public override void Design()
        {
            deviceGroup = new List<int>();
            if (Properties.Settings.Default.给定谐振电容)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.谐振电容型号1))
                {
                    deviceGroup.Add(GetDeviceId(Properties.Settings.Default.谐振电容型号1));
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.谐振电容型号2))
                {
                    deviceGroup.Add(GetDeviceId(Properties.Settings.Default.谐振电容型号2));
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.谐振电容型号3))
                {
                    deviceGroup.Add(GetDeviceId(Properties.Settings.Default.谐振电容型号3));
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.谐振电容型号4))
                {
                    deviceGroup.Add(GetDeviceId(Properties.Settings.Default.谐振电容型号4));
                }
                device = deviceGroup.ToArray();
                seriesConnectedNumber = Properties.Settings.Default.谐振电容串联数;
                parallelConnectedNumber = Properties.Settings.Default.谐振电容并联数;
                Evaluate();
                designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                return;
            }
            designC = -1;
            GroupDesign(0);
            if (!Configuration.CAN_OPTIMIZE_RESONANT_CAPACITOR)
            {
                if (designC != -1)
                {
                    device = designDevice;
                    seriesConnectedNumber = designS;
                    parallelConnectedNumber = designP;
                    Evaluate();
                    designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                }
            }
        }

        /// <summary>
        /// 组合设计（串联）
        /// </summary>
        /// <param name="n">第n个型号</param>
        /// <param name="startId">从器件库的哪个id开始（避免型号重复）</param>
        private void GroupDesign(int startId)
        {
            for (int i = startId; i < Data.CapacitorList.Count; i++) //搜寻库中所有电容型号
            {
                //验证电容类型
                if (Data.CapacitorList[i].Category != "STD")
                {
                    continue;
                }
                string category = Data.CapacitorList[i].Category;
                double Un = Data.CapacitorList[i].Math_Un;
                double Irms = Data.CapacitorList[i].Math_Irms;
                double C = Data.CapacitorList[i].Math_C;
                bool ok = true;
                foreach (int id in deviceGroup)
                {
                    //只允许同类同电压等级的器件组合使用
                    if (!Data.CapacitorList[id].Category.Equals(category) || Data.CapacitorList[id].Math_Un != Un)
                    {
                        ok = false;
                        break;
                    }
                    Irms += Data.CapacitorList[id].Math_Irms;
                    C += Data.CapacitorList[id].Math_C;
                }
                //只允许同类同电压等级的器件组合使用
                if (!ok)
                {
                    continue;
                }
                deviceGroup.Add(i); //添加当前型号

                //继续组合
                if (deviceGroup.Count < Properties.Settings.Default.电容不同型号数量上限)
                {
                    GroupDesign(i + 1);
                }

                //考虑组合后的串并联
                int maxNumber = Properties.Settings.Default.电容个数上限;
                int numberSeriesConnectedMin = (int)Math.Ceiling(voltageMax * (1 - Properties.Settings.Default.电容电压裕量) / Un);
                int numberParallelConnectedMin = (int)Math.Ceiling(currentRMSMax * (1 - Properties.Settings.Default.电容电流裕量) / Irms);
                for (int M = numberSeriesConnectedMin; M * deviceGroup.Count <= maxNumber; M++)
                {
                    for (int N = numberParallelConnectedMin; M * N * deviceGroup.Count <= maxNumber; N++)
                    {
                        device = deviceGroup.ToArray();
                        seriesConnectedNumber = M;
                        parallelConnectedNumber = N;
                        if (Validate()) //验证该电容是否可用
                        {
                            if (!Configuration.CAN_OPTIMIZE_RESONANT_CAPACITOR)
                            {
                                //若不优化谐振电容，则仅保留与所需容值最接近的设计
                                if (designC == -1 || Math.Abs(C * N / M - capacitor * 1e6) < Math.Abs(designC - capacitor * 1e6))
                                {
                                    designC = C * N / M;
                                    designDevice = device;
                                    designS = M;
                                    designP = N;
                                }
                            }
                            else
                            {
                                Evaluate();
                                designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                            }
                        }
                    }
                }
                deviceGroup.Remove(i);
            }
        }

        /// <summary>
        /// 验证电容的编号、电压、容值、电流是否满足要求
        /// </summary>
        /// <returns>验证结果，true为满足</returns>
        public new bool Validate()
        {
            if (!base.Validate())
            {
                return false;
            }
            
            double Urms = -1;
            double C = 0;
            foreach (int id in device)
            {
                C += Data.CapacitorList[id].Math_C;
                //验证不同型号的交流电压有效值是否相同
                if (Urms != -1 && Urms != Data.CapacitorList[id].Math_Urms)
                {
                    return false;
                }
                Urms = Data.CapacitorList[id].Math_Urms;
            }

            double kv = Properties.Settings.Default.电容电压裕量;
            //验证交流电压有效值是否满足
            if (Urms * (1 - kv) * Math.Sqrt(2) * seriesConnectedNumber < voltageMax)
            {
                return false;
            }

            //容值检查
            //if (Math.Abs(C * parallelConnectedNumber / seriesConnectedNumber - capacitor * 1e6) / (capacitor * 1e6) > 0.05) //相对误差小于等于5%
            if (Math.Abs(C * parallelConnectedNumber / seriesConnectedNumber - capacitor * 1e6) > 0.022) //绝对误差小于等于22nF（目前容值最小的器件）
            {
                return false;
            }

            return true;
        }
    }
}
