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
		/// 复制一条曲线，可根据附加参数进行变换
		/// </summary>
		/// <param name="ratio">缩放比</param>
		/// <param name="offsetX">x轴平移量</param>
		/// <param name="offsetY">y轴平移量</param>
		/// <returns>复制的曲线</returns>
		public Curve Copy(double ratio = 1, double offsetX = 0, double offsetY = 0)
		{
			Curve curve = new Curve();
			for (int i = 0; i < data.Count; i++)
			{
				curve.Add(data[i].X * ratio + offsetX, data[i].Y * ratio + offsetY);
			}
			return curve;
		}

		/// <summary>
		/// 截取曲线的某一部分
		/// </summary>
		/// <param name="start">左边界</param>
		/// <param name="end">右边界</param>
		/// <returns>截取的曲线</returns>
		public Curve SubCurve(double start, double end)
		{
			Curve curve = new Curve();
			for (int i = 0; i < data.Count; i++)
			{
				//double运算时会丢失精度，因此等号不一定能判断相等。此外还需考虑特殊情况，如10与9.999999999999以及10.000000000001可以认为相等
				if (Function.GE(data[i].X, start) && Function.LE(data[i].X, end))
				{
					curve.Add(data[i].X, data[i].Y);
				}
			}
			return curve;
		}
	}
}
