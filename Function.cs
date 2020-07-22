using System;

namespace PV_analysis
{
    internal static class Function
    {
        public const double ERROR = 1e-12; //最小计算误差，小于该值则认为为0

        /// <summary>
        /// 浮点数比较，判断两个变量是否相等
        /// </summary>
        /// <param name="left">左变量</param>
        /// <param name="right">右变量</param>
        /// <returns>判断结果</returns>
        public static bool EQ(double left, double right)
        {
            return Math.Abs(left - right) < ERROR;
        }

        /// <summary>
        /// 浮点数比较，判断左变量是否大于右变量
        /// </summary>
        /// <param name="left">左变量</param>
        /// <param name="right">右变量</param>
        /// <returns>判断结果</returns>
        public static bool GT(double left, double right)
        {
            return left > right && !EQ(left, right);
        }

        /// <summary>
        /// 浮点数比较，判断左变量是否小于右变量
        /// </summary>
        /// <param name="left">左变量</param>
        /// <param name="right">右变量</param>
        /// <returns>判断结果</returns>
        public static bool LT(double left, double right)
        {
            return left < right && !EQ(left, right);
        }

        /// <summary>
        /// 浮点数比较，判断左变量是否大于等于右变量
        /// </summary>
        /// <param name="left">左变量</param>
        /// <param name="right">右变量</param>
        /// <returns>判断结果</returns>
        public static bool GE(double left, double right)
        {
            return left > right || EQ(left, right);
        }

        /// <summary>
        /// 浮点数比较，判断左变量是否小于等于右变量
        /// </summary>
        /// <param name="left">左变量</param>
        /// <param name="right">右变量</param>
        /// <returns>判断结果</returns>
        public static bool LE(double left, double right)
        {
            return left < right || EQ(left, right);
        }

        /// <summary>
        /// 两个一次函数乘积的快速积分法（牛顿-莱布尼茨公式）
        /// </summary>
        /// <param name="x1">左边界x坐标</param>
        /// <param name="f1">左边界f(x)坐标</param>
        /// <param name="g1">左边界g(x)坐标</param>
        /// <param name="x2">右边界x坐标</param>
        /// <param name="f2">右边界f(x)坐标</param>
        /// <param name="g2">右边界g(x)坐标</param>
        /// <returns>积分结果</returns>
        public static double IntegrateTwoLinear(double x1, double f1, double g1, double x2, double f2, double g2)
        {
            if (EQ(x1, x2))
            {
                return 0; //左右边界相等，积分结果为0
            }
            //f(x)的系数
            double k1 = (f2 - f1) / (x2 - x1);
            double b1 = f1;
            //g(x)的系数
            double k2 = (g2 - g1) / (x2 - x1);
            double b2 = g1;
            //由牛顿-莱布尼茨公式得到积分结果
            return k1 * k2 * Math.Pow(x2 - x1, 3) / 3 + (k1 * b2 + k2 * b1) * Math.Pow(x2 - x1, 2) / 2 + b1 * b2 * (x2 - x1);
        }
    }
}
