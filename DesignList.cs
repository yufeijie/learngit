using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PV_analysis
{
    /// <summary>
    /// 设计方案集合
    /// </summary>
    internal class DesignList
    {
        //采用双向链表进行存储
        private int size = 0; //设计方案数
        private DesignData head = null; //头指针
        private DesignData tail = null; //尾指针

        public interface IDesignData
        {
            double PowerLoss { get; }
            double Volume { get; }
            double Cost { get; }
            string[] Info { get; }
        }

        private class DesignData : IDesignData
        {
            public DesignData Prev { get; set; } = null; //上一个节点
            public DesignData Next { get; set; } = null; //下一个节点
            public double PowerLoss { get; }
            public double Volume { get; }
            public double Cost { get; }
            public string[] Info { get; }

            public DesignData(double powerLoss, double volume, double cost, string[] info)
            {
                PowerLoss = powerLoss;
                Volume = volume;
                Cost = cost;
                Info = info;
            }
        }

        public IDesignData[] Data { get { return data; } }

        /// <summary>
        /// 在双向链表尾部插入一个节点
        /// </summary>
        /// <param name="data"></param>
        private void Insert(DesignData node)
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
        private void Delete(DesignData node)
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

        /// <summary>
        /// 添加一个点，并进行Pareto改进
        /// </summary>
        /// <param name="powerLoss">损耗</param>
        /// <param name="volume">体积</param>
        /// <param name="cost">成本</param>
        /// <param name="info">设计信息</param>
        public void Add(double powerLoss, double volume, double cost, string[] info)
        {
            //Pareto改进
            DesignData now = head;
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
            DesignData node = new DesignData(powerLoss, volume, cost, info);
            Insert(node);
        }
    }
}
