using System;

namespace PV_analysis.Components
{
    //CHB专用，可用半桥与全桥IGBT模块
    internal class CHBModule : Semiconductor
    {
        //限制条件
        private static readonly bool isCheckTemperature = true; //在评估时是否进行温度检查
        private static readonly bool isCheckExcess = false; //是否检查过剩容量
        private static readonly double excess = 1; //允许过剩容量
        private static bool selectSiC = true; //SiC器件选用开关，true为可选用
        private static readonly double margin = 0.2; //裕量

        //器件参数
        private int device; //开关器件编号

        //设计条件
        //TODO 选取器件时考虑fsmax
        private double math_Vmax; //电压应力
        private double math_Imax; //电流应力
        private double math_fs_max; //最大开关频率

        //电路参数
        private double cycleTime; //开关周期
        private double frequencyGrid; //工频
        private double voltage; //开通/关断电压
        private int cycleNumber; //一个工频周期内的开关周期总数
        private double[,][] timeTurnOnIgbt; //IGBT开通时间
        private double[,][] timeTurnOnDiode; //二极管开通时间
        private double[] currentOutput; //输出电流波形
        private double[,][] currentOutputForEvaluation = new double[5, 7][]; //输出电流波形（用于评估）

        //损耗参数（同类器件中其中一个的损耗）
        private double[,] math_PTcon; //主管通态损耗
        private double[,] math_Pon; //主管开通损耗
        private double[,] math_Poff; //主管关断损耗
        private double[,] math_PDcon; //反并二极管通态损耗
        private double[,] math_Prr; //反并二极管反向恢复损耗

        //成本参数（同类器件中其中一个的损耗）
        private double semiconductorCost; //开关器件成本
        private double driverCost; //驱动成本

        //温度参数(℃)
        private static readonly double math_Th_max = 60; //散热器允许最高温度
        private static readonly double math_Tj_max = 110;//最高结温

