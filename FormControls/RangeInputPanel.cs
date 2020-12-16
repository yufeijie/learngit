using System;
using System.Drawing;
using System.Windows.Forms;

namespace PV_analysis.FormControls
{
    /// <summary>
    /// 范围输入辅助面板
    /// </summary>
    internal class RangeInputPanel : Panel
    {
        private Control inputBox;

        Button clearButton;
        Button addButton;
        Button addRangeButton;
        Label leftSplitLineLabel;
        Label rightSplitLineLabel;
        TextBox singleTextBox;
        TextBox minTextBox;
        TextBox maxTextBox;
        TextBox stepTextBox;

        public RangeInputPanel(Control inputBox)
        {
            this.inputBox = inputBox;
            BorderStyle = BorderStyle.FixedSingle;
            Location = new Point(820, 3);
            Margin = new Padding(0);
            Size = new Size(442, 38);

            clearButton = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                Font = new Font("微软雅黑", 14.25F),
                Location = new Point(2, 1),
                Margin = new Padding(0),
                Size = new Size(58, 34),
                Text = "清空",
                UseVisualStyleBackColor = true
            };
            clearButton.Click += Clear_Click;

            leftSplitLineLabel = new Label
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Times New Roman", 14.25F),
                Location = new Point(188, 0),
                Size = new Size(1, 38),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            singleTextBox = new TextBox
            {
                Font = new Font("Times New Roman", 14.25F),
                ForeColor = Color.DarkGray,
                Location = new Point(66, 4),
                Name = "(单个)", //用Name记录提示文字
                Size = new Size(60, 29),
                Text = "(单个)",
                TextAlign = HorizontalAlignment.Center
            };
            singleTextBox.Leave += TextBox_Leave;
            singleTextBox.Enter += TextBox_Enter;

            addButton = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                Font = new Font("微软雅黑", 14.25F),
                Location = new Point(128, 1),
                Margin = new Padding(0),
                Size = new Size(58, 34),
                Text = "添加",
                UseVisualStyleBackColor = true
            };
            addButton.Click += Add_Click;

            rightSplitLineLabel = new Label
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Times New Roman", 14.25F),
                Location = new Point(62, 0),
                Size = new Size(1, 38),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            minTextBox = new TextBox
            {
                Font = new Font("Times New Roman", 14.25F),
                ForeColor = Color.DarkGray,
                Location = new Point(192, 4),
                Name = "(最小)", //用Name记录提示文字
                Size = new Size(60, 29),
                Text = "(最小)",
                TextAlign = HorizontalAlignment.Center
            };
            minTextBox.Leave += TextBox_Leave;
            minTextBox.Enter += TextBox_Enter;

            maxTextBox = new TextBox
            {
                Font = new Font("Times New Roman", 14.25F),
                ForeColor = Color.DarkGray,
                Location = new Point(255, 4),
                Name = "(最大)", //用Name记录提示文字
                Size = new Size(60, 29),
                Text = "(最大)",
                TextAlign = HorizontalAlignment.Center
            };
            maxTextBox.Leave += TextBox_Leave;
            maxTextBox.Enter += TextBox_Enter;

            stepTextBox = new TextBox
            {
                Font = new Font("Times New Roman", 14.25F),
                ForeColor = Color.DarkGray,
                Location = new Point(318, 4),
                Name = "(步长)", //用Name记录提示文字
                Size = new Size(60, 29),
                Text = "(步长)",
                TextAlign = HorizontalAlignment.Center
            };
            stepTextBox.Leave += TextBox_Leave;
            stepTextBox.Enter += TextBox_Enter;

            addRangeButton = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                Font = new Font("微软雅黑", 14.25F),
                Location = new Point(380, 1),
                Margin = new Padding(0),
                Size = new Size(58, 34),
                Text = "添加",
                UseVisualStyleBackColor = true
            };
            addRangeButton.Click += Add_Range_Click;

            Controls.Add(clearButton);
            Controls.Add(leftSplitLineLabel);
            Controls.Add(singleTextBox);
            Controls.Add(addButton);
            Controls.Add(rightSplitLineLabel);
            Controls.Add(minTextBox);
            Controls.Add(maxTextBox);
            Controls.Add(stepTextBox);
            Controls.Add(addRangeButton);
        }

        private void Add_Click(object sender, EventArgs e)
        {
            if (!(Function.IsNumber(singleTextBox.Text)))
            {
                return;
            }

            if (!string.IsNullOrEmpty(inputBox.Text))
            {
                inputBox.Text += ",";
            }
            inputBox.Text += singleTextBox.Text;
        }

        private void Add_Range_Click(object sender, EventArgs e)
        {
            if (!(Function.IsNumber(minTextBox.Text) && Function.IsNumber(maxTextBox.Text) && Function.IsNumber(stepTextBox.Text)))
            {
                return;
            }

            if (!string.IsNullOrEmpty(inputBox.Text))
            {
                inputBox.Text += ",";
            }
            inputBox.Text += Function.GenerateRangeToString(double.Parse(minTextBox.Text), double.Parse(maxTextBox.Text), double.Parse(stepTextBox.Text));
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            inputBox.Text = "";
        }

        private void TextBox_Leave(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            //退出失去焦点，重新显示
            if (string.IsNullOrEmpty(control.Text))
            {
                control.ForeColor = Color.DarkGray;
                control.Text = control.Name; //用Name记录提示文字
            }
        }

        private void TextBox_Enter(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            //进入获得焦点，清空
            if (control.Text.Equals(control.Name)) //用Name记录提示文字
            {
                control.ForeColor = Color.Black;
                control.Text = "";
            }
        }
    }
}
