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
    }
}
