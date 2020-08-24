using PV_analysis.Components;
using PV_analysis.Converters;
using System;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// 三电平Boost拓扑
    /// </summary>
    internal class InterleavedBoost : Topology
    {
        private static readonly double math_kIrip = 0.2; //电流纹波系数
        private static readonly double math_kVrip = 0.1; //电压纹波系数

        private DCDCConverter converter; //所属变换器

        //基本电路参数
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

        //元器件
        private DualModule dualModule;
        private Inductor inductor;
        private Capacitor capacitor;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="converter">所属变换器</param>
        public InterleavedBoost(DCDCConverter converter)
        {
            //获取设计规格
            this.converter = converter;
            math_Pfull = converter.Math_Psys / converter.Number;
            math_fs = converter.Math_fs;
            math_Vin_min = converter.Math_Vin_min;
            math_Vin_max = converter.Math_Vin_max;
            math_Vo = converter.Math_Vo;

            //初始化元器件
            dualModule = new DualModule(2, false);
            inductor = new Inductor(2);
            capacitor = new Capacitor(1);
            components = new Component[] { dualModule, inductor, capacitor };
            componentGroups = new Component[1][];
            componentGroups[0] = new Component[] { dualModule, inductor, capacitor };
        }

        /// <summary>
        /// 获取拓扑名
        /// </summary>
        /// <returns>拓扑名</returns>
        public override string GetName()
        {
            return "交错并联Boost";
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            double P = math_Pfull;
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
            Curve iC = new Curve(); //TODO 优化这里的电容电流模拟
            if (Function.GE(ILmin, 0))
            {
                //CCM
                iL.Add(0, ILmin);
                iL.Add(D * Ts, ILmax);
                iL.Add(Ts, ILmin);

                if (D > 0.5)
                {
                    iC.Add(0, -Io);
                    iC.Add((D - 0.5) * Ts, -Io);
                    iC.Add((D - 0.5) * Ts, ILmax - Io);
                    iC.Add(0.5 * Ts, ILmin - Io);
                    iC.Add(0.5 * Ts, -Io);
                }
                else
                {
                    double i1 = ILmax - ILrip / (Ts - D * Ts) * (0.5 * Ts - D * Ts);
                    double i2 = ILmax - ILrip / (Ts - D * Ts) * (0.5 * Ts);
                    iC.Add(0, i1 - Io);
                    iC.Add(D * Ts, i2 - Io);
                    iC.Add(D * Ts, ILmax + i2 - Io);
                    iC.Add(0.5 * Ts, ILmin + i1 - Io);
                    iC.Add(0.5 * Ts, i1 - Io);
                }
            }
            else
            {
                //DCM
                ILmin = 0;
                D = Math.Sqrt(2 * IL * L * (Vo - Vin) / (Ts * Vin * Vo));
                double D1 = D * Vin / (Vo - Vin);
                ILmax = D * Ts * Vin / L;
                iL.Add(0, ILmin);
                iL.Add(D * Ts, ILmax);
                iL.Add((D + D1) * Ts, ILmin);
                iL.Add(Ts, ILmin);
                ILrip = ILmax;

                if (D > 0.5)
                {
                    iC.Add(0, -Io);
                    iC.Add((D - 0.5) * Ts, -Io);
                    iC.Add((D - 0.5) * Ts, ILmax - Io);
                    iC.Add((D + D1 - 0.5) * Ts, -Io);
                    iC.Add(0.5 * Ts, -Io);
                }
                else
                {
                    if (D1 > 0.5)
                    {
                        double i1 = ILmax - ILrip / (D1 * Ts) * (0.5 * Ts - D * Ts);
                        double i2 = ILmax - ILrip / (D1 * Ts) * (0.5 * Ts);
                        iC.Add(0, i1 - Io);
                        iC.Add(D * Ts, i2 - Io);
                        iC.Add(D * Ts, ILmax + i2 - Io);
                        iC.Add(0.5 * Ts, ILmin + i1 - Io);
                        iC.Add(0.5 * Ts, i1 - Io);
                    }
                    else if (D1 > 0.5 - D)
                    {
                        double i1 = ILmax - ILrip / (D1 * Ts) * (0.5 * Ts - D * Ts);
                        iC.Add(0, i1 - Io);
                        iC.Add((D + D1 - 0.5) * Ts, -Io);
                        iC.Add(D * Ts, -Io);
                        iC.Add(D * Ts, ILmax - Io);
                        iC.Add(0.5 * Ts, ILmin + i1 - Io);
                        iC.Add(0.5 * Ts, i1 - Io);
                    }
                    else
                    {
                        iC.Add(0, -Io);
                        iC.Add(D * Ts, -Io);
                        iC.Add(D * Ts, ILmax - Io);
                        iC.Add((D + D1) * Ts, -Io);
                        iC.Add(0.5 * Ts, -Io);
                    }
                }
            }

            //记录电路参数
            math_IL = IL;
            math_ILrip = ILrip;
            curve_iS = iL.Filter(0, D * Ts);
            curve_iD = iL.Filter(D * Ts, Ts);
            math_ICrms = iC.CalcRMS();
            //Console.WriteLine(Function.EQ(iC.Integrate(), 0));
            //Graph graph = new Graph();
            //graph.Add(iC, "iC");
            //graph.Draw();
        }

        /// <summary>
        /// 准备评估所需的电路参数
        /// </summary>
        public override void Prepare()
        {
            //计算电路参数
            DesignCircuitParam();

            math_ICrms_max = 0;
            int m = Config.CGC_VOLTAGE_RATIO.Length;
            int n = Config.CGC_POWER_RATIO.Length;
            for (int i = 0; i < m; i++)
            {
                math_Vin = math_Vin_min + (math_Vin_max - math_Vin_min) * Config.CGC_VOLTAGE_RATIO[i];
                for (int j = 0; j < n; j++)
                {
                    math_P = math_Pfull * Config.CGC_POWER_RATIO[j]; //改变负载
                    Simulate();
                    //Graph graph = new Graph();
                    //graph.Add(curve_iS, "iS");
                    //graph.Add(curve_iD, "iD");
                    //graph.Draw();
                    math_ICrms_max = Math.Max(math_ICrms_max, math_ICrms);

                    //设置元器件的电路参数（用于评估）
                    dualModule.AddEvalParameters(i, j, math_VSmax, curve_iD.Copy(-1), curve_iS); //采用半桥模块时，第二个开关管波形为-iD
                    inductor.AddEvalParameters(i, j, math_IL, math_ILrip);
                    capacitor.AddEvalParameters(i, j, math_ICrms);
                }
            }

            //设置元器件的设计条件
            dualModule.SetConditions(math_VSmax, math_ISmax, math_fs);
            inductor.SetConditions(math_L, math_ILmax, math_fs);
            capacitor.SetConditions(math_C, math_VCmax, math_ICrms_max);
        }

        /// <summary>
		/// 计算相应负载下的电路参数
		/// </summary>
		/// <param name="load">负载</param>
		public override void Calc(double load)
        {
            math_P = math_Pfull * load; //改变负载
            Simulate();
            //设置元器件的电路参数
            dualModule.SetParameters(math_VSmax, curve_iS, curve_iD.Copy(-1), math_fs); //采用半桥模块时，第二个开关管波形为-iD
            inductor.SetParameters(math_IL, math_ILrip, math_fs);
            capacitor.SetParameters(math_ICrms);
        }
    }
}
