using PV_analysis.Informations;
using System;
using System.Collections.Generic;
using static PV_analysis.Curve;

namespace PV_analysis.Components
{
    internal class Single : Semiconductor
    {
        //限制条件
        //private bool isSoftOff = false; //是否以软关断的方式进行计算(仅在符合条件时为true)
        private static readonly int paralleledNumMax = Configuration.MAX_SEMICONDUCTOR_NUM; //最大并联数

        //器件参数
        private int paralleledNum; //并联数量

        //电路参数
        private double math_Vsw; //开通/关断电压
        private Curve curve_i; //电流波形
        private double math_fs; //开关频率        
        private double[,] math_Vsw_eval = new double[5, 7]; //开通/关断电压（用于评估）
        private Curve[,] curve_i_eval = new Curve[5, 7]; //电流波形（用于评估）
        private double[,] math_fs_eval = new double[5, 7]; //开关频率（用于评估）

        //损耗参数（同类器件中其中一个的损耗）
        private double math_PTcon; //主管通态损耗
        private double math_Pon; //主管开通损耗
        private double math_Poff; //主管关断损耗
        private double math_PDcon; //反并二极管通态损耗
        private double math_Prr; //反并二极管反向恢复损耗

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类开关器件数量</param>
        public Single(int number)
        {
            this.number = number;
        }

        /// <summary>
        /// 获取器件型号
        /// </summary>
        /// <returns>型号</returns>
        private string GetDeviceType()
        {
            return Data.SemiconductorList[device].Type;
        }

        /// <summary>
        /// 设置器件型号
        /// </summary>
        /// <returns>型号</returns>
        private void SetDeviceType(string type)
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
        /// 获取设计方案的配置信息标题
        /// </summary>
        /// <returns>配置信息标题</returns>
        public override string[] GetConfigTitles()
        {
            string[] data = { "同类器件数量", "型号", "并联数" };
            return data;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            return new string[] { number.ToString(), GetDeviceType(), paralleledNum.ToString() };
        }

