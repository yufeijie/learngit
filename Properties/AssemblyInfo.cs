using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的一般信息由以下
// 控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("PV_analysis")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("PV_analysis")]
[assembly: AssemblyCopyright("Copyright ©  2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 会使此程序集中的类型
//对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型
//请将此类型的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("059bb485-5f99-437b-adfc-8bd52dffaada")]

// 程序集的版本信息由下列四个值组成: 
//
//      主版本
//      次版本
//      生成号
//      修订号
//
//可以指定所有这些值，也可以使用“生成号”和“修订号”的默认值
//通过使用 "*"，如下所示:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.9.7")]
[assembly: AssemblyFileVersion("0.9.7")]

// ===版本号规则===
// 主版本号.子版本号.修正版本号
// 当项目在进行了局部修改或 bug 修正时，主版本号和子版本号都不变，修正版本号加1
// 当项目在原有的基础上增加了部分功能时，主版本号不变，子版本号加1，修正版本号复位为0
// 当项目在进行了重大修改或局部修正累积较多，主版本号加1，子版本号复位为0，修正版本号复位为0
// 1.0.0的版本后适用此规则
