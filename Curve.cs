using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;

namespace PV_analysis
{
    /// <summary>
    /// 曲线数据类，用于存储电压、电流波形
    /// </summary>
    internal class Curve
    {
        //采用双向链表进行存储
        public int Size { get; private set; } = 0; //数据点数
        public Point Head { get; set; } = null; //头指针
        public Point Tail { get; set; } = null; //尾指针

        /// <summary>
        /// 曲线名
        /// </summary>
        public string Name { get; set; }

        //TODO 封装
        /// <summary>
        /// 数据点，包括X坐标和Y坐标
        /// </summary>
        public class Point
        {
            public Point Prev { get; set; } = null; //上一个节点
            public Point Next { get; set; } = null; //下一个节点
            public double X { get; }
            public double Y { get; }
            public Point(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        /// <summary>
        /// 曲线数据
        /// </summary>
        public Point[] GetData()
        {
            Point[] data = new Point[Size];
            Point now = Head;
            for (int i = 0; i < Size; i++)
            {
                data[i] = now;
                now = now.Next;
            }
            return data;
        }

        /// <summary>
        /// 添加一个点
        /// </summary>
        /// <param name="x">点的x坐标</param>
        /// <param name="y">点的y坐标</param>
        public void Add(double x, double y)
        {
            InsertTail(new Point(x, y));
        }

        /// <summary>
        /// 计算有效值
        /// </summary>
        /// <returns>有效值</returns>
        public double CalcRMS()
        {
            double result = 0;
            Point l = Head;
            Point r = Head.Next;
            while (r != null)
            {
                result += (r.Y * r.Y + l.Y * l.Y) * (r.X - l.X) / 2;
                l = l.Next;
                r = r.Next;
            }
            result = Math.Sqrt(result / (Tail.X - Head.X));
            return result;
        }

        /// <summary>
        /// 积分整条曲线（仅检查用）
        /// </summary>
        /// <returns>积分结果</returns>
        public double Integrate()
        {
            double result = 0;
            Point l = Head;
            Point r = Head.Next;
            while (r != null)
            {
                result += (r.Y + l.Y) * (r.X - l.X) / 2;
                l = l.Next;
                r = r.Next;
            }
            return result;
        }

        //TODO 是否拆分该函数？拆分为平移、翻转等
        /// <summary>
        /// 复制一条曲线，可根据附加参数进行变换
        /// </summary>
        /// <param name="ratioY">y轴缩放比</param>
        /// <param name="offsetX">x轴平移量</param>
        /// <param name="offsetY">y轴平移量</param>
        /// <returns>复制的曲线</returns>
        public Curve Copy(double ratioY = 1, double offsetX = 0, double offsetY = 0)
        {
            Curve curve = new Curve();
            Point now = Head;
            while (now != null)
            {
                curve.Add(now.X + offsetX, now.Y * ratioY + offsetY);
                now = now.Next;
            }
            return curve;
        }

        //TODO 无法处理两个端点之间
        /// <summary>
        /// 截断曲线的某一部分，在左右边界添加纵坐标为0的边界点
        /// </summary>
        /// <param name="start">左边界</param>
        /// <param name="end">右边界</param>
        /// <param name="ratioY">y轴缩放比</param>
        /// <returns>截断后的曲线</returns>
        public Curve Cut(double start, double end, double ratioY = 1)
        {
            Curve curve = new Curve();
            bool isFirst = true;
            Point now = Head;
            while (now != null)
            {
                //double运算时会丢失精度，因此等号不一定能判断相等。此外还需考虑特殊情况，如10与9.999999999999以及10.000000000001可以认为相等
                if (Function.GE(now.X, start) && Function.LE(now.X, end))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        if (!Function.EQ(now.X, start) || !Function.EQ(now.Y, 0))
                        {
                            curve.Add(start, 0);
                        }
                    }
                    curve.Add(now.X, now.Y * ratioY);
                }
                now = now.Next;
            }
            if (!isFirst)
            {
                if (!Function.EQ(curve.Tail.X, end) || !Function.EQ(curve.Tail.Y, 0))
                {
                    curve.Add(end, 0);
                }
            }
            return curve;
        }

