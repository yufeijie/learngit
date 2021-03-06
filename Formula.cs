using MathWorks.MATLAB.NET.Arrays;
using myMATLAB;
using System;

namespace PV_analysis
{
    /// <summary>
    /// 计算公式
    /// </summary>
    internal static class Formula
    {
        public static solve solve; //MATLAB求解对象

        //初始化MATLAB求解对象（MATLAB对象初始化需要一定时间，采用统一对象节省运行时间）
        public static void Init()
        {
            if (solve == null)
            {
                solve = new solve();
            }
        }

        public static double CAC_boost_t3(double D0, double Vo, double fs, double Lr, double wr, double ILmin, int nI)
        {
            double Vcc = CAC_boost_Vcc(D0, Vo);
            double t2 = CAC_boost_t2(D0, Vo, fs, Lr, wr, ILmin, nI);
            double value = Math.Acos(-Vcc / Vo) / wr + t2;
            return value;
        }

        public static double CAC_boost_t2(double D0, double Vo, double fs, double Lr, double wr, double ILmin, int nI)
        {
            double ILr1 = CAC_boost_ILr1(D0, Vo, fs, Lr, wr, ILmin, nI);
            double t1 = CAC_boost_t1(D0, Vo, fs, Lr, wr, ILmin, nI);
            double value = Lr / Vo * ILr1 + t1;
            return value;
        }

        public static double CAC_boost_ILr1(double D0, double Vo, double fs, double Lr, double wr, double ILmin, int nI)
        {
            double Zr = wr * Lr;
            double A0 = CAC_boost_A0(D0, Vo, fs, Lr, wr, ILmin, nI);
            double φ0 = CAC_boost_φ0(D0, Vo, fs, Lr, wr, ILmin, nI);
            double t1 = CAC_boost_t1(D0, Vo, fs, Lr, wr, ILmin, nI);
            double value = nI * ILmin - A0 / Zr * Math.Cos(wr * t1 + φ0);
            return value;
        }

        public static double CAC_boost_t1(double D0, double Vo, double fs, double Lr, double wr, double ILmin, int nI)
        {
            double A0 = CAC_boost_A0(D0, Vo, fs, Lr, wr, ILmin, nI);
            double φ0 = CAC_boost_φ0(D0, Vo, fs, Lr, wr, ILmin, nI);
            double value = (Math.PI - Math.Asin(-Vo / A0) - φ0) / wr;
            return value;
        }

        public static double CAC_boost_φ0(double D0, double Vo, double fs, double Lr, double wr, double ILmin, int nI)
        {
            double Zr = wr * Lr;
            double Vcc = CAC_boost_Vcc(D0, Vo);
            double ILr0 = CAC_boost_ILr0(D0, Vo, fs, Lr, wr);
            double value = Math.Atan(Vcc / (Zr * (nI * ILmin - ILr0)));
            if (Math.Sin(value) <= 0)
            {
                value += Math.PI;
            }
            return value;
        }

        public static double CAC_boost_A0(double D0, double Vo, double fs, double Lr, double wr, double ILmin, int nI)
        {
            double Zr = wr * Lr;
            double Vcc = CAC_boost_Vcc(D0, Vo);
            double ILr0 = CAC_boost_ILr0(D0, Vo, fs, Lr, wr);
            double value = Math.Sqrt(Vcc * Vcc + Math.Pow(Zr * (nI * ILmin - ILr0), 2));
            return value;
        }

        public static double CAC_boost_ILr0(double D0, double Vo, double fs, double Lr, double wr)
        {
            double Zr = wr * Lr;
            double Vcc = CAC_boost_Vcc(D0, Vo);
            double ILr3 = CAC_boost_ILr3(D0, Vo, fs, Zr);
            double value = ILr3 + Vcc / Lr * (1 - D0) / fs;
            return value;
        }

        public static double CAC_boost_ILr3(double D0, double Vo, double fs, double Zr)
        {
            double Vcc = CAC_boost_Vcc(D0, Vo);
            double value = -Math.Sqrt(Vo * Vo - Vcc * Vcc) / Zr;
            return value;
        }

        public static double CAC_boost_Vcc(double D0, double Vo)
        {
            double value = D0 / (1 - D0) * Vo;
            return value;
        }

        public static double DTC_SRC_Ψm(double Vin, double Vp, double Vbase, double T, double Td, double fs, double Q, double M, int mode)
        {
            double value;
            if (mode == 1)
            {
                value = Vp * (0.5 - Td) * T;
            }
            else
            {
                double Te2 = DTC_SRC_Te2(Td, fs, Q, M, mode);
                double Vcrp = DTC_SRC_Vcrpk(Td, fs, Q, M);
                double Ψ1 = Vp * (Te2 - Td) * T;
                double Ψ2 = (Vin - Vbase * Vcrp) * (0.5 - Te2) * T; //FIXME Ψ2<0?
                value = Ψ1 + Ψ2;
            }
            return value;
        }

