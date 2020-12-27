using System;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    internal class ResonantCapacitor : Capacitor
    {
        List<int> deviceGroup;

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
                if (!string.IsNullOrEmpty(Properties.Settings.Default.电容型号1))
                {
                    deviceGroup.Add(GetDeviceId(Properties.Settings.Default.电容型号1));
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.电容型号2))
                {
                    deviceGroup.Add(GetDeviceId(Properties.Settings.Default.电容型号2));
                }
                if (!string.IsNullOrEmpty(Properties.Settings.Default.电容型号3))
                {
                    deviceGroup.Add(GetDeviceId(Properties.Settings.Default.电容型号3));
                }
                device = deviceGroup.ToArray();
                seriesConnectedNumber = Properties.Settings.Default.电容串联数;
                parallelConnectedNumber = Properties.Settings.Default.电容并联数;
                Evaluate();
                designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                return;
            }
            GroupDesign(0);
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
                    C += Data.CapacitorList[i].Math_C;
                }
                //容值过大时，不使用当前型号
                if (!ok || (C / 1e6 > capacitor && Math.Abs(C / 1e6 - capacitor) / capacitor > 0.05))
                {
                    continue;
                }
                deviceGroup.Add(i); //添加当前型号

                //继续组合
                if (C / 1e6 < capacitor && deviceGroup.Count < Properties.Settings.Default.电容不同型号数量上限)
                {
                    GroupDesign(i + 1);
                }

                //考虑组合后的串并联
                int maxNumber = Properties.Settings.Default.电容总个数上限;
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
                            Evaluate();
                            designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
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

            double C = 0;
            foreach (int id in device)
            {
                C += Data.CapacitorList[id].Math_C;
            }

            //容值检查
            if (Math.Abs(C * parallelConnectedNumber / seriesConnectedNumber - capacitor * 1e6) / (capacitor * 1e6) > 0.05)
            {
                return false;
            }

            return true;
        }
    }
}
