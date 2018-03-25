﻿#region
/*
    Resample引擎IO模型类
    引擎输入输出对象应严格按照这3个结构体进行定义。
    若开发语言为C#应保证：
        1.引擎DLL文件的Assmbley的某一个public属性的class中应包含以下结构体，并保证结构体声明内容和ResamplerIOModels相同（结构体名可自定义,但结构体内字段含字段名称必须一致用于反射调用）
            public struct EngineInformation
            public struct EngineInput
            public struct EngineOutput
        2.引擎DLL文件的Assmbley的某一个public属性的class中应包含以下过程，并且该过程为public static类型。过程名称含大小写必须一直
            public static EngineOutput DoResampler(EngineInput Input)
            public static EngineInformation GetInformation()
        3.EngineInformation和EngineOutput返回中的任何对象必须初始化，不可存在null类型。
 
    若开发语言为C++，应保证
        1.存在以下结构体声明
 	        typedef struct sEngineFlgItem
	        {
                char* flagStr;
                char* ThreeLetterName;
                double Max;
                double Min;
		        double Default;
            }EngineFlgItem;	

	        typedef struct sEngineInformation
            {
                char* Name;
                char* Version;
                char* Author;
                char* Usuage;
		        int FlgItemCount;
		        EngineFlgItem* FlgItem;
            }EngineInformation;

	        typedef struct sEngineInput
            {
                char* inputWaveFile;
                char* NoteString;
                double Velocity;
                char* StrFlags;
                double Offset;
                double RequiredLength;
                double Consonant;
                double Cutoff;
                double Volume;
                double Modulation;
                double Tempo;
                int nPitchBend;
                int pitchBend[65536];
            }EngineInput;

	        typedef struct sEngineOutput
            {
                long nWavData;
                char* wavData;
            }EngineOutput;
       2. 存在以下dllexport用于调用
            extern "C" __declspec(dllexport) EngineOutput* DoResampler(EngineInput Input);
	        extern "C" __declspec(dllexport) EngineInformation* GetInformation();
       3.EngineInformation和EngineOutput返回中的任何对象必须初始化，不可存在空指针或未申请的指针。
    
     若开发语言为其他高级语言，可以参考C++导出格式
     若引擎为exe，那么系统默认认为该引擎可调用并作为resampler引擎使用
 */
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using OpenUtau.Core.Render;

namespace OpenUtau.Core.ResamplerDriver
{
    public class DriverModels
    {
        #region 信息模型
        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct EngineFlagItem
        {
            public string flagStr;
            public string ThreeLetterName;
            public double Max;
            public double Min;
            public double Default;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct EngineInfo
        {
            public string Name;
            public string Version;
            public string Author;
            public string Usuage;
            public int FlagItemCount;
            public EngineFlagItem[] FlagItem;
            public override string ToString() { return Name + " " + Version; }
        }
        #endregion

        #region 输入模型
        /// <summary>
        /// 参数输入模型
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct EngineInput
        {
            public string inputWaveFile;//1
            public string intermediateWaveFile;//2;1
            public string outputWaveFile;//;1
            public string NoteString;//noteID;//3
            public double Velocity;//fixSpeed;//4
            public string StrFlags;//5
            public double Offset;//6
            public double RequiredLength;//fixedLength;//7
            public double Consonant;//startBlank;//9
            public double Cutoff;//endBlank;//10
            public double Volume;//11
            public double Modulation;//12
            public double Tempo;//13
            public int nPitchBend;//13
            public int[] pitchBend;//13
            public double stp;//;3
            public int durTick;//;4
            public double adjustment;//;4
            public double[] envelope;//;5
            public bool lastnote;//;6 , moresampler only
        }
        /// <summary>
        /// 参数初始化过程
        /// </summary>
        /// <returns></returns>
        public static EngineInput CreateInputModel()
        {
            EngineInput Input = new EngineInput
            {
                inputWaveFile = "",
                intermediateWaveFile = "",
                outputWaveFile = "",
                NoteString = "",
                Velocity = 100,
                StrFlags = "",
                Offset = 0,
                RequiredLength = 0,
                Consonant = 0,
                Cutoff = 0,
                Volume = 100,
                Modulation = 100,
                Tempo = 120,
                nPitchBend = 0,
                pitchBend = new int[0],
                stp = 0,
                durTick = 0,
                adjustment = 0,
                envelope = new double[] { 0,0 }
            };
            return Input;
        }
        /// <summary>
        /// 从RenderItem初始化过程
        /// </summary>
        /// <returns></returns>
        internal static EngineInput CreateInputModel(RenderItem renderItem)
        {
            EngineInput Ret = new EngineInput
            {
                inputWaveFile = renderItem.RawFile,
                intermediateWaveFile = renderItem.MidFile,
                outputWaveFile = renderItem.OutFile,
                NoteString = MusicMath.GetNoteString(renderItem.NoteNum),
                Velocity = renderItem.Velocity,
                StrFlags = renderItem.StrFlags,
                Offset = renderItem.Oto?.Offset ?? 0,
                RequiredLength = renderItem.RequiredLength,
                Consonant = renderItem.Oto?.Consonant ?? 0,
                Cutoff = renderItem.Oto?.Cutoff ?? 0,
                Volume = renderItem.Volume,
                Modulation = renderItem.Modulation,
                pitchBend = renderItem.PitchData.ToArray(),
                nPitchBend = renderItem.PitchData.Count,
                Tempo = renderItem.Tempo,
                durTick = renderItem.DurTick,
                adjustment = renderItem.LengthAdjustment,
                stp = /*Math.Max(0, (renderItem.Oto?.Preutter ?? 0) - renderItem.Phoneme.Preutter)*/renderItem.SkipOver,
                envelope = new double[] {
                    Math.Round(renderItem.Envelope[0].X + renderItem.Phoneme.Preutter, 5),
                    renderItem.Envelope[1].X - renderItem.Envelope[0].X,
                    renderItem.Envelope[4].X - renderItem.Envelope[3].X,
                    renderItem.Envelope[0].Y,renderItem.Envelope[1].Y,
                    renderItem.Envelope[3].Y,renderItem.Envelope[4].Y,
                    renderItem.Overlap,
                    0,
                    Math.Min(Math.Max(0, renderItem.Envelope[2].X - renderItem.Envelope[1].X),
                    renderItem.Envelope[3].X - renderItem.Envelope[2].X),renderItem.Envelope[2].Y
                }

            };
            return Ret;
        }
        #endregion

        #region 输出模型
        /// <summary>
        /// 真实输出模型
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        protected struct EngineOutput
        {
            public int nWavData;
            public byte[] wavData;
        }
        #endregion

    }
}
