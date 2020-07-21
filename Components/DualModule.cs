using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Components
{
	internal class DualModule : Semiconductor
	{
		//限制条件
		private static readonly bool isCheckTemperature = true; //在评估时是否进行温度检查
		private bool isSoftOff = false; //是否以软关断的方式进行计算(仅在符合条件时为true)
		private static readonly bool isCheckExcess = false; //是否检查过剩容量
		private static bool selectSiC = false; //SiC器件选用开关，true为可选用
		private static readonly double margin = 0.15; //裕量
		private static readonly double numberMax = 10; //最大器件数

		//基本参数
		private int number; //同类开关器件数量

		//器件参数
		private int device; //开关器件编号
		private int paralleledNum; //并联数量

		//设计条件
		private double math_Vmax; //电压应力
		private double math_Imax; //电流应力
		private double math_fs; //开关频率

		//电路参数
		private double math_Vsw; //开通/关断电压

		//波形模拟
		private Curve curve_iUp; //上管电流波形
		private Curve curve_iDown; //下管电流波形
		private Curve[,] curve_iUp_eval; //上管电流波形（用于评估）
		private Curve[,] curve_iDown_eval; //下管电流波形（用于评估）

		//损耗参数（同类中一个开关器件的损耗）
		private double powerLoss; //单个损耗
        private double[] math_PTcon; //主管导通损耗
		private double[] math_Pon; //主管开通损耗
		private double[] math_Poff; //主管关断损耗
		private double[] math_PDcon; //反并二极管导通损耗
		private double[] math_Prr; //反并二极管反向恢复损耗
		private double math_Peval; //损耗评估值

		//成本参数（同类中一个开关器件的成本）
		private double cost; //单个成本
		private double semiconductorCost; //开关器件成本
		private double driverCost; //驱动成本

		//体积参数（同类中一个开关器件的体积）
		private double volume; //单个体积(dm^3)

		//温度参数(℃)
		private double math_Th_max = 60; //散热器设计最高温度
		private double math_Tc; //壳温
		//	private double temperatureCaseIgbt; //IGBT壳温
		//	private double temperatureCaseDiode; //二极管壳温
		private double math_Tj_max = 110;//最高结温
		private double math_Tj_main; //主管结温
		private double math_Tj_diode; //反并二极管结温

		/// <summary>
		/// 总损耗
		/// </summary>
		public double PowerLoss { get { return number * powerLoss; } }

		/// <summary>
		/// 总成本
		/// </summary>
		public double Cost { get { return number * cost; } }

		/// <summary>
		/// 总体积
		/// </summary>
		public double Volume { get { return number * volume; } }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="number">同类开关器件数量</param>
        public DualModule(int number)
		{
			this.number = number;
		}

		/// <summary>
		/// 设置设计条件
		/// </summary>
		/// <param name="Vmax">电压应力</param>
		/// <param name="Imax">电流应力</param>
		/// <param name="fs">开关频率</param>
		public void SetDesignCondition(double Vmax, double Imax, double fs)
		{
			math_Vmax = Vmax;
			math_Imax = Imax;
			math_fs = fs;
		}

		/// <summary>
		/// 设置用于评估的波形
		/// </summary>
		/// <param name="i_eval">电流波形</param>
		public void SetEvalCurve(Curve[,] iUp_eval, Curve[,] iDown_eval)
		{
			curve_iUp_eval = iUp_eval;
			curve_iDown_eval = iDown_eval;
		}

		/// <summary>
		/// 选择当前评估的波形
		/// </summary>
		/// <param name="m">输入电压对应编号</param>
		/// <param name="n">负载点对应编号</param>
		private void SelectEvalCurve(int m, int n)
		{
			curve_iUp = curve_iUp_eval[m, n];
			curve_iDown = curve_iDown_eval[m, n];
		}

		/// <summary>
		/// 自动设计
		/// </summary>
		public void Design()
		{
			for (int i = 0; i < Data.SemiconductorList.Count; i++) //搜寻库中所有开关器件型号
			{
				this.device = i; //选用当前型号器件
				if (Validate()) //验证该器件是否可用
				{
					if (Evaluate()) //损耗评估，并进行温度检查
					{ 
						CalcVolume(); //计算体积
						CalcCost(); //计算成本
						double efficiency = 1 - this.getPowerLossEvaluation() / this.power;
						double volume = this.getVolume();
						double cost = this.getCost();
						String[] data = this.getDesignData();
						this.design.add(efficiency, volume, cost, data); //记录设计
					}
				}
			}
		}

		/// <summary>
		/// 损耗评估，并进行温度检查
		/// </summary>
		/// <param name="m">输入电压对应编号</param>
		/// <param name="n">负载点对应编号</param>
		/// <returns>评估结果，若温度检查不通过则返回false</returns>
		private bool Evaluate()
		{
			math_Peval = 0;
			for (int m = 0; m < Config.CGC_VOLTAGE_RATIO.Length; m++) //对不同输入电压进行计算
			{
				for (int n = 0; n < Config.CGC_POWER_RATIO.Length; n++) //对不同功率点进行计算
				{
					SelectEvalCurve(m, n); //设置对应条件下的电路参数
					CalcPowerLoss(); //计算对应条件下的损耗
					if (isCheckTemperature && this.calcTemperatureHeatsink() < this.temperatureHeatsinkMax) //验证散热器温度
					{
						return false;
					}
					math_Peval += powerLoss * Config.CGC_POWER_WEIGHT[n] / Config.CGC_POWER_RATIO[n]; //计算损耗评估值
				}
			}
			math_Peval /= Config.CGC_VOLTAGE_RATIO.Length;
			return true;
		}

	    /// <summary>
		/// 验证开关器件的型号、电压、电流等是否满足要求
		/// </summary>
		/// <returns>验证结果，true为满足</returns>
		private bool Validate()
		{
			//验证编号是否合法
			if (device < 0 || device >= Data.SemiconductorList.Count)
			{
				return false;
			}

			//验证器件类型是否符合
			if (!Data.SemiconductorList[device].Category.Equals("IGBT-Module") || !Data.SemiconductorList[device].Category.Equals("SiC-Module"))
			{
				return false;
			}

			//验证器件结构是否符合
			if (!Data.SemiconductorList[device].Configuration.Equals("Dual"))
			{
				return false;
			}

			//验证SiC器件的选用是否符合限制条件
			if ((Data.SemiconductorList[device].Category.Equals("SiC-Module")) && (!selectSiC || math_fs < 50e3))
			{
				return false;
			}

			//验证电压、电流应力是否满足
			if (Data.SemiconductorList[device].Math_Vmax * (1 - margin) < math_Vmax || Data.SemiconductorList[device].Math_Imax * (1 - margin) < math_Imax)
			{
				return false;
			}
			
			//容量过剩检查
			if (isCheckExcess && (Data.SemiconductorList[device].Math_Vmax * (1 - margin) > math_Vmax * 2 || Data.SemiconductorList[device].Math_Imax * (1 - margin) > math_Imax * 2))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// 计算成本
		/// </summary>
		private void CalcCost()
		{
			semiconductorCost = Data.SemiconductorList[device].Price;
			driverCost = 2 * 31.4253; //IX2120B IXYS MOQ100 Mouser TODO 驱动需要不同
			cost = semiconductorCost + driverCost;
		}

		/// <summary>
		/// 计算体积
		/// </summary>
		public void CalcVolume()
		{
			volume = Data.SemiconductorList[device].Volume;
		}

		/// <summary>
		/// 计算损耗 TODO 未考虑同步整流
		/// </summary>
		public void CalcPowerLoss()
		{
			math_PTcon = new double[] { 0, 0 };
            math_Pon = new double[] { 0, 0 };
			math_Poff = new double[] { 0, 0 };
			math_PDcon = new double[] { 0, 0 };
			math_Prr = new double[] { 0, 0 };
			
			double t1, i1, t2, i2;
			for (int i = 1; i < curve_iDown.Data.Count; i++)
			{
				t1 = curve_iDown.Data[i - 1].X;
				i1 = curve_iDown.Data[i - 1].Y;
				t2 = curve_iDown.Data[i].X;
				i2 = curve_iDown.Data[i].Y;
				if (Function.EQ(i1, 0) && Function.EQ(i2, 0)) //两点电流都为0时无损耗
				{
					continue;
				}
				else if (Function.EQ(t1, t2)) //t1=t2时，可能有开关损耗，没有导通损耗
				{
					if (Function.LE(i1, 0) && Function.GT(i2, 0)) //i1<=0、i2>0时，计算主管开通损耗
					{
						calc_Pon(i2);
					}
					if (Function.GT(i1, 0) && Function.LE(i2, 0)) //i1>0、i2<=0时，计算主管关断损耗
					{
						calc_Poff(i1);
					}
					if (Function.LT(i1, 0) && Function.GE(i2, 0)) //i1<0、i2>=0时，计算反并二极管反向恢复损耗
					{
						calc_Prr(i1);
					}
				}
				else //t1≠t2时，只有导通损耗
				{
					if (Function.GE(i1, 0) && Function.GE(i2, 0)) //电流都不为负时，为主管导通损耗
					{
						calc_PTcon(t1, i1, t2, i2); //计算主管导通损耗
					}
					else if (Function.LE(i1, 0) && Function.LE(i2, 0)) //电流都不为正时，为反并二极管导通损耗
					{
						calc_PDcon(t1, i1, t2, i2); //计算反并二极管导通损耗
					}
					else //电流一正一负时，既包含主管导通损耗，又包含反并二极管导通损耗
					{
						double z = (i1 * t2 - i2 * t1) / (i1 - i2); //计算过零点
						if (i1 > 0) //i1>0时，主管先为导通状态
						{
							calc_PTcon(t1, i1, z, 0);
							calc_PDcon(z, 0, t2, i2);
						}
						else //否则i2>0，反并二极管先为导通状态
						{
							calc_PDcon(t1, i1, z, 0);
							calc_PTcon(z, 0, t2, i2);
						}
					}
				}
			}

			for (int i = 0; i < 2; i++)
			{
				powerLoss = math_PTcon[i] + math_Pon[i] + math_Poff[i] + math_PDcon[i] + math_Prr[i];
			}
		}

		/**
		 * @method calcPowerLossIgbtConductionCurve
		 * @description 计算单个开关器件IGBT导通损耗（波形模拟）
		 */
		private void calcPowerLossIgbtConductionCurve(double t1, double i1, double t2, double i2)
		{
			double energy = 0;
			if (Data.switche[this.device][2].contentEquals("SiC-MOSFET"))
			{
				energy = myIntegral(t1, i1, i1, t2, i2, i2) * Double.parseDouble(Data.switche[this.device][6]) * 1e-3 / this.numberParallelConnected; //计算导通能耗
			}
			else
			{
				//采用牛顿-莱布尼茨公式进行电压电流积分的优化计算
				double u1 = getCurveValue(6, this.device, i1); //获取左边界电流对应的导通压降
				double u2 = getCurveValue(6, this.device, i2); //获取右边界电流对应的导通压降
				energy = myIntegral(t1, i1, u1, t2, i2, u2); //计算导通能耗
			}
			this.powerLossIgbtConduction += energy * this.frequency;
		}

		/**
		 * @method calcPowerLossDiodeConductionCurve
		 * @description 计算单个开关器件二极管导通损耗（波形模拟）
		 * FIXME 导通损耗计算方法的统一
		 */
		private void calcPowerLossDiodeConductionCurve(double t1, double i1, double t2, double i2)
		{
			//采用牛顿-莱布尼茨公式进行电压电流积分的优化计算
			i1 = (i1 >= 0 ? i1 : -i1) / this.numberParallelConnected;
			i2 = (i2 >= 0 ? i2 : -i2) / this.numberParallelConnected;
			double u1 = getCurveValue(7, this.device, i1); //获取左边界电流对应的导通压降
			double u2 = getCurveValue(7, this.device, i2); //获取右边界电流对应的导通压降
			double energy = myIntegral(t1, i1, u1, t2, i2, u2) * this.numberParallelConnected; //计算导通能耗
			this.powerLossDiodeConduction += energy * this.frequency;
		}

		/**
		 * @method calcPowerLossTurnOnCurve
		 * @description 计算单个开关器件IGBT开通损耗（波形模拟）
		 */
		private void calcPowerLossTurnOnCurve(double current)
		{
			//根据开通电流查表得到对应损耗
			if (current == 0)
			{
				return;
			}
			if (Data.switche[this.device][2].contentEquals("SiC-MOSFET"))
			{
				System.out.println("SiC-MOSFET Pon error!");
				System.exit(-1);
			}
			int n = (int)Double.parseDouble(Data.switche[this.device][8]);
			this.powerLossTurnOn += this.frequency * this.voltage / Double.parseDouble(Data.curve[n][3]) * getCurveValue(8, this.device, current) * 1e-3;
		}

		/**
		 * @method calcPowerLossTurnOffCurve
		 * @description 计算单个开关器件IGBT关断损耗（波形模拟）
		 */
		private void calcPowerLossTurnOffCurve(double current)
		{
			//根据关断电流查表得到对应损耗
			if (current == 0)
			{
				return;
			}
			current /= this.numberParallelConnected; //FIXME 注意这里，多管并联时，电流和寄生电容的计算
			int n = (int)Double.parseDouble(Data.switche[this.device][9]);
			if (Data.switche[this.device][2].contentEquals("SiC-MOSFET"))
			{
				if (this.isSoftOff)
				{
					double tr = this.timeVoltageRise;
					double Vth = Double.parseDouble(Data.characteristics[n][0]);
					double gm = Double.parseDouble(Data.characteristics[n][1]);
					double Ciss = Double.parseDouble(Data.characteristics[n][2]) * 1e-12;
					double Coss = Double.parseDouble(Data.characteristics[n][3]) * 1e-12;
					double Crss = Double.parseDouble(Data.characteristics[n][4]) * 1e-12;
					double Rg = Double.parseDouble(Data.characteristics[n][5]);
					double Cgd = Crss;
					double Cds = (Coss - Crss) * this.numberParallelConnected;
					double tm = ((1 + gm * Rg) * Cgd + Cds) / (gm * Vth + current) * this.voltage; //FIXME 实际上还有一段，关断后到进入米勒平台前的时间t1
					double tf = Rg * Ciss * Math.log((current / gm + Vth) / Vth);
					double e1 = 0;
					double e2 = 0;
					if (tr <= tm)
					{
						e1 = tr * current * 0.5 * this.voltage + (tm - tr) * current * this.voltage;
						e2 = tf * 0.5 * current * this.voltage;
					}
					else
					{
						if (tr <= tm + tf)
						{
							double v1 = this.voltage / tr * tm;
							double i1 = current;
							double v2 = this.voltage;
							double i2 = this.currentOffMOSFET(current, tr, tm, tf);
							e1 = tm * current * 0.5 * v1;
							e2 = myIntegral(tm, i1, v1, tr, i2, v2)
								 + (tm + tf - tr) * 0.5 * i2 * this.voltage;
						}
						else
						{
							double v1 = this.voltage / tr * tm;
							double i1 = current;
							double v2 = this.voltage / tr * (tm + tf);
							double i2 = 0;
							e1 = tm * current * 0.5 * v1;
							e2 = myIntegral(tm, i1, v1, tm + tf, i2, v2);
						}
					}
					this.powerLossTurnOff += this.numberParallelConnected * this.frequency * (e1 + e2);
				}
				else
				{
					double Vth = Double.parseDouble(Data.characteristics[n][0]);
					double gm = Double.parseDouble(Data.characteristics[n][1]);
					double Ciss = Double.parseDouble(Data.characteristics[n][2]) * 1e-12;
					double Coss = Double.parseDouble(Data.characteristics[n][3]) * 1e-12;
					double Crss = Double.parseDouble(Data.characteristics[n][4]) * 1e-12;
					double Rg = Double.parseDouble(Data.characteristics[n][5]);
					double Cgd = Crss;
					double Cds = (Coss - Crss) * this.numberParallelConnected;
					double tf = Rg * Ciss * Math.log((current / gm + Vth) / Vth);
					double t2 = ((1 + gm * Rg) * Cgd + Cds) / (gm * Vth + current) * this.voltage;
					double e1 = tf * this.voltage * 0.5 * current;
					double e2 = t2 * this.voltage * 0.5 * current;
					this.powerLossTurnOff += this.numberParallelConnected * this.frequency * (e1 + e2);
				}
			}
			else
			{
				this.powerLossTurnOff += this.numberParallelConnected * this.frequency * this.voltage / Double.parseDouble(Data.curve[n][3]) * getCurveValue(9, this.device, current) * 1e-3;
			}
		}

		/**
		 * @method calcPowerLossReverseRecoverCurve
		 * @description 计算单个开关器件二极管反向恢复导通损耗（波形模拟）
		 */
		private void calcPowerLossReverseRecoverCurve(double current)
		{
			//根据关断电流查表得到对应损耗
			if (current == 0)
			{
				return;
			}
			if (Data.switche[this.device][2].contentEquals("SiC-MOSFET"))
			{
				System.out.println("SiC-MOSFET Prr error!");
				System.exit(-1);
			}
			current = current >= 0 ? current : -current;
			int n = (int)Double.parseDouble(Data.switche[this.device][10]);
			this.powerLossReverseRecover += this.frequency * this.voltage / Double.parseDouble(Data.curve[n][3]) * getCurveValue(10, this.device, current) * 1e-3;
		}

		/**
		 * @method calcTemperatureHeatsink
		 * @description 计算所需散热器温度
		 */
		public double calcTemperatureHeatsink()
		{
			//		this.temperatureCaseIgbt = this.temperatureJunctionMax-this.powerLossIgbt*Double.parseDouble(Data.switche[this.device][11]);
			//		this.temperatureCaseDiode = this.temperatureJunctionMax-this.powerLossDiode*Double.parseDouble(Data.switche[this.device][13]);
			//		double temperatureHeatsink = Math.min(this.temperatureCaseIgbt-this.powerLossIgbt*Double.parseDouble(Data.switche[this.device][12]),
			//											  this.temperatureCaseDiode-this.powerLossDiode*Double.parseDouble(Data.switche[this.device][14]));
			if (Data.switche[this.device][3].contentEquals("Single"))
			{
				this.temperatureCase = this.temperatureJunctionMax - (this.powerLoss / this.numberParallelConnected) * Double.parseDouble(Data.switche[this.device][11]);
			}
			else
			{
				this.temperatureCase = Math.min(this.temperatureJunctionMax - (this.powerLossIgbt / this.numberParallelConnected) * Double.parseDouble(Data.switche[this.device][11]),
												this.temperatureJunctionMax - (this.powerLossDiode / this.numberParallelConnected) * Double.parseDouble(Data.switche[this.device][13]));
			}
			double temperatureHeatsink = this.temperatureCase - (this.powerLoss / this.numberParallelConnected) * Double.parseDouble(Data.switche[this.device][15]);
			return temperatureHeatsink;
		}

		/**
		 * @method calcTemperature
		 * @description 计算开关器件温度
		 */
		public void calcTemperature(double temperatureHeatsink)
		{
			//		this.temperatureCaseIgbt = temperatureHeatsink+this.powerLossIgbt*Double.parseDouble(Data.switche[this.device][12]);
			//		this.temperatureCaseDiode = temperatureHeatsink+this.powerLossDiode*Double.parseDouble(Data.switche[this.device][14]);
			//		this.temperatureJunctionIgbt = this.temperatureCaseIgbt+this.powerLossIgbt*Double.parseDouble(Data.switche[this.device][11]);
			//		this.temperatureJunctionDiode = this.temperatureCaseDiode+this.powerLossDiode*Double.parseDouble(Data.switche[this.device][13]);
			this.temperatureCase = temperatureHeatsink + (this.powerLoss / this.numberParallelConnected) * Double.parseDouble(Data.switche[this.device][15]);
			this.temperatureJunctionIgbt = this.temperatureCase + (this.powerLossIgbt / this.numberParallelConnected) * Double.parseDouble(Data.switche[this.device][11]);
			if (Data.switche[this.device][3].contentEquals("Single"))
			{
				this.temperatureJunctionDiode = 0;
			}
			else
			{
				this.temperatureJunctionDiode = this.temperatureCase + (this.powerLossDiode / this.numberParallelConnected) * Double.parseDouble(Data.switche[this.device][13]);
			}
		}
	}
}
