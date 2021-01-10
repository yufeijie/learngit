using PV_analysis.Informations;
using System;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    /// <summary>
    /// 并网电感器设计（空心电抗器设计）
    /// </summary>
    internal class GridInductor : Magnetics
    {
        //限制条件
        private static readonly double kp = 0; //涡流损耗系数

        public int MultiNumber { get; set; } //模块数（评估时，将损耗、成本、体积分散到每个模块中去，以满足现有的计算方法）

        //器件参数
        private int Nc = 20; //层数
        private int wire; //绕线编号
        private int Wn; //并绕股数
        private int N; //匝数（考虑各层线圈完全相同）
        private double H; //线圈高度(mm)
        private double D = 3000; //线圈直径(mm)

        //设计条件
        private double math_L; //感值(H)
        private double math_Imax; //电流最大值(A)
        private double math_fg; //并网频率

        //电路参数
        private double math_Irms; //电感电流有效值(A)
        private double[,] math_Irms_eval = new double[5, 7]; //电感电流有效值（用于评估）

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类电感数量</param>
        public GridInductor(int number)
        {
            this.number = number;
        }

        /// <summary>
        /// 获取设计方案的配置信息标题
        /// </summary>
        /// <returns>配置信息标题</returns>
        public override string[] GetConfigTitles()
        {
            string[] data = { "同类器件数量", "层数", "绕线型号", "并绕股数", "匝数", "线圈高度(mm)", "线圈直径(mm)" };
            return data;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            return new string[] { number.ToString(), Nc.ToString(), GetWireType(wire), Wn.ToString(), N.ToString(), H.ToString(), D.ToString() };
        }

        /// <summary>
        /// 获取损耗分布
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public override List<Info> GetLossBreakdown()
        {
            List<Info> list = new List<Info>()
            {
                new Info(Name + "(铜损)", Math.Round(number * powerLossCu, 2)),
                new Info(Name + "(涡流损耗)", Math.Round(number * powerLossFe, 2)) //涡流损耗
            };
            return list;
        }

        /// <summary>
        /// 获取手动设计信息
        /// </summary>
        /// <returns>手动设计信息</returns>
        public override List<(MainForm.ControlType, string)> GetManualInfo()
        {
            List<(MainForm.ControlType, string)> list = new List<(MainForm.ControlType, string)>()
            {
                (MainForm.ControlType.Text, "层数"),
                (MainForm.ControlType.Wire, "绕线型号"),
                (MainForm.ControlType.Text, "并绕股数"),
                (MainForm.ControlType.Text, "匝数"),
                (MainForm.ControlType.Text, "线圈高度(mm)"),
                (MainForm.ControlType.Text, "线圈直径(mm)")
            };
            return list;
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public override void Load(string[] configs, ref int index)
        {
            number = int.Parse(configs[index++]);
            Nc = int.Parse(configs[index++]);
            wire = GetWireId(configs[index++]);
            Wn = int.Parse(configs[index++]);
            N = int.Parse(configs[index++]);
            H = double.Parse(configs[index++]);
            D = double.Parse(configs[index++]);
        }

        /// <summary>
        /// 设置设计条件
        /// </summary>
        /// <param name="L">感值</param>
        /// <param name="Imax">电流最大值</param>
        /// <param name="fg">并网频率</param>
        public void SetConditions(double L, double Imax, double fg)
        {
            math_L = L;
            math_Imax = Imax;
            math_fg = fg;
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="Irms">电感电流有效值</param>
        public void AddEvalParameters(int m, int n, double Irms)
        {
            math_Irms_eval[m, n] = Irms;
        }

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        protected override void SelectParameters(int m, int n)
        {
            math_Irms = math_Irms_eval[m, n];
        }

        /// <summary>
        /// 设置电路参数
        /// </summary>
        /// <param name="Irms">电感电流有效值</param>
        public void SetParameters(double Irms)
        {
            math_Irms = Irms;
        }

        /// <summary>
        /// 自动设计
        /// </summary>
        public override void Design()
        {
            //若感值为0则退出设计
            if (math_L == 0)
            {
                return;
            }

            for(int n = 10; n <= 20; n++)
            {
                Nc = n;
                //参数初始化
                double K = 1.08; //绝缘及导线间隙系数
                double J = Properties.Settings.Default.电流密度; //电流密度(A/cm^2)
                double Axbmin = math_Imax / Nc / J; //满足电流密度所需裸线面积(cm^2)
                double delta = Math.Sqrt(math_ρCu / (Math.PI * math_μ0 * math_μCu * math_fg)) * 1e2; //集肤深度(cm)

                //选取绕线
                for (int w = 0; w < Data.WireList.Count; w++)
                {
                    //只考虑漆包线
                    if (Data.WireList[w].Category != "Magnet")
                    {
                        continue;
                    }
                    //集肤深度验证
                    double r = Data.WireList[w].Math_Db / 2 * 0.1; //裸线半径(cm)
                    if (r > delta)
                    {
                        continue;
                    }
                    wire = w;
                    double Axb = Data.WireList[w].Math_Ab * 1e-3; //绕线裸线面积(cm^2)
                    Wn = (int)Math.Ceiling(Axbmin / Axb);
                    
                    double d = Data.WireList[w].Math_D; //绕线外径(mm)

                    for(int DD = 100; DD <= 4000; DD+= 100)
                    {
                        D = DD;
                        double a = 0.08 * D * D * 1e-2;
                        double b = -8 * K * d * 0.1 * Nc * math_L * 1e6;
                        double c = -3.5 * D * 0.1 * Nc * math_L * 1e6;

                        N = (int)Math.Round((-b + Math.Sqrt(b * b - 4 * a * c)) / (2 * a));
                        H = N * d;

                        //评估
                        Evaluate();
                        designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                    }
                }
            }
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        protected override void CalcCost()
        {
            costCore = 0;
            costWire = Math.PI * D * 1e-3 * N * Wn * Nc * Data.WireList[wire].Weight * Data.WireList[wire].Price / MultiNumber; //折算到每个模块中
            cost = costCore + costWire;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        protected override void CalcVolume()
        {
            volume = 2 * Math.PI * D * D / 4 * H / 1e6 / MultiNumber;  //折算到每个模块中
        }

        /// <summary>
        /// 计算损耗
        /// </summary>
        public override void CalcPowerLoss()
        {
            double Axb = Data.WireList[wire].Math_Ab * 1e-3; //绕线裸线面积(cm^2)
            double Rwire = math_ρCu * Math.PI * D * 1e-3 * N / (Wn * Axb * 1e-4) / Nc; //直流电阻
            powerLossCu = Math.Pow(math_Irms, 2) * Rwire / MultiNumber; //计算纹波铜损，折算到每个模块中
            powerLossFe = powerLossCu * kp;
            powerLoss = powerLossCu + powerLossFe;
        }
    }
}
