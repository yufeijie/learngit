using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using PV_analysis.Converters;
using PV_analysis.Structures;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;

namespace PV_analysis
{
    /// <summary>
    /// 左右布局
    /// 左侧边栏界面实现各类页面切换（主页、评估、展示等），右侧主界面显示各类页面
    /// 在右侧主界面，通过按钮切换下级子页面
    /// </summary>
    internal partial class MainForm : Form
    {
        private Thread evaluationThread; //评估线程
        private bool isPrintDetails = false;

        private readonly Panel[] panelNow = new Panel[5]; //下标0——当前显示页面，下标1-4——各类页面的当前子页面
        private System.Drawing.Color activeColor; //左侧边栏按钮，当前选中颜色
        private System.Drawing.Color inactiveColor; //左侧边栏按钮，未选中颜色
        private List<Label> labelList = new List<Label>();

        private Structure structure; //架构
        private string selectedStructure; //所要评估的架构，三级架构或两级架构

        //评估参数（系统）
        private double Psys; //架构总功率
        private double Vpv_min; //光伏MPPT电压最小值
        private double Vpv_max; //光伏MPPT电压最大值
        private double Vpv_peak; //光伏输出电压最大值
        private double Vg; //并网电压（线电压）
        private double Vo; //输出电压（并网相电压）
        private double fg = 50; //并网频率
        private double[] VbusRange; //母线电压范围

        //评估参数（前级DC/DC）
        private int[] DCDC_numberRange; //可用模块数序列
        private string[] DCDC_topologyRange; //可用拓扑序列
        private double[] DCDC_frequencyRange; //可用开关频率序列

        //评估参数（隔离DC/DC）
        private double isolatedDCDC_Q; //品质因数
        private int[] isolatedDCDC_secondaryRange; //可用副边个数序列
        private int[] isolatedDCDC_numberRange; //可用模块数序列
        private string[] isolatedDCDC_topologyRange; //可用拓扑序列
        private double[] isolatedDCDC_resonanceFrequencyRange; //可用谐振频率序列

        //评估参数（DC/AC）
        private double DCAC_Ma; //电压调制比
        private double DCAC_phi = 0; //功率因数角(rad)
        private double DCAC_Vin_def = 1300; //逆变器直流侧电压（两级架构）
        private string[] DCAC_topologyRange; //可用拓扑序列
        private string[] DCAC_modulationRange = { "PSPWM", "LSPWM" }; //可用调制方式序列
        private double[] DCAC_frequencyRange;

        //评估参数（变换单元）
        private Converter converter; //变换单元
        private string selectedConverter; //所要评估的变换单元

        //不同负载下的损耗分布
        private int div = 100; //空载到满载划分精度

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 切换页面类型，更新当前显示页面以及侧边栏显示
        /// </summary>
        /// <param name="index">页面类型编号</param>
        private void ChangePanel(int index)
        {
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[index];
            panelNow[0].Visible = true;

            switch (index)
            {
                case 1:
                    Tab_Home_Button.BackColor = activeColor;
                    Tab_Estimate_Button.BackColor = inactiveColor;
                    Tab_Display_Button.BackColor = inactiveColor;
                    Tab_Admin_Button.BackColor = inactiveColor;
                    break;
                case 2:
                    Tab_Home_Button.BackColor = inactiveColor;
                    Tab_Estimate_Button.BackColor = activeColor;
                    Tab_Display_Button.BackColor = inactiveColor;
                    Tab_Admin_Button.BackColor = inactiveColor;
                    break;
                case 3:
                    Tab_Home_Button.BackColor = inactiveColor;
                    Tab_Estimate_Button.BackColor = inactiveColor;
                    Tab_Display_Button.BackColor = activeColor;
                    Tab_Admin_Button.BackColor = inactiveColor;
                    break;
                case 4:
                    Tab_Home_Button.BackColor = inactiveColor;
                    Tab_Estimate_Button.BackColor = inactiveColor;
                    Tab_Display_Button.BackColor = inactiveColor;
                    Tab_Admin_Button.BackColor = activeColor;
                    break;
            }
        }

        /// <summary>
        /// 切换子页面，更新当前显示页面以及侧边栏显示
        /// </summary>
        /// <param name="index">页面类型编号</param>
        /// <param name="panel">要显示的页面</param>
        private void ChangePanel(int index, Panel panel)
        {
            panelNow[index] = panel;
            ChangePanel(index);
        }

        /// <summary>
        /// 选择器件步骤，创建器件组名标签
        /// </summary>
        /// <param name="text">器件组名</param>
        /// <returns>创建的Label</returns>
        private Label Estimate_Step4_CreateLabel(string text)
        {
            return new Label
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                Size = new System.Drawing.Size(1240, 30),
                Text = text,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
        }

        /// <summary>
        /// 选择器件步骤，根据器件名生成选项
        /// </summary>
        /// <param name="text">器件名</param>
        /// <returns>创建的CheckBox</returns>
        private CheckBox Estimate_Step4_CreateCheckBox(string text)
        {
            return new CheckBox
            {
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                Size = new System.Drawing.Size(150, 25),
                Text = text,
                UseVisualStyleBackColor = true,
                Checked = true
            };
        }

