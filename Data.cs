using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NPOI.SS.UserModel;
using Org.BouncyCastle.Asn1.Mozilla;

namespace PV_analysis
{
    internal static class Data
    {
        private static readonly string dataPath = Application.StartupPath + "/Resources/data.xlsx"; //数据库文件位置
        private static readonly string resultPath = "Result/"; //输出文件位置

        public class Semiconductor //开关器件数据类
        {
            //基本信息
            public readonly string Type; //型号
            public readonly string Manufacturer; //厂商
            public readonly string Category; //类型（IGBT，SiC-MOSFET单管，SiC模块等）
            public readonly string Configuration; //结构（单管，半桥，全桥等）
            public readonly double Price; //价格
            public readonly double Volume; //体积

            //电气特性
            public readonly double M_Vces; //IGBT耐压
            public readonly double M_Icnom; //IGBT连续电流
            public readonly double M_Vdsmax; //MOSFET耐压
            public readonly double M_Idcon; //MOSFET连续电流
            public readonly double M_Rdson; //MOSFET导通电阻
            //MOSFET特性
            public readonly double M_Vth; //开启电压
            public readonly double M_gfs; //跨导
            public readonly double M_Ciss; //输入电容
            public readonly double M_Coss; //输出电容
            public readonly double M_Crss; //反向传输电容
            public readonly double M_Rg; //内部门极电阻
            public readonly double M_Vgs_h; //正向驱动电压
            public readonly double M_Vgs_l; //反向驱动电压
            public readonly double M_Rg_drive; //驱动电压

            //损耗曲线编号
            public readonly int Id_Vce; //IGBT导通压降曲线编号
            public readonly int Id_Vds; //MOSFET正向导通压降曲线编号
            public readonly int Id_Vsd; //MOSFET反向导通压降曲线编号
            public readonly int Id_Vf; //Diode导通压降曲线编号
            public readonly int Id_Eon; //开通能耗曲线编号
            public readonly int Id_Eoff; //关断能耗曲线编号
            public readonly int Id_Err; //反向恢复能耗曲线编号

            //热特性
            public readonly double IGBT_RthJC; //IGBT结-外壳热阻
            public readonly double IGBT_RthCH; //IGBT外壳-散热器热阻
            public readonly double MOSFET_RthJC; //MOSFET结-外壳热阻
            public readonly double MOSFET_RthCH; //MOSFET外壳-散热器热阻
            public readonly double Diode_RthJC; //Diode结-外壳热阻
            public readonly double Diode_RthCH; //Diode外壳-散热器热阻
            public readonly double Module_RthCH; //模块外壳-散热器热阻

            public Semiconductor(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Manufacturer = row.GetCell(2).StringCellValue;
                Category = row.GetCell(3).StringCellValue;
                Configuration = row.GetCell(4).StringCellValue;
                Price = row.GetCell(17).NumericCellValue;
                Volume = row.GetCell(20).NumericCellValue;
                switch (Category)
                {
                    case "IGBT-Module":
                        M_Vces = row.GetCell(5).NumericCellValue;
                        M_Icnom = row.GetCell(6).NumericCellValue;
                        Id_Vce = (int)row.GetCell(7).NumericCellValue;
                        Id_Vf = (int)row.GetCell(8).NumericCellValue;
                        Id_Eon = (int)row.GetCell(9).NumericCellValue;
                        Id_Eoff = (int)row.GetCell(10).NumericCellValue;
                        Id_Err = (int)row.GetCell(11).NumericCellValue;
                        IGBT_RthJC = row.GetCell(12).NumericCellValue;
                        IGBT_RthCH = row.GetCell(13).NumericCellValue;
                        Diode_RthJC = row.GetCell(14).NumericCellValue;
                        Diode_RthCH = row.GetCell(15).NumericCellValue;
                        Module_RthCH = row.GetCell(16).NumericCellValue;
                        break;
                    case "SiC-Module":
                        M_Vdsmax = row.GetCell(5).NumericCellValue;
                        M_Idcon = row.GetCell(6).NumericCellValue;
                        Id_Vds = (int)row.GetCell(7).NumericCellValue;
                        Id_Vf = (int)row.GetCell(8).NumericCellValue;
                        Id_Eon = (int)row.GetCell(9).NumericCellValue;
                        Id_Eoff = (int)row.GetCell(10).NumericCellValue;
                        Id_Err = (int)row.GetCell(11).NumericCellValue;
                        MOSFET_RthJC = row.GetCell(12).NumericCellValue;
                        Diode_RthJC = row.GetCell(14).NumericCellValue;
                        Module_RthCH = row.GetCell(16).NumericCellValue;
                        break;
                    case "SiC-MOSFET":
                        M_Vdsmax = row.GetCell(5).NumericCellValue;
                        M_Idcon = row.GetCell(6).NumericCellValue;
                        M_Rdson = (int)row.GetCell(7).NumericCellValue;
                        Id_Vsd = (int)row.GetCell(8).NumericCellValue;
                        MOSFET_RthJC = row.GetCell(12).NumericCellValue;
                        MOSFET_RthCH = row.GetCell(16).NumericCellValue;
                        break;
                }
            }
        }

