using System;

namespace PV_analysis.Components
{
    internal class Transformer : Magnetics
    {
        //限制条件
        private bool isCheckBm = true; //在评估时是否进行交流磁通密度检查 TODO 目前仅在变压器设计时检查

        //器件参数
        private int Wnp; //原边并绕股数
        private int Np; //原边匝数
        private int Wns; //副边并绕股数
        private int Ns; //副边匝数

        //设计条件
        private double power; //功率(W)
        private double frequency; //开关频率(Hz)
        private double currentPeakMax; //电流最大值(A)（原边电流最大值）
        private double turnRatio; //变比
        private double fluxLinkage; //磁链
        private double fluxLinkageMax = 0; //最大磁链

        //电路参数
        private double currentAverage; //电感平均电流(A) （有效值）
        private double currentRipple; //电感电流纹波(A) （峰峰值）
        private double[,] frequencyForEvaluation = new double[5, 7]; //开关频率（用于评估）
        private double[,] currentAverageForEvaluation = new double[5, 7]; //电感平均电流（用于评估）
        private double[,] currentRippleForEvaluation = new double[5, 7]; //电感电流纹波（用于评估）
        private double[,] fluxLinkageForEvaluation = new double[5, 7]; //磁链（用于评估）

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类变压器数量</param>
        public Transformer(int number)
        {
            this.number = number;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        private string[] GetConfigs()
        {
            return new string[] { "Transformer", number.ToString(), GetCoreType(), numberCore.ToString(), GetWireType(), Wnp.ToString(), Np.ToString(), Wns.ToString(), Ns.ToString() };
        }

        /// <summary>
        /// 设置设计条件
        /// </summary>
        /// <param name="power">功率</param>
        /// <param name="frequency">开关频率</param>
        /// <param name="currentPeakMax">电流最大值</param>
        /// <param name="turnRatio">变比</param>
        /// <param name="fluxLinkage">磁链</param>
        public void SetDesignCondition(double power, double frequency, double currentPeakMax, double turnRatio, double fluxLinkage)
        {
            this.power = power;
            this.frequency = frequency;
            this.currentPeakMax = currentPeakMax;
            this.turnRatio = turnRatio;
            this.fluxLinkage = fluxLinkage;
        }

        /// <summary>
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="currentAverage">电感平均电流</param>
        /// <param name="currentRipple">电感电流纹波</param>
        public void AddEvalParameters(int m, int n, double currentAverage, double currentRipple)
        {
            currentAverageForEvaluation[m, n] = currentAverage;
            currentRippleForEvaluation[m, n] = currentRipple;
        }

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        public void SelectParameters(int m, int n)
        {
            currentAverage = currentAverageForEvaluation[m, n];
            currentRipple = currentRippleForEvaluation[m, n];
        }

        /// <summary>
        /// 自动设计
        /// </summary>
        public override void Design()
        {
            if (turnRatio == 0)
            {
                return;
            }

            //参数初始化
            double ratioWaveform = 4; //波形系数（方波4.0，正弦波4.44）
            double ratioWindowUtilization = 0.4; //窗口利用系数
            double magneticFluxDensityMax = 0.4; //最大工作磁密(T)
            double currentDensity = 400; //电流密度(A/cm^2)
            double S3 = 0.75; //有效窗口系数
            double S2 = 0.6; //填充系数
            double Axp = currentPeakMax / currentDensity; //原边满足电流密度所需裸线面积(cm^2)
            double areaProduct = 2 * power * 1e4 / (ratioWaveform * ratioWindowUtilization * magneticFluxDensityMax * currentDensity * frequency); //所需磁芯面积积最小值(cm^4)

            //选取磁芯（视在功率需具体计算）
            for (int j = 1; j <= numberCoreMax; j++) //采用不同的磁芯数量
            {
                numberCore = j;
                for (int i = 0; i < Data.CoreList.Count; i++)//搜寻库中所有磁芯型号
                {
                    core = i;
                    double AP = j * Data.CoreList[i].Math_AP * 1e-4;//计算当前磁芯的面积积(cm^4)               
                    //System.out.println(AP+" "+areaProduct);                                                         
                    if (AP > areaProduct) //磁芯面积积要大于所需最小值
                    {
                        //获取磁芯参数
                        double Aw = Data.CoreList[i].Math_Aw * 1e-2; //窗口面积(cm^2)
                        double Aecc = j * Data.CoreList[i].Math_Ae * 1e-2; //等效磁芯面积(cm^2)

                        //选取绕线
                        //System.out.println(AP+" "+areaProduct+" "+(int)Math.ceil(Axp/AxpAWG));
                        double delta = Math.Sqrt(lowCu / (Math.PI * miu0 * miuCu * frequency)) * 1e2; //集肤深度(cm)
                        for (int w = 0; w < Data.WireList.Count; w++)
                        {
                            //集肤深度验证
                            double r = Data.WireList[i].Math_Db / 2 * 0.1; //裸线半径(cm)
                            if (r > delta)
                            {
                                continue;
                            }
                            double AxpAWG = Data.WireList[w].Math_Ab * 1e-3; //绕线裸线面积(cm^2)
                            if (AxpAWG < Axp)
                            {
                                continue;
                            }
                            wire = w;
                            Wnp = Data.WireList[w].Math_Wn; //原边并绕股数 FIXME 并绕股数对可用匝数（窗口利用系数）的影响
                            Wns = Data.WireList[w].Math_Wn; //副边并绕股数
                            double Ax = Data.WireList[w].Math_A * 1e-3; //绕线截面积(cm^2)
                            //绕线设计 FIXME Np:Ns匝比可能不等于变比（变比不为整数时）
                            Np = (int)Math.Ceiling(0.5 * fluxLinkage / (magneticFluxDensityMax * Aecc * 1e-4)); //原边绕组匝数
                            //this.Np = (int)Math.ceil(this.voltageInput*1e4/(ratioWaveform*magneticFluxDensityMax*this.frequency*Aecc)); //原边绕组匝数
                            for (; Np < 1e5; Np++)
                            {
                                if (Np < 0)
                                {
                                    Console.WriteLine("Wrong Np!");
                                    System.Environment.Exit(-1);
                                }
                                Ns = (int)Math.Ceiling(Np / turnRatio);
                                //窗口系数检查
                                double Awp = Np * Ax; //并绕后原边所占窗口面积(cm^2)
                                double Aws = Ns * Ax; //并绕后副边所占窗口面积(cm^2)
                                double Ku = (Awp + Aws) / Aw;
                                if (Ku > S2 * S3)
                                {
                                    break;
                                }

                                //评估
                                if (Evaluate())
                                {
                                    CalcVolume();
                                    CalcCost();
                                    designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 验证变压器的各个设计参数是否满足要求
        /// </summary>
        /// <returns>验证结果，true为满足</returns>
        public bool Validate()
        {
            if (wire < 0 || wire >= Data.WireList.Count)
            {
                return false;
            }
            if (Wnp <= 0)
            {
                return false;
            }
            if (Wns <= 0)
            {
                return false;
            }
            if (core < 0 || core >= Data.CoreList.Count)
            {
                return false;
            }
            if (Np <= 0)
            {
                return false;
            }
            if (Ns <= 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 损耗评估
        /// </summary>
        private bool Evaluate()
        {
            int m = Config.CGC_VOLTAGE_RATIO.Length;
            int n = Config.CGC_POWER_RATIO.Length;

            if (!VoltageVariable) //输入电压不变
            {
                m = 1;
            }

            for (int i = 0; i < m; i++) //对不同输入电压进行计算
            {
                for (int j = n - 1; j >= 0; j--) //对不同功率点进行计算
                {
                    SelectParameters(i, j); //设置对应条件下的电路参数
                    CalcPowerLoss(); //计算对应条件下的损耗
                    //交流磁通检查 FIXME
                    if (isCheckBm && !CheckBm())
                    {
                        return false;
                    }
                    if (PowerVariable)
                    {
                        powerLossEvaluation += powerLoss * Config.CGC_POWER_WEIGHT[j] / Config.CGC_POWER_RATIO[j]; //计算损耗评估值
                    }
                    else //若负载不变，则只评估满载
                    {
                        powerLossEvaluation = powerLoss;
                        break;
                    }
                }
            }
            powerLossEvaluation /= m;
            return true;
        }

        /// <summary>
        /// 计算成本
        /// </summary>
        public void CalcCost()
        {
            costCore = 2 * numberCore * Data.CoreList[core].Price;
            double MLT; //一匝绕线长度(cm) 
            double C = Data.CoreList[core].Math_C * 0.1; //(cm)
            MLT = (numberCore - 1) * C * 2 + Data.CoreList[core].Math_MLT * 0.1;
            costWire = MLT * 1e-2 * (Np + Ns) * Data.WireList[wire].Price;
            cost = costCore + costWire;
        }

        //TODO 修正体积计算
        /// <summary>
        /// 计算体积
        /// </summary>
        public void CalcVolume()
        {
            double length; //长(mm)
            double width; //宽(mm)
            double height; //高(mm)
            double A = Data.CoreList[core].Math_A; //(mm)
            double B = Data.CoreList[core].Math_B; //(mm)
            double C = Data.CoreList[core].Math_C; //(mm)
            length = A;
            width = B * 2; //B（mm）
            height = numberCore * C; //C*磁芯数量+绕线厚度修正
            volume = length * width * height / 1e6;
        }

        /// <summary>
        /// 计算损耗
        /// </summary>
        public void CalcPowerLoss()
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
            double AxpAWG = Data.WireList[wire].Math_Ab * 1e-3; //绕线裸线面积(cm^2)
            double C = Data.CoreList[core].Math_C * 0.1; //(cm)
            double MLT = (numberCore - 1) * C * 2 + Data.CoreList[core].Math_MLT * 0.1; //一匝绕线长度(cm) 
            double Rwire = lowCu * MLT * 1e-2 * Np / (AxpAWG * 1e-4); //原边单根绕线电阻(ohm)
            double Pp = Math.Pow(currentAverage, 2) * Rwire; //原边铜损
            Rwire = lowCu * MLT * 1e-2 * Ns / (AxpAWG * 1e-4); //副边单根绕线电阻
            double Ps = Math.Pow(currentAverage, 2) * Rwire; //副边铜损
            powerLossCu = Pp + Ps; //计算铜损
        }

        /// <summary>
        /// 计算铁损
        /// </summary>
        private void CalcPowerLossFe()
        {
            double Aecc = numberCore * Data.CoreList[core].Math_Ae * 1e-2; //等效磁芯面积(cm^2)
            double Bm = 0.5 * fluxLinkage / (Np * Aecc * 1e-4); //交流磁通密度(cm^2)
            double prewV = GetInductanceFeLoss(frequency, Bm);// //单位体积铁损(W/m^3)
            double volume = numberCore * Data.CoreList[core].Math_Ve * 1e-9; //磁芯体积(m^3) Datasheet中给出的即为一对磁芯的有效磁体积
            powerLossFe = prewV * volume; //计算铁损
        }

        /// <summary>
        /// 检查交流磁通密度
        /// </summary>
        /// <returns>检查结果，true为通过</returns>
        protected bool CheckBm()
        {
            double magneticFluxDensityMax = 0.4;
            double Aecc = numberCore * Data.CoreList[core].Math_Ae * 1e-2; //等效磁芯面积(cm^2)
            double Bm = 0.5 * fluxLinkage / (Np * Aecc * 1e-4);
            if (Bm > magneticFluxDensityMax)
            {
                return false;
            }
            return true;
        }

    }
}
