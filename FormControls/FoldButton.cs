﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PV_analysis.FormControls
{
    /// <summary>
    /// 折叠按钮
    /// </summary>
    internal class FoldButton : Button
    {
        List<Control> controlList = new List<Control>();

        public FoldButton()
        {
            Click += Fold_Button_Click;
        }

        public void Add(Control control)
        {
            controlList.Add(control);
        }

        private void Fold_Button_Click(object sender, EventArgs e)
        {
            foreach (Control control in controlList)
            {
                control.Visible = !control.Visible;
            }
        }
    }
}
