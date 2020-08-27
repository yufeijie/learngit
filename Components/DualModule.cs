using System;
using System.Collections.Generic;
using static PV_analysis.Curve;

namespace PV_analysis.Components
{
    internal class DualModule : Semiconductor
    {
        //器件参数
        private int device; //开关器件编号
        private bool isPowerLossBalance; //损耗是否均衡，若均衡则只需计算上管的损耗，默认为均衡

        //设计条件
        //TODO 选取器件时考虑fsmax
        private double math_Vmax; //电压应力
        private double math_Imax; //电流应力
        private double math_fs_max; //最大开关频率

        //电路参数
        private double math_Vsw; //开通/关断电压
        private Curve curve_iUp; //上管电流波形
        private Curve curve_iDown; //下管电流波形
        private double math_fs; //开关频率        
        private double[,] math_Vsw_eval = new double[5, 7]; //开通/关断电压（用于评估）
        private Curve[,] curve_iUp_eval = new Curve[5, 7]; //上管电流波形（用于评估）
        private Curve[,] curve_iDown_eval = new Curve[5, 7]; //下管电流波形（用于评估）
        private double[,] math_fs_eval = new double[5, 7]; //开关频率（用于评估）

        //损耗参数（同类器件中其中一个的损耗）
        private double[] math_PTcon; //主管通态损耗
        private double[] math_Pon; //主管开通损耗
        private double[] math_Poff; //主管关断损耗
        private double[] math_PDcon; //反并二极管通态损耗
        private double[] math_Prr; //反并二极管反向恢复损耗

        //成本参数（同类器件中其中一个的损耗）
        private double semiconductorCost; //开关器件成本
        private double driverCost; //驱动成本

        //温度参数(℃)
        private static readonly double math_Th_max = 60; //散热器允许最高温度
        private static readonly double math_Tj_max = 110;//最高结温

        /// <summary>
        /// 上管名称
        /// </summary>
        public string Name_Up { get; set; }

