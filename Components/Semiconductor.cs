using NPOI.SS.Formula.Functions;
using PV_analysis.Informations;
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
        protected static bool isSelectSiC = Configuration.CAN_SELECT_SIC; //SiC器件选用开关，true为可选用
        protected static readonly double math_Tj_max = Configuration.MAX_JUNCTION_TEMPERATURE;//最高结温
        protected static readonly double math_Th_max = Configuration.MAX_HEATSINK_TEMPERATURE; //散热器允许最高温度

        //特殊参数
        protected bool checkZVSOn = false; //是否检查ZVS开通（默认不检查）

        //器件参数
        protected int device; //开关器件编号

        //设计条件
        //TODO 选取器件时考虑fsmax
        protected double math_Vmax; //电压应力
        protected double math_Imax; //电流应力
        protected double math_fs_max; //最大开关频率

        //成本参数（同类器件中其中一个的损耗）
        protected double semiconductorCost; //开关器件成本
        protected double driverCost; //驱动成本

        /// <summary>
        /// 获取器件型号
        /// </summary>
        /// <returns>型号</returns>
        protected string GetDeviceType()
        {
            return Data.SemiconductorList[device].Type;
        }

        /// <summary>
        /// 设置器件型号
        /// </summary>
        /// <returns>型号</returns>
        protected void SetDeviceType(string type)
        {
            for (int i = 0; i < Data.SemiconductorList.Count; i++)
            {
                if (type.Equals(Data.SemiconductorList[i].Type))
                {
                    device = i;
                    return;
                }
            }
            device = -1;
        }

        /// <summary>
        /// 获取成本分布
        /// </summary>
        /// <returns>成本分布信息</returns>
        public override List<Info> GetCostBreakdown()
        {
            List<Info> list = new List<Info>()
            {
                new Info(Name, Math.Round(number * semiconductorCost, 2)),
                new Info("驱动", Math.Round(number * driverCost, 2))
            };
            return list;
        }

        /// <summary>
        /// 获取体积分布
        /// </summary>
        /// <returns>体积分布信息</returns>
        public override List<Info> GetVolumeBreakdown()
        {
            List<Info> list = new List<Info>()
            {
                new Info(Name, Math.Round(Volume, 2))
            };
            return list;
        }

        /// <summary>
        /// 评估，得到效率、体积、成本，并进行温度检查
        /// </summary>
        /// <returns>评估结果，若温度检查不通过则返回false</returns>
        protected new bool Evaluate()
        {
            int m = Configuration.voltageRatio.Length;
            int n = Configuration.powerRatio.Length;

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
                        powerLossEvaluation += powerLoss * Configuration.powerWeight[j] / Configuration.powerRatio[j]; //计算损耗评估值
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

        /// <summary>
        /// 验证电压、电流条件是否满足
        /// </summary>
        /// <param name="paralleledNum">并联数量</param>
        /// <returns></returns>
        protected bool ValidateVoltageAndCurrent(int paralleledNum = 1)
        {
            double kV = Properties.Settings.Default.开关器件电压裕量;
            double kI = Properties.Settings.Default.开关器件电流裕量;

            //电压应力检查
            if (Data.SemiconductorList[device].Math_Vmax * (1 - kV) < math_Vmax) return false;

            //电流应力检查
            if (paralleledNum * Data.SemiconductorList[device].Math_Imax * (1 - kI) < math_Imax) return false;

            //容量过剩检查
            if (Configuration.CAN_CHECK_SEMICONDUCTOR_EXCESS)
            {
                //电压容量过剩检查
                if (Data.SemiconductorList[device].Math_Vmax * (1 - kV) > math_Vmax * (1 + Configuration.SEMICONDUCTOR_VOLTAGE_EXCESS_RATIO)) return false;

                //电流容量过剩检查
                if (paralleledNum * Data.SemiconductorList[device].Math_Imax * (1 - kI) > math_Imax * (1 + Configuration.SEMICONDUCTOR_CURRENT_EXCESS_RATIO)) return false;
            }
            return true;
        }
    }
}
