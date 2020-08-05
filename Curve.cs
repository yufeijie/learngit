using System;
using System.Collections.Generic;

namespace PV_analysis
{
	/// <summary>
	/// 曲线数据类，用于存储电压、电流波形
	/// </summary>
    internal class Curve
    {
		private readonly List<Point> data = new List<Point>(); //曲线数据

		public class Point
		{
			public double X { get; }
			public double Y { get; }
			public Point(double x, double y)
			{
				X = x;
				Y = y;
			}
		}

		/// <summary>
		/// 曲线名
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 曲线数据
		/// </summary>
		public IReadOnlyList<Point> Data { get { return data; } }

		/// <summary>
		/// 添加一个点
		/// </summary>
		/// <param name="x">点的x坐标</param>
		/// <param name="y">点的y坐标</param>
		public void Add(double x, double y)
		{
			data.Add(new Point(x, y));
		}
		
		/// <summary>
		/// 计算有效值
		/// </summary>
		/// <returns>有效值</returns>
		public double CalcRMS()
		{
			double result = 0;
			for (int i = 1; i < data.Count; i++)
			{
				result += (data[i].Y * data[i].Y + data[i-1].Y * data[i-1].Y) * (data[i].X - data[i-1].X) / 2;
			}
			result = Math.Sqrt(result / (data[data.Count - 1].X - data[0].X));
			return result;
		}

		/// <summary>
		/// 积分整条曲线（仅检查用）
		/// </summary>
		/// <returns>积分结果</returns>
		public double Integrate()
		{
			double result = 0;
			for (int i = 1; i < data.Count; i++)
			{
				result += (data[i].Y + data[i - 1].Y) * (data[i].X - data[i - 1].X) / 2;
			}
			return result;
		}

		//TODO 是否拆分该函数？拆分为平移、翻转等
		/// <summary>
		/// 复制一条曲线，可根据附加参数进行变换
		/// </summary>
		/// <param name="ratioY">y轴缩放比</param>
		/// <param name="offsetX">x轴平移量</param>
		/// <param name="offsetY">y轴平移量</param>
		/// <returns>复制的曲线</returns>
		public Curve Copy(double ratioY = 1, double offsetX = 0, double offsetY = 0)
		{
			Curve curve = new Curve();
			for (int i = 0; i < data.Count; i++)
			{
				curve.Add(data[i].X + offsetX, data[i].Y * ratioY + offsetY);
			}
			return curve;
		}

		//TODO 只能处理端点处
		/// <summary>
		/// 截断曲线的某一部分，在截断点添加从0开始的跳变
		/// </summary>
		/// <param name="start">左边界</param>
		/// <param name="end">右边界</param>
		/// <returns>截断后的曲线</returns>
		public Curve Cut(double start, double end)
		{
			Curve curve = new Curve();
			bool isFirst = true;
			for (int i = 0; i < data.Count; i++)
			{
				//double运算时会丢失精度，因此等号不一定能判断相等。此外还需考虑特殊情况，如10与9.999999999999以及10.000000000001可以认为相等
				if (Function.GE(data[i].X, start) && Function.LE(data[i].X, end))
				{
					if (isFirst)
					{
						isFirst = false;
						if (!Function.EQ(data[i].Y, 0))
						{
							curve.Add(data[i].X, 0);
						}
					}
					curve.Add(data[i].X, data[i].Y);
				}
			}
			if (!isFirst)
			{
				if (!Function.EQ(curve.data[curve.data.Count - 1].Y, 0))
				{
					curve.Add(curve.data[curve.data.Count - 1].X, 0);
				}
			}
			return curve;
		}

		//TODO 只能处理端点处
		/// <summary>
		/// 过滤曲线的某一部分，剩余部分变为0，在边界点添加从0开始的跳变
		/// </summary>
		/// <param name="start">左边界</param>
		/// <param name="end">右边界</param>
		/// <returns>过滤后的曲线</returns>
		public Curve Filter(double start, double end)
		{
			Curve curve = new Curve();
			bool isFirst = true;
			for (int i = 0; i < data.Count; i++)
			{
				//double运算时会丢失精度，因此等号不一定能判断相等。此外还需考虑特殊情况，如10与9.999999999999以及10.000000000001可以认为相等
				if (Function.GE(data[i].X, start) && Function.LE(data[i].X, end))
				{
					if (isFirst)
					{
						isFirst = false;
						if (!Function.EQ(data[i].X, 0))
						{
							curve.Add(0, 0);
						}
						if (!Function.EQ(data[i].Y, 0))
						{
							curve.Add(data[i].X, 0);
						}
					}
					curve.Add(data[i].X, data[i].Y);
				}
			}
			if (!isFirst)
			{
				if (!Function.EQ(curve.data[curve.data.Count - 1].Y, 0))
				{
					curve.Add(curve.data[curve.data.Count - 1].X, 0);
				}
				if (!Function.EQ(curve.data[curve.data.Count - 1].X, data[data.Count - 1].X))
				{
					curve.Add(data[data.Count - 1].X, 0);
				}
			}
			return curve;
		}
	}
}
