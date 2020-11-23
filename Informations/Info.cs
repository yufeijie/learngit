using System;
using System.Collections.Generic;

namespace PV_analysis.Informations
{
    /// <summary>
    /// 信息-数据结构
    /// </summary>
    internal class Info
    {
        public string Title { get; set; }
        public object Content { get; set; }
        public Info(string title, object content)
        {
            Title = title;
            Content = content;
        }
    }
}
