using PV_analysis.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Converters
{
    internal abstract class Converter
    {
		//优化与评估
		protected bool isEvaluatedAtDiffInputVoltage; //是否对不同输入电压进行评估
		protected static bool isRecordResult = true; //是否记录单级变换器评估结果

		//---基本参数---
		protected string name = null; //变换器名
		protected short stage = 0; //第几级变换器
		protected short phaseNum = 1; //相数(单相or三相)
		protected double math_Psys; //系统功率
		protected double math_fs; //开关频率
		protected int number; //模块数

		//---设计信息---
		//protected ConverterDesignInfo designInfo;

		//---评估结果---
		protected double efficiency; //效率
		protected double efficiencyCGC; //中国效率
		//损耗(W)
		protected double powerLossTotal; //总损耗
		//(单个模块)
		protected double powerLossModule; //模块总损耗
		protected double powerLossSwitch; //全体开关器件损耗
		protected double powerLossIgbtConduction; //全体开关器件IGBT导通损耗
		protected double powerLossIgbtTurnOn; //全体开关器件IGBT开通损耗
		protected double powerLossIgbtTurnOff; //全体开关器件IGBT关断损耗
		protected double powerLossDiodeConduction; //全体开关器件二极管导通损耗
		protected double powerLossDiodeReverseRecover; //全体开关器件二极管反向恢复损耗
		protected double powerLossMagnetics; //全体磁性元件损耗
		protected double powerLossMagneticsCu; //全体磁性元件铜损
		protected double powerLossMagneticsFe; //全体磁性元件铁损
		protected double powerLossCapacitor; //全体电容损耗
		protected double powerLossEvaluation; //总损耗评估值
		//成本(￥)
		protected double costTotal; //总成本
		//(单个模块)
		protected double costModule; //模块总成本
		protected double costSwitch; //开关器件成本
		protected double costMagnetics; //磁性元件成本
		protected double costMagneticsCore; //磁性元件磁芯成本
		protected double costMagneticsWire; //磁性元件绕线成本
		protected double costCapacitor; //电容成本
		protected double costCandD; //控制与驱动成本
		protected double costDriver; //驱动成本
		protected double costController; //控制成本
		protected double costPCB; //PCB成本
		protected double costHeatsink; //散热片成本
		//protected double costHeatsinkFin; //鳍片成本
		//protected double costHeatsinkFan; //风扇成本
		//体积(dm^3)
		protected double powerDensity; //功率密度(kW/dm^3)
		protected double volumeTotal; //总体积
		//(单个模块)
		protected double volumeModule; //总体积
		protected double volumeSwitch; //开关器件体积
		protected double volumeMagnetics; //磁性元件体积
		protected double volumeCapacitor; //电容体积
		protected double volumeHeatsink; //散热片体积
		//protected double volumeHeatsinkFin; //鳍片体积
		//protected double volumeHeatsinkFan; //风扇体积
		//温度(℃)
		protected double temperatureHeatsink; //散热器温度

		//---设计结果---
		//protected ParetoList design; //Pareto最优设计结果

		/// <summary>
		/// 系统功率
		/// </summary>
		public double Math_Psys
		{
			get { return math_Psys; }
			set { math_Psys = value; }
		}

		/// <summary>
		/// 开关频率
		/// </summary>
		public double Math_fs
		{
			get { return math_fs; }
			set { math_fs = value; }
		}

		/// <summary>
		/// 模块数
		/// </summary>
		public int Number
		{
			get { return number; }
			set { number = value; }
		}

	}
}
