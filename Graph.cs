﻿using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Serialization.Configuration;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace PV_analysis
{
    /// <summary>
    /// 绘图
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

        public void Add(Curve curve, string name)
        {
            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            for (int i = 0; i < curve.Data.Count; i++)
            {
                values.Add(new ObservablePoint(curve.Data[i].X, curve.Data[i].Y));
            }
            chart.Series.Add(new LineSeries {
                Title = name,
                Values = values,
                LineSmoothness = 0,
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