        public static double DTC_SRC_vcr(double t, double Td, double fs, double Q, double M, int mode)
        {
            double value;
            if (t >= 0.5)
            {
                value = -DTC_SRC_vcr(t - 0.5, Td, fs, Q, M, mode);
            }
            else
            {
                if (t < Td)
                {
                    double Vcrpk = DTC_SRC_Vcrpk(Td, fs, Q, M);
                    value = 1 - (Vcrpk + 1) * Math.Cos(2 * Math.PI * t / fs);
                }
                else
                {
                    double Te2 = DTC_SRC_Te2(Td, fs, Q, M, mode);
                    if (t < Te2)
                    {
                        double R2 = DTC_SRC_R2(Td, fs, Q, M);
                        double BP = DTC_SRC_BP(Td, fs, Q, M);
                        value = (1 - M) - R2 * Math.Cos(2 * Math.PI * (t - Td) / fs + BP);
                    }
                    else
                    {
                        if (mode == 1)
                        {
                            double R3 = DTC_SRC_R3(Td, fs, Q, M);
                            value = (-1 - M) - R3 * Math.Cos(2 * Math.PI * t / fs + Math.PI * (1 - 1 / fs));
                        }
                        else
                        {
                            double Vcrpk = DTC_SRC_Vcrpk(Td, fs, Q, M);
                            value = Vcrpk;
                        }
                    }
                }
            }
            return value;
        }

        public static double DTC_SRC_ilr(double t, double Td, double fs, double Q, double M, int mode)
        {
            double value;
            if (t >= 0.5)
            {
                value = -DTC_SRC_ilr(t - 0.5, Td, fs, Q, M, mode);
            }
            else
            {
                if (t < Td)
                {
                    value = (DTC_SRC_Vcrpk(Td, fs, Q, M) + 1) * Math.Sin(2 * Math.PI * t / fs);
                }
                else
                {
                    double Te2 = DTC_SRC_Te2(Td, fs, Q, M, mode);
                    if (t < Te2)
                    {
                        double R2 = DTC_SRC_R2(Td, fs, Q, M);
                        double BP = DTC_SRC_BP(Td, fs, Q, M);
                        value = R2 * Math.Sin(2 * Math.PI * (t - Td) / fs + BP);
                    }
                    else
                    {
                        if (mode == 1)
                        {
                            double R3 = DTC_SRC_R3(Td, fs, Q, M);
                            value = R3 * Math.Sin(2 * Math.PI * t / fs + Math.PI * (1 - 1 / fs));
                        }
                        else
                        {
                            value = 0;
                        }
                    }
                }
            }
            return value;
        }

        public static double DTC_SRC_Te2(double Td, double fs, double Q, double M, int mode)
        {
            double B = DTC_SRC_B(Td, fs, Q, M, mode);
            double value = Td + B * fs / (2 * Math.PI);
            return value;
        }

        public static double DTC_SRC_B(double Td, double fs, double Q, double M, int mode)
        {
            double BP = DTC_SRC_BP(Td, fs, Q, M);
            double R3 = DTC_SRC_R3(Td, fs, Q, M);
            double value;
            if (mode == 1)
            {
                value = Math.PI - BP - Math.Asin(R3 * Math.Sin(Math.PI / fs - 2 * Math.PI * Td / fs + BP) / 2);
            }
            else
            {
                value = Math.PI - BP;
            }
            return value;
        }

        public static double DTC_SRC_R3(double Td, double fs, double Q, double M)
        {
            double Vcrpk = DTC_SRC_Vcrpk(Td, fs, Q, M);
            double value = Vcrpk + M + 1;
            return value;
        }

        public static double DTC_SRC_R2(double Td, double fs, double Q, double M)
        {
            double Vcrpk = DTC_SRC_Vcrpk(Td, fs, Q, M);
            double value = Math.Sqrt(Math.Pow(Vcrpk + 1, 2) + M * M - 2 * M * (Vcrpk + 1) * Math.Cos(2 * Math.PI * Td / fs));
            return value;
        }

        public static double DTC_SRC_R1(double Td, double fs, double Q, double M)
        {
            double Vcrpk = DTC_SRC_Vcrpk(Td, fs, Q, M);
            double value = Vcrpk + 1;
            return value;
        }

        public static int DTC_SRC_CCMflag(double Td, double fs, double Q, double M)
        {
            double BPccm = DTC_SRC_BP(Td, fs, Q, M);
            if (Math.Sin(Math.PI / fs - 2 * Math.PI * Td / fs + BPccm) >= 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public static double DTC_SRC_BP(double Td, double fs, double Q, double M)
        {
            double Vcrpk = DTC_SRC_Vcrpk(Td, fs, Q, M);
            double value = Atanx((Vcrpk + 1) * Math.Sin(2 * Math.PI * Td / fs) / ((Vcrpk + 1) * Math.Cos(2 * Math.PI * Td / fs) - M));
            return value;
        }

        public static double DTC_SRC_Mgain1(double Td, double fs, double Q)
        {
            double value = -1;
            MWArray output = solve.solve_DTCSRC_Mgain1(Td, fs, Q);
            MWNumericArray result = (MWNumericArray)output;
            value = result.ToScalarDouble();
            if (value < 0)
            {
                Console.WriteLine("Wrong DTC_SRC_Mgain1!");
                System.Environment.Exit(-1);
            }
            return value;
        }

        public static double DTC_SRC_Mgain2(double Td, double fs, double Q)
        {
            double value = -1;
            MWArray output = solve.solve_DTCSRC_Mgain2(Td, fs, Q);
            MWNumericArray result = (MWNumericArray)output;
            value = result.ToScalarDouble();
            if (value < 0)
            {
                Console.WriteLine("Wrong DTC_SRC_Mgain1!");
                System.Environment.Exit(-1);
            }
            return value;
        }

        public static double DTC_SRC_Vcrpk(double Td, double fs, double Q, double M)
        {
            double value = (1 - Math.Cos(2 * Math.PI * Td / fs) + Math.PI / fs * Q * M) / (1 + Math.Cos(2 * Math.PI * Td / fs));
            return value;
        }

        public static double Atanx(double x)
        {
            double value = Math.Atan(x);
            if (value < 0)
            {
                value += Math.PI;
            }
            return value;
        }
    }
}
