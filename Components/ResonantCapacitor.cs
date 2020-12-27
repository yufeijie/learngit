using System;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    internal class ResonantCapacitor : Capacitor
    {
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
            for (int i = 0; i < Data.CapacitorList.Count; i++) //搜寻库中所有电容型号
            {
                device = i; //选用当前型号电容
                int numberSeriesConnectedMin = (int)Math.Ceiling(voltageMax / (Data.CapacitorList[device].Math_Un) * (1 - Properties.Settings.Default.电容电压裕量));
                int numberParallelConnectedMin = (int)Math.Ceiling(currentRMSMax / (Data.CapacitorList[device].Math_Irms) * (1 - Properties.Settings.Default.电容电流裕量));
                for (int M = numberSeriesConnectedMin; M <= maxNumber; M++)
                {
                    for (int N = numberParallelConnectedMin; M * N <= maxNumber; N++)
                    {
                        seriesConnectedNumber = M;
                        parallelConnectedNumber = N;
                        if (Validate()) //验证该电容是否可用
                        {
                            Evaluate();
                            designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                        }
                    }
                }
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

            //容值检查
            if (Math.Abs(Data.CapacitorList[device].Math_C * parallelConnectedNumber / seriesConnectedNumber - capacitor * 1e6) / (capacitor * 1e6) > 0.05)
            {
                return false;
            }

            return true;
        }
    }
}
