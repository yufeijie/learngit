using System;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    internal class Inductor : Magnetics
    {
        //限制条件
        private static readonly double lgDesignMax = 1; //气隙长度设计允许最大值(cm)
        private static readonly double lgDelta = 1e-4; //气隙长度精度(cm)

        //器件参数
        private int wire; //绕线编号
        private double lg; //气隙长度(cm)
        private int N; //匝数

        //设计条件
        private double inductance; //感值(H)
        private double currentPeakMax; //电流最大值(A)（原边电流最大值）
        private double frequencyMax; //最高开关频率

        //电路参数
        private double currentAverage; //电感平均电流(A) （有效值）
        private double currentRipple; //电感电流纹波(A) （峰峰值）
        private double frequency; //开关频率(Hz)
        private double[,] currentAverageForEvaluation = new double[5, 7]; //电感平均电流（用于评估）
        private double[,] currentRippleForEvaluation = new double[5, 7]; //电感电流纹波（用于评估）
        private double[,] frequencyForEvaluation = new double[5, 7]; //开关频率（用于评估）

        /// <summary>
        /// 是否为交流电感
        /// </summary>
        public bool IsAC { get; set; } = false;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类电感数量</param>
        public Inductor(int number)
        {
            this.number = number;
        }

        /// <summary>
        /// 获取设计方案的配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        private string[] GetConfigs()
        {
            return new string[] { number.ToString(), GetCoreType(), numberCore.ToString(), lg.ToString(), GetWireType(wire), N.ToString() };
        }

        /// <summary>
        /// 获取损耗分布
        /// </summary>
        /// <returns>损耗分布信息</returns>
        public override List<Item> GetLossBreakdown()
        {
            List<Item> list = new List<Item>
            {
                new Item(Name + "(Cu)", Math.Round(number * powerLossCu, 2)),
                new Item(Name + "(Fe)", Math.Round(number * powerLossFe, 2))
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
        /// <param name="inductance">感值</param>
        /// <param name="currentPeakMax">电流最大值</param>
        /// <param name="frequencyMax">最大开关频率</param>
        public void SetConditions(double inductance, double currentPeakMax, double frequencyMax)
        {
            this.inductance = inductance;
            this.currentPeakMax = currentPeakMax;
            this.frequencyMax = frequencyMax;
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
        /// 添加电路参数（用于评估）
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        /// <param name="currentAverage">电感平均电流</param>
        /// <param name="currentRipple">电感电流纹波</param>
        /// <param name="frequency">开关频率</param>
        public void AddEvalParameters(int m, int n, double currentAverage, double currentRipple, double frequency)
        {
            currentAverageForEvaluation[m, n] = currentAverage;
            currentRippleForEvaluation[m, n] = currentRipple;
            frequencyVariable = true;
            frequencyForEvaluation[m, n] = frequency;
        }

        /// <summary>
        /// 选择电路参数用于当前计算
        /// </summary>
        /// <param name="m">输入电压对应编号</param>
        /// <param name="n">负载点对应编号</param>
        protected override void SelectParameters(int m, int n)
        {
            currentAverage = currentAverageForEvaluation[m, n];
            currentRipple = currentRippleForEvaluation[m, n];
            if (frequencyVariable)
            {
                frequency = frequencyForEvaluation[m, n];
            }
            else
            {
                frequency = frequencyMax;
            }
        }

        /// <summary>
        /// 设置电路参数
        /// </summary>
        /// <param name="currentAverage">电感平均电流</param>
        /// <param name="currentRipple">电感电流纹波</param>
        /// <param name="frequency">开关频率</param>
        public void SetParameters(double currentAverage, double currentRipple, double frequency)
        {
            this.currentAverage = currentAverage;
            this.currentRipple = currentRipple;
            this.frequency = frequency;
        }

        /// <summary>
        /// 自动设计
        /// </summary>
        public override void Design()
        {
            //若感值为0则退出设计
            if (inductance == 0)
            {
                return;
            }

            //参数初始化
            double ratioWindowUtilization = 0.4; //窗口利用系数
            double magneticFluxDensityMax = 0.4; //最大工作磁密(T)
            double currentDensity = 400; //电流密度(A/cm^2)
            double S3 = 0.75; //有效窗口系数
            double S2 = 0.6; //填充系数
            double energyMax = 0.5 * inductance * Math.Pow(currentPeakMax, 2);
            double APmin = 2 * energyMax / (ratioWindowUtilization * magneticFluxDensityMax * currentDensity) * 1e4; //所需磁芯面积积最小值(cm^4)
            double Axbmin = currentPeakMax / currentDensity; //满足电流密度所需裸线面积(cm^2)

            //选取磁芯
            for (int j = 1; j <= numberCoreMax; j++) //采用不同的磁芯数量
            {
                numberCore = j;
                for (int i = 0; i < Data.CoreList.Count; i++) //搜寻库中所有磁芯型号
                {
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
                    if (AP > APmin) //磁芯面积积要大于所需最小值
                    {
                        //获取磁芯参数
                        double length = Data.CoreList[i].Math_F * 2 * 0.1; //磁芯参数(cm) 开气隙的边
                        double Aw = Data.CoreList[i].Math_Aw * 1e-2; //窗口面积(cm^2)
                        double Aecc = j * Data.CoreList[i].Math_Ae * 1e-2; //等效磁芯面积(cm^2)

                        //选取绕线
                        double delta = Math.Sqrt(lowCu / (Math.PI * miu0 * miuCu * frequencyMax)) * 1e2; //集肤深度(cm)
                        for (int w = 0; w < Data.WireList.Count; w++)
                        {
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
                            int Nmax = (int)Math.Floor(S2 * S3 * Aw / Ax); //满绕匝数
                            //System.out.println(AP+" "+areaProduct+" "+this.Wn+" "+Nmax);
                            if (Nmax <= 0) //可能有一匝都绕不下的情况，即并绕股数过多
                            {
                                break; //退出并绕股数设计
                            }
                            else
                            {
                                //this.showLgChange(G, Aecc, Nmax, magneticFluxDensityMax); //查看当前条件下，变量随气隙长度变化的曲线（！！！谨慎使用）
                                //设计气隙长度
                                double lgMax = FindLgMax(Nmax, length, Aecc); //通过二分查找满绕下对应的最大气隙长度，取其与变量lgMax中的最小值作为气隙长度最大值，提高运算速度

                                //优化
                                if (lgMax < lgDelta)
                                {
                                    break; //若满绕匝数对应的气隙长度小于精度，则退出并绕股数设计
                                }
                                else
                                {
                                    double magneticFluxDensityPeak = CalcMagneticFluxDensityPeak(lgMax, length, Aecc); //磁通密度峰值(T)
                                    if (magneticFluxDensityPeak > magneticFluxDensityMax)
                                    {
                                        //System.out.println("core="+(i+1)+", Wn="+Wn+", lg="+String.format("%.2f", lgMax)+", N="+this.calcN(lgMax, length, Aecc)+", Nmax="+Nmax+", Bp="+String.format("%.6f", magneticFluxDensityPeak)+", Bm="+magneticFluxDensityMax);
                                        break; //若满绕匝数对应的磁通密度峰值超过最大工作磁密，则退出并绕股数设计
                                    }
                                }
                                //得到设计结果
                                lg = FindLgBest(lgMax, length, Aecc, magneticFluxDensityMax);
                                N = CalcN(lg, length, Aecc);

                                //评估
                                Evaluate();                                
                                designList.Add(Math_Peval, Volume, Cost, GetConfigs()); //记录设计
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 在给定的气隙长度最大值及相关条件下，找到最佳气隙长度，使得损耗最低
        /// </summary>
        /// <param name="lgMax">气隙长度最大值(cm)</param>
        /// <param name="G">磁芯参数(cm)</param>
        /// <param name="Aecc">等效磁芯面积(cm^2)</param>
        /// <param name="magneticFluxDensityMax">最大工作磁密(T)</param>
        /// <returns>最佳气隙长度(cm)</returns>
        private double FindLgBest(double lgMax, double G, double Aecc, double magneticFluxDensityMax)
        {
            //二分查找，在lgDelta(cm)~lgMax(cm)范围内寻找
            int l = 1, r = (int)(lgMax / lgDelta); //将lgDelta(cm)~lgMax(cm)映射到对应整数中
            while (l < r)
            {
                int mid = (l + r) / 2; //二分取中值（向上取整）
                double magneticFluxDensityPeak = CalcMagneticFluxDensityPeak((double)mid * lgDelta, G, Aecc); //磁通密度峰值(T)
                                                                                                              //System.out.println(l+" "+r+" "+magneticFluxDensityPeak+" "+magneticFluxDensityMax);
                if (magneticFluxDensityPeak > magneticFluxDensityMax)
                {
                    l = mid + 1; //当前气隙长度对应的磁通密度峰值大于最大工作磁密，说明结果大于当前气隙长度，即结果落在右半区
                }
                else
                {
                    r = mid; //当前气隙长度对应的磁通密度峰值小于等于最大工作磁密，说明结果小于等于当前气隙长度，即结果落在左半区
                }
            }
            //得到最佳气隙长度
            return (double)l * lgDelta;
        }

        /// <summary>
        /// 通过二分查找满绕匝数对应的最大气隙长度，取其与变量lgMax中的最小值作为气隙长度最大值
        /// </summary>
        /// <param name="Nmax">满绕匝数</param>
        /// <param name="G">磁芯参数(cm)</param>
        /// <param name="Aecc">等效磁芯面积(cm^2)</param>
        /// <returns>气隙长度最大值(cm)</returns>
        private double FindLgMax(int Nmax, double G, double Aecc)
        {
            double lg; //气隙长度(cm)
            int N; //匝数

            //二分查找，在lgDelta(cm)~lgDesignMax(cm)范围内寻找
            int l = 1, r = (int)(lgDesignMax / lgDelta); //将lgDelta(cm)~lgDesignMax(cm)映射到对应整数中
            while (l < r)
            {
                int mid = (l + r + 1) / 2; //二分取中值（向上取整）
                N = CalcN((double)mid * lgDelta, G, Aecc); //将变量mid映射成对应气隙长度，计算此时的匝数
                                                           //System.out.println(l+" "+r+" "+N+" "+Nmax);
                if (N <= Nmax)
                {
                    l = mid; //当前气隙长度对应的匝数小于等于满绕匝数，说明结果大于等于当前气隙长度，即结果落在右半区
                }
                else
                {
                    r = mid - 1; //当前气隙长度对应的匝数大于满绕匝数，说明结果小于当前气隙长度，即结果落在左半区
                }
            }

            //得到气隙长度最大值并验算
            lg = (double)l * lgDelta;
            N = CalcN(lg, G, Aecc);
            if (N > Nmax)
            {
                lg = 0; //若气隙长度精度对应的匝数依然比满绕匝数大，则返回0
            }
            //		System.out.println(lg);
            return lg;
        }

        /// <summary>
        /// 计算边缘磁通系数
        /// </summary>
        /// <param name="lg">气隙长度(cm)</param>
        /// <param name="G">磁芯参数(cm)</param>
        /// <param name="Aecc">等效磁芯面积(cm^2)</param>
        /// <returns>边缘磁通系数</returns>
        private double CalcFF(double lg, double G, double Aecc)
        {
            return 1 + lg / Math.Sqrt(Aecc) * Math.Log(2 * G / lg);
        }

        /// <summary>
        /// 计算匝数
        /// </summary>
        /// <param name="lg">气隙长度(cm)</param>
        /// <param name="G">磁芯参数(cm)</param>
        /// <param name="Aecc">等效磁芯面积(cm^2)</param>
        /// <returns>匝数</returns>
        private int CalcN(double lg, double G, double Aecc)
        {
            double FF = CalcFF(lg, G, Aecc);
            return (int)Math.Ceiling(Math.Sqrt(lg * inductance / (0.4 * Math.PI * Aecc * FF * 1e-8)));
        }

        /// <summary>
        /// 计算磁通密度峰值
        /// </summary>
        /// <param name="lg">气隙长度(cm)</param>
        /// <param name="G">磁芯参数(cm)</param>
        /// <param name="Aecc">等效磁芯面积(cm^2)</param>
        /// <returns>磁通密度峰值(T)</returns>
        private double CalcMagneticFluxDensityPeak(double lg, double G, double Aecc)
        {
            double FF = CalcFF(lg, G, Aecc);
            int N = CalcN(lg, G, Aecc);
            return 0.4 * Math.PI * N * FF * currentPeakMax / lg * 1e-4;
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
            if (lg <= 0 || lg > lgDesignMax)
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

        //TODO U型磁芯
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
            double Rwire = lowCu * MLT * 1e-2 * N / (Axb * 1e-4); //单根绕线电阻(ohm)
            powerLossCu = Math.Pow(currentAverage, 2) * Rwire; //计算铜损
            if (IsAC)
            {
                Rwire = lowCu * MLT * 1e-2 * N / (Axb * 1e-4); //直流电阻
                double r = Data.WireList[wire].Math_Db / 2 * 0.1;
                double delta = Math.Sqrt(lowCu / (Math.PI * miu0 * miuCu * frequency)) * 1e2; //集肤深度(cm)
                double Rwire1; //开关频率下的电阻 FIXME
                if (r <= delta)
                {
                    Rwire1 = Rwire;
                }
                else
                {
                    Rwire1 = Rwire * (r * r) / (delta * delta);
                }
                powerLossCu +=  Math.Pow(currentRipple, 2) * Rwire1; //计算纹波铜损
            }
        }

        /// <summary>
        /// 计算铁损（磁损）
        /// </summary>
        private void CalcPowerLossFe()
        {
            //TODO 交流磁损
            //TODO U型磁芯
            double length = Data.CoreList[core].Math_F * 2 * 0.1; //磁芯参数F(cm)
            double Aecc = numberCore * Data.CoreList[core].Math_Ae * 1e-2; //等效磁芯面积(cm^2)
            double FF = 1 + lg / Math.Sqrt(Aecc) * Math.Log(2 * length / lg); //边缘磁通系数
            double magneticFluxDensityAC = 0.4 * Math.PI * N * FF * currentRipple * 0.5 / lg * 1e-4; //交流磁通密度(T)
            double prewV = GetInductanceFeLoss(frequency, magneticFluxDensityAC);// //单位体积铁损(W/m^3)
            double volume = numberCore * Data.CoreList[core].Math_Ve * 1e-9; //磁芯体积(m^3)
            powerLossFe = prewV * volume; //计算铁损
        }
    }
}
