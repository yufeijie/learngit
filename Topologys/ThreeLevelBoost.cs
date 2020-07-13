using System;
using System.Collections.Generic;
using PV_analysis.Components;
using PV_analysis.Converters;

namespace PV_analysis.Topologys
{
    internal class ThreeLevelBoost : Topology
    {
		private readonly DCDCConverter converter; //所属变换器
		private readonly Component[][] allComponentGroups; //所有可行元件组
		private readonly Component[] allComponents; //可行元件组中出现的所有元件（待设计的元件）
		private Component[] componentGroup; //当前选用的元件组

		//基本电路参数
		private double m_P; //额定功率
		private double m_fs; //开关频率
		private double m_Vin; //输入电压
		private double m_D; //占空比
		private double m_Ts; //开关周期
		private double m_Iin; //输入电流
		private double m_Io; //输出电流
		
		//特殊时刻
		private double m_t1;
		private double m_t2;
		private double m_t3;
		private double m_t4;
		//主电路元件相关电路参数
		private double voltageSwitch; //开关器件电压
		private double[] currentWaveformIgbt; //Igbt电流波形（线性化处理的波形，数据存放格式为[x1, y1, x2, y2, ...]，仅记录器件开通到关断的波形）
		private double[] currentWaveformDiode; //二极管电流波形（线性化处理的波形）
		private double inductance; //感值
		private double currentInductorAverage; //电感电流平均值
		private double currentInductorRipple; //电感电流纹波
		private double currentInductorPeak; //电感电流峰值
		private double currentInductorTrough; //电感电流谷值
											  //private double[] currentInductor = new double[degree+1]; //电感电流
		private double capacitance; //容值
		private double voltageCapacitorAverage; //电容电压平均值
		private double voltageCapacitorRipple; //电容电压纹波
		private double currentCapacitorRMS; //电容电流有效值

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

			this.m_P = converter.
		}

		public void Design()
		{
			
		}
	}
}
