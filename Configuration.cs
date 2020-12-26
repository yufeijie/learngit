using System;

namespace PV_analysis
{
    internal static class Configuration
    {
		//光伏并网系统设计规格
		public const double GRID_FREQUENCY = 50; //并网频率(Hz)
		public const double POWER_FACTOR_ANGLE = 0; //并网功率因数角(rad)

		//评估过程
		//中国效率
		public static readonly double[] CGC_VOLTAGE_RATIO = { 0, 0.3, 0.5, 0.7, 1.0 }; //输入电压变化系数
		public static readonly double[] CGC_POWER_RATIO = { 0.05, 0.10, 0.20, 0.30, 0.50, 0.75, 1.00 }; //功率点
		public static readonly double[] CGC_POWER_WEIGHT = { 0.02, 0.03, 0.06, 0.12, 0.25, 0.37, 0.15 }; //权重
		//计算
		public const int DEGREE = (int)1e3; //精度
		public const double IGNORE = 1e-3; //小于该值则不计算开关损耗
		public const double ERROR = 1e-12; //最小计算误差，小于该值则认为为0
		public const double ERROR_BIG = 1e-8; //最小计算误差（数字较大），小于该值则认为为0
		//LLC谐振电感设计
		//可选参数
        public static readonly bool IS_RESONANT_INDUCTANCE_INTEGRATED = false; //是否认为谐振电感集成在变压器中
		//开关器件设计
		public static readonly bool IS_SELECT_SIC = true; //是否选用SiC器件
		public const int MAX_SEMICONDUCTOR_NUM = 10; //开关器件并联数上限
		public const double IGBT_DEAD_TIME = 1e-6; //IGBT死区时间1us
		public const double MOSFET_DEAD_TIME = 200e-9; //MOS死区时间200ns
		public const double SIC_SELECTION_FREQUENCY = 50e3; //选用SiC器件的最低频率
		//磁性元件设计
		public const double ABSOLUTE_PERMEABILITY = 4 * Math.PI * 1e-7; //绝对磁导率(H/m) [MKS] or 1(Gs/Oe) [CGS]
		public const double COPPER_RESISTIVITY = 1.724 * 1e-8; //铜电阻率
		public const double COPPER_RELATIVE_PERMEABILITY = 1; //铜相对磁导率 0.9999912
		public const int MAX_CORE_NUM = 10; //磁芯数量最大值
		//电感设计
		public const double AIR_GAP_LENGTH_DELTA = 1e-4; //气隙长度精度(cm)
		//电容设计
		public const int MAX_CAPACITOR_NUM = 20; //电容器件个数上限
		//温度(℃)
		public const double MAX_JUNCTION_TEMPERATURE = 110; //允许最高结温
		public const double MAX_HEATSINK_TEMPERATURE = 60; //散热器允许最高温度
		public const double AMBIENT_TEMPERATURE = 40; //环境温度
		//散热器设计
		public const double CSPI = 15; //cooling system performance index 冷却系统性能指标(W/K*dm^3)
		public const double HEATSINK_UNIT_PRICE = 35 + 15; //单位体积散热器成本(RMB/dm^3)
		//DSP成本
		public const double DSP_PRICE = 157.296; //型号：TMS320F28335PGFA TI 100 Mouser FIXM
		//限制条件
		public static readonly bool IS_CHECK_SEMICONDUCTOR_EXCESS = false; //是否检查开关器件过剩容量
		public static readonly bool IS_CHECK_CAPACITOR_EXCESS = false; //是否检查电容过剩容量
		public const double EXCESS_RATIO = 1; //允许过剩比例

		//评估结果限制
		public const double MIN_EFFICIENCY = 0; //最低效率
		public const double MAX_COST = 1e9; //最高成本(￥)
		public const double MAX_VOLUME = 1e9; //最大体积(dm^3)

		//界面显示
		public static readonly bool IS_PRINT_DEBUG = true; //是否打印Debug信息
		public const short PRINT_LEVEL = 4; //允许打印的详细信息等级
		public static readonly System.Drawing.Color COLOR_ACTIVE = System.Drawing.SystemColors.ControlDarkDark; //左侧边栏按钮，当前选中颜色
		public static readonly System.Drawing.Color COLOR_INACTIVE = System.Drawing.SystemColors.ButtonFace; //左侧边栏按钮，未选中颜色
	}
}
