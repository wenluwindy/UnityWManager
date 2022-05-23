using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WManager;

public class SoundExample : MonoBehaviour
{
    public AudioClip BGMAudioClip;
    public AudioClip FXAudioClip;
    public AudioClip EFFAudioClip;
    public AudioClip EFFAudioClip2;
    /// <summary>
    /// 暂停或播放音乐
    /// </summary>
    public void PlayPauseBGM()
    {
        var a = SoundManager.FindMusic(BGMAudioClip);
        if (a != null && a.IsPLaying)
        {
            SoundManager.PauseMusic(BGMAudioClip);
        }
        else
        {
            SoundManager.PlayMusic(BGMAudioClip);
        }
    }
    /// <summary>
    /// 暂停或播放背景音
    /// </summary>
    public void PlayPauseFx()
    {
        var a = SoundManager.FindFx(FXAudioClip);
        if (a != null && a.IsPLaying)
        {
            SoundManager.PauseFx(FXAudioClip);
        }
        else
        {
            SoundManager.PlayFx(FXAudioClip,1,true,true,1);
        }
    }
    /// <summary>
    /// 改变指定背景音量
    /// </summary>
    /// <param name="value"></param>
    public void FxVolume(float value)
    {
        SoundManager.FindFx(FXAudioClip).Volume = value;
    }
    public void PlayOnce()
    {
        SoundManager.PlayOnce(EFFAudioClip);
    }
    public void PlayEFF()
    {
        SoundManager.PlayEffect(EFFAudioClip2);
    }
    public void EFFVolume(float value)
    {
        SoundManager.FindEffect(EFFAudioClip2).Volume = value;
    }
    private void Update()
    {
        //SoundManager.FindMusic(0);
        //Debug.Log(SoundManager.FindMusic(BGMAudioClip));
    }
}
