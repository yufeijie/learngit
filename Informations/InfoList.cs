using System;
using System.Collections.Generic;

namespace PV_analysis.Informations
{
    /// <summary>
    /// 信息列表-数据结构
    /// </summary>
    internal class InfoList
    {
        public string Title { get; }
        public int Size { get { return content.Count; } }
        private readonly List<Info> content = new List<Info>();

        public InfoList(string title)
        {
            Title = title;
        }

        public void Add(Info info)
        {
            content.Add(info);
        }

        public void AddRange(InfoList list)
        {
            content.AddRange(list.content);
        }

        public Info this[int index]
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