        public class Curve //拟合曲线数据类
        {
            public readonly double M_Vsw; //开通/关断电压

            //a0~a4为拟合曲线参数（四次函数拟合）
            public readonly double M_a0;
            public readonly double M_a1;
            public readonly double M_a2;
            public readonly double M_a3;
            public readonly double M_a4;

            //线性拟合点（x0，y0），若横坐标小于x0则采用线性拟合
            public readonly double M_x0;
            public readonly double M_y0;

            public Curve(IRow row)
            {
                M_Vsw = (row.GetCell(3) != null) ? row.GetCell(3).NumericCellValue : 0;
                M_a0 = row.GetCell(4).NumericCellValue;
                M_a1 = row.GetCell(5).NumericCellValue;
                M_a2 = row.GetCell(6).NumericCellValue;
                M_a3 = row.GetCell(7).NumericCellValue;
                M_a4 = row.GetCell(8).NumericCellValue;
                M_x0 = (row.GetCell(9) != null) ? row.GetCell(9).NumericCellValue : 0;
                M_y0 = (row.GetCell(10) != null) ? row.GetCell(10).NumericCellValue : 0;
            }
        }

        public class Wire //绕线数据类
        {
            //基本信息
            public readonly string Type; //型号
            public readonly string Category; //类型（利兹线，漆包线）
            public readonly double Price; //单位价格

            //参数
            public readonly double M_Ab; //裸线面积
            public readonly double M_A; //截面积
            public readonly double M_Rb; //裸线半径
            public readonly double M_Wn; //并绕股数

            public Wire(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Category = row.GetCell(2).StringCellValue;
                Price = row.GetCell(6).NumericCellValue;
                M_Ab = row.GetCell(3).NumericCellValue;
                M_A = row.GetCell(4).NumericCellValue;
                M_Rb = row.GetCell(7).NumericCellValue;
                M_Wn = row.GetCell(10).NumericCellValue;
            }
        }

        public class Core //绕线数据类
        {
            //基本信息
            public readonly string Type; //型号
            public readonly string Manufacturer; //厂商
            public readonly string Shape; //磁性形状（EE，U等）
            public readonly double Price; //价格
            public readonly double Volume; //体积

            //参数
            public readonly double M_AP; //面积积
            public readonly double M_Aw; //窗口面积
            public readonly double M_MLT; //平均匝长
            public readonly double M_Ae; //有效截面积
            //尺寸规格
            public readonly double M_A; 
            public readonly double M_B;
            public readonly double M_C;
            public readonly double M_D;
            public readonly double M_E;
            public readonly double M_F;

            public Core(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Manufacturer = row.GetCell(3).StringCellValue;
                Shape = row.GetCell(4).StringCellValue;
                Price = row.GetCell(20).NumericCellValue;
                Volume = row.GetCell(24).NumericCellValue;
                M_AP = row.GetCell(5).NumericCellValue;
                M_Aw = row.GetCell(6).NumericCellValue;
                M_MLT = row.GetCell(7).NumericCellValue;
                M_Ae = row.GetCell(9).NumericCellValue;

                switch (Shape)
                {
                    case "EE":
                        M_A = row.GetCell(13).NumericCellValue;
                        M_B = row.GetCell(14).NumericCellValue;
                        M_C = row.GetCell(15).NumericCellValue;
                        M_D = row.GetCell(16).NumericCellValue;
                        M_E = row.GetCell(17).NumericCellValue;
                        M_F = row.GetCell(18).NumericCellValue;
                        break;
                    case "U":
                        M_A = row.GetCell(13).NumericCellValue;
                        M_B = row.GetCell(14).NumericCellValue;
                        M_C = row.GetCell(15).NumericCellValue;
                        M_D = row.GetCell(16).NumericCellValue;
                        M_E = row.GetCell(17).NumericCellValue;
                        break;
                }
            }
        }

