using PV_analysis.Converters;
using System;
using System.Collections.Generic;

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

            Fomula.Init();
            //EvaluateDCDCconverter();
            EvaluateIsolatedDCDCconverter();
        }

        public static void EvaluateDCDCconverter()
        {
            double Psys = 6e6;
            double Vin_min = 860;
            double Vin_max = 1300;
            double Vo = 1300;
            int[] numberRange = GenerateNumberRange(1, 120);
            string[] topologyRange = { "ThreeLevelBoost", "TwoLevelBoost", "InterleavedBoost" };
            double[] frequencyRange = GenerateFrequencyRange(1e3, 50e3);

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

            Data.Record(converter.ParetoDesignList);
            Data.RecordAll(converter.AllDesignList);
        }

        public static void EvaluateIsolatedDCDCconverter()
        {
            double Psys = 6e6;
            //double Vin_min = 860;
            //double Vin_max = 1300;
            double Vin = 1300;
            double Vo = 1300;
            double Q = 1;
            int[] numberRange = GenerateNumberRange(20, 20);
            string[] topologyRange = { "SRC" };
            double[] frequencyRange = GenerateFrequencyRange(25e3, 25e3);

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

            Data.Record(converter.ParetoDesignList);
            Data.RecordAll(converter.AllDesignList);
        }

        /// <summary>
        /// 生成可用模块数序列
        /// </summary>
        /// <param name="min">最少模块数</param>
        /// <param name="max">最多模块数</param>
        /// <returns>可用模块数序列</returns>
        public static int[] GenerateNumberRange(int min, int max)
        {
            List<int> numberRange = new List<int>();
            int n = min;
            while (n <= max)
            {
                numberRange.Add(n);
                n++;
            }
            return numberRange.ToArray();
        }

        /// <summary>
        /// 生成可用模块数序列
        /// </summary>
        /// <param name="min">最少模块数</param>
        /// <param name="max">最多模块数</param>
        /// <param name="step">间隔</param>
        /// <returns>可用模块数序列</returns>
        public static int[] GenerateNumberRange(int min, int max, int step)
        {
            List<int> numberRange = new List<int>();
            int n = min;
            while (n <= max)
            {
                numberRange.Add(n);
                n += step;
            }
            return numberRange.ToArray();
        }

        /// <summary>
        /// 生成可用频率序列
        /// </summary>
        /// <param name="min">最低频率</param>
        /// <param name="max">最高频率</param>
        /// <returns>可用频率序列</returns>
        public static double[] GenerateFrequencyRange(double min, double max)
        {
            List<double> frequencyRange = new List<double>();
            double f = min;
            while (f <= max)
            {
                frequencyRange.Add(f);
                if (f < 20e3)
                {
                    f += 1e3;
                }
                else
                {
                    if (f < 100e3)
                    {
                        f += 5e3;
                    }
                    else
                    {
                        f += 10e3;
                    }
                }
            }
            return frequencyRange.ToArray();
        }

        /// <summary>
        /// 生成可用频率序列
        /// </summary>
        /// <param name="min">最低频率</param>
        /// <param name="max">最高频率</param>
        /// <param name="step">间隔</param>
        /// <returns>可用频率序列</returns>
        public static double[] GenerateFrequencyRange(double min, double max, double step)
        {
            List<double> frequencyRange = new List<double>();
            double f = min;
            while (f <= max)
            {
                frequencyRange.Add(f);
                f += step;
            }
            return frequencyRange.ToArray();
        }
    }
}
