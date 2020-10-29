using PV_analysis.Components;
using PV_analysis.Converters;
using System;
using static PV_analysis.Curve;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// LLC拓扑
    /// </summary>
    internal class LLC : Topology
    {
        //可选参数
        private bool isLeakageInductanceIntegrated = true; //是否认为谐振电感集成在变压器中
        //private static readonly double math_kIrip = 0.2; //电流纹波系数
        //private static readonly double math_kVrip = 0.1; //电压纹波系数

        private IsolatedDCDCConverter converter; //所属变换器

        //基本电路参数
        private double math_Vin; //输入电压
        private double math_Vo; //输出电压预设值
        private double math_Q; //品质因数
        private double math_k; //=Lm/Lr
        private double math_fr; //谐振频率
        private double math_fs; //开关频率

        //主电路元件参数
        private double math_VSpmax; //原边开关器件电压应力
        private double math_VSsmax; //副边开关器件电压应力
        private double math_n; //变压器变比
        private double math_ψ; //磁链
        private double math_Lr; //谐振电感值
        private double math_Lm; //励磁电感值
        private double math_ILrrms; //谐振电感电流有效值
        private double math_ILrp; //谐振电感电流峰值
        private double math_Cr; //谐振电容值
        private double math_VCrp; //谐振电容电压峰值
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
        private DualModule secondaryDualModule; //TODO 此处应为二极管
        private Inductor resonantInductor;
        private Transformer transformer;
        private Capacitor resonantCapacitor;
        private Capacitor filteringCapacitor;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="converter">所属变换器</param>
        public LLC(IsolatedDCDCConverter converter)
        {
            //获取设计规格
            this.converter = converter;
            math_Pfull = converter.Math_Psys / converter.PhaseNum / converter.Number;
            math_Vin = converter.Math_Vin;
            math_Vo = converter.Math_Vo;
            math_fr = converter.Math_fr;
            math_Q = converter.Math_Q;
            math_k = 5;

            //初始化元器件
            primaryDualModule = new DualModule(2)
            {
                Name = "原边开关管",
                VoltageVariable = false
            };
            secondaryDualModule = new DualModule(2)
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
            filteringCapacitor = new FilteringCapacitor(1)
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
            return "LLC";
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            double P = math_Pfull;
            double Vin = math_Vin;
            double Vo = math_Vo;
            double Q = math_Q;
            double k = math_k;
            double fr = math_fr;

            double Tr = 1 / fr; //谐振周期
            double Td = Tr / 50; //死区时间
            double Ts = Tr + Td; //开关周期
            double fs = 1 / Ts; //开关频率
            double wr = 2 * Math.PI * fr; //谐振角速度
            double RL = Math.Pow(Vo, 2) / P; //负载等效电阻
            double n = Vin / Vo; //变比
            double Zr = Q * 8 * Math.Pow(n / Math.PI, 2) * RL; //谐振阻抗

            math_fs = fs;
            math_n = n;
            math_Lr = Zr / wr;
            math_Lm = k * math_Lr;
            math_Cr = 1 / Zr / wr;
            math_ψ = 0.5 * Vo * n * Tr; //TODO 修正：实际上在不同负载下，输出电压会变化
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
            double fr = math_fr;
            double fs = math_fs;
            double n = math_n;
            double Lr = math_Lr;
            double Lm = math_Lm;
            double Cr = math_Cr;

            double wr = 2 * Math.PI * fr; //谐振角速度
            double Zr = Math.Sqrt(Lr / Cr); //谐振阻抗
            double Tr = 1 / fr; //谐振周期
            double Ts = 1 / fs; //开关周期
            double Td = Ts - Tr; //死区时间

            double Vo = Vin / (n * (1 - Math.PI / (8 * Lm) * Zr * Td)); //实际输出电压
            double Io = P / Vo; //输出电流平均值
            double ILrp = Math.Sqrt(Math.Pow(P * wr * Ts / (4 * n * Vo), 2) + Math.Pow(n * Vo * Tr / (4 * Lm), 2));
            double VCrp = -Vin + n * Vo + Zr * ILrp;
            double ILm = n * Vo * Tr / (4 * Lm); //环流时，认为励磁电感电流不变
            double φ = Math.Atan(-n * n * Vo * Vo * Tr / (P * wr * Lm * Ts));

            curve_iLr = new Curve();
            Curve curve_iLm = new Curve();
            Curve curve_io = new Curve();
            curve_vCr = new Curve();
            double startTime = 0;
            double endTime = Ts;
            double dt = (endTime - startTime) / Config.DEGREE;
            double t;
            for (int i = 0; i <= Config.DEGREE; i++)
            {
                t = startTime + dt * i;
                double iLm;
                double iLr;
                double vCr;
                double io;
                double k = 1;
                while (t >= Ts / 2)
                {
                    t -= Ts / 2;
                    k *= -1;
                }
                if (t <= Tr / 2)
                {
                    iLm = -ILm + n * Vo / Lm * t;
                    iLr = ILrp * Math.Sin(wr * t + φ);
                    io = iLr - iLm;
                    vCr = Vin - n * Vo - Zr * ILrp * Math.Cos(wr * t + φ);
                }
                else
                {
                    iLm = ILm;
                    iLr = ILm;
                    vCr = Vin - n * Vo + P * Ts / (4 * n * Cr * Vo) + Math.PI * n * Vo * Zr / (2 * Lm) * (t - Tr / 2);
                }
                io = n * (iLr - iLm);
                iLm *= k;
                iLr *= k;
                vCr *= k;

                t = startTime + dt * i; //之前t可能已经改变
                curve_iLr.Add(t, iLr);
                curve_iLm.Add(t, iLm);
                curve_io.Add(t, io);
                curve_vCr.Add(t, vCr);
            }
            //补充特殊点（保证现有的开关器件损耗计算方法正确）
            double t0 = -φ / wr;
            curve_iLr.Order(t0, 0);
            curve_iLr.Order(t0 + Ts / 2, 0);
            curve_vCr.Order(t0, -VCrp);
            curve_vCr.Order(t0 + Ts / 2, VCrp);
            //生成主电路元件波形
            curve_iSp = curve_iLr.Cut(0, Ts / 2, 1);
            curve_iSs = curve_io.Cut(0, Ts / 2, 1);
            math_vSs = Vin;
            math_vSp = Vo;
            curve_iCf = curve_iSs.Copy(1, 0, -Io);
            //计算有效值
            math_ILrrms = curve_iLr.CalcRMS();
            math_ICfrms = curve_iCf.CalcRMS();

            math_VCrp = VCrp;
            math_ILrp = ILrp;

            Graph graph = new Graph();
            //graph.Add(curve_vCr, "vCr");
            //graph.Draw();

            //graph = new Graph();
            graph.Add(curve_iLm, "iLm");
            graph.Add(curve_iLr, "iLr");
            //graph.Add(curve_iSp, "iSp");
            graph.Draw();

            //graph = new Graph();
            //graph.Add(curve_io, "io");
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
            
            int n = Config.CGC_POWER_RATIO.Length;

            for (int j = 0; j < n; j++)
            {
                math_P = math_Pfull * Config.CGC_POWER_RATIO[j]; //改变负载
                Simulate(); //进行对应负载下的波形模拟
                //Graph graph = new Graph();
                //graph.Add(currentSwitch_P, "iP");
                //graph.Add(currentSwitch_S, "iS");
                //graph.Draw();
                ILrmax = Math.Max(ILrmax, math_ILrp);
                ILrrms_max = Math.Max(ILrrms_max, math_ILrrms);
                VCrmax = Math.Max(VCrmax, math_VCrp);
                ICfrms_max = Math.Max(ICfrms_max, math_ICfrms);

                //设置元器件的电路参数（用于评估）
                primaryDualModule.AddEvalParameters(0, j, math_vSp, curve_iSp, curve_iSp);
                Curve iD = curve_iSs.Copy(-1);
                secondaryDualModule.AddEvalParameters(0, j, math_vSs, iD, iD);
                resonantInductor.AddEvalParameters(0, j, math_ILrrms, math_ILrp * 2);
                transformer.AddEvalParameters(0, j, math_ILrrms, math_ILrp * 2);
                resonantCapacitor.AddEvalParameters(0, j, math_ILrrms);
                filteringCapacitor.AddEvalParameters(0, j, math_ICfrms);
            }

            //若认为谐振电感集成在变压器中，则不考虑额外谐振电感
            //if (this.isLeakageInductanceIntegrated)
            //{
            //    this.deviceInductorNum = 0;
            //}

            //设置元器件的设计条件
            primaryDualModule.SetConditions(math_VSpmax, ILrmax, math_fs);
            secondaryDualModule.SetConditions(math_VSsmax, math_n * ILrmax, math_fs);
            resonantInductor.SetConditions(math_Lr, ILrmax, math_fs);
            transformer.SetConditions(math_P, ILrmax, math_fs, math_n, math_ψ); //FIXME 磁链是否会变化？
            resonantCapacitor.SetConditions(math_Cr, VCrmax, ILrrms_max);
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
            resonantInductor.SetParameters(math_ILrrms, math_ILrp * 2, math_fs);
            transformer.SetParameters(math_ILrrms, math_ILrp * 2, math_fs, math_ψ);
            resonantCapacitor.SetParameters(math_ILrrms);
            filteringCapacitor.SetParameters(math_ICfrms);
        }
    }
}
