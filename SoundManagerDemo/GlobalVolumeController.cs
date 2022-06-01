using System.Runtime.InteropServices.WindowsRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using WManager;

/// <summary>
/// 全局音量控制器
/// </summary>
[AddComponentMenu("管理器/全局音量控制器")]
public class GlobalVolumeController : MonoBehaviour
{
    [Header("总音量")]
    [Header("音量调节器,可绑定Slider进行调节")]
    [Range(0,1)]
    public float GVolume = 1;
    [Header("总背景音量")]
    [Range(0,1)]
    public float GFXVolume = 1;
    [Header("总BGM音量")]
    [Range(0,1)]
    public float GMusicVolume = 1;
    [Header("总特效音量")]
    [Range(0,1)]
    public float GEffVolume = 1;
    float a = 1;
    float b = 1;
    float c = 1;
    float d = 1;
    /// <summary>
    /// 判断当前在用什么调节音量
    /// 使用检查器面板时传入的音量为-1
    /// 使用Slider调节时传入的音量为Slider的值
    /// </summary>
    private void Update() 
    {
        if (GVolume != a)
        {
            GlobalVolume();
            a= GVolume;
        }
        if (GFXVolume != b)
        {
            GlobalFXVolume();
            b = GFXVolume;
        }
        if (GMusicVolume != c)
        {
            GlobalMusicVolume();
            c = GMusicVolume;
        }
        if (GEffVolume != d)
        {
            GlobalEffectVolume();
            d = GEffVolume;
        }
    }
    /// <summary>
    /// 全局音量
    /// </summary>
    /// <param name="volume"></param>
    public void GlobalVolume(float volume = -1)
    {
        if (volume == -1)
        {
            SoundManager.MainVolume = GVolume;
        }
        else
        {
            SoundManager.MainVolume = volume;
        }
    }
    /// <summary>
    /// 全局背景音量
    /// </summary>
    /// <param name="volume"></param>
    public void GlobalFXVolume(float volume = -1)
    {
        if (volume == -1)
        {
            SoundManager.MainFxVolume = GFXVolume;
        }
        else
        {
            SoundManager.MainFxVolume = volume;
        }
    }
    /// <summary>
    /// 全局音乐音量
    /// </summary>
    /// <param name="volume"></param>
    public void GlobalMusicVolume(float volume = -1)
    {
        if (volume == -1)
        {
            SoundManager.MainMusicVolume = GMusicVolume;
        }
        else
        {
            SoundManager.MainMusicVolume = volume;
        }
        
    }
    /// <summary>
    /// 全局特效音量
    /// </summary>
    /// <param name="volume"></param>
    public void GlobalEffectVolume(float volume = -1)
    {
        if (volume == -1)
        {
            SoundManager.MainEffectVolume = GEffVolume;
        }
        else
        {
            SoundManager.MainEffectVolume = volume;
        }
        
    }
}
