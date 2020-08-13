using MathWorks.MATLAB.NET.Arrays;
using PV_analysis.Components;
using PV_analysis.Converters;
using System;

namespace PV_analysis.Topologys
{
    internal class DTCSRC : Topology
    {

        //特殊参数
        private bool isLeakageInductanceIntegrated = true; //是否认为谐振电感集成在变压器中

        //可选参数
        //private double ratioVoltageRipple; //电压纹波系数

        private static readonly double math_kIrip = 0.2; //电流纹波系数
        private static readonly double math_kVrip = 0.1; //电压纹波系数

        private IsolatedDCDCConverter converter; //所属变换器

        //电路参数
        //需优化参数
        private double frequencyResonance; //谐振频率

        //给定参数
        private double voltageInputDef; //模块输入电压预设值
        private double voltageInputMinDef; //模块输入电压预设最小值
        private double voltageInputMaxDef; //模块输入电压预设最大值
        private double voltageOutputDef; //模块输出电压预设值
        private double qualityFactorDef; //品质因数预设值

        //基本电路参数
        private double powerFull; //满载模块功率
        private double power; //功率
        private double voltageInput; //输入电压
        private double voltageOutput; //输出电压
        private double qualityFactor; //品质因数
        private double frequencySwitch; //开关频率
        private double angularVelocityResonance; //谐振角速度
        private double timeCycleSwitch; //开关周期
        private double timeCycleResonance; //谐振周期
        private double resistanceLoad; //负载等效电阻
        private double time0; //过零点
        private double currentOutput; //输出电流
        private double conductionMode; //电流导通模式 0->DCM 1->CCM
        private double gain; //变换器增益
        private double timeDelay; //DTC-SRC第一阶段时间
        private double fluxLinkage; //磁链

        //标幺值参数
        private double voltageBase; //电压基值
        private double currentBase; //电流基值
        private double frequencyBase; //频率基值
        private double timeCycleBase; //周期基值

        //需设计的电路参数
        private double turnRatioTransformer; //变压器匝比
        private double impedanceResonance; //谐振阻抗
        private double inductanceResonance; //谐振电感值
        private double capacitanceResonance; //谐振电容值
        private double deadTime; //死区时间

        //主电路元件参数
        //	private double dutyCycle; //占空比
        private double currentInductorRMS; //电感电流有效值
        private double currentInductorRMSMax; //电感电流有效值最大值
        private double currentInductorPeak; //电感电流峰值
        private double currentInductorMax; //电感电流最大值
        private double voltageCapacitorPeak; //电容电压峰值
        private double voltageCapacitorMax; //电容电压最大值
        private double currentCapacitorFilterRMS; //滤波电容电流有效值
        private double currentCapacitorFilterRMSMax; //滤波电容电流有效值最大值
        private double voltageSwitch_P; //原边开关器件电压
        private double voltageSwitch_S; //副边开关器件电压
        private double voltageSwitch_D; //副边二极管电压
        private double frequencySwitchMax; //最大开关频率
        private double fluxLinkageMax; //最大磁链

        //电压、电流波形
        private Curve currentSwitch_P; //原边开关器件电流波形
        private Curve currentSwitch_S; //副边开关器件电流波形
        private Curve currentSwitch_D; //副边二极管电流波形
        private Curve currentInductor; //谐振电感电流波形
        private Curve voltageCapacitor; //谐振电容电压波形
        private Curve currentCapacitorFilter; //滤波电容电流波形

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="converter">所属变换器</param>
        public DTCSRC(IsolatedDCDCConverter converter)
        {
            this.converter = converter;
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            inductanceResonance = 40.5e-6;
            capacitanceResonance = 1e-6;
            turnRatioTransformer = 1;

            impedanceResonance = Math.Sqrt(inductanceResonance / capacitanceResonance);
            frequencyResonance = 1 / (2 * Math.PI * Math.Sqrt(inductanceResonance * capacitanceResonance));
            voltageOutput = voltageOutputDef;

            voltageBase = voltageOutput;
            currentBase = voltageBase / impedanceResonance;
            frequencyBase = frequencyResonance;
        }

