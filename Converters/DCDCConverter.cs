using System;
using PV_analysis.Components;
using PV_analysis.Topologys;

namespace PV_analysis.Converters
{
	/// <summary>
	/// DC/DC变换器类
	/// </summary>
	internal class DCDCConverter : Converter
	{
		//---可选参数---
		private readonly double diodeCurrentDropRate = 125e6; //二极管电流下降速率(A/s)

		//---给定参数---
		private double math_Vin_min; //输入电压最小值
		private double math_Vin_max; //输入电压最大值
		private double math_Vo; //输出电压

		/// <summary>
		/// 输入电压最小值
		/// </summary>
		public double Math_Vin_min { get { return math_Vin_min; } }

		/// <summary>
		/// 输入电压最大值
		/// </summary>
		public double Math_Vin_max { get { return math_Vin_max; } }

		/// <summary>
		/// 输出电压
		/// </summary>
		public double Math_Vo { get { return math_Vo; } }

		/// <summary>
		/// 初始化
		/// </summary>
		/// <param name="math_Psys">系统功率</param>
		/// <param name="math_Vin_min">最小输入电压</param>
		/// <param name="math_Vin_max">最大输入电压</param>
		/// <param name="math_Vo">输出电压</param>
		/// <param name="numberRange">可用模块数</param>
		/// <param name="topologyRange">可用拓扑</param>
		/// <param name="frequencyRange">可用开关频率</param>
		public DCDCConverter(double Psys, double Vin_min, double Vin_max, double Vo)
		{
			math_Psys = Psys;
			math_Vin_min = Vin_min;
			math_Vin_max = Vin_max;
			math_Vo = Vo;
		}
	}
}
