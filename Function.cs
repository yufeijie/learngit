using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PV_analysis
{
    internal static class Function
    {
        public const double ERROR = 1e-12; //最小计算误差，小于该值则认为为0
        public const double ERROR_BIG = 1e-8; //最小计算误差（数字较大），小于该值则认为为0

        /// <summary>
        /// 浮点数比较，判断两个变量是否相等
        /// </summary>
        /// <param name="left">左变量</param>
        /// <param name="right">右变量</param>
        /// <returns>判断结果</returns>
        public static bool EQ(double left, double right)
        {
            if (Math.Min(left, right) < 1e4)
            {
                return Math.Abs(left - right) < ERROR;
            }
            else
            {
                return Math.Abs(left - right) < ERROR_BIG;
            }
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

        /// <summary>
        /// 生成可用模块数序列
        /// </summary>
        /// <param name="min">最少模块数</param>
        /// <param name="max">最多模块数</param>
        /// <returns>可用模块数序列</returns>
        public static int[] GenerateNumberRange(int min, int max)
        {
            return GenerateNumberRange(min, max, 1);
        }

        /// <summary>
        /// 生成可用模块数序列
        /// </summary>
        /// <param name="min">最少模块数</param>
        /// <param name="max">最多模块数</param>
        /// <param name="step">间隔</param>
        /// <returns>可用模块数序列</returns>
        public static int[] GenerateNumberRange(int min, int max, int step)
        {
            List<int> numberRange = new List<int>();
            int n = min;
            while (n <= max)
            {
                numberRange.Add(n);
                n += step;
            }
            return numberRange.ToArray();
        }

        /// <summary>
        /// 生成可用频率序列
        /// </summary>
        /// <param name="min">最低频率</param>
        /// <param name="max">最高频率</param>
        /// <returns>可用频率序列</returns>
        public static double[] GenerateFrequencyRange(double min, double max)
        {
            List<double> frequencyRange = new List<double>();
            double f = min;
            while (f <= max)
            {
                frequencyRange.Add(f);
                if (f < 20)
                {
                    f += 1;
                }
                else
                {
                    if (f < 100)
                    {
                        f += 5;
                    }
                    else
                    {
                        f += 10;
                    }
                }
            }
            return frequencyRange.ToArray();
        }

        /// <summary>
        /// 生成字符串形式的double序列
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="step">步长</param>
        /// <returns>字符串形式的double序列</returns>
        public static string GenerateRangeToString(double min, double max, double step)
        {
            string str = "";
            double v = min;
            while (v <= max)
            {
                str += v.ToString() + ",";
                v += step;
            }
            return string.IsNullOrEmpty(str) ? null : str.Substring(0, str.Length - 1); //删掉最后多余的逗号
        }

        /// <summary>
        /// 可用模块数序列转化为字符串
        /// </summary>
        /// <param name="numberRange">可用模块数序列</param>
        /// <returns>对应字符串</returns>
        public static string IntArrayToString(int[] numberRange)
        {
            String str = "";
            foreach (int n in numberRange)
            {
                str = str + n.ToString() + ",";
            }
            str = str.Substring(0, str.Length - 1);
            return str;
        }

        /// <summary>
        /// 可用拓扑序列转化为字符串
        /// </summary>
        /// <param name="topologyRange">可用拓扑序列</param>
        /// <returns>对应字符串</returns>
        public static string StringArrayToString(string[] topologyRange)
        {
            string str = "";
            if (topologyRange.Length != 0)
            {
                foreach (string to in topologyRange)
                {
                    str = str + to + ",";
                }
                str = str.Substring(0, str.Length - 1);
            }
            return str;
        }

        /// <summary>
        /// double序列转化为字符串
        /// </summary>
        /// <param name="array">序列</param>
        /// <param name="ratio">比例</param>
        /// <returns>对应字符串</returns>
        public static string DoubleArrayToString(double[] array, double ratio = 1)
        {
            string str = "";
            foreach (double a in array)
            {
                str += (a * ratio).ToString() + ",";
            }
            return string.IsNullOrEmpty(str) ? null : str.Substring(0, str.Length - 1); //删掉最后多余的逗号
        }

        /// <summary>
        /// 将字符串转化为double序列
        /// </summary>
        /// <param name="str">字符串形式的double序列</param>
        /// <param name="ratio">放大比例</param>
        /// <returns>double序列</returns>
        public static double[] StringToDoubleArray(string str, double ratio = 1)
        {
            string[] s = str.Split(','); //根据逗号进行分割
            double[] array = new double[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                array[i] = double.Parse(s[i]) * ratio;
            }
            return array;
        }

        /// <summary>
        /// 将字符串转化为int序列
        /// </summary>
        /// <param name="str">字符串形式的int序列</param>
        /// <returns>int序列</returns>
        public static int[] StringToIntArray(string str)
        {
            string[] s = str.Split(','); //根据逗号进行分割
            int[] array = new int[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                array[i] = int.Parse(s[i]);
            }
            return array;
        }

        /// <summary>
        /// 判断字符串是否是数字
        /// </summary>
        /// <param name="str">待判断的字符串</param>
        /// <returns>判断结果</returns>
        public static bool IsNumeric(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;
            return Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$");
        }

    }
}
