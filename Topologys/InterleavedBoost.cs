﻿using PV_analysis.Components;
using PV_analysis.Converters;
using System;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// 三电平Boost类
    /// </summary>
    internal class InterleavedBoost : Topology
    {
        private static readonly double math_kIrip = 0.2; //电流纹波系数
        private static readonly double math_kVrip = 0.1; //电压纹波系数

        //基本电路参数
        private double math_Pmax; //满载功率
        private double math_P; //功率
        private double math_fs; //开关频率
        private double math_Vin_min; //输入电压最小值
        private double math_Vin_max; //输入电压最大值
        private double math_Vin; //输入电压
        private double math_Vo; //输出电压

        //主电路元件参数
        private double math_L; //电感值
        private double math_IL; //电感电流平均值
        private double math_ILrip; //电感电流纹波
        private double math_ILmax; //电感电流峰值最大值
        private double math_VSmax; //开关器件电压应力
        private double math_ISmax; //开关器件电流应力
        private double math_C; //电容值
        private double math_VCmax; //电容电压应力
        private double math_ICrms; //电容电流有效值
        private double math_ICrms_max; //电容电流有效值最大值

        //电压、电流波形
        private Curve curve_iS; //主管电流
        private Curve curve_iD; //升压二极管电流

        //电路参数（用于评估）
        private double[,] math_IL_eval; //电感电流平均值（用于评估）
        private double[,] math_ILrip_eval; //电感电流纹波（用于评估）
        private double[,] math_ICrms_eval; //电容电流有效值（用于评估）

        //电压、电流波形（用于评估）
        private Curve[,] curve_iS_eval; //主管电流（用于评估）
        private Curve[,] curve_iD_eval; //升压二极管电流（用于评估）
        private Curve[,] curve_iS_dual_eval; //使用半桥模块进行设计时，上管电流（用于评估）

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="converter">所属变换器</param>
        public InterleavedBoost(DCDCConverter converter)
        {
            this.converter = converter;
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            double P = math_Pmax;
            double Vin = math_Vin_min;
            double Vo = math_Vo;
            double fs = math_fs;
            double kIrip = math_kIrip;
            double kVrip = math_kVrip;

            double D = 1 - (Vin / Vo); //占空比
            double Iin = P / Vin; //输入电流平均值
            double Io = P / Vo; //输出电流平均值
            double IL = Iin * 0.5; //电感电流平均值
            double ILrip = IL * kIrip; ; //电感电流纹波
            double L = D * Vin / (fs * ILrip); //感值
            double VC = Vo; //电容电压平均值
            double VCrip = VC * kVrip; //电压纹波
            double C; //容值
            if (D > 0.5)
            {
                C = (D - 0.5) * Io / (fs * VCrip);
            }
            else
            {
                C = D * (Io - IL) / (fs * VCrip);
            }

            //记录设计结果
            math_L = L;
            math_C = C;

            //计算设计条件
            math_ILmax = IL + ILrip * 0.5;
            math_ISmax = math_ILmax;
            math_VSmax = Vo;
            math_VCmax = VC + VCrip * 0.5;
        }

        /// <summary>
        /// 计算电路参数，并模拟电压、电流波形
        /// </summary>
        private void Simulate()
        {
            double P = math_P;
            double Vin = math_Vin;
            double Vo = math_Vo;
            double fs = math_fs;
            double L = math_L;

            //计算电路参数
            double Ts = 1 / fs; //开关周期
            double Iin = P / Vin; //输入电流平均值
            double Io = P / Vo; //输出电流平均值
            double IL = Iin * 0.5; //电感电流平均值
            double D = 1 - (Vin / Vo); //占空比
            double ILrip = D * Ts * Vin / L; //电感电流纹波
            double ILmax = IL + ILrip * 0.5; //电感电流峰值
            double ILmin = IL - ILrip * 0.5; //电感电流谷值

            Curve iL = new Curve();
            if (Function.GE(ILmin, 0))
            {
                //CCM
                iL.Add(0, ILmin);
                iL.Add(D * Ts, ILmax);
                iL.Add(Ts, ILmin);
            }
            else
            {
                //DCM
                ILmin = 0;
                D = Math.Sqrt(2 * Iin * L * (Vo - Vin) / (Ts * Vin * Vo));
                double D1 = D * Vin / (Vo - Vin);
                ILmax = D * Ts * Vin / L;
                iL.Add(0, ILmin);
                iL.Add(D * Ts, ILmax);
                iL.Add((D + D1) * Ts, ILmin);
                iL.Add(Ts, ILmin);
                ILrip = ILmax / 2;
            }

            //记录电路参数
            math_IL = IL;
            math_ILrip = ILrip;
            curve_iS = iL.Cut(0, D * Ts);
            curve_iD = iL.Cut(D * Ts, Ts);

            ////电容电流有效值
            //double c, f1, f2;
            //if (D > 0.5)
            //{
            //    f1 = ILmin - this.currentOutput;
            //    f2 = this.currentInductorPeak - this.currentOutput;
            //    c = (Math.pow(this.currentOutput, 2) * this.time1 + myIntegral(this.time1, f2, f2, this.time2, f1, f1)) * 2;
            //}
            //else
            //{
            //    double i1 = this.currentInductorPeak - this.currentInductorRipple / (this.timeCycle - this.time3) * (this.time2 - this.time3);
            //    double i2 = this.currentInductorPeak - this.currentInductorRipple / (this.timeCycle - this.time3) * (this.time4 - this.time3);
            //    f1 = i1 - this.currentOutput;
            //    f2 = i2 - this.currentOutput;
            //    double f3 = this.currentInductorPeak + i2 - this.currentOutput;
            //    double f4 = this.currentInductorTrough + i1 - this.currentOutput;
            //    c = (myIntegral(0, f2, f2, this.time3, f1, f1) + myIntegral(this.time3, f3, f3, this.time2, f4, f4)) * 2;
            //}
            //math_ICrms = Math.sqrt(this.frequency * c);
        }

        /// <summary>
        /// 自动设计，得到每个器件的设计方案
        /// </summary>
        public override void Design()
        {
            //初始化
            DualModule dualModule = new DualModule(2);
            Inductor inductor = new Inductor(1);
            Capacitor capacitor = new Capacitor(1);
            components = new Component[] { dualModule, inductor, capacitor };
            componentGroups = new Component[1][];
            componentGroups[0] = new Component[] { dualModule, inductor, capacitor };

            //获取设计规格
            math_Pmax = converter.Math_Psys / converter.Number;
            math_fs = converter.Math_fs;
            math_Vin_min = converter.Math_Vin_min;
            math_Vin_max = converter.Math_Vin_max;
            math_Vo = converter.Math_Vo;

            //计算电路参数
            DesignCircuitParam();
            math_ICrms_max = 0;
            int m = Config.CGC_VOLTAGE_RATIO.Length;
            int n = Config.CGC_POWER_RATIO.Length;
            math_IL_eval = new double[m, n];
            math_ILrip_eval = new double[m, n];
            curve_iS_eval = new Curve[m, n];
            curve_iD_eval = new Curve[m, n];
            curve_iS_dual_eval = new Curve[m, n];
            math_ICrms_eval = new double[m, n];
            for (int i = 0; i < m; i++)
            {
                math_Vin = math_Vin_min + (math_Vin_max - math_Vin_min) * Config.CGC_VOLTAGE_RATIO[i];
                for (int j = 0; j < n; j++)
                {
                    math_P = math_Pmax * Config.CGC_POWER_RATIO[j]; //改变模块功率
                    Simulate();
                    //Graph graph = new Graph();
                    //graph.Add(curve_iS, "iS");
                    //graph.Add(curve_iD, "iD");
                    //graph.Draw();
                    math_ICrms_max = Math.Max(math_ICrms_max, math_ICrms);
                    math_IL_eval[i, j] = math_IL;
                    math_ILrip_eval[i, j] = math_ILrip;
                    curve_iS_eval[i, j] = curve_iS.Copy(); //类对象需要进行复制
                    curve_iD_eval[i, j] = curve_iD.Copy();
                    curve_iS_dual_eval[i, j] = curve_iD.Copy(-1); //采用半桥模块时，第二个开关管波形为-iD
                    math_ICrms_eval[i, j] = math_ICrms;
                }
            }

            //设置元器件的设计条件
            dualModule.SetConditions(math_VSmax, math_ISmax, math_fs);
            inductor.SetConditions(math_L, math_fs, math_ILmax);
            capacitor.SetConditions(math_C, math_VCmax, math_ICrms_max);

            //设置元器件的电路参数（用于评估）
            dualModule.SetEvalParameters(math_VSmax, curve_iS_dual_eval, curve_iS_eval);
            inductor.SetEvalParameters(math_IL_eval, math_ILrip_eval);
            capacitor.SetEvalParameters(math_ICrms_eval);

            foreach (Component component in components)
            {
                component.Design();
            }
        }
    }
}