        /// <summary>
        /// 获取手动设计信息
        /// </summary>
        /// <returns>手动设计信息</returns>
        public override List<(MainForm.ControlType, string)> GetManualInfo()
        {
            List<(MainForm.ControlType, string)> list = new List<(MainForm.ControlType, string)>()
            {
                (MainForm.ControlType.Semiconductor, "型号"),
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
            List<Info> list = new List<Info>()
            {
                new Info(Name + "(PTcon)", Math.Round(number * math_PTcon, 2)),
                new Info(Name + "(Pon)", Math.Round(number * math_Pon, 2)),
                new Info(Name + "(Poff)", Math.Round(number * math_Poff, 2)),
                new Info(Name + "(PDcon)", Math.Round(number * math_PDcon, 2)),
                new Info(Name + "(Prr)", Math.Round(number * math_Prr, 2))
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
            SetDeviceType(configs[index++]);
            paralleledNum = int.Parse(configs[index++]);
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
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="Vsw">开关电压</param>
        /// <param name="i">电流波形</param>
        public void AddEvalParameters(int m, int n, double Vsw, Curve i)
        {
            math_Vsw_eval[m, n] = Vsw;
            curve_i_eval[m, n] = i;
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="Vsw">开关电压</param>
        /// <param name="i">电流波形</param>
        /// <param name="fs">开关频率</param>
        public void AddEvalParameters(int m, int n, double Vsw, Curve i, double fs)
        {
            math_Vsw_eval[m, n] = Vsw;
            curve_i_eval[m, n] = i;
            frequencyVariable = true;
            math_fs_eval[m, n] = fs;
        }

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        protected override void SelectParameters(int m, int n)
        {
            math_Vsw = math_Vsw_eval[m, n];
            curve_i = curve_i_eval[m, n];
            if (frequencyVariable)
            {
                math_fs = math_fs_eval[m, n];
            }
            else
            {
                math_fs = math_fs_max;
            }
        }

        /// <summary>
        /// 设置电路参数
        /// </summary>
        /// <param name="Vsw">开关电压</param>
        /// <param name="i">电流波形</param>
        /// <param name="fs">开关频率</param>
        public void SetParameters(double Vsw, Curve i, double fs)
        {
            math_Vsw = Vsw;
            curve_i = i;
            math_fs = fs;
        }

        /// <summary>
        /// 自动设计
        /// </summary>
        public override void Design()
        {
            for (int j = 1; j <= paralleledNumMax; j++)
            {
                paralleledNum = j;
                for (int i = 0; i < Data.SemiconductorList.Count; i++) //搜寻库中所有开关器件型号
                {
                    device = i; //选用当前型号器件
                    if (Validate()) //验证该器件是否可用
                    {
                        if (Evaluate()) //损耗评估，并进行温度检查
                        {
                            designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 验证开关器件的型号、电压、电流等是否满足要求
        /// </summary>
        /// <returns>验证结果，true为满足</returns>
        private bool Validate()
        {
            //验证编号是否合法
            if (device < 0 || device >= Data.SemiconductorList.Count) return false;

            //验证器件是否可用
            if (!Data.SemiconductorList[device].Available) return false;

            //验证并联数目是否合法
            if (paralleledNum <= 0 || paralleledNum > paralleledNumMax) return false;

            //验证器件类型是否符合
            if (!Data.SemiconductorList[device].Category.Equals("SiC-MOSFET")) return false;

            //验证器件结构是否符合
            if (!Data.SemiconductorList[device].Configuration.Equals("Single")) return false;

            //验证电压、电流条件是否满足
            if (!ValidateVoltageAndCurrent(paralleledNum)) return false;

            return true;
        }

        /// <summary>
        /// 计算损耗 TODO 未考虑MOSFET反向导通
        /// </summary>
        public override void CalcPowerLoss()
        {
            CalcPowerLoss_MOSFET(curve_i, out math_PTcon, out math_Pon, out math_Poff, out math_PDcon, out math_Prr);
            powerLoss = math_PTcon + math_Pon + math_Poff + math_PDcon + math_Prr;
        }

        /// <summary>
        /// 计算MOSFET单管损耗
        /// </summary>
        /// <param name="curve">电流曲线</param>
        /// <param name="PTcon">主管通态损耗</param>
        /// <param name="Pon">主管开通损耗</param>
        /// <param name="Poff">主管关断损耗</param>
        /// <param name="PDcon">反并二极管通态损耗</param>
        /// <param name="Prr">反并二极管反向恢复损耗</param>
        private void CalcPowerLoss_MOSFET(Curve curve, out double PTcon, out double Pon, out double Poff, out double PDcon, out double Prr)
        {
            //TODO 未考虑同步整流
            PTcon = 0;
            Pon = 0;
            Poff = 0;
            PDcon = 0;
            Prr = 0;
            Point[] data = curve.GetData();
            for (int i = 1; i < data.Length; i++)
            {
                double t1 = data[i - 1].X;
                double i1 = data[i - 1].Y;
                double t2 = data[i].X;
                double i2 = data[i].Y;
                if (Function.EQ(i1, 0) && Function.EQ(i2, 0)) //两点电流都为0时无损耗
                {
                    continue;
                }
                else if (Function.EQ(t1, t2)) //t1=t2时，可能有开关损耗，没有通态损耗
                {
                    if (Function.LE(i1, 0) && Function.BigEnough(i1) && Function.GT(i2, 0)) //i1<=0、i2>0时，计算主管开通损耗
                    {
                        Pon += CalcPon_MOSFET(i2);
                    }
                    if (Function.GT(i1, 0) && Function.BigEnough(i1) && Function.LE(i2, 0)) //i1>0、i2<=0时，计算主管关断损耗
                    {
                        Poff += CalcPoff_MOSFET(i1);
                    }
                    if (Function.LT(i1, 0) && Function.BigEnough(i1) && Function.GE(i2, 0)) //i1<0、i2>=0时，计算反并二极管反向恢复损耗
                    {
                        Prr += CalcPrr_MOSFET(i1);
                    }
                }
                else //t1≠t2时，只有通态损耗
                {
                    if (Function.GE(i1, 0) && Function.GE(i2, 0)) //电流都不为负时，为主管通态损耗
                    {
                        PTcon += CalcPTcon_MOSFET(t1, i1, t2, i2); //计算主管通态损耗
                    }
                    else if (Function.LE(i1, 0) && Function.LE(i2, 0)) //电流都不为正时，为反并二极管通态损耗
                    {
                        PDcon += CalcPDcon_MOSFET(t1, i1, t2, i2); //计算反并二极管通态损耗
                    }
                    else //电流一正一负时，既包含主管通态损耗，又包含反并二极管通态损耗
                    {
                        double z = (i1 * t2 - i2 * t1) / (i1 - i2); //计算过零点
                        if (i1 > 0) //i1>0时，主管先为导通状态
                        {
                            PTcon += CalcPTcon_MOSFET(t1, i1, z, 0);
                            PDcon += CalcPDcon_MOSFET(z, 0, t2, i2);
                        }
                        else //否则i2>0，反并二极管先为导通状态
                        {
                            PDcon += CalcPDcon_MOSFET(t1, i1, z, 0);
                            PTcon += CalcPTcon_MOSFET(z, 0, t2, i2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 计算主管通态损耗（MOSFET单管）
        /// </summary>
        /// <param name="t1">左时刻</param>
        /// <param name="i1">左电流</param>
        /// <param name="t2">右时刻</param>
        /// <param name="i2">右电流</param>
        /// <returns>计算结果</returns>
        private double CalcPTcon_MOSFET(double t1, double i1, double t2, double i2)
        {
            double energy = Function.IntegrateTwoLinear(t1, i1, i1, t2, i2, i2) * Data.SemiconductorList[device].Math_Rdson * 1e-3 / paralleledNum; //计算导通能耗
            return energy * math_fs;
        }

        /// <summary>
        /// 计算主管开通损耗（MOSFET单管）
        /// </summary>
        /// <param name="Ion">开通电流</param>
        /// <returns>计算结果</returns>
        private double CalcPon_MOSFET(double Ion)
        {
            //TODO MOSFET Pon
            if (Function.EQ(Ion, 0))
            {
                return 0;
            }
            Console.WriteLine("MOSFET Pon error!");
            System.Environment.Exit(-1);
            return 0;
        }

        /// <summary>
        /// 计算主管关断损耗（MOSFET单管）
        /// </summary>
        /// <param name="Ioff">关断电流</param>
        /// <returns>计算结果</returns>
        private double CalcPoff_MOSFET(double Ioff)
        {
            //根据关断电流查表得到对应损耗
            if (Function.EQ(Ioff, 0))
            {
                return 0;
            }
            Ioff /= paralleledNum; //TODO 注意这里，多管并联时，电流和寄生电容的计算

            double Vth = Data.SemiconductorList[device].Math_Vth;
            double gm = Data.SemiconductorList[device].Math_gfs;
            double Ciss = Data.SemiconductorList[device].Math_Ciss * 1e-12;
            double Coss = Data.SemiconductorList[device].Math_Coss * 1e-12;
            double Crss = Data.SemiconductorList[device].Math_Crss * 1e-12;
            double Rg = Data.SemiconductorList[device].Math_Rg;
            double Cgd = Crss;
            double Cds = (Coss - Crss) * paralleledNum;
            double tf = Rg * Ciss * Math.Log((Ioff / gm + Vth) / Vth);
            double t2 = ((1 + gm * Rg) * Cgd + Cds) / (gm * Vth + Ioff) * math_Vsw;
            double e1 = tf * math_Vsw * 0.5 * Ioff;
            double e2 = t2 * math_Vsw * 0.5 * Ioff;
            return paralleledNum * math_fs * (e1 + e2);
        }

        /// <summary>
        /// 计算反并二极管通态损耗（MOSFET单管）
        /// </summary>
        /// <param name="t1">左时刻</param>
        /// <param name="i1">左电流</param>
        /// <param name="t2">右时刻</param>
        /// <param name="i2">右电流</param>
        /// <returns>计算结果</returns>
        private double CalcPDcon_MOSFET(double t1, double i1, double t2, double i2)
        {
            //采用牛顿-莱布尼茨公式进行电压电流积分的优化计算
            i1 = (i1 >= 0 ? i1 : -i1) / paralleledNum;
            i2 = (i2 >= 0 ? i2 : -i2) / paralleledNum;
            int id = Data.SemiconductorList[device].Id_Vf;
            double u1 = Data.CurveList[id].GetValue(i1); //获取左边界电流对应的导通压降
            double u2 = Data.CurveList[id].GetValue(i2); //获取右边界电流对应的导通压降
            double energy = Function.IntegrateTwoLinear(t1, i1, u1, t2, i2, u2) * paralleledNum; //计算导通能耗
            return energy * math_fs;
        }

        /// <summary>
        /// 计算反并二极管反向恢复损耗（MOSFET单管）
        /// </summary>
        /// <param name="Ioff">关断电流</param>
        /// <returns>计算结果</returns>
        private double CalcPrr_MOSFET(double Ioff)
        {
            //TODO MOSFET Prr
            if (Function.EQ(Ioff, 0))
            {
                return 0;
            }
            Console.WriteLine("MOSFET Prr error!");
            System.Environment.Exit(-1);
            return 0;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        protected override void CalcCost()
        {
            semiconductorCost = paralleledNum * Data.SemiconductorList[device].Price;
            //TODO 驱动需要不同
            driverCost = paralleledNum * 31.4253; //IX2120B IXYS MOQ100 Mouser
            cost = semiconductorCost + driverCost;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        protected override void CalcVolume()
        {
            volume = paralleledNum * Data.SemiconductorList[device].Volume;
        }

        /// <summary>
        /// 验证温度
        /// </summary>
        /// <returns>是否验证通过</returns>
        protected override bool CheckTemperature()
        {
            //计算工作在最大结温时的散热器温度
            double P = (math_PTcon + math_Pon + math_Poff + math_PDcon + math_Prr) / paralleledNum;
            double Tc;
            Tc = math_Tj_max - P * Data.SemiconductorList[device].MOSFET_RthJC;
            double Th = Tc - P * Data.SemiconductorList[device].MOSFET_RthCH;
            if (Th < math_Th_max) //若此时的散热器温度低于允许温度，则不通过
            {
                return false;
            }
            return true;
        }
    }
}