        /// <summary>
        /// 下管名称
        /// </summary>
        public string Name_Down { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类开关器件数量</param>
        /// <param name="isPowerLossBalance">损耗是否均衡，默认均衡</param>
        public DualModule(int number, bool isPowerLossBalance = true)
        {
            this.number = number;
            this.isPowerLossBalance = isPowerLossBalance;
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
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        private string[] GetConfigs()
        {
            return new string[] { number.ToString(), GetDeviceType() };
        }

        /// <summary>
        /// 获取损耗分布
        /// </summary>
        public override List<Item> GetLossBreakdown()
        {
            string name_Up;
            string name_Down;
            if (Name == null)
            {
                name_Up = Name_Up;
                name_Down = Name_Down;
            }
            else
            {
                name_Up = Name;
                name_Down = Name;
            }
            List<Item> lossList = new List<Item>();
            if (isPowerLossBalance)
            {
                lossList.Add(new Item(name_Up + "(PTcon)", number * math_PTcon[0] * 2));
                lossList.Add(new Item(name_Up + "(Pon)", number * math_Pon[0] * 2));
                lossList.Add(new Item(name_Up + "(Poff)", number * math_Poff[0] * 2));
                lossList.Add(new Item(name_Up + "(PDcon)", number * math_PDcon[0] * 2));
                lossList.Add(new Item(name_Up + "(Prr)", number * math_Prr[0] * 2));
            }
            else
            {
                lossList.Add(new Item(name_Up + "(PTcon)", number * math_PTcon[0]));
                lossList.Add(new Item(name_Up + "(Pon)", number * math_Pon[0]));
                lossList.Add(new Item(name_Up + "(Poff)", number * math_Poff[0]));
                lossList.Add(new Item(name_Up + "(PDcon)", number * math_PDcon[0]));
                lossList.Add(new Item(name_Up + "(Prr)", number * math_Prr[0]));
                lossList.Add(new Item(name_Down + "(PTcon)", number * math_PTcon[1]));
                lossList.Add(new Item(name_Down + "(Pon)", number * math_Pon[1]));
                lossList.Add(new Item(name_Down + "(Poff)", number * math_Poff[1]));
                lossList.Add(new Item(name_Down + "(PDcon)", number * math_PDcon[1]));
                lossList.Add(new Item(name_Down + "(Prr)", number * math_Prr[1]));
            }
            return lossList;
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
        /// <param name="iUp">上管电流波形</param>
        /// <param name="iDown">下管电流波形</param>
        public void AddEvalParameters(int m, int n, double Vsw, Curve iUp, Curve iDown)
        {
            math_Vsw_eval[m, n] = Vsw;
            curve_iUp_eval[m, n] = iUp;
            curve_iDown_eval[m, n] = iDown;
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="Vsw">开关电压</param>
        /// <param name="iUp">上管电流波形</param>
        /// <param name="iDown">下管电流波形</param>
        /// <param name="fs">开关频率</param>
        public void AddEvalParameters(int m, int n, double Vsw, Curve iUp, Curve iDown, double fs)
        {
            math_Vsw_eval[m, n] = Vsw;
            curve_iUp_eval[m, n] = iUp;
            curve_iDown_eval[m, n] = iDown;
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
            curve_iUp = curve_iUp_eval[m, n];
            curve_iDown = curve_iDown_eval[m, n];
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
        /// 设置电路参数（损耗不均衡）
        /// </summary>
        /// <param name="Vsw">开关电压</param>
        /// <param name="iUp">上管电流波形</param>
        /// <param name="iDown">下管电流波形</param>
        /// <param name="fs">开关频率</param>
        public void SetParameters(double Vsw, Curve iUp, Curve iDown, double fs)
        {
            math_Vsw = Vsw;
            curve_iUp = iUp;
            curve_iDown = iDown;
            math_fs = fs;
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
                        designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
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
            if (!Data.SemiconductorList[device].Configuration.Equals("Dual"))
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
            if (isCheckExcess)
            {
                if (Data.SemiconductorList[device].Math_Vmax * (1 - margin) > math_Vmax * (1 + excess) || Data.SemiconductorList[device].Math_Imax * (1 - margin) > math_Imax * (1 + excess))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 计算损耗 TODO 未考虑MOSFET反向导通
        /// </summary>
        public override void CalcPowerLoss()
        {
            math_PTcon = new double[] { 0, 0 };
            math_Pon = new double[] { 0, 0 };
            math_Poff = new double[] { 0, 0 };
            math_PDcon = new double[] { 0, 0 };
            math_Prr = new double[] { 0, 0 };
            powerLoss = 0;

            CalcPowerLoss_Module(curve_iUp, out math_PTcon[0], out math_Pon[0], out math_Poff[0], out math_PDcon[0], out math_Prr[0]);
            if (isPowerLossBalance)
            {
                math_PTcon[1] = math_PTcon[0];
                math_Pon[1] = math_Pon[0];
                math_Poff[1] = math_Poff[0];
                math_PDcon[1] = math_PDcon[0];
                math_Prr[1] = math_Prr[0];
            }
            else
            {
                CalcPowerLoss_Module(curve_iDown, out math_PTcon[1], out math_Pon[1], out math_Poff[1], out math_PDcon[1], out math_Prr[1]);
            }
            for (int i = 0; i < 2; i++)
            {
                powerLoss += math_PTcon[i] + math_Pon[i] + math_Poff[i] + math_PDcon[i] + math_Prr[i];
            }
        }

        /// <summary>
        /// 计算模块损耗
        /// </summary>
        /// <param name="curve">电流曲线</param>
        /// <param name="PTcon">主管通态损耗</param>
        /// <param name="Pon">主管开通损耗</param>
        /// <param name="Poff">主管关断损耗</param>
        /// <param name="PDcon">反并二极管通态损耗</param>
        /// <param name="Prr">反并二极管反向恢复损耗</param>
        private void CalcPowerLoss_Module(Curve curve, out double PTcon, out double Pon, out double Poff, out double PDcon, out double Prr)
        {
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
                    if (Function.LE(i1, 0) && Function.GT(i2, 0)) //i1<=0、i2>0时，计算主管开通损耗
                    {
                        Pon += CalcPon_Module(i2);
                    }
                    if (Function.GT(i1, 0) && Function.LE(i2, 0)) //i1>0、i2<=0时，计算主管关断损耗
                    {
                        Poff += CalcPoff_Module(i1);
                    }
                    if (Function.LT(i1, 0) && Function.GE(i2, 0)) //i1<0、i2>=0时，计算反并二极管反向恢复损耗
                    {
                        Prr += CalcPrr_Module(i1);
                    }
                }
                else //t1≠t2时，只有通态损耗
                {
                    if (Function.GE(i1, 0) && Function.GE(i2, 0)) //电流都不为负时，为主管通态损耗
                    {
                        PTcon += CalcPTcon_Module(t1, i1, t2, i2); //计算主管通态损耗
                    }
                    else if (Function.LE(i1, 0) && Function.LE(i2, 0)) //电流都不为正时，为反并二极管通态损耗
                    {
                        PDcon += CalcPDcon_Module(t1, i1, t2, i2); //计算反并二极管通态损耗
                    }
                    else //电流一正一负时，既包含主管通态损耗，又包含反并二极管通态损耗
                    {
                        double z = (i1 * t2 - i2 * t1) / (i1 - i2); //计算过零点
                        if (i1 > 0) //i1>0时，主管先为导通状态
                        {
                            PTcon += CalcPTcon_Module(t1, i1, z, 0);
                            PDcon += CalcPDcon_Module(z, 0, t2, i2);
                        }
                        else //否则i2>0，反并二极管先为导通状态
                        {
                            PDcon += CalcPDcon_Module(t1, i1, z, 0);
                            PTcon += CalcPTcon_Module(z, 0, t2, i2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 计算主管通态损耗（模块）
        /// </summary>
        /// <param name="t1">左时刻</param>
        /// <param name="i1">左电流</param>
        /// <param name="t2">右时刻</param>
        /// <param name="i2">右电流</param>
        /// <returns>计算结果</returns>
        private double CalcPTcon_Module(double t1, double i1, double t2, double i2)
        {
            //采用牛顿-莱布尼茨公式进行积分
            int id;
            if (Data.SemiconductorList[device].Category.Equals("SiC-Module"))
            {
                id = Data.SemiconductorList[device].Id_Vds;
            }
            else
            {
                id = Data.SemiconductorList[device].Id_Vce;
            }
            double u1 = Data.CurveList[id].GetValue(i1); //获取左边界电流对应的导通压降
            double u2 = Data.CurveList[id].GetValue(i2); //获取右边界电流对应的导通压降
            double energy = Function.IntegrateTwoLinear(t1, i1, u1, t2, i2, u2); //计算导通能耗
            return energy * math_fs;
        }

        /// <summary>
        /// 计算主管开通损耗（模块）
        /// </summary>
        /// <param name="Ion">开通电流</param>
        /// <returns>计算结果</returns>
        private double CalcPon_Module(double Ion)
        {
            //根据开通电流查表得到对应损耗
            if (Function.EQ(Ion, 0))
            {
                return 0;
            }
            int id = Data.SemiconductorList[device].Id_Eon;
            return math_fs * math_Vsw / Data.CurveList[id].Math_Vsw * Data.CurveList[id].GetValue(Ion) * 1e-3;
        }

        /// <summary>
        /// 计算主管关断损耗（模块）
        /// </summary>
        /// <param name="Ioff">关断电流</param>
        /// <returns>计算结果</returns>
        private double CalcPoff_Module(double Ioff)
        {
            //根据关断电流查表得到对应损耗
            if (Function.EQ(Ioff, 0))
            {
                return 0;
            }
            int id = Data.SemiconductorList[device].Id_Eoff;
            return math_fs * math_Vsw / Data.CurveList[id].Math_Vsw * Data.CurveList[id].GetValue(Ioff) * 1e-3;
        }

        /// <summary>
        /// 计算反并二极管通态损耗（模块）
        /// </summary>
        /// <param name="t1">左时刻</param>
        /// <param name="i1">左电流</param>
        /// <param name="t2">右时刻</param>
        /// <param name="i2">右电流</param>
        /// <returns>计算结果</returns>
        private double CalcPDcon_Module(double t1, double i1, double t2, double i2)
        {
            //采用牛顿-莱布尼茨公式进行电压电流积分的优化计算
            i1 = (i1 >= 0 ? i1 : -i1);
            i2 = (i2 >= 0 ? i2 : -i2);
            int id = Data.SemiconductorList[device].Id_Vf;
            double u1 = Data.CurveList[id].GetValue(i1); //获取左边界电流对应的导通压降
            double u2 = Data.CurveList[id].GetValue(i2); //获取右边界电流对应的导通压降
            double energy = Function.IntegrateTwoLinear(t1, i1, u1, t2, i2, u2); //计算导通能耗
            return energy * math_fs;
        }

        /// <summary>
        /// 计算反并二极管反向恢复损耗（模块）
        /// </summary>
        /// <param name="Ioff">关断电流</param>
        /// <returns>计算结果</returns>
        private double CalcPrr_Module(double Ioff)
        {
            Ioff = (Ioff >= 0 ? Ioff : -Ioff);
            //根据关断电流查表得到对应损耗
            if (Function.EQ(Ioff, 0))
            {
                return 0;
            }
            int id = Data.SemiconductorList[device].Id_Err;
            return math_fs * math_Vsw / Data.CurveList[id].Math_Vsw * Data.CurveList[id].GetValue(Ioff) * 1e-3;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        protected override void CalcCost()
        {
            semiconductorCost = Data.SemiconductorList[device].Price;
            driverCost = 2 * 31.4253; //IX2120B IXYS MOQ100 Mouser TODO 驱动需要不同
            cost = semiconductorCost + driverCost;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        protected override void CalcVolume()
        {
            volume = Data.SemiconductorList[device].Volume;
        }

        /// <summary>
        /// 验证温度
        /// </summary>
        /// <returns>是否验证通过</returns>
        protected override bool CheckTemperature()
        {
            //计算工作在最大结温时的散热器温度
            double Pmain = math_PTcon[0] + math_Pon[0] + math_Poff[0];
            double Pdiode = math_PDcon[0] + math_Prr[0];
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
            //上下桥臂分开验证
            Pmain = math_PTcon[1] + math_Pon[1] + math_Poff[1];
            Pdiode = math_PDcon[1] + math_Prr[1];
            if (Data.SemiconductorList[device].Category.Equals("SiC-Module"))
            {
                Tc = math_Tj_max - Math.Max(Pmain * Data.SemiconductorList[device].MOSFET_RthJC, Pdiode * Data.SemiconductorList[device].Diode_RthJC);
            }
            else
            {
                Tc = math_Tj_max - Math.Max(Pmain * Data.SemiconductorList[device].IGBT_RthJC, Pdiode * Data.SemiconductorList[device].Diode_RthJC);
            }
            Th = Tc - (Pmain + Pdiode) * Data.SemiconductorList[device].Module_RthCH;
            if (Th < math_Th_max)
            {
                return false;
            }
            return true;
        }
    }
}
