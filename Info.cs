namespace PV_analysis.Informations
{
    /// <summary>
    /// 信息-数据结构
    /// </summary>
    internal class Info
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 内容
        /// </summary>
        public object Content { get; set; }

        public Info(string title, object content)
        {
            Title = title;
            Content = content;
        }
    }
}
