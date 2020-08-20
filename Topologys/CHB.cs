using PV_analysis.Components;
using PV_analysis.Converters;
using System;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// CHB拓扑 PSPWM调制方式
    /// </summary>
    internal class CHB : Topology
    {
        //可选参数
        private double ratioCurrentRipple = 0.2; //电流纹波系数

        //给定参数
        private int number; //模块数
        private double voltageInputDef; //直流侧输入电压给定值（若设置此值，则按照直流侧设计）
        private double voltageOutputTotalDef; //整体输出电压给定值
        private double frequencyGrid; //工频
        private double anglePowerFactor; //功率因数角(rad)

        private DCACConverter converter; //所属变换器

        //电路参数
        //需优化参数
        private string modulation; //调制方式
        private double frequency; //开关频率

        //基本电路参数
        private double voltageInput; //模块输入电压
        private double voltageOutput; //模块输出电压
        private double voltageOutputTotal; //整体输出电压
        private double ratioAmplitudeModulation; //幅度调制比
        private int ratioFrequencyModulation; //频率调制比
        private double currentOutputRMS; //输出电流基波有效值
        private double currentOutputRippleRMS; //输出电流纹波有效值
        private double[] currentSwitch; //开关器件电流波形
        private double[,][] timeTurnOnIgbt; //Igbt开通时间
        private double[,][] timeTurnOnDiode; //Diode开通时间

        //主电路元件相关电路参数
        private double voltageSwitch; //开关器件电压
        private double inductance; //感值

        //模拟波形参数
        private Curve curveVoltageOutputTotalBase; //模拟逆变器整体输出电压基波波形
        private Curve curveVoltageOutputTotal; //模拟逆变器整体输出电压波形

        //元器件
        private CHBModule semiconductor;
        private Inductor inductor;

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
            voltageInputDef = converter.Math_Vin_def;
            voltageOutputTotalDef = converter.Math_Vo;
            frequencyGrid = converter.Math_fg;
            anglePowerFactor = converter.Math_phi;
            ratioAmplitudeModulation = converter.Math_Ma;
            frequency = converter.Math_fs;
            if (voltageInputDef == 0)
            {
                voltageOutputTotal = voltageOutputTotalDef;
                voltageOutput = voltageOutputTotal / number;
                currentOutputRMS = math_Pfull / (voltageOutput * Math.Cos(anglePowerFactor));
                voltageInput = Math.Sqrt(2) * voltageOutput / ratioAmplitudeModulation;
            }
            else
            {
                voltageInput = voltageInputDef;
                voltageOutput = voltageInput / Math.Sqrt(2) * ratioAmplitudeModulation;
                voltageOutputTotal = voltageOutput * number;
                currentOutputRMS = math_Pfull / (voltageOutput * Math.Cos(anglePowerFactor));
            }
            converter.Math_Vin = voltageInput;

            //初始化元器件
            semiconductor = new CHBModule(1) { VoltageVariable = false, MultiNumber = number };
            inductor = new Inductor(1) { VoltageVariable = false };
            components = new Component[] { semiconductor, inductor };
            componentGroups = new Component[1][];
            componentGroups[0] = new Component[] { semiconductor, inductor };

            DesignCircuitParam();
            semiconductor.SetConstants(frequencyGrid, voltageSwitch, ratioFrequencyModulation, timeTurnOnIgbt, timeTurnOnDiode);
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            //基本电路参数
            ratioFrequencyModulation = (int)(frequency / frequencyGrid); //TODO 小数？
            voltageSwitch = voltageInput;

            //生成输出电压波形
            int A = 1;
            curveVoltageOutputTotalBase = new Curve
            {
                Category = "Sine",
                Amplitude = Math.Sqrt(2) * voltageOutputTotal,
                Frequency = frequencyGrid,
                InitialAngle = 0
            };
            curveVoltageOutputTotal = new Curve();
            Curve us = new Curve
            {
                Name = "Us",
                Category = "Sine",
                Amplitude = A * ratioAmplitudeModulation,
                Frequency = frequencyGrid,
                InitialAngle = 0
            };
            Curve[,] uc = new Curve[number, 7];

            double phi = Math.PI;
            for (int i = 0; i < number; i++)
            {
                uc[i, 1] = new Curve
                {
                    Name = "Uc" + (i + 1),
                    Category = "Triangle",
                    Amplitude = A,
                    Frequency = frequency,
                    InitialAngle = phi
                };
                uc[i, 2] = new Curve
                {
                    Name = "Uc" + (i + 1) + "'",
                    Category = "Triangle",
                    Amplitude = A,
                    Frequency = frequency,
                    InitialAngle = Math.PI + phi
                };
                uc[i, 3] = new Curve { Name = "g" + (i + 1) + "1" };
                uc[i, 3].Compare(us, uc[i, 1], 0, 1 / frequencyGrid);
                uc[i, 5] = new Curve { Name = "g" + (i + 1) + "3" };
                uc[i, 5].Compare(us, uc[i, 2], 0, 1 / frequencyGrid);
                uc[i, 4] = new Curve { Name = "g" + (i + 1) + "2" };
                uc[i, 4].Not(uc[i, 5]);
                uc[i, 6] = new Curve { Name = "g" + (i + 1) + "4" };
                uc[i, 6].Not(uc[i, 3]);
                uc[i, 0] = new Curve { Name = "Uo" + (i + 1) };
                uc[i, 0].Drive(uc[i, 3], uc[i, 4], voltageInput);
                curveVoltageOutputTotal.Plus(uc[i, 0]);
                phi -= Math.PI / number;
            }

            //Graph graph = new Graph();
            //graph.Add(curveVoltageOutputTotal);
            //graph.Draw();

            //得到输出电压波形与滤波电感感值
            Curve ioR = new Curve { Name = "Io_Ripple" };
            inductance = ioR.CreateCurrentRipple(curveVoltageOutputTotal, curveVoltageOutputTotalBase, Math.Sqrt(2) * currentOutputRMS * ratioCurrentRipple); //FIXME 电感对功率因素角的影响？
            currentOutputRippleRMS = ioR.CalcRMS();
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
            timeTurnOnIgbt = new double[number, 4][]; //Igbt开通时间
            timeTurnOnDiode = new double[number, 4][]; //Diode开通时间
            for (int i = 0; i < number; i++) //每个模块分开计算
            {
                double[,] dutyCycle = new double[4, ratioFrequencyModulation]; //占空比                                                                                      
                //this.dataGraph[i+2] = new DataGraph("Vo_block"+(i+1), "t", "V");
                for (int j = 0; j < 4; j++)
                {
                    timeTurnOnIgbt[i, j] = new double[ratioFrequencyModulation];
                    timeTurnOnDiode[i, j] = new double[ratioFrequencyModulation];
                }
                for (int k = 0; k < ratioFrequencyModulation; k++)
                { //每个开关周期
                  //计算占空比
                    switch (modulation)
                    {
                        case "PSPWM":
                            dutyCycle[0, k] = 0.5 * ratioAmplitudeModulation * MySin(k, 0) + 0.5;
                            dutyCycle[2, k] = 0.5 * ratioAmplitudeModulation * MySin(k, 0) + 0.5;
                            //						if(2*k*this.frequencyGrid < this.frequency) {
                            //							dutyCycle[0, k] = 1;
                            //							dutyCycle[2, k] = this.ratioAmplitudeModulation*Math.abs(this.mySin(k, 0));
                            //						} else {
                            //							dutyCycle[0, k] = 0;
                            //							dutyCycle[2, k] = 1-this.ratioAmplitudeModulation*Math.abs(this.mySin(k, 0));
                            //						}
                            break;
                        case "LSPWM":
                            dutyCycle[0, k] = number * ratioAmplitudeModulation * MySin(k, 0) - i;
                            if (dutyCycle[0, k] > 1)
                            {
                                dutyCycle[0, k] = 1;
                            }
                            if (dutyCycle[0, k] < 0)
                            {
                                dutyCycle[0, k] = 0;
                            }
                            dutyCycle[2, k] = number * ratioAmplitudeModulation * MySin(k, 0) + i + 1;
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
                    if (MySin(k, -anglePowerFactor) >= 0)
                    {
                        timeTurnOnIgbt[i, 0][k] = dutyCycle[0, k] / frequency;
                        timeTurnOnDiode[i, 0][k] = 0;
                        timeTurnOnIgbt[i, 1][k] = 0;
                        timeTurnOnDiode[i, 1][k] = dutyCycle[1, k] / frequency;
                        timeTurnOnIgbt[i, 2][k] = dutyCycle[2, k] / frequency;
                        timeTurnOnDiode[i, 2][k] = 0;
                        timeTurnOnIgbt[i, 3][k] = 0;
                        timeTurnOnDiode[i, 3][k] = dutyCycle[3, k] / frequency;
                    }
                    else
                    {
                        timeTurnOnIgbt[i, 0][k] = 0;
                        timeTurnOnDiode[i, 0][k] = dutyCycle[0, k] / frequency;
                        timeTurnOnIgbt[i, 1][k] = dutyCycle[1, k] / frequency;
                        timeTurnOnDiode[i, 1][k] = 0;
                        timeTurnOnIgbt[i, 2][k] = 0;
                        timeTurnOnDiode[i, 2][k] = dutyCycle[2, k] / frequency;
                        timeTurnOnIgbt[i, 3][k] = dutyCycle[3, k] / frequency;
                        timeTurnOnDiode[i, 3][k] = 0;
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
            currentOutputRMS = math_P / (voltageOutput * Math.Cos(anglePowerFactor));
            currentSwitch = new double[ratioFrequencyModulation];
            for (int k = 0; k < ratioFrequencyModulation; k++)
            {
                currentSwitch[k] = Math.Sqrt(2) * currentOutputRMS * MySin(k, -anglePowerFactor);
            }
        }

        /// <summary>
        /// 准备评估所需的电路参数
        /// </summary>
        public override void Prepare()
        {
            //计算电路参数
            int n = Config.CGC_POWER_RATIO.Length;

            for (int j = 0; j < n; j++)
            {
                math_P = math_Pfull * Config.CGC_POWER_RATIO[j]; //改变负载
                Simulate();
                //Graph graph = new Graph();
                //graph.Add(currentSwitch_P, "iP");
                //graph.Add(currentSwitch_S, "iS");
                //graph.Draw();

                //设置元器件的电路参数（用于评估）
                semiconductor.AddEvalParameters(0, j, currentSwitch);
                inductor.AddEvalParameters(0, j ,currentOutputRMS, currentOutputRippleRMS);
            }

            //设置元器件的设计条件
            double voltageStressSwitch = voltageInput;
            double currentOutputPeak = Math.Sqrt(2) * currentOutputRMS; //TODO 纹波？
            semiconductor.SetConditions(voltageStressSwitch, currentOutputPeak, frequency);
            inductor.SetConditions(inductance, currentOutputPeak, frequency);
        }

        /// <summary>
		/// 计算相应负载下的电路参数
		/// </summary>
		/// <param name="load">负载</param>
		public override void Calc(double load)
        {
            math_P = math_Pfull * load; //改变负载
            Simulate();
            //设置元器件的电路参数
            semiconductor.SetParameters(currentSwitch);
            inductor.SetParameters(currentOutputRMS, currentOutputRippleRMS, frequency);
        }

        /// <summary>
        /// 生成离散化正弦波
        /// </summary>
        /// <param name="k">第k个开关周期</param>
        /// <param name="angleInitial">初相角(rad)</param>
        /// <returns>第k个开关周期中点对应正弦波</returns>
        private double MySin(int k, double angleInitial)
        {
            return Math.Sin(2 * Math.PI * frequencyGrid * (k + 0.5) / frequency + angleInitial);
        }
    }
}
