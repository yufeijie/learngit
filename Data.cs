using NPOI.HSSF.Record;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace PV_analysis
{
    /// <summary>
    /// 数据库类，用于访问数据库中各个元件的信息，存放/读取评估结果
    /// </summary>
    internal static class Data
    {
        private static readonly string dataPath = Application.StartupPath + "\\Resources\\data.xlsx"; //数据库文件位置
        private static readonly string resultPath = Application.StartupPath + "\\Results\\"; //默认输出文件位置

        /// <summary>
        /// 默认输出位置
        /// </summary>
        public static string ResultPath { get { return resultPath; } }

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

        //TODO 以下类改为接口
        public class Semiconductor //开关器件数据类
        {
            //基本信息
            public bool Available { get; set; } = true; //可用状态
            public string Type { get; } //型号
            public string Manufacturer { get; } //厂商
            public string Category { get; } //类型（IGBT，SiC-MOSFET单管，SiC模块等）
            public string Configuration { get; } //结构（单管，半桥，全桥等）
            public double Price { get; } //价格(RMB)
            public double Volume { get; } //体积(dm^3)

            //电气特性
            public double Math_Vmax { get; } //IGBT耐压(V)
            public double Math_Imax { get; } //IGBT耐流(A)

            //MOSFET特性
            public double Math_Rdson { get; } //MOSFET导通电阻(Ohm)
            public double Math_Vth { get; } //开启电压(V)
            public double Math_gfs { get; } //跨导(s)
            public double Math_Ciss { get; } //输入电容(pF)
            public double Math_Coss { get; } //输出电容(pF)
            public double Math_Crss { get; } //反向传输电容(pF)
            public double Math_Rg { get; } //内部门极电阻(Ohm)
            public double Math_Vgs_h { get; } //正向驱动电压(V)
            public double Math_Vgs_l { get; } //反向驱动电压(V)
            public double Math_Rg_drive { get; } //驱动电阻(Ohm)

            //损耗曲线编号
            public int Id_Vce { get; } //IGBT导通压降曲线编号
            public int Id_Vds { get; } //MOSFET正向导通压降曲线编号
            public int Id_Vsd { get; } //MOSFET反向导通压降曲线编号
            public int Id_Vf { get; } //Diode导通压降曲线编号
            public int Id_Eon { get; } //开通能耗曲线编号
            public int Id_Eoff { get; } //关断能耗曲线编号
            public int Id_Err { get; } //反向恢复能耗曲线编号

            //热特性
            public double IGBT_RthJC { get; } //IGBT结-外壳热阻(K/W)
            public double IGBT_RthCH { get; } //IGBT外壳-散热器热阻(K/W)
            public double MOSFET_RthJC { get; } //MOSFET结-外壳热阻(K/W)
            public double MOSFET_RthCH { get; } //MOSFET外壳-散热器热阻(K/W)
            public double Diode_RthJC { get; } //Diode结-外壳热阻(K/W)
            public double Diode_RthCH { get; } //Diode外壳-散热器热阻(K/W)
            public double Module_RthCH { get; } //模块外壳-散热器热阻(K/W)

            public Semiconductor(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Manufacturer = row.GetCell(2).StringCellValue;
                Category = row.GetCell(3).StringCellValue;
                Configuration = row.GetCell(4).StringCellValue;
                Price = row.GetCell(17).NumericCellValue;
                Volume = row.GetCell(20).NumericCellValue;
                Math_Vmax = row.GetCell(5).NumericCellValue;
                Math_Imax = row.GetCell(6).NumericCellValue;
                switch (Category)
                {
                    case "IGBT-Module":
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
                        Math_Rdson = (int)row.GetCell(7).NumericCellValue;
                        Id_Vsd = (int)row.GetCell(8).NumericCellValue;
                        MOSFET_RthJC = row.GetCell(12).NumericCellValue;
                        MOSFET_RthCH = row.GetCell(16).NumericCellValue;
                        Math_Vth = row.GetCell(24).NumericCellValue;
                        Math_gfs = row.GetCell(25).NumericCellValue;
                        Math_Ciss = row.GetCell(26).NumericCellValue;
                        Math_Coss = row.GetCell(27).NumericCellValue;
                        Math_Crss = row.GetCell(28).NumericCellValue;
                        Math_Rg = row.GetCell(29).NumericCellValue;
                        Math_Vgs_h = row.GetCell(30).NumericCellValue;
                        Math_Vgs_l = row.GetCell(31).NumericCellValue;
                        Math_Rg_drive = row.GetCell(32).NumericCellValue;
                        break;
                }
            }
        }

        public class Curve //拟合曲线数据类
        {
            public double Math_Vsw { get; } = double.NaN; //开通/关断电压(V)

            //a0~a4为拟合曲线参数（四次函数拟合）
            public double Math_a0 { get; }
            public double Math_a1 { get; }
            public double Math_a2 { get; }
            public double Math_a3 { get; }
            public double Math_a4 { get; }

            //线性拟合点（x0，y0），若横坐标小于x0则采用线性拟合
            public double Math_x0 { get; } = 0;
            public double Math_y0 { get; } = 0;

            /// <summary>
            /// 根据x坐标获取拟合曲线y坐标
            /// </summary>
            /// <param name="x">点的x坐标</param>
            /// <returns>点的y坐标</returns>
            public double GetValue(double x)
            {
                //获取曲线数据
                double value = 0;
                if (Math_x0 > 0 && Math_y0 > 0 && Function.LE(x, Math_x0)) //在x坐标较小时，直接线性化
                {
                    value = x / Math_x0 * Math_y0;
                }
                else
                {
                    value = Math_a0 + Math_a1 * x + Math_a2 * Math.Pow(x, 2) + Math_a3 * Math.Pow(x, 3) + Math_a4 * Math.Pow(x, 4);
                }
                return value;
            }

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
            public bool Available { get; set; } = true; //可用状态
            public string Type { get; } //型号
            public string Category { get; } //类型（利兹线，漆包线）
            public double Price { get; } //单位价格(RMB/unit)

            //参数
            public double Math_Ab { get; } //裸线面积(cm^2/10^3)
            public double Math_A { get; } //截面积(cm^2/10^3)
            public double Math_Db { get; } //裸线直径(mm)
            public double Math_D { get; } //直径(mm)
            public int Math_Wn { get; } //并绕股数

            public Wire(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Category = row.GetCell(2).StringCellValue;
                Price = row.GetCell(6).NumericCellValue;
                Math_Ab = row.GetCell(3).NumericCellValue;
                Math_A = row.GetCell(4).NumericCellValue;
                Math_Db = row.GetCell(7).NumericCellValue;
                Math_D = row.GetCell(8).NumericCellValue;
                Math_Wn = (int)row.GetCell(10).NumericCellValue;
            }
        }

        public class Core //绕线数据类
        {
            //基本信息
            public bool Available { get; set; } = true; //可用状态
            public string Type { get; } //型号
            public string Manufacturer { get; } //厂商
            public string Shape { get; } //磁性形状（EE，U等）
            public double Price { get; } //价格(RMB)
            
            //参数
            public double Math_AP { get; } //面积积(mm^4)
            public double Math_Aw { get; } //窗口面积(mm^2)
            public double Math_MLT { get; } //平均匝长(mm)
            public double Math_Ae { get; } //有效截面积(mm^2)
            public double Math_Ve { get; } //有效磁体积(mm^3) Datasheet中给出的即为一对磁芯的有效磁体积

            //尺寸规格
            public double Math_A { get; } //(mm)
            public double Math_B { get; } //(mm)
            public double Math_C { get; } //(mm)
            public double Math_D { get; } //(mm)
            public double Math_E { get; } //(mm)
            public double Math_F { get; } //(mm)

            public Core(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Manufacturer = row.GetCell(3).StringCellValue;
                Shape = row.GetCell(4).StringCellValue;
                Price = row.GetCell(20).NumericCellValue;
                Math_AP = row.GetCell(5).NumericCellValue;
                Math_Aw = row.GetCell(6).NumericCellValue;
                Math_MLT = row.GetCell(7).NumericCellValue;
                Math_Ae = row.GetCell(9).NumericCellValue;
                Math_Ve = row.GetCell(11).NumericCellValue;
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
            public bool Available { get; set; } = true; //可用状态
            public string Type { get; } //型号
            public string Category { get; } //类型
            public double Price { get; } //价格(RMB)
            public double Volume { get; } //体积(dm^3)

            //参数
            public double Math_Un { get; } //耐压(V)
            public double Math_C { get; } //容值(uF)
            public double Math_Irms { get; } //最大电流有效值(A)
            public double Math_Ipeak { get; } //最大电流(A)
            public double Math_ESR { get; } //等效串联电阻(mOhm)

            public Capacitor(IRow row)
            {
                Type = row.GetCell(1).StringCellValue;
                Category = row.GetCell(2).StringCellValue;
                Price = row.GetCell(7).NumericCellValue;
                Volume = row.GetCell(10).NumericCellValue;
                Math_Un = row.GetCell(3).NumericCellValue;
                Math_C = row.GetCell(4).NumericCellValue;
                Math_Irms = row.GetCell(5).NumericCellValue;
                Math_Ipeak = (row.GetCell(14) != null) ? row.GetCell(14).NumericCellValue : 0;
                Math_ESR = row.GetCell(6).NumericCellValue;
            }
        }

        /// <summary>
        /// 初始化，读取数据库到内存
        /// </summary>
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

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="name">文件名</param>
        /// <param name="conditionTitles">设计条件标题</param>
        /// <param name="conditions">设计条件</param>
        /// <param name="designList">设计方案</param>
        public static void Save(string name, string[] conditionTitles, string[] conditions, ConverterDesignList designList)
        {
            Save(resultPath, name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"), conditionTitles, conditions, designList);
        }

        /// <summary>
        /// 保存设计结果
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="name">文件名</param>
        /// <param name="conditionTitles">设计条件标题</param>
        /// <param name="conditions">设计条件</param>
        /// <param name="designList">设计方案</param>
        public static void Save(string path, string name, string[] conditionTitles, string[] conditions, ConverterDesignList designList)
        {
            IWorkbook workbook = new XSSFWorkbook(); //新建Excel

            //记录设计条件
            ISheet sheet = workbook.CreateSheet("Conditions"); //新建一个工作薄
            IRow row = sheet.CreateRow(0); //设计条件标题
            for (int i = 0; i < conditionTitles.Length; i++)
            {
                row.CreateCell(i).SetCellValue(conditionTitles[i]);
            }
            row = sheet.CreateRow(1); //设计条件
            for (int i = 0; i < conditions.Length; i++)
            {
                row.CreateCell(i).SetCellValue(conditions[i]);
            }

            //记录设计结果
            sheet = workbook.CreateSheet("Results"); //新建一个工作薄
            IConverterDesignData[] designs = designList.GetData();
            for (int i = 0; i < designs.Length; i++)
            {
                row = sheet.CreateRow(i);
                for (int j = 0; j < designs[i].Configs.Length; j++)
                {
                    row.CreateCell(j).SetCellValue(designs[i].Configs[j]);
                }
            }

            //检查文件夹是否存在，若不存在则创建
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            //再次检查，查看文件夹是否创建成功
            if (Directory.Exists(path))
            {
                FileStream file = new FileStream(path + name + ".xlsx", FileMode.Create);
                workbook.Write(file);
                file.Close();
            } //TODO 错误提示
            workbook.Close();
        }

        /// <summary>
        /// 读取设计结果
        /// </summary>
        /// <param name="name">文件名</param>
        /// <param name="n">行号</param>
        /// <returns>设计结果</returns>
        public static string[][] Load(string name, int n)
        {
            List<string> conditions = new List<string>();
            List<string> configs = new List<string>();

            //打开Excel
            IWorkbook workbook = WorkbookFactory.Create(resultPath + name);

            //读取设计条件
            ISheet sheet = workbook.GetSheetAt(0); //获取第二个工作薄
            IRow row = sheet.GetRow(1);
            for (int i = 0; i < row.LastCellNum; i++)
            {
                conditions.Add(row.GetCell(i).StringCellValue);
            }

            //读取设计结果
            sheet = workbook.GetSheetAt(1); //获取第二个工作薄
            row = sheet.GetRow(n);
            for (int i = 0; i < row.LastCellNum; i++)
            {
                configs.Add(row.GetCell(i).StringCellValue);
            }

            return new string[][] { conditions.ToArray(), configs.ToArray() };
        }

        /// <summary>
        /// 读取设计结果
        /// </summary>
        /// <param name="filePath">路径名</param>
        /// <returns>设计结果</returns>
        public static string[][] Load(string filePath)
        {
            string[][] results;

            //打开Excel
            IWorkbook workbook = WorkbookFactory.Create(filePath);

            //读取设计结果
            ISheet sheet = workbook.GetSheetAt(1); //获取第二个工作薄
            IRow row;
            int n = sheet.LastRowNum; //获取行数，这里行数=sheet.LastRowNum+1
            results = new string[n + 2][]; //results[0][]用于存储设计条件
            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                row = sheet.GetRow(i);
                string[] configs = new string[row.LastCellNum];
                for (int j = 0; j < row.LastCellNum; j++)
                {
                    configs[j] = row.GetCell(j).StringCellValue;
                }
                results[i + 1] = configs;
            }

            //读取设计条件
            sheet = workbook.GetSheetAt(0); //获取第二个工作薄
            row = sheet.GetRow(1);
            string[] conditions = new string[row.LastCellNum];
            for (int i = 0; i < row.LastCellNum; i++)
            {
                conditions[i] = row.GetCell(i).StringCellValue;
            }
            results[0] = conditions;

            return results;
        }
    }
}