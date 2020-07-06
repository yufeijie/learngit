using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace PV_analysis
{
    internal static class Data
    {
        public struct Semicondutor
        {
            public String type;
            public String manufacturer;
            public String Category;
            public String Configuration;
            public Double Vces;
            public Double Icnom;
        }

        private static String resultPath = "../Result/";

        public static int switchLength = 0; //开关器件型号数量
        public static int curveLength = 0; //拟合曲线数量
        public static int characteristicsLength = 0; //开关器件特性数据数量
        public static int wireLength = 0; //绕线型号数量
        public static int coreLength = 0; //磁芯型号数量
        public static int capacitorLength = 0; //电容型号数量

        //public static readonly String[,] Semiconductor = new String[100, 24]; //开关器件数据
        //public static readonly String[,] Curve = new String[200, 13]; //拟合曲线数据
        //public static readonly String[,] Characteristics = new String[100, 10]; //开关器件特性数据
        //public static readonly String[,] Wire = new String[120, 10]; //绕线数据
        //public static readonly String[,] Core = new String[100, 25]; //磁芯数据
        //public static readonly String[,] Capacitor = new String[100, 30]; //电容数据

        public static readonly List<Semicondutor> sc;

        public static void Init()
        {
            CreateDB();
        }

        private static void CreateDB()
        {
            string path = "data.sqlite";
            SQLiteConnection cn = new SQLiteConnection("data source=" + path);
            cn.Open();
            cn.Close();
        }
    }
}