namespace PV_analysis.Systems
{
    internal abstract class Structure
    {
        //---整体参数---
        /// <summary>
        /// 架构总功率
        /// </summary>
        public double Math_Psys { get; set; }

        /// <summary>
        /// 光伏板MPPT电压最小值
        /// </summary>
        public double Math_Vpv_min { get; set; }

        /// <summary>
        /// 光伏板MPPT电压最大值
        /// </summary>
        public double Math_Vpv_max { get; set; }

        /// <summary>
        /// 并网电压（线电压）
        /// </summary>
        public double Math_Vg { get; set; }

        /// <summary>
        /// 输出电压（并网相电压）
        /// </summary>
        public double Math_Vo { get; set; }

        /// <summary>
        /// 并网频率
        /// </summary>
        public double Math_fg { get; set; }

        /// <summary>
        /// 母线电压范围
        /// </summary>
        public double[] Math_VbusRange { get; set; }

        /// <summary>
        /// 功率因数角(rad)
        /// </summary>
        public double Math_phi { get; set; }

        /// <summary>
        /// DCAC直流侧电压预设值
        /// </summary>
        public double DCAC_Vin_def { get; set; }

        //---DC/DC参数---
        /// <summary>
        /// DCDC可用模块数序列
        /// </summary>
        public int[] DCDC_numberRange { get; set; }

        /// <summary>
        /// DCDC可用拓扑序列
        /// </summary>
        public string[] DCDC_topologyRange { get; set; }

        /// <summary>
        /// DCDC可用开关频率序列
        /// </summary>
        public double[] DCDC_frequencyRange { get; set; }

        //---隔离DC/DC参数---
        /// <summary>
        /// 品质因数预设值
        /// </summary>
        public double IsolatedDCDC_Q { get; set; }

        /// <summary>
        /// 隔离DCDC可用拓扑序列
        /// </summary>
        public string[] IsolatedDCDC_topologyRange { get; set; }

        /// <summary>
        /// 隔离DCDC可用谐振频率序列
        /// </summary>
        public double[] IsolatedDCDC_resonanceFrequencyRange { get; set; }

        //---DC/AC参数---
        /// <summary>
        /// DCAC可用模块数序列，隔离DCDC与此同
        /// </summary>
        public int[] DCAC_numberRange { get; set; }

        /// <summary>
        /// DCAC可用拓扑序列
        /// </summary>
        public string[] DCAC_topologyRange { get; set; }

        /// <summary>
        /// DCAC可用调制方式序列
        /// </summary>
        public string[] DCAC_modulationRange { get; set; }

        /// <summary>
        /// DCAC可用开关频率序列
        /// </summary>
        public double[] DCAC_frequencyRange { get; set; }

        /// <summary>
        /// Pareto最优设计方案
        /// </summary>
        public ConverterDesignList ParetoDesignList { get; } = new ConverterDesignList();

        /// <summary>
        /// 所有设计方案
        /// </summary>
        public ConverterDesignList AllDesignList { get; } = new ConverterDesignList { IsAll = true };

        /// <summary>
        /// 获取设计条件标题
        /// </summary>
        /// <returns>配置信息</returns>
        public abstract string[] GetConditionTitles();

        /// <summary>
        /// 获取设计条件
        /// </summary>
        /// <returns>配置信息</returns>
        public abstract string[] GetConditions();

        /// <summary>
        /// 根据给定的条件，对变换器进行优化设计
        /// </summary>
        public abstract void Optimize();

        /// <summary>
        /// 保存设计结果
        /// </summary>
        public void Save()
        {
            string[] conditionTitles = GetConditionTitles();
            string[] conditions = GetConditions();
            Data.Save(GetType().Name + "_Pareto", conditionTitles, conditions, ParetoDesignList);
            Data.Save(GetType().Name + "_all", conditionTitles, conditions, AllDesignList);
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        /// <param name="configs">配置信息</param>
        /// <param name="index">当前下标</param>
        public abstract void Load(string[] configs, int index);
    }
}
