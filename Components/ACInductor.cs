using PV_analysis.Informations;
using System;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    /// <summary>
    /// 交流开气隙电感器，设计（无直流分量）-参考李静航SRC设计以及赵修科的《开关电源中磁性元器件》
    /// </summary>
    internal class ACInductor : Magnetics
    {
        //器件参数
        private int wire; //绕线编号
        private double lg; //气隙长度(cm)
        private int N; //匝数

        //设计条件
        private double math_L; //感值(H)
        private double math_Imax; //电流最大值(A)（原边电流最大值）
        private double math_fs_max; //最高开关频率

        //电路参数
        private double math_Irms; //电感电流有效值(A)
        private double math_Irip; //电感电流纹波(A) （峰峰值）
        private double math_fs; //开关频率(Hz)
        private double[,] math_Irms_eval = new double[5, 7]; //电感平均电流（用于评估）
        private double[,] math_Irip_eval = new double[5, 7]; //电感电流纹波（用于评估）
        private double[,] math_fs_eval = new double[5, 7]; //开关频率（用于评估）

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类电感数量</param>
        public ACInductor(int number)
        {
            this.number = number;
        }

        /// <summary>
        /// 获取设计方案的配置信息标题
        /// </summary>
        /// <returns>配置信息标题</returns>
        public override string[] GetConfigTitles()
        {
            string[] data = { "同类器件数量", "磁芯型号", "磁芯数", "气隙长度(cm)", "绕线型号", "匝数" };
            return data;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public override string[] GetConfigs()
        {
            return new string[] { number.ToString(), GetCoreType(), numberCore.ToString(), lg.ToString(), GetWireType(wire), N.ToString() };
        }

        /// <summary>
        /// 获取损耗分布
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public override List<Info> GetLossBreakdown()
        {
            List<Info> list = new List<Info>()
            {
                new Info(Name + "(Cu)", Math.Round(number * powerLossCu, 2)),
                new Info(Name + "(Fe)", Math.Round(number * powerLossFe, 2))
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
                (MainForm.ControlType.Core, "磁芯型号"),
                (MainForm.ControlType.Text, "磁芯数"),
                (MainForm.ControlType.Text, "气隙长度(cm)"),
                (MainForm.ControlType.Wire, "绕线型号"),
                (MainForm.ControlType.Text, "匝数")
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
            SetCoreType(configs[index++]);
            numberCore = int.Parse(configs[index++]);
            lg = double.Parse(configs[index++]);
            wire = GetWireId(configs[index++]);
            N = int.Parse(configs[index++]);
        }

        /// <summary>
        /// 设置设计条件
        /// </summary>
        /// <param name="L">感值</param>
        /// <param name="Imax">电流最大值</param>
        /// <param name="fs_max">最大开关频率</param>
        public void SetConditions(double L, double Imax, double fs_max)
        {
            math_L = L;
            math_Imax = Imax;
            math_fs_max = fs_max;
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="Irms">电感电流有效值</param>
        /// <param name="Irip">电感电流纹波</param>
        public void AddEvalParameters(int m, int n, double Irms, double Irip)
        {
            math_Irms_eval[m, n] = Irms;
            math_Irip_eval[m, n] = Irip;
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="Irms">电感电流有效值</param>
        /// <param name="Irip">电感电流纹波</param>
        /// <param name="fs">开关频率</param>
        public void AddEvalParameters(int m, int n, double Irms, double Irip, double fs)
        {
            math_Irms_eval[m, n] = Irms;
            math_Irip_eval[m, n] = Irip;
            frequencyVariable = true;
            math_fs_eval[m, n] = fs;
        }

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        protected override void SelectParameters(int m, int n)
        {
            math_Irms = math_Irms_eval[m, n];
            math_Irip = math_Irip_eval[m, n];
            if (frequencyVariable)
            {
                math_fs = math_fs_eval[m, n];
            }
            else
            {
                math_fs = math_fs_max;
            }
        }

        /// <summary>
        /// 设置电路参数
        /// </summary>
        /// <param name="Irms">电感电流有效值</param>
        /// <param name="Irip">电感电流纹波</param>
        /// <param name="fs">开关频率</param>
        public void SetParameters(double Irms, double Irip, double fs)
        {
            math_Irms = Irms;
            math_Irip = Irip;
            math_fs = fs;
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

            if (Properties.Settings.Default.给定谐振电感) //未验证
            {
                SetCoreType(Properties.Settings.Default.电感磁芯型号);
                numberCore = Properties.Settings.Default.电感磁芯数;
                lg = Properties.Settings.Default.电感气隙长度;
                wire = GetWireId(Properties.Settings.Default.电感绕线型号);
                N = Properties.Settings.Default.电感绕线匝数;
                Evaluate();
                designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                return;
            }

            //参数初始化
            double Kw = 0.4; //窗口利用系数
            double Bw = Properties.Settings.Default.最大工作磁密; //最大工作磁密(T) 
            double Bs = 0.4; //铁氧体的饱和磁通密度(T) 
            double Kf = 4.44; //波形系数（方波4.0，正弦波4.44）
            double Kj = Properties.Settings.Default.电流密度; //电流密度(A/cm^2)
            double V = 2 * Math.PI * math_fs_max * math_L * math_Imax; //计算电感上的压降（这里用的是电流最大值计算）
            double APmin = V * math_Imax * 1e4 / (Kf * Kw * Kj * math_fs_max * Bw); //所需磁芯面积积最小值(cm^4)
            double Axbmin = math_Imax / Kj; //满足电流密度所需裸线面积(cm^2)

            //选取磁芯
            for (int j = 1; j <= Properties.Settings.Default.磁芯数量上限; j++) //采用不同的磁芯数量
            {
                numberCore = j;
                for (int i = 0; i < Data.CoreList.Count; i++) //搜寻库中所有磁芯型号
                {
                    //只考虑EE型磁芯
                    if (!Data.CoreList[i].Shape.Equals("EE"))
                    {
                        continue;
                    }
                    //验证器件是否可用
                    if (!Data.CoreList[i].Available)
                    {
                        continue;
                    }
                    core = i;
                    double AP = j * Data.CoreList[i].Math_AP * 1e-4; //计算当前磁芯的面积积(cm^4)
                    //磁芯过剩容量检查
                    if (Configuration.CAN_CHECK_CORE_EXCESS && AP > APmin * (1 + Configuration.INDUCTOR_AREA_PRODUCT_EXCESS_RATIO))
                    {
                        continue;
                    }
                    if (AP > APmin) //磁芯面积积要大于所需最小值
                    {
                        //获取磁芯参数
                        double length = Data.CoreList[i].Math_F * 2 * 0.1; //磁芯参数(cm) 开气隙的边
                        double Aw = Data.CoreList[i].Math_Aw * 1e-2; //窗口面积(cm^2)
                        double Aecc = j * Data.CoreList[i].Math_Ae * 1e-2; //等效磁芯面积(cm^2)
                        int Nmin = (int)Math.Ceiling(V * 1e4 / (Kf * math_fs_max * Bw * Aecc)); //为防止磁芯饱和，所需要的匝数
                        lg = Math.Round(0.4 * Math.PI * Nmin * Nmin * Aecc * 1e-8 / math_L * 1e4) / 1e4; //计算所需气隙长度(cm) 未考虑磁芯本身的μm
                        if (lg > Properties.Settings.Default.电感最大气隙长度) //检查气隙长度
                        {
                            continue;
                        }
                        double FF = 1 + lg / Math.Sqrt(Aecc) * Math.Log(2 * length / lg); //边缘磁通系数
                        N = (int)Math.Ceiling(Math.Sqrt(lg * math_L / (0.4 * Math.PI * Aecc * FF * 1e-8))); //考虑边缘磁通后，得到修正后的匝数
                        double Bp = V * 1e4 / (Kf * math_fs_max * N * Aecc); //磁通密度峰值(T)
                        if (Bp > Bs) //检查磁通密度峰值
                        {
                            continue;
                        }

                        //选取绕线
                        double delta = Math.Sqrt(math_ρCu / (Math.PI * math_μ0 * math_μCu * math_fs_max)) * 1e2; //集肤深度(cm)
                        for (int w = 0; w < Data.WireList.Count; w++)
                        {
                            //只考虑励磁线
                            if (Data.WireList[w].Category != "Litz")
                            {
                                continue;
                            }
                            //集肤深度验证
                            double r = Data.WireList[w].Math_Db / 2 * 0.1; //裸线半径(cm)
                            if (r > delta)
                            {
                                continue;
                            }
                            double Axb = Data.WireList[w].Math_Ab * 1e-3; //绕线裸线面积(cm^2)
                            if (Axb < Axbmin)
                            {
                                continue;
                            }
                            wire = w;
                            double Ax = Data.WireList[w].Math_A * 1e-3; //绕线截面积(cm^2)

                            //窗口利用系数检查
                            if (N * Ax / Aw > Kw) //绕线太粗时，不能满足窗口利用系数
                            {
                                continue; //选取下一个绕线
                            }

                            //评估
                            Evaluate();
                            designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                            //不优化绕线，则只选取设计成功的并绕股数最少的绕线
                            if (!Configuration.CAN_OPTIMIZE_WIRE)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 验证电感的绕线编号、并绕股数、磁芯编号、气隙长度、匝数是否满足要求
        /// </summary>
        /// <returns>验证结果，true为满足</returns>
        public bool Validate()
        {
            if (wire < 0 || wire >= Data.WireList.Count)
            {
                return false;
            }
            if (core < 0 || core >= Data.CoreList.Count)
            {
                return false;
            }
            if (lg <= 0 || lg > Properties.Settings.Default.电感最大气隙长度)
            {
                return false;
            }
            if (N <= 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        protected override void CalcCost()
        {
            costCore = 2 * numberCore * Data.CoreList[core].Price;
            double MLT; //一匝绕线长度(cm) 
                        //TODO U型磁芯
            double C = Data.CoreList[core].Math_C * 0.1; //(cm)
            MLT = (numberCore - 1) * C * 2 + Data.CoreList[core].Math_MLT * 0.1;
            costWire = MLT * 1e-2 * N * Data.WireList[wire].Price;
            cost = costCore + costWire;
        }

        /// <summary>
        /// 计算体积
        /// </summary>
        protected override void CalcVolume()
        {
            double length; //长(mm)
            double width; //宽(mm)
            double height; //高(mm)
            double A = Data.CoreList[core].Math_A; //(mm)
            double B = Data.CoreList[core].Math_B; //(mm)
            double C = Data.CoreList[core].Math_C; //(mm)                                     
            double F = Data.CoreList[core].Math_C; //(mm)
            length = A;
            width = B * 2 + lg * 10 / 2; //B+气隙长度修正（lg单位为cm）
            double dWire = Data.WireList[wire].Math_D; //(mm)
            double dHeight = Math.Ceiling(dWire / (F * 2 + lg * 10) * N) * dWire * 2;
            height = numberCore * C + dHeight; //C*磁芯数量+绕线厚度修正
            volume = length * width * height / 1e6;
        }

        /// <summary>
        /// 计算损耗
        /// </summary>
        public override void CalcPowerLoss()
        {
            CalcPowerLossCu(); //计算铜损
            CalcPowerLossFe(); //计算铁损
            powerLoss = powerLossCu + powerLossFe;
        }

        /// <summary>
        /// 计算铜损
        /// </summary>
        private void CalcPowerLossCu()
        {
            double Axb = Data.WireList[wire].Math_Ab * 1e-3; //绕线裸线面积(cm^2)
            double C = Data.CoreList[core].Math_C * 0.1; //(cm)
            double MLT = (numberCore - 1) * C * 2 + Data.CoreList[core].Math_MLT * 0.1; //一匝绕线长度(cm) 
            double Rwire = math_ρCu * MLT * 1e-2 * N / (Axb * 1e-4); //绕线电阻(ohm)
            powerLossCu = Math.Pow(math_Irms, 2) * Rwire; //计算铜损
        }

        /// <summary>
        /// 计算铁损（磁损）
        /// </summary>
        private void CalcPowerLossFe()
        {
            //TODO 交流磁损
            //TODO U型磁芯
            double length = Data.CoreList[core].Math_F * 0.1; //磁芯参数F(cm)
            double Aecc = numberCore * Data.CoreList[core].Math_Ae * 1e-2; //等效磁芯面积(cm^2)
            double FF = 1 + lg / Math.Sqrt(Aecc) * Math.Log(2 * length / lg); //边缘磁通系数
            double magneticFluxDensityAC = 0.4 * Math.PI * N * FF * math_Irip * 0.5 / lg * 1e-4; //交流磁通密度(T)
            double volume = numberCore * Data.CoreList[core].Math_Ve * 1e-6; //有效磁芯体积(dm^3)
            powerLossFe = GetInductanceFeLoss(math_fs, magneticFluxDensityAC, volume); //计算铁损
        }
    }
}
