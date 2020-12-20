using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PV_analysis.FormControls
{
    /// <summary>
    /// 范围输入辅助面板
    /// </summary>
    internal class SeriesCheckBox : CheckBox
    {
        private List<CheckBox> checkBoxList = new List<CheckBox>();

        public SeriesCheckBox()
        {
            CheckedChanged += SeriesCheckBox_CheckedChanged;
        }
        public void Add(CheckBox checkBox)
        {
            checkBoxList.Add(checkBox);
        }

        private void SeriesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            foreach(CheckBox checkBox in checkBoxList)
            {
                checkBox.Checked = Checked;
            }
        }
    }
}
