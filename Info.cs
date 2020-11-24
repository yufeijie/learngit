namespace PV_analysis.Informations
{
    /// <summary>
    /// 信息-数据结构
    /// </summary>
    internal class Info
    {
        /// <summary>
        /// 设计类型，用于生成手动设计页面
        /// </summary>
        public enum DesignCategory
        {
            Text,
            Semiconductor,
            Core,
            Wire,
            Capacitor
        }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 内容
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public DesignCategory Category { get; }

        public Info(string title, object content)
        {
            Title = title;
            Content = content;
        }

        public Info(string title, DesignCategory category)
        {
            Title = title;
            Category = category;
        }

        public Info(string title, object content, DesignCategory category)
        {
            Title = title;
            Content = content;
            Category = category;
        }
    }
}
