using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using PV_analysis.Converters;
using PV_analysis.Informations;
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
        //页面切换、侧边栏
        private readonly Panel[] panelNow = new Panel[5]; //下标0——当前显示页面，下标1-4——各类页面的当前子页面
        private System.Drawing.Color activeColor; //左侧边栏按钮，当前选中颜色
        private System.Drawing.Color inactiveColor; //左侧边栏按钮，未选中颜色

        //评估对象
        private bool isStructureEvaluation; //是否为架构评估（若为false，则为变换单元）
        private string evaluationObjectName; //评估的对象名
        private Structure structureForEvaluation; //用于评估的架构
        private Converter converterForEvaluation; //用于评估的变换单元

        //可用拓扑序列
        private string[] DCDC_topologyRange;
        private string[] isolatedDCDC_topologyRange;
        private string[] DCAC_topologyRange;

        //评估过程
        private Thread evaluationThread; //评估线程
        private readonly bool IS_PRINT_DEBUG = true; //是否打印Debug信息
        private readonly short PRINT_LEVEL = 4; //允许打印的详细信息等级

        //展示对象
        private bool isStructureDisplay; //是否为架构展示（若为false，则为变换单元）
        private bool isAllDisplay = true; //是否展示所有结果
        private bool isParetoDisplay = true; //是否展示Pareto前沿
        private int displayNum = 0; //展示的数量（记录已在图像中绘制出的展示总数）
        private List<string> seriesNameList; //展示图像中的系列名
        private List<Structure> structureListForDisplay; //用于展示的架构
        private List<Converter> converterListForDisplay; //用于展示的变换单元

        //选择对象
        private Structure selectedStructure; //展示图像中选中的架构
        private Converter selectedConverter; //展示图像中选中的变换单元

        //对比对象
        private bool isComponentInfoDisplay = true; //是否展示器件的设计信息
        private List<Structure> structureListForContrast; //用于对比的架构
        private List<Converter> converterListForContrast; //用于对比的变换单元

        //负载-效率曲线
        private readonly int div = 100; //空载到满载划分精度

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
        /// 打印详细信息
        /// 由信息等级决定是否需要打印
        /// 由IS_PRINT_DEBUG判断是否需要打印Debug信息
        /// </summary>
        /// <param name="level">信息等级</param>
        /// <param name="text">文字内容</param>
        public void PrintDetails(int level, string text = "")
        {
            if (IS_PRINT_DEBUG)
            {
                Console.WriteLine(text);
            }

            if (level >= PRINT_LEVEL)
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
        /// 评估结果步骤，设置进度条百分比
        /// </summary>
        /// <param name="percent">百分比</param>
        public void Estimate_Result_ProgressBar_Set(double percent)
        {
            percent = percent > 100 ? 100 : percent;
            percent = percent < 0 ? 0 : percent;
            if (Thread.CurrentThread.IsBackground)
            {
                BeginInvoke(new EventHandler(delegate
                {
                    Estimate_Result_ProgressBar.Value = (int)Math.Ceiling(percent / 100 * (Estimate_Result_ProgressBar.Maximum - Estimate_Result_ProgressBar.Minimum)) + Estimate_Result_ProgressBar.Minimum;
                }));
            }
            else
            {
                Estimate_Result_ProgressBar.Value = (int)Math.Ceiling(percent / 100 * (Estimate_Result_ProgressBar.Maximum - Estimate_Result_ProgressBar.Minimum)) + Estimate_Result_ProgressBar.Minimum;
            }
        }

        /// <summary>
        /// 评估结果步骤，开始评估
        /// </summary>
        private void Estimate_Result_Evaluate()
        {
            Estimate_Result_ProgressBar_Set(0);
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

            if (isStructureEvaluation)
            {
                double Psys = double.Parse(Estimate_Step3_Psys_TextBox.Text) * 1e6; //架构总功率
                double Vpv_min = double.Parse(Estimate_Step3_Vpvmin_TextBox.Text); //光伏MPPT电压最小值
                double Vpv_max = double.Parse(Estimate_Step3_Vpvmax_TextBox.Text); //光伏MPPT电压最大值
                double Vpv_peak = double.Parse(Estimate_Step3_Vpvpeak_TextBox.Text); //光伏输出电压最大值
                double Vg = double.Parse(Estimate_Step3_Vgrid_TextBox.Text) * 1e3; //并网电压（线电压）
                double Vo = Vg / Math.Sqrt(3); //输出电压（并网相电压）
                double fg = 50; //并网频率
                double[] VbusRange; //母线电压范围
                double[] VinvRange = Function.GenerateVinvRange(int.Parse(Estimate_Step3_Vinvmin_TextBox.Text), int.Parse(Estimate_Step3_Vinvmax_TextBox.Text)); //逆变直流侧电压范围
                int[] DCDC_numberRange; //DC/DC可用模块数序列
                double[] DCDC_frequencyRange; //DC/DC可用开关频率序列
                double isolatedDCDC_Q = double.Parse(Estimate_Step3_IsolatedDCDCQ_TextBox.Text); //品质因数    
                int[] isolatedDCDC_secondaryRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_IsolatedDCDCMinSecondary_TextBox.Text), int.Parse(Estimate_Step3_IsolatedDCDCMaxSecondary_TextBox.Text)); //隔离DC/DC可用副边个数序列
                int[] isolatedDCDC_numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_IsolatedDCDCMinNumber_TextBox.Text), int.Parse(Estimate_Step3_IsolatedDCDCMaxNumber_TextBox.Text)); //隔离DC/DC可用模块数序列
                double[] isolatedDCDC_resonanceFrequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_IsolatedDCDCMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_IsolatedDCDCMaxFrequency_TextBox.Text) * 1e3); //隔离DC/DC可用谐振频率序列
                double DCAC_Ma_min = double.Parse(Estimate_Step3_DCACMamin_TextBox.Text); //最小电压调制比
                double DCAC_Ma_max = double.Parse(Estimate_Step3_DCACMamax_TextBox.Text); //最大电压调制比
                double DCAC_φ = 0; //功率因数角(rad)
                string[] DCAC_modulationRange = { "PSPWM", "LSPWM" }; //DC/AC可用调制方式序列
                double[] DCAC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_DCACMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_DCACMaxFrequency_TextBox.Text) * 1e3); //DC/AC开关谐振频率序列

                Formula.Init();
                switch (evaluationObjectName)
                {
                    case "三级架构":
                        VbusRange = Function.GenerateVbusRange(int.Parse(Estimate_Step3_Vbusmin_TextBox.Text), int.Parse(Estimate_Step3_Vbusmax_TextBox.Text));
                        DCDC_numberRange = Function.GenerateNumberRange(int.Parse(Estimate_Step3_DCDCMinNumber_TextBox.Text), int.Parse(Estimate_Step3_DCDCMaxNumber_TextBox.Text));
                        DCDC_frequencyRange = Function.GenerateFrequencyRange(double.Parse(Estimate_Step3_DCDCMinFrequency_TextBox.Text) * 1e3, double.Parse(Estimate_Step3_DCDCMaxFrequency_TextBox.Text) * 1e3);
                        structureForEvaluation = new ThreeLevelStructure
                        {
                            Name = evaluationObjectName,
                            Math_Psys = Psys,
                            Math_Vpv_min = Vpv_min,
                            Math_Vpv_max = Vpv_max,
                            Math_Vg = Vg,
                            Math_Vo = Vo,
                            Math_fg = fg,
                            Math_VbusRange = VbusRange,
                            Math_VinvRange = VinvRange,
                            DCDC_numberRange = DCDC_numberRange,
                            DCDC_topologyRange = DCDC_topologyRange,
                            DCDC_frequencyRange = DCDC_frequencyRange,
                            IsolatedDCDC_Q = isolatedDCDC_Q,
                            IsolatedDCDC_secondaryRange = isolatedDCDC_secondaryRange,
                            IsolatedDCDC_numberRange = isolatedDCDC_numberRange,
                            IsolatedDCDC_topologyRange = isolatedDCDC_topologyRange,
                            IsolatedDCDC_resonanceFrequencyRange = isolatedDCDC_resonanceFrequencyRange,
                            DCAC_Ma_min = DCAC_Ma_min,
                            DCAC_Ma_max = DCAC_Ma_max,
                            DCAC_φ = DCAC_φ,
                            DCAC_topologyRange = DCAC_topologyRange,
                            DCAC_modulationRange = DCAC_modulationRange,
                            DCAC_frequencyRange = DCAC_frequencyRange,
                        };
                        break;
                    case "两级架构":
                        structureForEvaluation = new TwoLevelStructure
                        {
                            Name = evaluationObjectName,
                            Math_Psys = Psys,
                            Math_Vpv_min = Vpv_min,
                            Math_Vpv_max = Vpv_max,
                            Math_Vg = Vg,
                            Math_Vo = Vo,
                            Math_fg = fg,
                            Math_VinvRange = VinvRange,
                            IsolatedDCDC_Q = isolatedDCDC_Q,
                            IsolatedDCDC_secondaryRange = isolatedDCDC_secondaryRange,
                            IsolatedDCDC_numberRange = isolatedDCDC_numberRange,
                            IsolatedDCDC_topologyRange = isolatedDCDC_topologyRange,
                            IsolatedDCDC_resonanceFrequencyRange = isolatedDCDC_resonanceFrequencyRange,
                            DCAC_Ma_min = DCAC_Ma_min,
                            DCAC_Ma_max = DCAC_Ma_max,
                            DCAC_φ = DCAC_φ,
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

                switch (evaluationObjectName)
                {
                    case "前级DC/DC变换单元_三级":
                        double Psys = double.Parse(Estimate_Step3B_Psys_TextBox.Text) * 1e6;
                        double Vin_min = double.Parse(Estimate_Step3B_Vinmin_TextBox.Text);
                        double Vin_max = double.Parse(Estimate_Step3B_Vinmax_TextBox.Text);
                        double Vo = double.Parse(Estimate_Step3B_Vo_TextBox.Text);
                        converterForEvaluation = new DCDCConverter()
                        {
                            Name = evaluationObjectName,
                            PhaseNum = 1,
                            Math_Psys = Psys,
                            Math_Vin_min = Vin_min,
                            Math_Vin_max = Vin_max,
                            IsInputVoltageVariation = true,
                            Math_Vo = Vo,
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
                        converterForEvaluation = new IsolatedDCDCConverter()
                        {
                            Name = evaluationObjectName,
                            PhaseNum = 3,
                            Math_Psys = Psys,
                            Math_Vin = Vin,
                            IsInputVoltageVariation = false,
                            Math_Vo = Vo,
                            Math_Q = Q,
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
                        converterForEvaluation = new IsolatedDCDCConverter()
                        {
                            Name = evaluationObjectName,
                            PhaseNum = 3,
                            Math_Psys = Psys,
                            Math_Vin_min = Vin_min,
                            Math_Vin_max = Vin_max,
                            IsInputVoltageVariation = true,
                            Math_Vo = Vo,
                            Math_Q = Q,
                            SecondaryRange = secondaryRange,
                            NumberRange = numberRange,
                            TopologyRange = isolatedDCDC_topologyRange,
                            FrequencyRange = frequencyRange
                        };
                        break;
                    case "逆变单元":
                        Psys = double.Parse(Estimate_Step3B_Psys_TextBox.Text) * 1e6;
                        Vin = double.Parse(Estimate_Step3B_Vin_TextBox.Text);
                        double Vg = double.Parse(Estimate_Step3B_Vo_TextBox.Text) * 1e3;
                        double fg = 50; //并网频率
                        double Ma_min = double.Parse(Estimate_Step3B_Mamin_TextBox.Text);
                        double Ma_max = double.Parse(Estimate_Step3B_Mamax_TextBox.Text);
                        double φ = 0; //功率因数角(rad)
                        string[] modulationRange = { "PSPWM", "LSPWM" };
                        converterForEvaluation = new DCACConverter()
                        {
                            Name = evaluationObjectName,
                            PhaseNum = 3,
                            Math_Psys = Psys,
                            Math_Vin = Vin,
                            IsInputVoltageVariation = false,
                            Math_Vg = Vg,
                            Math_Vo = Vg / Math.Sqrt(3),
                            Math_fg = fg,
                            Math_Ma_min = Ma_min,
                            Math_Ma_max = Ma_max,
                            Math_φ = φ,
                            NumberRange = numberRange,
                            TopologyRange = DCAC_topologyRange,
                            ModulationRange = modulationRange,
                            FrequencyRange = frequencyRange
                        };
                        break;
                }
            }
            PrintMsg("开始评估！");

            if (isStructureEvaluation)
            {
                structureForEvaluation.Optimize(this, 0, 100);
            }
            else
            {
                converterForEvaluation.Optimize(this, 0, 100);
            }
            Estimate_Result_ProgressBar_Set(100);
            PrintMsg("完成评估！");

            BeginInvoke(new EventHandler(delegate
            {
                //按钮状态设置
                Estimate_Result_End_Button.Visible = false;
                Estimate_Result_End_Button.Enabled = false;
                Estimate_Result_Restart_Button.Visible = true;
                Estimate_Result_Restart_Button.Enabled = true;
                Estimate_Result_QuickSave_Button.Enabled = true;
                Estimate_Result_Save_Button.Enabled = true;
                Estimate_Result_AddDisplay_Button.Enabled = true;
                Estimate_Result_NewDisplay_Button.Enabled = true;
            }));
        }

        /// <summary>
        /// 评估结果步骤，显示评估结果图像
        /// </summary>
        private void Estimate_Result_Display()
        {
            seriesNameList.Add("评估结果");
            if (isStructureEvaluation)
            {
                structureListForDisplay.Add(structureForEvaluation);
            }
            else
            {
                converterListForDisplay.Add(converterForEvaluation);
            }

            Display_Show_Display(); //更新结果图像显示
        }

        /// <summary>
        /// 展示页面，读取评估结果
        /// </summary>
        private void Display_Show_Load()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog //打开文件窗口
            {
                Filter = "Excel表格|*.xls;*.xlsx", //设定打开的文件类型
                InitialDirectory = Data.ResultPath
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK) //如果选定了文件
            {
                Structure structure = new ThreeLevelStructure();
                Converter converter = new DCDCConverter();
                bool isStructure; //读取的对象是否为架构
                string filePath = openFileDialog.FileName; //取得文件路径及文件名
                string name = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName); //取得文件名
                string[][] info = Data.Load(filePath); //读取数据
                string[] conditions = info[0];
                string obj = conditions[0];
                switch (obj)
                {
                    case "DCDCConverter":
                        isStructure = false;
                        converter = new DCDCConverter()
                        {
                            Name = "前级DC/DC变换单元_三级",
                            PhaseNum = 1,
                            Math_Psys = double.Parse(conditions[1]),
                            Math_Vin_min = double.Parse(conditions[2]),
                            Math_Vin_max = double.Parse(conditions[3]),
                            IsInputVoltageVariation = true,
                            Math_Vo = double.Parse(conditions[4])
                        };
                        break;

                    case "IsolatedDCDCConverter":
                        Formula.Init();
                        isStructure = false;
                        converter = new IsolatedDCDCConverter()
                        {
                            Name = "隔离DC/DC变换单元_三级",
                            PhaseNum = 3,
                            Math_Psys = double.Parse(conditions[1]),
                            Math_Vin = double.Parse(conditions[2]),
                            IsInputVoltageVariation = false,
                            Math_Vo = double.Parse(conditions[3]),
                            Math_Q = double.Parse(conditions[4]),
                        };
                        break;

                    case "IsolatedDCDCConverter_TwoStage":
                        Formula.Init();
                        isStructure = false;
                        converter = new IsolatedDCDCConverter()
                        {
                            Name = "隔离DC/DC变换单元_两级",
                            PhaseNum = 3,
                            Math_Psys = double.Parse(conditions[1]),
                            Math_Vin_min = double.Parse(conditions[2]),
                            Math_Vin_max = double.Parse(conditions[3]),
                            IsInputVoltageVariation = true,
                            Math_Vo = double.Parse(conditions[4]),
                            Math_Q = double.Parse(conditions[5]),
                        };
                        break;

                    case "DCACConverter":
                        isStructure = false;
                        converter = new DCACConverter()
                        {
                            Name = "逆变单元",
                            PhaseNum = 3,
                            Math_Psys = double.Parse(conditions[1]),
                            Math_Vin = double.Parse(conditions[2]),
                            IsInputVoltageVariation = false,
                            Math_Vg = double.Parse(conditions[3]),
                            Math_Vo = double.Parse(conditions[3]) / Math.Sqrt(3),
                            Math_fg = double.Parse(conditions[4]),
                            Math_Ma_min = double.Parse(conditions[5]),
                            Math_Ma_max = double.Parse(conditions[6]),
                            Math_φ = double.Parse(conditions[7])
                        };
                        break;

                    case "ThreeLevelStructure":
                        Formula.Init();
                        isStructure = true;
                        structure = new ThreeLevelStructure()
                        {
                            Name = "三级架构",
                            Math_Psys = double.Parse(conditions[1]),
                            Math_Vpv_min = double.Parse(conditions[2]),
                            Math_Vpv_max = double.Parse(conditions[3]),
                            Math_Vg = double.Parse(conditions[4]),
                            Math_Vo = double.Parse(conditions[4]) / Math.Sqrt(3),
                            Math_fg = double.Parse(conditions[5]),
                            IsolatedDCDC_Q = double.Parse(conditions[6]),
                            DCAC_Ma_min = double.Parse(conditions[7]),
                            DCAC_Ma_max = double.Parse(conditions[8]),
                            DCAC_φ = double.Parse(conditions[9])
                        };
                        break;

                    case "TwoLevelStructure":
                        Formula.Init();
                        isStructure = true;
                        structure = new TwoLevelStructure()
                        {
                            Name = "两级架构",
                            Math_Psys = double.Parse(conditions[1]),
                            Math_Vpv_min = double.Parse(conditions[2]),
                            Math_Vpv_max = double.Parse(conditions[3]),
                            Math_Vg = double.Parse(conditions[4]),
                            Math_Vo = double.Parse(conditions[4]) / Math.Sqrt(3),
                            Math_fg = double.Parse(conditions[5]),
                            IsolatedDCDC_Q = double.Parse(conditions[6]),
                            DCAC_Ma_min = double.Parse(conditions[7]),
                            DCAC_Ma_max = double.Parse(conditions[8]),
                            DCAC_φ = double.Parse(conditions[9])
                        };
                        break;

                    default:
                        return;
                }

                //评估对象不符（架构/变换单元）则无法读取
                if ((structureListForDisplay.Count > 0 && !isStructure) || (converterListForDisplay.Count > 0 && isStructure)) //这里isStructureDisplay可能未初始化，用List的数量进行判断
                {
                    MessageBox.Show("与现在展示的评估对象不符（架构/变换单元）！");
                    return;
                }

                seriesNameList.Add(name);
                if (isStructure)
                {
                    for (int i = 1; i < info.Length; i++) //i=0为标题行
                    {
                        double efficiency = double.Parse(info[i][0]);
                        double volume = double.Parse(info[i][1]);
                        double cost = double.Parse(info[i][2]);
                        structure.AllDesignList.Add(efficiency, volume, cost, info[i]);
                    }
                    structureListForDisplay.Add(structure);
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
                    converterListForDisplay.Add(converter);
                }
                isStructureDisplay = isStructure;

                Display_Show_Display(); //更新图像显示
            }
        }

        /// <summary>
        /// 展示页面_向图像添加数据
        /// </summary>
        private void Display_Show_Add(IConverterDesignData[] data)
        {
            if (!isAllDisplay && !isParetoDisplay)
            {
                return;
            }

            double x;
            double y;
            double[] dataX = new double[data.Length];
            double[] dataY = new double[data.Length];
            List<double> paretoX = new List<double>();
            List<double> paretoY = new List<double>();

            ChartValues<ObservablePoint> values = new ChartValues<ObservablePoint>();
            switch (Display_Show_GraphCategory_ToolStripComboBox.Text)
            {
                case "成本-效率":
                    for (int i = 0; i < data.Length; i++)
                    {
                        x = data[i].Cost / 1e4;
                        y = data[i].Efficiency * 100;
                        values.Add(new ObservablePoint(x, y));

                        //排序
                        int j;
                        for (j = i - 1; j >= 0; j--)
                        {
                            if (x < dataX[j] || (x == dataX[j] && y > dataY[j]))
                            {
                                dataX[j + 1] = dataX[j];
                                dataY[j + 1] = dataY[j];
                            }
                            else
                            {
                                break;
                            }
                        }
                        dataX[j + 1] = x;
                        dataY[j + 1] = y;
                    }
                    //Pareto最优求解
                    for (int i = 0; i < dataX.Length; i++)
                    {
                        if (paretoY.Count == 0 || dataY[i] > paretoY[paretoY.Count - 1])
                        {
                            paretoX.Add(dataX[i]);
                            paretoY.Add(dataY[i]);
                        }
                    }
                    break;
                case "体积-效率":
                    for (int i = 0; i < data.Length; i++)
                    {
                        x = data[i].Volume;
                        y = data[i].Efficiency * 100;
                        values.Add(new ObservablePoint(x, y));
                        //排序
                        int j;
                        for (j = i - 1; j >= 0; j--)
                        {
                            if (x < dataX[j] || (x == dataX[j] && y > dataY[j]))
                            {
                                dataX[j + 1] = dataX[j];
                                dataY[j + 1] = dataY[j];
                            }
                            else
                            {
                                break;
                            }
                        }
                        dataX[j + 1] = x;
                        dataY[j + 1] = y;
                    }
                    //Pareto最优求解
                    for (int i = 0; i < dataX.Length; i++)
                    {
                        if (paretoY.Count == 0 || dataY[i] > paretoY[paretoY.Count - 1])
                        {
                            paretoX.Add(dataX[i]);
                            paretoY.Add(dataY[i]);
                        }
                    }
                    break;
                case "成本-体积":
                    for (int i = 0; i < data.Length; i++)
                    {
                        x = data[i].Cost / 1e4;
                        y = data[i].Volume;
                        values.Add(new ObservablePoint(x, y));
                        //排序
                        int j;
                        for (j = i - 1; j >= 0; j--)
                        {
                            if (x < dataX[j] || (x == dataX[j] && y < dataY[j]))
                            {
                                dataX[j + 1] = dataX[j];
                                dataY[j + 1] = dataY[j];
                            }
                            else
                            {
                                break;
                            }
                        }
                        dataX[j + 1] = x;
                        dataY[j + 1] = y;
                    }
                    //Pareto最优求解
                    for (int i = 0; i < dataX.Length; i++)
                    {
                        if (paretoY.Count == 0 || dataY[i] < paretoY[paretoY.Count - 1])
                        {
                            paretoX.Add(dataX[i]);
                            paretoY.Add(dataY[i]);
                        }
                    }
                    break;
            }

            if (isAllDisplay)
            {
                Display_Show_Graph_CartesianChart.Series.Add(new GScatterSeries
                {
                    Name = "Series_" + (displayNum + 1).ToString(),
                    Title = seriesNameList[displayNum],
                    Values = values.AsGearedValues().WithQuality(Quality.Low),
                    StrokeThickness = 0.5,
                    PointGeometry = null
                });
            }

            //Pareto前沿
            if (isParetoDisplay)
            {
                values = new ChartValues<ObservablePoint>();
                for (int i = 0; i < paretoX.Count; i++)
                {
                    values.Add(new ObservablePoint(paretoX[i], paretoY[i]));
                }
                Display_Show_Graph_CartesianChart.Series.Add(new LineSeries
                {
                    Name = null,
                    Title = seriesNameList[displayNum++] + "_Pareto前沿",
                    Values = values.AsGearedValues().WithQuality(Quality.Low),
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0,
                    PointGeometry = null
                });
            }
        }

        /// <summary>
        /// 展示页面，绘制评估结果图像
        /// </summary>
        private void Display_Show_Draw()
        {
            //重置图像
            Display_Show_Graph_Panel.Controls.Remove(Display_Show_Graph_CartesianChart);
            Display_Show_Graph_CartesianChart.Dispose();
            Display_Show_Graph_CartesianChart = new LiveCharts.WinForms.CartesianChart
            {
                BackColor = System.Drawing.Color.White,
                DisableAnimations = true,
                Font = new System.Drawing.Font("宋体", 12F),
                Location = new System.Drawing.Point(67, 88),
                Size = new System.Drawing.Size(946, 665),
                Zoom = ZoomingOptions.Xy,
                LegendLocation = LegendLocation.Bottom
            };
            Display_Show_Graph_CartesianChart.DataClick += Chart_OnDataClick; //添加评估图像点的点击事件

            Display_Show_Graph_Panel.Controls.Add(Display_Show_Graph_CartesianChart);
            Display_Show_Graph_Panel.Visible = false; //解决底色变黑
            Display_Show_Graph_Panel.Visible = true;
            switch (Display_Show_GraphCategory_ToolStripComboBox.Text) //设置横纵轴
            {
                case "成本-效率":

                    Display_Show_Graph_CartesianChart.AxisX.Add(new Axis
                    {
                        FontSize = 18F,
                        Title = "成本（万元）"
                    });
                    Display_Show_Graph_CartesianChart.AxisY.Add(new Axis
                    {
                        FontSize = 18F,
                        LabelFormatter = value => Math.Round(value, 8).ToString(),
                        Title = "中国效率（%）"
                    });
                    break;
                case "体积-效率":
                    Display_Show_Graph_CartesianChart.AxisX.Add(new Axis
                    {
                        FontSize = 18F,
                        Title = "体积（dm^3）"
                    });
                    Display_Show_Graph_CartesianChart.AxisY.Add(new Axis
                    {
                        FontSize = 18F,
                        LabelFormatter = value => Math.Round(value, 8).ToString(),
                        Title = "中国效率（%）"
                    });
                    break;
                case "成本-体积":
                    Display_Show_Graph_CartesianChart.AxisX.Add(new Axis
                    {
                        FontSize = 18F,
                        Title = "成本（万元）"
                    });
                    Display_Show_Graph_CartesianChart.AxisY.Add(new Axis
                    {
                        FontSize = 18F,
                        Title = "体积（dm^3）"
                    });
                    break;
            }

            //释放当前选取的点
            Display_Show_Preview_Main_Panel.Controls.Clear(); //清空预览面板显示
            Display_Show_Select_Button.Enabled = false; //更新控件可用状态
            Display_Show_Detail_Button.Enabled = false;

            displayNum = 0;
            //获取数据
            if (isStructureDisplay)
            {
                for (int n = 0; n < structureListForDisplay.Count; n++)
                {
                    Display_Show_Add(structureListForDisplay[n].AllDesignList.GetData());
                }
            }
            else
            {
                for (int n = 0; n < converterListForDisplay.Count; n++)
                {
                    Display_Show_Add(converterListForDisplay[n].AllDesignList.GetData());
                }
            }
        }

        /// <summary>
        /// 展示页面，更新结果图像显示
        /// </summary>
        private void Display_Show_Display()
        {
            //更新控件、图像
            if (Display_Show_GraphCategory_ToolStripComboBox.SelectedIndex >= 0)
            {
                Display_Show_Draw();
            }
            else
            {
                Display_Show_GraphCategory_ToolStripComboBox.SelectedIndex = 0;
            }

            //页面切换
            ChangePanel(3, Display_Show_Panel);
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
                Location = new System.Drawing.Point(6, 0),
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
                Location = new System.Drawing.Point(6, 0),
                Size = new System.Drawing.Size(164, 40),
                Text = title,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            });
            panel.Controls.Add(new Label
            {
                AutoSize = false,
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                Location = new System.Drawing.Point(176, 0),
                Size = new System.Drawing.Size(164, 40),
                Text = text,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            });
            return panel;
        }

        /// <summary>
        /// 根据当前选择的设计，显示预览信息
        /// </summary>
        private void Display_Show_Preview()
        {
            List<Panel> panelList = new List<Panel>(); //用于记录将要在预览面板中显示的信息（因为显示时设置了Dock=Top，而后生成的信息将显示在上方，所以在此处记录后，逆序添加控件）

            if (isStructureDisplay)
            {
                //生成预览面板显示信息
                panelList.Add(Display_Show_Preview_CreateTitle("性能表现："));
                panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (selectedStructure.EfficiencyCGC * 100).ToString("f2") + "%"));
                panelList.Add(Display_Show_Preview_CreateInfo("成本：", (selectedStructure.Cost / 1e4).ToString("f2") + "万元"));
                panelList.Add(Display_Show_Preview_CreateInfo("体积：", selectedStructure.Volume.ToString("f2") + "dm^3"));
                panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
                panelList.Add(Display_Show_Preview_CreateInfo("架构：", selectedStructure.Name));
                switch (selectedStructure.Name)
                {
                    case "三级架构":
                        panelList.Add(Display_Show_Preview_CreateInfo("母线电压：", selectedStructure.Math_Vbus.ToString() + "V"));
                        panelList.Add(Display_Show_Preview_CreateInfo("逆变直流侧电压：", selectedStructure.DCAC_Vinv.ToString() + "V"));
                        panelList.Add(Display_Show_Preview_CreateTitle("前级DC/DC："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)selectedStructure).DCDC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)selectedStructure).DCDC.Math_fs / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)selectedStructure).DCDC.Topology.GetName()));
                        panelList.Add(Display_Show_Preview_CreateTitle("隔离DC/DC："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("副边个数：", ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Math_No.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("品质因数：", ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Math_Q.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("谐振频率：", (((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Topology.GetName()));
                        panelList.Add(Display_Show_Preview_CreateTitle("逆变："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)selectedStructure).DCAC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)selectedStructure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)selectedStructure).DCAC.Topology.GetName()));
                        panelList.Add(Display_Show_Preview_CreateInfo("调制方式：", ((ThreeLevelStructure)selectedStructure).DCAC.Modulation.ToString()));
                        break;

                    case "两级架构":
                        panelList.Add(Display_Show_Preview_CreateInfo("逆变直流侧电压：", selectedStructure.DCAC_Vinv.ToString() + "V"));
                        panelList.Add(Display_Show_Preview_CreateTitle("隔离DC/DC："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("副边个数：", ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Math_No.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("品质因数：", ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Math_Q.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("谐振频率：", (((TwoLevelStructure)selectedStructure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Topology.GetName()));
                        panelList.Add(Display_Show_Preview_CreateTitle("逆变："));
                        panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((TwoLevelStructure)selectedStructure).DCAC.Number.ToString()));
                        panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((TwoLevelStructure)selectedStructure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz"));
                        panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((TwoLevelStructure)selectedStructure).DCAC.Topology.GetName()));
                        panelList.Add(Display_Show_Preview_CreateInfo("调制方式：", ((TwoLevelStructure)selectedStructure).DCAC.Modulation.ToString()));
                        break;
                }
            }
            else
            {
                //生成预览面板显示信息
                panelList.Add(Display_Show_Preview_CreateTitle("性能表现："));
                panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (selectedConverter.EfficiencyCGC * 100).ToString("f2") + "%"));
                panelList.Add(Display_Show_Preview_CreateInfo("成本：", (selectedConverter.Cost / 1e4).ToString("f2") + "万元"));
                panelList.Add(Display_Show_Preview_CreateInfo("体积：", selectedConverter.Volume.ToString("f2") + "dm^3"));
                panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
                panelList.Add(Display_Show_Preview_CreateInfo("模块数：", selectedConverter.Number.ToString()));
                if (selectedConverter.Name.Equals("隔离DC/DC变换单元_三级") || selectedConverter.Name.Equals("隔离DC/DC变换单元_两级"))
                {
                    panelList.Add(Display_Show_Preview_CreateInfo("副边个数：", ((IsolatedDCDCConverter)selectedConverter).Math_No.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("品质因数：", ((IsolatedDCDCConverter)selectedConverter).Math_Q.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("谐振频率：", (((IsolatedDCDCConverter)selectedConverter).Math_fr / 1e3).ToString("f1") + "kHz"));
                }
                else
                {
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (selectedConverter.Math_fs / 1e3).ToString("f1") + "kHz"));
                }
                panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", selectedConverter.Topology.GetName()));
                if (selectedConverter.Name.Equals("逆变单元"))
                {
                    panelList.Add(Display_Show_Preview_CreateInfo("调制方式：", ((DCACConverter)selectedConverter).Modulation.ToString()));
                }
            }

            //更新预览面板显示
            Display_Show_Preview_Main_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Show_Preview_Main_Panel.Controls.Add(panelList[i]);
            }

            //更新控件可用状态
            Display_Show_Select_Button.Enabled = true;
            Display_Show_Detail_Button.Enabled = true;
        }

        /// <summary>
        /// 评估图像点的点击事件
        /// 载入该点对应设计方案，并更新预览面板
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="chartPoint">点击的点</param>
        private void Chart_OnDataClick(object sender, ChartPoint chartPoint)
        {
            Series series = (Series)chartPoint.SeriesView;
            if (series.Name == null)
            {
                return;
            }
            int n = int.Parse(series.Name.Substring(7)) - 1;
            if (isStructureDisplay)
            {
                Structure structure = structureListForDisplay[n]; //获取当前选取的架构
                selectedStructure = structure.Clone(); //将复制用于比较和详情展示
                string[] configs = structure.AllDesignList.GetConfigs(chartPoint.Key); //查找对应设计方案
                int index = 0;
                selectedStructure.Load(configs, ref index); //读取设计方案
            }
            else
            {
                Converter converter = converterListForDisplay[n]; //获取当前选取的变换单元
                selectedConverter = converter.Clone(); //将复制用于比较和详情展示
                string[] configs = converter.AllDesignList.GetConfigs(chartPoint.Key); //查找对应设计方案
                int index = 0;
                selectedConverter.Load(configs, ref index); //读取设计方案
            }
            Display_Show_Preview();
        }

        /// <summary>
        /// 更新饼图显示
        /// </summary>
        /// <param name="pieChart">操作的饼图对象</param>
        /// <param name="dataList">数据</param>
        private void DisplayPieChart(LiveCharts.WinForms.PieChart pieChart, InfoList info)
        {
            //string labelPoint(ChartPoint chartPoint) => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation); //饼图数据标签显示格式
            SeriesCollection series = new SeriesCollection();
            for (int i = 0; i < info.Size; i++)
            {
                if (Math.Round((double)info[i].Content, 2) > 0)
                {
                    series.Add(new PieSeries
                    {
                        FontSize = 12,
                        Title = info[i].Title,
                        Values = new ChartValues<double> { (double)info[i].Content },
                        DataLabels = true,
                        //LabelPoint = labelPoint
                    });
                }
            }
            pieChart.Series = series;
            pieChart.Font = new System.Drawing.Font("宋体", 10.5F);
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
            panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (selectedStructure.EfficiencyCGC * 100).ToString("f2") + "%"));
            panelList.Add(Display_Show_Preview_CreateInfo("成本：", (selectedStructure.Cost / 1e4).ToString("f2") + "万元"));
            panelList.Add(Display_Show_Preview_CreateInfo("体积：", selectedStructure.Volume.ToString("f2") + "dm^3"));
            panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
            panelList.Add(Display_Show_Preview_CreateInfo("架构：", selectedStructure.Name));
            switch (selectedStructure.Name)
            {
                case "三级架构":
                    panelList.Add(Display_Show_Preview_CreateInfo("母线电压：", selectedStructure.Math_Vbus.ToString() + "V"));
                    panelList.Add(Display_Show_Preview_CreateInfo("逆变直流侧电压：", selectedStructure.DCAC_Vinv.ToString() + "V"));
                    panelList.Add(Display_Show_Preview_CreateTitle("前级DC/DC："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)selectedStructure).DCDC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)selectedStructure).DCDC.Math_fs / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)selectedStructure).DCDC.Topology.GetName()));
                    panelList.Add(Display_Show_Preview_CreateTitle("隔离DC/DC："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("副边个数：", ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Math_No.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("品质因数：", ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Math_Q.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("谐振频率：", (((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Topology.GetName()));
                    panelList.Add(Display_Show_Preview_CreateTitle("逆变："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((ThreeLevelStructure)selectedStructure).DCAC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((ThreeLevelStructure)selectedStructure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((ThreeLevelStructure)selectedStructure).DCAC.Modulation.ToString()));
                    break;

                case "两级架构":
                    panelList.Add(Display_Show_Preview_CreateInfo("逆变直流侧电压：", selectedStructure.DCAC_Vinv.ToString() + "V"));
                    panelList.Add(Display_Show_Preview_CreateTitle("隔离DC/DC："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("副边个数：", ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Math_No.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("品质因数：", ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Math_Q.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((TwoLevelStructure)selectedStructure).IsolatedDCDC.Math_fr / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Topology.GetName()));
                    panelList.Add(Display_Show_Preview_CreateTitle("逆变："));
                    panelList.Add(Display_Show_Preview_CreateInfo("模块数：", ((TwoLevelStructure)selectedStructure).DCAC.Number.ToString()));
                    panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (((TwoLevelStructure)selectedStructure).DCAC.Math_fs / 1e3).ToString("f1") + "kHz"));
                    panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", ((TwoLevelStructure)selectedStructure).DCAC.Modulation.ToString()));
                    break;
            }
            //更新面板显示
            Display_Detail_System_Right_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Detail_System_Right_Panel.Controls.Add(panelList[i]);
            }

            //生成图像
            DisplayPieChart(Display_Detail_System_CostBreakdown_PieChart, selectedStructure.GetCostBreakdown()); //成本分布饼图
            DisplayPieChart(Display_Detail_System_VolumeBreakdown_PieChart, selectedStructure.GetVolumeBreakdown()); //体积分布饼图

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
                    Title = "Vin=" + selectedStructure.Math_Vpv_min + "V",
                    Fill = Brushes.Transparent,
                    Values = values,
                    PointGeometry = null
                }
            };
            Display_Detail_System_LoadVsEfficiency_CartesianChart.AxisX = new AxesCollection
            {
                new Axis
                {
                    FontSize = 18F,
                    Title = "负载（%）"
                }
            };
            Display_Detail_System_LoadVsEfficiency_CartesianChart.AxisY = new AxesCollection
            {
                new Axis
                {
                    FontSize = 18F,
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Detail_System_LoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Bottom;

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
            panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (selectedConverter.EfficiencyCGC * 100).ToString("f2") + "%"));
            panelList.Add(Display_Show_Preview_CreateInfo("成本：", (selectedConverter.Cost / 1e4).ToString("f2") + "万元"));
            panelList.Add(Display_Show_Preview_CreateInfo("体积：", selectedConverter.Volume.ToString("f2") + "dm^3"));
            panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
            panelList.Add(Display_Show_Preview_CreateInfo("模块数：", selectedConverter.Number.ToString()));
            panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (selectedConverter.Math_fs / 1e3).ToString("f1") + "kHz"));
            panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", selectedConverter.Topology.GetName()));
            if (isComponentInfoDisplay)
            {
                InfoPackage info = selectedConverter.GetComponentConfigInfo();
                for (int i = 0; i < info.Size; i++)
                {
                    InfoList list = info[i];
                    panelList.Add(Display_Show_Preview_CreateTitle(list.Title + "："));
                    for (int j = 0; j < list.Size; j++)
                    {
                        panelList.Add(Display_Show_Preview_CreateInfo(list[j].Title + "：", (string)list[j].Content));
                    }
                }
            }

            //更新面板显示
            Display_Detail_DCDC_Right_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Detail_DCDC_Right_Panel.Controls.Add(panelList[i]);
            }

            //生成图像
            DisplayPieChart(Display_Detail_DCDC_CostBreakdown_PieChart, selectedConverter.GetCostBreakdown()); //成本分布饼图
            DisplayPieChart(Display_Detail_DCDC_VolumeBreakdown_PieChart, selectedConverter.GetVolumeBreakdown()); //体积分布饼图

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
                    Title = "Vin=" + ((DCDCConverter)selectedConverter).Math_Vin_min + "V",
                    Fill = Brushes.Transparent,
                    Values = values,
                    PointGeometry = null
                }
            };
            Display_Detail_DCDC_LoadVsEfficiency_CartesianChart.AxisX = new AxesCollection
            {
                new Axis
                {
                    FontSize = 18F,
                    Title = "负载（%）"
                }
            };
            Display_Detail_DCDC_LoadVsEfficiency_CartesianChart.AxisY = new AxesCollection
            {
                new Axis
                {
                    FontSize = 18F,
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Detail_DCDC_LoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Bottom;

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
            panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (selectedConverter.EfficiencyCGC * 100).ToString("f2") + "%"));
            panelList.Add(Display_Show_Preview_CreateInfo("成本：", (selectedConverter.Cost / 1e4).ToString("f2") + "万元"));
            panelList.Add(Display_Show_Preview_CreateInfo("体积：", selectedConverter.Volume.ToString("f2") + "dm^3"));
            panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
            panelList.Add(Display_Show_Preview_CreateInfo("模块数：", selectedConverter.Number.ToString()));
            panelList.Add(Display_Show_Preview_CreateInfo("副边个数：", ((IsolatedDCDCConverter)selectedConverter).Math_No.ToString()));
            panelList.Add(Display_Show_Preview_CreateInfo("品质因数：", ((IsolatedDCDCConverter)selectedConverter).Math_Q.ToString()));
            panelList.Add(Display_Show_Preview_CreateInfo("谐振频率：", (((IsolatedDCDCConverter)selectedConverter).Math_fr / 1e3).ToString("f1") + "kHz"));
            if (isComponentInfoDisplay)
            {
                InfoPackage info = selectedConverter.GetComponentConfigInfo();
                for (int i = 0; i < info.Size; i++)
                {
                    InfoList list = info[i];
                    panelList.Add(Display_Show_Preview_CreateTitle(list.Title + "："));
                    for (int j = 0; j < list.Size; j++)
                    {
                        panelList.Add(Display_Show_Preview_CreateInfo(list[j].Title + "：", (string)list[j].Content));
                    }
                }
            }

            panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", selectedConverter.Topology.GetName()));
            //更新面板显示
            Display_Detail_IsolatedDCDC_Right_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Detail_IsolatedDCDC_Right_Panel.Controls.Add(panelList[i]);
            }

            //生成图像
            DisplayPieChart(Display_Detail_IsolatedDCDC_CostBreakdown_PieChart, selectedConverter.GetCostBreakdown()); //成本分布饼图
            DisplayPieChart(Display_Detail_IsolatedDCDC_VolumeBreakdown_PieChart, selectedConverter.GetVolumeBreakdown()); //体积分布饼图

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
                    Fill = Brushes.Transparent,
                    Values = values,
                    PointGeometry = null
                }
            };
            Display_Detail_IsolatedDCDC_LoadVsEfficiency_CartesianChart.AxisX = new AxesCollection
            {
                new Axis
                {
                    FontSize = 18F,
                    Title = "负载（%）"
                }
            };
            Display_Detail_IsolatedDCDC_LoadVsEfficiency_CartesianChart.AxisY = new AxesCollection
            {
                new Axis
                {
                    FontSize = 18F,
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Detail_IsolatedDCDC_LoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Bottom;

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
            panelList.Add(Display_Show_Preview_CreateInfo("中国效率：", (selectedConverter.EfficiencyCGC * 100).ToString("f2") + "%"));
            panelList.Add(Display_Show_Preview_CreateInfo("成本：", (selectedConverter.Cost / 1e4).ToString("f2") + "万元"));
            panelList.Add(Display_Show_Preview_CreateInfo("体积：", selectedConverter.Volume.ToString("f2") + "dm^3"));
            panelList.Add(Display_Show_Preview_CreateTitle("设计参数："));
            panelList.Add(Display_Show_Preview_CreateInfo("模块数：", selectedConverter.Number.ToString()));
            panelList.Add(Display_Show_Preview_CreateInfo("开关频率：", (selectedConverter.Math_fs / 1e3).ToString("f1") + "kHz"));
            panelList.Add(Display_Show_Preview_CreateInfo("拓扑：", selectedConverter.Topology.GetName()));
            panelList.Add(Display_Show_Preview_CreateInfo("调制方式：", ((DCACConverter)selectedConverter).Modulation.ToString()));
            if (isComponentInfoDisplay)
            {
                InfoPackage info = selectedConverter.GetComponentConfigInfo();
                for (int i = 0; i < info.Size; i++)
                {
                    InfoList list = info[i];
                    panelList.Add(Display_Show_Preview_CreateTitle(list.Title + "："));
                    for (int j = 0; j < list.Size; j++)
                    {
                        panelList.Add(Display_Show_Preview_CreateInfo(list[j].Title + "：", (string)list[j].Content));
                    }
                }
            }

            //更新面板显示
            Display_Detail_DCAC_Right_Panel.Controls.Clear(); //清空原有控件
            for (int i = panelList.Count - 1; i >= 0; i--) //逆序添加控件，以正常显示
            {
                Display_Detail_DCAC_Right_Panel.Controls.Add(panelList[i]);
            }

            //生成图像
            DisplayPieChart(Display_Detail_DCAC_CostBreakdown_PieChart, selectedConverter.GetCostBreakdown()); //成本分布饼图
            DisplayPieChart(Display_Detail_DCAC_VolumeBreakdown_PieChart, selectedConverter.GetVolumeBreakdown()); //体积分布饼图

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
                    Fill = Brushes.Transparent,
                    Values = values,
                    PointGeometry = null
                }
            };
            Display_Detail_DCAC_LoadVsEfficiency_CartesianChart.AxisX = new AxesCollection
            {
                new Axis
                {
                    FontSize = 18F,
                    Title = "负载（%）"
                }
            };
            Display_Detail_DCAC_LoadVsEfficiency_CartesianChart.AxisY = new AxesCollection
            {
                new Axis
                {
                    FontSize = 18F,
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Detail_DCAC_LoadVsEfficiency_CartesianChart.LegendLocation = LegendLocation.Bottom;

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

            if (isStructureDisplay)
            {
                //生成数据
                selectedStructure.Operate(load, Vin);

                //更新显示
                //更新图像
                DisplayPieChart(Display_Detail_System_LossBreakdown_PieChart, selectedStructure.GetLossBreakdown()); //整体系统损耗分布饼图
                switch (selectedStructure.Name)
                {
                    case "三级架构":
                        DisplayPieChart(Display_Detail_DCDC_LossBreakdown_PieChart, ((ThreeLevelStructure)selectedStructure).DCDC.GetModuleLossBreakdown()); //前级DC/DC损耗分布饼图
                        DisplayPieChart(Display_Detail_IsolatedDCDC_LossBreakdown_PieChart, ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.GetModuleLossBreakdown()); //隔离DC/DC损耗分布信息
                        DisplayPieChart(Display_Detail_DCAC_LossBreakdown_PieChart, ((ThreeLevelStructure)selectedStructure).DCAC.GetModuleLossBreakdown()); //DC/AC损耗分布信息
                        break;

                    case "两级架构":
                        DisplayPieChart(Display_Detail_IsolatedDCDC_LossBreakdown_PieChart, ((TwoLevelStructure)selectedStructure).IsolatedDCDC.GetModuleLossBreakdown()); //隔离DC/DC损耗分布信息
                        DisplayPieChart(Display_Detail_DCAC_LossBreakdown_PieChart, ((TwoLevelStructure)selectedStructure).DCAC.GetModuleLossBreakdown()); //DC/AC损耗分布信息
                        break;
                }
            }
            else
            {
                //更新显示                
                switch (selectedConverter.Name)
                {
                    case "前级DC/DC变换单元_三级":
                        selectedConverter.Operate(load, Vin);
                        DisplayPieChart(Display_Detail_DCDC_LossBreakdown_PieChart, selectedConverter.GetModuleLossBreakdown()); //前级DC/DC损耗分布饼图
                        break;
                    case "隔离DC/DC变换单元_三级":
                        selectedConverter.Operate(load);
                        DisplayPieChart(Display_Detail_IsolatedDCDC_LossBreakdown_PieChart, selectedConverter.GetModuleLossBreakdown()); //隔离DC/DC损耗分布饼图
                        break;
                    case "隔离DC/DC变换单元_两级":
                        selectedConverter.Operate(load, Vin);
                        DisplayPieChart(Display_Detail_IsolatedDCDC_LossBreakdown_PieChart, selectedConverter.GetModuleLossBreakdown()); //隔离DC/DC损耗分布饼图
                        break;
                    case "逆变单元":
                        selectedConverter.Operate(load);
                        DisplayPieChart(Display_Detail_DCAC_LossBreakdown_PieChart, selectedConverter.GetModuleLossBreakdown()); //逆变损耗分布饼图
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
            isStructureEvaluation = true;
            ChangePanel(2, Estimate_Step1_Panel);
        }

        private void Estimate_Ready_Converter_button_Click(object sender, EventArgs e)
        {
            isStructureEvaluation = false;
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
                evaluationObjectName = Estimate_Step1_CheckedListBox.GetItemText(Estimate_Step1_CheckedListBox.CheckedItems[0]);
                switch (evaluationObjectName)
                {
                    case "三级架构":
                        Estimate_Step2_Group1_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = true;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = false;
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
                evaluationObjectName = Estimate_Step1B_CheckedListBox.GetItemText(Estimate_Step1B_CheckedListBox.CheckedItems[0]);
                switch (evaluationObjectName)
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
                        Estimate_Step2_Group2_Item3_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Enabled = true;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group2_Item4_Left_CheckBox.Enabled = false;
                        Estimate_Step2_Group3_Item1_Left_CheckBox.Enabled = false;

                        Estimate_Step2_Group1_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item3_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item4_CheckBox.Checked = false;
                        Estimate_Step2_Group3_Item1_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item1_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item2_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group1_Item3_Left_CheckBox.Checked = false;
                        Estimate_Step2_Group2_Item1_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item2_Left_CheckBox.Checked = true;
                        Estimate_Step2_Group2_Item3_Left_CheckBox.Checked = false;
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
            if (isStructureEvaluation)
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

            if (isStructureEvaluation)
            {
                if (evaluationObjectName.Equals("三级架构") && DCDC_topologyList.Count == 0)
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
                    switch (evaluationObjectName)
                    {
                        case "三级架构":
                            Estimate_Step3_Vbusmin_TextBox.Enabled = true;
                            Estimate_Step3_Vbusmax_TextBox.Enabled = true;
                            Estimate_Step3_DCDCMinNumber_TextBox.Enabled = true;
                            Estimate_Step3_DCDCMaxNumber_TextBox.Enabled = true;
                            Estimate_Step3_DCDCMinFrequency_TextBox.Enabled = true;
                            Estimate_Step3_DCDCMaxFrequency_TextBox.Enabled = true;
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
                            Estimate_Step3_DCACMamin_TextBox.Text = "0.7";
                            Estimate_Step3_DCACMamax_TextBox.Text = "0.9";
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
                            Estimate_Step3_DCACMamin_TextBox.Text = "0.95";
                            Estimate_Step3_DCACMamax_TextBox.Text = "0.95";
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
                if (evaluationObjectName.Equals("前级DC/DC变换单元_三级") && DCDC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项前级DC/DC拓扑");
                    return;
                }
                else if ((evaluationObjectName.Equals("隔离DC/DC变换单元_三级") || evaluationObjectName.Equals("隔离DC/DC变换单元_两级")) && isolatedDCDC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项隔离DC/DC拓扑");
                    return;
                }
                else if (evaluationObjectName.Equals("逆变单元") && DCAC_topologyList.Count == 0)
                {
                    MessageBox.Show("请至少选择一项逆变拓扑");
                    return;
                }
                else
                {
                    Estimate_Step3B_Converter_Label.Text = evaluationObjectName;
                    switch (evaluationObjectName)
                    {
                        case "前级DC/DC变换单元_三级":
                            Estimate_Step3B_VinRange_Panel.Visible = true;
                            Estimate_Step3B_Vin_Panel.Visible = false;
                            Estimate_Step3B_Q_Panel.Visible = false;
                            Estimate_Step3B_Secondary_Panel.Visible = false;
                            Estimate_Step3B_Ma_Panel.Visible = false;

                            Estimate_Step3B_Vinmin_TextBox.Text = "860";
                            Estimate_Step3B_Vinmax_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Label.Text = "输出电压";
                            Estimate_Step3B_Vo_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Unit_Label.Text = "V";
                            Estimate_Step3B_MinNumber_TextBox.Text = "1";
                            Estimate_Step3B_MaxNumber_TextBox.Text = "120";
                            Estimate_Step3B_Frequency_Label.Text = "开关频率";
                            Estimate_Step3B_MinFrequency_TextBox.Text = "1";
                            Estimate_Step3B_MaxFrequency_TextBox.Text = "100";

                            DCDC_topologyRange = DCDC_topologyList.ToArray();
                            break;
                        case "隔离DC/DC变换单元_三级":
                            Estimate_Step3B_VinRange_Panel.Visible = false;
                            Estimate_Step3B_Vin_Panel.Visible = true;
                            Estimate_Step3B_Q_Panel.Visible = true;
                            Estimate_Step3B_Secondary_Panel.Visible = true;
                            Estimate_Step3B_Ma_Panel.Visible = false;

                            Estimate_Step3B_Vin_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Label.Text = "输出电压";
                            Estimate_Step3B_Vo_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Unit_Label.Text = "V";
                            Estimate_Step3B_Q_TextBox.Text = "1";
                            Estimate_Step3B_MinSecondary_TextBox.Text = "1";
                            Estimate_Step3B_MaxSecondary_TextBox.Text = "1";
                            Estimate_Step3B_MinNumber_TextBox.Text = "1";
                            Estimate_Step3B_MaxNumber_TextBox.Text = "40";
                            Estimate_Step3B_Frequency_Label.Text = "谐振频率";
                            Estimate_Step3B_MinFrequency_TextBox.Text = "1";
                            Estimate_Step3B_MaxFrequency_TextBox.Text = "100";

                            isolatedDCDC_topologyRange = isolatedDCDC_topologyList.ToArray();
                            break;
                        case "隔离DC/DC变换单元_两级":
                            Estimate_Step3B_VinRange_Panel.Visible = true;
                            Estimate_Step3B_Vin_Panel.Visible = false;
                            Estimate_Step3B_Q_Panel.Visible = true;
                            Estimate_Step3B_Secondary_Panel.Visible = false;
                            Estimate_Step3B_Ma_Panel.Visible = false;

                            Estimate_Step3B_Vinmin_TextBox.Text = "860";
                            Estimate_Step3B_Vinmax_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Label.Text = "输出电压";
                            Estimate_Step3B_Vo_TextBox.Text = "1300";
                            Estimate_Step3B_Vo_Unit_Label.Text = "V";
                            Estimate_Step3B_Q_TextBox.Text = "1";
                            Estimate_Step3B_MinNumber_TextBox.Text = "20";
                            Estimate_Step3B_MaxNumber_TextBox.Text = "20";
                            Estimate_Step3B_Frequency_Label.Text = "谐振频率";
                            Estimate_Step3B_MinFrequency_TextBox.Text = "25";
                            Estimate_Step3B_MaxFrequency_TextBox.Text = "25";

                            isolatedDCDC_topologyRange = isolatedDCDC_topologyList.ToArray();
                            break;
                        case "逆变单元":
                            Estimate_Step3B_VinRange_Panel.Visible = false;
                            Estimate_Step3B_Vin_Panel.Visible = true;
                            Estimate_Step3B_Q_Panel.Visible = false;
                            Estimate_Step3B_Secondary_Panel.Visible = false;
                            Estimate_Step3B_Ma_Panel.Visible = true;

                            Estimate_Step3B_Vin_TextBox.Text = "1000";
                            Estimate_Step3B_Vo_Label.Text = "并网电压";
                            Estimate_Step3B_Vo_TextBox.Text = "35";
                            Estimate_Step3B_Vo_Unit_Label.Text = "kV";
                            Estimate_Step3B_Mamin_TextBox.Text = "0.7";
                            Estimate_Step3B_Mamax_TextBox.Text = "0.9";
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
            if (isStructureEvaluation)
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
            Estimate_Result_AddDisplay_Button.Enabled = false;
            Estimate_Result_NewDisplay_Button.Enabled = false;

            //切换显示
            Estimate_Result_Print_RichTextBox.Text = "";
            ChangePanel(2, Estimate_Result_Panel);
            evaluationThread = new Thread(new ThreadStart(Estimate_Result_Evaluate)) //多线程
            {
                IsBackground = true
            };
            evaluationThread.Start();
            //Estimate_Result_Evaluate(); //不使用多线程
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
            Estimate_Result_NewDisplay_Button.Enabled = false;
            Estimate_Result_AddDisplay_Button.Enabled = false;

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
            if (isStructureEvaluation)
            {
                structureForEvaluation.Save();
            }
            else
            {
                converterForEvaluation.Save();
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
                if (isStructureEvaluation)
                {
                    structureForEvaluation.Save(path, name);
                }
                else
                {
                    converterForEvaluation.Save(path, name);
                }
            }
        }

        private void Estimate_Result_AddDisplay_Button_Click(object sender, EventArgs e)
        {
            //评估对象不符（架构/变换单元）则无法加入
            if ((structureListForDisplay.Count > 0 && !isStructureEvaluation) || (converterListForDisplay.Count > 0 && isStructureEvaluation)) //这里isStructureDisplay可能未初始化，用List的数量进行判断
            {
                MessageBox.Show("与现在展示的评估对象不符（架构/变换单元）！");
                return;
            }
            Estimate_Result_Display();
        }

        private void Estimate_Result_NewDisplay_Button_Click(object sender, EventArgs e)
        {
            seriesNameList = new List<string>();
            structureListForDisplay = new List<Structure>();
            converterListForDisplay = new List<Converter>();
            structureListForContrast = new List<Structure>();
            converterListForContrast = new List<Converter>();
            Estimate_Result_Display();
            Display_Show_Contrast_Button.Enabled = false;
            Display_Show_Clear_Button.Enabled = false;
        }

        private void Display_Show_Restart_Button_Click(object sender, EventArgs e)
        {
            ChangePanel(3, Display_Ready_Panel);
        }

        private void Display_Show_Add_Button_Click(object sender, EventArgs e)
        {
            Display_Show_Load();
        }

        private void Display_Show_GraphCategory_ToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Display_Show_Draw();
        }

        private void Display_Ready_Load_Button_Click(object sender, EventArgs e)
        {
            seriesNameList = new List<string>();
            structureListForDisplay = new List<Structure>();
            converterListForDisplay = new List<Converter>();
            structureListForContrast = new List<Structure>();
            converterListForContrast = new List<Converter>();
            Display_Show_Load();
            Display_Show_Contrast_Button.Enabled = false;
            Display_Show_Clear_Button.Enabled = false;
        }

        private void Display_Show_Refresh_ToolStripButton_Click(object sender, EventArgs e)
        {
            Display_Show_Display();
        }

        private void Display_Show_All_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isAllDisplay)
            {
                isAllDisplay = false;
                Display_Show_All_ToolStripMenuItem.Text = "显示评估结果";
            }
            else
            {
                isAllDisplay = true;
                Display_Show_All_ToolStripMenuItem.Text = "隐藏评估结果";
            }
            Display_Show_Display();
        }

        private void Display_Show_Pareto_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isParetoDisplay)
            {
                isParetoDisplay = false;
                Display_Show_Pareto_ToolStripMenuItem.Text = "显示Pareto前沿";
            }
            else
            {
                isParetoDisplay = true;
                Display_Show_Pareto_ToolStripMenuItem.Text = "隐藏Pareto前沿";
            }
            Display_Show_Display();
        }

        private void Display_Show_SelectYmax_ToolStripButton_Click(object sender, EventArgs e)
        {
            if (isStructureDisplay)
            {
                //查找效率最高设计方案
                string[] configs = new string[1];
                for (int i = 0; i < structureListForDisplay.Count; i++)
                {
                    string[] con = structureListForDisplay[i].AllDesignList.GetMaxEfficiencyConfigs();
                    if (i == 0 || double.Parse(con[0]) > double.Parse(configs[0]))
                    {
                        configs = con;
                        selectedStructure = structureListForDisplay[i].Clone();
                    }
                }
                int index = 0;
                selectedStructure.Load(configs, ref index); //读取设计方案
            }
            else
            {
                //查找效率最高设计方案
                string[] configs = new string[1];
                for (int i = 0; i < converterListForDisplay.Count; i++)
                {
                    string[] con = converterListForDisplay[i].AllDesignList.GetMaxEfficiencyConfigs();
                    if (i == 0 || double.Parse(con[0]) > double.Parse(configs[0]))
                    {
                        configs = con;
                        selectedConverter = converterListForDisplay[i].Clone();
                    }
                }
                int index = 0;
                selectedConverter.Load(configs, ref index); //读取设计方案
            }
            Display_Show_Preview();
        }

        /// <summary>
        /// 对比页面，在表格中插入标题行
        /// </summary>
        /// <param name="table">对象表格</param>
        /// <param name="text">文字信息</param>
        /// <param name="height">行高</param>
        private void Display_Show_Contrast_InsertTitle(TableLayoutPanel table, string text, float height = 40)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            panel.Controls.Add(new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                Text = text,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            });
            table.SetColumnSpan(panel, table.ColumnCount);
            table.Controls.Add(panel, 0, table.RowCount++);
        }

        /// <summary>
        /// 对比页面，在表格中插入单元格内容
        /// </summary>
        /// <param name="table">对象表格</param>
        /// <param name="column">列</param>
        /// <param name="row">行</param>
        /// <param name="text">文字信息</param>
        /// <param name="height">行高</param>
        private void Display_Show_Contrast_InsertCell(TableLayoutPanel table, int column, int row, string text, float height = 40)
        {
            while (table.RowCount <= row)
            {
                table.RowCount++;
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            }

            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            Label label = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                Name = "Label_" + column + "_" + row,
                Text = text,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(label);
            //突显差异
            for (int i = 1; i < column; i++)
            {
                Control control = table.Controls.Find("Label_" + i + "_" + row, true)[0];
                if (!text.Equals(control.Text))
                {
                    control.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
                    label.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
                    control = table.Controls.Find("Label_" + 0 + "_" + row, true)[0];
                    control.Font = new System.Drawing.Font("Times New Roman", 14.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
                }
            }
            table.Controls.Add(panel, column, row);
        }

        /// <summary>
        /// 对比页面，在表格中插入单元格控件
        /// </summary>
        /// <param name="table">对象表格</param>
        /// <param name="column">列</param>
        /// <param name="row">行</param>
        /// <param name="control">控件</param>
        /// <param name="height">行高</param>
        private void Display_Show_Contrast_InsertCell(TableLayoutPanel table, int column, int row, Control control, float height = 400)
        {
            while (table.RowCount <= row)
            {
                table.RowCount++;
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            }

            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            panel.Controls.Add(control);
            table.Controls.Add(panel, column, row);
        }

        /// <summary>
        /// 对比页面，在表格中插入整行控件
        /// </summary>
        /// <param name="table">对象表格</param>
        /// <param name="row">行</param>
        /// <param name="control">控件</param>
        /// <param name="height">行高</param>
        private void Display_Show_Contrast_InsertRow(TableLayoutPanel table, int row, Control control, float height = 450)
        {
            while (table.RowCount <= row)
            {
                table.RowCount++;
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            }

            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            panel.Controls.Add(control);
            table.SetColumnSpan(panel, table.ColumnCount - 1);
            table.Controls.Add(panel, 1, row);
        }

        private void Display_Show_Contrast_Button_Click(object sender, EventArgs e)
        {
            int m;
            int row;
            if (isStructureDisplay)
            {
                m = structureListForContrast.Count;
                for (int i = 1; i < m; i++)
                {
                    if (!structureListForContrast[0].Name.Equals(structureListForContrast[i].Name))
                    {
                        MessageBox.Show("当前只能进行同种架构的比较！");
                        return;
                    }
                }
            }
            else
            {
                m = converterListForContrast.Count;
                for (int i = 1; i < m; i++)
                {
                    if (!converterListForContrast[0].Name.Equals(converterListForContrast[i].Name))
                    {
                        MessageBox.Show("当前只能进行同种变换单元的比较！");
                        return;
                    }
                }
            }

            TableLayoutPanel table = new TableLayoutPanel()
            {
                AutoScroll = true,
                AutoSize = false,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.OutsetDouble,
                Dock = DockStyle.Fill,
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(1430, 807)
            };
            table.ColumnCount = 5;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21F));
            table.RowCount = 0;
            row = 0;
            Display_Show_Contrast_InsertTitle(table, "性能表现");
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, "设计"+(i+1));
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "中国效率（%）");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, (structureListForContrast[i].EfficiencyCGC * 100).ToString("f2"));
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "成本（万元）");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, (structureListForContrast[i].Cost / 1e4).ToString("f2"));
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "体积（dm^3）");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, structureListForContrast[i].Volume.ToString("f2"));
            }
            row++;
            Display_Show_Contrast_InsertTitle(table, "设计参数");
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "架构");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, structureListForContrast[i].Name);
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "母线电压（V）");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, structureListForContrast[i].Math_Vbus.ToString());
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "逆变直流侧电压（V）");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, structureListForContrast[i].DCAC_Vinv.ToString());
            }
            row++;
            Display_Show_Contrast_InsertTitle(table, "前级DC/DC");
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "模块数");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).DCDC.Number.ToString());
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "开关频率（kHz）");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, (((ThreeLevelStructure)structureListForContrast[i]).DCDC.Math_fs / 1e3).ToString("f1"));
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "拓扑");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).DCDC.Topology.GetName());
            }
            if (isComponentInfoDisplay)
            {
                InfoPackage[] info = new InfoPackage[m];
                for (int k = 0; k < m; k++)
                {
                    info[k] = ((ThreeLevelStructure)structureListForContrast[k]).DCDC.GetComponentConfigInfo();
                }
                
                for (int i = 0; i < info[0].Size; i++)
                {
                    InfoList[] list = new InfoList[m];
                    for (int k = 0; k < m; k++)
                    {
                        list[k] = info[k][i];
                    }
                    
                    for (int j = 0; j < list[0].Size; j++)
                    {
                        row++;
                        Display_Show_Contrast_InsertCell(table, 0, row, "[" + list[0].Title + "]" + list[0][j].Title);
                        for (int k = 0; k < m; k++)
                        {
                            Display_Show_Contrast_InsertCell(table, k + 1, row, (string)list[k][j].Content);
                        }
                    }
                }
            }
            row++;
            Display_Show_Contrast_InsertTitle(table, "隔离DC/DC");
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "模块数");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).IsolatedDCDC.Number.ToString());
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "副边个数");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).IsolatedDCDC.Math_No.ToString());
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "品质因数");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).IsolatedDCDC.Math_Q.ToString());
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "谐振频率（kHz）");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, (((ThreeLevelStructure)structureListForContrast[i]).IsolatedDCDC.Math_fr / 1e3).ToString("f1"));
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "拓扑");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).IsolatedDCDC.Topology.GetName());
            }
            if (isComponentInfoDisplay)
            {
                InfoPackage[] info = new InfoPackage[m];
                for (int k = 0; k < m; k++)
                {
                    info[k] = ((ThreeLevelStructure)structureListForContrast[k]).IsolatedDCDC.GetComponentConfigInfo();
                }

                for (int i = 0; i < info[0].Size; i++)
                {
                    InfoList[] list = new InfoList[m];
                    for (int k = 0; k < m; k++)
                    {
                        list[k] = info[k][i];
                    }

                    for (int j = 0; j < list[0].Size; j++)
                    {
                        row++;
                        Display_Show_Contrast_InsertCell(table, 0, row, "[" + list[0].Title + "]" + list[0][j].Title);
                        for (int k = 0; k < m; k++)
                        {
                            Display_Show_Contrast_InsertCell(table, k + 1, row, (string)list[k][j].Content);
                        }
                    }
                }
            }
            row++;
            Display_Show_Contrast_InsertTitle(table, "逆变");
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "模块数");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).DCAC.Number.ToString());
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "开关频率（kHz）");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, (((ThreeLevelStructure)structureListForContrast[i]).DCAC.Math_fs / 1e3).ToString("f1"));
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "拓扑");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).DCAC.Topology.GetName());
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "调制方式");
            for (int i = 0; i < m; i++)
            {
                Display_Show_Contrast_InsertCell(table, i + 1, row, ((ThreeLevelStructure)structureListForContrast[i]).DCAC.Modulation.ToString());
            }
            if (isComponentInfoDisplay)
            {
                InfoPackage[] info = new InfoPackage[m];
                for (int k = 0; k < m; k++)
                {
                    info[k] = ((ThreeLevelStructure)structureListForContrast[k]).DCAC.GetComponentConfigInfo();
                }

                for (int i = 0; i < info[0].Size; i++)
                {
                    InfoList[] list = new InfoList[m];
                    for (int k = 0; k < m; k++)
                    {
                        list[k] = info[k][i];
                    }

                    for (int j = 0; j < list[0].Size; j++)
                    {
                        row++;
                        Display_Show_Contrast_InsertCell(table, 0, row, "[" + list[0].Title + "]" + list[0][j].Title);
                        for (int k = 0; k < m; k++)
                        {
                            Display_Show_Contrast_InsertCell(table, k + 1, row, (string)list[k][j].Content);
                        }
                    }
                }
            }
            row++;
            Display_Show_Contrast_InsertTitle(table, "负载-效率曲线");
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "整体", 450);
            LiveCharts.WinForms.CartesianChart chartAll = new LiveCharts.WinForms.CartesianChart()
            {
                BackColor = System.Drawing.Color.White,
                Location = new System.Drawing.Point(300, 0),
                Font = new System.Drawing.Font("宋体", 10.5F),
                Size = new System.Drawing.Size(600, 450),
                LegendLocation = LegendLocation.Bottom
            };
            chartAll.AxisX = new AxesCollection
            {
                new Axis
                {
                    FontSize = 16F,
                    Title = "负载（%）"
                }
            };
            chartAll.AxisY = new AxesCollection
            {
                new Axis
                {
                    FontSize = 16F,
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Show_Contrast_InsertRow(table, row, chartAll);
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "前级DC/DC", 450);
            LiveCharts.WinForms.CartesianChart chartDCDC = new LiveCharts.WinForms.CartesianChart()
            {
                BackColor = System.Drawing.Color.White,
                Location = new System.Drawing.Point(300, 0),
                Font = new System.Drawing.Font("宋体", 10.5F),
                Size = new System.Drawing.Size(600, 450),
                LegendLocation = LegendLocation.Bottom
            };
            chartDCDC.AxisX = new AxesCollection
            {
                new Axis
                {
                    FontSize = 16F,
                    Title = "负载（%）"
                }
            };
            chartDCDC.AxisY = new AxesCollection
            {
                new Axis
                {
                    FontSize = 16F,
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Show_Contrast_InsertRow(table, row, chartDCDC);
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "隔离DC/DC", 450);
            LiveCharts.WinForms.CartesianChart chartIsolatedDCDC = new LiveCharts.WinForms.CartesianChart()
            {
                BackColor = System.Drawing.Color.White,
                Location = new System.Drawing.Point(300, 0),
                Font = new System.Drawing.Font("宋体", 10.5F),
                Size = new System.Drawing.Size(600, 450),
                LegendLocation = LegendLocation.Bottom
            };
            chartIsolatedDCDC.AxisX = new AxesCollection
            {
                new Axis
                {
                    FontSize = 16F,
                    Title = "负载（%）"
                }
            };
            chartIsolatedDCDC.AxisY = new AxesCollection
            {
                new Axis
                {
                    FontSize = 16F,
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Show_Contrast_InsertRow(table, row, chartIsolatedDCDC);
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "逆变", 450);
            LiveCharts.WinForms.CartesianChart chartDCAC = new LiveCharts.WinForms.CartesianChart()
            {
                BackColor = System.Drawing.Color.White,
                Location = new System.Drawing.Point(300, 0),
                Font = new System.Drawing.Font("宋体", 10.5F),
                Size = new System.Drawing.Size(600, 450),
                LegendLocation = LegendLocation.Bottom
            };
            chartDCAC.AxisX = new AxesCollection
            {
                new Axis
                {
                    FontSize = 16F,
                    Title = "负载（%）"
                }
            };
            chartDCAC.AxisY = new AxesCollection
            {
                new Axis
                {
                    FontSize = 16F,
                    LabelFormatter = value => Math.Round(value, 8).ToString(),
                    Title = "效率（%）"
                }
            };
            Display_Show_Contrast_InsertRow(table, row, chartDCAC);
            for (int i = 0; i < m; i++)
            {
                structureListForContrast[i].Evaluate();
                ChartValues<ObservablePoint> valuesAll = new ChartValues<ObservablePoint>();
                ChartValues<ObservablePoint> valuesDCDC = new ChartValues<ObservablePoint>();
                ChartValues<ObservablePoint> valuesIsolatedDCDC = new ChartValues<ObservablePoint>();
                ChartValues<ObservablePoint> valuesDCAC = new ChartValues<ObservablePoint>();
                for (int j = 1; j <= div; j++)
                {
                    structureListForContrast[i].Operate(1.0 * j / div, structureListForContrast[i].Math_Vpv_min);
                    valuesAll.Add(new ObservablePoint(100 * j / div, structureListForContrast[i].Efficiency * 100));
                    valuesDCDC.Add(new ObservablePoint(100 * j / div, ((ThreeLevelStructure)structureListForContrast[i]).DCDC.Efficiency * 100));
                    valuesIsolatedDCDC.Add(new ObservablePoint(100 * j / div, ((ThreeLevelStructure)structureListForContrast[i]).IsolatedDCDC.Efficiency * 100));
                    valuesDCAC.Add(new ObservablePoint(100 * j / div, ((ThreeLevelStructure)structureListForContrast[i]).DCAC.Efficiency * 100));
                }
                chartAll.Series.Add(new LineSeries
                {
                    Title = "设计" + (i + 1) + "_Vin=" + structureListForContrast[i].Math_Vpv_min,
                    Fill = Brushes.Transparent,
                    Values = valuesAll,
                    PointGeometry = null
                });
                chartDCDC.Series.Add(new LineSeries
                {
                    Title = "设计" + (i + 1) + "_Vin=" + structureListForContrast[i].Math_Vpv_min,
                    Fill = Brushes.Transparent,
                    Values = valuesDCDC,
                    PointGeometry = null
                });
                chartIsolatedDCDC.Series.Add(new LineSeries
                {
                    Title = "设计" + (i + 1),
                    Fill = Brushes.Transparent,
                    Values = valuesIsolatedDCDC,
                    PointGeometry = null
                });
                chartDCAC.Series.Add(new LineSeries
                {
                    Title = "设计" + (i + 1),
                    Fill = Brushes.Transparent,
                    Values = valuesDCAC,
                    PointGeometry = null
                });
            }

            row++;
            Display_Show_Contrast_InsertTitle(table, "损耗分布");
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "整体", 400);
            for (int i = 0; i < m; i++)
            {
                LiveCharts.WinForms.PieChart pieChart = new LiveCharts.WinForms.PieChart()
                {
                    Dock = DockStyle.Fill
                };
                DisplayPieChart(pieChart, structureListForContrast[i].GetLossBreakdown());
                Display_Show_Contrast_InsertCell(table, i + 1, row, pieChart);
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "前级DC/DC", 400);
            for (int i = 0; i < m; i++)
            {
                LiveCharts.WinForms.PieChart pieChart = new LiveCharts.WinForms.PieChart()
                {
                    Dock = DockStyle.Fill
                };
                DisplayPieChart(pieChart, ((ThreeLevelStructure)structureListForContrast[i]).DCDC.GetTotalLossBreakdown());
                Display_Show_Contrast_InsertCell(table, i + 1, row, pieChart);
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "隔离DC/DC", 400);
            for (int i = 0; i < m; i++)
            {
                LiveCharts.WinForms.PieChart pieChart = new LiveCharts.WinForms.PieChart()
                {
                    Dock = DockStyle.Fill
                };
                DisplayPieChart(pieChart, ((ThreeLevelStructure)structureListForContrast[i]).IsolatedDCDC.GetTotalLossBreakdown());
                Display_Show_Contrast_InsertCell(table, i + 1, row, pieChart);
            }
            row++;
            Display_Show_Contrast_InsertCell(table, 0, row, "逆变", 400);
            for (int i = 0; i < m; i++)
            {
                LiveCharts.WinForms.PieChart pieChart = new LiveCharts.WinForms.PieChart()
                {
                    Dock = DockStyle.Fill
                };
                DisplayPieChart(pieChart, ((ThreeLevelStructure)structureListForContrast[i]).DCAC.GetTotalLossBreakdown());
                Display_Show_Contrast_InsertCell(table, i + 1, row, pieChart);
            }

            Display_Contrast_Main_Panel.Controls.Clear();
            Display_Contrast_Main_Panel.Controls.Add(table);

            ChangePanel(3, Display_Contrast_Panel);
        }

        private void Display_Show_Clear_Button_Click(object sender, EventArgs e)
        {
            structureListForContrast = new List<Structure>();
            converterListForContrast = new List<Converter>();
            Display_Show_Contrast_Button.Enabled = false;
            Display_Show_Clear_Button.Enabled = false;
        }

        private void Display_Show_Select_Button_Click(object sender, EventArgs e)
        {
            if (structureListForContrast.Count >= 4 || converterListForContrast.Count >= 4)
            {
                MessageBox.Show("最多只能选择四个进行比较！");
                return;
            }

            if (structureListForContrast.Count >= 1 || converterListForContrast.Count >= 1)
            {
                Display_Show_Contrast_Button.Enabled = true;
            }
            Display_Show_Clear_Button.Enabled = true;

            if (isStructureDisplay)
            {
                structureListForContrast.Add(selectedStructure);
            }
            else
            {
                converterListForContrast.Add(selectedConverter);
            }
        }

        private void Display_Show_Detail_Button_Click(object sender, EventArgs e)
        {
            Display_Detail_TabControl.Controls.Clear(); //清除所有控件以隐藏

            if (isStructureDisplay)
            {
                //生成数据
                selectedStructure.Evaluate();
                double[,] systemData = new double[div, 2]; //记录负载-效率曲线数据
                double[,] DCDCData = new double[div, 2];
                double[,] isolatedDCDCData = new double[div, 2];
                double[,] DCACData = new double[div, 2];
                for (int i = 1; i <= div; i++)
                {
                    selectedStructure.Operate(1.0 * i / div, selectedStructure.Math_Vpv_min);
                    //记录负载-效率曲线数据
                    systemData[i - 1, 0] = 100 * i / div; //负载点(%)
                    systemData[i - 1, 1] = selectedStructure.Efficiency * 100; //整体架构效率(%)
                    switch (selectedStructure.Name)
                    {
                        case "三级架构":
                            DCDCData[i - 1, 0] = 100 * i / div; //负载点(%)
                            DCDCData[i - 1, 1] = ((ThreeLevelStructure)selectedStructure).DCDC.Efficiency * 100; //前级DCDC效率(%)
                            isolatedDCDCData[i - 1, 0] = 100 * i / div; //负载点(%)
                            isolatedDCDCData[i - 1, 1] = ((ThreeLevelStructure)selectedStructure).IsolatedDCDC.Efficiency * 100; //隔离DCDC效率(%)
                            DCACData[i - 1, 0] = 100 * i / div; //负载点(%)
                            DCACData[i - 1, 1] = ((ThreeLevelStructure)selectedStructure).DCAC.Efficiency * 100; //逆变效率(%)
                            break;

                        case "两级架构":
                            isolatedDCDCData[i - 1, 0] = 100 * i / div; //负载点(%)
                            isolatedDCDCData[i - 1, 1] = ((TwoLevelStructure)selectedStructure).IsolatedDCDC.Efficiency * 100; //隔离DCDC效率(%)
                            DCACData[i - 1, 0] = 100 * i / div; //负载点(%)
                            DCACData[i - 1, 1] = ((TwoLevelStructure)selectedStructure).DCAC.Efficiency * 100; //逆变效率(%)
                            break;
                    }
                }

                //更新显示
                Display_Show_Detail_System_Display(systemData);
                switch (selectedStructure.Name)
                {
                    case "三级架构":
                        selectedConverter = ((ThreeLevelStructure)selectedStructure).DCDC;
                        Display_Show_Detail_DCDC_Display(DCDCData);
                        selectedConverter = ((ThreeLevelStructure)selectedStructure).IsolatedDCDC;
                        Display_Show_Detail_IsolatedDCDC_Display(isolatedDCDCData);
                        selectedConverter = ((ThreeLevelStructure)selectedStructure).DCAC;
                        Display_Show_Detail_DCAC_Display(DCACData);
                        selectedConverter = null;
                        break;

                    case "两级架构":
                        selectedConverter = ((TwoLevelStructure)selectedStructure).IsolatedDCDC;
                        Display_Show_Detail_IsolatedDCDC_Display(isolatedDCDCData);
                        selectedConverter = ((TwoLevelStructure)selectedStructure).DCAC;
                        Display_Show_Detail_DCAC_Display(DCACData);
                        selectedConverter = null;
                        break;
                }
            }
            else
            {
                //生成数据
                selectedConverter.Evaluate();
                double[,] data = new double[div, 2]; //记录负载-效率曲线数据
                for (int i = 1; i <= div; i++)
                {
                    switch (selectedConverter.Name)
                    {
                        case "前级DC/DC变换单元_三级":
                            selectedConverter.Operate(1.0 * i / div, ((DCDCConverter)selectedConverter).Math_Vin_min);
                            break;
                        case "隔离DC/DC变换单元_三级":
                            selectedConverter.Operate(1.0 * i / div, ((IsolatedDCDCConverter)selectedConverter).Math_Vin);
                            break;
                        case "隔离DC/DC变换单元_两级":
                            selectedConverter.Operate(1.0 * i / div, ((IsolatedDCDCConverter)selectedConverter).Math_Vin_min);
                            break;
                        case "逆变单元":
                            selectedConverter.Operate(1.0 * i / div, ((DCACConverter)selectedConverter).Math_Vin);
                            break;
                    }
                    //记录负载-效率曲线数据
                    data[i - 1, 0] = 100 * i / div; //负载点(%)
                    data[i - 1, 1] = selectedConverter.Efficiency * 100; //整体架构效率(%)
                                                                         //Console.WriteLine(data[i - 1, 1]);
                }

                //更新显示                
                switch (selectedConverter.Name)
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
            if (isStructureDisplay)
            {
                Display_Detail_Vin_TrackBar.Visible = true;
                Display_Detail_Vin_TrackBar.Minimum = (int)selectedStructure.Math_Vpv_min;
                Display_Detail_Vin_TrackBar.Maximum = (int)selectedStructure.Math_Vpv_max;
                Display_Detail_Vin_TrackBar.Value = (int)selectedStructure.Math_Vpv_min;
            }
            else
            {
                switch (selectedConverter.Name)
                {
                    case "前级DC/DC变换单元_三级":
                        Display_Detail_Vin_TrackBar.Visible = true;
                        Display_Detail_Vin_Label.Visible = true;
                        Display_Detail_Vin_Value_Label.Visible = true;
                        Display_Detail_Vin_TrackBar.Minimum = (int)((DCDCConverter)selectedConverter).Math_Vin_min;
                        Display_Detail_Vin_TrackBar.Maximum = (int)((DCDCConverter)selectedConverter).Math_Vin_max;
                        Display_Detail_Vin_TrackBar.Value = (int)((DCDCConverter)selectedConverter).Math_Vin_min;
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
                        Display_Detail_Vin_TrackBar.Minimum = (int)((IsolatedDCDCConverter)selectedConverter).Math_Vin_min;
                        Display_Detail_Vin_TrackBar.Maximum = (int)((IsolatedDCDCConverter)selectedConverter).Math_Vin_max;
                        Display_Detail_Vin_TrackBar.Value = (int)((IsolatedDCDCConverter)selectedConverter).Math_Vin_min;
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
            ChangePanel(3, Display_Show_Panel);
        }

        private void Display_Contrast_Back_Button_Click(object sender, EventArgs e)
        {
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
    }
}
