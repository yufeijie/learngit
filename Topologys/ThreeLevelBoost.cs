using PV_analysis.Components;
using PV_analysis.Converters;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// 三电平Boost类
    /// </summary>
    internal class ThreeLevelBoost : Topology
    {
        private static readonly double math_kIrip = 0.2; //电流纹波系数
        private static readonly double math_kVrip = 0.1; //电压纹波系数

        private readonly DCDCConverter converter; //所属变换器
        private readonly Component[][] allComponentGroups; //所有可行元器件组
        private readonly Component[] allComponents; //可行元件组中出现的所有元器件（待设计的元器件）
        private Component[] componentGroup; //当前选用的元器件组

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
        private double math_C; //电容值
        private double math_VSmax; //开关器件电压应力
        private double math_ISmax; //开关器件电流应力
        private double math_ILrip_max; //电感电流纹波最大值
        private double math_ILpeak_max; //电感电流峰值最大值
        private double math_
        private double math_VCmax; //电容电压应力
        private double math_ICrms; //电容电流有效值

        //电压、电流波形
        private Curve curve_iS;
        private Curve curve_iD;

        //电压、电流波形（不同输入电压、负载点）
        private Curve[,] curve_iS_eval;
        private Curve[,] curve_iD_eval;

        public ThreeLevelBoost(DCDCConverter converter)
        {
            this.converter = converter;
            Component DualIGBT= new DualIGBT();
            Component inductor = new Inductor();
            Component capacitor = new Capacitor();
            allComponents = new Component[] { DualIGBT, inductor, capacitor };
            allComponentGroups = new Component[1][];
            allComponentGroups[0] = new Component[] { DualIGBT, inductor, capacitor };

            math_Pmax = converter.Math_Psys / converter.Number;
            math_fs = converter.Math_fs;
            math_Vin_min = converter.Math_Vin_min;
            math_Vin_max = converter.Math_Vin_max;
            math_Vo = converter.Math_Vo;
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        public void DesignCircuitParam()
        {
            double P = math_P;
            double Vin = math_Vin;
            double Vo = math_Vo;
            double fs = math_fs;
            double kIrip = math_kIrip;
            double kVrip = math_kVrip;

            double D = 1 - (Vin / Vo); //占空比
            double Iin = P / Vin; //输入电流平均值
            double Io = P / Vo; //输出电流平均值
            double ILrip = Iin * kIrip; ; //电感电流纹波
            double L; //感值
            if (D > 0.5)
            {
                L = (D - 0.5) * Vin / (fs * ILrip);
            }
            else
            {
                L = D * (Vin - 0.5 * Vo) / (fs * ILrip);
            }
            double VSmax = Vo * 0.5; //开关器件电压应力
            double VCmax = Vo * 0.5; //电容电压平均值
            double VCrip = VC * kVrip; //电压纹波
            double C = D * Io / (fs * VCrip); //容值

            //记录设计结果
            math_L = L;
            math_C = C;
            math_VS = VS;
            math_VC = VC;
        }

        /// <summary>
        /// 计算电路参数，并模拟电压、电流波形
        /// </summary>
        public void Simulate()
        {
            double P = math_P;
            double Vin = math_Vin;
            double Vo = math_Vo;
            double fs = math_fs;
            double L = math_L;

            //计算电路参数
            double D = 1 - (Vin / Vo); //占空比
            double Ts = 1 / fs; //开关周期
            double t1 = (D - 0.5) * Ts; //特殊时刻
            double t2 = 0.5 * Ts;
            double t3 = D * Ts;
            double t4 = (D + 0.5) * Ts;
            double Iin = P / Vin; //输入电流平均值
            double Io = P / Vo; //输出电流平均值
            double IL = Iin; //电感电流平均值
            double ILrip; //电感电流纹波
            if (D > 0.5)
            {
                ILrip = (D - 0.5) * Vin / (fs * L);
            }
            else
            {
                ILrip = D * (Vin - 0.5 * Vo) / (fs * L);
            }
            double ILmax = IL + ILrip * 0.5; //电感电流峰值
            double ILmin = IL - ILrip * 0.5; //电感电流谷值

            //主开关管电流波形
            curve_iS = new Curve();
            if (D > 0.5)
            {
                curve_iS.Add(0, 0);
                curve_iS.Add(0, ILmin);
                curve_iS.Add(t1, ILmax);
                curve_iS.Add(t2, ILmin);
                curve_iS.Add(t3, ILmax);
                curve_iS.Add(t3, 0);
                curve_iS.Add(Ts, 0);
            }
            else
            {
                curve_iS.Add(0, 0);
                curve_iS.Add(0, ILmin);
                curve_iS.Add(t3, ILmax);
                curve_iS.Add(t3, 0);
                curve_iS.Add(Ts, 0);
            }
            //升压二极管电流波形
            curve_iD = new Curve();
            if (D > 0.5)
            {
                curve_iD.Add(0, 0);
                curve_iD.Add(t3, 0);
                curve_iD.Add(t3, ILmax);
                curve_iD.Add(Ts, ILmin);
                curve_iD.Add(Ts, 0);
            }
            else
            {
                curve_iD.Add(0, 0);
                curve_iD.Add(t3, 0);
                curve_iD.Add(t3, ILmax);
                curve_iD.Add(t2, ILmin);
                curve_iD.Add(t4, ILmax);
                curve_iD.Add(Ts, ILmin);
                curve_iD.Add(Ts, 0);
            }
            //电容电流波形
            Curve math_iC = curve_iD.Copy(0, -Io);

            double ICrms = math_iC.CalcRMS(); //电容电流有效值
        }

        public void Design()
        {
            CircuitParamDesign();
            int m = Config.CGC_VOLTAGE_RATIO.Length;
            int n = Config.CGC_POWER_RATIO.Length;
            curve_iD_eval = new Curve[m, n];
            curve_iS_eval = new Curve[m, n];
            for (int i = 0; i < m; i++)
            {
                math_Vin = math_Vin_min + (math_Vin_max - math_Vin_min) * Config.CGC_VOLTAGE_RATIO[i];
                for (int j = 0; j < n; j++)
                {
                    math_P = math_Pmax * Config.CGC_POWER_RATIO[j]; //改变模块功率
                    Simulate();
                    curve_iS_eval[i, j] = curve_iS.Copy();
                    curve_iD_eval[i, j] = curve_iD.Copy();
                }
            }
        }
    }
}
