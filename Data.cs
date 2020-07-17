using System.Collections.Generic;
using System.Windows.Forms;
using NPOI.SS.UserModel;

namespace PV_analysis
{
    internal static class Data
    {
        private static readonly string dataPath = Application.StartupPath + "/Resources/data.xlsx"; //数据库文件位置
        private static readonly string resultPath = "Result/"; //输出文件位置

        //TODO 以下类改为接口
        public class Semiconductor //开关器件数据类
        {
            //基本信息
            public string Type { get; } //型号
            public string Manufacturer { get; } //厂商
            public string Category { get; } //类型（IGBT，SiC-MOSFET单管，SiC模块等）
            public string Configuration { get; } //结构（单管，半桥，全桥等）
            public double Price { get; } //价格
            public double Volume { get; } //体积

            //电气特性
            public double Math_Vces { get; } //IGBT耐压
            public double Math_Icnom { get; } //IGBT连续电流
            public double Math_Vdsmax { get; } //MOSFET耐压
            public double Math_Idcon { get; } //MOSFET连续电流
            public double Math_Rdson { get; } //MOSFET导通电阻
            //MOSFET特性
            public double Math_Vth { get; } //开启电压
            public double Math_gfs { get; } //跨导
            public double Math_Ciss { get; } //输入电容
            public double Math_Coss { get; } //输出电容
            public double Math_Crss { get; } //反向传输电容
            public double Math_Rg { get; } //内部门极电阻
            public double Math_Vgs_h { get; } //正向驱动电压
            public double Math_Vgs_l { get; } //反向驱动电压
            public double Math_Rg_drive { get; } //驱动电压

            //损耗曲线编号
            public int Id_Vce { get; } //IGBT导通压降曲线编号
            public int Id_Vds { get; } //MOSFET正向导通压降曲线编号
            public int Id_Vsd { get; } //MOSFET反向导通压降曲线编号
            public int Id_Vf { get; } //Diode导通压降曲线编号
            public int Id_Eon { get; } //开通能耗曲线编号
            public int Id_Eoff { get; } //关断能耗曲线编号
            public int Id_Err { get; } //反向恢复能耗曲线编号

