using MathWorks.MATLAB.NET.Arrays;
using PV_analysis.Components;
using PV_analysis.Converters;
using System;
using static PV_analysis.Curve;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// SRC拓扑
    /// </summary>
    internal class SRC : Topology
    {
        private IsolatedDCDCConverter converter; //所属变换器

        //可选参数
        private bool isLeakageInductanceIntegrated = true; //是否认为谐振电感集成在变压器中

        //给定参数
        private double math_Vin; //输入电压
        private double math_Vo; //输出电压预设值
        private int math_No; //副边个数
        private double math_Q; //品质因数
        private double math_fr; //谐振频率

        //主电路元件参数
        private double math_fs; //开关频率
        private double math_VSpmax; //原边开关器件电压应力
        private double math_VSsmax; //副边开关器件电压应力
        private double math_n; //变压器变比
        private double math_ψ; //磁链
        private double math_Lr; //谐振电感值
        private double math_ILrms; //谐振电感电流有效值
        private double math_ILp; //谐振电感电流峰值
        private double math_Cr; //谐振电容值
        private double math_VCrp; //谐振电容电压峰值
        private double math_VCfmax; //电容电压应力
        private double math_ICfrms; //滤波电容电流有效值

        //电压、电流波形
        private double math_vSp; //原边开关器件电压
        private double math_vSs; //副边开关器件电压
        private Curve curve_iSp; //原边开关器件电流波形
        private Curve curve_iSs; //副边开关器件电流波形
        private Curve curve_iL; //谐振电感电流波形
        private Curve curve_vCr; //谐振电容电压波形
        private Curve curve_iCf; //滤波电容电流波形

        //元器件
        private DualModule primaryDualModule;
        private DualModule secondaryDualModule; //TODO 此处应为二极管
        private Inductor resonantInductor;
        private Transformer transformer;
        private Capacitor resonantCapacitor;
        private Capacitor filteringCapacitor;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="converter">所属变换器</param>
        public SRC(IsolatedDCDCConverter converter)
        {
            //获取设计规格
            this.converter = converter;
            math_Pfull = converter.Math_Psys / converter.PhaseNum / converter.Number;
            math_Vin = converter.Math_Vin;
            math_Vo = converter.Math_Vo;
            math_No = converter.Math_No;
            math_fr = converter.Math_fr;
            math_Q = converter.Math_Q;

            //初始化元器件
            primaryDualModule = new DualModule(2)
            {
                Name = "原边开关管",
                VoltageVariable = false
            };
            secondaryDualModule = new DualModule(2 * math_No)
            {
                Name = "副边二极管",
                VoltageVariable = false
            };
            resonantInductor = new Inductor(1)
            {
                Name = "谐振电感",
                VoltageVariable = false
            };
            transformer = new Transformer(1)
            {
                Name = "变压器",
                VoltageVariable = false
            };
            resonantCapacitor = new ResonantCapacitor(1)
            {
                Name = "谐振电容",
                VoltageVariable = false,
            };
            filteringCapacitor = new FilteringCapacitor(math_No)
            {
                Name = "滤波电容",
                VoltageVariable = false,
            };

            componentGroups = new Component[1][];
            if (isLeakageInductanceIntegrated)
            {
                components = new Component[] { primaryDualModule, secondaryDualModule, transformer, resonantCapacitor, filteringCapacitor };
                componentGroups[0] = new Component[] { primaryDualModule, secondaryDualModule, transformer, resonantCapacitor, filteringCapacitor };
            }
            else
            {
                components = new Component[] { primaryDualModule, secondaryDualModule, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
                componentGroups[0] = new Component[] { primaryDualModule, secondaryDualModule, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
            }
        }

        /// <summary>
        /// 获取拓扑名
        /// </summary>
        /// <returns>拓扑名</returns>
        public override string GetName()
        {
            return "SRC";
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            double P = math_Pfull;
            double Vin = math_Vin;
            double Vo = math_Vo;
            double No = math_No;
            double Q = math_Q;
            double fr = math_fr;

            double Tr = 1 / fr; //谐振周期
            double Td = Tr / 50; //死区时间
            double wr = 2 * Math.PI * fr; //谐振角速度
            double RL = No * Math.Pow(Vo, 2) / P; //负载等效电阻
            double n = Vin / Vo; //变比
            double Zr = Q * 8 * Math.Pow(n / Math.PI, 2) * RL / No; //谐振阻抗
            //求解fs
            MWArray output = Formula.solve.solveSRC_fs(Q, Vin, n, Td, fr);
            MWNumericArray result = (MWNumericArray)output;
            double fs = result.ToScalarDouble();
            if (fs < fr || fs > fr * 2)
            {
                Console.WriteLine("Wrong fs!");
                System.Environment.Exit(-1);
            }

            math_fs = fs;
            math_n = n;
            math_Lr = Zr / wr;
            math_Cr = 1 / Zr / wr;
            math_ψ = 0.5 * Vo * n / fs;
            math_VSpmax = Vin;
            math_VSsmax = Vo;
            math_VCfmax = Vo;
        }

        /// <summary>
        /// 计算电路参数，并模拟电压、电流波形
        /// </summary>
        private void Simulate()
        {
            double P = math_P;
            double Vin = math_Vin;
            double Vo_ref = math_Vo;
            double No = math_No;
            double fr = math_fr;
            double fs = math_fs;
            double n = math_n;
            double Lr = math_Lr;
            double Cr = math_Cr;

            double wr = 2 * Math.PI * fr; //谐振角速度
            double Zr = Math.Sqrt(Lr / Cr); //谐振阻抗
            double RL = No * Math.Pow(Vo_ref, 2) / P; //负载等效电阻
            double Q = Zr / (Math.Pow(n, 2) * RL / No); //品质因数（仅用于计算，并非基波等效的品质因数）
            double Ts = 1 / fs; //开关周期

            //求解Vo和t0
            MWArray output = Formula.solve.solveSRC(Q, Vin, n, fs, fr);
            MWNumericArray result = (MWNumericArray)output;
            double Vo = result[1].ToScalarDouble(); //实际输出电压
            double t0 = result[2].ToScalarDouble(); //电流过零点
            if (Vo < Vo_ref * 0.8)
            {
                Console.WriteLine("Wrong Vo!");
                System.Environment.Exit(-1);
            }
            if (t0 < 0 || t0 >= Ts / 2)
            {
                Console.WriteLine("Wrong t0!");
                System.Environment.Exit(-1);
            }
            double VCrp = Formula.SRC_Vcrp(n, Q, Vo, fr, fs);
            double Io = Vo / RL;

            double ILp = 0;
            curve_iL = new Curve();
            curve_vCr = new Curve();
            double startTime = 0;
            double endTime = Ts;
            double dt = (endTime - startTime) / Config.DEGREE;
            double t;
            for (int i = 0; i <= Config.DEGREE; i++)
            {
                t = startTime + dt * i;
                double vab = Formula.SRC_vab(t, Ts, Vin);
                double vTp = Formula.SRC_vTp(t, Ts, t0, n, Vo);
                double iLrp = Formula.SRC_ilrp(t, Ts, VCrp, vab, vTp, Zr);
                double iLr = Formula.SRC_ilr(t, Ts, t0, iLrp, wr);
                double vCr = Formula.SRC_vcr(t, Ts, t0, VCrp, vab, vTp, wr);
                curve_iL.Add(t, iLr);
                curve_vCr.Add(t, vCr);
                ILp = Math.Max(ILp, Math.Abs(iLr)); //记录峰值
            }
            //补充特殊点（保证现有的开关器件损耗计算方法正确）
            curve_iL.Order(t0, 0);
            curve_iL.Order(t0 + Ts / 2, 0);
            curve_vCr.Order(t0, -VCrp);
            curve_vCr.Order(t0 + Ts / 2, VCrp);
            //生成主电路元件波形
            curve_iSp = curve_iL.Cut(0, Ts / 2, 1);
            curve_iSs = curve_iL.Cut(t0, t0 + Ts / 2, n / No);
            math_vSs = Vin;
            math_vSp = Vo;
            curve_iCf = curve_iSs.Copy(1, 0, -Io);
            //计算有效值
            math_ILrms = curve_iL.CalcRMS();
            math_ICfrms = curve_iCf.CalcRMS();

            math_VCrp = VCrp;
            math_ILp = ILp;

            //提取数据
            //Point[] data = currentInductor.GetData();
            //Console.WriteLine("-----------" + currentInductor.Name + "_x------------");
            //for (int i = 0; i < data.Length; i++)
            //{
            //    Console.WriteLine(data[i].X);
            //}

            //Console.WriteLine("-----------" + currentInductor.Name + "_y------------");
            //for (int i = 0; i < data.Length; i++)
            //{
            //    Console.WriteLine(data[i].Y);
            //}

            //data = voltageCapacitor.GetData();
            //Console.WriteLine("-----------" + voltageCapacitor.Name + "_x------------");
            //for (int i = 0; i < data.Length; i++)
            //{
            //    Console.WriteLine(data[i].X);
            //}

            //Console.WriteLine("-----------" + voltageCapacitor.Name + "_y------------");
            //for (int i = 0; i < data.Length; i++)
            //{
            //    Console.WriteLine(data[i].Y);
            //}
        }

        /// <summary>
        /// 准备评估所需的电路参数
        /// </summary>
        public override void Prepare()
        {
            //计算电路参数
            DesignCircuitParam();

            double ILmax = 0; //谐振电感电流最大值
            double ILrms_max = 0; //谐振电感电流有效值最大值
            double VCrmax = 0; //谐振电容电压最大值
            double ICfrms_max = 0; //滤波电容电流有效值最大值
            
            int n = Config.CGC_POWER_RATIO.Length;

            for (int j = 0; j < n; j++)
            {
                math_P = math_Pfull * Config.CGC_POWER_RATIO[j]; //改变负载
                Simulate(); //进行对应负载下的波形模拟
                //Graph graph = new Graph();
                //graph.Add(curve_iSp, "iP");
                //graph.Add(curve_iSs, "iS");
                //graph.Draw();
                ILmax = Math.Max(ILmax, math_ILp);
                ILrms_max = Math.Max(ILrms_max, math_ILrms);
                VCrmax = Math.Max(VCrmax, math_VCrp);
                ICfrms_max = Math.Max(ICfrms_max, math_ICfrms);

                //设置元器件的电路参数（用于评估）
                primaryDualModule.AddEvalParameters(0, j, math_vSp, curve_iSp, curve_iSp);
                Curve iD = curve_iSs.Copy(-1);
                secondaryDualModule.AddEvalParameters(0, j, math_vSs, iD, iD);
                resonantInductor.AddEvalParameters(0, j, math_ILrms, math_ILp * 2);
                transformer.AddEvalParameters(0, j, math_ILrms, math_ILp * 2);
                resonantCapacitor.AddEvalParameters(0, j, math_ILrms);
                filteringCapacitor.AddEvalParameters(0, j, math_ICfrms);
            }

            //若认为谐振电感集成在变压器中，则不考虑额外谐振电感
            //if (this.isLeakageInductanceIntegrated)
            //{
            //    this.deviceInductorNum = 0;
            //}

            //设置元器件的设计条件
            primaryDualModule.SetConditions(math_VSpmax, ILmax, math_fs);
            secondaryDualModule.SetConditions(math_VSsmax, math_n * ILmax, math_fs);
            resonantInductor.SetConditions(math_Lr, ILmax, math_fs);
            transformer.SetConditions(math_Pfull, ILmax, math_fs, math_n, math_No, math_ψ); //FIXME 磁链是否会变化？
            resonantCapacitor.SetConditions(math_Cr, VCrmax, ILrms_max);
            filteringCapacitor.SetConditions(200 * 1e-6, math_VCfmax, ICfrms_max); //TODO 滤波电容的设计
        }

        /// <summary>
		/// 计算电路参数
		/// </summary>
		public override void Calc()
        {
            math_P = converter.Math_P;
            Simulate();
            //设置元器件的电路参数
            primaryDualModule.SetParameters(math_vSp, curve_iSp, curve_iSp, math_fs);
            Curve iD = curve_iSs.Copy(-1);
            secondaryDualModule.SetParameters(math_vSs, iD, iD, math_fs);
            resonantInductor.SetParameters(math_ILrms, math_ILp * 2, math_fs);
            transformer.SetParameters(math_ILrms, math_ILp * 2, math_fs, math_ψ);
            resonantCapacitor.SetParameters(math_ILrms);
            filteringCapacitor.SetParameters(math_ICfrms);
        }
    }
}
