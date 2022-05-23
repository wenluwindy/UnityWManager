using System;
using UnityEngine;
using UnityEngine.UI;
using WManager;

public class DemoPlayer : MonoBehaviour
{
    /// <summary>
    /// 当前的AudioClip
    /// </summary>
    private AudioClip _currentAudioClip;
    private int _index;

    private int _length;

    private Func<AudioClip, float, SoundManager.SoundManagerAudio> _pause;
    private Func<AudioClip, SoundManager.SoundManagerAudio> _play;
    private Action<AudioClip, float> _stop;
    private Func<AudioClip, SoundManager.SoundManagerAudio> _find;
    private Action<float> _volume;

    /// <summary>
    /// 当前声音名称
    /// </summary>
    [SerializeField]
    private Text _selectedClipText = null;

    /// <summary>
    /// 声音的播放类型
    /// </summary>
    [SerializeField]
    private SoundType _soundTypeChoice = SoundType.OneTime;

    [Header("在此添加AudioClip")]
    public AudioClip[] _audioClips;

    public AudioClip CurrentAudioClip
    {
        get => _currentAudioClip;
        set
        {
            _selectedClipText.text = value.name;
            _currentAudioClip = value;
        }
    }

    private void Awake()
    {
        if (_audioClips.Length == 0)
        {
            _selectedClipText.text = "!!请在检查器中添加AudioClip!!";
            return;
        }

        Builder();

        _index = 0;
        _length = _audioClips.Length;
        CurrentAudioClip = _audioClips[0];
    }

    private void Start()
    {
        Application.targetFrameRate = 30;
    }

    public void Play()
    {
        if (_soundTypeChoice == SoundType.OneTime)
        {
            SoundManager.PlayOnce(_currentAudioClip);
            return;
        }

        _play(_currentAudioClip);
    }

    public void PlayPause()
    {
        SoundManager.SoundManagerAudio clip = _find(_currentAudioClip);

        if (clip != null && clip.IsPLaying) _pause(_currentAudioClip, 0.0f);
        else _play(_currentAudioClip);
    }

    public void Stop()
    {
        _stop(_currentAudioClip, 0.0f);
    }

    public void Next()
    {
        CurrentAudioClip = _audioClips[Mathf.Abs(++_index % _length)];
    }

    public void Prev()
    {
        CurrentAudioClip = _audioClips[Mathf.Abs(--_index % _length)];
    }

    public void SetVolume(float volume)
    {
        _volume(Mathf.Clamp(volume, 0, 1.0f));
    }

    private void Builder()
    {
        switch (_soundTypeChoice)
        {
            case SoundType.Effect:
                _pause = SoundManager.PauseEffect;
                _play = clip => SoundManager.PlayEffect(clip);
                _stop = SoundManager.StopEffect;
                _find = SoundManager.FindEffect;
                _volume = volume => SoundManager.MainEffectVolume = volume;
                break;
            case SoundType.Fx:
                _pause = SoundManager.PauseFx;
                _play = clip => SoundManager.PlayFx(clip, 1.0f, true);
                _stop = SoundManager.StopFx;
                _find = SoundManager.FindFx;
                _volume = volume => SoundManager.MainFxVolume = volume;
                break;
            case SoundType.Music:
                _pause = SoundManager.PauseMusic;
                _play = clip => SoundManager.PlayMusic(clip, 1.0f, true, 2.0f);
                _stop = SoundManager.StopMusic;
                _find = SoundManager.FindMusic;
                _volume = volume => SoundManager.MainMusicVolume = volume;
                break;
            case SoundType.OneTime:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private enum SoundType
    {
        Effect,
        Fx,
        Music,
        OneTime
    }
}