            //热特性
            public double IGBT_RthJC { get; } //IGBT结-外壳热阻
            public double IGBT_RthCH { get; } //IGBT外壳-散热器热阻
            public double MOSFET_RthJC { get; } //MOSFET结-外壳热阻
            public double MOSFET_RthCH { get; } //MOSFET外壳-散热器热阻
            public double Diode_RthJC { get; } //Diode结-外壳热阻
            public double Diode_RthCH { get; } //Diode外壳-散热器热阻
            public double Module_RthCH { get; } //模块外壳-散热器热阻

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
                        Math_Vces = row.GetCell(5).NumericCellValue;
                        Math_Icnom = row.GetCell(6).NumericCellValue;
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
                        Math_Vdsmax = row.GetCell(5).NumericCellValue;
                        Math_Idcon = row.GetCell(6).NumericCellValue;
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
                        Math_Vdsmax = row.GetCell(5).NumericCellValue;
                        Math_Idcon = row.GetCell(6).NumericCellValue;
                        Math_Rdson = (int)row.GetCell(7).NumericCellValue;
                        Id_Vsd = (int)row.GetCell(8).NumericCellValue;
                        MOSFET_RthJC = row.GetCell(12).NumericCellValue;
                        MOSFET_RthCH = row.GetCell(16).NumericCellValue;
                        break;
                }
            }
        }

        public class Curve //拟合曲线数据类
        {
            public double Math_Vsw { get; } //开通/关断电压

            //a0~a4为拟合曲线参数（四次函数拟合）
            public double Math_a0 { get; }
            public double Math_a1 { get; }
            public double Math_a2 { get; }
            public double Math_a3 { get; }
            public double Math_a4 { get; }

            //线性拟合点（x0，y0），若横坐标小于x0则采用线性拟合
            public double Math_x0 { get; }
            public double Math_y0 { get; }

            public Curve(IRow row)
            {
                Math_Vsw = (row.GetCell(3) != null) ? row.GetCell(3).NumericCellValue : 0;
                Math_a0 = row.GetCell(4).NumericCellValue;
                Math_a1 = row.GetCell(5).NumericCellValue;
                Math_a2 = row.GetCell(6).NumericCellValue;
                Math_a3 = row.GetCell(7).NumericCellValue;
                Math_a4 = row.GetCell(8).NumericCellValue;
                Math_x0 = (row.GetCell(9) != null) ? row.GetCell(9).NumericCellValue : 0;
                Math_y0 = (row.GetCell(10) != null) ? row.GetCell(10).NumericCellValue : 0;
            }
        }

        public class Wire //绕线数据类
        {
            //基本信息
            public string Type { get; } //型号
            public string Category { get; } //类型（利兹线，漆包线）
            public double Price { get; } //单位价格

            //参数
            public double Math_Ab { get; } //裸线面积
            public double Math_A { get; } //截面积
            public double Math_Rb { get; } //裸线半径
            public double Math_Wn { get; } //并绕股数

            public Wire(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Category = row.GetCell(2).StringCellValue;
                Price = row.GetCell(6).NumericCellValue;
                Math_Ab = row.GetCell(3).NumericCellValue;
                Math_A = row.GetCell(4).NumericCellValue;
                Math_Rb = row.GetCell(7).NumericCellValue;
                Math_Wn = row.GetCell(10).NumericCellValue;
            }
        }

        public class Core //绕线数据类
        {
            //基本信息
            public string Type { get; } //型号
            public string Manufacturer { get; } //厂商
            public string Shape { get; } //磁性形状（EE，U等）
            public double Price { get; } //价格
            public double Volume { get; } //体积

            //参数
            public double Math_AP { get; } //面积积
            public double Math_Aw { get; } //窗口面积
            public double Math_MLT { get; } //平均匝长
            public double Math_Ae { get; } //有效截面积
            //尺寸规格
            public double Math_A { get; } 
            public double Math_B { get; }
            public double Math_C { get; }
            public double Math_D { get; }
            public double Math_E { get; }
            public double Math_F { get; }

            public Core(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Manufacturer = row.GetCell(3).StringCellValue;
                Shape = row.GetCell(4).StringCellValue;
                Price = row.GetCell(20).NumericCellValue;
                Volume = row.GetCell(24).NumericCellValue;
                Math_AP = row.GetCell(5).NumericCellValue;
                Math_Aw = row.GetCell(6).NumericCellValue;
                Math_MLT = row.GetCell(7).NumericCellValue;
                Math_Ae = row.GetCell(9).NumericCellValue;
                switch (Shape)
                {
                    case "EE":
                        Math_A = row.GetCell(13).NumericCellValue;
                        Math_B = row.GetCell(14).NumericCellValue;
                        Math_C = row.GetCell(15).NumericCellValue;
                        Math_D = row.GetCell(16).NumericCellValue;
                        Math_E = row.GetCell(17).NumericCellValue;
                        Math_F = row.GetCell(18).NumericCellValue;
                        break;
                    case "U":
                        Math_A = row.GetCell(13).NumericCellValue;
                        Math_B = row.GetCell(14).NumericCellValue;
                        Math_C = row.GetCell(15).NumericCellValue;
                        Math_D = row.GetCell(16).NumericCellValue;
                        Math_E = row.GetCell(17).NumericCellValue;
                        break;
                }
            }
        }

        public class Capacitor //电容数据类
        {
            //基本信息
            public string Type { get; } //型号
            public double Price { get; } //价格
            public double Volume { get; } //体积

            //参数
            public double Math_Un { get; } //耐压
            public double Math_C { get; } //容值
            public double Math_Irms { get; } //最大电流有效值
            public double Math_Ipeak { get; } //最大电流
            public double Math_ESR { get; } //等效串联电阻

            public Capacitor(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Price = row.GetCell(6).NumericCellValue;
                Volume = row.GetCell(9).NumericCellValue;
                Math_Un = row.GetCell(2).NumericCellValue;
                Math_C = row.GetCell(3).NumericCellValue;
                Math_Irms = row.GetCell(4).NumericCellValue;
                Math_Ipeak = (row.GetCell(13) != null) ? row.GetCell(13).NumericCellValue : 0;
                Math_ESR = row.GetCell(5).NumericCellValue;
            }
        }

        /// <summary>
        /// 开关器件数据
        /// </summary>
        public static IReadOnlyList<Semiconductor> SemiconductorList { get; }

        /// <summary>
        /// 拟合曲线数据
        /// </summary>
        public static IReadOnlyList<Curve> CurveList { get; }

        /// <summary>
        /// 绕线数据
        /// </summary>
        public static IReadOnlyList<Wire> WireList { get; }

        /// <summary>
        /// 磁芯数据
        /// </summary>
        public static IReadOnlyList<Core> CoreList { get; }

        /// <summary>
        /// 电容数据
        /// </summary>
        public static IReadOnlyList<Capacitor> CapacitorList { get; }

        static Data()
        {
            //因为IReadOnlyList没有Add方法，这里使用临时变量进行赋值
            List<Semiconductor> semicondutorList = new List<Semiconductor>();
            List<Curve> curveList = new List<Curve>();
            List<Wire> wireList = new List<Wire>();
            List<Core> coreList = new List<Core>();
            List<Capacitor> capacitorList = new List<Capacitor>();

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
                    semicondutorList.Add(new Semiconductor(row)); //添加器件信息
                }
            }
            
            //读取拟合曲线数据
            sheet = workbook.GetSheetAt(1);
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                curveList.Add(new Curve(row));
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
                    wireList.Add(new Wire(row));
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
                    coreList.Add(new Core(row));
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
                    capacitorList.Add(new Capacitor(row));
                }
            }

            //为只读属性赋值
            SemiconductorList = semicondutorList;
            CurveList = curveList;
            WireList = wireList;
            CoreList = coreList;
            CapacitorList = capacitorList;
        }
    }
}