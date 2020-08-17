using PV_analysis.Converters;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PV_analysis
{
    internal static class Start
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        public static void Main()
        {
            //生成窗体
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

            //EvaluateDCDCConverter();
            //EvaluateIsolatedDCDCConverter();
            //EvaluateIsolatedDCDCConverter_TwoStage();
            //EvaluateDCACConverter();
            //EvaluateThreeStageSystem();
            //EvaluateTwoStageSystem();
        }

        public static void EvaluateThreeStageSystem()
        {
            Console.WriteLine("Start...");
            Console.WriteLine();

            Formula.Init();
            ConverterDesignList paretoDesignList = new ConverterDesignList();
            ConverterDesignList allDesignList = new ConverterDesignList { IsAll = true };

            //整体参数
            double Psys = 6e6; //架构总功率
            double Vpv_min = 860; //光伏板输出电压最小值
            double Vpv_max = 1300; //光伏板输出电压最大值
            double Vg = 35e3; //并网电压（线电压）
            double Vo = Vg / Math.Sqrt(3); //输出电压（并网相电压）
            double fg = 50; //并网频率
            double[] VbusRange = { 1300 }; //母线电压范围
            double phi = 0; //功率因数角(rad)

            //前级DC/DC参数
            int[] DCDC_numberRange = Function.GenerateNumberRange(1, 120); //可用模块数序列
            string[] DCDC_topologyRange = { "ThreeLevelBoost", "TwoLevelBoost", "InterleavedBoost" }; //可用拓扑序列
            double[] DCDC_frequencyRange = Function.GenerateFrequencyRange(1e3, 100e3); //可用开关频率序列

            //隔离DC/DC参数
            double isolatedDCDC_Q = 1; //品质因数预设值
            string[] isolatedDCDC_topologyRange = { "SRC" }; //可用拓扑序列
            double[] isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(1e3, 100e3); //可用谐振频率序列

            //DC/AC参数
            int[] DCAC_numberRange = Function.GenerateNumberRange(1, 40); //可用模块数序列，隔离DCDC与此同
            string[] DCAC_topologyRange = { "CHB" };
            string[] DCAC_modulationRange = { "PSPWM", "LSPWM" }; //可用调制方式序列
            double[] DCAC_frequencyRange = Function.GenerateFrequencyRange(10e3, 10e3);

            //系统设计
            foreach (double Vbus in VbusRange) //母线电压变化
            {
                Console.WriteLine("Now DC bus voltage = " + Vbus + ":");
                //前级DC/DC变换器设计
                Console.WriteLine("-------------------------");
                Console.WriteLine("Front-stage DC/DC converters design...");
                DCDCConverter DCDC = new DCDCConverter(Psys, Vpv_min, Vpv_max, Vbus);
                foreach (string tp in DCDC_topologyRange) //拓扑变化
                {
                    DCDC.CreateTopology(tp);
                    foreach (int n in DCDC_numberRange) //模块数变化
                    {
                        DCDC.Number = n;
                        foreach (double fs in DCDC_frequencyRange) //开关频率变化
                        {
                            DCDC.Math_fs = fs;
                            Console.WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                            DCDC.Design();
                        }
                    }
                }
                if (DCDC.AllDesignList.Size <= 0)
                {
                    continue;
                }
                foreach (int j in DCAC_numberRange) //目前只考虑一拖一
                {
                    //逆变器设计
                    Console.WriteLine("-------------------------");
                    Console.WriteLine("Inverters design...");
                    DCACConverter DCAC = new DCACConverter(Psys, Vo, fg, phi) { Number = j };
                    foreach (string tp in DCAC_topologyRange) //拓扑变化
                    {
                        DCAC.CreateTopology(tp);
                        foreach (string mo in DCAC_modulationRange) //拓扑变化
                        {
                            DCAC.Modulation = mo;
                            foreach (double fs in DCAC_frequencyRange) //谐振频率变化
                            {
                                DCAC.Math_fs = fs;
                                Console.WriteLine("Now topology=" + tp + ", n=" + j + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                                DCAC.Math_Vin = 0;
                                //inverter.setVoltageInputDef(inv_voltageInput); //FIXME
                                DCAC.Design();
                            }
                        }
                    }
                    if (DCAC.AllDesignList.Size <= 0)
                    {
                        continue;
                    }

                    //隔离DC/DC变换器设计
                    Console.WriteLine("-------------------------");
                    Console.WriteLine("Isolated DC/DC converters design...");
                    IsolatedDCDCConverter isolatedDCDC = new IsolatedDCDCConverter(Psys, Vbus, DCAC.Math_Vin, isolatedDCDC_Q) { Number = j };
                    foreach (string tp in isolatedDCDC_topologyRange) //拓扑变化
                    {
                        isolatedDCDC.CreateTopology(tp);
                        foreach (double fr in isolatedDCDC_resonanceFrequencyRange) //谐振频率变化
                        {
                            isolatedDCDC.Math_fr = fr;
                            Console.WriteLine("Now topology=" + tp + ", n=" + j + ", fs=" + string.Format("{0:N1}", fr / 1e3) + "kHz");
                            isolatedDCDC.Design();
                        }
                    }
                    if (isolatedDCDC.AllDesignList.Size <= 0)
                    {
                        continue;
                    }

                    //整合得到最终结果
                    Console.WriteLine("-------------------------");
                    Console.WriteLine("Inv num=" + j + ", DC bus voltage=" + Vbus + ", Combining...");
                    ConverterDesignList newDesignList = new ConverterDesignList();
                    newDesignList.Combine(DCDC.ParetoDesignList);
                    newDesignList.Combine(isolatedDCDC.ParetoDesignList);
                    newDesignList.Combine(DCAC.ParetoDesignList);
                    newDesignList.Transfer(new string[] { Vbus.ToString(), DCAC.Math_Vin.ToString() });
                    paretoDesignList.Merge(newDesignList); //记录Pareto最优设计
                    allDesignList.Merge(newDesignList); //记录所有设计
                }
                Console.WriteLine("=========================");
            }

            string[] conditionTitles = new string[]
            {
                "Total power",
                "PV min voltage",
                "PV max voltage",
                "Grid voltage",
                "Grid frequency(Hz)",
                "DC bus voltage range",
                "DCDC number range",
                "DCDC topology range",
                "DCDC frequency range(kHz)",
                "Isolated DCDC quality factor default",
                "Isolated DCDC topology range",
                "Isolated DCDC resonance frequency range(kHz)",
                "DCAC power factor angle(rad)",
                "DCAC number range",
                "DCAC topology range",
                "DCAC modulation range",
                "DCAC frequency range(kHz)"
            };

            string[] conditions = new string[]
            {
                Psys.ToString(),
                Vpv_min.ToString(),
                Vpv_max.ToString(),
                Vg.ToString(),
                fg.ToString(),
                Function.DoubleArrayToString(VbusRange),
                Function.IntArrayToString(DCDC_numberRange),
                Function.StringArrayToString(DCDC_topologyRange),
                Function.DoubleArrayToString(DCDC_frequencyRange),
                isolatedDCDC_Q.ToString(),
                Function.StringArrayToString(isolatedDCDC_topologyRange),
                Function.DoubleArrayToString(isolatedDCDC_resonanceFrequencyRange),
                phi.ToString(),
                Function.IntArrayToString(DCAC_numberRange),
                Function.StringArrayToString(DCAC_topologyRange),
                Function.StringArrayToString(DCAC_modulationRange),
                Function.DoubleArrayToString(DCAC_frequencyRange)
            };

            Data.Record("ThreeStageSystem_Pareto", conditionTitles, conditions, paretoDesignList);
            Data.Record("ThreeStageSystem_all", conditionTitles, conditions, allDesignList);
        }

        public static void EvaluateTwoStageSystem()
        {
            Console.WriteLine("Start...");
            Console.WriteLine();

            Formula.Init();
            ConverterDesignList paretoDesignList = new ConverterDesignList();
            ConverterDesignList allDesignList = new ConverterDesignList { IsAll = true };

            //整体参数
            double Psys = 6e6; //架构总功率
            double Vpv_min = 860; //光伏板输出电压最小值
            double Vpv_max = 1300; //光伏板输出电压最大值
            double Vg = 35e3; //并网电压（线电压）
            double Vo = Vg / Math.Sqrt(3); //输出电压（并网相电压）
            double fg = 50; //并网频率
            double[] VbusRange = { 1300 }; //母线电压范围
            double phi = 0; //功率因数角(rad)

            //隔离DC/DC参数
            double isolatedDCDC_Q = 1; //品质因数预设值
            string[] isolatedDCDC_topologyRange = { "DTCSRC" }; //可用拓扑序列
            double[] isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(25e3, 25e3); //可用谐振频率序列

            //DC/AC参数
            double DCAC_Vin = 1300; //逆变器直流侧电压
            int[] DCAC_numberRange = Function.GenerateNumberRange(20, 20); //可用模块数序列，隔离DCDC与此同
            string[] DCAC_topologyRange = { "CHB" };
            string[] DCAC_modulationRange = { "PSPWM", "LSPWM" }; //可用调制方式序列
            double[] DCAC_frequencyRange = Function.GenerateFrequencyRange(10e3, 10e3);

            foreach (int j in DCAC_numberRange) //目前只考虑一拖一
            {
                //隔离DC/DC变换器设计
                Console.WriteLine("-------------------------");
                Console.WriteLine("Isolated DC/DC converters design...");
                IsolatedDCDCConverter isolatedDCDC = new IsolatedDCDCConverter(Psys, Vpv_min, Vpv_max, DCAC_Vin, isolatedDCDC_Q) { Number = j };
                foreach (string tp in isolatedDCDC_topologyRange) //拓扑变化
                {
                    isolatedDCDC.CreateTopology(tp);
                    foreach (double fr in isolatedDCDC_resonanceFrequencyRange) //谐振频率变化
                    {
                        isolatedDCDC.Math_fr = fr;
                        Console.WriteLine("Now topology=" + tp + ", n=" + j + ", fs=" + string.Format("{0:N1}", fr / 1e3) + "kHz");
                        isolatedDCDC.Design();
                    }
                }
                if (isolatedDCDC.AllDesignList.Size <= 0)
                {
                    continue;
                }

                //逆变器设计
                Console.WriteLine("-------------------------");
                Console.WriteLine("Inverters design...");
                DCACConverter DCAC = new DCACConverter(Psys, Vo, fg, phi) { Number = j, Math_Vin = DCAC_Vin };
                foreach (string tp in DCAC_topologyRange) //拓扑变化
                {
                    DCAC.CreateTopology(tp);
                    foreach (string mo in DCAC_modulationRange) //拓扑变化
                    {
                        DCAC.Modulation = mo;
                        foreach (double fs in DCAC_frequencyRange) //谐振频率变化
                        {
                            DCAC.Math_fs = fs;
                            Console.WriteLine("Now topology=" + tp + ", n=" + j + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                            //inverter.setVoltageInputDef(inv_voltageInput); //FIXME
                            DCAC.Design();
                        }
                    }
                }
                if (DCAC.AllDesignList.Size <= 0)
                {
                    continue;
                }

                //整合得到最终结果
                Console.WriteLine("-------------------------");
                Console.WriteLine("Inv num=" + j + ", Combining...");
                ConverterDesignList newDesignList = new ConverterDesignList();
                newDesignList.Combine(isolatedDCDC.ParetoDesignList);
                newDesignList.Combine(DCAC.ParetoDesignList);
                newDesignList.Transfer(new string[] { });
                paretoDesignList.Merge(newDesignList); //记录Pareto最优设计
                allDesignList.Merge(newDesignList); //记录所有设计
            }

            string[] conditionTitles = new string[]
            {
                "Total power",
                "PV min voltage",
                "PV max voltage",
                "Grid voltage",
                "Grid frequency(Hz)",
                "Isolated DCDC quality factor default",
                "Isolated DCDC topology range",
                "Isolated DCDC resonance frequency range(kHz)",
                "DCAC input voltage",
                "DCAC power factor angle(rad)",
                "DCAC number range",
                "DCAC topology range",
                "DCAC modulation range",
                "DCAC frequency range(kHz)"
            };

            string[] conditions = new string[]
            {
                Psys.ToString(),
                Vpv_min.ToString(),
                Vpv_max.ToString(),
                Vg.ToString(),
                fg.ToString(),
                isolatedDCDC_Q.ToString(),
                Function.StringArrayToString(isolatedDCDC_topologyRange),
                Function.DoubleArrayToString(isolatedDCDC_resonanceFrequencyRange),
                DCAC_Vin.ToString(),
                phi.ToString(),
                Function.IntArrayToString(DCAC_numberRange),
                Function.StringArrayToString(DCAC_topologyRange),
                Function.StringArrayToString(DCAC_modulationRange),
                Function.DoubleArrayToString(DCAC_frequencyRange)
            };

            Data.Record("TwoStageSystem_Pareto", conditionTitles, conditions, paretoDesignList);
            Data.Record("TwoStageSystem_all", conditionTitles, conditions, allDesignList);
        }

        public static void EvaluateDCDCConverter()
        {
            double Psys = 6e6;
            double Vin_min = 860;
            double Vin_max = 1300;
            double Vo = 1300;
            int[] numberRange = Function.GenerateNumberRange(1, 120);
            string[] topologyRange = { "ThreeLevelBoost", "TwoLevelBoost", "InterleavedBoost" };
            double[] frequencyRange = Function.GenerateFrequencyRange(1e3, 50e3);

            DCDCConverter converter = new DCDCConverter(Psys, Vin_min, Vin_max, Vo);

            foreach (string tp in topologyRange) //拓扑变化
            {
                converter.CreateTopology(tp);
                foreach (int n in numberRange) //模块数变化
                {
                    converter.Number = n;
                    foreach (double fs in frequencyRange) //开关频率变化
                    {
                        converter.Math_fs = fs;
                        Console.WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                        converter.Design();
                    }
                }
            }

            string[] conditionTitles = new string[]
            {
                "Total power",
                "Minimum input voltage",
                "Maximum input voltage",
                "Output voltage",
                "Number range",
                "Topology range",
                "Resonance frequency range(kHz)"
            };

            string[] conditions = new string[]
            {
                Psys.ToString(),
                Vin_min.ToString(),
                Vin_max.ToString(),
                Vo.ToString(),
                Function.IntArrayToString(numberRange),
                Function.StringArrayToString(topologyRange),
                Function.DoubleArrayToString(frequencyRange)
            };

            Data.Record(converter.GetType().Name + "_Pareto", conditionTitles, conditions, converter.ParetoDesignList);
            Data.Record(converter.GetType().Name + "_all", conditionTitles, conditions, converter.AllDesignList);
        }

        public static void EvaluateIsolatedDCDCConverter()
        {
            Formula.Init();
            double Psys = 6e6;
            //double Vin_min = 860;
            //double Vin_max = 1300;
            double Vin = 1300;
            double Vo = 1300;
            double Q = 1;
            int[] numberRange = Function.GenerateNumberRange(20, 20);
            string[] topologyRange = { "SRC" };
            double[] frequencyRange = Function.GenerateFrequencyRange(25e3, 25e3);

            IsolatedDCDCConverter converter = new IsolatedDCDCConverter(Psys, Vin, Vo, Q);

            foreach (string tp in topologyRange) //拓扑变化
            {
                converter.CreateTopology(tp);
                foreach (int n in numberRange) //模块数变化
                {
                    converter.Number = n;
                    foreach (double fr in frequencyRange) //谐振频率变化
                    {
                        converter.Math_fr = fr;
                        Console.WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fr / 1e3) + "kHz");
                        converter.Design();
                    }
                }
            }

            string[] conditionTitles = new string[]
            {
                "Total power",
                "Input voltage",
                "Output voltage",
                "Quality factor",
                "Number range",
                "Topology range",
                "Resonance frequency range(kHz)"
            };

            string[] conditions = new string[]
            {
                Psys.ToString(),
                Vin.ToString(),
                Vo.ToString(),
                Q.ToString(),
                Function.IntArrayToString(numberRange),
                Function.StringArrayToString(topologyRange),
                Function.DoubleArrayToString(frequencyRange)
            };

            Data.Record(converter.GetType().Name + "_Pareto", conditionTitles, conditions, converter.ParetoDesignList);
            Data.Record(converter.GetType().Name + "_all", conditionTitles, conditions, converter.AllDesignList);
        }

        public static void EvaluateIsolatedDCDCConverter_TwoStage()
        {
            Formula.Init();
            double Psys = 6e6;
            double Vin_min = 860;
            double Vin_max = 1300;
            double Vo = 1300;
            double Q = 1;
            int[] numberRange = Function.GenerateNumberRange(20, 20);
            string[] topologyRange = { "DTCSRC" };
            double[] frequencyRange = Function.GenerateFrequencyRange(25e3, 25e3);

            IsolatedDCDCConverter converter = new IsolatedDCDCConverter(Psys, Vin_min, Vin_max, Vo, Q);

            foreach (string tp in topologyRange) //拓扑变化
            {
                converter.CreateTopology(tp);
                foreach (int n in numberRange) //模块数变化
                {
                    converter.Number = n;
                    foreach (double fr in frequencyRange) //谐振频率变化
                    {
                        converter.Math_fr = fr;
                        Console.WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fr / 1e3) + "kHz");
                        converter.Design();
                    }
                }
            }

            string[] conditionTitles = new string[]
            {
                "Total power",
                "Minimum input voltage",
                "Maximum input voltage",
                "Output voltage",
                "Quality factor",
                "Number range",
                "Topology range",
                "Resonance frequency range(kHz)"
            };

            string[] conditions = new string[]
            {
                Psys.ToString(),
                Vin_min.ToString(),
                Vin_max.ToString(),
                Vo.ToString(),
                Q.ToString(),
                Function.IntArrayToString(numberRange),
                Function.StringArrayToString(topologyRange),
                Function.DoubleArrayToString(frequencyRange)
            };

            Data.Record(converter.GetType().Name + "_Pareto", conditionTitles, conditions, converter.ParetoDesignList);
            Data.Record(converter.GetType().Name + "_all", conditionTitles, conditions, converter.AllDesignList);
        }

        public static void EvaluateDCACConverter()
        {
            double Psys = 6e6;
            double Vg = 35e3; //并网电压（线电压）
            double Vo = Vg / Math.Sqrt(3); //输出电压（并网相电压）
            double fg = 50; //并网频率
            double phi = 0; //功率因数角(rad)

            int[] numberRange = Function.GenerateNumberRange(20, 30);
            string[] topologyRange = { "CHB" };
            string[] modulationRange = { "PSPWM", "LSPWM" };
            double[] frequencyRange = Function.GenerateFrequencyRange(10e3, 10e3);

            DCACConverter converter = new DCACConverter(Psys, Vo, fg, phi);

            foreach (string tp in topologyRange) //拓扑变化
            {
                converter.CreateTopology(tp);
                foreach (string mo in modulationRange) //拓扑变化
                {
                    converter.Modulation = mo;
                    foreach (int n in numberRange) //模块数变化
                    {
                        converter.Number = n;
                        foreach (double fs in frequencyRange) //谐振频率变化
                        {
                            converter.Math_fs = fs;
                            Console.WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                            converter.Math_Vin = 0;
                            converter.Design();
                        }
                    }
                }
            }

            string[] conditionTitles = new string[]
            {
                "Total power",
                "Grid voltage",
                "Grid frequency(Hz)",
                "Power factor angle(rad)",
                "Number range",
                "Topology range",
                "Modulation range",
                "Frequency range(kHz)"
            };

            string[] conditions = new string[]
            {
                Psys.ToString(),
                Vg.ToString(),
                fg.ToString(),
                phi.ToString(),
                Function.IntArrayToString(numberRange),
                Function.StringArrayToString(topologyRange),
                Function.StringArrayToString(modulationRange),
                Function.DoubleArrayToString(frequencyRange)
            };

            Data.Record(converter.GetType().Name + "_Pareto", conditionTitles, conditions, converter.ParetoDesignList);
            Data.Record(converter.GetType().Name + "_all", conditionTitles, conditions, converter.AllDesignList);
        }
    }
}