        //TODO 无法处理两个端点之间
        /// <summary>
        /// 过滤曲线的某一部分，剩余部分变为0，在左右边界添加纵坐标为0的边界点
        /// </summary>
        /// <param name="start">左边界</param>
        /// <param name="end">右边界</param>
        /// <returns>过滤后的曲线</returns>
        public Curve Filter(double start, double end)
        {
            Curve curve = new Curve();
            bool isFirst = true;
            Point now = Head;
            while (now != null)
            {
                //double运算时会丢失精度，因此等号不一定能判断相等。此外还需考虑特殊情况，如10与9.999999999999以及10.000000000001可以认为相等
                if (Function.GE(now.X, start) && Function.LE(now.X, end))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        if (!Function.EQ(now.X, 0) || !Function.EQ(now.Y, 0))
                        {
                            curve.Add(0, 0);
                        }
                        if (!Function.EQ(now.X, start) || !Function.EQ(now.Y, 0))
                        {
                            curve.Add(start, 0);
                        }
                    }
                    curve.Add(now.X, now.Y);
                }
                now = now.Next;
            }
            if (!isFirst)
            {
                if (!Function.EQ(curve.Tail.X, end) || !Function.EQ(curve.Tail.Y, 0))
                {
                    curve.Add(end, 0);
                }
                if (!Function.EQ(curve.Tail.X, Tail.X) || !Function.EQ(curve.Tail.Y, 0))
                {
                    curve.Add(Tail.X, 0);
                }
            }
            return curve;
        }

        /// <summary>
        /// 添加一个特殊点，保持曲线数据顺序不变
        /// </summary>
        /// <param name="x">点的x坐标</param>
        /// <param name="y">点的y坐标</param>
        public void Order(double x, double y)
        {
            Point node = new Point(x, y);
            Point now = Head;
            //首先找到超过特殊点横坐标的点
            while (now != null)
            {
                if (Function.GT(now.X, x))
                {
                    break;
                }
                now = now.Next;
            }

            //前移到上一个点
            if (now == null)
            {
                now = Tail;
            }
            else
            {
                now = now.Prev;
            }

            if (now == null)
            {
                InsertHead(node); //若上一个点为空，则直接在头部插入
            }
            else
            {
                if (!Function.EQ(now.X, x) || !Function.EQ(now.Y, y)) //判重，若上一个点与添加的点不同，则将特殊点插入在上一个点后
                {
                    InsertAfter(node, now);
                }
            }
        }

        /// <summary>
        /// 在指点节点后插入一个节点
        /// </summary>
        /// <param name="node">插入的节点</param>
        /// <param name="now">指定的节点</param>
        private void InsertAfter(Point node, Point now)
        {
            if (now.Next == null)
            {
                Tail = node;
            }
            else
            {
                now.Next.Prev = node;
                node.Next = now.Next;
            }
            now.Next = node;
            node.Prev = now;
            Size++;
        }

        /// <summary>
        /// 在链表头部插入一个节点
        /// </summary>
        /// <param name="node">插入的节点</param>
        private void InsertHead(Point node)
        {
            if (Tail == null)
            {
                Tail = node;
            }
            else
            {
                Head.Prev = node;
                node.Next = Head;
            }
            Head = node;
            Size++;
        }

        /// <summary>
        /// 在链表尾部插入一个节点
        /// </summary>
        /// <param name="node">插入的节点</param>
        private void InsertTail(Point node)
        {
            if (Head == null)
            {
                Head = node;
            }
            else
            {
                Tail.Next = node;
                node.Prev = Tail;
            }
            Tail = node;
            Size++;
        }

        /// <summary>
        /// 删除一个节点
        /// </summary>
        /// <param name="node">删除的节点</param>
        private void Delete(Point node)
        {
            if (node.Prev != null)
            {
                node.Prev.Next = node.Next;
            }
            else
            {
                Head = node.Next;
            }
            if (node.Next != null)
            {
                node.Next.Prev = node.Prev;
            }
            else
            {
                Tail = node.Prev;
            }
            node.Prev = null;
            node.Next = null;
            Size--;
        }
    }
}