        public class Capacitor //电容数据类
        {
            //基本信息
            public readonly string Type; //型号
            public readonly double Price; //价格
            public readonly double Volume; //体积

            //参数
            public readonly double M_Un; //耐压
            public readonly double M_C; //容值
            public readonly double M_Irms; //最大电流有效值
            public readonly double M_Ipeak; //最大电流
            public readonly double M_ESR; //等效串联电阻

            public Capacitor(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Price = row.GetCell(6).NumericCellValue;
                Volume = row.GetCell(9).NumericCellValue;
                M_Un = row.GetCell(2).NumericCellValue;
                M_C = row.GetCell(3).NumericCellValue;
                M_Irms = row.GetCell(4).NumericCellValue;
                M_Ipeak = (row.GetCell(13) != null) ? row.GetCell(13).NumericCellValue : 0;
                M_ESR = row.GetCell(5).NumericCellValue;
            }
        }

        //public static readonly String[,] Characteristic = new String[100, 10]; //开关器件特性数据
        //public static readonly String[,] Wire = new String[120, 10]; //绕线数据
        //public static readonly String[,] Core = new String[100, 25]; //磁芯数据
        //public static readonly String[,] Capacitor = new String[100, 30]; //电容数据

        public static readonly IReadOnlyList<Semiconductor> Semiconductors; //开关器件数据
        public static readonly IReadOnlyList<Curve> Curves; //拟合曲线数据
        public static readonly IReadOnlyList<Wire> Wires; //绕线数据
        public static readonly IReadOnlyList<Core> Cores; //磁芯数据
        public static readonly IReadOnlyList<Capacitor> Capacitors; //电容数据

        static Data()
        {
            //因为IReadOnlyList没有Add方法，这里使用临时变量进行赋值
            List<Semiconductor> semicondutors = new List<Semiconductor>();
            List<Curve> curves = new List<Curve>();
            List<Wire> wires = new List<Wire>();
            List<Core> cores = new List<Core>();
            List<Capacitor> capacitors = new List<Capacitor>();

            //打开Excel
            IWorkbook workbook = WorkbookFactory.Create(dataPath);

            //读取开关器件数据
            ISheet sheet = workbook.GetSheetAt(0); //获取第一个工作薄，从0开始
            for (int i = 1; i <= sheet.LastRowNum; i++) //获取每一行的数据，0对应标题行
            {
                IRow row = sheet.GetRow(i);
                if (row.GetCell(0) == null) { continue; } //NPOI读取空单元格时会为null，对应器件为不可用状态
                string status = row.GetCell(0).StringCellValue;
                if (status.Equals("Y")) //判断器件可用状态
                {
                    semicondutors.Add(new Semiconductor(row)); //添加器件信息
                }
            }

            //读取拟合曲线数据
            sheet = workbook.GetSheetAt(1);
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                curves.Add(new Curve(row));
            }

            //读取绕线数据
            sheet = workbook.GetSheetAt(2);
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row.GetCell(0) == null) { continue; }
                string status = row.GetCell(0).StringCellValue;
                if (status.Equals("Y"))
                {
                    wires.Add(new Wire(row));
                }
            }

            //读取磁芯数据
            sheet = workbook.GetSheetAt(3);
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row.GetCell(0) == null) { continue; }
                string status = row.GetCell(0).StringCellValue;
                if (status.Equals("Y"))
                {
                    cores.Add(new Core(row));
                }
            }

            //读取电容数据
            sheet = workbook.GetSheetAt(4);
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row.GetCell(0) == null) { continue; }
                string status = row.GetCell(0).StringCellValue;
                if (status.Equals("Y"))
                {
                    capacitors.Add(new Capacitor(row));
                }
            }

            //为只读字段赋值
            Semiconductors = semicondutors;
            Curves = curves;
            Wires = wires;
            Cores = cores;
            Capacitors = capacitors;
        }
    }
}