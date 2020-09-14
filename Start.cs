using PV_analysis.Converters;
using PV_analysis.Structures;
using System;
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

            //OptimizeDCDCConverter();
            //OptimizeIsolatedDCDCConverter();
            //OptimizeIsolatedDCDCConverter_TwoStage();
            //OptimizeDCACConverter();

            //Load("TwoLevelStructure_Pareto_20200821_112116_772.xlsx", 1);
        }

        public static void OptimizeDCDCConverter()
        {
            double Psys = 6e6;
            double Vin_min = 860;
            double Vin_max = 1300;
            double Vo = 1300;
            int[] numberRange = Function.GenerateNumberRange(1, 120);
            string[] topologyRange = { "ThreeLevelBoost", "TwoLevelBoost", "InterleavedBoost" };
            double[] frequencyRange = Function.GenerateFrequencyRange(1e3, 100e3);
            DCDCConverter converter = new DCDCConverter(Psys, Vin_min, Vin_max, Vo)
            {
                NumberRange = numberRange,
                TopologyRange = topologyRange,
                FrequencyRange = frequencyRange
            };
            converter.Optimize();
            converter.Save();
        }

        public static void OptimizeIsolatedDCDCConverter()
        {
            Formula.Init();
            double Psys = 6e6;
            double Vin = 1300;
            double Vo = 1300;
            double Q = 1;
            int[] numberRange = Function.GenerateNumberRange(20, 20);
            string[] topologyRange = { "SRC" };
            double[] frequencyRange = Function.GenerateFrequencyRange(25e3, 25e3);
            IsolatedDCDCConverter converter = new IsolatedDCDCConverter(Psys, Vin, Vo, Q)
            {
                NumberRange = numberRange,
                TopologyRange = topologyRange,
                FrequencyRange = frequencyRange
            };
            converter.Optimize();
            converter.Save();
        }

        public static void OptimizeIsolatedDCDCConverter_TwoStage()
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
            IsolatedDCDCConverter converter = new IsolatedDCDCConverter(Psys, Vin_min, Vin_max, Vo, Q)
            {
                NumberRange = numberRange,
                TopologyRange = topologyRange,
                FrequencyRange = frequencyRange
            };
            converter.Optimize();
            converter.Save();
        }

        public static void OptimizeDCACConverter()
        {
            double Psys = 6e6;
            double Vg = 35e3; //并网电压（线电压）
            double fg = 50; //并网频率
            double phi = 0; //功率因数角(rad)
            int[] numberRange = Function.GenerateNumberRange(20, 30);
            string[] topologyRange = { "CHB" };
            string[] modulationRange = { "PSPWM", "LSPWM" };
            double[] frequencyRange = Function.GenerateFrequencyRange(10e3, 10e3);
            DCACConverter converter = new DCACConverter(Psys, Vg, fg, phi)
            {
                NumberRange = numberRange,
                TopologyRange = topologyRange,
                ModulationRange = modulationRange,
                FrequencyRange = frequencyRange
            };
            converter.Optimize();
            converter.Save();
        }

        public static void Load(string name, int n)
        {
            string[][] info = Data.Load(name, n);
            string[] conditions = info[0];
            string[] configs = info[1];
            string obj = conditions[0];
            Converter converter;
            Structure structure;
            int index = 0;
            switch (obj)
            {
                case "DCDCConverter":
                    double Psys = double.Parse(conditions[1]);
                    double Vin_min = double.Parse(conditions[2]);
                    double Vin_max = double.Parse(conditions[3]);
                    double Vo = double.Parse(conditions[4]);
                    converter = new DCDCConverter(Psys, Vin_min, Vin_max, Vo);
                    converter.Load(configs, ref index); //数字为数组下标，读取信息后，下标位置会相应改变
                    converter.Evaluate(); //进行评估
                    break;

                case "IsolatedDCDCConverter":
                    Formula.Init();
                    Psys = double.Parse(conditions[1]);
                    double Vin = double.Parse(conditions[2]);
                    Vo = double.Parse(conditions[3]);
                    double Q = double.Parse(conditions[4]);
                    converter = new IsolatedDCDCConverter(Psys, Vin, Vo, Q);
                    converter.Load(configs, ref index); //数字为数组下标，读取信息后，下标位置会相应改变
                    converter.Evaluate(); //进行评估
                    break;

                case "IsolatedDCDCConverter(TwoStage)":
                    Formula.Init();
                    Psys = double.Parse(conditions[1]);
                    Vin_min = double.Parse(conditions[2]);
                    Vin_max = double.Parse(conditions[3]);
                    Vo = double.Parse(conditions[4]);
                    Q = double.Parse(conditions[5]);
                    converter = new IsolatedDCDCConverter(Psys, Vin_min, Vin_max, Vo, Q);
                    converter.Load(configs, ref index); //数字为数组下标，读取信息后，下标位置会相应改变
                    converter.Evaluate(); //进行评估
                    break;

                case "DCACConverter":
                    Psys = double.Parse(conditions[1]);
                    double Vg = double.Parse(conditions[2]);
                    double fg = double.Parse(conditions[3]);
                    double phi = double.Parse(conditions[4]);
                    converter = new DCACConverter(Psys, Vg, fg, phi);
                    converter.Load(configs, ref index); //数字为数组下标，读取信息后，下标位置会相应改变
                    converter.Evaluate(); //进行评估
                    break;

                case "ThreeLevelStructure":
                    Formula.Init();
                    Psys = double.Parse(conditions[1]);
                    double Vpv_min = double.Parse(conditions[2]);
                    double Vpv_max = double.Parse(conditions[3]);
                    Vg = double.Parse(conditions[4]);
                    fg = double.Parse(conditions[5]);
                    Q = double.Parse(conditions[6]);
                    phi = double.Parse(conditions[7]);
                    structure = new TwoLevelStructure()
                    {
                        Math_Psys = Psys,
                        Math_Vpv_min = Vpv_min,
                        Math_Vpv_max = Vpv_max,
                        Math_Vg = Vg,
                        Math_Vo = Vg / Math.Sqrt(3),
                        Math_fg = fg,
                        IsolatedDCDC_Q = Q,
                        Math_phi = phi,
                    };
                    structure.Load(configs, ref index); //数字为数组下标，读取信息后，下标位置会相应改变
                    structure.Evaluate(); //进行评估
                    break;

                case "TwoLevelStructure":
                    Formula.Init();
                    Psys = double.Parse(conditions[1]);
                    Vpv_min = double.Parse(conditions[2]);
                    Vpv_max = double.Parse(conditions[3]);
                    Vg = double.Parse(conditions[4]);
                    fg = double.Parse(conditions[5]);
                    Q = double.Parse(conditions[6]);
                    double DCAC_Vin_def = double.Parse(conditions[7]);
                    phi = double.Parse(conditions[8]);
                    structure = new TwoLevelStructure()
                    {
                        Math_Psys = Psys,
                        Math_Vpv_min = Vpv_min,
                        Math_Vpv_max = Vpv_max,
                        Math_Vg = Vg,
                        Math_Vo = Vg / Math.Sqrt(3),
                        Math_fg = fg,
                        IsolatedDCDC_Q = Q,
                        DCAC_Vin_def = DCAC_Vin_def,
                        Math_phi = phi,
                    };
                    structure.Load(configs, ref index); //数字为数组下标，读取信息后，下标位置会相应改变
                    structure.Evaluate(); //进行评估
                    break;
            }
        }
    }
}