        /// <summary>
        /// 计算电路参数，并模拟电压、电流波形
        /// </summary>
        private void Simulate()
        {
            gain = turnRatioTransformer * voltageOutput / voltageInput;
            resistanceLoad = Math.Pow(voltageOutput, 2) / power;
            qualityFactor = impedanceResonance / (Math.Pow(turnRatioTransformer, 2) * resistanceLoad);
            //求解Vo和t0
            MWArray output = Formula.solve.solve_DTCSRC(qualityFactor, gain);
            MWNumericArray result = (MWNumericArray)output;
            timeDelay = result[1].ToScalarDouble();
            frequencySwitch = result[2].ToScalarDouble();
            if (timeDelay < 0 || timeDelay > 0.5)
            {
                Console.WriteLine("Wrong Td!");
                System.Environment.Exit(-1);
            }
            if (frequencySwitch < 0.75 || frequencySwitch >= 100)
            {
                Console.WriteLine("Wrong fs!");
                System.Environment.Exit(-1);
            }

            if (frequencySwitch > 1.5)
            {//定频控制，求解对应Td
                frequencySwitch = 1.5;
                output = Formula.solve.solve_DTCSRC_Td(qualityFactor, gain, frequencySwitch);
                result = (MWNumericArray)output;
                timeDelay = result.ToScalarDouble();
                if (timeDelay < 0 || timeDelay > 0.5)
                {
                    Console.WriteLine("Wrong Td!");
                    System.Environment.Exit(-1);
                }
            }
            timeCycleSwitch = 1 / (frequencySwitch * frequencyBase);
            timeCycleBase = timeCycleSwitch;
            voltageCapacitorPeak = voltageBase * Formula.DTC_SRC_Vcrpk(timeDelay, frequencySwitch, qualityFactor, gain);
            currentOutput = voltageOutput / resistanceLoad;
            fluxLinkage = Formula.DTC_SRC_Ψm(voltageInput, voltageOutput * turnRatioTransformer, voltageBase, timeCycleSwitch, timeDelay, frequencySwitch, qualityFactor, gain, conductionMode);

            currentInductorPeak = 0;
            currentInductor = new Curve();
            voltageCapacitor = new Curve();
            double startTime = 0;
            double endTime = 1;
            double dt = (endTime - startTime) / Config.DEGREE;
            for (int i = 0; i <= Config.DEGREE; i++)
            {
                double t = startTime + dt * i;
                double iLr = Formula.DTC_SRC_ilr(t, timeDelay, frequencySwitch, qualityFactor, gain, conductionMode);
                double vCr = Formula.DTC_SRC_vcr(t, timeDelay, frequencySwitch, qualityFactor, gain, conductionMode);
                currentInductor.Add(timeCycleBase * t, currentBase * iLr);
                voltageCapacitor.Add(timeCycleBase * t, voltageBase * vCr);
                currentInductorPeak = Math.Max(currentInductorPeak, Math.Abs(currentBase * iLr)); //记录峰值
            }
            //补充特殊点（保证现有的开关器件损耗计算中，判断开通/关断/导通状态的部分正确） FIXME 更好的方法？
            double Ts = timeCycleBase;
            double Td = timeCycleBase * timeDelay;
            double Te2 = timeCycleBase * Formula.DTC_SRC_Te2(timeDelay, frequencySwitch, qualityFactor, gain, conductionMode);
            currentInductor.Order(0, 0);
            currentInductor.Order(Ts / 2, 0);
            //生成主电路元件波形
            currentSwitch_P = currentInductor.Cut(Te2, Te2 + Ts / 2, -1);
            currentSwitch_S = currentInductor.Cut(0, Td + Ts / 2, -turnRatioTransformer);
            currentSwitch_D = currentInductor.Cut(Td, Ts / 2, turnRatioTransformer);
            voltageSwitch_P = voltageInput;
            voltageSwitch_S = voltageOutput;
            voltageSwitch_D = voltageOutput;
            currentCapacitorFilter = currentSwitch_D.Copy(1, 0, -currentOutput);
            currentCapacitorFilter.Order(0, -currentOutput);
            currentCapacitorFilter.Order(Td, -currentOutput);
            //计算有效值
            currentInductorRMS = currentInductor.CalcRMS();
            currentCapacitorFilterRMS = currentCapacitorFilter.CalcRMS();
            //记录最大值
            currentInductorMax = Math.Max(currentInductorMax, currentInductorPeak);
            voltageCapacitorMax = Math.Max(voltageCapacitorMax, voltageCapacitorPeak);
            currentInductorRMSMax = Math.Max(currentInductorRMSMax, currentInductorRMS);
            currentCapacitorFilterRMSMax = Math.Max(currentCapacitorFilterRMSMax, currentCapacitorFilterRMS);
        }

