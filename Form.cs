using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PV_analysis
{
    internal partial class Form : System.Windows.Forms.Form
    {
        private Panel[] panelNow = new Panel[5];
        private Color activeColor;
        private Color inactiveColor;

        public Form()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            panelNow[0] = this.Home_Panel;
            panelNow[1] = this.Home_Panel;
            panelNow[2] = this.Estimate_Ready_Panel;
            panelNow[3] = this.Display_Ready_Panel;
            panelNow[4] = this.Admin_Panel;

            activeColor = this.Tab_Home_Button.BackColor;
            inactiveColor = this.Tab_Estimate_Button.BackColor;
        }

        private void Tab_Home_Button_Click(object sender, EventArgs e)
        {
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[1];
            panelNow[0].Visible = true;

            this.Tab_Home_Button.BackColor = activeColor;
            this.Tab_Estimate_Button.BackColor = inactiveColor;
            this.Tab_Display_Button.BackColor = inactiveColor;
            this.Tab_Admin_Button.BackColor = inactiveColor;
        }

        private void Tab_Estimate_Button_Click(object sender, EventArgs e)
        {
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;

            this.Tab_Home_Button.BackColor = inactiveColor;
            this.Tab_Estimate_Button.BackColor = activeColor;
            this.Tab_Display_Button.BackColor = inactiveColor;
            this.Tab_Admin_Button.BackColor = inactiveColor;
        }

        private void Tab_Display_Button_Click(object sender, EventArgs e)
        {
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;

            this.Tab_Home_Button.BackColor = inactiveColor;
            this.Tab_Estimate_Button.BackColor = inactiveColor;
            this.Tab_Display_Button.BackColor = activeColor;
            this.Tab_Admin_Button.BackColor = inactiveColor;
        }

        private void Tab_Admin_Button_Click(object sender, EventArgs e)
        {
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[4];
            panelNow[0].Visible = true;

            this.Tab_Home_Button.BackColor = inactiveColor;
            this.Tab_Estimate_Button.BackColor = inactiveColor;
            this.Tab_Display_Button.BackColor = inactiveColor;
            this.Tab_Admin_Button.BackColor = activeColor;
        }

        private void Estimate_Ready_Begin_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step1_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step1_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Ready_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step1_Next_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step2_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step2_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step1_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step2_Next_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step3_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step3_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step2_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step3_Next_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step4_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step4_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step3_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step4_Next_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step5_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step5_Prev_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Step4_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step5_Next_button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Result_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Estimate_Result_Save_Button_Click(object sender, EventArgs e)
        {

        }

        private void Estimate_Result_Display_Button_Click(object sender, EventArgs e)
        {
            panelNow[3] = this.Display_Show_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;

            this.Tab_Estimate_Button.BackColor = inactiveColor;
            this.Tab_Display_Button.BackColor = activeColor;
        }

        private void Estimate_Result_Restart_Button_Click(object sender, EventArgs e)
        {
            panelNow[2] = this.Estimate_Ready_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[2];
            panelNow[0].Visible = true;
        }

        private void Display_Ready_Load_Button_Click(object sender, EventArgs e)
        {
            panelNow[3] = this.Display_Show_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;
        }

        private void Display_Show_Detail_Button_Click(object sender, EventArgs e)
        {
            panelNow[3] = this.Display_Detail_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;
        }

        private void Display_Show_Restart_Button_Click(object sender, EventArgs e)
        {
            panelNow[3] = this.Display_Ready_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;
        }

        private void Display_Detail_Back_Button_Click(object sender, EventArgs e)
        {
            panelNow[3] = this.Display_Show_Panel;
            panelNow[0].Visible = false;
            panelNow[0] = panelNow[3];
            panelNow[0].Visible = true;
        }

        private void Estimate_Step1_CheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Estimate_Step1_Item1_CheckBox.Checked = this.Estimate_Step1_CheckedListBox.GetItemChecked(0);
            this.Estimate_Step1_Item2_CheckBox.Checked = this.Estimate_Step1_CheckedListBox.GetItemChecked(1);
        }

        private void Estimate_Step1_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Estimate_Step1_CheckedListBox.SetItemChecked(0, this.Estimate_Step1_Item1_CheckBox.Checked);
        }

        private void Estimate_Step1_Item2_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Estimate_Step1_CheckedListBox.SetItemChecked(1, this.Estimate_Step1_Item2_CheckBox.Checked);
        }

        private void Estimate_Step2_Group1_CheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group1_Item1_CheckBox.Checked = this.Estimate_Step2_Group1_CheckedListBox.GetItemChecked(0);
            this.Estimate_Step2_Group1_Item2_CheckBox.Checked = this.Estimate_Step2_Group1_CheckedListBox.GetItemChecked(1);
            this.Estimate_Step2_Group1_Item3_CheckBox.Checked = this.Estimate_Step2_Group1_CheckedListBox.GetItemChecked(2);
        }

        private void Estimate_Step2_Group2_CheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group2_Item1_CheckBox.Checked = this.Estimate_Step2_Group2_CheckedListBox.GetItemChecked(0);
            this.Estimate_Step2_Group2_Item2_CheckBox.Checked = this.Estimate_Step2_Group2_CheckedListBox.GetItemChecked(1);
        }

        private void Estimate_Step2_Group3_CheckedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group3_Item1_CheckBox.Checked = this.Estimate_Step2_Group3_CheckedListBox.GetItemChecked(0);
        }

        private void Estimate_Step2_Group1_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group1_CheckedListBox.SetItemChecked(0, this.Estimate_Step2_Group1_Item1_CheckBox.Checked);
        }

        private void Estimate_Step2_Group1_Item2_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group1_CheckedListBox.SetItemChecked(1, this.Estimate_Step2_Group1_Item2_CheckBox.Checked);
        }

        private void Estimate_Step2_Group1_Item3_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group1_CheckedListBox.SetItemChecked(2, this.Estimate_Step2_Group1_Item3_CheckBox.Checked);
        }

        private void Estimate_Step2_Group2_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group2_CheckedListBox.SetItemChecked(0, this.Estimate_Step2_Group2_Item1_CheckBox.Checked);
        }

        private void Estimate_Step2_Group2_Item2_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group2_CheckedListBox.SetItemChecked(1, this.Estimate_Step2_Group2_Item2_CheckBox.Checked);
        }

        private void Estimate_Step2_Group3_Item1_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.Estimate_Step2_Group3_CheckedListBox.SetItemChecked(0, this.Estimate_Step2_Group3_Item1_CheckBox.Checked);
        }

    }

}
