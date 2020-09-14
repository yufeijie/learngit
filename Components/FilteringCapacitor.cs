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
        /// 自动设计
        /// </summary>
        public override void Design()
        {
            //尽量使用少的器件进行设计
            for (int M = 1; M <= numberMax; M++)
            {
                numberSeriesConnected = M;
                for (int N = 1; M * N <= numberMax; N++)
                {
                    numberParallelConnected = N;
                    for (int i = 0; i < Data.CapacitorList.Count; i++) //搜寻库中所有电容型号
                    {
                        device = i; //选用当前型号电容
                        if (Validate()) //验证该电容是否可用
                        {
                            M = numberMax; //若得到设计方案，则不再考虑使用更多的器件
                            N = numberMax;
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
            if (Data.CapacitorList[device].Math_C * numberParallelConnected / numberSeriesConnected < capacitor * 1e6)
            {
                return false;
            }

            return true;
        }
    }
}
