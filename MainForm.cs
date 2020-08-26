using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using PV_analysis.Structures;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Media;

namespace PV_analysis
{
    internal partial class MainForm : System.Windows.Forms.Form
    {
        private Panel[] panelNow = new Panel[5];
        private System.Drawing.Color activeColor;
        private System.Drawing.Color inactiveColor;
        private List<Label> labelList = new List<Label>();

        private string selectedStructure; //所要评估的架构，三级架构或两级架构

        private double Psys; //架构总功率
        private double Vpv_min; //光伏MPPT电压最小值
        private double Vpv_max; //光伏MPPT电压最大值
        private double Vpv_peak; //光伏输出电压最大值
        private double Vg; //并网电压（线电压）
        private double Vo; //输出电压（并网相电压）
        private double fg = 50; //并网频率
        private double[] VbusRange = { 1300 }; //母线电压范围
        private double phi = 0; //功率因数角(rad)
        private double DCAC_Vin_def = 1300; //逆变器直流侧电压

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

        private Structure structure; //架构

        //不同负载下的损耗分布
        private int div = 100; //空载到满载划分精度
        private List<Item>[] system_lossLists; //系统损耗分布信息
        private List<Item>[] DCDC_lossLists; //前级DC/DC损耗分布信息
        private List<Item>[] isolatedDCDC_lossLists; //隔离DC/DC损耗分布信息
        private List<Item>[] DCAC_lossLists; //DC/AC损耗分布信息

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
                selectedStructure = Estimate_Step1_CheckedListBox.GetItemText(Estimate_Step1_CheckedListBox.CheckedItems[0]);
                switch (selectedStructure)
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

