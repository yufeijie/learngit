using MathWorks.MATLAB.NET.Arrays;
using PV_analysis.Components;
using PV_analysis.Converters;
using System;
using SingleMOS = PV_analysis.Components.SingleMOS;

namespace PV_analysis.Topologys
{
    internal class DTCSRC : Topology
    {
        private IsolatedDCDCConverter converter; //所属变换器

        //特殊参数
        private bool isLeakageInductanceIntegrated = true; //是否认为谐振电感集成在变压器中

        //给定参数
        private double math_Vinmin; //输入电压最小值
        private double math_Vinmax; //输入电压最大值
        private double math_Vin; //输入电压
        private double math_Vo; //输出电压
        private double math_Q; //品质因数
        private double math_fr; //谐振频率
        
        //主电路元件参数
        private double math_fs; //开关频率
        private double math_ψ; //磁链
        private double math_n; //变压器变比
        private double math_Lr; //谐振电感值
        private double math_Cr; //谐振电容值
        private double math_ILrms; //电感电流有效值
        private double math_ILp; //电感电流峰值
        private double math_VCrp; //电容电压峰值
        private double math_ICfrms; //滤波电容电流有效值

        //电压、电流波形
        private double math_vSp; //原边开关器件电压
        private double math_vSs; //副边开关器件电压
        private double math_vDs; //副边二极管电压
        private Curve curve_iSp; //原边开关器件电流波形
        private Curve curve_iSs; //副边开关器件电流波形
        private Curve curve_iDs; //副边二极管电流波形
        private Curve curve_iL; //谐振电感电流波形
        private Curve curve_vCr; //谐振电容电压波形
        private Curve curve_iCf; //滤波电容电流波形

