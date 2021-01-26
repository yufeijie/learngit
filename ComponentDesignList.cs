using PV_analysis.Converters;

namespace PV_analysis
{
    /// <summary>
    /// 器件设计方案集合
    /// </summary>
    internal class ComponentDesignList
    {
        //采用单向链表进行存储
        private int size = 0; //方案数
        private ComponentDesignData head = null; //头指针

        /// <summary>
        /// 是否记录全部设计，若为false则在执行Add方法时同时进行Pareto改进
        /// 默认为false
        /// </summary>
        public bool IsAll { get; set; } = false;

        public int Size { get { return size; } }

        /// <summary>
        /// 存储器件设计方案的评估结果与配置信息
        /// </summary>
        private class ComponentDesignData : IComponentDesignData
        {
            public ComponentDesignData Prev { get; set; } = null; //上一个节点
            public ComponentDesignData Next { get; set; } = null; //下一个节点
            public double PowerLoss { get; set; } //损耗
            public double Volume { get; set; } //体积
            public double Cost { get; set; } //成本
            public string[] Configs { get; set; } //配置信息 
        }

        /// <summary>
        /// 获取器件设计方案数据（数组形式）
        /// </summary>
        /// <returns>器件设计方案数据</returns>
        public IComponentDesignData[] GetData()
        {
            IComponentDesignData[] data = new IComponentDesignData[size];
            ComponentDesignData now = head;
            for (int i = 0; i < size; i++)
            {
                data[i] = now;
                now = now.Next;
            }
            return data;
        }

        /// <summary>
        /// 添加一个设计，并进行Pareto改进
        /// </summary>
        /// <param name="powerLoss">损耗</param>
        /// <param name="volume">体积</param>
        /// <param name="cost">成本</param>
        /// <param name="configs">配置信息</param>
        public void Add(double powerLoss, double volume, double cost, string[] configs)
        {
            if (!IsAll) //若不记录全部设计，则进行Pareto改进
            {
                //Pareto改进
                ComponentDesignData now = head;
                while (now != null)
                {
                    //若当前Pareto集合中存在一个点，可以支配新添加的点，则新添加的点不为Pareto最优解
                    if (now.PowerLoss <= powerLoss && now.Volume <= volume && now.Cost <= cost)
                    {
                        return;
                    }

                    //若新添加的点支配集合中存在的点，则将被支配的点剔除
                    if (now.PowerLoss >= powerLoss && now.Volume >= volume && now.Cost >= cost)
                    {
                        Delete(now);
                    }

                    now = now.Next;
                }
            }

            //若新添加的点未被支配，则将该点添加进集合中
            Insert(new ComponentDesignData()
            {
                PowerLoss = powerLoss,
                Volume = volume,
                Cost = cost,
                Configs = configs
            });
        }

        /// <summary>
        /// 将当前设计方案集合与另一设计方案集合组合成新的设计方案集合，并更新当前设计方案集合
        /// </summary>
        /// <param name="designList">另一个设计方案集合</param>
        public void Combine(ComponentDesignList designList)
        {
            if (head == null)
            {
                head = designList.head;
                size = designList.size;
            }
            else
            {
                ComponentDesignList newList = new ComponentDesignList();
                ComponentDesignData p = head;
                while (p != null)
                {
                    ComponentDesignData q = designList.head;
                    while (q != null)
                    {
                        string[] configs = new string[p.Configs.Length + q.Configs.Length];
                        p.Configs.CopyTo(configs, 0);
                        q.Configs.CopyTo(configs, p.Configs.Length);
                        newList.Add(p.PowerLoss + q.PowerLoss, p.Volume + q.Volume, p.Cost + q.Cost, configs);
                        q = q.Next;
                    }
                    p = p.Next;
                }
                head = newList.head;
                size = newList.size;
            }
        }

        /// <summary>
        /// 设计散热器
        /// 设计DSP
        /// </summary>
        public void DesignAuxComponent()
        {
            ComponentDesignData now = head;
            while (now != null)
            {
                //设计散热器
                double Rh = (Configuration.MAX_HEATSINK_TEMPERATURE - Configuration.AMBIENT_TEMPERATURE) / now.PowerLoss; //此处应采用损耗最大值
                double Vh = 1 / (Configuration.CSPI * Rh);
                double Ch = Vh * Configuration.HEATSINK_UNIT_PRICE;
                now.Volume += Vh;
                now.Cost += Ch;

                //设计DSP
                now.Cost += Configuration.DSP_PRICE; //每个变换器模块用一个DSP

                now = now.Next;
            }
        }

        /// <summary>
        /// 在链表头部插入一个节点
        /// </summary>
        /// <param name="data"></param>
        private void Insert(ComponentDesignData node)
        {
            if (head != null)
            {
                head.Prev = node;
                node.Next = head;
            }
            head = node;
            size++;
        }

        /// <summary>
        /// 删除一个节点
        /// </summary>
        /// <param name="data"></param>
        private void Delete(ComponentDesignData node)
        {
            if (node.Prev != null)
            {
                node.Prev.Next = node.Next;
            }
            else
            {
                head = node.Next;
            }
            if (node.Next != null)
            {
                node.Next.Prev = node.Prev;
            }
            node.Prev = null;
            node.Next = null;
            size--;
        }
    }
}
