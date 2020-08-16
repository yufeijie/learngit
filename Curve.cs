using System;

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

        /// <summary>
        /// 特殊曲线类型，正弦Sine或三角波Triangle
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 幅值
        /// </summary>
        public double Amplitude { get; set; }

        /// <summary>
        /// 频率
        /// </summary>
        public double Frequency { get; set; }

        /// <summary>
        /// 初相角
        /// </summary>
        public double InitialAngle { get; set; }

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
        /// 生成特殊曲线
        /// </summary>
        public void Produce(double start, double end)
        {
            switch (Category)
            {
                case "Sine":
                case "Triangle":
                    double dt = (end - start) / Config.DEGREE;
                    for (int i = 0; i <= Config.DEGREE; i++)
                    {
                        double t = start + dt * i;
                        Add(t, GetValue(t));
                    }
                    break;
            }
        }

        /// <summary>
        /// 比较两条曲线生成驱动波形，若a>b则为1，否则为0
        /// </summary>
        /// <param name="a">曲线a</param>
        /// <param name="b">曲线b</param>
        /// <param name="fs">开关频率</param>
        public void Compare(Curve a, Curve b, double start, double end)
        {
            double dx = 0.5 / b.Frequency;
            double value;
            double l = start;
            double dr = dx - b.Regulation(start);
            if (Function.EQ(dr, 0))
            {
                dr = dx;
            }
            else
            {
                if (dr < 0)
                {
                    dr += dx;
                }
            }
            double r = l + dr;
            while (l < end && !Function.EQ(l, end))
            {
                if ((!Function.EQ(a.GetValue(l), b.GetValue(l)) && a.GetValue(l) > b.GetValue(l)) || (Function.EQ(a.GetValue(l), b.GetValue(l)) && a.GetValue(r) > b.GetValue(r)))
                {
                    value = 1;
                }
                else
                {
                    value = 0;
                }
                double x = this.MySolve(a, b, l, r);
                if (double.IsNaN(x) || Function.EQ(x, l) || Function.EQ(x, r))
                {
                    this.Add(l, value);
                    this.Add(r, value);
                }
                else
                {
                    this.Add(l, value);
                    this.Add(x, value);
                    this.Add(x, 1 - value);
                    this.Add(r, 1 - value);
                }

                l = r;
                r += dx;
                if (r > end || Function.EQ(r, end))
                {
                    r = end;
                }
            }
        }

        /// <summary>
        /// 对驱动波形取反
        /// </summary>
        /// <param name="a">驱动波形</param>
        public void Not(Curve a)
        {
            Point now = a.Head;
            while (now != null)
            {
                Add(now.X, 1 - now.Y);
                now = now.Next;
            }
        }

        /// <summary>
        /// 根据驱动波形，生成实际输出波形
        /// </summary>
        /// <param name="g1">驱动波形1</param>
        /// <param name="g2">驱动波形2</param>
        /// <param name="Vin">输入电压</param>
        public void Drive(Curve g1, Curve g2, double Vin)
        {
            Point p = g1.Head;
            Point q = g2.Head;
            while (p != null || q != null)
            {
                if (Function.EQ(p.X, q.X))
                {
                    this.Add(p.X, (p.Y - q.Y) * Vin);
                    p = p.Next;
                    q = q.Next;
                }
                else
                {
                    if (p.X < q.X)
                    {
                        this.Add(p.X, (p.Y - q.Y) * Vin);
                        p = p.Next;
                    }
                    else
                    {
                        this.Add(q.X, (p.Y - q.Y) * Vin);
                        q = q.Next;
                    }
                }
            }
        }

        /// <summary>
        /// 叠加两个输出波形
        /// </summary>
        /// <param name="a">另一个波形</param>
        public void Plus(Curve a)
        {
            Curve curve = new Curve();

            Point p = Head;
            Point q = a.Head;
            while (p != null || q != null)
            {
                if (p == null)
                {
                    curve.Add(q.X, q.Y);
                    q = q.Next;
                }
                else if (q == null)
                {
                    curve.Add(p.X, p.Y);
                    p = p.Next;
                }
                else if (Function.EQ(p.X, q.X))
                { //精度问题
                    curve.Add(p.X, (p.Y + q.Y));
                    p = p.Next;
                    q = q.Next;
                }
                else if (p.X < q.X)
                {
                    curve.Add(p.X, (p.Y + q.Y));
                    p = p.Next;
                }
                else
                {
                    curve.Add(q.X, (p.Y + q.Y));
                    q = q.Next;
                }
            }

            Size = curve.Size;
            Head = curve.Head;
            Tail = curve.Tail;
        }

        /// <summary>
        /// 根据实际输出电压波形和理想输出电压波形得到电感电流与滤波电感感值
        /// </summary>
        /// <param name="g1">实际输出电压波形</param>
        /// <param name="g2">理想输出电压波形</param>
        /// <param name="currentRippleMax">电流纹波上限</param>
        /// <returns>感值</returns>
        public double CreateCurrentRipple(Curve g1, Curve g2, double currentRippleMax)
        {
            Point l = g1.Head;
            double inductance = 0;
            while (l != null)
            {
                Point r = l.Next;
                if (!Function.EQ(l.Y, r.Y))
                {
                    return Double.NaN;
                }
                double x = MySolve(g2, l.X, r.X, l.Y);
                if (Double.IsNaN(x) || Function.EQ(x, l.X) || Function.EQ(x, r.X))
                {
                    inductance = Math.Max(inductance, Math.Abs(l.Y - g2.GetValue((l.X + r.X) / 2)) * (r.X - l.X) / currentRippleMax);
                }
                else
                {
                    inductance = Math.Max(inductance, Math.Abs(l.Y - g2.GetValue((l.X + x) / 2)) * (x - l.X) / currentRippleMax);
                    inductance = Math.Max(inductance, Math.Abs(r.Y - g2.GetValue((x + r.X) / 2)) * (r.X - x) / currentRippleMax);
                }
                l = r.Next;
            }

            l = g1.Head;

            double c = 0;
            this.Add(l.X, c);
            while (l != null)
            {
                Point r = l.Next;
                double x = MySolve(g2, l.X, r.X, l.Y);
                if (Double.IsNaN(x) || Function.EQ(x, l.X) || Function.EQ(x, r.X))
                {
                    double dc = (l.Y - g2.GetValue((l.X + r.X) / 2)) * (r.X - l.X) / inductance;
                    c += dc;
                    this.Add(r.X, c);
                }
                else
                {
                    double dc = (l.Y - g2.GetValue((l.X + x) / 2)) * (x - l.X) / inductance;
                    c += dc;
                    this.Add(x, c);
                    dc = (r.Y - g2.GetValue((x + r.X) / 2)) * (r.X - x) / inductance;
                    c += dc;
                    this.Add(r.X, c);
                }
                l = r.Next;
            }

            return inductance;
        }           

        /// <summary>
        /// 获取特殊曲线数值
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <returns>对应y坐标</returns>
        public double GetValue(double x)
        {
            double value = double.NaN;
            switch (Category)
            {
                case "Sine":
                    value = Amplitude * Math.Sin(2 * Math.PI * Frequency * x + InitialAngle);
                    break;
                case "Triangle":
                    double xx = Regulation(x);
                    if (xx < 0.5 / Frequency)
                    {
                        value = Amplitude * (1 - 4 * Frequency * xx);
                    }
                    else
                    {
                        value = Amplitude * (-3 + 4 * Frequency * xx);
                    }
                    break;
            }
            return value;
        }

        /// <summary>
        /// 将周期函数的横坐标修正到第一个周期内
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <returns>修正后x坐标</returns>
        private double Regulation(double x)
        {
            double xRegulated = x + InitialAngle / (2 * Math.PI * Frequency);
            xRegulated -= Math.Floor(Frequency * xRegulated) / Frequency;
            return xRegulated;
        }

        /// <summary>
        /// 获取曲线与另一直线在一定范围内的交点
        /// </summary>
        /// <param name="a">曲线</param>
        /// <param name="l">左边界</param>
        /// <param name="r">右边界</param>
        /// <param name="y">直线</param>
        /// <returns></returns>
        private double MySolve(Curve a, double l, double r, double y)
        {
            double y1 = a.GetValue(l) - y; //FIXME 计算精度问题（相等？0？）
            double y2 = a.GetValue(r) - y;
            if (y1 * y2 > 0)
            {
                return double.NaN;
            }

            int k = 0;
            while (k < 100) //迭代上限100次
            {
                double mid = (l + r) / 2;
                y1 = a.GetValue(l) - y;
                y2 = a.GetValue(mid) - y;
                if (y1 * y2 <= 0)
                {
                    r = mid;
                }
                else
                {
                    l = mid;
                }
                k++;
            }

            y1 = a.GetValue(l) - y;
            y2 = a.GetValue(r) - y;
            if (Math.Abs(y1) < Math.Abs(y2))
            {
                return l;
            }
            else
            {
                return r;
            }
        }

        /// <summary>
        /// 获取曲线与另一曲线在一定范围内的交点
        /// </summary>
        /// <param name="a">曲线a</param>
        /// <param name="b">曲线b</param>
        /// <param name="l">左边界</param>
        /// <param name="r">右边界</param>
        /// <returns></returns>
        private double MySolve(Curve a, Curve b, double l, double r)
        {
            double y1 = a.GetValue(l) - b.GetValue(l); //FIXME 计算精度问题（相等？0？）
            double y2 = a.GetValue(r) - b.GetValue(r);
            if (y1 * y2 > 0)
            {
                return double.NaN;
            }

            int k = 0;
            while (k < 100)
            {
                double mid = (l + r) / 2;
                y1 = a.GetValue(l) - b.GetValue(l);
                y2 = a.GetValue(mid) - b.GetValue(mid);
                if (y1 * y2 <= 0)
                {
                    r = mid;
                }
                else
                {
                    l = mid;
                }
                k++;
            }

            y1 = a.GetValue(l) - b.GetValue(l);
            y2 = a.GetValue(r) - b.GetValue(r);
            if (Math.Abs(y1) < Math.Abs(y2))
            {
                return l;
            }
            else
            {
                return r;
            }
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

        //TODO 无法处理边界处于两个端点之间
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
                        if (!Function.EQ(now.Y, 0))
                        {
                            curve.Add(now.X, 0);
                        }
                    }
                    curve.Add(now.X, now.Y * ratioY);
                }
                now = now.Next;
            }
            if (!isFirst)
            {
                if (!Function.EQ(curve.Tail.Y, 0))
                {
                    curve.Add(curve.Tail.X, 0);
                }
            }
            return curve;
        }

        //TODO 无法处理边界处于两个端点之间
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
                        if (!Function.EQ(now.X, Head.X))
                        {
                            curve.Add(0, 0);
                        }
                        if (!Function.EQ(now.Y, 0))
                        {
                            curve.Add(now.X, 0);
                        }
                    }
                    curve.Add(now.X, now.Y);
                }
                now = now.Next;
            }
            if (!isFirst)
            {
                if (!Function.EQ(curve.Tail.Y, 0))
                {
                    curve.Add(curve.Tail.X, 0);
                }
                if (!Function.EQ(curve.Tail.X, Tail.X))
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
