namespace PV_analysis
{
    /// <summary>
    /// 设计方案集合
    /// </summary>
    internal class ComponentDesignList
    {
        //采用双向链表进行存储
        private int size = 0; //设计方案数
        private ComponentDesignData head = null; //头指针
        private ComponentDesignData tail = null; //尾指针

        /// <summary>
        /// 存储设计方案的评估结果与配置信息
        /// </summary>
        private class ComponentDesignData : IComponentDesignData
        {
            public ComponentDesignData Prev { get; set; } = null; //上一个节点
            public ComponentDesignData Next { get; set; } = null; //下一个节点
            public double PowerLoss { get; } //损耗
            public double Volume { get; } //体积
            public double Cost { get; } //成本
            public string[] Configs { get; } //配置信息

            public ComponentDesignData(double powerLoss, double volume, double cost, string[] configs)
            {
                PowerLoss = powerLoss;
                Volume = volume;
                Cost = cost;
                Configs = configs;
            }
        }

        public IComponentDesignData[] GetData()
        {
            IComponentDesignData[] data = new IComponentDesignData[size];
            ComponentDesignData now = head;
            for (int i = 0;  i < size; i++)
            {
                data[i] = now;
                now = now.Next;
            }
            return data;
        }

        /// <summary>
        /// 添加一个点，并进行Pareto改进
        /// </summary>
        /// <param name="powerLoss">损耗</param>
        /// <param name="volume">体积</param>
        /// <param name="cost">成本</param>
        /// <param name="info">设计信息</param>
        public void Add(double powerLoss, double volume, double cost, string[] configs)
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

            //若新添加的点未被支配，则将该点添加进集合中
            ComponentDesignData node = new ComponentDesignData(powerLoss, volume, cost, configs);
            Insert(node);
        }

        /// <summary>
        /// 在双向链表尾部插入一个节点
        /// </summary>
        /// <param name="data"></param>
        private void Insert(ComponentDesignData node)
        {
            if (head == null)
            {
                head = node;
            }
            if (tail != null)
            {
                tail.Next = node;
                node.Prev = tail;
            }
            tail = node;
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
            else
            {
                tail = node.Prev;
            }
            size--;
        }
    }
}
