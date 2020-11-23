using System;
using System.Collections.Generic;

namespace PV_analysis.Informations
{
    /// <summary>
    /// 信息包-数据结构
    /// </summary>
    internal class InfoPackage
    {
        public string Title { get; }
        public int Size { get { return content.Count; } }
        private readonly List<InfoList> content = new List<InfoList>();

        public InfoPackage(string title)
        {
            Title = title;
        }

        public void Add(InfoList infoSection)
        {
            content.Add(infoSection);
        }

        public void AddRange(InfoPackage infoPackage)
        {
            content.AddRange(infoPackage.content);
        }

        public InfoList this[int index]
        {
            get
            {
                if (index < 0 || index >= content.Count)
                    throw new ArgumentOutOfRangeException("index", "索引超出范围");
                return content[index];
            }
            set
            {
                if (index < 0 || index >= content.Count)
                    throw new ArgumentOutOfRangeException("index", "索引超出范围");
                content[index] = value;
            }
        }
    }
}
