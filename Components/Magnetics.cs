using PV_analysis.Informations;
using System;
using System.Collections.Generic;

namespace PV_analysis.Components
{
    /// <summary>
    /// 磁性元件抽象类
    /// </summary>
    internal abstract class Magnetics : Component
    {
        //常量
        protected static readonly double math_μ0 = Configuration.ABSOLUTE_PERMEABILITY; //绝对磁导率(H/m) [MKS] or 1(Gs/Oe) [CGS]
        protected static readonly double math_ρCu = Configuration.COPPER_RESISTIVITY; //铜电阻率
        protected static readonly double math_μCu = Configuration.COPPER_RELATIVE_PERMEABILITY; //铜相对磁导率 0.9999912

        //器件参数
        protected string material = "Ferrite"; //材料：铁氧体Ferrite，非晶Amorphous
        protected int core; //磁芯编号
        protected int numberCore; //磁芯数量(单位:对)

        //损耗参数（同类器件中其中一个的损耗）
        protected double powerLossCu; //单个电感铜损(W)
        protected double powerLossFe; //单个电感铁损(W)

        //成本参数（同类器件中其中一个的损耗）
        protected double costCore; //单个磁芯成本
        protected double costWire; //单个绕线成本

        /// <summary>
        /// 获取磁芯型号
        /// </summary>
        /// <returns>型号</returns>
        protected string GetCoreType()
        {
            return Data.CoreList[core].Type;
        }

        /// <summary>
        /// 设置磁芯型号
        /// </summary>
        protected void SetCoreType(string type)
        {
            for (int i = 0; i < Data.CoreList.Count; i++)
            {
                if (type.Equals(Data.CoreList[i].Type))
                {
                    core = i;
                    return;
                }
            }
            core = -1;
        }

        /// <summary>
        /// 获取绕线型号
        /// </summary>
        /// <returns>型号</returns>
        protected string GetWireType(int wire)
        {
            return Data.WireList[wire].Type;
        }

        /// <summary>
        /// 获取绕线编号
        /// </summary>
        /// <returns>编号</returns>
        protected int GetWireId(string type)
        {
            for (int i = 0; i < Data.WireList.Count; i++)
            {
                if (type.Equals(Data.WireList[i].Type))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取成本分布
        /// </summary>
        /// <returns>成本分布信息</returns>
        public override List<Info> GetCostBreakdown()
        {
            List<Info> list = new List<Info>()
            {
                new Info(Name, Math.Round(number * (costCore + costWire), 2))
            };
            return list;
        }

        /// <summary>
        /// 获取体积分布
        /// </summary>
        /// <returns>体积分布信息</returns>
        public override List<Info> GetVolumeBreakdown()
        {
            List<Info> list = new List<Info>()
            {
                new Info(Name, Math.Round(Volume, 2))
            };
            return list;
        }

        /// <summary>
        /// 获取铁损（DMR95）
        /// </summary>
        /// <param name="f">开关频率(Hz)</param>
        /// <param name="B">交流磁通密度(T)</param>
        /// <param name="V">磁芯体积(dm^3)</param>
        /// <returns>铁损(W)</returns>
        protected double GetInductanceFeLoss(double f, double B, double V)
        {
            //Steinmetz方程
            double Cm = 8.468305e-8;
            double a = 1.8787258;
            double b = 2.52072788;
            double ct0 = 1.47462783;
            double ct1 = 0.0149349514;
            double ct2 = 0.000103236;
            double T = 50; //温度(℃)
            double ct = ct0 - ct1 * T + ct2 * T * T;
            return ct * Cm * Math.Pow(f / 1e3, a) * Math.Pow(B * 1e3, b) * V;
        }

        /// <summary>
        /// 获取铁损（N27 20oC）
        /// </summary>
        /// <param name="f">开关频率(Hz)</param>
        /// <param name="B">交流磁通密度(T)</param>
        /// <returns>单位体积铁损(W/m^3)</returns>
        protected double GetInductanceFeLoss_N27(double f, double B)
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
