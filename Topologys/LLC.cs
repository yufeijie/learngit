using PV_analysis.Components;
using PV_analysis.Converters;
using System;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// LLC拓扑
    /// </summary>
    internal class LLC : Topology
    {
        private IsolatedDCDCConverter converter; //所属变换器

        //基本电路参数
        private double math_Vin; //输入电压
        private double math_Vo; //输出电压预设值
        private int math_No; //副边个数
        private double math_fs; //开关频率
        private double math_Td; //死区时间
        private double math_Q; //品质因数
        private double math_k; //电感比Lm/Lr
        private double math_b = 1; //桥臂系数，半桥为2，全桥为1
        private double math_p = 1; //电平系数，三电平为2，两电平为1

        //主电路元件参数
        private double math_VSpmax; //原边开关器件电压应力
        private double math_VSsmax; //副边开关器件电压应力
        private double math_n; //变压器变比
        private double math_ψ; //磁链
        private double math_fr; //谐振频率
        private double math_Lr; //谐振电感值
        private double math_Lm; //励磁电感值
        private double math_ILrrms; //谐振电感电流有效值
        private double math_ILrmax; //谐振电感电流最大值
        private double math_Cr; //谐振电容值
        private double math_VCrmax; //谐振电容电压最大值
        private double math_VCfmax; //电容电压应力
        private double math_ICfrms; //滤波电容电流有效值
        private double math_ITsrms; //变压器副边电流最大值
        private double math_ITsmax; //变压器副边电流最大值

        //电压、电流波形
        private double math_vSp; //原边开关器件电压
        private double math_vSs; //副边开关器件电压
        private Curve curve_iSp; //原边开关器件电流波形
        private Curve curve_iSs; //副边开关器件电流波形
        private Curve curve_iLr; //谐振电感电流波形
        private Curve curve_vCr; //谐振电容电压波形
        private Curve curve_iCf; //滤波电容电流波形
        private Curve curve_iLm; //励磁电感波形
        private Curve curve_iTs; //变压器副边电流波形

        //元器件
        private DualModule primaryDualModule;
        private DualDiodeModule secondaryDualDiodeModule;
        private DCInductor resonantInductor;
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
            math_No = converter.Math_No;
            math_fs = converter.Math_fs;
            math_Td = math_fs < Configuration.SIC_SELECTION_FREQUENCY ? Configuration.IGBT_DEAD_TIME : Configuration.MOSFET_DEAD_TIME;
            math_Q = converter.Math_Q;
            math_k = converter.Math_k;

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
            resonantInductor = new DCInductor(1)
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
            if (Configuration.IS_RESONANT_INDUCTANCE_INTEGRATED)
            {
                components = new Component[] { primaryDualModule, secondaryDualDiodeModule, transformer, resonantCapacitor, filteringCapacitor };
                componentGroups[0] = new Component[] { primaryDualModule, secondaryDualDiodeModule, transformer, resonantCapacitor, filteringCapacitor };
            }
            else
            {
                components = new Component[] { primaryDualModule, secondaryDualDiodeModule, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
                componentGroups[0] = new Component[] { primaryDualModule, secondaryDualDiodeModule, resonantInductor, transformer, resonantCapacitor, filteringCapacitor };
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
            double No = math_No;
            double fs = math_fs;
            double Td = math_Td;
            double Q = math_Q;
            double k = math_k;
            double b = math_b;
            double p = math_p;

            double RL = No * Vo * Vo / P; //负载等效电阻
            double n = Vin / Vo / b; //变比
            double Ts = 1 / fs; //开关周期
            double Tr = Ts - 2 * Td; //谐振周期
            double fr = 1 / Tr; //谐振频率
            double wr = 2 * Math.PI * fr; //谐振角速度
            double Req = 8 * Math.Pow(n / Math.PI, 2) * RL / No; //谐振腔等效电阻
            double Zr = Req * Q; //谐振阻抗
            double Lr = Zr / wr;
            double Cr = 1 / Zr / wr;
            double Lm = k * Lr;
            
            math_n = n;
            math_fr = fr;
            math_Lr = Lr;
            math_Lm = Lm;
            math_Cr = Cr;
            math_ψ = 0.5 * Tr * n * Vo; //TODO 修正：实际上在不同负载下，输出电压会变化
            math_VSpmax = Vin / p;
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
            double Td = math_Td;
            double n = math_n;
            double fr = math_fr;
            double Lr = math_Lr;
            double Lm = math_Lm;
            double Cr = math_Cr;
            double b = math_b;
            double p = math_p;

            double wr = 2 * Math.PI * fr; //谐振角速度
            double Zr = Math.Sqrt(Lr / Cr); //谐振阻抗
            double Tr = 1 / fr; //谐振周期
            double Ts = 1 / fs; //开关周期

            curve_iLr = new Curve();
            curve_iLm = new Curve();
            curve_iTs = new Curve();
            curve_vCr = new Curve();

            double Vo_act = Vo / (1 - Tr * Td / (8 * Lm * Cr)); //实际输出电压
            double Io_act = P / Vo_act / No; //实际输出电流平均值
            double Vab = Vin / b; //原边逆变桥臂输出电压
            double Vtp = n * Vo_act; //变压器原边电压
            double ILrmax = Math.Sqrt(Math.Pow(Vtp * Tr / (4 * Lm), 2) + Math.Pow(No * Io_act * wr * Ts / (4 * n), 2)); //谐振电感电流最大值（轻载下不一定是最大值）
            double φ = Math.Atan(-n * Vtp * Tr / (No * Io_act * wr * Ts * Lm)); //谐振电感电流相角
            double ILmmax = Vtp * Tr / (4 * Lm); //死区时间内，认为励磁电感电流不变
            double VCrmax = ILrmax * Zr - Vab + Vtp; //谐振电容电压最大值

            math_ITsmax = 0;
            double startTime = 0;
            double endTime = Ts;
            double dt = (endTime - startTime) / Configuration.DEGREE;
            double t;
            for (int i = 0; i <= Configuration.DEGREE; i++)
            {
                t = startTime + dt * i;
                double iLm;
                double iLr;
                double vCr;
                double iTs;
                double q = 1;
                while (t >= Ts / 2)
                {
                    t -= Ts / 2;
                    q *= -1;
                }
                if (t <= Tr / 2)
                {
                    iLm = -ILmmax + Vtp / Lm * t;
                    iLr = ILrmax * Math.Sin(wr * t + φ);
                    vCr = Vab - Vtp - ILrmax * Zr * Math.Cos(wr * t + φ);
                }
                else
                {
                    iLm = ILmmax;
                    iLr = ILmmax;
                    vCr = Vab - Vtp + No * Io_act * Ts / (4 * n * Cr) + Vtp * Tr / (4 * Lm * Cr) * (t - Tr / 2);
                }
                iLr = iLr > iLm ? iLr : iLm; //修正轻载
                iTs = n / No * (iLr - iLm);
                iLm *= q;
                iLr *= q;
                vCr *= q;

                t = startTime + dt * i; //之前t可能已经改变
                curve_iLr.Add(t, iLr);
                curve_iLm.Add(t, iLm);
                curve_iTs.Add(t, iTs);
                curve_vCr.Add(t, vCr);

                math_ITsmax = Math.Max(math_ITsmax, iTs);
            }
            //补充特殊点（保证现有的开关器件损耗计算方法正确）
            double t1 = -φ / wr;
            curve_iLr.Order(t1, 0);
            curve_iLr.Order(t1 + Ts / 2, 0);
            //生成主电路元件波形
            curve_iSp = curve_iLr.Cut(0, Ts / 2, 1);
            curve_iSs = curve_iTs.Cut(0, Ts / 2, 1);
            math_vSp = Vin / p;
            math_vSs = Vo_act;
            curve_iCf = curve_iSs.Copy(1, 0, -Io_act);
            //计算有效值
            math_ILrrms = curve_iLr.CalcRMS();
            math_ICfrms = curve_iCf.CalcRMS();
            math_ITsrms = curve_iTs.CalcRMS();
            //记录最大值
            math_VCrmax = VCrmax;
            math_ILrmax = ILrmax;

            //Graph graph = new Graph();
            //graph.Add(curve_vCr, "vCr");
            //graph.Draw();

            //graph = new Graph();
            //graph.Add(curve_iLm, "iLm");
            //graph.Add(curve_iLr, "iLr");
            //graph.Add(curve_iSp, "iSp");
            //graph.Draw();

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
            double ITsmax = 0; //变压器副边电流最大值

            //Graph graph1 = new Graph();
            //Graph graph2 = new Graph();
            //Graph graph3 = new Graph();
            int n = Configuration.powerRatio.Length;
            for (int j = 0; j < n; j++)
            {
                math_P = math_Pfull * Configuration.powerRatio[j]; //改变负载
                Simulate(); //进行对应负载下的波形模拟
                //graph1.Add(curve_vCr, "vCr_" + j);
                //graph2.Add(curve_iLm, "iLm_" + j);
                //graph2.Add(curve_iLr, "iLr_" + j);
                //graph3.Add(curve_iTs, "iTs_" + j);
                ILrmax = Math.Max(ILrmax, math_ILrmax);
                ILrrms_max = Math.Max(ILrrms_max, math_ILrrms);
                VCrmax = Math.Max(VCrmax, math_VCrmax);
                ICfrms_max = Math.Max(ICfrms_max, math_ICfrms);
                ITsmax = Math.Max(ITsmax, math_ITsmax);

                //设置元器件的电路参数（用于评估）
                primaryDualModule.AddEvalParameters(0, j, math_vSp, curve_iSp, curve_iSp);
                secondaryDualDiodeModule.AddEvalParameters(0, j, math_vSs, curve_iSs, curve_iSs);
                resonantInductor.AddEvalParameters(0, j, math_ILrrms, math_ILrmax * 2);
                transformer.AddEvalParameters(0, j, math_ILrrms, math_ITsrms);
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
            secondaryDualDiodeModule.SetConditions(math_VSsmax, math_n / math_No * ILrmax, math_fs);
            resonantInductor.SetConditions(math_Lr, ILrmax, math_fs);
            transformer.SetConditions(math_P, ILrmax, ITsmax, math_fs, math_n, math_No, math_ψ); //FIXME 磁链是否会变化？
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
            resonantInductor.SetParameters(math_ILrrms, math_ILrmax * 2, math_fs);
            transformer.SetParameters(math_ILrrms, math_ITsrms, math_fs, math_ψ);
            resonantCapacitor.SetParameters(math_ILrrms);
            filteringCapacitor.SetParameters(math_ICfrms);
        }
    }
}
