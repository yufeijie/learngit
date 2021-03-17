using System.Windows.Forms;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using static PV_analysis.Curve;

namespace PV_analysis
{
    /// <summary>
    /// 绘图（仅用于测试计算结果波形）
    /// </summary>
    internal class Graph
    {
        private readonly Form form = new Form //窗体
        {
            Size = new System.Drawing.Size(1280, 720)
        };

        private readonly LiveCharts.WinForms.CartesianChart chart = new LiveCharts.WinForms.CartesianChart //图表控件
        {
            Location = new System.Drawing.Point(40, 20),
            Size = new System.Drawing.Size(1200, 640),
            Anchor = AnchorStyles.None
        };

        /// <summary>
        /// 添加曲线（已定义曲线名）
        /// </summary>
        /// <param name="curve">曲线</param>
        public void Add(Curve curve)
        {
            Add(curve, curve.Name);
        }

        /// <summary>
        /// 添加曲线
        /// </summary>
        /// <param name="curve">曲线</param>
        /// <param name="name">曲线名</param>
        public void Add(Curve curve, string name)
        {
            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            Point[] data = curve.GetData();
            for (int i = 0; i < data.Length; i++)
            {
                values.Add(new ObservablePoint(data[i].X, data[i].Y));
            }
            chart.Series.Add(new GLineSeries
            {
                Title = name,
                Values = values.AsGearedValues().WithQuality(Quality.Low),
                Fill = Brushes.Transparent,
                LineSmoothness = 0, //0对应纯直线，若为1则是插件自动生成的顺滑曲线
                StrokeThickness = .5,
                PointGeometry = null
            });
        }

        /// <summary>
        /// 绘制曲线图像
        /// </summary>
        /// <param name="curve">要绘制的曲线</param>
        public void Draw()
        {
            chart.AxisX.Add(new Axis
            {
                Title = "Time"
            });

            chart.AxisY.Add(new Axis
            {
                Title = "Value"
            });
            chart.LegendLocation = LegendLocation.Right;
            form.Controls.Add(chart);
            form.ShowDialog();
        }
    }
}
