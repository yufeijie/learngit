﻿using MathWorks.MATLAB.NET.Arrays;
using PV_analysis.Components;
using PV_analysis.Converters;
using System;

namespace PV_analysis.Topologys
{
    /// <summary>
    /// SRC拓扑
    /// </summary>
    internal class SRC : Topology
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
        private int topology; //拓扑编号  0对应SRC
        private double frequencyResonance; //谐振频率

        //给定参数
        private double powerPhase; //单相总功率
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
        private double voltageSwitch_P; //开关器件电压
        private double voltageSwitch_S; //开关器件电压

        //电压、电流波形
        private Curve currentSwitch_P; //原边开关器件电流波形
        private Curve currentSwitch_S; //副边开关器件电流波形
        private Curve currentInductor; //谐振电感电流波形
        private Curve voltageCapacitor; //谐振电容电压波形
        private Curve currentCapacitorFilter; //滤波电容电流波形

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="converter">所属变换器</param>
        public SRC(IsolatedDCDCConverter converter)
        {
            this.converter = converter;
        }

        /// <summary>
        /// 设计主电路元件参数
        /// </summary>
        private void DesignCircuitParam()
        {
            timeCycleResonance = 1 / frequencyResonance;
            deadTime = timeCycleResonance / 50;
            angularVelocityResonance = 2 * Math.PI * frequencyResonance;
            resistanceLoad = Math.Pow(voltageOutputDef, 2) / power;
            turnRatioTransformer = voltageInputDef / voltageOutputDef;
            impedanceResonance = qualityFactorDef * Math.Pow(turnRatioTransformer, 2) * resistanceLoad;
            inductanceResonance = impedanceResonance / angularVelocityResonance;
            capacitanceResonance = 1 / impedanceResonance / angularVelocityResonance;
            //求解fs
            MWArray output = Formula.solve.solveSRC_fs(qualityFactorDef, voltageInput, turnRatioTransformer, deadTime, frequencyResonance);
            MWNumericArray result = (MWNumericArray)output;
            frequencySwitch = result.ToScalarDouble();
            if (frequencySwitch < frequencyResonance || frequencySwitch > frequencyResonance * 2)
            {
                Console.WriteLine("Wrong fs!");
                System.Environment.Exit(-1);
            }
            timeCycleSwitch = 1 / frequencySwitch;
        }

        /// <summary>
        /// 计算电路参数，并模拟电压、电流波形
        /// </summary>
        private void Simulate()
        {
            resistanceLoad = Math.Pow(voltageOutputDef, 2) / power;
            qualityFactor = impedanceResonance / (Math.Pow(turnRatioTransformer, 2) * resistanceLoad);
            //求解Vo和t0
            MWArray[] output = Formula.solve.solveSRC(2, qualityFactor, voltageInput, turnRatioTransformer, frequencySwitch, frequencyResonance);
            MWNumericArray result1 = (MWNumericArray)output[0];
            MWNumericArray result2 = (MWNumericArray)output[1];
            voltageOutput = result1.ToScalarDouble();
            time0 = result2.ToScalarDouble();
            if (voltageOutput < voltageOutputDef * 0.8)
            {
                Console.WriteLine("Wrong Vo!");
                System.Environment.Exit(-1);
            }
            if (time0 < 0 || time0 >= timeCycleSwitch / 2)
            {
                Console.WriteLine("Wrong t0!");
                System.Environment.Exit(-1);
            }
            voltageCapacitorPeak = Formula.SRC_Vcrp(turnRatioTransformer, qualityFactor, voltageOutput, frequencyResonance, frequencySwitch);
            currentOutput = voltageOutput / resistanceLoad;
            fluxLinkage = 0.5 * voltageOutput * turnRatioTransformer * timeCycleSwitch;

            currentInductorPeak = 0;
            currentInductor = new Curve();
            voltageCapacitor = new Curve();
            double startTime = 0;
            double endTime = timeCycleSwitch;
            double dt = (endTime - startTime) / Config.DEGREE;
            double t = 0;
            for (int i = 0; i <= Config.DEGREE; i++)
            {
                t = startTime + dt * i;
                double vab = Formula.SRC_vab(t, timeCycleSwitch, voltageInput);
                double vTp = Formula.SRC_vTp(t, timeCycleSwitch, time0, turnRatioTransformer, voltageOutput);
                double iLrp = Formula.SRC_ilrp(t, timeCycleSwitch, voltageCapacitorPeak, vab, vTp, impedanceResonance);
                double iLr = Formula.SRC_ilr(t, timeCycleSwitch, time0, iLrp, angularVelocityResonance);
                double vCr = Formula.SRC_vcr(t, timeCycleSwitch, time0, voltageCapacitorPeak, vab, vTp, angularVelocityResonance);
                currentInductor.Add(t, iLr);
                voltageCapacitor.Add(t, vCr);
                currentInductorPeak = Math.Max(currentInductorPeak, Math.Abs(iLr)); //记录峰值
            }
            //补充特殊点（保证现有的开关器件损耗计算方法正确）
            currentInductor.Order(time0, 0);
            currentInductor.Order(time0 + timeCycleSwitch / 2, 0);
            voltageCapacitor.Order(time0, -voltageCapacitorPeak);
            voltageCapacitor.Order(time0 + timeCycleSwitch / 2, voltageCapacitorPeak);
            //生成主电路元件波形
            currentSwitch_P = currentInductor.Cut(0, timeCycleSwitch / 2, 1);
            currentSwitch_S = currentInductor.Cut(time0, time0 + timeCycleSwitch / 2, turnRatioTransformer);
            voltageSwitch_P = voltageInput;
            voltageSwitch_S = voltageOutput;
            currentCapacitorFilter = currentSwitch_S.Copy(1, 0, -currentOutput);
            //计算有效值
            currentInductorRMS = currentInductor.CalcRMS();
            currentCapacitorFilterRMS = currentCapacitorFilter.CalcRMS();
            //在输入电压不变化时，程序执行后恰好是满载功率点，不需要记录最大值
        }

