using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis
{
	/// <summary>
	/// 曲线数据类，用于存储电压、电流波形
	/// </summary>
    internal class Curve
    {
		private const double ERROR = 1e-12; //最小计算误差，用于equal方法判断double类型是否相等，小于该值则相等

		private readonly List<double> dataX = new List<double>(); //曲线x坐标
		private readonly List<double> dataY = new List<double>(); //曲线y坐标

		/// <summary>
		/// 曲线x坐标
		/// </summary>
		public IReadOnlyList<double> X { get { return dataX; } }

		/// <summary>
		/// 曲线y坐标
		/// </summary>
		public IReadOnlyList<double> Y { get { return dataY; } }

		/// <summary>
		/// 添加一个点
		/// </summary>
		/// <param name="x">点的x坐标</param>
		/// <param name="y">点的y坐标</param>
		public void Add(double x, double y)
		{
			dataX.Add(x);
			dataY.Add(y);
		}
		
		/// <summary>
		/// 计算有效值
		/// </summary>
		/// <returns>有效值</returns>
		public double CalcRMS()
		{
			double result = 0;
			for (int i = 1; i < dataX.Count; i++)
			{
				result += (dataY[i] * dataY[i] + dataY[i-1] * dataY[i-1]) * (dataX[i] - dataX[i-1]) / 2;
			}
			result = Math.Sqrt(result / (dataX[dataX.Count - 1] - dataX[0]));
			return result;
		}
		
		/// <summary>
		/// 复制一条曲线并平移
		/// </summary>
		/// <param name="offsetX">x轴平移量</param>
		/// <param name="offsetY">y轴平移量</param>
		/// <returns>复制的曲线</returns>
		public Curve Copy(double offsetX = 0, double offsetY = 0)
		{
			Curve curve = new Curve();
			for (int i = 0; i < dataX.Count; i++)
			{
				curve.Add(dataX[i] + offsetX, dataY[i] + offsetY);
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
			for (int i = 0; i < dataX.Count; i++)
			{
				//double运算时会丢失精度，因此等号不一定能判断相等。此外还需考虑特殊情况，如10与9.999999999999以及10.000000000001可以认为相等
				if ((dataX[i] >= start || Equal(dataX[i], start)) && (dataX[i] <= end || Equal(dataX[i], end)))
				{
					curve.Add(dataX[i], dataY[i]);
				}
			}
			return curve;
		}

		/// <summary>
		/// 判断的两个double类型变量是否相等
		/// </summary>
		/// <param name="a">变量a</param>
		/// <param name="b">变量b</param>
		/// <returns>是否相等</returns>
		private bool Equal(double a, double b)
		{
			double e = Math.Abs(a - b);
			if (e < ERROR)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

	}
}
