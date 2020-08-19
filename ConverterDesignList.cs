using System.Collections.Generic;
using System.Linq;

namespace PV_analysis
{
    /// <summary>
    /// 变换器设计方案集合
    /// </summary>
    internal class ConverterDesignList
    {
        //采用单向链表进行存储
        private int size = 0; //设计方案数
        private ConverterDesignData head = null; //头指针

        /// <summary>
        /// 是否记录全部设计，若为false则在执行Add方法时同时进行Pareto改进
        /// 默认为false
        /// </summary>
        public bool IsAll { get; set; } = false;

        public int Size{ get { return size; } }

        /// <summary>
        /// 存储变换器设计方案的评估结果与配置信息
        /// </summary>
        private class ConverterDesignData : IConverterDesignData
        {
            public ConverterDesignData Prev { get; set; } = null; //上一个节点
            public ConverterDesignData Next { get; set; } = null; //下一个节点
            public double Efficiency { get; set; } //效率
            public double Volume { get; set; } //体积
            public double Cost { get; set; } //成本
            public string[] Configs { get; set; } //配置信息 
        }

        /// <summary>
        /// 获取变换器设计方案数据（数组形式）
        /// </summary>
        /// <returns>变换器设计方案数据</returns>
        public IConverterDesignData[] GetData()
        {
            IConverterDesignData[] data = new IConverterDesignData[size];
            ConverterDesignData now = head;
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
        /// <param name="efficiency">效率</param>
        /// <param name="volume">体积</param>
        /// <param name="cost">成本</param>
        /// <param name="configs">配置信息</param>
        public void Add(double efficiency, double volume, double cost, string[] configs)
        {
            if (!IsAll) //若不记录全部设计，则进行Pareto改进
            {
                //Pareto改进
                ConverterDesignData now = head;
                while (now != null)
                {
                    //若当前Pareto集合中存在一个点，可以支配新添加的点，则新添加的点不为Pareto最优解，不需要添加进集合
                    if (now.Efficiency >= efficiency && now.Volume <= volume && now.Cost <= cost)
                    {
                        return;
                    }

                    //若新添加的点支配集合中存在的点，则将被支配的点剔除
                    if (now.Efficiency <= efficiency && now.Volume >= volume && now.Cost >= cost)
                    {
                        Delete(now);
                    }

                    now = now.Next;
                }
            }

            //将该点添加进集合中
            Insert(new ConverterDesignData()
            {
                Efficiency = efficiency,
                Volume = volume,
                Cost = cost,
                Configs = configs
            });
        }

        /// <summary>
        /// 将当前设计方案集合与另一设计方案集合组合成新的设计方案集合，并更新当前设计方案集合
        /// </summary>
        /// <param name="designList">另一个设计方案集合</param>
        public void Combine(ConverterDesignList designList)
        {
            if (head == null)
            {
                head = designList.head;
                size = designList.size;
            }
            else
            {
                ConverterDesignList newList = new ConverterDesignList();
                ConverterDesignData p = head;
                while (p != null)
                {
                    ConverterDesignData q = designList.head;
                    while (q != null)
                    {
                        string[] configs = new string[p.Configs.Length + q.Configs.Length];
                        p.Configs.CopyTo(configs, 0);
                        q.Configs.CopyTo(configs, p.Configs.Length);
                        newList.Add(p.Efficiency + q.Efficiency - 1, p.Volume + q.Volume, p.Cost + q.Cost, configs);
                        q = q.Next;
                    }
                    p = p.Next;
                }
                head = newList.head;
                size = newList.size;
            }
        }

        /// <summary>
        /// 合并另一个设计方案集合
        /// </summary>
        /// <param name="designList">另一个设计方案集合</param>
        public void Merge(ConverterDesignList designList)
        {
            ConverterDesignData now = designList.head;
            while (now != null)
            {
                Add(now.Efficiency, now.Volume, now.Cost, now.Configs);
                now = now.Next;
            }
        }

        /// <summary>
        /// 将当前的变换器设计方案集合转化为系统设计方案集合
        /// </summary>
        /// <param name="configs">额外配置信息</param>
        public void Transfer(string[] configs)
        {
            ConverterDesignData now = head;
            while (now != null)
            {
                List<string> newConfigs = new List<string>
                {
                    now.Efficiency.ToString(),
                    now.Volume.ToString(),
                    now.Cost.ToString()
                };
                foreach (string config in now.Configs)
                {
                    newConfigs.Add(config);
                }
                foreach (string config in configs)
                {
                    newConfigs.Add(config);
                }
                now.Configs = newConfigs.ToArray();
                now = now.Next;
            }
            Filter();
        }

        /// <summary>
        /// 将器件设计方案集合转化为变换器设计方案集合，并合并到当前集合中
        /// </summary>
        /// <param name="componentDesignList">器件设计方案集合</param>
        /// <param name="power">总功率</param>
        /// <param name="number">模块数</param>
        /// <param name="phaseNum">相数</param>
        /// <param name="configs">变换器配置信息</param>
        public void Transfer(ComponentDesignList componentDesignList, double power, double number, double phaseNum, string[] configs)
        {
            IComponentDesignData[] designs = componentDesignList.GetData();
            foreach (IComponentDesignData design in designs)
            {
                double efficiency = 1 - design.PowerLoss * number * phaseNum / power;
                double volume = design.Volume * number * phaseNum;
                double cost = design.Cost * number * phaseNum;
                List<string> newConfigs = new List<string>
                {
                    efficiency.ToString(),
                    volume.ToString(),
                    cost.ToString()
                };
                foreach (string config in configs)
                {
                    newConfigs.Add(config);
                }
                foreach (string config in design.Configs)
                {
                    newConfigs.Add(config);
                }
                Add(efficiency, volume, cost, newConfigs.ToArray());
            }
            Filter();
        }

        /// <summary>
        /// 去除效率低于90%的设计方案
        /// </summary>
        public void Filter()
        {
            ConverterDesignData now = head;
            List<ConverterDesignData> list = new List<ConverterDesignData>();
            while (now != null)
            {
                if (now.Efficiency < 0.9)
                {
                    list.Add(now);
                }
                now = now.Next;
            }
            foreach (ConverterDesignData design in list)
            {
                Delete(design);
            }
        }

        /// <summary>
        /// 在链表头部插入一个节点
        /// </summary>
        /// <param name="data"></param>
        private void Insert(ConverterDesignData node)
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
        private void Delete(ConverterDesignData node)
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
