﻿using System;
using PV_analysis.Components;
using PV_analysis.Converters;

namespace PV_analysis.Topologys
{
	/// <summary>
	/// 三电平Boost类
	/// </summary>
    internal class ThreeLevelBoost : Topology
    {
		private readonly double math_kIrip = 0.2; //电流纹波系数
		private readonly double math_kVrip = 0.1; //电压纹波系数

		private readonly DCDCConverter converter; //所属变换器
		private readonly Component[][] allComponentGroups; //所有可行元器件组
		private readonly Component[] allComponents; //可行元件组中出现的所有元器件（待设计的元器件）
		private Component[] componentGroup; //当前选用的元器件组

		//基本电路参数
		private double math_P; //额定功率
		private double math_fs; //开关频率
		private double math_Vin_min; //输入电压最小值
		private double math_Vin_max; //输入电压最大值
		private double math_Vo; //输出电压

		//电压、电流波形
		private Curve math_iS = new Curve();
		private Curve math_iD = new Curve();
		private Curve math_iL = new Curve();
		private Curve math_iC = new Curve();

		//临时参数 FIXME
		private double currentInductorRMS; //电感电流有效值
		private double currentInductorResonanceRMS; //谐振电感电流有效值
		private double currentInductorResonanceRipple; //谐振电感电流纹波
		private double currentInductorResonancePeak; //谐振电感电流峰值
		private double currentSwitchPeak; //开关器件电流峰值 FIXME 包含在波形里
		private double currentSwitchPeak2; //开关器件电流峰值
		private double currentSwitchPeak3; //开关器件电流峰值
		private double currentSwitchPeakMax; //开关器件电流峰值最大值
		private double currentSwitchPeakMax2; //开关器件电流峰值最大值
		private double currentSwitchPeakMax3; //开关器件电流峰值最大值
		private double currentInductorPeakMax; //电感电流峰值最大值
		private double currentInductorResonancePeakMax; //谐振电感电流峰值最大值
		private double voltageCapacitorResonanceMax; //谐振电容电压最大值
		private double voltageCapacitorClampingMax; //箝位电容电压最大值
		private double voltageCapacitorFilterMax; //滤波电容电压最大值
		private double currentCapacitorClampingRMS; //滤波电容电流有效值
		private double currentCapacitorClampingRMSMax; //滤波电容电流有效值最大值
		private double currentCapacitorFilterRMS; //滤波电容电流有效值
		private double currentCapacitorFilterRMSMax; //滤波电容电流有效值最大值

		public ThreeLevelBoost(DCDCConverter converter)
		{
			this.converter = converter;
			allComponentGroups = new Component[1][];
			Component main = new Semiconductor();
			Component diode = new Semiconductor();
			Component inductor = new Inductor();
			Component capacitor = new Capacitor();
			allComponentGroups[0] = new Component[] { main, diode, inductor, capacitor };
			allComponents = new Component[] { main, diode, inductor, capacitor };

			math_P = converter.Math_Psys / converter.Number;
			math_fs = converter.Math_fs;
			math_Vin_min = converter.Math_Vin_min;
			math_Vin_max = converter.Math_Vin_max;
			math_Vo = converter.Math_Vo;
		}

		/// <summary>
		/// 计算电路参数，并模拟电压、电流波形
		/// </summary>
		public void Simulate()
		{
			double P = math_P;
			double Vin = math_Vin_min;
			double Vo = math_Vo;
			double fs = math_fs;
			double kIrip = math_kIrip;
			double kVrip = math_kVrip;

			double D = 1 - Vin / Vo; //占空比
			double Ts = 1 / fs; //开关周期
			double t1 = (D - 0.5) * Ts; //特殊时刻
			double t2 = 0.5 * Ts;
			double t3 = D * Ts;
			double t4 = (D + 0.5) * Ts;
			double Iin = P / Vin; //输入电流平均值
			double Io = P / Vo; //输出电流平均值
			double IL = Iin; //电感电流平均值
			double ILrip; //电感电流纹波
			if (D == 0)
			{
				ILrip = 0;
			}
			else
			{
				ILrip = Iin * kIrip;
			}
			double ILmax = IL + kIrip * 0.5; //电感电流峰值
			double ILmin = IL - kIrip * 0.5; //电感电流谷值
			double L; //感值
			if (D == 0)
			{
				L = 0;
			}
			else if (D > 0.5)
			{
				L = (D - 0.5) * Vin / (fs * ILrip);
			}
			else
			{
				L = D * (Vin - 0.5 * Vo) / (fs * ILrip);
			}
			double VS = Vo * 0.5; //开关器件电压应力
			//主开关管电流波形
			if (D > 0.5)
			{
				math_iS.Add(0, 0);
				math_iS.Add(0, ILmin);
				math_iS.Add(t1, ILmax);
				math_iS.Add(t2, ILmin);
				math_iS.Add(t3, ILmax);
				math_iS.Add(t3, 0);
				math_iS.Add(Ts, 0);
			}
			else
			{
				math_iS.Add(0, 0);
				math_iS.Add(0, ILmin);
				math_iS.Add(t3, ILmax);
				math_iS.Add(t3, 0);
				math_iS.Add(Ts, 0);
			}
			//升压二极管电流波形
			if (D > 0.5)
			{
				math_iD.Add(0, 0);
				math_iD.Add(t3, 0);
				math_iD.Add(t3, ILmax);
				math_iD.Add(Ts, ILmin);
				math_iD.Add(Ts, 0);
			}
			else
			{
				math_iD.Add(0, 0);
				math_iD.Add(t3, 0);
				math_iD.Add(t3, ILmax);
				math_iD.Add(t2, ILmin);
				math_iD.Add(t4, ILmax);
				math_iD.Add(Ts, ILmin);
				math_iD.Add(Ts, 0);
			}
			double VC = Vo * 0.5; //电容电压平均值
			double VCrip = VC * kVrip; //电压纹波
			double C = D * Io / (fs * VCrip); //容值
			//电容电流波形
			if (D > 0.5)
			{
				math_iC = math.iD - Io;
			}
			double f1 = ILmin - Io;
			double f2 = ILmax - Io;
			double c;
			if (D > 0.5)
			{
				c = Math.Pow(Io, 2) * t3 + myIntegral(t3, f2, f2, Ts, f1, f1);
			}
			else if (D < 0.5 && D > 0)
			{
				c = Math.Pow(Io, 2) * t3 +
					 myIntegral(t3, f2, f2, t2, f1, f1) +
					 myIntegral(t2, f1, f1, t4, f2, f2) +
					 myIntegral(t4, f2, f2, Ts, f1, f1);
			}
			else
			{
				c = 0;
			}
			double ICrms = Math.Sqrt(fs * c); //电容电流有效值
		}

		public void Design()
		{
			Simulate();
		}
	}
}
