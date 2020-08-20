using PV_analysis.Converters;
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

            //LoadDCDCConverter();
            //LoadIsolatedDCDCConverter();
            //LoadIsolatedDCDCConverter_TwoStage();
            //LoadDCACConverter();
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

        public static void LoadDCDCConverter()
        {
            string[][] info = Data.Load("DCDCConverter_all_20200819_153509_760.xlsx", 48213);
            string[] conditions = info[0];
            string[] configs = info[1];
            double Psys = double.Parse(conditions[0]);
            double Vin_min = double.Parse(conditions[1]);
            double Vin_max = double.Parse(conditions[2]);
            double Vo = double.Parse(conditions[3]);
            DCDCConverter converter = new DCDCConverter(Psys, Vin_min, Vin_max, Vo);
            converter.Load(configs, 3); //数字为数组下标，读取信息后，下标位置会相应改变
            converter.Evaluate(); //进行评估
            converter.Operate();
        }

        public static void LoadIsolatedDCDCConverter()
        {
            Formula.Init();
            string[][] info = Data.Load("IsolatedDCDCConverter_Pareto_20200820_174738_294.xlsx", 1);
            string[] conditions = info[0];
            string[] configs = info[1];
            double Psys = double.Parse(conditions[0]);
            double Vin = double.Parse(conditions[1]);
            double Vo = double.Parse(conditions[2]);
            double Q = double.Parse(conditions[3]);
            IsolatedDCDCConverter converter = new IsolatedDCDCConverter(Psys, Vin, Vo, Q);
            converter.Load(configs, 3); //数字为数组下标，读取信息后，下标位置会相应改变
            converter.Evaluate(); //进行评估
            converter.Operate();
        }

        public static void LoadIsolatedDCDCConverter_TwoStage()
        {
            Formula.Init();
            string[][] info = Data.Load("IsolatedDCDCConverter_Pareto_20200820_231505_317.xlsx", 1);
            string[] conditions = info[0];
            string[] configs = info[1];
            double Psys = double.Parse(conditions[0]);
            double Vin_min = double.Parse(conditions[1]);
            double Vin_max = double.Parse(conditions[2]);
            double Vo = double.Parse(conditions[3]);
            double Q = double.Parse(conditions[4]);
            IsolatedDCDCConverter converter = new IsolatedDCDCConverter(Psys, Vin_min, Vin_max, Vo, Q);
            converter.Load(configs, 3); //数字为数组下标，读取信息后，下标位置会相应改变
            converter.Evaluate(); //进行评估
            converter.Operate();
        }

        public static void LoadDCACConverter()
        {
            Formula.Init();
            string[][] info = Data.Load("DCACConverter_Pareto_20200820_231626_630.xlsx", 2);
            string[] conditions = info[0];
            string[] configs = info[1];
            double Psys = double.Parse(conditions[0]);
            double Vg = double.Parse(conditions[1]);
            double fg = double.Parse(conditions[2]);
            double phi = double.Parse(conditions[3]);
            DCACConverter converter = new DCACConverter(Psys, Vg, fg, phi);
            converter.Load(configs, 3); //数字为数组下标，读取信息后，下标位置会相应改变
            converter.Evaluate(); //进行评估
            converter.Operate();
        }
    }
}
