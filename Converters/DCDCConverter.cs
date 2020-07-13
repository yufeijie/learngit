using System;
using PV_analysis.Components;
using PV_analysis.Topologys;

namespace PV_analysis.Converters
{
    internal class DCDCConverter : Converter
    {
		//可选参数
		private readonly double ratioCurrentRipple = 0.2; //电流纹波系数
		private readonly double ratioVoltageRipple = 0.1; //电压纹波系数
		private readonly double diodeCurrentDropRate = 125e6; //二极管电流下降速率(A/s)

		//给定参数
		private readonly double m_Vin_min; //输入电压最小值
		private readonly double m_Vin_max; //输入电压最大值
		private readonly double m_Vo; //输出电压
		private readonly int[] numberRange; //可用模块数
		private readonly double[] frequencyRange; //可用开关频率
		private readonly int[] topologyRange; //可用拓扑

		/// <summary>
		/// 初始化
		/// </summary>
		/// <param name="m_Psys">系统功率</param>
		/// <param name="m_Vin_min">最小输入电压</param>
		/// <param name="m_Vin_max">最大输入电压</param>
		/// <param name="m_Vo">输出电压</param>
		/// <param name="numberRange">可用模块数</param>
		/// <param name="topologyRange">可用拓扑</param>
		/// <param name="frequencyRange">可用开关频率</param>
		public DCDCConverter(double m_Psys, double m_Vin_min, double m_Vin_max, double m_Vo, int[] numberRange, int[] topologyRange, double[] frequencyRange)
		{
			this.m_Psys = m_Psys;
			this.m_Vin_min = m_Vin_min;
			this.m_Vin_max = m_Vin_max;
			this.m_Vo = m_Vo;
			this.numberRange = numberRange;
			this.topologyRange = topologyRange;
			this.frequencyRange = frequencyRange;
		}

