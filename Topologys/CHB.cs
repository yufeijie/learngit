using PV_analysis.Components;
using PV_analysis.Converters;
using System;
using static PV_analysis.Curve;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// CHB拓扑 PSPWM调制方式
    /// </summary>
    internal class CHB : Topology
    {
        private DCACConverter converter; //所属变换器

        //可选参数
        //private double ratioCurrentRipple = 0.2; //电流纹波系数

        //给定参数
        private int number; //模块数
        private string modulation; //调制方式
        private double math_Vin; //模块输入电压
        private double math_Votot; //总输出电压
        private double math_Vo; //模块输出电压
        private double math_fg; //工频
        private double math_fs; //开关频率
        private double math_φ; //功率因数角(rad)

        //主电路元件参数
        private double math_Ma; //幅度调制比
        private int math_Mf; //频率调制比
        private double math_VSmax; //开关器件电压
        private double math_L = 40 * 1e-3; //感值
        private double math_Iorms; //输出电流基波有效值
        //private double math_Iorip_rms; //输出电流纹波有效值
        private double[] curve_iS; //开关器件电流波形
        private double[,][] math_Ton_igbt; //Igbt开通时间
        private double[,][] math_Ton_diode; //Diode开通时间
        
        //波形
        //private Curve curve_Vg; //理想并网相电压波形
        //private Curve curve_Votot; //总输出电压波形

        //元器件
        private CHBModule semiconductor;
        private GridInductor inductor;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="converter">所属变换器</param>
        public CHB(DCACConverter converter)
        {
            //获取设计规格
            this.converter = converter;
            number = converter.Number;
            modulation = converter.Modulation;
            math_Pfull = converter.Math_Psys / converter.PhaseNum / number;
            math_Vin = converter.Math_Vin;
            math_Votot = converter.Math_Vo;
            math_fg = converter.Math_fg;
            math_φ = converter.Math_φ;
            math_fs = converter.Math_fs;

            math_Ma = math_Votot * Math.Sqrt(2) / (number * math_Vin);
            math_Vo = math_Votot / number;
            math_Iorms = math_Pfull / (math_Vo * Math.Cos(math_φ));
            
            //初始化元器件
            semiconductor = new CHBModule(1)
            {
                Name = "开关器件",
                VoltageVariable = false,
                MultiNumber = number
            };
            inductor = new GridInductor(1)
            {
                Name = "并网滤波电感",
                VoltageVariable = false,
                MultiNumber = number
            };
            components = new Component[] { semiconductor, inductor };
            componentGroups = new Component[1][];
            componentGroups[0] = new Component[] { semiconductor, inductor };
        }

        /// <summary>
        /// 获取拓扑名
        /// </summary>
        /// <returns>拓扑名</returns>
        public override string GetName()
        {
            return "CHB";
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            //基本电路参数
            math_Mf = (int)(math_fs / math_fg); //TODO 小数？
            math_VSmax = math_Vin;

            //生成输出电压波形
            //int A = 1;
            //curve_Vg = new Curve
            //{
            //    Category = "Sine",
            //    Amplitude = Math.Sqrt(2) * math_Votot,
            //    Frequency = math_fg,
            //    InitialAngle = 0
            //};
            //curve_Votot = new Curve();
            //Curve us = new Curve
            //{
            //    Name = "Us",
            //    Category = "Sine",
            //    Amplitude = A * math_Ma,
            //    Frequency = math_fg,
            //    InitialAngle = 0
            //};
            //Curve[,] uc = new Curve[number, 7];

            ////TODO 这里模拟的只是PSPWM的波形
            //double phi = Math.PI;
            //for (int i = 0; i < number; i++)
            //{
            //    uc[i, 1] = new Curve
            //    {
            //        Name = "Uc" + (i + 1),
            //        Category = "Triangle",
            //        Amplitude = A,
            //        Frequency = math_fs,
            //        InitialAngle = phi
            //    };
            //    uc[i, 2] = new Curve
            //    {
            //        Name = "Uc" + (i + 1) + "'",
            //        Category = "Triangle",
            //        Amplitude = A,
            //        Frequency = math_fs,
            //        InitialAngle = Math.PI + phi
            //    };
            //    uc[i, 3] = new Curve { Name = "g" + (i + 1) + "1" };
            //    uc[i, 3].Compare(us, uc[i, 1], 0, 1 / math_fg);
            //    uc[i, 5] = new Curve { Name = "g" + (i + 1) + "3" };
            //    uc[i, 5].Compare(us, uc[i, 2], 0, 1 / math_fg);
            //    uc[i, 4] = new Curve { Name = "g" + (i + 1) + "2" };
            //    uc[i, 4].Not(uc[i, 5]);
            //    uc[i, 6] = new Curve { Name = "g" + (i + 1) + "4" };
            //    uc[i, 6].Not(uc[i, 3]);
            //    uc[i, 0] = new Curve { Name = "Uo" + (i + 1) };
            //    uc[i, 0].Drive(uc[i, 3], uc[i, 4], math_Vin);
            //    curve_Votot.Plus(uc[i, 0]);
            //    phi -= Math.PI / number;
            //}

            //Point[] data = curve_Votot.GetData();
            //Console.WriteLine("-----------" + curve_Votot.Name + "_x------------");
            //for (int i = 0; i < data.Length; i++)
            //{
            //    Console.WriteLine(data[i].X);
            //}

            //Console.WriteLine("-----------" + curve_Votot.Name + "_y------------");
            //for (int i = 0; i < data.Length; i++)
            //{
            //    Console.WriteLine(data[i].Y);
            //}

            //Graph graph = new Graph();
            //graph.Add(curve_Votot, "Vo_tot");
            //curve_Vg.Produce(0, 1 / math_fg);
            //graph.Add(curve_Vg, "Vo");
            //graph.Draw();

            //得到输出电压波形与滤波电感感值
            //Curve ioR = new Curve { Name = "Io_Ripple" };
            //math_L = ioR.CreateCurrentRipple(curve_Votot, curve_Vg, Math.Sqrt(2) * math_Iorms * ratioCurrentRipple); //FIXME 电感对功率因素角的影响？
            //math_Iorip_rms = ioR.CalcRMS();
            
            //		Curve ioR2 = new Curve("Io_Ripple'", "t(ms)", "I(A)");
            //		ioR2.turn(ioR);
            //		GraphXY graphWaveform2 = new GraphXY("Waveform2");
            //		graphWaveform2.addData(ioR);
            //		graphWaveform2.addData(ioR2);
            //		graphWaveform2.drawX(ioR.getXLabel(), ioR.getYLabel(), "n="+this.number);
            //		
            //		Curve io1 = new Curve("Io_1", "t(ms)", "I(A)");
            //		io1.createSine(Math.sqrt(2)*this.currentOutputRMS, this.frequencyGrid, -this.anglePowerFactor);
            //		io1.produce(0, 1/this.frequencyGrid);
            //		Curve io = new Curve("Io", "t(ms)", "I(A)");
            //		io.accumulate(io1, ioR2);
            //		
            //		GraphXY graphWaveform3 = new GraphXY("Waveform3");
            //		graphWaveform3.addData(io1);
            //		graphWaveform3.addData(io);
            //		graphWaveform3.drawX(io1.getXLabel(), io1.getYLabel(), "n="+this.number);

            //生成每个开关器件的波形
            //计算开通时间
            math_Ton_igbt = new double[number, 4][]; //Igbt开通时间
            math_Ton_diode = new double[number, 4][]; //Diode开通时间
            for (int i = 0; i < number; i++) //每个模块分开计算
            {
                double[,] dutyCycle = new double[4, math_Mf]; //占空比                                                                                      
                //this.dataGraph[i+2] = new DataGraph("Vo_block"+(i+1), "t", "V");
                for (int j = 0; j < 4; j++)
                {
                    math_Ton_igbt[i, j] = new double[math_Mf];
                    math_Ton_diode[i, j] = new double[math_Mf];
                }
                for (int k = 0; k < math_Mf; k++)
                { //每个开关周期
                  //计算占空比
                    switch (modulation)
                    {
                        case "PSPWM":
                            dutyCycle[0, k] = 0.5 * math_Ma * MySin(k, 0) + 0.5;
                            dutyCycle[2, k] = 0.5 * math_Ma * MySin(k, 0) + 0.5;
                            //						if(2*k*this.frequencyGrid < this.frequency) {
                            //							dutyCycle[0, k] = 1;
                            //							dutyCycle[2, k] = this.ratioAmplitudeModulation*Math.abs(this.mySin(k, 0));
                            //						} else {
                            //							dutyCycle[0, k] = 0;
                            //							dutyCycle[2, k] = 1-this.ratioAmplitudeModulation*Math.abs(this.mySin(k, 0));
                            //						}
                            break;
                        case "LSPWM":
                            dutyCycle[0, k] = number * math_Ma * MySin(k, 0) - i;
                            if (dutyCycle[0, k] > 1)
                            {
                                dutyCycle[0, k] = 1;
                            }
                            if (dutyCycle[0, k] < 0)
                            {
                                dutyCycle[0, k] = 0;
                            }
                            dutyCycle[2, k] = number * math_Ma * MySin(k, 0) + i + 1;
                            if (dutyCycle[2, k] > 1)
                            {
                                dutyCycle[2, k] = 1;
                            }
                            if (dutyCycle[2, k] < 0)
                            {
                                dutyCycle[2, k] = 0;
                            }
                            break;
                    }
                    dutyCycle[1, k] = 1 - dutyCycle[2, k];
                    dutyCycle[3, k] = 1 - dutyCycle[0, k];

                    //得到开通时间
                    if (MySin(k, -math_φ) >= 0)
                    {
                        math_Ton_igbt[i, 0][k] = dutyCycle[0, k] / math_fs;
                        math_Ton_diode[i, 0][k] = 0;
                        math_Ton_igbt[i, 1][k] = 0;
                        math_Ton_diode[i, 1][k] = dutyCycle[1, k] / math_fs;
                        math_Ton_igbt[i, 2][k] = dutyCycle[2, k] / math_fs;
                        math_Ton_diode[i, 2][k] = 0;
                        math_Ton_igbt[i, 3][k] = 0;
                        math_Ton_diode[i, 3][k] = dutyCycle[3, k] / math_fs;
                    }
                    else
                    {
                        math_Ton_igbt[i, 0][k] = 0;
                        math_Ton_diode[i, 0][k] = dutyCycle[0, k] / math_fs;
                        math_Ton_igbt[i, 1][k] = dutyCycle[1, k] / math_fs;
                        math_Ton_diode[i, 1][k] = 0;
                        math_Ton_igbt[i, 2][k] = 0;
                        math_Ton_diode[i, 2][k] = dutyCycle[2, k] / math_fs;
                        math_Ton_igbt[i, 3][k] = dutyCycle[3, k] / math_fs;
                        math_Ton_diode[i, 3][k] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// 计算电路参数，并模拟电压、电流波形
        /// </summary>
        private void Simulate()
        {
            //还原输出电流波形
            math_Iorms = math_P / (math_Vo * Math.Cos(math_φ));
            curve_iS = new double[math_Mf];
            for (int k = 0; k < math_Mf; k++)
            {
                curve_iS[k] = Math.Sqrt(2) * math_Iorms * MySin(k, -math_φ);
            }
        }

        /// <summary>
        /// 准备评估所需的电路参数
        /// </summary>
        public override void Prepare()
        {
            //计算电路参数
            DesignCircuitParam();
            semiconductor.SetConstants(math_fg, math_VSmax, math_VSmax, math_Mf, math_Ton_igbt, math_Ton_diode);

            int n = Configuration.powerRatio.Length;

            for (int j = 0; j < n; j++)
            {
                math_P = math_Pfull * Configuration.powerRatio[j]; //改变负载
                Simulate();
                //Graph graph = new Graph();
                //graph.Add(currentSwitch_P, "iP");
                //graph.Add(currentSwitch_S, "iS");
                //graph.Draw();

                //设置元器件的电路参数（用于评估）
                semiconductor.AddEvalParameters(0, j, curve_iS);
                inductor.AddEvalParameters(0, j ,math_Iorms);
            }

            //设置元器件的设计条件
            double voltageStressSwitch = math_Vin;
            double currentOutputPeak = Math.Sqrt(2) * math_Iorms; //TODO 纹波？
            semiconductor.SetConditions(voltageStressSwitch, currentOutputPeak, math_fs);
            inductor.SetConditions(math_L, currentOutputPeak, math_fg);
        }

        /// <summary>
		/// 计算电路参数
		/// </summary>
		public override void Calc()
        {
            math_P = converter.Math_P;
            Simulate();
            //设置元器件的电路参数
            semiconductor.SetParameters(curve_iS);
            inductor.SetParameters(math_Iorms);
        }

        /// <summary>
        /// 生成离散化正弦波
        /// </summary>
        /// <param name="k">第k个开关周期</param>
        /// <param name="angleInitial">初相角(rad)</param>
        /// <returns>第k个开关周期中点对应正弦波</returns>
        private double MySin(int k, double angleInitial)
        {
            return Math.Sin(2 * Math.PI * math_fg * (k + 0.5) / math_fs + angleInitial);
        }
    }
}
