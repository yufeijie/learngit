using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using NPOI.SS.UserModel;
using PV_analysis.Converters;
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

        private double Psys; //架构总功率
        private double Vpv_min; //光伏板MPPT电压最小值
        private double Vpv_max; //光伏板MPPT电压最大值
        private double Vpv_peak; //光伏板输出电压最大值
        private double Vg; //并网电压（线电压）
        private double Vo; //输出电压（并网相电压）
        private double fg = 50; //并网频率
        private double[] VbusRange = { 1300 }; //母线电压范围
        private double phi = 0; //功率因数角(rad)

        //前级DC/DC参数
        private int[] DCDC_numberRange; //可用模块数序列
        private string[] DCDC_topologyRange; //可用拓扑序列
        private double[] DCDC_frequencyRange; //可用开关频率序列

        //隔离DC/DC参数
        private double isolatedDCDC_Q = 1; //品质因数预设值
        private string[] isolatedDCDC_topologyRange; //可用拓扑序列
        private double[] isolatedDCDC_resonanceFrequencyRange; //可用谐振频率序列

        //DC/AC参数
        private int[] DCAC_numberRange; //可用模块数序列，隔离DCDC与此同
        private string[] DCAC_topologyRange; //可用拓扑序列
        private string[] DCAC_modulationRange = { "PSPWM", "LSPWM" }; //可用调制方式序列
        private double[] DCAC_frequencyRange;

        //设计结果
        private ConverterDesignList paretoDesignList;
        private ConverterDesignList allDesignList;
        private string[] conditionTitles;
        private string[] conditions;

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
                        textBox11.Enabled = true;
                        textBox7.Enabled = true;
                        textBox6.Enabled = true;
                        textBox14.Enabled = true;
                        textBox13.Enabled = true;
                        textBox12.Enabled = true;
                        textBox10.Text = "100";
                        textBox9.Text = "1";
                        textBox8.Text = "100";
                        textBox11.Text = "40";
                        textBox7.Text = "1";
                        textBox6.Text = "100";
                        textBox14.Text = "40";
                        textBox13.Text = "10";
                        textBox12.Text = "10";
                        break;
                    case "两级架构":
                        textBox10.Enabled = false;
                        textBox9.Enabled = false;
                        textBox8.Enabled = false;
                        textBox11.Enabled = false;
                        textBox7.Enabled = false;
                        textBox6.Enabled = false;
                        textBox14.Enabled = false;
                        textBox13.Enabled = false;
                        textBox12.Enabled = false;
                        textBox10.Text = "";
                        textBox9.Text = "";
                        textBox8.Text = "";
                        textBox11.Text = "20";
                        textBox7.Text = "25";
                        textBox6.Text = "25";
                        textBox14.Text = "20";
                        textBox13.Text = "25";
                        textBox12.Text = "25";
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
            //记录
            Psys = double.Parse(textBox1.Text) * 1e6;
            Vpv_min = double.Parse(textBox2.Text);
            Vpv_max = double.Parse(textBox3.Text);
            Vpv_peak = double.Parse(textBox4.Text);
            Vg = double.Parse(textBox5.Text) * 1e3;
            Vo = Vg / Math.Sqrt(3);
            switch (selectedSystem)
            {
                case "三级架构":
                    DCDC_numberRange = Function.GenerateNumberRange(1, int.Parse(textBox10.Text));
                    DCDC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(textBox9.Text) * 1e3, double.Parse(textBox8.Text) * 1e3);
                    isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(double.Parse(textBox7.Text) * 1e3, double.Parse(textBox6.Text) * 1e3);
                    DCAC_numberRange = Function.GenerateNumberRange(1, int.Parse(textBox14.Text));
                    DCAC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(textBox13.Text) * 1e3, double.Parse(textBox12.Text) * 1e3);
                    break;
                case "两级架构":
                    isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(double.Parse(textBox7.Text) * 1e3, double.Parse(textBox6.Text) * 1e3);
                    DCAC_numberRange = Function.GenerateNumberRange(int.Parse(textBox14.Text), int.Parse(textBox14.Text));
                    DCAC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(textBox13.Text) * 1e3, double.Parse(textBox12.Text) * 1e3);
                    break;
            }
            
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
                flowLayoutPanel1.Controls.Add(CreateLabel(manufacturer + ":"));
                foreach (Data.Semiconductor semiconductor in Data.SemiconductorList)
                {
                    if (semiconductor.Manufacturer.Equals(manufacturer))
                    {
                        flowLayoutPanel1.Controls.Add(CreateCheckBox(semiconductor.Type));
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
                flowLayoutPanel2.Controls.Add(CreateLabel(manufacturer + ":"));
                foreach (Data.Core core in Data.CoreList)
                {
                    if (core.Manufacturer.Equals(manufacturer))
                    {
                        flowLayoutPanel2.Controls.Add(CreateCheckBox(core.Type));
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
                flowLayoutPanel3.Controls.Add(CreateLabel(category + ":"));
                foreach (Data.Wire wire in Data.WireList)
                {
                    if (wire.Category.Equals(category))
                    {
                        flowLayoutPanel3.Controls.Add(CreateCheckBox(wire.Type));
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
                flowLayoutPanel4.Controls.Add(CreateLabel(category + ":"));
                foreach (Data.Capacitor capacitor in Data.CapacitorList)
                {
                    if (capacitor.Category.Equals(category))
                    {
                        flowLayoutPanel4.Controls.Add(CreateCheckBox(capacitor.Type));
                    }
                }
            }

            panelNow[2] = Estimate_Step4_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                Size = new System.Drawing.Size(1240, 30),
                TabIndex = 63,
                Text = text,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
        }

        private CheckBox CreateCheckBox(string text)
        {
            return new CheckBox
            {
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                Size = new System.Drawing.Size(150, 25),
                TabIndex = 40,
                Text = text,
                UseVisualStyleBackColor = true,
                Checked = true
            };
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
            //更新开关器件可用状态
            foreach (Control control in flowLayoutPanel1.Controls)
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
            foreach (Control control in flowLayoutPanel2.Controls)
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
            foreach (Control control in flowLayoutPanel3.Controls)
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
            foreach (Control control in flowLayoutPanel4.Controls)
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

            //更新信息
            label63.Text = textBox1.Text;
            label64.Text = textBox2.Text;
            label65.Text = textBox3.Text;
            label66.Text = textBox4.Text;
            label67.Text = textBox5.Text;
            label78.Text = selectedSystem;
            label68.Text = textBox10.Text;
            label73.Text = textBox9.Text;
            label72.Text = textBox8.Text;
            List<string> DCDC_topologyList = new List<string>();
            if (Estimate_Step2_Group1_Item1_Left_CheckBox.Checked)
            {
                DCDC_topologyList.Add("三电平Boost");
            }
            if (Estimate_Step2_Group1_Item2_Left_CheckBox.Checked)
            {
                DCDC_topologyList.Add("两电平Boost");
            }
            if (Estimate_Step2_Group1_Item3_Left_CheckBox.Checked)
            {
                DCDC_topologyList.Add("交错并联Boost");
            }
            label79.Text = Function.StringArrayToString(DCDC_topologyList.ToArray());
            label69.Text = textBox11.Text;
            label71.Text = textBox7.Text;
            label74.Text = textBox6.Text;
            List<string> isolatedDCDC_topologyList = new List<string>();
            if (Estimate_Step2_Group2_Item1_Left_CheckBox.Checked)
            {
                isolatedDCDC_topologyList.Add("SRC");
            }
            if (Estimate_Step2_Group2_Item2_Left_CheckBox.Checked)
            {
                isolatedDCDC_topologyList.Add("DTCSRC");
            }

            label81.Text = Function.StringArrayToString(isolatedDCDC_topologyList.ToArray());
            label70.Text = textBox14.Text;
            label75.Text = textBox13.Text;
            label76.Text = textBox12.Text;
            List<string> DCAC_topologyList = new List<string>();
            if (Estimate_Step2_Group3_Item1_Left_CheckBox.Checked)
            {
                DCAC_topologyList.Add("CHB");
            }
            label83.Text = Function.StringArrayToString(DCAC_topologyList.ToArray());

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
            label85.Text = "";

            panelNow[2] = Estimate_Result_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;

            switch (selectedSystem)
            {
                case "三级架构":
                    EvaluateThreeStageSystem();
                    break;
                case "两级架构":
                    EvaluateTwoStageSystem();
                    break;
            }
        }

        private void EvaluateThreeStageSystem()
        {
            WriteLine("Start...");
            WriteLine();

            Formula.Init();
            paretoDesignList = new ConverterDesignList();
            allDesignList = new ConverterDesignList { IsAll = true };

            //系统设计
            foreach (double Vbus in VbusRange) //母线电压变化
            {
                WriteLine("Now DC bus voltage = " + Vbus + ":");
                //前级DC/DC变换器设计
                WriteLine("-------------------------");
                WriteLine("Front-stage DC/DC converters design...");
                DCDCConverter DCDC = new DCDCConverter(Psys, Vpv_min, Vpv_max, Vbus);
                foreach (string tp in DCDC_topologyRange) //拓扑变化
                {
                    DCDC.CreateTopology(tp);
                    foreach (int n in DCDC_numberRange) //模块数变化
                    {
                        DCDC.Number = n;
                        foreach (double fs in DCDC_frequencyRange) //开关频率变化
                        {
                            DCDC.Math_fs = fs;
                            WriteLine("Now topology=" + tp + ", n=" + n + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                            DCDC.Design();
                        }
                    }
                }
                if (DCDC.AllDesignList.Size <= 0)
                {
                    continue;
                }
                foreach (int j in DCAC_numberRange) //目前只考虑一拖一
                {
                    //逆变器设计
                    WriteLine("-------------------------");
                    WriteLine("Inverters design...");
                    DCACConverter DCAC = new DCACConverter(Psys, Vo, fg, phi) { Number = j };
                    foreach (string tp in DCAC_topologyRange) //拓扑变化
                    {
                        DCAC.CreateTopology(tp);
                        foreach (string mo in DCAC_modulationRange) //拓扑变化
                        {
                            DCAC.Modulation = mo;
                            foreach (double fs in DCAC_frequencyRange) //谐振频率变化
                            {
                                DCAC.Math_fs = fs;
                                WriteLine("Now topology=" + tp + ", n=" + j + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                                DCAC.Math_Vin = 0;
                                //inverter.setVoltageInputDef(inv_voltageInput); //FIXME
                                DCAC.Design();
                            }
                        }
                    }
                    if (DCAC.AllDesignList.Size <= 0)
                    {
                        continue;
                    }

                    //隔离DC/DC变换器设计
                    WriteLine("-------------------------");
                    WriteLine("Isolated DC/DC converters design...");
                    IsolatedDCDCConverter isolatedDCDC = new IsolatedDCDCConverter(Psys, Vbus, DCAC.Math_Vin, isolatedDCDC_Q) { Number = j };
                    foreach (string tp in isolatedDCDC_topologyRange) //拓扑变化
                    {
                        isolatedDCDC.CreateTopology(tp);
                        foreach (double fr in isolatedDCDC_resonanceFrequencyRange) //谐振频率变化
                        {
                            isolatedDCDC.Math_fr = fr;
                            WriteLine("Now topology=" + tp + ", n=" + j + ", fs=" + string.Format("{0:N1}", fr / 1e3) + "kHz");
                            isolatedDCDC.Design();
                        }
                    }
                    if (isolatedDCDC.AllDesignList.Size <= 0)
                    {
                        continue;
                    }

                    //整合得到最终结果
                    WriteLine("-------------------------");
                    WriteLine("Inv num=" + j + ", DC bus voltage=" + Vbus + ", Combining...");
                    ConverterDesignList newDesignList = new ConverterDesignList();
                    newDesignList.Combine(DCDC.ParetoDesignList);
                    newDesignList.Combine(isolatedDCDC.ParetoDesignList);
                    newDesignList.Combine(DCAC.ParetoDesignList);
                    newDesignList.Transfer(new string[] { Vbus.ToString(), DCAC.Math_Vin.ToString() });
                    paretoDesignList.Merge(newDesignList); //记录Pareto最优设计
                    allDesignList.Merge(newDesignList); //记录所有设计
                }
                WriteLine("=========================");
            }

            conditionTitles = new string[]
            {
                "Total power",
                "PV min voltage",
                "PV max voltage",
                "Grid voltage",
                "Grid frequency(Hz)",
                "DC bus voltage range",
                "DCDC number range",
                "DCDC topology range",
                "DCDC frequency range(kHz)",
                "Isolated DCDC quality factor default",
                "Isolated DCDC topology range",
                "Isolated DCDC resonance frequency range(kHz)",
                "DCAC power factor angle(rad)",
                "DCAC number range",
                "DCAC topology range",
                "DCAC modulation range",
                "DCAC frequency range(kHz)"
            };

            conditions = new string[]
            {
                Psys.ToString(),
                Vpv_min.ToString(),
                Vpv_max.ToString(),
                Vg.ToString(),
                fg.ToString(),
                Function.DoubleArrayToString(VbusRange),
                Function.IntArrayToString(DCDC_numberRange),
                Function.StringArrayToString(DCDC_topologyRange),
                Function.DoubleArrayToString(DCDC_frequencyRange),
                isolatedDCDC_Q.ToString(),
                Function.StringArrayToString(isolatedDCDC_topologyRange),
                Function.DoubleArrayToString(isolatedDCDC_resonanceFrequencyRange),
                phi.ToString(),
                Function.IntArrayToString(DCAC_numberRange),
                Function.StringArrayToString(DCAC_topologyRange),
                Function.StringArrayToString(DCAC_modulationRange),
                Function.DoubleArrayToString(DCAC_frequencyRange)
            };
        }

        private void EvaluateTwoStageSystem()
        {
            WriteLine("Start...");
            WriteLine();

            Formula.Init();
            paretoDesignList = new ConverterDesignList();
            allDesignList = new ConverterDesignList { IsAll = true };

            //DC/AC参数
            double DCAC_Vin = 1300; //逆变器直流侧电压

            foreach (int j in DCAC_numberRange) //目前只考虑一拖一
            {
                //隔离DC/DC变换器设计
                WriteLine("-------------------------");
                WriteLine("Isolated DC/DC converters design...");
                IsolatedDCDCConverter isolatedDCDC = new IsolatedDCDCConverter(Psys, Vpv_min, Vpv_max, DCAC_Vin, isolatedDCDC_Q) { Number = j };
                foreach (string tp in isolatedDCDC_topologyRange) //拓扑变化
                {
                    isolatedDCDC.CreateTopology(tp);
                    foreach (double fr in isolatedDCDC_resonanceFrequencyRange) //谐振频率变化
                    {
                        isolatedDCDC.Math_fr = fr;
                        WriteLine("Now topology=" + tp + ", n=" + j + ", fs=" + string.Format("{0:N1}", fr / 1e3) + "kHz");
                        isolatedDCDC.Design();
                    }
                }
                if (isolatedDCDC.AllDesignList.Size <= 0)
                {
                    continue;
                }

                //逆变器设计
                WriteLine("-------------------------");
                WriteLine("Inverters design...");
                DCACConverter DCAC = new DCACConverter(Psys, Vo, fg, phi) { Number = j, Math_Vin = DCAC_Vin };
                foreach (string tp in DCAC_topologyRange) //拓扑变化
                {
                    DCAC.CreateTopology(tp);
                    foreach (string mo in DCAC_modulationRange) //拓扑变化
                    {
                        DCAC.Modulation = mo;
                        foreach (double fs in DCAC_frequencyRange) //谐振频率变化
                        {
                            DCAC.Math_fs = fs;
                            WriteLine("Now topology=" + tp + ", n=" + j + ", fs=" + string.Format("{0:N1}", fs / 1e3) + "kHz");
                            //inverter.setVoltageInputDef(inv_voltageInput); //FIXME
                            DCAC.Design();
                        }
                    }
                }
                if (DCAC.AllDesignList.Size <= 0)
                {
                    continue;
                }

                //整合得到最终结果
                WriteLine("-------------------------");
                WriteLine("Inv num=" + j + ", Combining...");
                ConverterDesignList newDesignList = new ConverterDesignList();
                newDesignList.Combine(isolatedDCDC.ParetoDesignList);
                newDesignList.Combine(DCAC.ParetoDesignList);
                newDesignList.Transfer(new string[] { });
                paretoDesignList.Merge(newDesignList); //记录Pareto最优设计
                allDesignList.Merge(newDesignList); //记录所有设计
            }

            conditionTitles = new string[]
            {
                "Total power",
                "PV min voltage",
                "PV max voltage",
                "Grid voltage",
                "Grid frequency(Hz)",
                "Isolated DCDC quality factor default",
                "Isolated DCDC topology range",
                "Isolated DCDC resonance frequency range(kHz)",
                "DCAC input voltage",
                "DCAC power factor angle(rad)",
                "DCAC number range",
                "DCAC topology range",
                "DCAC modulation range",
                "DCAC frequency range(kHz)"
            };

            conditions = new string[]
            {
                Psys.ToString(),
                Vpv_min.ToString(),
                Vpv_max.ToString(),
                Vg.ToString(),
                fg.ToString(),
                isolatedDCDC_Q.ToString(),
                Function.StringArrayToString(isolatedDCDC_topologyRange),
                Function.DoubleArrayToString(isolatedDCDC_resonanceFrequencyRange),
                DCAC_Vin.ToString(),
                phi.ToString(),
                Function.IntArrayToString(DCAC_numberRange),
                Function.StringArrayToString(DCAC_topologyRange),
                Function.StringArrayToString(DCAC_modulationRange),
                Function.DoubleArrayToString(DCAC_frequencyRange)
            };
        }

        private void WriteLine()
        {
            label85.Text += "\r\n";
        }

        private void WriteLine(string text)
        {
            label85.Text += "\r\n" + text;
        }

        private void Estimate_Result_Save_Button_Click(object sender, EventArgs e)
        {
            string path = "";
            string name = "";

            switch (selectedSystem)
            {
                case "三级架构":
                    name = "ThreeStageSystem";
                    break;
                case "两级架构":
                    name = "TwoStageSystem";
                    break;
            }
            //string localFilePath, fileNameExt, newFileName, FilePath; 
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = name,
                Filter = "Excel表格(*.xlsx)|*.xlsx", //设置文件类型
                InitialDirectory = Data.ResultPath
            };

            //点了保存按钮进入 
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName.ToString(); //获得文件路径 
                name = filePath.Substring(filePath.LastIndexOf("\\") + 1, filePath.LastIndexOf(".xlsx") - (filePath.LastIndexOf("\\") + 1)); //获取文件名，不带路径
                path = filePath.Substring(0, filePath.LastIndexOf("\\")) + "\\"; //获取文件路径，不带文件名 
            }

            Data.Save(path, name + "_Pareto.xlsx", conditionTitles, conditions, paretoDesignList);
            Data.Save(path, name + "_all.xlsx", conditionTitles, conditions, allDesignList);
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
                Filter = "Excel表格|*.xls;*.xlsx", //设定打开的文件类型
                InitialDirectory = Data.ResultPath
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