		/// <summary>
		/// 自动优化拓扑、模块数和开关频率，找到最合适的模块设计
		/// </summary>
		public void Optimize()
		{
			foreach (int tp in topologyRange) //模块数变化
			{
				ThreeLevelBoost topology = new ThreeLevelBoost(this);
				foreach (int n in numberRange)
				{
					Number = n;
					foreach (double fs in frequencyRange)
					{

						Console.WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("%.1f", fs / 1e3) + "kHz");
						topology.Design();
					}
				}
			}
		}
							this.prepare(); //计算电路参数
							if (this.designDevice())
							{ //设计主电路元件
							  //整合各部分设计
								ParetoList design = this.combineDesign(); //整合各部分设计
								int designSize = design.getSize();
								double[] designEfficiency = design.getEfficiency();
								double[] designVolume = design.getVolume();
								double[] designCost = design.getCost();
								String[][] designData = design.getData();
								//						System.out.println(designSize);
								for (int j = 0; j < designSize; j++)
								{
									if (designEfficiency[j] < Config.MIN_EFFICIENCY || designVolume[j] * this.number > Config.MAX_VOLUME || designCost[j] * this.number > Config.MAX_COST)
									{
										continue;
									}
									//记录绘图信息
									//							len++;
									//							x[len] = this.number;
									//							y[len] = this.frequency/1e3;
									//							z[len] = designEfficiency[j]*100;
									//							l[len] = designVolume[j]*this.number;
									//							C[len] = designCost[j]*this.number/1e4;
									//记录最优设计方案
									ArrayList<String> dataArray = new ArrayList<String>();
									dataArray.add(String.format("%.2f", designEfficiency[j] * 100) + "%");
									dataArray.add(String.format("%.2f", designCost[j] * this.number));
									dataArray.add(String.format("%.2f", designVolume[j] * this.number));
									dataArray.add(String.format("%.1f", this.power));
									dataArray.add(String.format("%.2f", this.power / 1e3 / designVolume[j]));
									dataArray.add(Integer.toString(this.number));
									dataArray.add(this.getTopologyName(to));
									dataArray.add(String.format("%.1f", f / 1e3));
									if (this.topology == 3)
									{
										dataArray.add(Integer.toString(this.numberInput));
										dataArray.add(String.format("%.1f", this.inductanceResonance * 1e6));
										dataArray.add(String.format("%.1f", this.capacitanceResonance * 1e9));
									}
									for (String d: designData[j])
									{
										dataArray.add(d);
									}
									dataArray.add(Double.toString(designVolume[j] * this.number));
									dataArray.add(Double.toString(designCost[j] * this.number / 1e4));
									dataArray.add(Double.toString(designEfficiency[j] * 100));
									String[] data = dataArray.toArray(new String[dataArray.size()]);
									this.design.add(designEfficiency[j] * 100, designVolume[j] * this.number, designCost[j] * this.number / 1e4, data);
									if (isRecordResult)
									{
										dataList.add(data);
									}
									//							graphResult.addData(designVolume[j]*this.number, designCost[j]*this.number/1e4, designEfficiency[j]*100);
									//							String[] dataGraph = new String[20];
									//							dataGraph[0] = String.format("%.2f", designEfficiency[j]*100)+"%";
									//							dataGraph[1] = String.format("%.2f", designCost[j]*this.number);
									//							dataGraph[2] = String.format("%.2f", designVolume[j]*this.number);
									//							dataGraph[3] = String.format("%.1f", this.power);
									//							dataGraph[5] = Integer.toString(this.number);
									//							dataGraph[7] = String.format("%.1f", this.frequency/1e3);
									//							dataGraph[8] = designData[j][0];
									//							dataGraph[17] = Double.toString(designVolume[j]*this.number);
									//							dataGraph[18] = Double.toString(designCost[j]*this.number/1e4);
									//							dataGraph[19] = Double.toString(designEfficiency[j]*100);
									//							graph.addData(dataGraph);
								}
								//						this.designPareto.display();
							}
						}
					}
				}
			}
		}
		}
		}

	/**
	 * @method prepare
	 * @description 计算电路参数
	 * @return 是否可以进行器件设计
	 */
	public void prepare()
	{
		//初始化主电路元件参数
		switch (this.topology)
		{
			case 0: //三电平
				this.deviceSwitchNum = 1;
				this.deviceSwitch[0] = new Semiconductor(2);
				this.deviceInductorNum = 1;
				this.deviceInductor[0] = new Inductor(1);
				this.deviceCapacitorNum = 1;
				this.deviceCapacitor[0] = new Capacitor(2);
				break;
			case 1: //交错并联
				this.deviceSwitchNum = 1;
				this.deviceSwitch[0] = new Semiconductor(2);
				this.deviceInductorNum = 1;
				this.deviceInductor[0] = new Inductor(2);
				this.deviceCapacitorNum = 1;
				this.deviceCapacitor[0] = new Capacitor(1);
				break;
			case 2: //两电平
				this.deviceSwitchNum = 1;
				this.deviceSwitch[0] = new Semiconductor(1);
				this.deviceInductorNum = 1;
				this.deviceInductor[0] = new Inductor(1);
				this.deviceCapacitorNum = 1;
				this.deviceCapacitor[0] = new Capacitor(1);
				break;
			case 3: //双路软开关
				this.deviceSwitchNum = 3;
				this.deviceSwitch[0] = new Semiconductor(this.numberInput);
				this.deviceSwitch[0].setCategory("Main_CAC");
				this.deviceSwitch[0].setEvaluatedAtDiffInputVoltage(true);
				this.deviceSwitch[1] = new Semiconductor(this.numberInput);
				this.deviceSwitch[1].setCategory("Diode_CAC");
				this.deviceSwitch[1].setEvaluatedAtDiffInputVoltage(true);
				this.deviceSwitch[2] = new Semiconductor(1);
				this.deviceSwitch[2].setCategory("Auxiliary_CAC");
				this.deviceSwitch[2].setEvaluatedAtDiffInputVoltage(true);
				this.deviceInductorNum = 2;
				this.deviceInductor[0] = new Inductor(1);
				this.deviceInductor[0].setCategory("Resonance");
				this.deviceInductor[0].setEvaluatedAtDiffInputVoltage(true);
				this.deviceInductor[1] = new Inductor(this.numberInput);
				this.deviceInductor[1].setCategory("Filter");
				this.deviceInductor[1].setEvaluatedAtDiffInputVoltage(true);
				this.deviceCapacitorNum = 2;
				//			this.deviceCapacitor[0] = new Capacitor(this.numberInput+1);
				//			this.deviceCapacitor[0].setCategory("Resonance");
				//			this.deviceCapacitor[0].setEvaluatedAtDiffInputVoltage(true);
				this.deviceCapacitor[0] = new Capacitor(1);
				this.deviceCapacitor[0].setCategory("Clamping");
				this.deviceCapacitor[0].setEvaluatedAtDiffInputVoltage(true);
				this.deviceCapacitor[1] = new Capacitor(1);
				this.deviceCapacitor[1].setCategory("Filter");
				this.deviceCapacitor[1].setEvaluatedAtDiffInputVoltage(true);
				this.currentSwitch = new Curve[3][5][7];
				this.timeVoltageRiseSwitch = new double[3][5][7];
				this.currentInductor = new Curve[2][5][7];
				this.currentCapacitor = new Curve[3][5][7];
				this.currentCapacitorClampingRMSMax = 0;
				this.currentCapacitorFilterRMSMax = 0;
		}

		switch (this.topology)
		{
			case 0:
			case 1:
			case 2:
				//计算并设定主电路元件设计条件
				this.setDesignCondition();

				//得到用于效率评估的不同输入电压与不同功率点的电路参数
				double p = this.power;
				//			Photovoltaic.setPowerMaximumPowerPoint(p); //通过改变并联数来调整光伏电池最大输出功率
				for (int i = 0; i < Array.getLength(Config.CGC_VOLTAGE_RATIO); i++)
				{
					this.voltageInput = this.voltageInputMin + (this.voltageInputMax - this.voltageInputMin) * Config.CGC_VOLTAGE_RATIO[i];
					for (int j = 0; j < Array.getLength(Config.CGC_POWER_RATIO); j++)
					{
						this.power = p * Config.CGC_POWER_RATIO[j]; //改变模块功率
																	//					Photovoltaic.updatePowerMaximumPowerPoint(this.power); //通过改变辐照度来调整光伏电池最大输出功率
																	//					this.voltageInput = Photovoltaic.getVoltageMaximumPowerPoint(); //得到最大功率点电压
						this.calcCircuitParam(); //计算电路参数
						this.addDeviceCircuitParamForEvaluation(i, j); //将相关参数添加到主电路元件中
					}
				}
				break;
			case 3:
				//设计电路参数
				this.voltageInput = this.voltageInputMin;
				this.designCircuitParam();

				this.currentSwitchPeakMax = 0;
				this.currentSwitchPeakMax2 = 0;
				this.currentSwitchPeakMax3 = 0;
				this.currentInductorPeakMax = 0;
				this.currentInductorResonancePeakMax = 0;
				this.currentCapacitorClampingRMSMax = 0;
				this.currentCapacitorFilterRMSMax = 0;
				this.voltageCapacitorResonanceMax = 0;
				this.voltageCapacitorClampingMax = 0;
				this.voltageCapacitorFilterMax = 0;

				p = this.power;
				for (int i = 0; i < Array.getLength(Config.CGC_VOLTAGE_RATIO); i++)
				{
					this.voltageInput = this.voltageInputMin + (this.voltageInputMax - this.voltageInputMin) * Config.CGC_VOLTAGE_RATIO[i];
					for (int j = 0; j < Array.getLength(Config.CGC_POWER_RATIO); j++)
					{
						this.power = p * Config.CGC_POWER_RATIO[j];
						this.curveSimulation(i, j); //波形模拟
						this.addCurveForEvaluation(i, j); //将相关参数添加到主电路元件中
					}
					//				GraphXY graphWaveform = new GraphXY("Waveform");
					//				GraphXY graphWaveform2 = new GraphXY("Waveform");
					//				GraphXY graphWaveform3 = new GraphXY("Waveform");
					//				GraphXY graphWaveform4 = new GraphXY("Waveform");
					//				GraphXY graphWaveform5 = new GraphXY("Waveform");
					//				for(int j = 0; j < Array.getLength(Config.CGC_POWER_RATIO); j++) {
					//					graphWaveform.addData(this.currentInductor[0][i][j]);
					//					graphWaveform2.addData(this.currentInductor[1][i][j]);
					//					graphWaveform3.addData(this.currentSwitch[0][i][j]);
					//					graphWaveform4.addData(this.currentSwitch[1][i][j]);
					//					graphWaveform5.addData(this.currentSwitch[2][i][j]);
					//				}
					//				graphWaveform.drawX(this.currentInductor[0][i][6].getXLabel(), this.currentInductor[0][i][6].getYLabel(), "n="+this.number);
					//				graphWaveform2.drawX(this.currentInductor[1][i][6].getXLabel(), this.currentInductor[1][i][6].getYLabel(), "n="+this.number);
					//				graphWaveform3.drawX(this.currentSwitch[0][i][6].getXLabel(), this.currentSwitch[0][i][6].getYLabel(), "n="+this.number);
					//				graphWaveform4.drawX(this.currentSwitch[1][i][6].getXLabel(), this.currentSwitch[1][i][6].getYLabel(), "n="+this.number);
					//				graphWaveform5.drawX(this.currentSwitch[2][i][6].getXLabel(), this.currentSwitch[2][i][6].getYLabel(), "n="+this.number);
				}
				this.setDesignCondition();
				break;
		}

		//		this.deviceCapacitorNum = 0; //不考虑电容
	}

	/**
	 * @method prepareCircuitParam
	 * @description 准备电路参数
	 */
	public void prepareCircuitParam()
	{
		switch (this.topology)
		{
			case 0:
			case 1:
			case 2:
				this.voltageInput = this.voltageInputMin;
				this.calcCircuitParam(); //计算电路参数
				this.setDeviceCircuitParam(); //改变主电路元件相关电路参数
				break;
			case 3:
				break; //FIXME ???
		}
	}

	/**
	 * @method calc
	 * @description 根据当前设定的模块参数直接进行效率计算
	 * @param type
	 */
	public void calc()
	{
		switch (this.topology)
		{
			case 0:
			case 1:
			case 2:
				this.evaluate();
				this.voltageInput = this.voltageInputMin;
				this.calcCircuitParam(); //计算电路参数 FIXME 重复
				this.setDeviceCircuitParam(); //改变主电路元件相关电路参数
				this.calcPowerLoss(); //计算效率与损耗
				this.calcVolume(); //计算体积与功率密度
				this.calcCost(); //计算成本
				break;
			case 3:
				this.evaluate();
				this.setDeviceCurve(0, 6);
				this.calcPowerLoss(); //计算效率与损耗
				this.calcVolume(); //计算体积与功率密度
				this.calcCost(); //计算成本
				break;
		}
	}

	/**
	 * @method designResonanceParam
	 * @description 谐振参数设计
	 * @return 所有谐振设计结果
	 */
	private double[][] designResonanceParam()
	{
		ArrayList<Double> listLr = new ArrayList<Double>();
		ArrayList<Double> listCr = new ArrayList<Double>();
		switch (this.topology)
		{ //拓扑适配
			case 3: //CAC
					//读取参数
				double P = this.power;
				int nI = this.numberInput;
				double Vo = this.voltageOutput;
				double fs = this.frequency;
				double Io = P / Vo;
				//谐振参数设计
				double Lrmin = Vo / this.diodeCurrentDropRate; //谐振电感最小值，抑制二极管反向恢复电流
				double Lr = Lrmin;
				double dLr = 0.2e-6; //谐振电感变化值（取决于精度）
				double Cr = 0; //=nI*Cs+Ca
				double Crmin = 200e-12 * (nI + 1); //谐振电容最小值，需大于MOS管等效输出电容
				double dCr = 200e-12; //谐振电容变化值（取决于精度）
				double D0max = 1 - 1 / (1 + 0.1); //最大丢失占空比，取决于Vo+Vcc，这里取Vo+Vcc<1.1Vo
				while (Lr <= 1.5 * Lrmin)
				{
					Cr = Crmin;
					double D0 = 0;
					while (Cr <= 3 * Crmin)
					{
						//检查丢失占空比
						try
						{
							Object[] input = { Vo, Io, fs, Lr, Cr };
							Object[] output = this.solveMATLAB.solve_CACboost_D0(1, input);
							MWNumericArray result = (MWNumericArray)output[0];
							D0 = result.getDouble(1);
							if (D0 < 0 || D0 > 0.5)
							{
								System.out.println("Wrong D0!");
								System.exit(-1);
							}
						}
						catch (Exception e)
						{
							System.out.println("Exception: " + e.toString());
						}
						if (D0 > D0max)
						{
							break;
						}
						//检查谐振周期
						double Tr = 2 * Math.PI * Math.sqrt(Lr * Cr);
						if (Tr >= 400e-9)
						{//找到一组可行谐振参数
							listLr.add(Lr);
							listCr.add(Cr);
						}
						Cr += dCr;
					}
					if (Cr == Crmin)
					{ //此时已超出D0限制，说明无可用谐振参数设计
						break;
					}
					Lr += dLr;
				}
				break;
		}
		if (listLr.size() > 0)
		{
			//			//谐振参数不优化
			//			double[][] result = new double[1][2];
			//			result[0][0] = this.voltageOutput/this.diodeCurrentDropRate;
			//			result[0][1] = 200e-12*(this.numberInput+1)*3;
			//谐振参数优化
			double[][] result = new double[listLr.size()][2];
			for (int i = 0; i < listLr.size(); i++)
			{
				result[i][0] = listLr.get(i);
				result[i][1] = listCr.get(i);
			}
			return result;
		}
		else
		{
			return null;
		}
	}

	/**
	 * @method designCircuitParam
	 * @description 设计电路无源元件参数
	 */
	private void designCircuitParam()
	{
		switch (this.topology)
		{ //拓扑适配
			case 3: //CAC
					//读取参数
				double P = this.power;
				int nI = this.numberInput;
				double Vin = this.voltageInput;
				double Vo = this.voltageOutput;
				double fs = this.frequency;
				double Lr = this.inductanceResonance;
				double Cr = this.capacitanceResonance;
				double Iin = P / (nI * Vin);
				double Io = P / Vo;
				double IL = Iin;
				double ILrip = 0.2 * IL;

				//升压电感参数计算
				double D0 = 0;
				try
				{
					Object[] input = { Vo, Io, fs, Lr, Cr };
					Object[] output = this.solveMATLAB.solve_CACboost_D0(1, input);
					MWNumericArray result = (MWNumericArray)output[0];
					D0 = result.getDouble(1);
					if (D0 < 0 || D0 > 0.5)
					{
						System.out.println("Wrong D0!");
						System.exit(-1);
					}
				}
				catch (Exception e)
				{
					System.out.println("Exception: " + e.toString());
				}
				double Vcc = Formula.CAC_boost_Vcc(D0, Vo);
				double D = 1 - Vin / (Vo + Vcc);
				double L = D * Vin / ILrip / fs;
				double Cc = 1 / (Lr * Math.pow(0.1 * 2 * Math.PI * fs, 2));
				double kvrip = this.ratioVoltageRipple;
				double Cdc = D * Io / (fs * Vo * kvrip);

				this.inductance = L;
				this.capacitanceClamping = Cc;
				this.capacitanceFilter = Cdc;
				break;
		}
	}

	/**
	 * @method calcCircuitParam
	 * @description 计算电路参数
	 */
	private void calcCircuitParam()
	{
		//基本电路参数
		this.dutyCycle = 1 - this.voltageInput / this.voltageOutput;
		this.timeCycle = 1 / this.frequency;
		this.time1 = (this.dutyCycle - 0.5) * this.timeCycle;
		this.time2 = 0.5 * this.timeCycle;
		this.time3 = this.dutyCycle * this.timeCycle;
		this.time4 = (this.dutyCycle + 0.5) * this.timeCycle;
		this.currentInput = this.power / this.voltageInput;
		this.currentOutput = this.power / this.voltageOutput;
		switch (this.topology)
		{ //拓扑适配 FIXME
			case 0: //三电平
					//电感电流 FIXME 平均值or连续值
				this.currentInductorAverage = this.currentInput;
				if (this.dutyCycle == 0)
				{
					this.currentInductorRipple = 0;
				}
				else
				{
					this.currentInductorRipple = this.currentInductorAverage * this.ratioCurrentRipple;
				}
				this.currentInductorPeak = this.currentInductorAverage + this.currentInductorRipple * 0.5;
				this.currentInductorTrough = this.currentInductorAverage - this.currentInductorRipple * 0.5;
				//感值
				if (this.dutyCycle == 0)
				{
					this.inductance = 0;
				}
				else if (this.dutyCycle > 0.5)
				{
					this.inductance = (this.dutyCycle - 0.5) * this.voltageInput / (this.frequency * this.currentInductorRipple);
				}
				else
				{
					this.inductance = this.dutyCycle * (this.voltageInput - 0.5 * this.voltageOutput) / (this.frequency * this.currentInductorRipple);
				}
				//开关器件电压
				this.voltageSwitch = this.voltageOutput * 0.5;
				//IGBT电流波形
				if (this.dutyCycle > 0.5)
				{
					this.currentWaveformIgbt[0] = 4; //线性化处理后，只有四个点
					this.currentWaveformIgbt[1] = 0; //点1横坐标
					this.currentWaveformIgbt[2] = this.currentInductorTrough; //点1纵坐标
					this.currentWaveformIgbt[3] = this.time1; //点2横坐标
					this.currentWaveformIgbt[4] = this.currentInductorPeak; //点2纵坐标
					this.currentWaveformIgbt[5] = this.time2; //点3横坐标
					this.currentWaveformIgbt[6] = this.currentInductorTrough; //点3纵坐标
					this.currentWaveformIgbt[7] = this.time3; //点4横坐标
					this.currentWaveformIgbt[8] = this.currentInductorPeak; //点4纵坐标
				}
				else
				{
					this.currentWaveformIgbt[0] = 2;
					this.currentWaveformIgbt[1] = 0;
					this.currentWaveformIgbt[2] = this.currentInductorTrough;
					this.currentWaveformIgbt[3] = this.time3;
					this.currentWaveformIgbt[4] = this.currentInductorPeak;
				}
				//二极管电流波形
				if (this.dutyCycle > 0.5)
				{
					this.currentWaveformDiode[0] = 2;
					this.currentWaveformDiode[1] = this.time3;
					this.currentWaveformDiode[2] = this.currentInductorPeak;
					this.currentWaveformDiode[3] = this.timeCycle;
					this.currentWaveformDiode[4] = this.currentInductorTrough;
				}
				else
				{
					this.currentWaveformDiode[0] = 4;
					this.currentWaveformDiode[1] = this.time3;
					this.currentWaveformDiode[2] = this.currentInductorPeak;
					this.currentWaveformDiode[3] = this.time2;
					this.currentWaveformDiode[4] = this.currentInductorTrough;
					this.currentWaveformDiode[5] = this.time4;
					this.currentWaveformDiode[6] = this.currentInductorPeak;
					this.currentWaveformDiode[7] = this.timeCycle;
					this.currentWaveformDiode[8] = this.currentInductorTrough;
				}
				//电容电压
				this.voltageCapacitorAverage = this.voltageOutput * 0.5;
				this.voltageCapacitorRipple = this.voltageCapacitorAverage * this.ratioVoltageRipple;
				//容值
				this.capacitance = this.dutyCycle * this.currentOutput / (this.frequency * this.voltageCapacitorRipple);
				//电容电流 FIXME 有效值的求取
				double f1 = this.currentInductorTrough - this.currentOutput;
				double f2 = this.currentInductorPeak - this.currentOutput;
				double c;
				if (this.dutyCycle > 0.5)
				{
					c = Math.pow(this.currentOutput, 2) * this.time3 + myIntegral(this.time3, f2, f2, this.timeCycle, f1, f1);
				}
				else if (this.dutyCycle < 0.5 && this.dutyCycle > 0)
				{
					c = Math.pow(this.currentOutput, 2) * this.time3 +
						 myIntegral(this.time3, f2, f2, this.time2, f1, f1) +
						 myIntegral(this.time2, f1, f1, this.time4, f2, f2) +
						 myIntegral(this.time4, f2, f2, this.timeCycle, f1, f1);
				}
				else
				{
					c = 0;
				}
				this.currentCapacitorRMS = Math.sqrt(this.frequency * c);
				break;
			case 1: //交错并联
					//电感电流
				this.currentInductorAverage = this.currentInput * 0.5;
				if (this.dutyCycle == 0)
				{
					this.currentInductorRipple = 0;
				}
				else
				{
					this.currentInductorRipple = this.currentInductorAverage * this.ratioCurrentRipple;
				}
				this.currentInductorPeak = this.currentInductorAverage + this.currentInductorRipple * 0.5;
				this.currentInductorTrough = this.currentInductorAverage - this.currentInductorRipple * 0.5;
				//感值
				if (this.dutyCycle == 0)
				{
					this.inductance = 0;
				}
				else
				{
					this.inductance = this.dutyCycle * this.voltageInput / (this.frequency * this.currentInductorRipple);
				}
				//开关器件电压
				this.voltageSwitch = this.voltageOutput;
				//IGBT电流波形
				this.currentWaveformIgbt[0] = 2;
				this.currentWaveformIgbt[1] = 0;
				this.currentWaveformIgbt[2] = this.currentInductorTrough;
				this.currentWaveformIgbt[3] = this.time3;
				this.currentWaveformIgbt[4] = this.currentInductorPeak;
				//二极管电流波形
				this.currentWaveformDiode[0] = 2;
				this.currentWaveformDiode[1] = this.time3;
				this.currentWaveformDiode[2] = this.currentInductorPeak;
				this.currentWaveformDiode[3] = this.timeCycle;
				this.currentWaveformDiode[4] = this.currentInductorTrough;
				//电容电压
				this.voltageCapacitorAverage = this.voltageOutput;
				this.voltageCapacitorRipple = this.voltageCapacitorAverage * this.ratioVoltageRipple;
				//容值
				if (this.dutyCycle > 0.5)
				{
					this.capacitance = (this.dutyCycle - 0.5) * this.currentOutput / (this.frequency * this.voltageCapacitorRipple);
				}
				else
				{
					this.capacitance = this.dutyCycle * (this.currentOutput - this.currentInductorAverage) / (this.frequency * this.voltageCapacitorRipple);
				}
				//电容电流
				if (this.dutyCycle > 0.5)
				{
					f1 = this.currentInductorTrough - this.currentOutput;
					f2 = this.currentInductorPeak - this.currentOutput;
					c = (Math.pow(this.currentOutput, 2) * this.time1 + myIntegral(this.time1, f2, f2, this.time2, f1, f1)) * 2;
				}
				else if (this.dutyCycle < 0.5 && this.dutyCycle > 0)
				{
					double i1 = this.currentInductorPeak - this.currentInductorRipple / (this.timeCycle - this.time3) * (this.time2 - this.time3);
					double i2 = this.currentInductorPeak - this.currentInductorRipple / (this.timeCycle - this.time3) * (this.time4 - this.time3);
					f1 = i1 - this.currentOutput;
					f2 = i2 - this.currentOutput;
					double f3 = this.currentInductorPeak + i2 - this.currentOutput;
					double f4 = this.currentInductorTrough + i1 - this.currentOutput;
					c = (myIntegral(0, f2, f2, this.time3, f1, f1) + myIntegral(this.time3, f3, f3, this.time2, f4, f4)) * 2;
				}
				else
				{
					c = 0;
				}
				this.currentCapacitorRMS = Math.sqrt(this.frequency * c);
				break;
			case 2: //两电平
					//电感电流
				this.currentInductorAverage = this.currentInput;
				if (this.dutyCycle == 0)
				{
					this.currentInductorRipple = 0;
				}
				else
				{
					this.currentInductorRipple = this.currentInductorAverage * this.ratioCurrentRipple;
				}
				this.currentInductorPeak = this.currentInductorAverage + this.currentInductorRipple * 0.5;
				this.currentInductorTrough = this.currentInductorAverage - this.currentInductorRipple * 0.5;
				//感值
				if (this.dutyCycle == 0)
				{
					this.inductance = 0;
				}
				else
				{
					this.inductance = this.dutyCycle * this.voltageInput / (this.frequency * this.currentInductorRipple);
				}
				//开关器件电压
				this.voltageSwitch = this.voltageOutput;
				//IGBT电流波形
				this.currentWaveformIgbt[0] = 2;
				this.currentWaveformIgbt[1] = 0;
				this.currentWaveformIgbt[2] = this.currentInductorTrough;
				this.currentWaveformIgbt[3] = this.time3;
				this.currentWaveformIgbt[4] = this.currentInductorPeak;
				//二极管电流波形
				this.currentWaveformDiode[0] = 2;
				this.currentWaveformDiode[1] = this.time3;
				this.currentWaveformDiode[2] = this.currentInductorPeak;
				this.currentWaveformDiode[3] = this.timeCycle;
				this.currentWaveformDiode[4] = this.currentInductorTrough;
				//电容电压
				this.voltageCapacitorAverage = this.voltageOutput;
				this.voltageCapacitorRipple = this.voltageCapacitorAverage * this.ratioVoltageRipple;
				//容值
				this.capacitance = this.dutyCycle * this.currentOutput / (this.frequency * this.voltageCapacitorRipple);
				//电容电流
				f1 = this.currentInductorTrough - this.currentOutput;
				f2 = this.currentInductorPeak - this.currentOutput;
				c = Math.pow(this.currentOutput, 2) * this.time3 + myIntegral(this.time3, f2, f2, this.timeCycle, f1, f1);
				this.currentCapacitorRMS = Math.sqrt(this.frequency * c);
				break;
		}
	}

	/**
	 * @method addDeviceCircuitParamForEvaluation
	 * @description 为主电路元件添加一组用于评估的电路参数
	 * @param m 输入电压对应编号
	 * @param n 功率点对应编号
	 */
	private void addDeviceCircuitParamForEvaluation(int m, int n)
	{
		//开关器件
		this.deviceSwitch[0].addCircuitParamForEvaluation(
				m,
				n,
				this.voltageSwitch,
				this.currentWaveformIgbt,
				this.currentWaveformDiode
				);
		//电感
		this.deviceInductor[0].addCircuitParamForEvaluation(
				m,
				n,
				this.currentInductorAverage,
				this.currentInductorRipple
				);
		//电容
		this.deviceCapacitor[0].addCircuitParamForEvaluation(
				m,
				n,
				this.currentCapacitorRMS
				);
	}

	/**
	 * @method setDeviceCircuitParam
	 * @description 设定主电路元件的电路参数
	 */
	private void setDeviceCircuitParam()
	{
		this.deviceSwitch[0].setCircuitParam(
				this.voltageSwitch,
				this.currentWaveformIgbt,
				this.currentWaveformDiode
				);
		this.deviceInductor[0].setCircuitParam(
				this.currentInductorAverage,
				this.currentInductorRipple
				);
		this.deviceCapacitor[0].setCircuitParam(
				this.currentCapacitorRMS
				);
	}

	/**
	 * @method setDeviceCurve
	 * @description 设定主电路元件的波形
	 * @param m 输入电压对应编号
	 * @param n 功率点对应编号
	 */
	private void setDeviceCurve(int m, int n)
	{
		switch (this.topology)
		{
			case 3: //CAC
				for (int i = 0; i < this.deviceSwitchNum; i++)
				{
					this.deviceSwitch[i].setCurve(m, n);
				}
				for (int i = 0; i < this.deviceInductorNum; i++)
				{
					this.deviceInductor[i].setCircuitParam(m, n);
				}
				break;
		}
	}

	/**
	 * @method curveSimulation
	 * @description 波形模拟
	 * @param m 输入电压对应编号
	 * @param n 功率点对应编号
	 */
	private void curveSimulation(int m, int n)
	{
		switch (this.topology)
		{ //拓扑适配
			case 3:
				//初始化
				this.currentSwitchPeak = 0;
				this.currentSwitchPeak2 = 0;
				this.currentSwitchPeak3 = 0;
				//读取参数
				double P = this.power;
				int nI = this.numberInput;
				double Vin = this.voltageInput;
				double Vo = this.voltageOutput;
				double fs = this.frequency;
				double Ts = 1 / fs;
				double Iin = P / (nI * Vin);
				double Io = P / Vo;
				double L = this.inductance;
				double Lr = this.inductanceResonance;
				double Cr = this.capacitanceResonance;
				double Cs = Cr / (nI + 1);
				double wr = 1 / Math.sqrt(Lr * Cr);
				double Zr = wr * Lr;
				boolean isCCM = true;

				double D0 = 0;
				//求解D0
				try
				{
					Object[] input = { Vo, Io, fs, Lr, Cr };
					Object[] output = this.solveMATLAB.solve_CACboost_D0(1, input);
					MWNumericArray result = (MWNumericArray)output[0];
					D0 = result.getDouble(1);
					if (D0 < 0 || D0 > 0.5)
					{
						System.out.println("Wrong D0!");
						System.exit(-1);
					}
				}
				catch (Exception e)
				{
					System.out.println("Exception: " + e.toString());
				}
				double Vcc = Formula.CAC_boost_Vcc(D0, Vo);
				double ILr3 = Formula.CAC_boost_ILr3(D0, Vo, fs, Zr);
				double D = 1 - Vin / (Vo + Vcc);
				double IL = Iin;
				double ILrip = D * Vin / (L * fs);
				double ILmin = IL - 0.5 * ILrip;
				double ILmax = IL + 0.5 * ILrip;
				if (ILmin < 0)
				{
					isCCM = false;
					D = Math.sqrt(2 * IL * L * fs * (Vo + Vcc - Vin) / (Vin * (Vo + Vcc)));
					ILmin = 0;
					ILrip = D * Vin / (L * fs);
					ILmax = ILrip;
				}
				double IL0 = ILmin;

				double t3 = 0;
				//求解t3
				try
				{
					Object[] input = { Vo, Vcc, IL0, ILr3, fs, Lr, Cr, (double)nI };
					Object[] output = this.solveMATLAB.solve_CACboost_t3(1, input);
					MWNumericArray result = (MWNumericArray)output[0];
					t3 = result.getDouble(1);
					if (t3 < 0 || t3 > Ts)
					{
						System.out.println("Wrong t3!");
						System.exit(-1);
					}
				}
				catch (Exception e)
				{
					System.out.println("Exception: " + e.toString());
				}
				double ILr0 = ILr3 + Vcc / Lr * (Ts - t3);
				double A0 = Math.sqrt(Vcc * Vcc + Math.pow(Zr * (nI * IL0 - ILr0), 2));
				double φ0 = Math.atan(Vcc / (Zr * (nI * IL0 - ILr0)));
				if (Math.sin(φ0) <= 0)
				{
					φ0 += Math.PI;
				}
				double t1 = (Math.PI - Math.asin(-Vo / A0) - φ0) / wr;
				double ILr1 = nI * IL0 - A0 / Zr * Math.cos(wr * t1 + φ0);
				double t2 = Lr / Vo * ILr1 + t1;
				double t4 = t1 + D * Ts;

				double ILrmin = ILr3;
				double ILrmax = ILr0;
				double ILrrip = ILrmax - ILrmin;
				this.currentInductorResonancePeak = ILrmax;
				this.currentInductorResonanceRipple = ILrrip;
				this.currentInductorPeak = ILmax;
				this.currentInductorRipple = ILrip;

				//ZVS off波形模拟
				double wr2 = 1 / Math.sqrt(L * nI * Cs);
				double Zr2 = wr2 * L;
				double A2 = Math.sqrt(Vin * Vin + Math.pow(Zr2 * ILmax, 2));
				double φ2 = Math.asin(-Vin / A2);
				double t4a = (Math.asin((Vo + Vcc - Vin) / A2) - φ2) / wr2 + t4;

				this.currentInductor[0][m][n] = new Curve("iLr_" + n, "t(ms)", "iLr(A)");
				this.currentInductor[0][m][n].createSimulation();
				this.currentInductor[1][m][n] = new Curve("iL_" + n, "t(ms)", "iL(A)");
				this.currentInductor[1][m][n].createSimulation();
				this.voltageSwitch = Vo + Vcc;
				this.currentSwitch[0][m][n] = new Curve("is1_" + n, "t/ms", "is1/A");
				this.currentSwitch[0][m][n].createSimulation();
				this.currentSwitch[1][m][n] = new Curve("id1_" + n, "t/ms", "id1/A");
				this.currentSwitch[1][m][n].createSimulation();
				this.currentSwitch[2][m][n] = new Curve("isa_" + n, "t/ms", "isa/A");
				this.currentSwitch[2][m][n].createSimulation();
				this.currentCapacitor[1][m][n] = new Curve("icc_" + n, "t/ms", "icc/A");
				this.currentCapacitor[1][m][n].createSimulation();
				this.currentCapacitor[2][m][n] = new Curve("icdc_" + n, "t/ms", "icdc/A");
				this.currentCapacitor[2][m][n].createSimulation();
				this.timeVoltageRiseSwitch[0][m][n] = t4a - t4;
				this.timeVoltageRiseSwitch[2][m][n] = t1;
				double startTime = 0;
				double endTime = Ts;
				double dt = (endTime - startTime) / Config.DEGREE;
				double t = 0;
				for (int i = 0; i <= Config.DEGREE; i++)
				{
					t = startTime + dt * i;
					double iL;
					if (t < t1)
					{
						if (isCCM)
						{
							iL = ILmin + (Vin - Vo - Vcc) / L * (t - t1);
						}
						else
						{
							iL = IL0;
						}
					}
					else
					{
						if (t < t4)
						{
							iL = ILmin + Vin / L * (t - t1);
						}
						else
						{
							iL = ILmax + (Vin - Vo - Vcc) / L * (t - t4);
							if (!isCCM && iL < 0)
							{
								iL = 0;
							}
						}
					}
					double iLr;
					if (t < t1)
					{
						iLr = nI * ILmin - A0 / Zr * Math.cos(wr * t + φ0);
					}
					else
					{
						if (t < t2)
						{
							iLr = ILr1 - Vo / Lr * (t - t1);
						}
						else
						{
							if (t < t3)
							{
								iLr = -Vo / Zr * Math.sin(wr * (t - t2));
							}
							else
							{
								iLr = ILr3 + Vcc / Lr * (t - t3);
							}
						}
					}
					double is1;
					if (t < t1)
					{
						is1 = 0;
					}
					else
					{
						if (t < t2)
						{
							is1 = iL - iLr / nI;
						}
						else
						{
							if (t < t4)
							{
								is1 = iL;
							}
							else
							{
								is1 = 0;
							}
						}
					}
					double id1;
					if (t < t1)
					{
						id1 = iL;
					}
					else
					{
						if (t < t2)
						{
							id1 = iLr / nI;
						}
						else
						{
							if (t < t4)
							{
								id1 = 0;
							}
							else
							{
								id1 = iL;
							}
						}
					}
					double isa;
					if (t < t3)
					{
						isa = 0;
					}
					else
					{
						if (t < t4)
						{
							isa = iLr;
						}
						else
						{
							isa = iLr - nI * iL;
						}
					}
					double icc = -isa;
					double icdc = icc + iLr - Io;
					this.currentInductor[0][m][n].add(t, iLr);
					this.currentInductor[1][m][n].add(t, iL);
					this.currentSwitch[0][m][n].add(t, is1);
					this.currentSwitch[1][m][n].add(t, id1);
					this.currentSwitch[2][m][n].add(t, isa);
					this.currentCapacitor[1][m][n].add(t, icc);
					this.currentCapacitor[2][m][n].add(t, icdc);
					this.currentSwitchPeak = Math.max(this.currentSwitchPeak, Math.abs(is1)); //记录峰值
					this.currentSwitchPeak2 = Math.max(this.currentSwitchPeak2, Math.abs(id1)); //记录峰值
					this.currentSwitchPeak3 = Math.max(this.currentSwitchPeak3, Math.abs(isa)); //记录峰值
				}
				//			//修正主电路元件波形 FIXME 保证现有损耗计算方法正确
				Curve a = new Curve("is1_" + n, "t/s", "is1/A");
				a.cut(this.currentSwitch[0][m][n], 0, t4, 1);
				this.currentSwitch[0][m][n] = a;
				//计算有效值
				this.currentInductorResonanceRMS = this.currentInductor[0][m][n].calRMS();
				this.currentInductorRMS = this.currentInductor[1][m][n].calRMS();
				this.currentCapacitorClampingRMS = this.currentCapacitor[1][m][n].calRMS();
				this.currentCapacitorFilterRMS = this.currentCapacitor[2][m][n].calRMS();
				//记录最大值
				this.currentSwitchPeakMax = Math.max(this.currentSwitchPeakMax, this.currentSwitchPeak);
				this.currentSwitchPeakMax2 = Math.max(this.currentSwitchPeakMax2, this.currentSwitchPeak2);
				this.currentSwitchPeakMax3 = Math.max(this.currentSwitchPeakMax3, this.currentSwitchPeak3);
				this.currentInductorResonancePeakMax = Math.max(this.currentInductorResonancePeakMax, this.currentInductorResonancePeak);
				this.currentInductorPeakMax = Math.max(this.currentInductorPeakMax, this.currentInductorPeak);
				this.currentCapacitorClampingRMSMax = Math.max(this.currentCapacitorClampingRMSMax, this.currentCapacitorClampingRMS);
				this.currentCapacitorFilterRMSMax = Math.max(this.currentCapacitorFilterRMSMax, this.currentCapacitorFilterRMS);
				this.voltageCapacitorResonanceMax = Math.max(this.voltageCapacitorResonanceMax, Vo + Vcc);
				this.voltageCapacitorClampingMax = Math.max(this.voltageCapacitorClampingMax, Vcc);
				this.voltageCapacitorFilterMax = Math.max(this.voltageCapacitorFilterMax, Vo);
				break;
		}
	}

	/**
	 * @method addCurveForEvaluation
	 * @description 为主电路元件添加一组用于评估的波形
	 * @param m 输入电压对应编号
	 * @param n 功率点对应编号
	 */
	private void addCurveForEvaluation(int m, int n)
	{
		switch (this.topology)
		{
			case 3:
				for (int i = 0; i < this.deviceSwitchNum; i++)
				{
					this.deviceSwitch[i].addCurveForEvaluation(m, n, this.voltageSwitch, this.currentSwitch[i][m][n], this.timeVoltageRiseSwitch[i][m][n]);
				}
				this.deviceInductor[0].addCircuitParamForEvaluation(m, n, this.currentInductorResonanceRMS, this.currentInductorResonanceRipple);
				this.deviceInductor[1].addCircuitParamForEvaluation(m, n, this.currentInductorRMS, this.currentInductorRipple);
				//			this.deviceCapacitor[0].addCircuitParamForEvaluation(m, n, 0);
				this.deviceCapacitor[0].addCircuitParamForEvaluation(m, n, this.currentCapacitorClampingRMS);
				this.deviceCapacitor[1].addCircuitParamForEvaluation(m, n, this.currentCapacitorFilterRMS);
				break;
		}
	}

	/**
	 * @method setDesignCondition
	 * @description 计算并设定主电路元件设计条件
	 */
	private void setDesignCondition()
	{
		switch (this.topology)
		{
			case 0:
			case 1:
			case 2:
				//计算应力参数
				this.voltageInput = this.voltageInputMin;
				this.calcCircuitParam();
				this.deviceSwitch[0].setDesignCondition(this.power, this.frequency, this.voltageSwitch, this.currentInductorPeak);
				this.deviceCapacitor[0].setDesignCondition(this.power, this.capacitance, this.voltageCapacitorAverage, this.currentCapacitorRMS);
				double currentInductorPeakMax = this.currentInductorPeak; //电感电流峰值最大值

				//计算电感最大值
				double inductanceMax = 0;
				for (double v = voltageInputMin; v < voltageInputMax; v += 1.0)
				{
					this.voltageInput = v;
					this.calcCircuitParam();
					inductanceMax = Math.max(inductanceMax, this.inductance);
				}
				//		System.out.println("frequency="+this.frequency+", n="+this.number+", topology="+this.getTopologyName(this.topology)+": ");
				//		System.out.println("inductanceMax="+inductanceMax+", currentInductorPeakMax="+currentInductorPeakMax);
				this.deviceInductor[0].setDesignCondition(this.power, inductanceMax, this.frequency, currentInductorPeakMax);
				break;
			case 3:
				this.deviceSwitch[0].setDesignCondition(this.power, this.frequency, this.voltageSwitch, this.currentSwitchPeakMax, this.capacitanceResonance / (this.numberInput + 1));
				this.deviceSwitch[1].setDesignCondition(this.power, this.frequency, this.voltageSwitch, this.currentSwitchPeakMax2);
				this.deviceSwitch[2].setDesignCondition(this.power, this.frequency, this.voltageSwitch, this.currentSwitchPeakMax3, this.capacitanceResonance / (this.numberInput + 1));
				this.deviceInductor[0].setDesignCondition(this.power, this.inductanceResonance, this.frequency, this.currentInductorResonancePeakMax);
				this.deviceInductor[1].setDesignCondition(this.power, this.inductance, this.frequency, this.currentInductorPeakMax);
				//			this.deviceCapacitor[0].setDesignCondition(this.power, this.capacitanceResonance/(this.numberInput+1), this.voltageCapacitorResonanceMax, 0);
				this.deviceCapacitor[0].setDesignCondition(this.power, this.capacitanceClamping, this.voltageCapacitorClampingMax, this.currentCapacitorClampingRMSMax);
				this.deviceCapacitor[1].setDesignCondition(this.power, this.capacitanceFilter, this.voltageCapacitorFilterMax, this.currentCapacitorFilterRMSMax);
				break;
		}
	}
}
}
