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
    }
}
