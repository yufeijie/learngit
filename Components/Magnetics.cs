﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PV_analysis.Components
{
    internal abstract class Magnetics : Component
    {
        //常量
        protected const double miu0 = 4 * Math.PI * 1e-7; //绝对磁导率(H/m) [MKS] or 1(Gs/Oe) [CGS]
        protected const double lowCu = 1.724 * 1e-8; //铜电阻率
        protected const double miuCu = 1; //铜相对磁导率 0.9999912

        //限制条件
        protected static readonly int numberCoreMax = 10; //磁芯数量最大值

        //器件参数
        protected String material = "Ferrite"; //材料：铁氧体Ferrite，非晶Amorphous
        protected int core; //磁芯编号
        protected int numberCore; //磁芯数量(单位:对)
        protected int wire; //绕线编号

        //损耗参数（同类器件中其中一个的损耗）
        protected double powerLossCu; //单个电感铜损(W)
        protected double powerLossFe; //单个电感铁损(W)

        //成本参数（同类器件中其中一个的损耗）
        protected double costCore; //单个磁芯成本
        protected double costWire; //单个绕线成本

        /// <summary>
        /// 获取磁芯的型号
        /// </summary>
        /// <returns>型号</returns>
        protected string GetCoreType()
        {
            return Data.CoreList[core].Type;
        }

        /// <summary>
        /// 获取绕线的型号
        /// </summary>
        /// <returns>型号</returns>
        protected string GetWireType()
        {
            return Data.WireList[wire].Type;
        }

        /// <summary>
        /// 获取铁损（N27 20oC）
        /// </summary>
        /// <param name="f">开关频率(Hz)</param>
        /// <param name="B">交流磁通密度(T)</param>
        /// <returns>单位体积铁损(W/m^3)</returns>
        protected double GetInductanceFeLoss(double f, double B)
        {
            //磁芯损耗曲线拟合参数
            double k = 1.212;
            double b = -1.308;
            double d = 0.646;
            double y = k * Math.Log10(f / 1e3) + b + d / Math.Log(2) * Math.Log(B * 1e3 / 25);
            return Math.Pow(10, y) * 1e3;
        }
    }
}
