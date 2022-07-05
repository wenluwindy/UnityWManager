using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 视频播放管理器
/// </summary>
[AddComponentMenu("管理器/视频播放器")]
public class VideoPlayerManager : MonoBehaviour
{
    [LabelText("视频名字，带后缀")]
    public string video_Name = "";//视频路径
    string Path;
    [LabelText("VideoPlayer组件")]
    public VideoPlayer videoPlayer;//从场景中拖入挂载VideoPlayer组件的物体
    [Title("以下功能可以不挂载物体")]
    [InfoBox("需要在Slider下添加Event Trigger，并绑定PointerDown和PointerUp事件")]
    [LabelText("视频进度条")]
    public Slider videoSlider;     //视频进度条
    [LabelText("视频进度条可调节？")]
    public bool Adjustable = true;
    [LabelText("进度条时间")]
    public Text tPlayerTime;    //进度条时间
    [LabelText("视频音量")]
    public Slider VolumeSlider;
    [LabelText("暂停/播放按钮")]
    public Button playButton;
    [LabelText("停止按钮")]
    public Button StopButton;

    [PropertySpace(20)]
    [LabelText("结束后运行事件")]
    public UnityEvent EndEvent;

    private bool m_bMouseUp = true;

    private void Awake()
    {
        Path = Application.streamingAssetsPath + "/" + video_Name;//将视频文件放于StreamingAssets文件夹
        //Debug.Log(Path);// 打印视频路径
        videoPlayer.url = Path;

    }
    
    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayOrPause);
        }
        if (StopButton != null)
        {
            StopButton.onClick.AddListener(StopPlay);
        }
        videoSlider.onValueChanged.AddListener((float value) =>
        {
            if (!m_bMouseUp)
            {
                SliderEvent(value);
            }
        });
    }

    void Update()
    {
        if (videoPlayer != null)
        {
            if (Adjustable == false)
            {
                //不让拖动的进度条显示功能
                videoSlider.value = videoPlayer.frame / (videoPlayer.frameCount * 1.0f);
            }
            if (tPlayerTime != null)
            {
                if (videoPlayer.isPlaying)
                {
                    //控制text显示视频当前时间和总时间
                    int _all = (int)float.Parse((1f * videoPlayer.frameCount / videoPlayer.frameRate).ToString("F1"));
                    TimeSpan allTime = new TimeSpan(0, 0, _all);

                    int _current = (int)float.Parse((1f * videoPlayer.frame / videoPlayer.frameRate).ToString("F1"));
                    TimeSpan currentTime = new TimeSpan(0, 0, _current);

                    tPlayerTime.text = currentTime.Minutes.ToString().PadLeft(2, '0') + ":" + currentTime.Seconds.ToString().PadLeft(2, '0') + "/" + allTime.Minutes.ToString().PadLeft(2, '0') + ":" + allTime.Seconds.ToString().PadLeft(2, '0');
                }
            }
            if (VolumeSlider != null)
            {
                //设置音量
                videoPlayer.SetDirectAudioVolume(0, VolumeSlider.value);
            }
            if (videoSlider.value > 0.9995f)
            {
                //播放结束后执行的事件
                EndEvent.Invoke();
            }
        }
    }

# region 进度条更新功能
    // 如果启用 MonoBehaviour，则每个固定帧速率的帧都将调用此函数
    private void FixedUpdate()
    {
        if (Adjustable)
        {
            if (m_bMouseUp)
            {
                videoSlider.value = videoPlayer.frame / (videoPlayer.frameCount * 1.0f);
            }
        } 
    }
    public void PointerDown()
    {
        if (Adjustable)
        {
            videoPlayer.Pause();
            m_bMouseUp = false;
        }
    }
    public void PointerUp()
    {
        if (Adjustable)
        {
            videoPlayer.Play();
            m_bMouseUp = true;
        }
    }
    public void SliderEvent(float value)
    {
        if (Adjustable)
        {
            videoPlayer.frame = long.Parse((value * videoPlayer.frameCount).ToString("0."));
        } 
    }
# endregion

    /// <summary>
    /// 播放或暂停
    /// </summary>
    public void PlayOrPause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else
        {
            videoPlayer.Play();
        }
    }
    /// <summary>
    /// 停止播放
    /// </summary>
    public void StopPlay()
    {
        videoPlayer.Stop();
    }

}
