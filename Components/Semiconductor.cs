using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Components
{
    internal abstract class Semiconductor : Component
    {
        //限制条件
        protected static bool selectSiC = true; //SiC器件选用开关，true为可选用
        protected static readonly double margin = 0.2; //裕量

        /// <summary>
        /// 评估，得到中国效率、体积、成本，并进行温度检查
        /// </summary>
        /// <returns>评估结果，若温度检查不通过则返回false</returns>
        protected new bool Evaluate()
        {
            int m = Config.CGC_VOLTAGE_RATIO.Length;
            int n = Config.CGC_POWER_RATIO.Length;

            if (!VoltageVariable) //输入电压不变
            {
                m = 1;
            }

            powerLossEvaluation = 0;
            for (int i = 0; i < m; i++) //对不同输入电压进行计算
            {
                for (int j = n - 1; j >= 0; j--) //对不同功率点进行计算
                {
                    SelectParameters(i, j); //设置对应条件下的电路参数
                    CalcPowerLoss(); //计算对应条件下的损耗
                    if (!CheckTemperature()) //验证散热器温度
                    {
                        return false;
                    }
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

            CalcVolume();
            CalcCost();
            return true;
        }

        /// <summary>
        /// 验证温度
        /// </summary>
        /// <returns>是否验证通过</returns>
        protected abstract bool CheckTemperature();
    }
}
