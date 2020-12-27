using MathWorks.MATLAB.NET.Arrays;
using PV_analysis.Components;
using PV_analysis.Converters;
using System;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// SRC拓扑
    /// </summary>
    internal class SRC : Topology
    {
        private IsolatedDCDCConverter converter; //所属变换器

        //给定参数
        private double math_Vin; //输入电压
        private double math_Vo; //输出电压预设值
        private int math_No; //副边个数
        private double math_fs; //开关频率
        private double math_Td; //死区时间
        private double math_Q; //品质因数

        //主电路元件参数
        private double math_VSpmax; //原边开关器件电压应力
        private double math_VSsmax; //副边开关器件电压应力
        private double math_n; //变压器变比
        private double math_ψ; //磁链
        private double math_fr; //谐振频率
        private double math_Lr; //谐振电感值
        private double math_ILrrms; //谐振电感电流有效值
        private double math_ILrmax; //谐振电感电流最大值
        private double math_Cr; //谐振电容值
        private double math_VCrmax; //谐振电容电压最大值
        private double math_VCfmax; //电容电压应力
        private double math_ICfrms; //滤波电容电流有效值

        //电压、电流波形
        private double math_vSp; //原边开关器件电压
        private double math_vSs; //副边开关器件电压
        private Curve curve_iSp; //原边开关器件电流波形
        private Curve curve_iSs; //副边开关器件电流波形
        private Curve curve_iLr; //谐振电感电流波形
        private Curve curve_vCr; //谐振电容电压波形
        private Curve curve_iCf; //滤波电容电流波形

        //元器件
        private DualModule primaryDualModule;
        private DualDiodeModule secondaryDualDiodeModule;
        private SingleIGBT primarySingleIGBT;
        private SingleIGBT secondarySingleIGBT;
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
            math_fs = converter.Math_fs;
            math_Td = math_fs < Configuration.SIC_SELECTION_FREQUENCY ? Configuration.IGBT_DEAD_TIME : Configuration.MOSFET_DEAD_TIME;
            math_Q = converter.Math_Q;

            //初始化元器件
            primaryDualModule = new DualModule(2)
            {
                Name = "原边开关管",
                VoltageVariable = false
            };
            secondaryDualDiodeModule = new DualDiodeModule(2 * math_No)
            {
                Name = "副边二极管",
                VoltageVariable = false
            };
            primarySingleIGBT = new SingleIGBT(4)
            {
                Name = "原边开关管",
                VoltageVariable = false
            };
            secondarySingleIGBT = new SingleIGBT(4 * math_No)
            {
                Name = "副边开关管",
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

            componentGroups = new Component[2][];
            if (Configuration.IS_RESONANT_INDUCTANCE_INTEGRATED)
            {
                components = new Component[] { primaryDualModule, secondaryDualDiodeModule, primarySingleIGBT, secondarySingleIGBT, transformer, resonantCapacitor, filteringCapacitor };
                componentGroups[0] = new Component[] { primaryDualModule, secondaryDualDiodeModule, transformer, resonantCapacitor, filteringCapacitor };
                componentGroups[1] = new Component[] { primarySingleIGBT, secondarySingleIGBT, transformer, resonantCapacitor, filteringCapacitor };
            }
            else
            {
                components = new Component[] { primaryDualModule, secondaryDualDiodeModule, primarySingleIGBT, secondarySingleIGBT, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
                componentGroups[0] = new Component[] { primaryDualModule, secondaryDualDiodeModule, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
                componentGroups[1] = new Component[] { primarySingleIGBT, secondarySingleIGBT, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
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
            double fs = math_fs;
            double Td = math_Td;
            double Q = math_Q;
                        
            double RL = No * Math.Pow(Vo, 2) / P; //负载等效电阻
            double n = Vin / Vo; //变比
            double ws = 2 * Math.PI * fs;
            double Re = 8 * n * n * RL / (No * Math.PI * Math.PI);
            double Zr = Re * Q; //谐振阻抗

            //求解Vo_act和wr
            MWArray output = Formula.solve.solveSRC_wr(P, Vin, n, fs, Zr, Td);
            MWNumericArray result = (MWNumericArray)output;
            double Vo_act = result[1].ToScalarDouble(); //实际输出电压
            double wr = result[2].ToScalarDouble(); //谐振角速度
            if (Vo_act > Vo || Vo_act < Vo / 2)
            {
                Console.WriteLine("Wrong Vo!");
                Environment.Exit(-1);
            }

            if (wr > ws || wr < ws / 2)
            {
                Console.WriteLine("Wrong wr!");
                Environment.Exit(-1);
            }

            math_n = n;
            math_fr = wr / 2 / Math.PI; 
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
            double Vo = math_Vo;
            double No = math_No;
            double fs = math_fs;
            double n = math_n;
            double fr = math_fr;
            double Lr = math_Lr;
            double Cr = math_Cr;
            double Td = math_Td;

            double Ts = 1 / fs; //开关周期
            double wr = 2 * Math.PI * fr; //谐振角速度
            double Zr = Math.Sqrt(Lr / Cr); //谐振阻抗
            double φd = -wr * Td; //死区时间对应初相角

            //求解CCM下Vo_act和φ
            MWArray output = Formula.solve.solveSRC_CCM(P, Vin, n, fs, wr, Zr, Td);
            MWNumericArray result = (MWNumericArray)output;
            double Vo_act = result[1].ToScalarDouble(); //实际输出电压
            double φ1 = result[2].ToScalarDouble(); //初相角
            if (Vo_act > Vo || Vo_act < Vo * 0.5)
            {
                Console.WriteLine("Wrong Vo!");
                Environment.Exit(-1);
            }
            if (φ1 > 0)
            {
                Console.WriteLine("Wrong φ!");
                Environment.Exit(-1);
            }
            double φ2 = φ1;
            bool IsCCM = true;
            if (-φ1 / wr < Td)
            {
                IsCCM = false;
                //求解DCM下Vo_act和φ
                output = Formula.solve.solveSRC_DCM(P, Vin, n, fs, wr, Zr, Td);
                result = (MWNumericArray)output;
                Vo_act = result[1].ToScalarDouble(); //实际输出电压
                φ1 = result[2].ToScalarDouble(); //初相角
                if (Vo_act > Vo || Vo_act < Vo * 0.5)
                {
                    Console.WriteLine("Wrong Vo!");
                    Environment.Exit(-1);
                }
                if (φ1 > 0)
                {
                    Console.WriteLine("Wrong φ!");
                    Environment.Exit(-1);
                }
                φ2 = φd;
            }

            double VCrmax = P / (4 * n * Vo_act * fs * Cr);
            double Io_act = P / Vo_act / No;
            double t1 = -φ1 / wr;
            double A1 = (Vin + n * Vo_act + VCrmax) / Zr;
            double A2 = (Vin - n * Vo_act + VCrmax) / Zr;
            double ILrmax = A2;

            curve_iLr = new Curve();
            curve_vCr = new Curve();
            double startTime = 0;
            double endTime = Ts;
            double dt = (endTime - startTime) / Configuration.DEGREE;
            double t;
            for (int i = 0; i <= Configuration.DEGREE; i++)
            {
                t = startTime + dt * i;
                double iLr;
                double vCr;
                double q = 1;
                while (t >= Ts / 2)
                {
                    t -= Ts / 2;
                    q *= -1;
                }
                if (t <= t1)
                {
                    iLr = A1 * Math.Sin(wr * t + φ1);
                    vCr = -A1 * Zr * Math.Cos(wr * t + φ1) + Vin + n * Vo_act;
                }
                else if (t1 < t && t <= Td)
                {
                    iLr = 0;
                    vCr = -VCrmax;
                }
                else
                {
                    iLr = A2 * Math.Sin(wr * t + φ2);
                    vCr = -A2 * Zr * Math.Cos(wr * t + φ2) + Vin - n * Vo_act;
                }
                iLr *= q;
                vCr *= q;
                t = startTime + dt * i; //之前t可能已经改变
                curve_iLr.Add(t, iLr);
                curve_vCr.Add(t, vCr);
            }
            //补充特殊点（保证现有的开关器件损耗计算方法正确）
            curve_iLr.Order(t1, 0);
            curve_iLr.Order(t1 + Ts / 2, 0);
            curve_vCr.Order(t1, -VCrmax);
            curve_vCr.Order(t1 + Ts / 2, VCrmax);
            if (!IsCCM)
            {
                curve_iLr.Order(Td, 0);
                curve_iLr.Order(Td + Ts / 2, 0);
                curve_vCr.Order(Td, -VCrmax);
                curve_vCr.Order(Td + Ts / 2, VCrmax);
            }
            
            //生成主电路元件波形
            curve_iSp = curve_iLr.Cut(0, Ts / 2, 1);
            curve_iSs = curve_iLr.Cut(t1, t1 + Ts / 2, n / No);
            math_vSs = Vin;
            math_vSp = Vo;
            curve_iCf = curve_iSs.Copy(1, 0, -Io_act);
            //计算有效值
            math_ILrrms = curve_iLr.CalcRMS();
            math_ICfrms = curve_iCf.CalcRMS();
            //记录最大值
            math_VCrmax = VCrmax;
            math_ILrmax = ILrmax;

            //Graph graph = new Graph();
            //graph.Add(curve_vCr, "vCr");
            //graph.Draw();

            //graph = new Graph();
            //graph.Add(curve_iLr, "iLr");
            //graph.Add(curve_iSp, "iSp");
            //graph.Draw();

            //graph = new Graph();
            //graph.Add(curve_iSs, "iSs");
            //graph.Draw();

            //graph = new Graph();
            //graph.Add(curve_iCf, "iCf");
            //graph.Draw();

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

            double ILrmax = 0; //谐振电感电流最大值
            double ILrrms_max = 0; //谐振电感电流有效值最大值
            double VCrmax = 0; //谐振电容电压最大值
            double ICfrms_max = 0; //滤波电容电流有效值最大值

            //Graph graph1 = new Graph();
            //Graph graph2 = new Graph();
            //Graph graph3 = new Graph();
            int n = Configuration.powerRatio.Length;
            for (int j = 0; j < n; j++)
            {
                math_P = math_Pfull * Configuration.powerRatio[j]; //改变负载
                Simulate(); //进行对应负载下的波形模拟
                //graph1.Add(curve_vCr, "vCr_" + j);
                //graph2.Add(curve_iLr, "iLr_" + j);
                //graph3.Add(curve_iSs, "io_" + j);
                ILrmax = Math.Max(ILrmax, math_ILrmax);
                ILrrms_max = Math.Max(ILrrms_max, math_ILrrms);
                VCrmax = Math.Max(VCrmax, math_VCrmax);
                ICfrms_max = Math.Max(ICfrms_max, math_ICfrms);

                //设置元器件的电路参数（用于评估）
                primaryDualModule.AddEvalParameters(0, j, math_vSp, curve_iSp, curve_iSp);
                secondaryDualDiodeModule.AddEvalParameters(0, j, math_vSs, curve_iSs, curve_iSs);
                primarySingleIGBT.AddEvalParameters(0, j, math_vSp, curve_iSp);
                secondarySingleIGBT.AddEvalParameters(0, j, math_vSs, curve_iSs.Copy(-1));
                resonantInductor.AddEvalParameters(0, j, math_ILrrms, math_ILrmax * 2);
                transformer.AddEvalParameters(0, j, math_ILrrms, math_ILrrms * math_n / math_No);
                resonantCapacitor.AddEvalParameters(0, j, math_ILrrms);
                filteringCapacitor.AddEvalParameters(0, j, math_ICfrms);
            }
            //graph1.Draw();
            //graph2.Draw();
            //graph3.Draw();

            //若认为谐振电感集成在变压器中，则不考虑额外谐振电感
            //if (this.isLeakageInductanceIntegrated)
            //{
            //    this.deviceInductorNum = 0;
            //}

            //设置元器件的设计条件
            primaryDualModule.SetConditions(math_VSpmax, ILrmax, math_fs);
            secondaryDualDiodeModule.SetConditions(math_VSsmax, ILrmax * math_n / math_No, math_fs);
            primarySingleIGBT.SetConditions(math_VSpmax, ILrmax, math_fs);
            secondarySingleIGBT.SetConditions(math_VSsmax, ILrmax * math_n / math_No, math_fs);
            resonantInductor.SetConditions(math_Lr, ILrmax, math_fs);
            transformer.SetConditions(math_Pfull, ILrmax, ILrmax * math_n / math_No, math_fs, math_n, math_No, math_ψ); //FIXME 磁链是否会变化？
            resonantCapacitor.SetConditions(math_Cr, VCrmax, ILrrms_max);
            filteringCapacitor.SetConditions(200 * 1e-6, math_VCfmax, ICfrms_max); //TODO 容值
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
            secondaryDualDiodeModule.SetParameters(math_vSs, curve_iSs, curve_iSs, math_fs);
            primarySingleIGBT.SetParameters(math_vSp, curve_iSp, math_fs);
            secondarySingleIGBT.SetParameters(math_vSs, curve_iSs.Copy(-1), math_fs);
            resonantInductor.SetParameters(math_ILrrms, math_ILrmax * 2, math_fs);
            transformer.SetParameters(math_ILrrms, math_ILrrms * math_n / math_No, math_fs, math_ψ);
            resonantCapacitor.SetParameters(math_ILrrms);
            filteringCapacitor.SetParameters(math_ICfrms);
        }
    }
}