            if (selectedStructure.Equals("三级架构") && DCDC_topologyList.Count == 0)
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
                switch (selectedStructure)
                {
                    case "三级架构":
                        Estimate_Step3_DCDCMinNumber_TextBox.Enabled = true;
                        Estimate_Step3_DCDCMaxNumber_TextBox.Enabled = true;
                        Estimate_Step3_DCDCMinFrequency_TextBox.Enabled = true;
                        Estimate_Step3_DCDCMaxFrequency_TextBox.Enabled = true;
                        Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Enabled = true;
                        Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Enabled = true;
                        Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Enabled = true;
                        Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Enabled = true;
                        Estimate_Step3_DCACMinNumber_TextBox.Enabled = true;
                        Estimate_Step3_DCACMaxNumber_TextBox.Enabled = true;
                        Estimate_Step3_DCACMinFrequency_TextBox.Enabled = true;
                        Estimate_Step3_DCACMaxFrequency_TextBox.Enabled = true;
                        Estimate_Step3_DCDCMinNumber_TextBox.Text = "1";
                        Estimate_Step3_DCDCMaxNumber_TextBox.Text = "120";
                        Estimate_Step3_DCDCMinFrequency_TextBox.Text = "1";
                        Estimate_Step3_DCDCMaxFrequency_TextBox.Text = "100";
                        Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text = "1";
                        Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text = "40";
                        Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text = "1";
                        Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text = "100";
                        Estimate_Step3_DCACMinNumber_TextBox.Text = "1";
                        Estimate_Step3_DCACMaxNumber_TextBox.Text = "40";
                        Estimate_Step3_DCACMinFrequency_TextBox.Text = "10";
                        Estimate_Step3_DCACMaxFrequency_TextBox.Text = "10";
                        break;
                    case "两级架构":
                        Estimate_Step3_DCDCMinNumber_TextBox.Enabled = false;
                        Estimate_Step3_DCDCMaxNumber_TextBox.Enabled = false;
                        Estimate_Step3_DCDCMinFrequency_TextBox.Enabled = false;
                        Estimate_Step3_DCDCMaxFrequency_TextBox.Enabled = false;
                        Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Enabled = false;
                        Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Enabled = false;
                        Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Enabled = false;
                        Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Enabled = false;
                        Estimate_Step3_DCACMinNumber_TextBox.Enabled = false;
                        Estimate_Step3_DCACMaxNumber_TextBox.Enabled = false;
                        Estimate_Step3_DCACMinFrequency_TextBox.Enabled = false;
                        Estimate_Step3_DCACMaxFrequency_TextBox.Enabled = false;
                        Estimate_Step3_DCDCMinNumber_TextBox.Text = "";
                        Estimate_Step3_DCDCMaxNumber_TextBox.Text = "";
                        Estimate_Step3_DCDCMinFrequency_TextBox.Text = "";
                        Estimate_Step3_DCDCMaxFrequency_TextBox.Text = "";
                        Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text = "20";
                        Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text = "20";
                        Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text = "25";
                        Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text = "25";
                        Estimate_Step3_DCACMinNumber_TextBox.Text = "20";
                        Estimate_Step3_DCACMaxNumber_TextBox.Text = "20";
                        Estimate_Step3_DCACMinFrequency_TextBox.Text = "10";
                        Estimate_Step3_DCACMaxFrequency_TextBox.Text = "10";
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

        private void Estimate_Step3_Next_Button_Click(object sender, EventArgs e)
        {
            //记录
            Psys = double.Parse(Estimate_Step3_Psys_TextBox.Text) * 1e6;
            Vpv_min = double.Parse(Estimate_Step3_Vpvmin_TextBox.Text);
            Vpv_max = double.Parse(Estimate_Step3_Vpvmax_TextBox.Text);
            Vpv_peak = double.Parse(Estimate_Step3_Vpvpeak_TextBox.Text);
            Vg = double.Parse(Estimate_Step3_Vgrid_TextBox.Text) * 1e3;
            Vo = Vg / Math.Sqrt(3);
            switch (selectedStructure)
            {
                case "三级架构":
                    DCDC_numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_DCDCMinNumber_TextBox.Text), int.Parse(Estimate_Step3_DCDCMaxNumber_TextBox.Text));
                    DCDC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_DCDCMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_DCDCMaxFrequency_TextBox.Text) * 1e3);
                    isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text) * 1e3);
                    DCAC_numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_DCACMinNumber_TextBox.Text), int.Parse(Estimate_Step3_DCACMaxNumber_TextBox.Text));
                    DCAC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_DCACMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_DCACMaxFrequency_TextBox.Text) * 1e3);
                    break;
                case "两级架构":
                    isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text) * 1e3);
                    DCAC_numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_DCACMinNumber_TextBox.Text), int.Parse(Estimate_Step3_DCACMaxNumber_TextBox.Text));
                    DCAC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_DCACMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_DCACMaxFrequency_TextBox.Text) * 1e3);
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
                Estimate_Step4_Semiconductor_FlowLayoutPanel.Controls.Add(CreateLabel(manufacturer + ":"));
                foreach (Data.Semiconductor semiconductor in Data.SemiconductorList)
                {
                    if (semiconductor.Manufacturer.Equals(manufacturer))
                    {
                        Estimate_Step4_Semiconductor_FlowLayoutPanel.Controls.Add(CreateCheckBox(semiconductor.Type));
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
                Estimate_Step4_Core_FlowLayoutPanel.Controls.Add(CreateLabel(manufacturer + ":"));
                foreach (Data.Core core in Data.CoreList)
                {
                    if (core.Manufacturer.Equals(manufacturer))
                    {
                        Estimate_Step4_Core_FlowLayoutPanel.Controls.Add(CreateCheckBox(core.Type));
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
                Estimate_Step4_Wire_FlowLayoutPanel.Controls.Add(CreateLabel(category + ":"));
                foreach (Data.Wire wire in Data.WireList)
                {
                    if (wire.Category.Equals(category))
                    {
                        Estimate_Step4_Wire_FlowLayoutPanel.Controls.Add(CreateCheckBox(wire.Type));
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
                Estimate_Step4_Capacitor_FlowLayoutPanel.Controls.Add(CreateLabel(category + ":"));
                foreach (Data.Capacitor capacitor in Data.CapacitorList)
                {
                    if (capacitor.Category.Equals(category))
                    {
                        Estimate_Step4_Capacitor_FlowLayoutPanel.Controls.Add(CreateCheckBox(capacitor.Type));
                    }
                }
            }

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

            //更新信息
            Estimate_Step5_Psys_Value_Label.Text = Estimate_Step3_Psys_TextBox.Text;
            Estimate_Step5_Vpvmin_Value_Label.Text = Estimate_Step3_Vpvmin_TextBox.Text;
            Estimate_Step5_Vpvmax_Value_Label.Text = Estimate_Step3_Vpvmax_TextBox.Text;
            Estimate_Step5_Vpvpeak_Value_Label.Text = Estimate_Step3_Vpvpeak_TextBox.Text;
            Estimate_Step5_Vgrid_Value_Label.Text = Estimate_Step3_Vgrid_TextBox.Text;
            Estimate_Step5_StructureRange_Value_Label.Text = selectedStructure;
            Estimate_Step5_DCDCMinNumber_Value_Label.Text = Estimate_Step3_DCDCMinNumber_TextBox.Text;
            Estimate_Step5_DCDCMaxNumber_Value_Label.Text = Estimate_Step3_DCDCMaxNumber_TextBox.Text;
            Estimate_Step5_DCDCMinFrequency_Label.Text = Estimate_Step3_DCDCMinFrequency_TextBox.Text;
            Estimate_Step5_DCDCMaxFrequency_Label.Text = Estimate_Step3_DCDCMaxFrequency_TextBox.Text;
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
            Estimate_Step5_DCDCTopologyRange_Value_Label.Text = Function.StringArrayToString(DCDC_topologyList.ToArray());
            Estimate_Step5_IsolatedDCDCMinNumber_Value_Label.Text = Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text;
            Estimate_Step5_IsolatedDCDCMaxNumber_Value_Label.Text = Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text;
            Estimate_Step5_IsolatedDCDCMinFrequency_Label.Text = Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text;
            Estimate_Step5_IsolatedDCDCMaxFrequency_Label.Text = Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text;
            List<string> isolatedDCDC_topologyList = new List<string>();
            if (Estimate_Step2_Group2_Item1_Left_CheckBox.Checked)
            {
                isolatedDCDC_topologyList.Add("SRC");
            }
            if (Estimate_Step2_Group2_Item2_Left_CheckBox.Checked)
            {
                isolatedDCDC_topologyList.Add("DTCSRC");
            }

            Estimate_Step5_IsolatedDCDCTopologyRange_Value_Label.Text = Function.StringArrayToString(isolatedDCDC_topologyList.ToArray());
            Estimate_Step5_DCACMinNumber_Value_Label.Text = Estimate_Step3_DCACMinNumber_TextBox.Text;
            Estimate_Step5_DCACMaxNumber_Value_Label.Text = Estimate_Step3_DCACMaxNumber_TextBox.Text;
            Estimate_Step5_DCACMinFrequency_Label.Text = Estimate_Step3_DCACMinFrequency_TextBox.Text;
            Estimate_Step5_DCACMaxFrequency_Label.Text = Estimate_Step3_DCACMaxFrequency_TextBox.Text;
            List<string> DCAC_topologyList = new List<string>();
            if (Estimate_Step2_Group3_Item1_Left_CheckBox.Checked)
            {
                DCAC_topologyList.Add("CHB");
            }
            Estimate_Step5_DCACTopologyRange_Value_Label.Text = Function.StringArrayToString(DCAC_topologyList.ToArray());

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

        private void WriteLine()
        {
            Estimate_Result_Print_Label.Text += "\r\n";
        }

        private void WriteLine(string text)
        {
            Estimate_Result_Print_Label.Text += "\r\n" + text;
        }

        private void Estimate_Step5_Next_button_Click(object sender, EventArgs e)
        {
            Estimate_Result_Print_Label.Text = "";

            panelNow[2] = Estimate_Result_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;

            Formula.Init();
            structure = new ThreeLevelStructure();
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
                        IsolatedDCDC_Q = isolatedDCDC_Q,
                        Math_phi = phi,
                        Math_VbusRange = VbusRange,
                        DCDC_numberRange = DCDC_numberRange,
                        DCDC_topologyRange = DCDC_topologyRange,
                        DCDC_frequencyRange = DCDC_frequencyRange,
                        IsolatedDCDC_topologyRange = isolatedDCDC_topologyRange,
                        IsolatedDCDC_resonanceFrequencyRange = isolatedDCDC_resonanceFrequencyRange,
                        DCAC_numberRange = DCAC_numberRange,
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
                        DCAC_Vin_def = DCAC_Vin_def,
                        Math_phi = phi,
                        IsolatedDCDC_topologyRange = isolatedDCDC_topologyRange,
                        IsolatedDCDC_resonanceFrequencyRange = isolatedDCDC_resonanceFrequencyRange,
                        DCAC_numberRange = DCAC_numberRange,
                        DCAC_topologyRange = DCAC_topologyRange,
                        DCAC_modulationRange = DCAC_modulationRange,
                        DCAC_frequencyRange = DCAC_frequencyRange,
                    };
                    break;
            }
            structure.Optimize();
            WriteLine("评估结束！");
            WriteLine();
        }

        private void Estimate_Result_Restart_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = Estimate_Ready_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Result_QuickSave_Button_Click(object sender, EventArgs e)
        {
            structure.Save();
        }

        private void Estimate_Result_Save_Button_Click(object sender, EventArgs e)
        {
            string path = "";
            string name = "";

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
                structure.Save(path, name);
            }
        }

        private void Display()
        {
            //获取数据
            IConverterDesignData[] data = structure.AllDesignList.GetData();

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
                    for (int i = 1; i < data.Length; i++)
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
                    for (int i = 1; i < data.Length; i++)
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
                    for (int i = 1; i < data.Length; i++)
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
            Display_Show_Graph_CartesianChart.DataClick += Chart_OnDataClick; //添加点击图像点事件

            //清空文本显示
            Display_Show_EfficiencyCGC_Value_Label.Text = "";
            Display_Show_Cost_Value_Label.Text = "";
            Display_Show_Volume_Value_Label.Text = "";
            Display_Show_DCDCNumber_Value_Label.Text = "";
            Display_Show_DCDCFrequency_Value_Label.Text = "";
            Display_Show_DCDCTopology_Value_Label.Text = "";
            Display_Show_Structure_Value_Label.Text = "";
            Display_Show_IsolatedDCDCNumber_Value_Label.Text = "";
            Display_Show_IsolatedDCDCFrequency_Value_Label.Text = "";
            Display_Show_IsolatedDCDCTopology_Value_Label.Text = "";
            Display_Show_DCACNumber_Value_Label.Text = "";
            Display_Show_DCACFrequency_Value_Label.Text = "";
            Display_Show_DCACTopology_Value_Label.Text = "";

            //更新控件            
            Display_Show_Detail_Button.Enabled = false;
        }

        private void Chart_OnDataClick(object sender, ChartPoint chartPoint)
        {
            //目前采用评估结果比较来查找 TODO 能否直接将chartPoint与点的具体信息相联系
            string[] configs = new string[1];
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
            structure.Load(configs, ref index);

            Display_Show_EfficiencyCGC_Value_Label.Text = (structure.EfficiencyCGC * 100).ToString("f2") + "%";
            Display_Show_Cost_Value_Label.Text = (structure.Cost / 1e4).ToString("f2") + "万元";
            Display_Show_Volume_Value_Label.Text = structure.Volume.ToString("f2") + "dm^3";
            Display_Show_Structure_Value_Label.Text = selectedStructure;
            switch (selectedStructure)
            {
                case "三级架构":
                    Display_Show_DCDCNumber_Value_Label.Text = ((ThreeLevelStructure)structure).DCDC.Number.ToString(); ;
                    Display_Show_DCDCFrequency_Value_Label.Text = (((ThreeLevelStructure)structure).DCDC.Math_fs / 1e3).ToString("f1") + "kHz";
                    Display_Show_DCDCTopology_Value_Label.Text = ((ThreeLevelStructure)structure).DCDC.Topology.GetName();
                    Display_Show_IsolatedDCDCNumber_Value_Label.Text = ((ThreeLevelStructure)structure).IsolatedDCDC.Number.ToString();
                    Display_Show_IsolatedDCDCFrequency_Value_Label.Text = (((ThreeLevelStructure)structure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz";
                    Display_Show_IsolatedDCDCTopology_Value_Label.Text = ((ThreeLevelStructure)structure).IsolatedDCDC.Topology.GetName();
                    Display_Show_DCACNumber_Value_Label.Text = ((ThreeLevelStructure)structure).DCAC.Number.ToString();
                    Display_Show_DCACFrequency_Value_Label.Text = (((ThreeLevelStructure)structure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz";
                    Display_Show_DCACTopology_Value_Label.Text = ((ThreeLevelStructure)structure).DCAC.Modulation.ToString();
                    break;

                case "两级架构":
                    Display_Show_DCDCNumber_Value_Label.Text = "";
                    Display_Show_DCDCFrequency_Value_Label.Text = "";
                    Display_Show_DCDCTopology_Value_Label.Text = "";
                    Display_Show_IsolatedDCDCNumber_Value_Label.Text = ((TwoLevelStructure)structure).IsolatedDCDC.Number.ToString();
                    Display_Show_IsolatedDCDCFrequency_Value_Label.Text = (((TwoLevelStructure)structure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz";
                    Display_Show_IsolatedDCDCTopology_Value_Label.Text = ((TwoLevelStructure)structure).IsolatedDCDC.Topology.GetName();
                    Display_Show_DCACNumber_Value_Label.Text = ((TwoLevelStructure)structure).DCAC.Number.ToString();
                    Display_Show_DCACFrequency_Value_Label.Text = (((TwoLevelStructure)structure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz";
                    Display_Show_DCACTopology_Value_Label.Text = ((TwoLevelStructure)structure).DCAC.Modulation.ToString();
                    break;
            }

            //更新控件
            Display_Show_Detail_Button.Enabled = true;
        }

        private void Estimate_Result_Display_Button_Click(object sender, EventArgs e)
        {
            //更新控件、图像
            if (Display_Show_GraphCategory_ComboBox.SelectedIndex == 0)
            {
                Display();
            }
            else
            {
                Display_Show_GraphCategory_ComboBox.SelectedIndex = 0;
            }

            //页面切换
            panelNow[3] = Display_Show_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;

            //左侧栏切换
            Tab_Estimate_Button.BackColor = inactiveColor;
            Tab_Display_Button.BackColor = activeColor;
        }

        private void Display_Show_Restart_Button_Click(object sender, EventArgs e)
        {
            panelNow[3] = Display_Ready_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
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
                //读取数据
                string filePath = openFileDialog.FileName; //取得文件路径及文件名
                string[][] info = Data.Load(filePath); //读取数据
                string[] conditions = info[0];
                string obj = conditions[0];
                switch (obj)
                {
                    case "ThreeLevelStructure":
                        selectedStructure = "三级架构";
                        Formula.Init();
                        double Psys = double.Parse(conditions[1]);
                        double Vpv_min = double.Parse(conditions[2]);
                        double Vpv_max = double.Parse(conditions[3]);
                        double Vg = double.Parse(conditions[4]);
                        double fg = double.Parse(conditions[5]);
                        double Q = double.Parse(conditions[6]);
                        double phi = double.Parse(conditions[7]);
                        structure = new ThreeLevelStructure()
                        {
                            Math_Psys = Psys,
                            Math_Vpv_min = Vpv_min,
                            Math_Vpv_max = Vpv_max,
                            Math_Vg = Vg,
                            Math_Vo = Vg / Math.Sqrt(3),
                            Math_fg = fg,
                            IsolatedDCDC_Q = Q,
                            Math_phi = phi,
                        };
                        break;

                    case "TwoLevelStructure":
                        selectedStructure = "两级架构";
                        Formula.Init();
                        Psys = double.Parse(conditions[1]);
                        Vpv_min = double.Parse(conditions[2]);
                        Vpv_max = double.Parse(conditions[3]);
                        Vg = double.Parse(conditions[4]);
                        fg = double.Parse(conditions[5]);
                        Q = double.Parse(conditions[6]);
                        double DCAC_Vin_def = double.Parse(conditions[7]);
                        phi = double.Parse(conditions[8]);
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
                            Math_phi = phi,
                        };
                        break;
                }
                for (int i = 1; i < info.Length; i++)
                {
                    double efficiency = double.Parse(info[i][0]);
                    double volume = double.Parse(info[i][1]);
                    double cost = double.Parse(info[i][2]);
                    structure.AllDesignList.Add(efficiency, volume, cost, info[i]);
                }

                //更新控件、图像
                if (Display_Show_GraphCategory_ComboBox.SelectedIndex == 0)
                {
                    Display();
                }
                else
                {
                    Display_Show_GraphCategory_ComboBox.SelectedIndex = 0;
                }

                //页面切换
                panelNow[3] = Display_Show_Panel;
                panelNow[0].Visible = false;
                panelNow[0] = panelNow[3];
                panelNow[0].Visible = true;
            }
        }

        private void DisplayLossBreakdown()
        {
            int load = Display_Detail_Load_TrackBar.Value;
            string labelPoint(ChartPoint chartPoint) => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);
            SeriesCollection system_series = new SeriesCollection();
            for (int i = 0; i < system_lossLists[load - 1].Count; i++)
            {
                if (Math.Round(system_lossLists[load - 1][i].Value, 2) > 0)
                {
                    system_series.Add(new PieSeries
                    {
                        Title = system_lossLists[load - 1][i].Name,
                        Values = new ChartValues<double> { Math.Round(system_lossLists[load - 1][i].Value, 2) },
                        DataLabels = true,
                        LabelPoint = labelPoint
                    });
                }
            }
            Display_Detail_SystemLossBreakdown_PieChart.Series = system_series;
            Display_Detail_SystemLossBreakdown_PieChart.StartingRotationAngle = 0;
            Display_Detail_SystemLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;
            switch (selectedStructure)
            {
                case "三级架构":
                    SeriesCollection DCDC_series = new SeriesCollection();
                    for (int i = 0; i < DCDC_lossLists[load - 1].Count; i++)
                    {
                        if (Math.Round(DCDC_lossLists[load - 1][i].Value, 2) > 0)
                        {
                            DCDC_series.Add(new PieSeries
                            {
                                Title = DCDC_lossLists[load - 1][i].Name,
                                Values = new ChartValues<double> { Math.Round(DCDC_lossLists[load - 1][i].Value, 2) },
                                DataLabels = true,
                                LabelPoint = labelPoint
                            });
                        }
                    }
                    Display_Detail_DCDCLossBreakdown_PieChart.Series = DCDC_series;
                    Display_Detail_DCDCLossBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_DCDCLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;

                    SeriesCollection isolatedDCDC_series = new SeriesCollection();
                    for (int i = 0; i < isolatedDCDC_lossLists[load - 1].Count; i++)
                    {
                        if (Math.Round(isolatedDCDC_lossLists[load - 1][i].Value, 2) > 0)
                        {
                            isolatedDCDC_series.Add(new PieSeries
                            {
                                Title = isolatedDCDC_lossLists[load - 1][i].Name,
                                Values = new ChartValues<double> { Math.Round(isolatedDCDC_lossLists[load - 1][i].Value, 2) },
                                DataLabels = true,
                                LabelPoint = labelPoint
                            });
                        }
                    }
                    Display_Detail_IsolatedDCDCLossBreakdown_PieChart.Series = isolatedDCDC_series;
                    Display_Detail_IsolatedDCDCLossBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_IsolatedDCDCLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;

                    SeriesCollection DCAC_series = new SeriesCollection();
                    for (int i = 0; i < DCAC_lossLists[load - 1].Count; i++)
                    {
                        if (Math.Round(DCAC_lossLists[load - 1][i].Value, 2) > 0)
                        {
                            DCAC_series.Add(new PieSeries
                            {
                                Title = DCAC_lossLists[load - 1][i].Name,
                                Values = new ChartValues<double> { Math.Round(DCAC_lossLists[load - 1][i].Value, 2) },
                                DataLabels = true,
                                LabelPoint = labelPoint
                            });
                        }
                    }
                    Display_Detail_DCACLossBreakdown_PieChart.Series = DCAC_series;
                    Display_Detail_DCACLossBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_DCACLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;
                    break;

                case "两级架构":
                    Display_Detail_DCDCLossBreakdown_PieChart.Series = new SeriesCollection();
                    Display_Detail_DCDCLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;

                    isolatedDCDC_series = new SeriesCollection();
                    for (int i = 0; i < isolatedDCDC_lossLists[load - 1].Count; i++)
                    {
                        if (Math.Round(isolatedDCDC_lossLists[load - 1][i].Value, 2) > 0)
                        {
                            isolatedDCDC_series.Add(new PieSeries
                            {
                                Title = isolatedDCDC_lossLists[load - 1][i].Name,
                                Values = new ChartValues<double> { Math.Round(isolatedDCDC_lossLists[load - 1][i].Value, 2) },
                                DataLabels = true,
                                LabelPoint = labelPoint
                            });
                        }
                    }
                    Display_Detail_IsolatedDCDCLossBreakdown_PieChart.Series = isolatedDCDC_series;
                    Display_Detail_IsolatedDCDCLossBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_IsolatedDCDCLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;

                    DCAC_series = new SeriesCollection();
                    for (int i = 0; i < DCAC_lossLists[load - 1].Count; i++)
                    {
                        if (Math.Round(DCAC_lossLists[load - 1][i].Value, 2) > 0)
                        {
                            DCAC_series.Add(new PieSeries
                            {
                                Title = DCAC_lossLists[load - 1][i].Name,
                                Values = new ChartValues<double> { Math.Round(DCAC_lossLists[load - 1][i].Value, 2) },
                                DataLabels = true,
                                LabelPoint = labelPoint
                            });
                        }
                    }
                    Display_Detail_DCACLossBreakdown_PieChart.Series = DCAC_series;
                    Display_Detail_DCACLossBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_DCACLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;
                    break;
            }
        }

        private void Display_Show_Detail_Button_Click(object sender, EventArgs e)
        {
            //详情信息显示
            structure.Evaluate(); //评估

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

            Func<ChartPoint, string> labelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);

            Display_Detail_EfficiencyCGC_Value_Label.Text = (structure.EfficiencyCGC * 100).ToString("f2") + "%";
            Display_Detail_Cost_Value_Label.Text = (structure.Cost / 1e4).ToString("f2") + "万元";
            Display_Detail_Volume_Value_Label.Text = structure.Volume.ToString("f2") + "dm^3";
            Display_Detail_Structure_Value_Label.Text = selectedStructure;
            switch (selectedStructure)
            {
                case "三级架构":
                    Display_Detail_DCDCNumber_Value_Label.Text = ((ThreeLevelStructure)structure).DCDC.Number.ToString(); ;
                    Display_Detail_DCDCFrequency_Value_Label.Text = (((ThreeLevelStructure)structure).DCDC.Math_fs / 1e3).ToString("f1") + "kHz";
                    Display_Detail_DCDCTopology_Value_Label.Text = ((ThreeLevelStructure)structure).DCDC.Topology.GetName();
                    Display_Detail_IsolatedDCDCNumber_Value_Label.Text = ((ThreeLevelStructure)structure).IsolatedDCDC.Number.ToString();
                    Display_Detail_IsolatedDCDCFrequency_Value_Label.Text = (((ThreeLevelStructure)structure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz";
                    Display_Detail_IsolatedDCDCTopology_Value_Label.Text = ((ThreeLevelStructure)structure).IsolatedDCDC.Topology.GetName();
                    Display_Detail_DCACNumber_Value_Label.Text = ((ThreeLevelStructure)structure).DCAC.Number.ToString();
                    Display_Detail_DCACFrequency_Value_Label.Text = (((ThreeLevelStructure)structure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz";
                    Display_Detail_DCACTopology_Value_Label.Text = ((ThreeLevelStructure)structure).DCAC.Modulation.ToString();
                    //显示图像
                    double p1 = Math.Round(100 - structure.Converters[0].EfficiencyCGC * 100, 2);
                    double p2 = Math.Round(100 - structure.Converters[1].EfficiencyCGC * 100, 2);
                    double p3 = Math.Round(100 - structure.Converters[2].EfficiencyCGC * 100, 2);
                    Display_Detail_SystemEvaluateLossBreakdown_PieChart.Series = new SeriesCollection
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
                    Display_Detail_SystemEvaluateLossBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_SystemEvaluateLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;

                    double v1 = Math.Round(structure.Converters[0].Volume, 2);
                    double v2 = Math.Round(structure.Converters[1].Volume, 2);
                    double v3 = Math.Round(structure.Converters[2].Volume, 2);
                    Display_Detail_SystemVolumeBreakdown_PieChart.Series = new SeriesCollection
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
                    Display_Detail_SystemVolumeBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_SystemVolumeBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;

                    double c1 = Math.Round(structure.Converters[0].Cost / 1e4, 2);
                    double c2 = Math.Round(structure.Converters[1].Cost / 1e4, 2);
                    double c3 = Math.Round(structure.Converters[2].Cost / 1e4, 2);
                    Display_Detail_SystemCostBreakdown_PieChart.Series = new SeriesCollection
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
                    Display_Detail_SystemCostBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_SystemCostBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;
                    break;

                case "两级架构":
                    Display_Detail_DCDCNumber_Value_Label.Text = "";
                    Display_Detail_DCDCFrequency_Value_Label.Text = "";
                    Display_Detail_DCDCTopology_Value_Label.Text = "";
                    Display_Detail_IsolatedDCDCNumber_Value_Label.Text = ((TwoLevelStructure)structure).IsolatedDCDC.Number.ToString();
                    Display_Detail_IsolatedDCDCFrequency_Value_Label.Text = (((TwoLevelStructure)structure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz";
                    Display_Detail_IsolatedDCDCTopology_Value_Label.Text = ((TwoLevelStructure)structure).IsolatedDCDC.Topology.GetName();
                    Display_Detail_DCACNumber_Value_Label.Text = ((TwoLevelStructure)structure).DCAC.Number.ToString();
                    Display_Detail_DCACFrequency_Value_Label.Text = (((TwoLevelStructure)structure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz";
                    Display_Detail_DCACTopology_Value_Label.Text = ((TwoLevelStructure)structure).DCAC.Modulation.ToString();
                    //显示图像
                    p1 = Math.Round(100 - structure.Converters[0].EfficiencyCGC * 100, 2);
                    p2 = Math.Round(100 - structure.Converters[1].EfficiencyCGC * 100, 2);
                    Display_Detail_SystemEvaluateLossBreakdown_PieChart.Series = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "隔离DC/DC",
                            Values = new ChartValues<double> {p1},
                            DataLabels = true,
                            LabelPoint = labelPoint
                        },
                        new PieSeries
                        {
                            Title = "逆变",
                            Values = new ChartValues<double> {p2},
                            DataLabels = true,
                            LabelPoint = labelPoint
                        },
                    };
                    Display_Detail_SystemEvaluateLossBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_SystemEvaluateLossBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;

                    v1 = Math.Round(structure.Converters[0].Volume, 2);
                    v2 = Math.Round(structure.Converters[1].Volume, 2);
                    Display_Detail_SystemVolumeBreakdown_PieChart.Series = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "隔离DC/DC",
                            Values = new ChartValues<double> {v1},
                            DataLabels = true,
                            LabelPoint = labelPoint
                        },
                        new PieSeries
                        {
                            Title = "逆变",
                            Values = new ChartValues<double> {v2},
                            DataLabels = true,
                            LabelPoint = labelPoint
                        },
                    };
                    Display_Detail_SystemVolumeBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_SystemVolumeBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;

                    c1 = Math.Round(structure.Converters[0].Cost / 1e4, 2);
                    c2 = Math.Round(structure.Converters[1].Cost / 1e4, 2);
                    Display_Detail_SystemCostBreakdown_PieChart.Series = new SeriesCollection
                    {
                        new PieSeries
                        {
                            Title = "隔离DC/DC",
                            Values = new ChartValues<double> {c1},
                            DataLabels = true,
                            LabelPoint = labelPoint
                        },
                        new PieSeries
                        {
                            Title = "逆变",
                            Values = new ChartValues<double> {c2},
                            DataLabels = true,
                            LabelPoint = labelPoint
                        },
                    };
                    Display_Detail_SystemCostBreakdown_PieChart.StartingRotationAngle = 0;
                    Display_Detail_SystemCostBreakdown_PieChart.LegendLocation = LegendLocation.Bottom;
                    break;
            }

            //生成不同负载下的损耗数据
            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            system_lossLists = new List<Item>[div];
            DCDC_lossLists = new List<Item>[div];
            isolatedDCDC_lossLists = new List<Item>[div];
            DCAC_lossLists = new List<Item>[div];
            for (int i = 1; i <= div; i++)
            {
                structure.Operate(1.0 * i / div);
                values.Add(new ObservablePoint(100 * i / div, structure.Efficiency * 100));
                system_lossLists[i - 1] = structure.GetLossBreakdown();
                switch (selectedStructure)
                {
                    case "三级架构":
                        DCDC_lossLists[i - 1] = ((ThreeLevelStructure)structure).DCDC.GetLossBreakdown();
                        isolatedDCDC_lossLists[i - 1] = ((ThreeLevelStructure)structure).IsolatedDCDC.GetLossBreakdown();
                        DCAC_lossLists[i - 1] = ((ThreeLevelStructure)structure).DCAC.GetLossBreakdown();
                        break;
                    case "两级架构":
                        isolatedDCDC_lossLists[i - 1] = ((TwoLevelStructure)structure).IsolatedDCDC.GetLossBreakdown();
                        DCAC_lossLists[i - 1] = ((TwoLevelStructure)structure).DCAC.GetLossBreakdown();
                        break;
                }
            }

            //负载-效率图像
            Display_Detail_Main_Panel.Controls.Remove(Display_Detail_SystemLoadVsEfficiency_CartesianChart);
            Display_Detail_SystemLoadVsEfficiency_CartesianChart.Dispose();
            Display_Detail_SystemLoadVsEfficiency_CartesianChart = new LiveCharts.WinForms.CartesianChart
            {
                BackColor = System.Drawing.Color.White,
                Location = new System.Drawing.Point(316, 877),
                Size = new System.Drawing.Size(800, 600),
                TabIndex = 160,
            };
            Display_Detail_Main_Panel.Controls.Add(Display_Detail_SystemLoadVsEfficiency_CartesianChart);
            Display_Detail_Main_Panel.Visible = false; //解决底色变黑
            Display_Detail_Main_Panel.Visible = true;
            Display_Detail_SystemLoadVsEfficiency_CartesianChart.Series.Add(new LineSeries
            {
                Title = "Vin=860V",
                Values = values
            });
            Display_Detail_SystemLoadVsEfficiency_CartesianChart.AxisX.Add(new Axis
            {
                Title = "负载（%）"
            });
            Display_Detail_SystemLoadVsEfficiency_CartesianChart.AxisY.Add(new Axis
            {
                LabelFormatter = value => Math.Round(value, 8).ToString(),
                Title = "效率（%）"
            });
            Display_Detail_SystemLoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Right;

            //损耗分布图像
            Display_Detail_Load_TrackBar.Value = 100;
            DisplayLossBreakdown();

            panelNow[3] = Display_Detail_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;
        }

        private void Display_Detail_Load_TrackBar_Scroll(object sender, EventArgs e)
        {
            Display_Detail_Load_Value_Label.Text = Display_Detail_Load_TrackBar.Value.ToString() + "%";
            DisplayLossBreakdown();
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

        private void Estimate_Step3_IsolatedDCDCMinNumber_TextBox_TextChanged(object sender, EventArgs e)
        {
            Estimate_Step3_DCACMinNumber_TextBox.Text = Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text;
        }

        private void Estimate_Step3_IsolatedDCDCMaxNumber_TextBox_TextChanged(object sender, EventArgs e)
        {
            Estimate_Step3_DCACMaxNumber_TextBox.Text = Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text;
        }

        private void Estimate_Step3_DCACMinNumber_TextBox_TextChanged(object sender, EventArgs e)
        {
            Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text = Estimate_Step3_DCACMinNumber_TextBox.Text;
        }

        private void Estimate_Step3_DCACMaxNumber_TextBox_TextChanged(object sender, EventArgs e)
        {
            Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text = Estimate_Step3_DCACMaxNumber_TextBox.Text;
        }

        private void Display_Show_GraphCategory_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Display();
        }
    }
}
