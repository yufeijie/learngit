using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PV_analysis.FormControls
{
    /// <summary>
    /// 系列复选框（点击后，将选择状态同步到绑定的复选框）
    /// </summary>
    internal class SeriesCheckBox : CheckBox
    {
        private List<CheckBox> checkBoxList = new List<CheckBox>(); //记录绑定的复选框

        public SeriesCheckBox()
        {
            CheckedChanged += SeriesCheckBox_CheckedChanged; //添加默认的选择状态改变事件
        }

        /// <summary>
        /// 绑定需要同步状态的复选框
        /// </summary>
        /// <param name="checkBox">绑定的复选框</param>
        public void Add(CheckBox checkBox)
        {
            checkBoxList.Add(checkBox);
        }

        /// <summary>
        /// 将选择状态同步到绑定的复选框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SeriesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            foreach(CheckBox checkBox in checkBoxList)
            {
                checkBox.Checked = Checked;
            }
        }
    }
}