        /// <summary>
        /// CHB模块数
        /// </summary>
        public int MultiNumber { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类开关器件数量</param>
        public CHBModule(int number)
        {
            this.number = number;
        }

        /// <summary>
        /// 获取器件的型号
        /// </summary>
        /// <returns>型号</returns>
        private string GetDeviceType()
        {
            return Data.SemiconductorList[device].Type;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        private string[] GetConfigs()
        {
            return new string[] { "CHBModule", number.ToString(), GetDeviceType() };
        }

        /// <summary>
        /// 设置设计条件
        /// </summary>
        /// <param name="Vmax">电压应力</param>
        /// <param name="Imax">电流应力</param>
        /// <param name="fs_max">最大开关频率</param>
        public void SetConditions(double Vmax, double Imax, double fs_max)
        {
            math_Vmax = Vmax;
            math_Imax = Imax;
            math_fs_max = fs_max;
            cycleTime = 1 / math_fs_max;
        }

        /// <summary>
        /// 添加一组不变的电路参数
        /// </summary>
        /// <param name="frequencyGrid">工频</param>
        /// <param name="voltage">开关电压</param>
        /// <param name="cycleNumber">一个工频周期内的开关周期总数</param>
        /// <param name="timeTurnOnIgbt">IGBT开通时间</param>
        /// <param name="timeTurnOnDiode">二极管开通时间</param>
        public void AddParameters(double frequencyGrid, double voltage, int cycleNumber, double[,][] timeTurnOnIgbt, double[,][] timeTurnOnDiode)
        {
            this.frequencyGrid = frequencyGrid;
            this.voltage = voltage;
            this.cycleNumber = cycleNumber;
            this.timeTurnOnIgbt = timeTurnOnIgbt;
            this.timeTurnOnDiode = timeTurnOnDiode;
        }

        /// <summary>
        /// 添加电路参数（损耗不均衡）（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="current">电流</param>
        public void AddEvalParameters(int m, int n, double[] current)
        {
            currentOutputForEvaluation[m, n] = current;
        }

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        private void SelectParameters(int m, int n)
        {
            currentOutput = currentOutputForEvaluation[m, n];
        }

        /// <summary>
        /// 自动设计
        /// </summary>
        public override void Design()
        {
            for (int i = 0; i < Data.SemiconductorList.Count; i++) //搜寻库中所有开关器件型号
            {
                device = i; //选用当前型号器件
                if (Validate()) //验证该器件是否可用
                {
                    if (Evaluate()) //损耗评估，并进行温度检查
                    {
                        CalcVolume(); //计算体积
                        CalcCost(); //计算成本
                        designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                    }
                }
            }
        }

        /// <summary>
        /// 损耗评估，并进行温度检查
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <returns>评估结果，若温度检查不通过则返回false</returns>
        private bool Evaluate()
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
                    if (isCheckTemperature && !CheckTemperature()) //验证散热器温度
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
            return true;
        }

        /// <summary>
        /// 验证开关器件的型号、电压、电流等是否满足要求
        /// </summary>
        /// <returns>验证结果，true为满足</returns>
        private bool Validate()
        {
            //验证编号是否合法
            if (device < 0 || device >= Data.SemiconductorList.Count)
            {
                return false;
            }

            //验证器件是否可用
            if (!Data.SemiconductorList[device].Available)
            {
                return false;
            }

            //验证器件类型是否符合
            if (!Data.SemiconductorList[device].Category.Equals("IGBT-Module") && !Data.SemiconductorList[device].Category.Equals("SiC-Module"))
            {
                return false;
            }

            //验证器件结构是否符合
            if (!Data.SemiconductorList[device].Configuration.Equals("Dual") && !Data.SemiconductorList[device].Configuration.Equals("Fourpack"))
            {
                return false;
            }

            //验证SiC器件的选用是否符合限制条件
            if ((Data.SemiconductorList[device].Category.Equals("SiC-Module")) && (!selectSiC || math_fs_max < 50e3))
            {
                return false;
            }

            //验证电压、电流应力是否满足
            if (Data.SemiconductorList[device].Math_Vmax * (1 - margin) < math_Vmax || Data.SemiconductorList[device].Math_Imax * (1 - margin) < math_Imax)
            {
                return false;
            }

            //容量过剩检查
            if (isCheckExcess && (Data.SemiconductorList[device].Math_Vmax * (1 - margin) > math_Vmax * (1 + excess) || Data.SemiconductorList[device].Math_Imax * (1 - margin) > math_Imax * (1 + excess)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        private void CalcCost()
        {
            switch (Data.SemiconductorList[device].Configuration)
            {
                case "Dual":
                    semiconductorCost = 2 * Data.SemiconductorList[device].Price;
                    break;
                case "Fourpack":
                    semiconductorCost = Data.SemiconductorList[device].Price;
                    break;
            }
            //TODO 驱动需要不同
            driverCost = 4 * 31.4253; //IX2120B IXYS MOQ100 Mouser
            cost = semiconductorCost + driverCost;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        private void CalcVolume()
        {
            switch (Data.SemiconductorList[device].Configuration)
            {
                case "Dual":
                    volume = 2 * Data.SemiconductorList[device].Volume;
                    break;
                case "Fourpack":
                    volume = Data.SemiconductorList[device].Volume;
                    break;
            }
        }

        /// <summary>
        /// 计算损耗 TODO 未考虑MOSFET反向导通
        /// </summary>
        private void CalcPowerLoss()
        {
            math_PTcon = new double[MultiNumber, 4];
            math_Pon = new double[MultiNumber, 4];
            math_Poff = new double[MultiNumber, 4];
            math_PDcon = new double[MultiNumber, 4];
            math_Prr = new double[MultiNumber, 4];
            powerLoss = 0;

            for (int i = 0; i < MultiNumber; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int id;
                    if (Data.SemiconductorList[device].Category.Equals("SiC-Module"))
                    {
                        id = Data.SemiconductorList[device].Id_Vds;
                    }
                    else
                    {
                        id = Data.SemiconductorList[device].Id_Vce;
                    }
                    double ETcon = 0;
                    for (int k = 0; k < cycleNumber; k++)
                    {
                        ETcon += Math.Abs(currentOutput[k]) * Data.CurveList[id].GetValue(Math.Abs(currentOutput[k])) * timeTurnOnIgbt[i, j][k];
                    }
                    math_PTcon[i, j] = ETcon * frequencyGrid;

                    id = Data.SemiconductorList[device].Id_Eon;
                    double Eon = 0;
                    for (int k = 0; k < cycleNumber; k++)
                    {
                        int l = (k + cycleNumber - 1) % cycleNumber;
                        if ((timeTurnOnIgbt[i, j][k] > 0 && timeTurnOnIgbt[i, j][k] < cycleTime && timeTurnOnIgbt[i, j][l] < cycleTime) ||
                           (timeTurnOnIgbt[i, j][k] == 0 && timeTurnOnIgbt[i, j][l] == cycleTime))
                        {
                            Eon += voltage / Data.CurveList[id].Math_Vsw * Data.CurveList[id].GetValue(Math.Abs(currentOutput[k])) * 1e-3;
                        }
                    }
                    math_Pon[i, j] = Eon * frequencyGrid;

                    id = Data.SemiconductorList[device].Id_Eoff;
                    double Eoff = 0;
                    for (int k = 0; k < cycleNumber; k++)
                    {
                        int r = (k + 1) % cycleNumber;
                        if ((timeTurnOnIgbt[i, j][k] > 0 && timeTurnOnIgbt[i, j][k] < cycleTime && timeTurnOnIgbt[i, j][r] < cycleTime) ||
                           (timeTurnOnIgbt[i, j][k] == cycleTime && timeTurnOnIgbt[i, j][r] == 0))
                        {
                            Eoff += voltage / Data.CurveList[id].Math_Vsw * Data.CurveList[id].GetValue(Math.Abs(currentOutput[k])) * 1e-3;
                        }
                    }
                    math_Poff[i, j] = Eoff * frequencyGrid;

                    id = Data.SemiconductorList[device].Id_Vf;
                    double EDcon = 0;
                    for (int k = 0; k < cycleNumber; k++)
                    {
                        EDcon += Math.Abs(currentOutput[k]) * Data.CurveList[id].GetValue(Math.Abs(currentOutput[k])) * timeTurnOnDiode[i, j][k];
                    }
                    math_PDcon[i, j] = EDcon * frequencyGrid;

                    id = Data.SemiconductorList[device].Id_Err;
                    double Err = 0;
                    for (int k = 0; k < cycleNumber; k++)
                    {
                        int r = (k + 1) % cycleNumber;
                        if ((timeTurnOnDiode[i, j][k] > 0 && timeTurnOnDiode[i, j][k] < cycleTime && timeTurnOnDiode[i, j][r] < cycleTime) ||
                           (timeTurnOnDiode[i, j][k] == cycleTime && timeTurnOnDiode[i, j][r] == 0))
                        {
                            Err += voltage / Data.CurveList[id].Math_Vsw * Data.CurveList[id].GetValue(Math.Abs(currentOutput[k])) * 1e-3;
                        }
                    }
                    math_Prr[i, j] = Err * frequencyGrid;
                    powerLoss += math_PTcon[i, j] + math_Pon[i, j] + math_Poff[i, j] + math_PDcon[i, j] + math_Prr[i, j];
                }
            }

            powerLoss /= MultiNumber;
        }

        /// <summary>
        /// 验证温度
        /// </summary>
        /// <returns>是否验证通过</returns>
        private bool CheckTemperature()
        {
            for (int i = 0; i < MultiNumber; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    //计算工作在最大结温时的散热器温度
                    double Pmain = math_PTcon[i, j] + math_Pon[i, j] + math_Poff[i, j];
                    double Pdiode = math_PDcon[i, j] + math_Prr[i, j];
                    double Tc;
                    if (Data.SemiconductorList[device].Category.Equals("SiC-Module"))
                    {
                        Tc = math_Tj_max - Math.Max(Pmain * Data.SemiconductorList[device].MOSFET_RthJC, Pdiode * Data.SemiconductorList[device].Diode_RthJC);
                    }
                    else
                    {
                        Tc = math_Tj_max - Math.Max(Pmain * Data.SemiconductorList[device].IGBT_RthJC, Pdiode * Data.SemiconductorList[device].Diode_RthJC);
                    }
                    double Th = Tc - (Pmain + Pdiode) * Data.SemiconductorList[device].Module_RthCH;
                    if (Th < math_Th_max) //若此时的散热器温度低于允许温度，则不通过
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
