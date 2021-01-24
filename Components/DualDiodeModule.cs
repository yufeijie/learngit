using PV_analysis.Informations;
using System;
using System.Collections.Generic;
using static PV_analysis.Curve;

namespace PV_analysis.Components
{
    internal class DualDiodeModule : Semiconductor
    {
        //器件参数
        private bool isPowerLossBalance; //损耗是否均衡，若均衡则只需计算上管的损耗，默认为均衡

        //电路参数
        private Curve curve_iUp; //上管电流波形
        private Curve curve_iDown; //下管电流波形
        private Curve[,] curve_iUp_eval = new Curve[5, 7]; //上管电流波形（用于评估）
        private Curve[,] curve_iDown_eval = new Curve[5, 7]; //下管电流波形（用于评估）

        //损耗参数（同类器件中其中一个的损耗）
        private double[] math_PDcon; //二极管通态损耗
        private double[] math_Prr; //二极管反向恢复损耗

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
        public DualDiodeModule(int number, bool isPowerLossBalance = true)
        {
            this.number = number;
            this.isPowerLossBalance = isPowerLossBalance;
        }

        /// <summary>
        /// 获取设计方案的配置信息标题
        /// </summary>
        /// <returns>配置信息标题</returns>
        public override string[] GetConfigTitles()
        {
            string[] data = { "同类器件数量", "型号" };
            return data;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            return new string[] { number.ToString(), GetDeviceType() };
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
            };
            return list;
        }

