using PV_analysis.Converters;
using PV_analysis.Topologys;
using System;

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
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new MainForm());

            double Psys = 6e6;
            double Vin_min = 860;
            double Vin_max = 1300;
            double Vo = 1300;
            int[] numberRange = { 120 };
            int[] topologyRange = { 1 };
            double[] frequencyRange = { 50e3 };

            DCDCConverter converter = new DCDCConverter(Psys, Vin_min, Vin_max, Vo);

            foreach (int tp in topologyRange) //模块数变化
            {
                ThreeLevelBoost topology = new ThreeLevelBoost(converter);
                foreach (int n in numberRange)
                {
                    converter.Number = n;
                    foreach (double fs in frequencyRange)
                    {
                        converter.Math_fs = fs;
                        Console.WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                        topology.Design();
                    }
                }
            }
        }
    }
}
