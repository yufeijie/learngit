using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis
{
    internal static class Config
    {
		//常数
		public static int DEGREE = (int)1e3; //精度

		//设计结果限制
		public const double MIN_EFFICIENCY = 0; //最低效率
		public const double MAX_COST = 1e9; //最高成本(￥)
		public const double MAX_VOLUME = 1e9; //最大体积(dm^3)

		//温度(℃)
		public const double MAX_JUNCTION_TEMPERATURE = 125; //允许最高结温
		public const double MAX_HEATSINK_TEMPERATURE = 60; //散热器允许最高温度
		public const double AMBIENT_TEMPERATURE = 40; //环境温度

		//中国效率评估
		public static readonly double[] CGC_VOLTAGE_RATIO = { 0, 0.3, 0.5, 0.7, 1.0 }; //输入电压变化系数
		public static readonly double[] CGC_POWER_RATIO = { 0.05, 0.10, 0.20, 0.30, 0.50, 0.75, 1.00 }; //功率点
		public static readonly double[] CGC_POWER_WEIGHT = { 0.02, 0.03, 0.06, 0.12, 0.25, 0.37, 0.15 }; //权重
	}
}
