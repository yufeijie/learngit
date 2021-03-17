using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PV_analysis.FormControls
{
    /// <summary>
    /// 折叠按钮（用来显示/隐藏绑定的控件）
    /// </summary>
    internal class FoldButton : Button
    {
        private List<Control> controlList = new List<Control>(); //记录绑定的控件

        /// <summary>
        /// 初始化
        /// </summary>
        public FoldButton()
        {
            Click += FoldButton_Click; //为此按钮添加默认的点击事件
        }

        /// <summary>
        /// 绑定需要显示/隐藏的控件
        /// </summary>
        /// <param name="control">绑定的控件</param>
        public void Add(Control control)
        {
            controlList.Add(control); //
        }

        /// <summary>
        /// 切换绑定控件的可见状态（显示/隐藏）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FoldButton_Click(object sender, EventArgs e)
        {
            foreach (Control control in controlList)
            {
                control.Visible = !control.Visible;
            }
        }
    }
}
