using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PV_analysis
{
    internal partial class MainForm : System.Windows.Forms.Form
    {
        private Panel[] panelNow = new Panel[5];
        private System.Drawing.Color activeColor;
        private System.Drawing.Color inactiveColor;
        private ResultList resultList = new ResultList();
        private int nowResult = -1;
        private List<Label> labelList = new List<Label>();

        private string selectedSystem; //所要评估的系统，三级架构或两级架构

        private double Psys = 6e6; //架构总功率
        private double Vpv_min = 860; //光伏板输出电压最小值
        private double Vpv_max = 1300; //光伏板输出电压最大值
        private double Vg = 35e3; //并网电压（线电压）
        private double Vo = 35e3 / Math.Sqrt(3); //输出电压（并网相电压）
        private double fg = 50; //并网频率
        private double[] VbusRange = { 1300 }; //母线电压范围
        private double phi = 0; //功率因数角(rad)

        //前级DC/DC参数
        private int[] DCDC_numberRange = Function.GenerateNumberRange(1, 120); //可用模块数序列
        private string[] DCDC_topologyRange; //可用拓扑序列
        private double[] DCDC_frequencyRange = Function.GenerateFrequencyRange(1e3, 100e3); //可用开关频率序列

        //隔离DC/DC参数
        private double isolatedDCDC_Q = 1; //品质因数预设值
        private string[] isolatedDCDC_topologyRange; //可用拓扑序列
        private double[] isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(1e3, 100e3); //可用谐振频率序列

        //DC/AC参数
        private int[] DCAC_numberRange = Function.GenerateNumberRange(1, 40); //可用模块数序列，隔离DCDC与此同
        private string[] DCAC_topologyRange; //可用拓扑序列
        private string[] DCAC_modulationRange = { "PSPWM", "LSPWM" }; //可用调制方式序列
        private double[] DCAC_frequencyRange = Function.GenerateFrequencyRange(10e3, 10e3);


        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
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
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[1];
            panelNow[0].Visible = true;

            Tab_Home_Button.BackColor = activeColor;
            Tab_Estimate_Button.BackColor = inactiveColor;
            Tab_Display_Button.BackColor = inactiveColor;
            Tab_Admin_Button.BackColor = inactiveColor;
        }

        private void Tab_Estimate_Button_Click(object sender, EventArgs e)
        {
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;

            Tab_Home_Button.BackColor = inactiveColor;
            Tab_Estimate_Button.BackColor = activeColor;
            Tab_Display_Button.BackColor = inactiveColor;
            Tab_Admin_Button.BackColor = inactiveColor;
        }

        private void Tab_Display_Button_Click(object sender, EventArgs e)
        {
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;

            Tab_Home_Button.BackColor = inactiveColor;
            Tab_Estimate_Button.BackColor = inactiveColor;
            Tab_Display_Button.BackColor = activeColor;
            Tab_Admin_Button.BackColor = inactiveColor;
        }

        private void Tab_Admin_Button_Click(object sender, EventArgs e)
        {
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[4];
            panelNow[0].Visible = true;

            Tab_Home_Button.BackColor = inactiveColor;
            Tab_Estimate_Button.BackColor = inactiveColor;
            Tab_Display_Button.BackColor = inactiveColor;
            Tab_Admin_Button.BackColor = activeColor;
        }

        private void Estimate_Ready_Begin_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Step1_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step1_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Ready_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
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
                selectedSystem = Estimate_Step1_CheckedListBox.GetItemText(Estimate_Step1_CheckedListBox.CheckedItems[0]);
                switch (selectedSystem)
                {
                    case "三级架构":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = true;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = true;
                        break;
                    case "两级架构":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = true;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = true;
                        break;
                }
                panelNow[2] = Estimate_Step2_Panel;
                panelNow[0].Visible = false;
                panelNow[0] = panelNow[2];
                panelNow[0].Visible = true;
            }
        }

        private void Estimate_Step2_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Step1_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
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
                isolatedDCDC_topologyList.Add("DTCSRC");
            }

            List<string> DCAC_topologyList = new List<string>();
            if (Estimate_Step2_Group3_Item1_Left_CheckBox.Checked)
            {
                DCAC_topologyList.Add("CHB");
            }

            if (selectedSystem.Equals("三级架构") && DCDC_topologyList.Count == 0)
            {
                MessageBox.Show("请至少选择一项前级DC/DC拓扑");
            }
            else if (isolatedDCDC_topologyList.Count == 0)
            {
                MessageBox.Show("请至少选择一项隔离DC/DC拓扑");
            }
            else if (DCAC_topologyList.Count == 0)
            {
                MessageBox.Show("请至少选择一项逆变拓扑");
            }
            else
            {
                switch (selectedSystem)
                {
                    case "三级架构":
                        textBox10.Enabled = true;
                        textBox9.Enabled = true;
                        textBox8.Enabled = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = true;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = true;
                        break;
                    case "两级架构":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = true;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Checked = true;
                        break;
                }
                DCDC_topologyRange = DCDC_topologyList.ToArray();
                isolatedDCDC_topologyRange = isolatedDCDC_topologyList.ToArray();
                DCAC_topologyRange = DCAC_topologyList.ToArray();
                panelNow[2] = Estimate_Step3_Panel;
                panelNow[0].Visible = false;
                panelNow[0] = panelNow[2];
                panelNow[0].Visible = true;
            }
        }

        private void Estimate_Step3_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Step2_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step3_Next_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Step4_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step4_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Step3_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step4_Next_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Step5_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step5_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Step4_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step5_Next_button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Result_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Result_Save_Button_Click(object sender, EventArgs e)
        {

        }

        private void Estimate_Result_Display_Button_Click(object sender, EventArgs e)
        {
            panelNow[3] = Display_Show_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;

            Tab_Estimate_Button.BackColor = inactiveColor;
            Tab_Display_Button.BackColor = activeColor;
        }

        private void Estimate_Result_Restart_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Ready_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Display_Ready_Load_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog //打开文件窗口
            {
                Filter = "Files|*.xls;*.xlsx", //设定打开的文件类型
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK) //如果选定了文件
            {
                string filePath = openFileDialog.FileName; //取得文件路径及文件名

                //读取数据
                IWorkbook workbook = WorkbookFactory.Create(filePath); //打开Excel
                ISheet sheet = workbook.GetSheetAt(1);
                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    string[] data = new string[row.LastCellNum + 1];
                    for (int j = 0; j < row.LastCellNum; j++)
                    {
                        data[j] = row.GetCell(j).StringCellValue;
                    }
                    resultList.efficiency.Add(Double.Parse(row.GetCell(79).StringCellValue));
                    resultList.cost.Add(Double.Parse(row.GetCell(80).StringCellValue));
                    resultList.volume.Add(Double.Parse(row.GetCell(81).StringCellValue));
                    resultList.data.Add(data);
                }

                //显示图像
                ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
                for (int i = 0; i < resultList.efficiency.Count; i++)
                {
                    values.Add(new ObservablePoint(resultList.cost[i], resultList.efficiency[i]));
                }
                cartesianChart1.Series.Add(new ScatterSeries
                {
                    Values = values
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

                cartesianChart1.AxisX.Add(new Axis
                {
                    Title = "成本（万元）"
                });

                cartesianChart1.AxisY.Add(new Axis
                {
                    Title = "中国效率（%）"
                });
                cartesianChart1.LegendLocation = LegendLocation.Right;
                cartesianChart1.DataClick += ChartOnDataClick; //添加点击图像点事件

                //显示默认值
                label107.Text = "";
                label106.Text = "";
                label104.Text = "";
                label98.Text = "";
                label97.Text = "";
                label96.Text = "";
                label90.Text = "";
                label92.Text = "";
                label87.Text = "";
                label86.Text = "";
                label95.Text = "";
                label94.Text = "";
                label93.Text = "";

                //切换到显示页面
                panelNow[3] = Display_Show_Panel;
                panelNow[0].Visible = false;
                panelNow[0] = panelNow[3];
                panelNow[0].Visible = true;
            }
        }

        private void ChartOnDataClick(object sender, ChartPoint chartPoint)
        {
            int n;
            //目前采用循环比较法查找 TODO 能否直接将chartPoint与点的具体信息相联系
            for (n = 0; n < resultList.data.Count; n++)
            {
                if (Function.EQ(chartPoint.X, resultList.cost[n]) && Function.EQ(chartPoint.Y, resultList.efficiency[n]))
                {
                    break;
                }
            }
            if (n < resultList.data.Count)
            {
                nowResult = n;
                label107.Text = resultList.data[n][0];
                label106.Text = resultList.data[n][1] + "万元";
                label104.Text = resultList.data[n][2] + "dm^3";
                label90.Text = "三级架构";
                label98.Text = resultList.data[n][8];
                label97.Text = resultList.data[n][10] + "kHz";
                label96.Text = resultList.data[n][9];
                label92.Text = resultList.data[n][31];
                label87.Text = resultList.data[n][33] + "kHz";
                label86.Text = resultList.data[n][32];
                label95.Text = resultList.data[n][64];
                label94.Text = resultList.data[n][67] + "kHz";
                label93.Text = resultList.data[n][66];
            }
        }

        private void Display_Show_Detail_Button_Click(object sender, EventArgs e)
        {
            if (nowResult >= 0 && nowResult < resultList.data.Count)
            {
                //int x = 100;
                //int y = 40;
                //int dx = 50;
                //int dy = 40;
                //labelList.Add(newLabel(new System.Drawing.Point(x, y), "测试"));
                //labelList.Add(newLabel(new System.Drawing.Point(x += dx, y), "测试"));
                //labelList.Add(newLabel(new System.Drawing.Point(x += dx, y), "测试2"));

                //for (int i = 0; i < labelList.Count; i++)
                //{
                //    Display_Detail_Main_Panel.Controls.Add(labelList[i]);
                //}

                int n = nowResult;
                label136.Text = resultList.data[n][0];
                label135.Text = resultList.data[n][1] + "万元";
                label134.Text = resultList.data[n][2] + "dm^3";
                label130.Text = "三级架构";
                label108.Text = resultList.data[n][8];
                label105.Text = resultList.data[n][10] + "kHz";
                label103.Text = resultList.data[n][9];
                label125.Text = resultList.data[n][31];
                label124.Text = resultList.data[n][33] + "kHz";
                label123.Text = resultList.data[n][32];
                label121.Text = resultList.data[n][64];
                label120.Text = resultList.data[n][67] + "kHz";
                label119.Text = resultList.data[n][66];

                //显示图像
                Func<ChartPoint, string> labelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);

                double p1 = Math.Round(100 - double.Parse(resultList.data[n][3].Substring(0, 5)), 2);
                double p2 = Math.Round(100 - double.Parse(resultList.data[n][26].Substring(0, 5)), 2);
                double p3 = Math.Round(100 - double.Parse(resultList.data[n][59].Substring(0, 5)), 2);
                pieChart1.Series = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "前级DC/DC",
                        Values = new ChartValues<double> {p1},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                    new PieSeries
                    {
                        Title = "隔离DC/DC",
                        Values = new ChartValues<double> {p2},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                    new PieSeries
                    {
                        Title = "逆变",
                        Values = new ChartValues<double> {p3},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                };
                pieChart1.LegendLocation = LegendLocation.Bottom;

                double c1 = Math.Round(double.Parse(resultList.data[n][4]) / 1e4, 2);
                double c2 = Math.Round(double.Parse(resultList.data[n][27]) / 1e4, 2);
                double c3 = Math.Round(double.Parse(resultList.data[n][60]) / 1e4, 2);
                pieChart2.Series = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "前级DC/DC",
                        Values = new ChartValues<double> {c1},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                    new PieSeries
                    {
                        Title = "隔离DC/DC",
                        Values = new ChartValues<double> {c2},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                    new PieSeries
                    {
                        Title = "逆变",
                        Values = new ChartValues<double> {c3},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                };
                pieChart2.LegendLocation = LegendLocation.Bottom;

                double v1 = Math.Round(double.Parse(resultList.data[n][5]), 2);
                double v2 = Math.Round(double.Parse(resultList.data[n][28]), 2);
                double v3 = Math.Round(double.Parse(resultList.data[n][61]), 2);
                pieChart3.Series = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "前级DC/DC",
                        Values = new ChartValues<double> {v1},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                    new PieSeries
                    {
                        Title = "隔离DC/DC",
                        Values = new ChartValues<double> {v2},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                    new PieSeries
                    {
                        Title = "逆变",
                        Values = new ChartValues<double> {v3},
                        DataLabels = true,
                        LabelPoint = labelPoint
                    },
                };
                pieChart3.LegendLocation = LegendLocation.Bottom;

                panelNow[3] = Display_Detail_Panel;
                panelNow[0].Visible = false;
                panelNow[0] = panelNow[3];
                panelNow[0].Visible = true;
            }
        }

        private Label newLabel(System.Drawing.Point point, string text)
        {
            Label label = new System.Windows.Forms.Label();
            label.AutoSize = true;
            label.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label.Location = point;
            label.Text = text;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            return label;
        }

        private void Display_Show_Restart_Button_Click(object sender, EventArgs e)
        {
            panelNow[3] = Display_Ready_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;
        }

        private void Display_Detail_Back_Button_Click(object sender, EventArgs e)
        {
            //Display_Detail_Main_Panel.Controls.Clear();
            //labelList.Clear();

            panelNow[3] = Display_Show_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;
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

        private void Estimate_Step2_Group3_Item1_Left_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Estimate_Step2_Group3_Item1_CheckBox.Checked = Estimate_Step2_Group3_Item1_Left_CheckBox.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void label107_Click(object sender, EventArgs e)
        {

        }

        private void label148_Click(object sender, EventArgs e)
        {

        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {
            textBox14.Text = textBox11.Text;
        }

        private void textBox14_TextChanged(object sender, EventArgs e)
        {
            textBox11.Text = textBox14.Text;
        }
    }

}