        /// <summary>
        /// 自动设计，得到每个器件的设计方案
        /// </summary>
        public override void Design()
        {
            //初始化
            DualModule primaryDualModule = new DualModule(2);
            DualModule MOSFET = new DualModule(2);
            DualModule secondaryDualModule = new DualModule(1); //TODO 此处应为二极管
            Inductor resonantInductor = new Inductor(1);
            Transformer transformer = new Transformer(1);
            Capacitor resonantCapacitor = new Capacitor(1);
            Capacitor filteringCapacitor = new Capacitor(1);
            components = new Component[] { primaryDualModule, MOSFET, secondaryDualModule, transformer, resonantCapacitor, filteringCapacitor };
            componentGroups = new Component[1][];
            componentGroups[0] = new Component[] { primaryDualModule, MOSFET, secondaryDualModule, transformer, resonantCapacitor, filteringCapacitor };

            //获取设计规格
            powerFull = converter.Math_Psys / converter.PhaseNum / converter.Number;
            voltageInputMinDef = converter.Math_Vin_min;
            voltageInputMaxDef = converter.Math_Vin_max;
            voltageOutputDef = converter.Math_Vo;
            frequencyResonance = converter.Math_fr;
            qualityFactorDef = converter.Math_Q;

            //计算电路参数
            DesignCircuitParam();
            currentInductorMax = 0;
            currentInductorRMSMax = 0;
            voltageCapacitorMax = 0;
            currentCapacitorFilterRMSMax = 0;
            int m = Config.CGC_VOLTAGE_RATIO.Length;
            int n = Config.CGC_POWER_RATIO.Length;

            currentInductorMax = 0;
            voltageCapacitorMax = 0;
            currentInductorRMSMax = 0;
            currentCapacitorFilterRMSMax = 0;
            frequencySwitchMax = 0;
            fluxLinkageMax = 0;

            //得到用于效率评估的不同输入电压与不同功率点的电路参数
            for (int i = 0; i < m; i++)
            {
                voltageInput = voltageInputMinDef + (voltageInputMaxDef - voltageInputMinDef) * Config.CGC_VOLTAGE_RATIO[i];
                for (int j = 0; j < n; j++)
                {
                    power = powerFull * Config.CGC_POWER_RATIO[j]; //改变模块功率
                    Simulate();
                    //Graph graph = new Graph();
                    //graph.Add(currentSwitch_P, "iP");
                    //graph.Add(currentSwitch_S, "iS");
                    //graph.Draw();
                    currentInductorMax = Math.Max(currentInductorMax, currentInductorPeak);
                    currentInductorRMSMax = Math.Max(currentInductorRMSMax, currentInductorRMS);
                    voltageCapacitorMax = Math.Max(voltageCapacitorMax, voltageCapacitorPeak);
                    currentCapacitorFilterRMSMax = Math.Max(currentCapacitorFilterRMSMax, currentCapacitorFilterRMS);
                    frequencySwitch *= frequencyBase; //还原实际值
                    frequencySwitchMax = Math.Max(frequencySwitchMax, frequencySwitch);
                    fluxLinkageMax = Math.Max(fluxLinkageMax, fluxLinkage);

                    //设置元器件的电路参数（用于评估）
                    primaryDualModule.AddEvalParameters(i, j, voltageSwitch_P, currentSwitch_P, frequencySwitch);
                    MOSFET.AddEvalParameters(i, j, voltageSwitch_S, currentSwitch_S, frequencySwitch);
                    secondaryDualModule.AddEvalParameters(i, j, voltageSwitch_D, currentSwitch_D.Copy(-1), frequencySwitch);
                    resonantInductor.AddEvalParameters(i, j, currentInductorRMS, currentInductorPeak * 2, frequencySwitch);
                    transformer.AddEvalParameters(i, j, currentInductorRMS, currentInductorPeak * 2, frequencySwitch, fluxLinkage);
                    resonantCapacitor.AddEvalParameters(i, j, currentInductorRMS);
                    filteringCapacitor.AddEvalParameters(i, j, currentCapacitorFilterRMS);
                }
            }

            //若认为谐振电感集成在变压器中，则不考虑额外谐振电感
            //if (this.isLeakageInductanceIntegrated)
            //{
            //    this.deviceInductorNum = 0;
            //}

            //设置元器件的设计条件
            primaryDualModule.SetConditions(voltageInputMaxDef, currentInductorMax, frequencySwitchMax); //TODO 电流取RMS最大值 or 最大值？
            MOSFET.SetConditions(voltageOutputDef, turnRatioTransformer * currentInductorMax, frequencySwitchMax);
            secondaryDualModule.SetConditions(voltageOutputDef, turnRatioTransformer * currentInductorMax, frequencySwitchMax);
            resonantInductor.SetConditions(inductanceResonance, currentInductorMax, frequencySwitchMax);
            transformer.SetConditions(power, currentInductorMax, frequencySwitchMax, turnRatioTransformer, fluxLinkageMax); //FIXME 磁链是否会变化？
            resonantCapacitor.SetConditions(capacitanceResonance, voltageCapacitorMax, currentInductorRMSMax);
            filteringCapacitor.SetConditions(200 * 1e-6, voltageOutputDef, currentCapacitorFilterRMSMax);

            foreach (Component component in components)
            {
                component.Design();
            }
        }
    }
}
