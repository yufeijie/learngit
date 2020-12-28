using System;

namespace PV_analysis.Components
{
    internal class FilteringCapacitor : Capacitor
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类电容数量</param>
        public FilteringCapacitor(int number) : base(number) { }

        /// <summary>
        /// 自动设计，滤波电感只考虑用同一种型号
        /// </summary>
        public override void Design()
        {
            if (Properties.Settings.Default.给定滤波电容)
            {
                device = new int[] { GetDeviceId(Properties.Settings.Default.滤波电容型号) };
                seriesConnectedNumber = Properties.Settings.Default.滤波电容串联数;
                parallelConnectedNumber = Properties.Settings.Default.滤波电容并联数;
                Evaluate();
                designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                return;
            }

            int maxNumber = Properties.Settings.Default.电容个数上限;
            //尽量使用少的器件进行设计
            for (int M = 1; M <= maxNumber; M++)
            {
                seriesConnectedNumber = M;
                for (int N = 1; M * N <= maxNumber; N++)
                {
                    parallelConnectedNumber = N;
                    for (int i = 0; i < Data.CapacitorList.Count; i++) //搜寻库中所有电容型号
                    {
                        device = new int[] { i }; //选用当前型号电容
                        if (Validate()) //验证该电容是否可用
                        {
                            Evaluate();
                            designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                            return; //若得到设计方案，则不再考虑其他设计
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
            if (Data.CapacitorList[device[0]].Math_C * parallelConnectedNumber / seriesConnectedNumber < capacitor * 1e6)
            {
                return false;
            }

            return true;
        }
    }
}