        //元器件
        private DualModule primaryDualModule;
        private SingleMOS single;
        private DualDiodeModule secondaryDualDiodeModule;
        private ACInductor resonantInductor;
        private Transformer transformer;
        private Capacitor resonantCapacitor;
        private Capacitor filteringCapacitor;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="converter">所属变换器</param>
        public DTCSRC(IsolatedDCDCConverter converter)
        {
            //获取设计规格
            this.converter = converter;
            math_Pfull = converter.Math_Psys / converter.PhaseNum / converter.Number;
            math_Vinmin = converter.Math_Vin_min;
            math_Vinmax = converter.Math_Vin_max;
            math_Vo = converter.Math_Vo;
            math_Q = converter.Math_Q;
            math_fr = converter.Math_fs; //以开关频率作为谐振频率

            //初始化元器件
            primaryDualModule = new DualModule(2)
            {
                Name = "原边开关管"
            };
            single = new SingleMOS(2)
            {
                Name = "副边开关管"
            };
            secondaryDualDiodeModule = new DualDiodeModule(1)
            {
                Name = "副边二极管"
            };
            resonantInductor = new ACInductor(1)
            {
                Name = "谐振电感"
            };
            transformer = new Transformer(1)
            {
                Name = "变压器"
            };
            resonantCapacitor = new ResonantCapacitor(1)
            {
                Name = "谐振电容"
            };
            filteringCapacitor = new FilteringCapacitor(1)
            {
                Name = "滤波电容"
            };
            componentGroups = new Component[1][];
            
            if (isLeakageInductanceIntegrated)
            {
                componentGroups[0] = new Component[] { primaryDualModule, single, secondaryDualDiodeModule, transformer, resonantCapacitor, filteringCapacitor };
                components = new Component[] { primaryDualModule, single, secondaryDualDiodeModule, transformer, resonantCapacitor, filteringCapacitor };
            }
            else
            {
                componentGroups[0] = new Component[] { primaryDualModule, single, secondaryDualDiodeModule, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
                components = new Component[] { primaryDualModule, single, secondaryDualDiodeModule, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
            }
        }

        /// <summary>
        /// 获取拓扑名
        /// </summary>
        /// <returns>拓扑名</returns>
        public override string GetName()
        {
            return "DTCSRC";
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            double P = math_Pfull;
            double Vinmax = math_Vinmax;
            double Vo = math_Vo;
            double Q = math_Q;
            double fr = math_fr;

            double wr = 2 * Math.PI * fr; //谐振角速度
            double RL = Math.Pow(Vo, 2) / P; //负载等效电阻
            double n = Vinmax / Vo;
            double Zr = Q * 8 * Math.Pow(n / Math.PI, 2) * RL; //谐振阻抗

            math_Lr = Zr / wr;
            math_Cr = 1 / Zr / wr;
            math_n = n;
        }

        /// <summary>
        /// 计算电路参数，并模拟电压、电流波形
        /// </summary>
        private void Simulate()
        {
            double P = math_P;
            double Vin = math_Vin;
            double Vo = math_Vo;
            double fr = math_fr;
            double n = math_n;
            double Lr = math_Lr;
            double Cr = math_Cr;
            
            double M = n * Vo / Vin;
            double Zr = Math.Sqrt(Lr / Cr);
            double RL = Math.Pow(Vo, 2) / P;
            double Q = Zr / (Math.Pow(n, 2) * RL); //品质因数（仅用于计算，并非基波等效的品质因数）

            //标幺化
            double Vbase = Vo;
            double Ibase = Vbase / Zr;
            double fbase = fr;

            //求解Td和fs
            MWArray output = Formula.solve.solve_DTCSRC(Q, M);
            MWNumericArray result = (MWNumericArray)output;
            double Td_base = result[1].ToScalarDouble();
            double fs_base = result[2].ToScalarDouble();
            if (Td_base < 0 || Td_base > 0.5)
            {
                Console.WriteLine("Wrong Td!");
                System.Environment.Exit(-1);
            }
            if (fs_base < 0.75 || fs_base >= 100)
            {
                Console.WriteLine("Wrong fs!");
                System.Environment.Exit(-1);
            }

            if (fs_base > 1.5)
            {//定频控制，求解对应Td
                fs_base = 1.5;
                output = Formula.solve.solve_DTCSRC_Td(Q, M, fs_base);
                result = (MWNumericArray)output;
                Td_base = result.ToScalarDouble();
                if (Td_base < 0 || Td_base > 0.5)
                {
                    Console.WriteLine("Wrong Td!");
                    System.Environment.Exit(-1);
                }
            }
            double Ts = 1 / (fs_base * fbase);
            double Tbase = Ts;
            
            int mode = Formula.DTC_SRC_CCMflag(Td_base, fs_base, Q, M); //电流导通模式 0->DCM 1->CCM            

            double ILp = 0;
            double VCrp = 0;
            curve_iL = new Curve();
            curve_vCr = new Curve();
            double startTime = 0;
            double endTime = 1;
            double dt = (endTime - startTime) / Configuration.DEGREE;
            for (int i = 0; i <= Configuration.DEGREE; i++)
            {
                double t = startTime + dt * i;
                double iLr = Formula.DTC_SRC_ilr(t, Td_base, fs_base, Q, M, mode);
                double vCr = Formula.DTC_SRC_vcr(t, Td_base, fs_base, Q, M, mode);
                curve_iL.Add(Tbase * t, Ibase * iLr);
                curve_vCr.Add(Tbase * t, Vbase * vCr);
                //记录峰值
                ILp = Math.Max(ILp, Math.Abs(Ibase * iLr));
                VCrp = Math.Max(VCrp, Math.Abs(Vbase * vCr));
            }
            //补充特殊点（保证现有的开关器件损耗计算中，判断开通/关断/导通状态的部分正确） FIXME 更好的方法？
            double Td = Tbase * Td_base;
            double Te2 = Tbase * Formula.DTC_SRC_Te2(Td_base, fs_base, Q, M, mode);
            curve_iL.Order(0, 0);
            curve_iL.Order(Ts / 2, 0);
            //生成主电路元件波形
            curve_iSp = curve_iL.Cut(Te2, Te2 + Ts / 2, -1);
            curve_iSs = curve_iL.Cut(0, Td + Ts / 2, -n);
            curve_iDs = curve_iL.Cut(Td, Ts / 2, n);
            math_vSp = Vin;
            math_vSs = Vo;
            math_vDs = Vo;
            double Io = Vo / RL;
            curve_iCf = curve_iDs.Copy(1, 0, -Io);
            curve_iCf.Order(0, -Io);
            curve_iCf.Order(Td_base, -Io);
            //计算有效值
            math_ILrms = curve_iL.CalcRMS();
            math_ICfrms = curve_iCf.CalcRMS();

            math_fs = fs_base * fbase; //还原实际值;
            math_ψ = Formula.DTC_SRC_Ψm(Vin, Vo * n, Vbase, Ts, Td_base, fs_base, Q, M, mode);
            math_ILp = ILp;
            math_VCrp = VCrp;
        }

        /// <summary>
        /// 准备评估所需的电路参数
        /// </summary>
        public override void Prepare()
        {
            //计算电路参数
            DesignCircuitParam();
            int m = Configuration.voltageRatio.Length;
            int n = Configuration.powerRatio.Length;

            double ILmax = 0;
            double ILrms_max = 0;
            double VCrmax = 0;
            double ICfrms_max = 0;
            double fsmax = 0;
            double ψmax = 0;
                        
            //得到用于效率评估的不同输入电压与不同功率点的电路参数
            for (int i = 0; i < m; i++)
            {
                //Graph graph1 = new Graph();
                //Graph graph2 = new Graph();
                math_Vin = math_Vinmin + (math_Vinmax - math_Vinmin) * Configuration.voltageRatio[i];
                for (int j = 0; j < n; j++)
                {
                    math_P = math_Pfull * Configuration.powerRatio[j]; //改变负载
                    Simulate();
                    //graph1.Add(curve_iL, "iL");
                    //graph2.Add(curve_vCr, "vCr");
                    //记录最大值
                    ILmax = Math.Max(ILmax, math_ILp);
                    ILrms_max = Math.Max(ILrms_max, math_ILrms);
                    VCrmax = Math.Max(VCrmax, math_VCrp);
                    ICfrms_max = Math.Max(ICfrms_max, math_ICfrms);
                    fsmax = Math.Max(fsmax, math_fs);
                    ψmax = Math.Max(ψmax, math_ψ);

                    //设置元器件的电路参数（用于评估）
                    primaryDualModule.AddEvalParameters(i, j, math_vSp, math_vSp, curve_iSp, curve_iSp, math_fs);
                    single.AddEvalParameters(i, j, math_vSs, math_vSs, curve_iSs, math_fs);
                    secondaryDualDiodeModule.AddEvalParameters(i, j, math_vDs, math_vDs, curve_iDs, curve_iDs, math_fs);
                    resonantInductor.AddEvalParameters(i, j, math_ILrms, math_ILp * 2, math_fs);
                    transformer.AddEvalParameters(i, j, math_ILrms, math_ILrms * math_n, math_fs, math_ψ);
                    resonantCapacitor.AddEvalParameters(i, j, math_ILrms);
                    filteringCapacitor.AddEvalParameters(i, j, math_ICfrms);
                }
                //graph1.Draw();
                //graph2.Draw();
            }

            //若认为谐振电感集成在变压器中，则不考虑额外谐振电感
            //if (this.isLeakageInductanceIntegrated)
            //{
            //    this.deviceInductorNum = 0;
            //}

            //设置元器件的设计条件
            primaryDualModule.SetConditions(math_Vinmax, ILmax, fsmax); //TODO 电流取RMS最大值 or 最大值？
            single.SetConditions(math_Vo, math_n * ILmax, fsmax);
            secondaryDualDiodeModule.SetConditions(math_Vo, math_n * ILmax, fsmax);
            resonantInductor.SetConditions(math_Lr, ILmax, fsmax);
            transformer.SetConditions(math_P, ILmax, ILmax * math_n, fsmax, math_n, 1, ψmax); //FIXME 磁链是否会变化？
            resonantCapacitor.SetConditions(math_Cr, VCrmax, ILrms_max);
            filteringCapacitor.SetConditions(200 * 1e-6, math_Vo, ICfrms_max);
        }

        /// <summary>
		/// 计算电路参数
		/// </summary>
		public override void Calc()
        {
            math_P = converter.Math_P;
            math_Vin = converter.Math_Vin;
            Simulate();
            //设置元器件的电路参数
            primaryDualModule.SetParameters(math_vSp, math_vSp, curve_iSp, curve_iSp, math_fs);
            single.SetParameters(math_vSs, math_vSs, curve_iSs, math_fs);
            secondaryDualDiodeModule.SetParameters(math_vDs, math_vDs, curve_iDs, curve_iDs, math_fs);
            resonantInductor.SetParameters(math_ILrms, math_ILp * 2, math_fs);
            transformer.SetParameters(math_ILrms, math_ILrms * math_n, math_fs, math_ψ);
            resonantCapacitor.SetParameters(math_ILrms);
            filteringCapacitor.SetParameters(math_ICfrms);
        }
    }
}