        /// <summary>
        /// 评估结果步骤，开始评估
        /// </summary>
        private void Estimate_Result_Evaluate()
        {
            PrintMsg("初始化……");
            //更新开关器件可用状态
            foreach (Control control in Estimate_Step4_Semiconductor_FlowLayoutPanel.Controls)
            {
                if (control.GetType() == typeof(CheckBox))
                {
                    foreach (Data.Semiconductor semiconductor in Data.SemiconductorList)
                    {
                        if (semiconductor.Type.Equals(control.Text))
                        {
                            semiconductor.Available = ((CheckBox)control).Checked;
                            break;
                        }
                    }
                }
            }

            //更新磁芯可用状态
            foreach (Control control in Estimate_Step4_Core_FlowLayoutPanel.Controls)
            {
                if (control.GetType() == typeof(CheckBox))
                {
                    foreach (Data.Core core in Data.CoreList)
                    {
                        if (core.Type.Equals(control.Text))
                        {
                            core.Available = ((CheckBox)control).Checked;
                            break;
                        }
                    }
                }
            }

            //更新绕线可用状态
            foreach (Control control in Estimate_Step4_Wire_FlowLayoutPanel.Controls)
            {
                if (control.GetType() == typeof(CheckBox))
                {
                    foreach (Data.Wire wire in Data.WireList)
                    {
                        if (wire.Type.Equals(control.Text))
                        {
                            wire.Available = ((CheckBox)control).Checked;
                            break;
                        }
                    }
                }
            }

            //更新电容可用状态
            foreach (Control control in Estimate_Step4_Capacitor_FlowLayoutPanel.Controls)
            {
                if (control.GetType() == typeof(CheckBox))
                {
                    foreach (Data.Capacitor capacitor in Data.CapacitorList)
                    {
                        if (capacitor.Type.Equals(control.Text))
                        {
                            capacitor.Available = ((CheckBox)control).Checked;
                            break;
                        }
                    }
                }
            }

            if (selectedStructure != null)
            {
                Psys = double.Parse(Estimate_Step3_Psys_TextBox.Text) * 1e6;
                Vpv_min = double.Parse(Estimate_Step3_Vpvmin_TextBox.Text);
                Vpv_max = double.Parse(Estimate_Step3_Vpvmax_TextBox.Text);
                Vpv_peak = double.Parse(Estimate_Step3_Vpvpeak_TextBox.Text);
                Vg = double.Parse(Estimate_Step3_Vgrid_TextBox.Text) * 1e3;
                Vo = Vg / Math.Sqrt(3);
                isolatedDCDC_Q = double.Parse(Estimate_Step3_IsolatedDCDCQ_TextBox.Text);
                DCAC_Ma = double.Parse(Estimate_Step3_DCACMa_TextBox.Text);
                switch (selectedStructure)
                {
                    case "三级架构":
                        VbusRange = Function.GenerateVbusRange(int.Parse(Estimate_Step3_Vbusmin_TextBox.Text), int.Parse(Estimate_Step3_Vbusmax_TextBox.Text));
                        DCDC_numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_DCDCMinNumber_TextBox.Text), int.Parse(Estimate_Step3_DCDCMaxNumber_TextBox.Text));
                        DCDC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_DCDCMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_DCDCMaxFrequency_TextBox.Text) * 1e3);
                        isolatedDCDC_secondaryRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.Text), int.Parse(Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.Text));
                        isolatedDCDC_numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text), int.Parse(Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text));
                        isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text) * 1e3);
                        DCAC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_DCACMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_DCACMaxFrequency_TextBox.Text) * 1e3);
                        break;
                    case "两级架构":
                        isolatedDCDC_secondaryRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.Text), int.Parse(Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.Text));
                        isolatedDCDC_numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text), int.Parse(Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text));
                        isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text) * 1e3);
                        DCAC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_DCACMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_DCACMaxFrequency_TextBox.Text) * 1e3);
                        break;
                }

                Formula.Init();
                switch (selectedStructure)
                {
                    case "三级架构":
                        structure = new ThreeLevelStructure
                        {
                            Math_Psys = Psys,
                            Math_Vpv_min = Vpv_min,
                            Math_Vpv_max = Vpv_max,
                            Math_Vg = Vg,
                            Math_Vo = Vo,
                            Math_fg = fg,
                            Math_VbusRange = VbusRange,
                            DCDC_numberRange = DCDC_numberRange,
                            DCDC_topologyRange = DCDC_topologyRange,
                            DCDC_frequencyRange = DCDC_frequencyRange,
                            IsolatedDCDC_Q = isolatedDCDC_Q,
                            IsolatedDCDC_secondaryRange = isolatedDCDC_secondaryRange,
                            IsolatedDCDC_numberRange = isolatedDCDC_numberRange,
                            IsolatedDCDC_topologyRange = isolatedDCDC_topologyRange,
                            IsolatedDCDC_resonanceFrequencyRange = isolatedDCDC_resonanceFrequencyRange,
                            DCAC_Ma = DCAC_Ma,
                            DCAC_phi = DCAC_phi,
                            DCAC_topologyRange = DCAC_topologyRange,
                            DCAC_modulationRange = DCAC_modulationRange,
                            DCAC_frequencyRange = DCAC_frequencyRange,
                        };
                        break;
                    case "两级架构":
                        structure = new TwoLevelStructure
                        {
                            Math_Psys = Psys,
                            Math_Vpv_min = Vpv_min,
                            Math_Vpv_max = Vpv_max,
                            Math_Vg = Vg,
                            Math_Vo = Vo,
                            Math_fg = fg,
                            IsolatedDCDC_Q = isolatedDCDC_Q,
                            IsolatedDCDC_secondaryRange = isolatedDCDC_secondaryRange,
                            IsolatedDCDC_numberRange = isolatedDCDC_numberRange,
                            IsolatedDCDC_topologyRange = isolatedDCDC_topologyRange,
                            IsolatedDCDC_resonanceFrequencyRange = isolatedDCDC_resonanceFrequencyRange,
                            DCAC_Vin_def = DCAC_Vin_def,
                            DCAC_Ma = DCAC_Ma,
                            DCAC_phi = DCAC_phi,
                            DCAC_topologyRange = DCAC_topologyRange,
                            DCAC_modulationRange = DCAC_modulationRange,
                            DCAC_frequencyRange = DCAC_frequencyRange,
                        };
                        break;
                }
            }
            else
            {
                int[] secondaryRange = new int[0];
                if (!Estimate_Step3B_MinSecondary_TextBox.Text.Equals(""))
                {
                    secondaryRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3B_MinSecondary_TextBox.Text), int.Parse(Estimate_Step3B_MaxSecondary_TextBox.Text));
                }
                int[] numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3B_MinNumber_TextBox.Text), int.Parse(Estimate_Step3B_MaxNumber_TextBox.Text));
                double[] frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3B_MinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3B_MaxFrequency_TextBox.Text) * 1e3);

                switch (selectedConverter)
                {
                    case "前级DC/DC变换单元_三级":
                        double Psys = double.Parse(Estimate_Step3B_Psys_TextBox.Text) * 1e6;
                        double Vin_min = double.Parse(Estimate_Step3B_Vinmin_TextBox.Text);
                        double Vin_max = double.Parse(Estimate_Step3B_Vinmax_TextBox.Text);
                        double Vo = double.Parse(Estimate_Step3B_Vo_TextBox.Text);
                        converter = new DCDCConverter(Psys, Vin_min, Vin_max, Vo)
                        {
                            NumberRange = numberRange,
                            TopologyRange = DCDC_topologyRange,
                            FrequencyRange = frequencyRange
                        };
                        break;
                    case "隔离DC/DC变换单元_三级":
                        Formula.Init();
                        Psys = double.Parse(Estimate_Step3B_Psys_TextBox.Text) * 1e6;
                        double Vin = double.Parse(Estimate_Step3B_Vin_TextBox.Text);
                        Vo = double.Parse(Estimate_Step3B_Vo_TextBox.Text);
                        double Q = double.Parse(Estimate_Step3B_Q_TextBox.Text);
                        converter = new IsolatedDCDCConverter(Psys, Vin, Vo, Q)
                        {
                            SecondaryRange = secondaryRange,
                            NumberRange = numberRange,
                            TopologyRange = isolatedDCDC_topologyRange,
                            FrequencyRange = frequencyRange
                        };
                        break;
                    case "隔离DC/DC变换单元_两级":
                        Formula.Init();
                        Psys = double.Parse(Estimate_Step3B_Psys_TextBox.Text) * 1e6;
                        Vin_min = double.Parse(Estimate_Step3B_Vinmin_TextBox.Text);
                        Vin_max = double.Parse(Estimate_Step3B_Vinmax_TextBox.Text);
                        Vo = double.Parse(Estimate_Step3B_Vo_TextBox.Text);
                        Q = double.Parse(Estimate_Step3B_Q_TextBox.Text);
                        converter = new IsolatedDCDCConverter(Psys, Vin_min, Vin_max, Vo, Q)
                        {
                            SecondaryRange = secondaryRange,
                            NumberRange = numberRange,
                            TopologyRange = isolatedDCDC_topologyRange,
                            FrequencyRange = frequencyRange
                        };
                        break;
                    case "逆变单元":
                        Psys = double.Parse(Estimate_Step3B_Psys_TextBox.Text) * 1e6;
                        double Vg = double.Parse(Estimate_Step3B_Vo_TextBox.Text) * 1e3;
                        double fg = 50; //并网频率
                        double Ma = double.Parse(Estimate_Step3B_Ma_TextBox.Text);
                        double phi = 0; //功率因数角(rad)
                        string[] modulationRange = { "PSPWM", "LSPWM" };
                        converter = new DCACConverter(Psys, Vg, fg, phi)
                        {
                            Math_Ma = Ma,
                            NumberRange = numberRange,
                            TopologyRange = DCAC_topologyRange,
                            ModulationRange = modulationRange,
                            FrequencyRange = frequencyRange
                        };
                        break;
                }
            }
            PrintMsg("开始评估！");
            
            if (selectedStructure != null)
            {
                structure.Optimize(this);
            }
            else
            {
                converter.Optimize(this);
            }

            BeginInvoke(new EventHandler(delegate
            {
                PrintMsg("完成评估！");

                //按钮状态设置
                Estimate_Result_End_Button.Visible = false;
                Estimate_Result_End_Button.Enabled = false;
                Estimate_Result_Restart_Button.Visible = true;
                Estimate_Result_Restart_Button.Enabled = true;
                Estimate_Result_QuickSave_Button.Enabled = true;
                Estimate_Result_Save_Button.Enabled = true;
                Estimate_Result_Display_Button.Enabled = true;
            }));
        }

        /// <summary>
        /// 打印详细信息
        /// 由isPrintDetails进行判断是否需要打印
        /// </summary>
        /// <param name="text">文字内容</param>
        public void PrintDetails(string text = "")
        {
            if (isPrintDetails)
            {
                PrintMsg(text);
            }
        }

        /// <summary>
        /// 打印信息
        /// </summary>
        /// <param name="text">文字内容</param>
        public void PrintMsg(string text = "")
        {
            if (Thread.CurrentThread.IsBackground)
            {
                BeginInvoke(new EventHandler(delegate
                {
                    Estimate_Result_Print_RichTextBox.AppendText(text + "\r\n");
                }));
            }
            else
            {
                Estimate_Result_Print_RichTextBox.AppendText(text + "\r\n");
            }
        }

        /// <summary>
        /// 展示页面，显示评估结果图像
        /// </summary>
        private void Display_Show_Display()
        {
            //获取数据
            IConverterDesignData[] data;
            if (selectedStructure != null)
            {
                data = structure.AllDesignList.GetData();
            }
            else
            {
                data = converter.AllDesignList.GetData();
            }

            //更新图像显示
            Display_Show_Graph_Panel.Controls.Remove(Display_Show_Graph_CartesianChart);
            Display_Show_Graph_CartesianChart.Dispose();
            Display_Show_Graph_CartesianChart = new LiveCharts.WinForms.CartesianChart
            {
                BackColor = System.Drawing.Color.White,
                DisableAnimations = true,
                Location = new System.Drawing.Point(67, 88),
                Size = new System.Drawing.Size(946, 665),
                TabIndex = 2,
            };
            Display_Show_Graph_Panel.Controls.Add(Display_Show_Graph_CartesianChart);
            Display_Show_Graph_Panel.Visible = false; //解决底色变黑
            Display_Show_Graph_Panel.Visible = true;
            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            switch (Display_Show_GraphCategory_ComboBox.Text)
            {
                case "成本-效率":
                    for (int i = 0; i < data.Length; i++)
                    {
                        values.Add(new ObservablePoint(data[i].Cost / 1e4, data[i].Efficiency * 100));
                    }
                    Display_Show_Graph_CartesianChart.AxisX.Add(new Axis
                    {
                        Title = "成本（万元）"
                    });
                    Display_Show_Graph_CartesianChart.AxisY.Add(new Axis
                    {
                        LabelFormatter = value => Math.Round(value, 8).ToString(),
                        Title = "中国效率（%）"
                    });
                    break;
                case "体积-效率":
                    for (int i = 0; i < data.Length; i++)
                    {
                        values.Add(new ObservablePoint(data[i].Volume, data[i].Efficiency * 100));
                    }
                    Display_Show_Graph_CartesianChart.AxisX.Add(new Axis
                    {

                        Title = "体积（dm^3）"
                    });
                    Display_Show_Graph_CartesianChart.AxisY.Add(new Axis
                    {
                        LabelFormatter = value => Math.Round(value, 8).ToString(),
                        Title = "中国效率（%）"
                    });
                    break;
                case "成本-体积":
                    for (int i = 0; i < data.Length; i++)
                    {
                        values.Add(new ObservablePoint(data[i].Cost / 1e4, data[i].Volume));
                    }
                    Display_Show_Graph_CartesianChart.AxisX.Add(new Axis
                    {
                        Title = "成本（万元）"
                    });
                    Display_Show_Graph_CartesianChart.AxisY.Add(new Axis
                    {
                        Title = "体积（dm^3）"
                    });
                    break;
            }
            Display_Show_Graph_CartesianChart.Series.Add(new GScatterSeries
            {
                Title = "Results",
                Values = values.AsGearedValues().WithQuality(Quality.Low),
                Fill = Brushes.Transparent,
                StrokeThickness = .5,
                PointGeometry = null //use a null geometry when you have many series
            });
            //Pareto前沿
            //values = new ChartValues<ObservablePoint>();
            //for (int i = 0; i < 20; i++)
            //{
            //    values.Add(new ObservablePoint(resultList.cost[i], resultList.efficiency[i]));
            //}
            //cartesianChart1.Series.Add(new LineSeries
            //{
            //    Values = values,
            //    LineSmoothness = 0,
            //    PointGeometry = null
            //});
            Display_Show_Graph_CartesianChart.Zoom = ZoomingOptions.Xy;
            Display_Show_Graph_CartesianChart.LegendLocation = LegendLocation.Right;
            Display_Show_Graph_CartesianChart.DataClick += Chart_OnDataClick; //添加评估图像点的点击事件

            //清空预览面板显示
            Display_Show_Preview_Main_Panel.Controls.Clear();

            //更新控件可用状态      
            Display_Show_Detail_Button.Enabled = false;
        }

        /// <summary>
        /// 展示页面-预览，生成标题行，用Panel包装
        /// </summary>
        /// <param name="text">文字信息</param>
        /// <returns>包含标题的Panel</returns>
        private Panel Display_Show_Preview_CreateTitle(string text)
        {
            Panel panel = new Panel
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(348, 60),
            };
            panel.Controls.Add(new Label
            {
                AutoSize = false,
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                Location = new System.Drawing.Point(48, 0),
                Size = new System.Drawing.Size(200, 60),
                Text = text,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            });
            return panel;
        }

        /// <summary>
        /// 展示页面-预览，生成信息行，用Panel包装
        /// </summary>
        /// <param name="title">信息标题</param>
        /// <param name="text">信息内容</param>
        /// <returns>包含信息的Panel</returns>
        private Panel Display_Show_Preview_CreateInfo(string title, string text)
        {
            Panel panel = new Panel
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(348, 40),
            };
            panel.Controls.Add(new Label
            {
                AutoSize = false,
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                Location = new System.Drawing.Point(48, 0),
                Size = new System.Drawing.Size(150, 40),
                Text = title,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            });
            panel.Controls.Add(new Label
            {
                AutoSize = false,
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                Location = new System.Drawing.Point(198, 0),
                Size = new System.Drawing.Size(150, 40),
                Text = text,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            });
            return panel;
        }

        /// <summary>
        /// 评估图像点的点击事件
        /// 载入该点对应设计方案，并更新预览面板
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="chartPoint">点击的点</param>
        private void Chart_OnDataClick(object sender, ChartPoint chartPoint)
        {
            List<Panel> panelList = new List<Panel>(); //用于记录将要在预览面板中显示的信息（因为显示时设置了Dock=Top，而后生成的信息将显示在上方，所以在此处记录后，逆序添加控件）

            //目前采用评估结果比较来查找 TODO 能否直接将chartPoint与点的具体信息相联系
            string[] configs = new string[1];
            if (selectedStructure != null)
            {
                //查找对应设计方案
                switch (Display_Show_GraphCategory_ComboBox.Text)
                {
                    case "成本-效率":
                        configs = structure.AllDesignList.GetConfigs(chartPoint.Y / 100, double.NaN, chartPoint.X * 1e4);
                        break;
                    case "体积-效率":
                        configs = structure.AllDesignList.GetConfigs(chartPoint.Y / 100, chartPoint.X, double.NaN);
                        break;
                    case "成本-体积":
                        configs = structure.AllDesignList.GetConfigs(double.NaN, chartPoint.Y, chartPoint.X * 1e4);
                        break;
                }
                int index = 0;
                structure.Load(configs, ref index); //读取设计方案

                //生成预览面板显示信息
                panelList.Add(Display_Show_Preview_CreateTitle("性能表现："));
                panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (structure.EfficiencyCGC * 100).ToString("f2") + "%"));
                panelList.Add(Display_Show_Preview_CreateInfo("成本：", (structure.Cost / 1e4).ToString("f2") + "万元"));
                panelList.Add(Display_Show_Preview_CreateInfo("体积：", structure.Volume.ToString("f2") + "dm^3"));
                panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
                panelList.Add(Display_Show_Preview_CreateInfo("架构：", selectedStructure));
                switch (selectedStructure)
                {
                    case "三级架构":
                        panelList.Add(Display_Show_Preview_CreateTitle("前级DC/DC："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)structure).DCDC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)structure).DCDC.Math_fs / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)structure).DCDC.Topology.GetName()));
                        panelList.Add(Display_Show_Preview_CreateTitle("隔离DC/DC："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)structure).IsolatedDCDC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)structure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)structure).IsolatedDCDC.Topology.GetName()));
                        panelList.Add(Display_Show_Preview_CreateTitle("逆变："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)structure).DCAC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)structure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)structure).DCAC.Modulation.ToString()));
                        break;

                    case "两级架构":
                        panelList.Add(Display_Show_Preview_CreateTitle("隔离DC/DC："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((TwoLevelStructure)structure).IsolatedDCDC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((TwoLevelStructure)structure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((TwoLevelStructure)structure).IsolatedDCDC.Topology.GetName()));
                        panelList.Add(Display_Show_Preview_CreateTitle("逆变："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((TwoLevelStructure)structure).DCAC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((TwoLevelStructure)structure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((TwoLevelStructure)structure).DCAC.Modulation.ToString()));
                        break;
                }
            }
            else
            {
                //查找对应设计方案
                switch (Display_Show_GraphCategory_ComboBox.Text)
                {
                    case "成本-效率":
                        configs = converter.AllDesignList.GetConfigs(chartPoint.Y / 100, double.NaN, chartPoint.X * 1e4);
                        break;
                    case "体积-效率":
                        configs = converter.AllDesignList.GetConfigs(chartPoint.Y / 100, chartPoint.X, double.NaN);
                        break;
                    case "成本-体积":
                        configs = converter.AllDesignList.GetConfigs(double.NaN, chartPoint.Y, chartPoint.X * 1e4);
                        break;
                }
                int index = 0;
                converter.Load(configs, ref index); //读取设计方案

                //生成预览面板显示信息
                panelList.Add(Display_Show_Preview_CreateTitle("性能表现："));
                panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (converter.EfficiencyCGC * 100).ToString("f2") + "%"));
                panelList.Add(Display_Show_Preview_CreateInfo("成本：", (converter.Cost / 1e4).ToString("f2") + "万元"));
                panelList.Add(Display_Show_Preview_CreateInfo("体积：", converter.Volume.ToString("f2") + "dm^3"));
                panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
                panelList.Add(Display_Show_Preview_CreateInfo("模块数：", converter.Number.ToString()));
                if (selectedConverter.Equals("隔离DC/DC变换单元_三级") || selectedConverter.Equals("隔离DC/DC变换单元_两级"))
                {
                    panelList.Add(Display_Show_Preview_CreateInfo("谐振频率：", (((IsolatedDCDCConverter)converter).Math_fr / 1e3).ToString("f1") + "kHz"));
                }
                else
                {
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (converter.Math_fs / 1e3).ToString("f1") + "kHz"));
                }
                panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", converter.Topology.GetName()));
                //switch (selectedConverter)
                //{
                //    case "前级DC/DC变换单元_三级":
                //        panelList.Add(CreateItem("模块数：", converter.Number.ToString()));
                //        panelList.Add(CreateItem("开关频率：", (converter.Math_fs / 1e3).ToString("f1") + "kHz"));
                //        panelList.Add(CreateItem("拓扑：", converter.Topology.GetName()));
                //        break;
                //    case "隔离DC/DC变换单元_三级":
                //        panelList.Add(CreateItem("模块数：", converter.Number.ToString()));
                //        panelList.Add(CreateItem("开关频率：", (converter.Math_fs / 1e3).ToString("f1") + "kHz"));
                //        panelList.Add(CreateItem("拓扑：", converter.Topology.GetName()));
                //        break;
                //    case "隔离DC/DC变换单元_两级":
                //        panelList.Add(CreateItem("模块数：", converter.Number.ToString()));
                //        panelList.Add(CreateItem("开关频率：", (converter.Math_fs / 1e3).ToString("f1") + "kHz"));
                //        panelList.Add(CreateItem("拓扑：", converter.Topology.GetName()));
                //        break;
                //    case "逆变单元":
                //        panelList.Add(CreateItem("模块数：", converter.Number.ToString()));
                //        panelList.Add(CreateItem("开关频率：", (converter.Math_fs / 1e3).ToString("f1") + "kHz"));
                //        panelList.Add(CreateItem("拓扑：", converter.Topology.GetName()));
                //        break;
                //}
            }

            //更新预览面板显示
            Display_Show_Preview_Main_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Show_Preview_Main_Panel.Controls.Add(panelList[i]);
            }

            //更新控件可用状态
            Display_Show_Detail_Button.Enabled = true;
        }

        /// <summary>
        /// 更新饼图显示
        /// </summary>
        /// <param name="pieChart">操作的饼图对象</param>
        /// <param name="dataList">数据</param>
        private void DisplayPieChart(LiveCharts.WinForms.PieChart pieChart, List<Item> dataList)
        {
            string labelPoint(ChartPoint chartPoint) => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation); //饼图数据标签显示格式
            SeriesCollection series = new SeriesCollection();
            for (int i = 0; i < dataList.Count; i++)
            {
                if (Math.Round(dataList[i].Value, 2) > 0)
                {
                    series.Add(new PieSeries
                    {
                        Title = dataList[i].Name,
                        Values = new ChartValues<double> { dataList[i].Value },
                        DataLabels = true,
                        LabelPoint = labelPoint
                    });
                }
            }
            pieChart.Series = series;
            pieChart.StartingRotationAngle = 0;
            pieChart.LegendLocation = LegendLocation.Bottom;
        }

        /// <summary>
        /// 详情页面，整体系统子页面显示（不包括损耗分布图像显示）
        /// </summary>
        /// <param name="data">负载-效率曲线数据</param>
        private void Display_Show_Detail_System_Display(double[,] data)
        {
            //生成文字信息
            List<Panel> panelList = new List<Panel>(); //用于记录将要在预览面板中显示的信息（因为显示时设置了Dock=Top，而后生成的信息将显示在上方，所以在此处记录后，逆序添加控件）
            panelList.Add(Display_Show_Preview_CreateTitle("性能表现："));
            panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (structure.EfficiencyCGC * 100).ToString("f2") + "%"));
            panelList.Add(Display_Show_Preview_CreateInfo("成本：", (structure.Cost / 1e4).ToString("f2") + "万元"));
            panelList.Add(Display_Show_Preview_CreateInfo("体积：", structure.Volume.ToString("f2") + "dm^3"));
            panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
            panelList.Add(Display_Show_Preview_CreateInfo("架构：", selectedStructure));
            switch (selectedStructure)
            {
                case "三级架构":
                    panelList.Add(Display_Show_Preview_CreateTitle("前级DC/DC："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)structure).DCDC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)structure).DCDC.Math_fs / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)structure).DCDC.Topology.GetName()));
                    panelList.Add(Display_Show_Preview_CreateTitle("隔离DC/DC："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)structure).IsolatedDCDC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)structure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)structure).IsolatedDCDC.Topology.GetName()));
                    panelList.Add(Display_Show_Preview_CreateTitle("逆变："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)structure).DCAC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)structure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)structure).DCAC.Modulation.ToString()));
                    break;

                case "两级架构":
                    panelList.Add(Display_Show_Preview_CreateTitle("隔离DC/DC："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((TwoLevelStructure)structure).IsolatedDCDC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((TwoLevelStructure)structure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((TwoLevelStructure)structure).IsolatedDCDC.Topology.GetName()));
                    panelList.Add(Display_Show_Preview_CreateTitle("逆变："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((TwoLevelStructure)structure).DCAC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((TwoLevelStructure)structure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((TwoLevelStructure)structure).DCAC.Modulation.ToString()));
                    break;
            }
            //更新面板显示
            Display_Detail_System_Right_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Detail_System_Right_Panel.Controls.Add(panelList[i]);
            }

            //生成图像
            DisplayPieChart(Display_Detail_System_CostBreakdown_PieChart, structure.GetCostBreakdown()); //成本分布饼图
            DisplayPieChart(Display_Detail_System_VolumeBreakdown_PieChart, structure.GetVolumeBreakdown()); //体积分布饼图

            //负载-效率图像
            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            for (int i = 0; i < div; i++)
            {
                values.Add(new ObservablePoint(data[i, 0], data[i, 1]));
            }
            Display_Detail_System_LoadVsEfficiency_CartesianChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Vin=" + structure.Math_Vpv_min + "V",
                    Values = values
                }
            };
            Display_Detail_System_LoadVsEfficiency_CartesianChart.AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = "负载（%）"
                }
            };
            Display_Detail_System_LoadVsEfficiency_CartesianChart.AxisY = new AxesCollection
            {
                new Axis
                {
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Detail_System_LoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Right;

            //更新控件状态以显示
            Display_Detail_TabControl.Controls.Add(Display_Detail_System_TabPage);
        }

        /// <summary>
        /// 详情页面，前级DCDC子页面显示（不包括损耗分布图像显示）
        /// </summary>
        /// <param name="data">负载-效率曲线数据</param>
        private void Display_Show_Detail_DCDC_Display(double[,] data)
        {
            //生成文字信息
            List<Panel> panelList = new List<Panel>(); //用于记录将要在预览面板中显示的信息（因为显示时设置了Dock=Top，而后生成的信息将显示在上方，所以在此处记录后，逆序添加控件）
            panelList.Add(Display_Show_Preview_CreateTitle("性能表现："));
            panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (converter.EfficiencyCGC * 100).ToString("f2") + "%"));
            panelList.Add(Display_Show_Preview_CreateInfo("成本：", (converter.Cost / 1e4).ToString("f2") + "万元"));
            panelList.Add(Display_Show_Preview_CreateInfo("体积：", converter.Volume.ToString("f2") + "dm^3"));
            panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
            panelList.Add(Display_Show_Preview_CreateInfo("模块数：", converter.Number.ToString()));
            panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (converter.Math_fs / 1e3).ToString("f1") + "kHz"));
            panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", converter.Topology.GetName()));
            //更新面板显示
            Display_Detail_DCDC_Right_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Detail_DCDC_Right_Panel.Controls.Add(panelList[i]);
            }

            //生成图像
            DisplayPieChart(Display_Detail_DCDC_CostBreakdown_PieChart, converter.GetCostBreakdown()); //成本分布饼图
            DisplayPieChart(Display_Detail_DCDC_VolumeBreakdown_PieChart, converter.GetVolumeBreakdown()); //体积分布饼图

            //负载-效率图像
            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            for (int i = 0; i < div; i++)
            {
                values.Add(new ObservablePoint(data[i, 0], data[i, 1]));
            }
            Display_Detail_DCDC_LoadVsEfficiency_CartesianChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Vin=" + ((DCDCConverter)converter).Math_Vin_min + "V",
                    Values = values
                }
            };
            Display_Detail_DCDC_LoadVsEfficiency_CartesianChart.AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = "负载（%）"
                }
            };
            Display_Detail_DCDC_LoadVsEfficiency_CartesianChart.AxisY = new AxesCollection
            {
                new Axis
                {
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Detail_DCDC_LoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Right;

            //更新控件状态以显示
            Display_Detail_TabControl.Controls.Add(Display_Detail_DCDC_TabPage);
        }

        /// <summary>
        /// 详情页面，隔离DCDC子页面显示（不包括损耗分布图像显示）
        /// </summary>
        /// <param name="data">负载-效率曲线数据</param>
        private void Display_Show_Detail_IsolatedDCDC_Display(double[,] data)
        {
            //生成文字信息
            List<Panel> panelList = new List<Panel>(); //用于记录将要在预览面板中显示的信息（因为显示时设置了Dock=Top，而后生成的信息将显示在上方，所以在此处记录后，逆序添加控件）
            panelList.Add(Display_Show_Preview_CreateTitle("性能表现："));
            panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (converter.EfficiencyCGC * 100).ToString("f2") + "%"));
            panelList.Add(Display_Show_Preview_CreateInfo("成本：", (converter.Cost / 1e4).ToString("f2") + "万元"));
            panelList.Add(Display_Show_Preview_CreateInfo("体积：", converter.Volume.ToString("f2") + "dm^3"));
            panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
            panelList.Add(Display_Show_Preview_CreateInfo("模块数：", converter.Number.ToString()));
            panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((IsolatedDCDCConverter)converter).Math_fr / 1e3).ToString("f1") + "kHz"));
            panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", converter.Topology.GetName()));
            //更新面板显示
            Display_Detail_IsolatedDCDC_Right_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Detail_IsolatedDCDC_Right_Panel.Controls.Add(panelList[i]);
            }

            //生成图像
            DisplayPieChart(Display_Detail_IsolatedDCDC_CostBreakdown_PieChart, converter.GetCostBreakdown()); //成本分布饼图
            DisplayPieChart(Display_Detail_IsolatedDCDC_VolumeBreakdown_PieChart, converter.GetVolumeBreakdown()); //体积分布饼图

            //负载-效率图像
            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            for (int i = 0; i < div; i++)
            {
                values.Add(new ObservablePoint(data[i, 0], data[i, 1]));
            }
            Display_Detail_IsolatedDCDC_LoadVsEfficiency_CartesianChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Vin=" + ((IsolatedDCDCConverter)converter).Math_Vin + "V",
                    Values = values
                }
            };
            Display_Detail_IsolatedDCDC_LoadVsEfficiency_CartesianChart.AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = "负载（%）"
                }
            };
            Display_Detail_IsolatedDCDC_LoadVsEfficiency_CartesianChart.AxisY = new AxesCollection
            {
                new Axis
                {
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Detail_IsolatedDCDC_LoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Right;

            //更新控件状态以显示
            Display_Detail_TabControl.Controls.Add(Display_Detail_IsolatedDCDC_TabPage);
        }

        /// <summary>
        /// 详情页面，逆变子页面显示（不包括损耗分布图像显示）
        /// </summary>
        /// <param name="data">负载-效率曲线数据</param>
        private void Display_Show_Detail_DCAC_Display(double[,] data)
        {
            //生成文字信息
            List<Panel> panelList = new List<Panel>(); //用于记录将要在预览面板中显示的信息（因为显示时设置了Dock=Top，而后生成的信息将显示在上方，所以在此处记录后，逆序添加控件）
            panelList.Add(Display_Show_Preview_CreateTitle("性能表现："));
            panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (converter.EfficiencyCGC * 100).ToString("f2") + "%"));
            panelList.Add(Display_Show_Preview_CreateInfo("成本：", (converter.Cost / 1e4).ToString("f2") + "万元"));
            panelList.Add(Display_Show_Preview_CreateInfo("体积：", converter.Volume.ToString("f2") + "dm^3"));
            panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
            panelList.Add(Display_Show_Preview_CreateInfo("模块数：", converter.Number.ToString()));
            panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (converter.Math_fs / 1e3).ToString("f1") + "kHz"));
            panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", converter.Topology.GetName()));
            //更新面板显示
            Display_Detail_DCAC_Right_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Detail_DCAC_Right_Panel.Controls.Add(panelList[i]);
            }

            //生成图像
            DisplayPieChart(Display_Detail_DCAC_CostBreakdown_PieChart, converter.GetCostBreakdown()); //成本分布饼图
            DisplayPieChart(Display_Detail_DCAC_VolumeBreakdown_PieChart, converter.GetVolumeBreakdown()); //体积分布饼图

            //负载-效率图像
            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            for (int i = 0; i < div; i++)
            {
                values.Add(new ObservablePoint(data[i, 0], data[i, 1]));
            }
            Display_Detail_DCAC_LoadVsEfficiency_CartesianChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Vin=" + ((DCACConverter)converter).Math_Vin + "V", //TODO 这里显示什么标签？
                    Values = values
                }
            };
            Display_Detail_DCAC_LoadVsEfficiency_CartesianChart.AxisX = new AxesCollection
            {
                new Axis
                {
                    Title = "负载（%）"
                }
            };
            Display_Detail_DCAC_LoadVsEfficiency_CartesianChart.AxisY = new AxesCollection
            {
                new Axis
                {
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Detail_DCAC_LoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Right;

            //更新控件状态以显示
            Display_Detail_TabControl.Controls.Add(Display_Detail_DCAC_TabPage);
        }

        /// <summary>
        /// 显示LossBreakdown图像
        /// </summary>
        private void Display_Show_Detail_DisplayLossBreakdown()
        {
            double load = Display_Detail_Load_TrackBar.Value / 100.0;
            double Vin = Display_Detail_Vin_TrackBar.Value;

            if (selectedStructure != null)
            {
                //生成数据
                structure.Operate(load, Vin);

                //更新显示
                //更新图像
                DisplayPieChart(Display_Detail_System_LossBreakdown_PieChart, structure.GetLossBreakdown()); //整体系统损耗分布饼图
                switch (selectedStructure)
                {
                    case "三级架构":
                        DisplayPieChart(Display_Detail_DCDC_LossBreakdown_PieChart, ((ThreeLevelStructure)structure).DCDC.GetLossBreakdown()); //前级DC/DC损耗分布饼图
                        DisplayPieChart(Display_Detail_IsolatedDCDC_LossBreakdown_PieChart, ((ThreeLevelStructure)structure).IsolatedDCDC.GetLossBreakdown()); //隔离DC/DC损耗分布信息
                        DisplayPieChart(Display_Detail_DCAC_LossBreakdown_PieChart, ((ThreeLevelStructure)structure).DCAC.GetLossBreakdown()); //DC/AC损耗分布信息
                        break;

                    case "两级架构":
                        DisplayPieChart(Display_Detail_IsolatedDCDC_LossBreakdown_PieChart, ((TwoLevelStructure)structure).IsolatedDCDC.GetLossBreakdown()); //隔离DC/DC损耗分布信息
                        DisplayPieChart(Display_Detail_DCAC_LossBreakdown_PieChart, ((TwoLevelStructure)structure).DCAC.GetLossBreakdown()); //DC/AC损耗分布信息
                        converter = null;
                        break;
                }
            }
            else
            {
                //更新显示                
                switch (selectedConverter)
                {
                    case "前级DC/DC变换单元_三级":
                        converter.Operate(load, Vin);
                        DisplayPieChart(Display_Detail_DCDC_LossBreakdown_PieChart, converter.GetLossBreakdown()); //前级DC/DC损耗分布饼图
                        break;
                    case "隔离DC/DC变换单元_三级":
                        converter.Operate(load);
                        DisplayPieChart(Display_Detail_IsolatedDCDC_LossBreakdown_PieChart, converter.GetLossBreakdown()); //隔离DC/DC损耗分布饼图
                        break;
                    case "隔离DC/DC变换单元_两级":
                        converter.Operate(load, Vin);
                        DisplayPieChart(Display_Detail_IsolatedDCDC_LossBreakdown_PieChart, converter.GetLossBreakdown()); //隔离DC/DC损耗分布饼图
                        break;
                    case "逆变单元":
                        converter.Operate(load);
                        DisplayPieChart(Display_Detail_DCAC_LossBreakdown_PieChart, converter.GetLossBreakdown()); //逆变损耗分布饼图
                        break;
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            panelNow[0] = Home_Panel;
            panelNow[1] = Home_Panel;
            panelNow[2] = Estimate_Ready_Panel;
            panelNow[3] = Display_Ready_Panel;
            panelNow[4] = Admin_Panel;

            activeColor = Tab_Home_Button.BackColor;
            inactiveColor = Tab_Estimate_Button.BackColor;
        }

        private void Tab_Home_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(1);
        }

        private void Tab_Estimate_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(2);
        }

        private void Tab_Display_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(3);
        }

        private void Tab_Admin_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(4);
        }

        private void Estimate_Ready_System_button_Click(object sender, EventArgs e)
        {
            ChangePanel(2, Estimate_Step1_Panel);
        }

        private void Estimate_Ready_Converter_button_Click(object sender, EventArgs e)
        {
            ChangePanel(2, Estimate_Step1B_Panel);
        }

        private void Estimate_Step1_Prev_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(2, Estimate_Ready_Panel);
        }

        private void Estimate_Step1_Next_Button_Click(object sender, EventArgs e)
        {
            if (Estimate_Step1_CheckedListBox.CheckedItems.Count <= 0)
            {
                MessageBox.Show("请选择一项");
            }
            else if (Estimate_Step1_CheckedListBox.CheckedItems.Count > 1)
            {
                MessageBox.Show("只能选择一项");
            }
            else
            {
                selectedConverter = null;
                selectedStructure = Estimate_Step1_CheckedListBox.GetItemText(Estimate_Step1_CheckedListBox.CheckedItems[0]);
                switch (selectedStructure)
                {
                    case "三级架构":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item3_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item4_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = true;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item3_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item4_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = true;
                        break;
                    case "两级架构":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Enabled = true;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = true;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Checked = true;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = true;
                        break;
                }
                ChangePanel(2, Estimate_Step2_Panel);
            }
        }

        private void Estimate_Step1B_Prev_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(2, Estimate_Ready_Panel);
        }

        private void Estimate_Step1B_Next_Button_Click(object sender, EventArgs e)
        {
            if (Estimate_Step1B_CheckedListBox.CheckedItems.Count <= 0)
            {
                MessageBox.Show("请选择一项");
            }
            else if (Estimate_Step1B_CheckedListBox.CheckedItems.Count > 1)
            {
                MessageBox.Show("只能选择一项");
            }
            else
            {
                selectedConverter = Estimate_Step1B_CheckedListBox.GetItemText(Estimate_Step1B_CheckedListBox.CheckedItems[0]);
                selectedStructure = null;
                switch (selectedConverter)
                {
                    case "前级DC/DC变换单元_三级":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = false;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = false;
                        break;

                    case "隔离DC/DC变换单元_三级":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item3_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item4_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = false;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item3_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item4_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = false;
                        break;

                    case "隔离DC/DC变换单元_两级":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Enabled = true;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = false;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Checked = true;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = false;
                        break;

                    case "逆变单元":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = true;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = true;
                        break;
                }
                ChangePanel(2, Estimate_Step2_Panel);
            }
        }

        private void Estimate_Step2_Prev_Button_Click(object sender, EventArgs e)
        {
            if (selectedStructure != null)
            {
                ChangePanel(2, Estimate_Step1_Panel);
            }
            else
            {
                ChangePanel(2, Estimate_Step1B_Panel);
            }
        }

        private void Estimate_Step2_Next_Button_Click(object sender, EventArgs e)
        {
            List<string> DCDC_topologyList = new List<string>();
            if (Estimate_Step2_Group1_Item1_Left_CheckBox.Checked)
            {
                DCDC_topologyList.Add("ThreeLevelBoost");
            }
            if (Estimate_Step2_Group1_Item2_Left_CheckBox.Checked)
            {
                DCDC_topologyList.Add("TwoLevelBoost");
            }
            if (Estimate_Step2_Group1_Item3_Left_CheckBox.Checked)
            {
                DCDC_topologyList.Add("InterleavedBoost");
            }

            List<string> isolatedDCDC_topologyList = new List<string>();
            if (Estimate_Step2_Group2_Item1_Left_CheckBox.Checked)
            {
                isolatedDCDC_topologyList.Add("SRC");
            }
            if (Estimate_Step2_Group2_Item2_Left_CheckBox.Checked)
            {
                isolatedDCDC_topologyList.Add("LLC");
            }
            if (Estimate_Step2_Group2_Item3_Left_CheckBox.Checked)
            {
                isolatedDCDC_topologyList.Add("DAB");
            }
            if (Estimate_Step2_Group2_Item4_Left_CheckBox.Checked)
            {
                isolatedDCDC_topologyList.Add("DTCSRC");
            }

            List<string> DCAC_topologyList = new List<string>();
            if (Estimate_Step2_Group3_Item1_Left_CheckBox.Checked)
            {
                DCAC_topologyList.Add("CHB");
            }

            if (selectedStructure != null)
            {
                if (selectedStructure.Equals("三级架构") && DCDC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项前级DC/DC拓扑");
                    return;
                }
                else if (isolatedDCDC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项隔离DC/DC拓扑");
                    return;
                }
                else if (DCAC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项逆变拓扑");
                    return;
                }
                else
                {
                    switch (selectedStructure)
                    {
                        case "三级架构":
                            Estimate_Step3_Vbusmin_TextBox.Enabled = true;
                            Estimate_Step3_Vbusmax_TextBox.Enabled = true;
                            Estimate_Step3_DCDCMinNumber_TextBox.Enabled = true;
                            Estimate_Step3_DCDCMaxNumber_TextBox.Enabled = true;
                            Estimate_Step3_DCDCMinFrequency_TextBox.Enabled = true;
                            Estimate_Step3_DCDCMaxFrequency_TextBox.Enabled = true;
                            Estimate_Step3_IsolatedDCDCQ_TextBox.ReadOnly = false;
                            Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.ReadOnly = false;
                            Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.ReadOnly = false;
                            Estimate_Step3_IsolatedDCDCMinNumber_TextBox.ReadOnly = false;
                            Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.ReadOnly = false;
                            Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.ReadOnly = false;
                            Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.ReadOnly = false;
                            Estimate_Step3_DCACMa_TextBox.ReadOnly = false;
                            Estimate_Step3_DCACMinFrequency_TextBox.ReadOnly = false;
                            Estimate_Step3_DCACMaxFrequency_TextBox.ReadOnly = false;
                            Estimate_Step3_Vbusmin_TextBox.Text = "1300";
                            Estimate_Step3_Vbusmax_TextBox.Text = "1500";
                            Estimate_Step3_DCDCMinNumber_TextBox.Text = "1";
                            Estimate_Step3_DCDCMaxNumber_TextBox.Text = "120";
                            Estimate_Step3_DCDCMinFrequency_TextBox.Text = "1";
                            Estimate_Step3_DCDCMaxFrequency_TextBox.Text = "100";
                            Estimate_Step3_IsolatedDCDCQ_TextBox.Text = "1";
                            Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.Text = "1";
                            Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.Text = "1";
                            Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text = "1";
                            Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text = "40";
                            Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text = "1";
                            Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text = "100";
                            Estimate_Step3_DCACMa_TextBox.Text = "0.8";
                            Estimate_Step3_DCACMinNumber_TextBox.Text = "1";
                            Estimate_Step3_DCACMaxNumber_TextBox.Text = "40";
                            Estimate_Step3_DCACMinFrequency_TextBox.Text = "10";
                            Estimate_Step3_DCACMaxFrequency_TextBox.Text = "10";
                            break;
                        case "两级架构":
                            Estimate_Step3_Vbusmin_TextBox.Enabled = false;
                            Estimate_Step3_Vbusmax_TextBox.Enabled = false;
                            Estimate_Step3_DCDCMinNumber_TextBox.Enabled = false;
                            Estimate_Step3_DCDCMaxNumber_TextBox.Enabled = false;
                            Estimate_Step3_DCDCMinFrequency_TextBox.Enabled = false;
                            Estimate_Step3_DCDCMaxFrequency_TextBox.Enabled = false;
                            Estimate_Step3_IsolatedDCDCQ_TextBox.ReadOnly = true;
                            Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.ReadOnly = true;
                            Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.ReadOnly = true;
                            Estimate_Step3_IsolatedDCDCMinNumber_TextBox.ReadOnly = true;
                            Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.ReadOnly = true;
                            Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.ReadOnly = true;
                            Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.ReadOnly = true;
                            Estimate_Step3_DCACMa_TextBox.ReadOnly = true;
                            Estimate_Step3_DCACMinFrequency_TextBox.ReadOnly = true;
                            Estimate_Step3_DCACMaxFrequency_TextBox.ReadOnly = true;
                            Estimate_Step3_Vbusmin_TextBox.Text = "";
                            Estimate_Step3_Vbusmax_TextBox.Text = "";
                            Estimate_Step3_DCDCMinNumber_TextBox.Text = "";
                            Estimate_Step3_DCDCMaxNumber_TextBox.Text = "";
                            Estimate_Step3_DCDCMinFrequency_TextBox.Text = "";
                            Estimate_Step3_DCDCMaxFrequency_TextBox.Text = "";
                            Estimate_Step3_IsolatedDCDCQ_TextBox.Text = "1";
                            Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.Text = "1";
                            Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.Text = "1";
                            Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text = "20";
                            Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text = "20";
                            Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text = "25";
                            Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text = "25";
                            Estimate_Step3_DCACMa_TextBox.Text = "0.95";
                            Estimate_Step3_DCACMinNumber_TextBox.Text = "20";
                            Estimate_Step3_DCACMaxNumber_TextBox.Text = "20";
                            Estimate_Step3_DCACMinFrequency_TextBox.Text = "10";
                            Estimate_Step3_DCACMaxFrequency_TextBox.Text = "10";
                            break;
                    }
                    DCDC_topologyRange = DCDC_topologyList.ToArray();
                    isolatedDCDC_topologyRange = isolatedDCDC_topologyList.ToArray();
                    DCAC_topologyRange = DCAC_topologyList.ToArray();

                    ChangePanel(2, Estimate_Step3_Panel);
                }
            }
            else
            {
                if (selectedConverter.Equals("前级DC/DC变换单元_三级") && DCDC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项前级DC/DC拓扑");
                    return;
                }
                else if ((selectedConverter.Equals("隔离DC/DC变换单元_三级") || selectedConverter.Equals("隔离DC/DC变换单元_两级")) && isolatedDCDC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项隔离DC/DC拓扑");
                    return;
                }
                else if (selectedConverter.Equals("逆变单元") && DCAC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项逆变拓扑");
                    return;
                }
                else
                {
                    Estimate_Step3B_Converter_Label.Text = selectedConverter;
                    switch (selectedConverter)
                    {
                        case "前级DC/DC变换单元_三级":
                            Estimate_Step3B_Vinmin_TextBox.Enabled = true;
                            Estimate_Step3B_Vinmax_TextBox.Enabled = true;
                            Estimate_Step3B_Vin_TextBox.Enabled = false;
                            Estimate_Step3B_Q_TextBox.Enabled = false;
                            Estimate_Step3B_MinSecondary_TextBox.Enabled = false;
                            Estimate_Step3B_MaxSecondary_TextBox.Enabled = false;
                            Estimate_Step3B_Ma_TextBox.Enabled = false;
                            Estimate_Step3B_MinNumber_TextBox.Enabled = true;
                            Estimate_Step3B_MaxNumber_TextBox.Enabled = true;
                            Estimate_Step3B_MinFrequency_TextBox.Enabled = true;
                            Estimate_Step3B_MaxFrequency_TextBox.Enabled = true;

                            Estimate_Step3B_Vinmin_TextBox.Text = "860";
                            Estimate_Step3B_Vinmax_TextBox.Text = "1300";
                            Estimate_Step3B_Vin_TextBox.Text = "";
                            Estimate_Step3B_Vo_Label.Text = "输出电压";
                            Estimate_Step3B_Vo_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Unit_Label.Text = "V";
                            Estimate_Step3B_Q_TextBox.Text = "";
                            Estimate_Step3B_MinSecondary_TextBox.Text = "";
                            Estimate_Step3B_MaxSecondary_TextBox.Text = "";
                            Estimate_Step3B_Ma_TextBox.Text = "";
                            Estimate_Step3B_MinNumber_TextBox.Text = "1";
                            Estimate_Step3B_MaxNumber_TextBox.Text = "120";
                            Estimate_Step3B_Frequency_Label.Text = "开关频率";
                            Estimate_Step3B_MinFrequency_TextBox.Text = "1";
                            Estimate_Step3B_MaxFrequency_TextBox.Text = "100";

                            DCDC_topologyRange = DCDC_topologyList.ToArray();
                            break;
                        case "隔离DC/DC变换单元_三级":
                            Estimate_Step3B_Vinmin_TextBox.Enabled = false;
                            Estimate_Step3B_Vinmax_TextBox.Enabled = false;
                            Estimate_Step3B_Vin_TextBox.Enabled = true;
                            Estimate_Step3B_Q_TextBox.Enabled = true;
                            Estimate_Step3B_MinSecondary_TextBox.Enabled = true;
                            Estimate_Step3B_MaxSecondary_TextBox.Enabled = true;
                            Estimate_Step3B_Ma_TextBox.Enabled = false;
                            Estimate_Step3B_MinNumber_TextBox.Enabled = true;
                            Estimate_Step3B_MaxNumber_TextBox.Enabled = true;
                            Estimate_Step3B_MinFrequency_TextBox.Enabled = true;
                            Estimate_Step3B_MaxFrequency_TextBox.Enabled = true;

                            Estimate_Step3B_Vinmin_TextBox.Text = "";
                            Estimate_Step3B_Vinmax_TextBox.Text = "";
                            Estimate_Step3B_Vin_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Label.Text = "输出电压";
                            Estimate_Step3B_Vo_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Unit_Label.Text = "V";
                            Estimate_Step3B_Q_TextBox.Text = "1";
                            Estimate_Step3B_MinSecondary_TextBox.Text = "1";
                            Estimate_Step3B_MaxSecondary_TextBox.Text = "1";
                            Estimate_Step3B_Ma_TextBox.Text = "";
                            Estimate_Step3B_MinNumber_TextBox.Text = "1";
                            Estimate_Step3B_MaxNumber_TextBox.Text = "40";
                            Estimate_Step3B_Frequency_Label.Text = "谐振频率";
                            Estimate_Step3B_MinFrequency_TextBox.Text = "1";
                            Estimate_Step3B_MaxFrequency_TextBox.Text = "100";

                            isolatedDCDC_topologyRange = isolatedDCDC_topologyList.ToArray();
                            break;
                        case "隔离DC/DC变换单元_两级":
                            Estimate_Step3B_Vinmin_TextBox.Enabled = true;
                            Estimate_Step3B_Vinmax_TextBox.Enabled = true;
                            Estimate_Step3B_Vin_TextBox.Enabled = false;
                            Estimate_Step3B_Q_TextBox.Enabled = true;
                            Estimate_Step3B_MinSecondary_TextBox.Enabled = false;
                            Estimate_Step3B_MaxSecondary_TextBox.Enabled = false;
                            Estimate_Step3B_Ma_TextBox.Enabled = false;
                            Estimate_Step3B_MinNumber_TextBox.Enabled = false;
                            Estimate_Step3B_MaxNumber_TextBox.Enabled = false;
                            Estimate_Step3B_MinFrequency_TextBox.Enabled = false;
                            Estimate_Step3B_MaxFrequency_TextBox.Enabled = false;

                            Estimate_Step3B_Vinmin_TextBox.Text = "860";
                            Estimate_Step3B_Vinmax_TextBox.Text = "1300";
                            Estimate_Step3B_Vin_TextBox.Text = "";
                            Estimate_Step3B_Vo_Label.Text = "输出电压";
                            Estimate_Step3B_Vo_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Unit_Label.Text = "V";
                            Estimate_Step3B_Q_TextBox.Text = "1";
                            Estimate_Step3B_MinSecondary_TextBox.Text = "1";
                            Estimate_Step3B_MaxSecondary_TextBox.Text = "1";
                            Estimate_Step3B_Ma_TextBox.Text = "";
                            Estimate_Step3B_MinNumber_TextBox.Text = "20";
                            Estimate_Step3B_MaxNumber_TextBox.Text = "20";
                            Estimate_Step3B_Frequency_Label.Text = "谐振频率";
                            Estimate_Step3B_MinFrequency_TextBox.Text = "25";
                            Estimate_Step3B_MaxFrequency_TextBox.Text = "25";

                            isolatedDCDC_topologyRange = isolatedDCDC_topologyList.ToArray();
                            break;
                        case "逆变单元":
                            Estimate_Step3B_Vinmin_TextBox.Enabled = false;
                            Estimate_Step3B_Vinmax_TextBox.Enabled = false;
                            Estimate_Step3B_Vin_TextBox.Enabled = false;
                            Estimate_Step3B_Q_TextBox.Enabled = false;
                            Estimate_Step3B_MinSecondary_TextBox.Enabled = false;
                            Estimate_Step3B_MaxSecondary_TextBox.Enabled = false;
                            Estimate_Step3B_Ma_TextBox.Enabled = true;
                            Estimate_Step3B_MinNumber_TextBox.Enabled = true;
                            Estimate_Step3B_MaxNumber_TextBox.Enabled = true;
                            Estimate_Step3B_MinFrequency_TextBox.Enabled = true;
                            Estimate_Step3B_MaxFrequency_TextBox.Enabled = true;

                            Estimate_Step3B_Vinmin_TextBox.Text = "";
                            Estimate_Step3B_Vinmax_TextBox.Text = "";
                            Estimate_Step3B_Vin_TextBox.Text = "";
                            Estimate_Step3B_Vo_Label.Text = "并网电压";
                            Estimate_Step3B_Vo_TextBox.Text = "35";
                            Estimate_Step3B_Vo_Unit_Label.Text = "kV";
                            Estimate_Step3B_Q_TextBox.Text = "";
                            Estimate_Step3B_MinSecondary_TextBox.Text = "";
                            Estimate_Step3B_MaxSecondary_TextBox.Text = "";
                            Estimate_Step3B_Ma_TextBox.Text = "0.8";
                            Estimate_Step3B_MinNumber_TextBox.Text = "1";
                            Estimate_Step3B_MaxNumber_TextBox.Text = "40";
                            Estimate_Step3B_Frequency_Label.Text = "开关频率";
                            Estimate_Step3B_MinFrequency_TextBox.Text = "10";
                            Estimate_Step3B_MaxFrequency_TextBox.Text = "10";

                            DCAC_topologyRange = DCAC_topologyList.ToArray();
                            break;
                    }
                    ChangePanel(2, Estimate_Step3B_Panel);
                }
            }
        }

        private void Estimate_Step3_Prev_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(2, Estimate_Step2_Panel);
        }

        private void Estimate_Step3B_Prev_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(2, Estimate_Step2_Panel);
        }

        private void Estimate_Step3_Next_Button_Click(object sender, EventArgs e)
        {
            //开关器件
            //检查所有厂家
            List<string> manufacturerList = new List<string>();
            foreach (Data.Semiconductor semiconductor in Data.SemiconductorList)
            {
                if (!manufacturerList.Contains(semiconductor.Manufacturer))
                {
                    manufacturerList.Add(semiconductor.Manufacturer);
                }
            }
            foreach (string manufacturer in manufacturerList)
            {
                Estimate_Step4_Semiconductor_FlowLayoutPanel.Controls.Add(Estimate_Step4_CreateLabel(manufacturer + ":"));
                foreach (Data.Semiconductor semiconductor in Data.SemiconductorList)
                {
                    if (semiconductor.Manufacturer.Equals(manufacturer))
                    {
                        Estimate_Step4_Semiconductor_FlowLayoutPanel.Controls.Add(Estimate_Step4_CreateCheckBox(semiconductor.Type));
                    }
                }
            }

            //磁芯
            //检查所有厂家
            manufacturerList = new List<string>();
            foreach (Data.Core core in Data.CoreList)
            {
                if (!manufacturerList.Contains(core.Manufacturer))
                {
                    manufacturerList.Add(core.Manufacturer);
                }
            }
            foreach (string manufacturer in manufacturerList)
            {
                Estimate_Step4_Core_FlowLayoutPanel.Controls.Add(Estimate_Step4_CreateLabel(manufacturer + ":"));
                foreach (Data.Core core in Data.CoreList)
                {
                    if (core.Manufacturer.Equals(manufacturer))
                    {
                        Estimate_Step4_Core_FlowLayoutPanel.Controls.Add(Estimate_Step4_CreateCheckBox(core.Type));
                    }
                }
            }

            //绕线
            //检查所有类型
            List<string> categoryList = new List<string>();
            foreach (Data.Wire wire in Data.WireList)
            {
                if (!categoryList.Contains(wire.Category))
                {
                    categoryList.Add(wire.Category);
                }
            }
            foreach (string category in categoryList)
            {
                Estimate_Step4_Wire_FlowLayoutPanel.Controls.Add(Estimate_Step4_CreateLabel(category + ":"));
                foreach (Data.Wire wire in Data.WireList)
                {
                    if (wire.Category.Equals(category))
                    {
                        Estimate_Step4_Wire_FlowLayoutPanel.Controls.Add(Estimate_Step4_CreateCheckBox(wire.Type));
                    }
                }
            }

            //电容
            //检查所有类型
            categoryList = new List<string>();
            foreach (Data.Capacitor capacitor in Data.CapacitorList)
            {
                if (!categoryList.Contains(capacitor.Category))
                {
                    categoryList.Add(capacitor.Category);
                }
            }
            foreach (string category in categoryList)
            {
                Estimate_Step4_Capacitor_FlowLayoutPanel.Controls.Add(Estimate_Step4_CreateLabel(category + ":"));
                foreach (Data.Capacitor capacitor in Data.CapacitorList)
                {
                    if (capacitor.Category.Equals(category))
                    {
                        Estimate_Step4_Capacitor_FlowLayoutPanel.Controls.Add(Estimate_Step4_CreateCheckBox(capacitor.Type));
                    }
                }
            }

            ChangePanel(2, Estimate_Step4_Panel);
        }

        private void Estimate_Step3B_Next_Button_Click(object sender, EventArgs e)
        {
            Estimate_Step3_Next_Button_Click(sender, e);
        }

        private void Estimate_Step4_Prev_Button_Click(object sender, EventArgs e)
        {
            if (selectedStructure != null)
            {
                ChangePanel(2, Estimate_Step3_Panel);
            }
            else
            {
                ChangePanel(2, Estimate_Step3B_Panel);
            }
        }

        private void Estimate_Step4_Next_Button_Click(object sender, EventArgs e)
        {
            //按钮状态设置
            Estimate_Result_End_Button.Visible = true;
            Estimate_Result_End_Button.Enabled = true;
            Estimate_Result_Restart_Button.Visible = false;
            Estimate_Result_Restart_Button.Enabled = false;
            Estimate_Result_QuickSave_Button.Enabled = false;
            Estimate_Result_Save_Button.Enabled = false;
            Estimate_Result_Display_Button.Enabled = false;

            //切换显示
            Estimate_Result_Print_RichTextBox.Text = "";
            ChangePanel(2, Estimate_Result_Panel);
            evaluationThread = new Thread(new ThreadStart(Estimate_Result_Evaluate))
            {
                IsBackground = true
            };
            evaluationThread.Start();
        }

        private void Estimate_Result_End_Button_Click(object sender, EventArgs e)
        {
            //按钮状态设置
            Estimate_Result_End_Button.Visible = false;
            Estimate_Result_End_Button.Enabled = false;
            Estimate_Result_Restart_Button.Visible = true;
            Estimate_Result_Restart_Button.Enabled = true;
            Estimate_Result_QuickSave_Button.Enabled = false;
            Estimate_Result_Save_Button.Enabled = false;
            Estimate_Result_Display_Button.Enabled = false;

            PrintMsg("评估被用户终止！");

            //结束线程
            evaluationThread.Abort();
        }

        private void Estimate_Result_Restart_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(2, Estimate_Ready_Panel);
        }

        private void Estimate_Result_QuickSave_Button_Click(object sender, EventArgs e)
        {
            if (selectedStructure != null)
            {
                structure.Save();
            }
            else
            {
                converter.Save();
            }
        }

        private void Estimate_Result_Save_Button_Click(object sender, EventArgs e)
        {
            string path;
            string name;

            //string localFilePath, fileNameExt, newFileName, FilePath; 
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel表格(*.xlsx)|*.xlsx", //设置文件类型
                InitialDirectory = Data.ResultPath
            };

            //点了保存按钮进入 
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName.ToString(); //获得文件路径 
                name = filePath.Substring(filePath.LastIndexOf("\\") + 1, filePath.LastIndexOf(".xlsx") - (filePath.LastIndexOf("\\") + 1)); //获取文件名，不带路径
                path = filePath.Substring(0, filePath.LastIndexOf("\\")) + "\\"; //获取文件路径，不带文件名
                if (selectedStructure != null)
                {
                    structure.Save(path, name);
                }
                else
                {
                    converter.Save(path, name);
                }
            }
        }

        private void Estimate_Result_Display_Button_Click(object sender, EventArgs e)
        {
            //更新控件、图像
            if (Display_Show_GraphCategory_ComboBox.SelectedIndex == 0)
            {
                Display_Show_Display();
            }
            else
            {
                Display_Show_GraphCategory_ComboBox.SelectedIndex = 0;
            }

            //页面切换
            ChangePanel(3, Display_Show_Panel);
        }

        private void Display_Show_Restart_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(3, Display_Ready_Panel);
        }

        private void Display_Ready_Load_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog //打开文件窗口
            {
                Filter = "Excel表格|*.xls;*.xlsx", //设定打开的文件类型
                InitialDirectory = Data.ResultPath
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK) //如果选定了文件
            {
                //读取数据
                string filePath = openFileDialog.FileName; //取得文件路径及文件名
                string[][] info = Data.Load(filePath); //读取数据
                string[] conditions = info[0];
                string obj = conditions[0];
                switch (obj)
                {
                    case "DCDCConverter":
                        selectedConverter = "前级DC/DC变换单元_三级";
                        selectedStructure = null;
                        double Psys = double.Parse(conditions[1]);
                        double Vin_min = double.Parse(conditions[2]);
                        double Vin_max = double.Parse(conditions[3]);
                        double Vo = double.Parse(conditions[4]);
                        converter = new DCDCConverter(Psys, Vin_min, Vin_max, Vo);
                        break;

                    case "IsolatedDCDCConverter":
                        selectedConverter = "隔离DC/DC变换单元_三级";
                        selectedStructure = null;
                        Formula.Init();
                        Psys = double.Parse(conditions[1]);
                        double Vin = double.Parse(conditions[2]);
                        Vo = double.Parse(conditions[3]);
                        double Q = double.Parse(conditions[4]);
                        converter = new IsolatedDCDCConverter(Psys, Vin, Vo, Q);
                        break;

                    case "IsolatedDCDCConverter_TwoStage":
                        selectedConverter = "隔离DC/DC变换单元_两级";
                        selectedStructure = null;
                        Formula.Init();
                        Psys = double.Parse(conditions[1]);
                        Vin_min = double.Parse(conditions[2]);
                        Vin_max = double.Parse(conditions[3]);
                        Vo = double.Parse(conditions[4]);
                        Q = double.Parse(conditions[5]);
                        converter = new IsolatedDCDCConverter(Psys, Vin_min, Vin_max, Vo, Q);
                        break;

                    case "DCACConverter":
                        selectedConverter = "逆变单元";
                        selectedStructure = null;
                        Psys = double.Parse(conditions[1]);
                        double Vg = double.Parse(conditions[2]);
                        double fg = double.Parse(conditions[3]);
                        double phi = double.Parse(conditions[4]);
                        converter = new DCACConverter(Psys, Vg, fg, phi);
                        break;

                    case "ThreeLevelStructure":
                        selectedConverter = null;
                        selectedStructure = "三级架构";
                        Formula.Init();
                        Psys = double.Parse(conditions[1]);
                        double Vpv_min = double.Parse(conditions[2]);
                        double Vpv_max = double.Parse(conditions[3]);
                        Vg = double.Parse(conditions[4]);
                        fg = double.Parse(conditions[5]);
                        Q = double.Parse(conditions[6]);
                        double Ma = double.Parse(conditions[7]);
                        phi = double.Parse(conditions[8]);
                        structure = new ThreeLevelStructure()
                        {
                            Math_Psys = Psys,
                            Math_Vpv_min = Vpv_min,
                            Math_Vpv_max = Vpv_max,
                            Math_Vg = Vg,
                            Math_Vo = Vg / Math.Sqrt(3),
                            Math_fg = fg,
                            IsolatedDCDC_Q = Q,
                            DCAC_Ma = Ma,
                            DCAC_phi = phi,
                        };
                        break;

                    case "TwoLevelStructure":
                        selectedConverter = null;
                        selectedStructure = "两级架构";
                        Formula.Init();
                        Psys = double.Parse(conditions[1]);
                        Vpv_min = double.Parse(conditions[2]);
                        Vpv_max = double.Parse(conditions[3]);
                        Vg = double.Parse(conditions[4]);
                        fg = double.Parse(conditions[5]);
                        Q = double.Parse(conditions[6]);
                        double DCAC_Vin_def = double.Parse(conditions[7]);
                        Ma = double.Parse(conditions[8]);
                        phi = double.Parse(conditions[9]);
                        structure = new TwoLevelStructure()
                        {
                            Math_Psys = Psys,
                            Math_Vpv_min = Vpv_min,
                            Math_Vpv_max = Vpv_max,
                            Math_Vg = Vg,
                            Math_Vo = Vg / Math.Sqrt(3),
                            Math_fg = fg,
                            IsolatedDCDC_Q = Q,
                            DCAC_Vin_def = DCAC_Vin_def,
                            DCAC_Ma = Ma,
                            DCAC_phi = phi,
                        };
                        break;
                }
                if (selectedStructure != null)
                {
                    for (int i = 1; i < info.Length; i++) //i=0为标题行
                    {
                        double efficiency = double.Parse(info[i][0]);
                        double volume = double.Parse(info[i][1]);
                        double cost = double.Parse(info[i][2]);
                        structure.AllDesignList.Add(efficiency, volume, cost, info[i]);
                    }
                }
                else
                {
                    for (int i = 1; i < info.Length; i++) //i=0为标题行
                    {
                        double efficiency = double.Parse(info[i][0]);
                        double volume = double.Parse(info[i][1]);
                        double cost = double.Parse(info[i][2]);
                        converter.AllDesignList.Add(efficiency, volume, cost, info[i]);
                    }
                }

                //更新控件、图像
                if (Display_Show_GraphCategory_ComboBox.SelectedIndex == 0)
                {
                    Display_Show_Display();
                }
                else
                {
                    Display_Show_GraphCategory_ComboBox.SelectedIndex = 0;
                }

                //页面切换
                ChangePanel(3, Display_Show_Panel);
            }
        }

        private void Display_Show_Detail_Button_Click(object sender, EventArgs e)
        {
            Display_Detail_TabControl.Controls.Clear(); //清除所有控件以隐藏

            if (selectedStructure != null)
            {
                //生成数据
                structure.Evaluate();
                double[,] systemData = new double[div, 2]; //记录负载-效率曲线数据
                double[,] DCDCData = new double[div, 2];
                double[,] isolatedDCDCData = new double[div, 2];
                double[,] DCACData = new double[div, 2];
                for (int i = 1; i <= div; i++)
                {
                    structure.Operate(1.0 * i / div, structure.Math_Vpv_min);
                    //记录负载-效率曲线数据
                    systemData[i - 1, 0] = 100 * i / div; //负载点(%)
                    systemData[i - 1, 1] = structure.Efficiency * 100; //整体架构效率(%)
                    switch (selectedStructure)
                    {
                        case "三级架构":
                            DCDCData[i - 1, 0] = 100 * i / div; //负载点(%)
                            DCDCData[i - 1, 1] = ((ThreeLevelStructure)structure).DCDC.Efficiency * 100; //前级DCDC效率(%)
                            isolatedDCDCData[i - 1, 0] = 100 * i / div; //负载点(%)
                            isolatedDCDCData[i - 1, 1] = ((ThreeLevelStructure)structure).IsolatedDCDC.Efficiency * 100; //隔离DCDC效率(%)
                            DCACData[i - 1, 0] = 100 * i / div; //负载点(%)
                            DCACData[i - 1, 1] = ((ThreeLevelStructure)structure).DCAC.Efficiency * 100; //逆变效率(%)
                            break;

                        case "两级架构":
                            isolatedDCDCData[i - 1, 0] = 100 * i / div; //负载点(%)
                            isolatedDCDCData[i - 1, 1] = ((TwoLevelStructure)structure).IsolatedDCDC.Efficiency * 100; //隔离DCDC效率(%)
                            DCACData[i - 1, 0] = 100 * i / div; //负载点(%)
                            DCACData[i - 1, 1] = ((TwoLevelStructure)structure).DCAC.Efficiency * 100; //逆变效率(%)
                            break;
                    }
                }

                //更新显示
                Display_Show_Detail_System_Display(systemData);
                switch (selectedStructure)
                {
                    case "三级架构":
                        converter = ((ThreeLevelStructure)structure).DCDC;
                        Display_Show_Detail_DCDC_Display(DCDCData);
                        converter = ((ThreeLevelStructure)structure).IsolatedDCDC;
                        Display_Show_Detail_IsolatedDCDC_Display(isolatedDCDCData);
                        converter = ((ThreeLevelStructure)structure).DCAC;
                        Display_Show_Detail_DCAC_Display(DCACData);
                        converter = null;
                        break;

                    case "两级架构":
                        converter = ((TwoLevelStructure)structure).IsolatedDCDC;
                        Display_Show_Detail_IsolatedDCDC_Display(isolatedDCDCData);
                        converter = ((TwoLevelStructure)structure).DCAC;
                        Display_Show_Detail_DCAC_Display(DCACData);
                        converter = null;
                        break;
                }
            }
            else
            {
                //生成数据
                converter.Evaluate();
                double[,] data = new double[div, 2]; //记录负载-效率曲线数据
                for (int i = 1; i <= div; i++)
                {
                    switch (selectedConverter)
                    {
                        case "前级DC/DC变换单元_三级":
                            converter.Operate(1.0 * i / div, ((DCDCConverter)converter).Math_Vin_min);
                            break;
                        case "隔离DC/DC变换单元_三级":
                            converter.Operate(1.0 * i / div, ((IsolatedDCDCConverter)converter).Math_Vin);
                            break;
                        case "隔离DC/DC变换单元_两级":
                            converter.Operate(1.0 * i / div, ((IsolatedDCDCConverter)converter).Math_Vin_min);
                            break;
                        case "逆变单元":
                            converter.Operate(1.0 * i / div, ((DCACConverter)converter).Math_Vin_def);
                            break;
                    }
                    //记录负载-效率曲线数据
                    data[i - 1, 0] = 100 * i / div; //负载点(%)
                    data[i - 1, 1] = converter.Efficiency * 100; //整体架构效率(%)
                    //Console.WriteLine(data[i - 1, 1]);
                }

                //更新显示                
                switch (selectedConverter)
                {
                    case "前级DC/DC变换单元_三级":
                        Display_Show_Detail_DCDC_Display(data);
                        break;
                    case "隔离DC/DC变换单元_三级":
                    case "隔离DC/DC变换单元_两级":
                        Display_Show_Detail_IsolatedDCDC_Display(data);
                        break;
                    case "逆变单元":
                        Display_Show_Detail_DCAC_Display(data);
                        break;
                }
            }

            //损耗分布图像
            Display_Detail_Load_TrackBar.Value = 100;
            Display_Detail_Load_Value_Label.Text = Display_Detail_Load_TrackBar.Value.ToString() + "%";
            if (selectedStructure != null)
            {
                Display_Detail_Vin_TrackBar.Visible = true;
                Display_Detail_Vin_TrackBar.Minimum = (int)structure.Math_Vpv_min;
                Display_Detail_Vin_TrackBar.Maximum = (int)structure.Math_Vpv_max;
                Display_Detail_Vin_TrackBar.Value = (int)structure.Math_Vpv_min;
            }
            else
            {
                switch (selectedConverter)
                {
                    case "前级DC/DC变换单元_三级":
                        Display_Detail_Vin_TrackBar.Visible = true;
                        Display_Detail_Vin_Label.Visible = true;
                        Display_Detail_Vin_Value_Label.Visible = true;
                        Display_Detail_Vin_TrackBar.Minimum = (int)((DCDCConverter)converter).Math_Vin_min;
                        Display_Detail_Vin_TrackBar.Maximum = (int)((DCDCConverter)converter).Math_Vin_max;
                        Display_Detail_Vin_TrackBar.Value = (int)((DCDCConverter)converter).Math_Vin_min;
                        Display_Detail_Vin_Value_Label.Text = Display_Detail_Vin_TrackBar.Value.ToString() + "V";
                        break;
                    case "隔离DC/DC变换单元_三级":
                        Display_Detail_Vin_TrackBar.Visible = false;
                        Display_Detail_Vin_Label.Visible = false;
                        Display_Detail_Vin_Value_Label.Visible = false;
                        break;
                    case "隔离DC/DC变换单元_两级":
                        Display_Detail_Vin_TrackBar.Visible = true;
                        Display_Detail_Vin_Label.Visible = true;
                        Display_Detail_Vin_Value_Label.Visible = true;
                        Display_Detail_Vin_TrackBar.Minimum = (int)((IsolatedDCDCConverter)converter).Math_Vin_min;
                        Display_Detail_Vin_TrackBar.Maximum = (int)((IsolatedDCDCConverter)converter).Math_Vin_max;
                        Display_Detail_Vin_TrackBar.Value = (int)((IsolatedDCDCConverter)converter).Math_Vin_min;
                        Display_Detail_Vin_Value_Label.Text = Display_Detail_Vin_TrackBar.Value.ToString() + "V";
                        break;
                    case "逆变单元":
                        Display_Detail_Vin_TrackBar.Visible = false;
                        Display_Detail_Vin_Label.Visible = false;
                        Display_Detail_Vin_Value_Label.Visible = false;
                        break;
                }
            }
            Display_Show_Detail_DisplayLossBreakdown();

            ChangePanel(3, Display_Detail_Panel);
        }

        private void Display_Detail_Load_TrackBar_Scroll(object sender, EventArgs e)
        {
            Display_Detail_Load_Value_Label.Text = Display_Detail_Load_TrackBar.Value.ToString() + "%";
            Display_Show_Detail_DisplayLossBreakdown();
        }

        private void Display_Detail_Vin_TrackBar_Scroll(object sender, EventArgs e)
        {
            Display_Detail_Vin_Value_Label.Text = Display_Detail_Vin_TrackBar.Value.ToString() + "V";
            Display_Show_Detail_DisplayLossBreakdown();
        }

        private void Display_Detail_Back_Button_Click(object sender, EventArgs e)
        {
            //Display_Detail_Main_Panel.Controls.Clear();
            //labelList.Clear();

            ChangePanel(3, Display_Show_Panel);
        }

        private void Estimate_Step1_CheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Estimate_Step1_Item1_CheckBox.Checked = Estimate_Step1_CheckedListBox.GetItemChecked(0);
            Estimate_Step1_Item2_CheckBox.Checked = Estimate_Step1_CheckedListBox.GetItemChecked(1);
        }

        private void Estimate_Step1_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step1_CheckedListBox.SetItemChecked(0, Estimate_Step1_Item1_CheckBox.Checked);
        }

        private void Estimate_Step1_Item2_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step1_CheckedListBox.SetItemChecked(1, Estimate_Step1_Item2_CheckBox.Checked);
        }

        private void Estimate_Step1B_CheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Estimate_Step1B_Item1_CheckBox.Checked = Estimate_Step1B_CheckedListBox.GetItemChecked(0);
            Estimate_Step1B_Item2_CheckBox.Checked = Estimate_Step1B_CheckedListBox.GetItemChecked(1);
            Estimate_Step1B_Item3_CheckBox.Checked = Estimate_Step1B_CheckedListBox.GetItemChecked(2);
            Estimate_Step1B_Item4_CheckBox.Checked = Estimate_Step1B_CheckedListBox.GetItemChecked(3);
        }

        private void Estimate_Step1B_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step1B_CheckedListBox.SetItemChecked(0, Estimate_Step1B_Item1_CheckBox.Checked);
        }

        private void Estimate_Step1B_Item2_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step1B_CheckedListBox.SetItemChecked(1, Estimate_Step1B_Item2_CheckBox.Checked);
        }

        private void Estimate_Step1B_Item3_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step1B_CheckedListBox.SetItemChecked(2, Estimate_Step1B_Item3_CheckBox.Checked);
        }

        private void Estimate_Step1B_Item4_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step1B_CheckedListBox.SetItemChecked(3, Estimate_Step1B_Item4_CheckBox.Checked);
        }

        private void Estimate_Step2_Group1_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = Estimate_Step2_Group1_Item1_CheckBox.Checked;
        }

        private void Estimate_Step2_Group1_Item2_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = Estimate_Step2_Group1_Item2_CheckBox.Checked;
        }

        private void Estimate_Step2_Group1_Item3_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = Estimate_Step2_Group1_Item3_CheckBox.Checked;
        }

        private void Estimate_Step2_Group2_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = Estimate_Step2_Group2_Item1_CheckBox.Checked;
        }

        private void Estimate_Step2_Group2_Item2_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = Estimate_Step2_Group2_Item2_CheckBox.Checked;
        }

        private void Estimate_Step2_Group2_Item3_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = Estimate_Step2_Group2_Item3_CheckBox.Checked;
        }

        private void Estimate_Step2_Group2_Item4_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group2_Item4_Left_CheckBox.Checked = Estimate_Step2_Group2_Item4_CheckBox.Checked;
        }

        private void Estimate_Step2_Group3_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = Estimate_Step2_Group3_Item1_CheckBox.Checked;
        }

        private void Estimate_Step2_Group1_Item1_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group1_Item1_CheckBox.Checked = Estimate_Step2_Group1_Item1_Left_CheckBox.Checked;
        }

        private void Estimate_Step2_Group1_Item2_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group1_Item2_CheckBox.Checked = Estimate_Step2_Group1_Item2_Left_CheckBox.Checked;
        }

        private void Estimate_Step2_Group1_Item3_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group1_Item3_CheckBox.Checked = Estimate_Step2_Group1_Item3_Left_CheckBox.Checked;
        }

        private void Estimate_Step2_Group2_Item1_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group2_Item1_CheckBox.Checked = Estimate_Step2_Group2_Item1_Left_CheckBox.Checked;
        }

        private void Estimate_Step2_Group2_Item2_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group2_Item2_CheckBox.Checked = Estimate_Step2_Group2_Item2_Left_CheckBox.Checked;
        }

        private void Estimate_Step2_Group2_Item3_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group2_Item3_CheckBox.Checked = Estimate_Step2_Group2_Item3_Left_CheckBox.Checked;
        }

        private void Estimate_Step2_Group2_Item4_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group2_Item4_CheckBox.Checked = Estimate_Step2_Group2_Item4_Left_CheckBox.Checked;
        }

        private void Estimate_Step2_Group3_Item1_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group3_Item1_CheckBox.Checked = Estimate_Step2_Group3_Item1_Left_CheckBox.Checked;
        }

        private void Estimate_Step3_IsolatedDCDCMinSecondary_TextBox_TextChanged(object sender, EventArgs e)
        {
            Estimate_Step3_DCACMinNumber_TextBox.Text = (int.Parse(Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text) * int.Parse(Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.Text)).ToString();
        }

        private void Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox_TextChanged(object sender, EventArgs e)
        {
            Estimate_Step3_DCACMaxNumber_TextBox.Text = (int.Parse(Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text) * int.Parse(Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.Text)).ToString();
        }

        private void Estimate_Step3_IsolatedDCDCMinNumber_TextBox_TextChanged(object sender, EventArgs e)
        {
            Estimate_Step3_DCACMinNumber_TextBox.Text = (int.Parse(Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text) * int.Parse(Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.Text)).ToString();
        }

        private void Estimate_Step3_IsolatedDCDCMaxNumber_TextBox_TextChanged(object sender, EventArgs e)
        {
            Estimate_Step3_DCACMaxNumber_TextBox.Text = (int.Parse(Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text) * int.Parse(Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.Text)).ToString();
        }

        private void Display_Show_GraphCategory_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Display_Show_Display();
        }
    }
}