        /// <summary>
        /// 自动设计，得到每个器件的设计方案
        /// </summary>
        public override void Design()
        {
            //初始化
            DualModule primaryDualModule = new DualModule(2);
            DualModule secondaryDualModule = new DualModule(2);
            Inductor resonantInductor = new Inductor(1);
            Transformer transformer = new Transformer(1);
            Capacitor resonantCapacitor = new Capacitor(1);
            Capacitor filteringCapacitor = new Capacitor(1);
            components = new Component[] { primaryDualModule, secondaryDualModule, transformer, resonantCapacitor, filteringCapacitor };
            componentGroups = new Component[1][];
            componentGroups[0] = new Component[] { primaryDualModule, secondaryDualModule, transformer, resonantCapacitor, filteringCapacitor };

            //获取设计规格
            powerFull = converter.Math_Psys / converter.PhaseNum / converter.Number;
            voltageInputDef = converter.Math_Vin;
            voltageOutputDef = converter.Math_Vo;
            frequencyResonance = converter.Math_fr;
            qualityFactorDef = converter.Math_Q;

            //计算电路参数
            DesignCircuitParam();
            int n = Config.CGC_POWER_RATIO.Length;

            for (int j = 0; j < n; j++)
            {
                power = powerFull * Config.CGC_POWER_RATIO[j]; //改变模块功率
                Simulate();
                //Graph graph = new Graph();
                //graph.Add(curve_iS, "iS");
                //graph.Add(curve_iD, "iD");
                //graph.Draw();
                primaryDualModule.AddEvalParameters(0, j, voltageSwitch_P, currentSwitch_P);
                secondaryDualModule.AddEvalParameters(0, j, voltageSwitch_S, currentSwitch_S);
                resonantInductor.AddEvalParameters(0, j, currentInductorRMS, currentInductorPeak * 2);
                transformer.AddEvalParameters(0, j, currentInductorRMS, currentInductorPeak * 2);
                resonantCapacitor.AddEvalParameters(0, j, currentInductorRMS);
                filteringCapacitor.AddEvalParameters(0, j, currentCapacitorFilterRMS);
            }

            //若认为谐振电感集成在变压器中，则不考虑额外谐振电感
            //if (this.isLeakageInductanceIntegrated)
            //{
            //    this.deviceInductorNum = 0;
            //}

            //设置元器件的设计条件
            primaryDualModule.SetConditions(voltageInput, currentInductorPeak, frequencySwitch);
            secondaryDualModule.SetConditions(voltageOutputDef, turnRatioTransformer * currentInductorPeak, frequencySwitch);
            resonantInductor.SetConditions(inductanceResonance, frequencySwitch, currentInductorPeak);
            transformer.SetDesignCondition(power, frequencySwitch, currentInductorPeak, turnRatioTransformer, fluxLinkage); //FIXME 磁链是否会变化？
            resonantCapacitor.SetConditions(capacitanceResonance, voltageCapacitorPeak, currentInductorRMS);
            filteringCapacitor.SetConditions(200 * 1e-6, voltageOutputDef, currentCapacitorFilterRMS);

            foreach (Component component in components)
            {
                component.Design();
            }
        }
    }
}