        /// <summary>
        /// 获取损耗分布
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public override List<Info> GetLossBreakdown()
        {
            List<Info> list = new List<Info>();
            if (isPowerLossBalance)
            {
                list.Add(new Info(Name + "(PDcon)", Math.Round(number * math_PDcon[0] * 2, 2)));
                list.Add(new Info(Name + "(Prr)", Math.Round(number * math_Prr[0] * 2, 2)));
            }
            else
            {
                list.Add(new Info(Name_Up + "(PDcon)", Math.Round(number * math_PDcon[0], 2)));
                list.Add(new Info(Name_Up + "(Prr)", Math.Round(number * math_Prr[0], 2)));
                list.Add(new Info(Name_Down + "(PDcon)", Math.Round(number * math_PDcon[1], 2)));
                list.Add(new Info(Name_Down + "(Prr)", Math.Round(number * math_Prr[1], 2)));
            }
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
        /// <param name="Von">开通电压</param>
        /// <param name="Voff">关断电压</param>
        /// <param name="iUp">上管电流波形</param>
        /// <param name="iDown">下管电流波形</param>
        public void AddEvalParameters(int m, int n, double Von, double Voff, Curve iUp, Curve iDown)
        {
            math_Von_eval[m, n] = Von;
            math_Voff_eval[m, n] = Voff;
            curve_iUp_eval[m, n] = iUp;
            curve_iDown_eval[m, n] = iDown;
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="Von">开通电压</param>
        /// <param name="Voff">关断电压</param>
        /// <param name="iUp">上管电流波形</param>
        /// <param name="iDown">下管电流波形</param>
        /// <param name="fs">开关频率</param>
        public void AddEvalParameters(int m, int n, double Von, double Voff, Curve iUp, Curve iDown, double fs)
        {
            math_Von_eval[m, n] = Von;
            math_Voff_eval[m, n] = Voff;
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
            math_Von = math_Von_eval[m, n];
            math_Voff = math_Voff_eval[m, n];
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
        /// <param name="Von">开通电压</param>
        /// <param name="Voff">关断电压</param>
        /// <param name="iUp">上管电流波形</param>
        /// <param name="iDown">下管电流波形</param>
        /// <param name="fs">开关频率</param>
        public void SetParameters(double Von, double Voff, Curve iUp, Curve iDown, double fs)
        {
            math_Von = Von;
            math_Voff = Voff;
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
            if (device < 0 || device >= Data.SemiconductorList.Count) return false;

            //验证器件是否可用
            if (!Data.SemiconductorList[device].Available) return false;

            //验证器件类型是否符合
            if (!Data.SemiconductorList[device].Category.Equals("Diode-Module") && !Data.SemiconductorList[device].Category.Equals("Diode-Module (No Err)")) return false;

            //验证器件结构是否符合
            if (!Data.SemiconductorList[device].Configuration.Equals("Dual") && !Data.SemiconductorList[device].Configuration.Equals("Single")) return false;

            //验证电压、电流条件是否满足
            if (!ValidateVoltageAndCurrent()) return false;

            return true;
        }

        /// <summary>
        /// 计算损耗 TODO 未考虑MOSFET反向导通
        /// </summary>
        public override void CalcPowerLoss()
        {
            math_PDcon = new double[] { 0, 0 };
            math_Prr = new double[] { 0, 0 };
            powerLoss = 0;

            CalcPowerLoss_Module(curve_iUp, out math_PDcon[0], out math_Prr[0]);
            if (isPowerLossBalance)
            {
                math_PDcon[1] = math_PDcon[0];
                math_Prr[1] = math_Prr[0];
            }
            else
            {
                CalcPowerLoss_Module(curve_iDown, out math_PDcon[1], out math_Prr[1]);
            }
            for (int i = 0; i < 2; i++)
            {
                powerLoss += math_PDcon[i] + math_Prr[i];
            }
        }

        /// <summary>
        /// 计算模块损耗
        /// </summary>
        /// <param name="curve">电流曲线</param>
        /// <param name="PDcon">二极管通态损耗</param>
        /// <param name="Prr">二极管反向恢复损耗</param>
        private void CalcPowerLoss_Module(Curve curve, out double PDcon, out double Prr)
        {
            PDcon = 0;
            Prr = 0;
            if (curve == null) return;

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
                    if (Function.GT(i1, 0) && Function.LE(i2, 0)) //i1>0, i2<=0时，计算反并二极管反向恢复损耗
                    {
                        Prr += CalcPrr_Module(i1);
                    }
                }
                else //t1≠t2时，只有通态损耗
                {
                    PDcon += CalcPDcon_Module(t1, i1, t2, i2); //计算反并二极管通态损耗
                }
            }
        }

        /// <summary>
        /// 计算二极管通态损耗（模块）
        /// </summary>
        /// <param name="t1">左时刻</param>
        /// <param name="i1">左电流</param>
        /// <param name="t2">右时刻</param>
        /// <param name="i2">右电流</param>
        /// <returns>计算结果</returns>
        private double CalcPDcon_Module(double t1, double i1, double t2, double i2)
        {
            double u1;
            double u2;
            double energy;
            //采用牛顿-莱布尼茨公式进行电压电流积分的优化计算
            if (Data.SemiconductorList[device].Category.Equals("Diode-Module (No Vf curve)"))
            {
                //Vf曲线简单拟合
                double p = 2; //曲线次数
                double Vt = 0.3; //二极管开启电压
                double V0 = Data.SemiconductorList[device].Curve_Vf_V0; //手册给出的导通压降
                double I0 = Data.SemiconductorList[device].Curve_Vf_I0; //手册导通压降对应电流
                double a = I0 / Math.Pow(V0 - Vt, p); //系数
                u1 = Math.Pow(i1 / a, 1 / p) + Vt; //获取左边界电流对应的导通压降
                u2 = Math.Pow(i2 / a, 1 / p) + Vt; //获取右边界电流对应的导通压降
                energy = Function.IntegrateTwoLinear(t1, i1, u1, t2, i2, u2); //计算导通能耗
            }
            else
            {
                int id = Data.SemiconductorList[device].Id_Vf;
                u1 = Data.CurveList[id].GetValue(i1); //获取左边界电流对应的导通压降
                u2 = Data.CurveList[id].GetValue(i2); //获取右边界电流对应的导通压降
                energy = Function.IntegrateTwoLinear(t1, i1, u1, t2, i2, u2); //计算导通能耗
            }
            return energy * math_fs;
        }

        /// <summary>
        /// 计算反并二极管反向恢复损耗（模块）
        /// </summary>
        /// <param name="Ioff">关断电流</param>
        /// <returns>计算结果</returns>
        private double CalcPrr_Module(double Ioff)
        {
            if (Data.SemiconductorList[device].Category.Equals("Diode-Module (No Err)"))
            {
                Console.WriteLine("Diode-Module (No Err)类器件无法计算反向恢复损耗！");
                Environment.Exit(-1);
            }
            //忽略电流极小的情况
            if (!Function.BigEnough(Ioff))
            {
                return 0;
            }
            //根据关断电流查表得到对应损耗
            int id = Data.SemiconductorList[device].Id_Err;
            return math_fs * math_Voff / Data.CurveList[id].Math_Vsw * Data.CurveList[id].GetValue(Ioff) * 1e-3;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        protected override void CalcCost()
        {
            semiconductorCost = Data.SemiconductorList[device].Price;
            if (Data.SemiconductorList[device].Configuration.Equals("Single"))
            {
                semiconductorCost *= 2;
            }
            cost = semiconductorCost;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        protected override void CalcVolume()
        {
            volume = Data.SemiconductorList[device].Volume;
            if (Data.SemiconductorList[device].Configuration.Equals("Single"))
            {
                volume *= 2;
            }
        }

        /// <summary>
        /// 验证温度
        /// </summary>
        /// <returns>是否验证通过</returns>
        protected override bool CheckTemperature()
        {
            //计算工作在最大结温时的散热器温度
            double Pdiode = math_PDcon[0] + math_Prr[0];
            double Tc = math_Tj_max - Pdiode * Data.SemiconductorList[device].Diode_RthJC;
            double Th = Tc - Pdiode * Data.SemiconductorList[device].Module_RthCH;
            if (Th < math_Th_max) //若此时的散热器温度低于允许温度，则不通过
            {
                return false;
            }
            //上下桥臂分开验证
            Pdiode = math_PDcon[1] + math_Prr[1];
            Tc = math_Tj_max - Pdiode * Data.SemiconductorList[device].Diode_RthJC;
            Th = Tc - Pdiode * Data.SemiconductorList[device].Module_RthCH;
            if (Th < math_Th_max)
            {
                return false;
            }
            return true;
        }
    }
}
