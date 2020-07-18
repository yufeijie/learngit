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
		/// 复制一条曲线并平移
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public Curve Copy(int offsetX, int offsetY)
		{
			Curve curve = new Curve();
			for (int i = 0; i < dataX.Count; i++)
			{
				curve.Add(dataX[i], dataY[i]);
			}
			return curve;
		}
	}
}
