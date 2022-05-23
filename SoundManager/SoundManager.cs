using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exceptions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngineObject = UnityEngine.Object;

namespace WManager
{
#pragma warning disable 4014
    /// <summary>
    /// 音频管理器
    /// ①OneTime：播放一次便会删除
    /// ②Effect：适合经常调用的短音,如UI点击特效
    /// ③FX:适合长时间使用的背景音
    /// ④Music：bgm,同时只能播放一个
    /// </summary>
    public static class SoundManager
    {
        public enum SoundManagerType
        {
            Effect,
            Fx,
            Music,
            OneTime
        }

        public static AudioMixerGroup AudioMixer;

        public static AudioMixerGroup EffectAudioMixer;
        public static AudioMixerGroup FxAudioMixer;
        public static AudioMixerGroup MusicAudioMixer;

        private static readonly Dictionary<int, SoundManagerAudio> EffectAudioCache;
        private static readonly Dictionary<int, SoundManagerAudio> FxAudioCache;
        private static readonly Dictionary<int, SoundManagerAudio> MusicAudioCache;
        private static readonly GameObject GameObject;

        private static SoundManagerAudio _currentMusic;
        private static float _musicTransitionTime;
        private static bool _advancePauseCheck;

        static SoundManager()
        {
            EffectAudioCache = new Dictionary<int, SoundManagerAudio>();
            FxAudioCache = new Dictionary<int, SoundManagerAudio>();
            MusicAudioCache = new Dictionary<int, SoundManagerAudio>();

            GameObject = new GameObject("SoundManager");
            UnityEngineObject.DontDestroyOnLoad(GameObject);
        }

        /// <summary>
        ///     音乐过渡时间
        /// </summary>
        private static float MusicTransitionTime
        {
            get => _musicTransitionTime;
            set => _musicTransitionTime = Mathf.Max(value, 0.0f);
        }

        /// <summary>
        ///     SoundManager音频对象
        /// </summary>
        public class SoundManagerAudio
        {
            private readonly AudioClip _audioClip;
            private readonly bool _doNotDestroy;
            private readonly float _duration;
            private readonly Func<float> _mainVolumeGroup;
            private readonly AudioMixerGroup _mixer;
            private readonly SoundManagerType _type;

            private AudioSource _audioSource;

            public AudioClip AudioClip => _audioClip;
            public bool DoNotDestroy => _doNotDestroy;
            public float Duration => _duration;

            private bool StopExist => _audioSource == null || Application.isPlaying == false;

            private bool _loop;
            private float _volume;

            private CancellationTokenSource _playingToken;
            private CancellationTokenSource _transitionToken;

            /// <summary>
            /// SoundManagerAudio 构造函数
            /// </summary>
            /// <param name="audioClip">要使用的音频剪辑</param>
            /// <param name="volume">播放声音的音量(0到1之间)</param>
            /// <param name="loop">音频是否循环</param>
            /// <param name="doNotDestroy">播放后是否保留AudioSource对象</param>
            /// <param name="type">SoundManagerAudio的类型</param>
            /// <param name="mixer">用于音频的混音器</param>
            /// <exception cref="ArgumentOutOfRangeException">如果类型未知，则抛出错误</exception>
            internal SoundManagerAudio(AudioClip audioClip, float volume, bool loop, bool doNotDestroy,
                SoundManagerType type, AudioMixerGroup mixer)
            {
                _audioClip = audioClip;
                _volume = Mathf.Clamp(volume, 0f, 1.0f);
                _loop = loop;
                _doNotDestroy = doNotDestroy;
                _type = type;
                _mixer = mixer;
                _duration = audioClip.length;

                switch (type)
                {
                    case SoundManagerType.Effect:
                        _mainVolumeGroup = () => MainEffectVolume;
                        break;
                    case SoundManagerType.Fx:
                        _mainVolumeGroup = () => MainFxVolume;
                        break;
                    case SoundManagerType.Music:
                        _mainVolumeGroup = () => MainMusicVolume;
                        break;
                    case SoundManagerType.OneTime:
                        _mainVolumeGroup = () => 1f;
                        break;
                    default:
                        Debug.LogError($"{type} 该类型不明");
                        break;
                }
            }

            /// <summary>
            ///     判断SoundManagerAudio当前是否正在播放
            /// </summary>
            public bool IsPLaying => _audioSource != null && _audioSource.isPlaying;

            /// <summary>
            ///     AudioSource GameObject
            /// </summary>
            private AudioSource AudioSource
            {
                get
                {
                    if (Application.isPlaying == false) return null;

                    if (_audioSource == null)
                    {
                        // 创建一个新的
                        _audioSource = GameObject.AddComponent<AudioSource>();

                        _audioSource.clip = _audioClip;
                        _audioSource.volume = _volume * _mainVolumeGroup() * MainVolume;
                        _audioSource.loop = _loop;
                        _audioSource.outputAudioMixerGroup = _mixer;
                        _audioSource.playOnAwake = false;
                    }

                    return _audioSource;
                }
            }

            /// <summary>
            ///     开启循环
            /// </summary>
            internal bool Loop
            {
                get => _loop;
                set
                {
                    if (StopExist) return;

                    _audioSource.loop = _loop = value;
                }
            }

            /// <summary>
            ///     调节音量为
            /// </summary>
            internal float Volume
            {
                get => _volume;
                set
                {
                    if (StopExist) return;

                    _volume = value;
                    _audioSource.volume = value * _mainVolumeGroup() * MainVolume;
                }
            }

            /// <summary>
            ///     停止
            /// </summary>
            internal void Stop()
            {
                if (StopExist) return;

                AudioSource.Stop();

                if (_doNotDestroy) return;

                DestroyAudioSource();

                if (_currentMusic == this) _currentMusic = null;
            }

            /// <summary>
            ///     暂停
            /// </summary>
            internal void Pause()
            {
                if (StopExist) return;

                _playingToken?.Cancel();

                AudioSource.Pause();
            }

            /// <summary>
            ///     播放
            /// </summary>
            /// <param name="fadeinTime">音量在几秒钟内减弱</param>
            internal void Play(float fadeinTime = 0.0f)
            {
                if (Application.isPlaying == false) return;

                fadeinTime = Mathf.Clamp(fadeinTime, 0.0f, _duration);

                _transitionToken?.Cancel();

                // 音乐的特定部分
                if (_type == SoundManagerType.Music)
                {
                    MusicTransitionTime = fadeinTime;

                    if (_currentMusic != null && _currentMusic != this)
                    {
                        if (_currentMusic.IsPLaying)
                        {
                            // 切换到下一段音乐
                            _currentMusic.MusicTransition(this);
                            return;
                        }

                        _currentMusic.Stop();
                    }

                    // 设置新音乐
                    _currentMusic = this;
                }

                if (_doNotDestroy || _loop)
                {
                    // 播放和循环或保持
                    if (fadeinTime > 0.0f)
                        Fadein(fadeinTime);
                    else
                        AudioSource.Play();
                }
                else
                {
                    _playingToken?.Cancel();
                    _playingToken = new CancellationTokenSource();

                    if (fadeinTime > 0.0f)
                        Fadein(fadeinTime);
                    else
                        AudioSource.Play();

                    DestroyAudioSourceAsync();
                }
            }

            /// <summary>
            ///     调节淡出时间
            /// </summary>
            /// <param name="fadeoutTime">淡出时间，以秒为单位</param>
            /// <param name="nextAction">接下来要执行的操作</param>
            internal async void Fadeout(float fadeoutTime, [CanBeNull] Action nextAction = null)
            {
                if (StopExist) return;

                fadeoutTime = Mathf.Clamp(fadeoutTime, 0.0f, _duration);

                float startTransitionTime = Time.time;
                float endTransitionTime = startTransitionTime + fadeoutTime;

                float initialVolume = _volume * MainVolume * _mainVolumeGroup();

                _transitionToken?.Cancel();

                AudioSource.volume = initialVolume;

                _transitionToken = new CancellationTokenSource();

                CancellationToken ct = _transitionToken.Token;

                while (Time.time < endTransitionTime)
                {
                    await Task.Delay(TimeSpan.FromSeconds(.05f), CancellationToken.None);
                    if (StopExist || ct.IsCancellationRequested) return;

                    AudioSource.volume = (initialVolume - initialVolume * (Mathf.Min((Time.time - startTransitionTime), fadeoutTime) / fadeoutTime)) * MainVolume * _mainVolumeGroup();
                }

                AudioSource.volume = initialVolume * MainVolume * _mainVolumeGroup();

                nextAction?.Invoke();
            }

            /// <summary>
            ///     调节淡入时间
            /// </summary>
            /// <param name="fadeinTime">淡入时间，以秒为单位</param>
            private async void Fadein(float fadeinTime)
            {
                // 音频源在这一步不存在，这是正常的
                if (Application.isPlaying == false) return;

                fadeinTime = Mathf.Clamp(fadeinTime, 0.0f, _duration);

                _transitionToken?.Cancel();
                AudioSource.volume = 0.0f;
                _audioSource.Play();

                float startTransitionTime = Time.time;
                float endTransitionTime = startTransitionTime + fadeinTime;

                _transitionToken = new CancellationTokenSource();

                CancellationToken ct = _transitionToken.Token;

                while (Time.time < endTransitionTime)
                {
                    await Task.Delay(TimeSpan.FromSeconds(.05f), CancellationToken.None);
                    if (StopExist || ct.IsCancellationRequested) return;

                    AudioSource.volume = (Mathf.Min((Time.time - startTransitionTime), fadeinTime) / fadeinTime) * MainVolume * _mainVolumeGroup();
                }

                AudioSource.volume = _volume * MainVolume * _mainVolumeGroup();
            }

            /// <summary>
            ///     两个音乐之间的过渡
            /// </summary>
            /// <param name="playingNext">接下来要播放的音乐</param>
            private void MusicTransition(SoundManagerAudio playingNext)
            {
                if (StopExist) return;

                _transitionToken?.Cancel();

                playingNext.Fadein(MusicTransitionTime);
                Fadeout(MusicTransitionTime, Stop);

                _currentMusic = playingNext;
            }

            /// <summary>
            ///     异步销毁AudioSource GameObject属性
            /// </summary>
            private async Task DestroyAudioSourceAsync()
            {
                await Task.Delay(TimeSpan.FromSeconds(_duration - _audioSource.time), _playingToken.Token);

                if (StopExist) return;

                DestroyAudioSource();
            }

            /// <summary>
            ///     销毁AudioSource游戏对象属性
            /// </summary>
            internal void DestroyAudioSource()
            {
                if (StopExist) return;

                UnityEngineObject.Destroy(_audioSource);
                _audioSource = null;
            }
        }

        #region 准备

        /// <summary>
        ///     通过id准备Effect
        /// </summary>
        /// <param name="id">准备Effect的id</param>
        /// <param name="volume">准备声音的音量</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareEffect(int id, float volume = 1.0f) =>
            PreparerBase(GetEffect(id), volume, false);

        /// <summary>
        ///     用SoundManagerObject准备一个Effect
        /// </summary>
        /// <param name="soundManagerAudio">“SoundManagerAudio”Effect准备</param>
        /// <param name="volume">准备声音的音量</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareEffect(SoundManagerAudio soundManagerAudio, float volume = 1.0f) =>
            PreparerBase(soundManagerAudio, volume, false);

        /// <summary>
        ///     添加AudioClip到Effect列表并准备它
        /// </summary>
        /// <param name="audioClip">准备的音频剪辑</param>
        /// <param name="volume">准备声音的音量</param>
        /// <param name="doNotDestroy">在准备之后保留AudioSource对象</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareEffect(AudioClip audioClip, float volume = 1.0f, bool doNotDestroy = false) =>
            PreparerBaseBuild(audioClip, EffectAudioCache, EffectAudioMixer, SoundManagerType.Effect, volume, false, doNotDestroy);

        /// <summary>
        ///     根据id准备一个FX
        /// </summary>
        /// <param name="id">要准备的FX的id</param>
        /// <param name="volume">准备声音的音量</param>
        /// <param name="loop">是否循环FX剪辑</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareFx(int id, float volume = 1.0f, bool loop = false) =>
            PreparerBase(GetFx(id), volume, loop);

        /// <summary>
        ///     通过SoundManagerObject准备一个FX
        /// </summary>
        /// <param name="soundManagerAudio">准备SoundManagerAudio FX</param>
        /// <param name="volume">准备声音的音量</param>
        /// <param name="loop">是否循环FX剪辑</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareFx(SoundManagerAudio soundManagerAudio, float volume = 1.0f, bool loop = false) =>
            PreparerBase(soundManagerAudio, volume, loop);

        /// <summary>
        ///     添加AudioClip到FX列表并准备它
        /// </summary>
        /// <param name="audioClip">准备的音频剪辑</param>
        /// <param name="volume">准备声音的音量</param>
        /// <param name="loop">是否循环FX剪辑</param>
        /// <param name="doNotDestroy">在准备之后保留AudioSource对象</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareFx(AudioClip audioClip, float volume = 1.0f, bool loop = false, bool doNotDestroy = true) =>
            PreparerBaseBuild(audioClip, FxAudioCache, FxAudioMixer, SoundManagerType.Fx, volume, loop, doNotDestroy);

        /// <summary>
        ///     根据id准备音乐
        /// </summary>
        /// <param name="id">准备音乐的ID</param>
        /// <param name="volume">准备音乐的音量</param>
        /// <param name="loop">是否循环播放音乐</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareMusic(int id, float volume = 1.0f, bool loop = false) =>
            PreparerBase(GetMusic(id), volume, loop);

        /// <summary>
        ///     通过SoundManagerObject准备音乐
        /// </summary>
        /// <param name="soundManagerAudio">准备音乐的SoundManagerAudio</param>
        /// <param name="volume">准备音乐的音量</param>
        /// <param name="loop">是否循环播放音乐</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareMusic(SoundManagerAudio soundManagerAudio, float volume = 1.0f, bool loop = false) =>
            PreparerBase(soundManagerAudio, volume, loop);

        /// <summary>
        ///     添加AudioClip到音乐列表，并准备它
        /// </summary>
        /// <param name="audioClip">准备的音频剪辑</param>
        /// <param name="volume">准备音乐的音量</param>
        /// <param name="loop">是否循环播放音乐</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PrepareMusic(AudioClip audioClip, float volume = 1.0f, bool loop = false) =>
            PreparerBaseBuild(audioClip, MusicAudioCache, MusicAudioMixer, SoundManagerType.Music, volume, loop, false);

        /// <summary>
        ///     为SoundManagerAudio基础方法准备逻辑
        /// </summary>
        /// <param name="soundManagerAudio"></param>
        /// <param name="volume">要准备的剪辑的音量</param>
        /// <param name="loop">音频是否循环</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        private static SoundManagerAudio PreparerBase(SoundManagerAudio soundManagerAudio, float volume, bool loop)
        {
            soundManagerAudio.Volume = volume;
            soundManagerAudio.Loop = loop;

            return soundManagerAudio;
        }

        /// <summary>
        ///     通用方法集中共享准备逻辑AudioClip基础方法
        /// </summary>
        /// <param name="audioClip">准备的音频剪辑</param>
        /// <param name="cache">创建后添加SoundManagerAudio的字典</param>
        /// <param name="audioMixerGroup">用于准备剪辑的音频混音器组</param>
        /// <param name="type">SoundManagerAudio的类型</param>
        /// <param name="volume">剪辑的音量</param>
        /// <param name="loop">音频是否循环</param>
        /// <param name="doNotDestroy">在准备之后保留AudioSource对象</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        private static SoundManagerAudio PreparerBaseBuild(AudioClip audioClip, IDictionary<int, SoundManagerAudio> cache,
            AudioMixerGroup audioMixerGroup, SoundManagerType type, float volume, bool loop, bool doNotDestroy)
        {
            int id = audioClip.GetInstanceID();
            cache.TryGetValue(audioClip.GetInstanceID(), out SoundManagerAudio soundManagerAudio);

            if (soundManagerAudio != null) return soundManagerAudio;

            if (MusicAudioMixer != null)
                soundManagerAudio = new SoundManagerAudio(audioClip, volume, loop, doNotDestroy, type, audioMixerGroup);
            else if (AudioMixer != null)
                soundManagerAudio = new SoundManagerAudio(audioClip, volume, loop, doNotDestroy, type, AudioMixer);
            else
                soundManagerAudio = new SoundManagerAudio(audioClip, volume, loop, doNotDestroy, type, null);

            cache.Add(id, soundManagerAudio);

            return soundManagerAudio;
        }

        #endregion

        #region 播放功能

        /// <summary>
        ///     按id播放Effect
        /// </summary>
        /// <param name="id">要播放Effect的id</param>
        /// <param name="volume">播放音量</param>
        /// <param name="fadeinTime">淡入时间(秒)</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayEffect(int id, float volume = 1.0f, float fadeinTime = 0.0f) =>
            PlayerBase(GetEffect(id), volume, false, fadeinTime);

        /// <summary>
        ///     按SoundManagerObject播放Effect
        /// </summary>
        /// <param name="soundManagerAudio">要播放Effect的SoundManagerAudio</param>
        /// <param name="volume">播放音量</param>
        /// <param name="fadeinTime">淡入时间(秒)</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayEffect(SoundManagerAudio soundManagerAudio, float volume = 1.0f, float fadeinTime = 0.0f) =>
            PlayerBase(soundManagerAudio, volume, false, fadeinTime);

        /// <summary>
        ///     将AudioClip添加到Effect列表并播放它
        /// </summary>
        /// <param name="audioClip">要播放的AudioClip</param>
        /// <param name="volume">播放音量</param>
        /// <param name="doNotDestroy">播放后是否保留AudioSource对象</param>
        /// <param name="fadeinTime">淡入时间(秒)</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayEffect(AudioClip audioClip, float volume = 1.0f, bool doNotDestroy = false, float fadeinTime = 0.0f) =>
            PlayerBaseBuild(audioClip, EffectAudioCache, EffectAudioMixer, SoundManagerType.Effect, volume, false, doNotDestroy, fadeinTime);

        /// <summary>
        ///     按id播放FX
        /// </summary>
        /// <param name="id">FX的id</param>
        /// <param name="volume">播放音量</param>
        /// <param name="loop">是否循环FX剪辑</param>
        /// <param name="fadeinTime">淡入时间(秒)</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayFx(int id, float volume = 1.0f, bool loop = false, float fadeinTime = 0.0f) =>
            PlayerBase(GetFx(id), volume, loop, fadeinTime);

        /// <summary>
        ///     按SoundManagerObject播放FX
        /// </summary>
        /// <param name="soundManagerAudio">FX的SoundManagerAudio</param>
        /// <param name="volume">播放音量</param>
        /// <param name="loop">是否循环FX剪辑</param>
        /// <param name="fadeinTime">淡入时间(秒)</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayFx(SoundManagerAudio soundManagerAudio, float volume = 1.0f, bool loop = false, float fadeinTime = 0.0f) =>
            PlayerBase(soundManagerAudio, volume, loop, fadeinTime);

        /// <summary>
        ///     添加AudioClip到FX列表并播放它
        /// </summary>
        /// <param name="audioClip">要播放的AudioClip</param>
        /// <param name="volume">播放音量</param>
        /// <param name="loop">是否循环FX剪辑</param>
        /// <param name="doNotDestroy">播放后是否保留AudioSource对象</param>
        /// <param name="fadeinTime">淡入时间(秒)</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayFx(AudioClip audioClip, float volume = 1.0f, bool loop = false, bool doNotDestroy = true, float fadeinTime = 0.0f) =>
            PlayerBaseBuild(audioClip, FxAudioCache, FxAudioMixer, SoundManagerType.Fx, volume, loop, doNotDestroy, fadeinTime);

        /// <summary>
        ///     用id播放music
        /// </summary>
        /// <param name="id">music的id</param>
        /// <param name="volume">播放music的音量</param>
        /// <param name="loop">是否循环播放音乐</param>
        /// <param name="fadeinTime">淡入时间（秒）</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayMusic(int id, float volume = 1.0f, bool loop = false, float fadeinTime = 0.0f) =>
            PlayerBase(GetMusic(id), volume, loop, fadeinTime);

        /// <summary>
        ///     按 SoundManagerObject播放 music 
        /// </summary>
        /// <param name="soundManagerAudio">music的SoundManagerAudio</param>
        /// <param name="volume">播放music的音量</param>
        /// <param name="loop">是否循环播放音乐</param>
        /// <param name="fadeinTime">淡入时间（秒）</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayMusic(SoundManagerAudio soundManagerAudio, float volume = 1.0f, bool loop = false, float fadeinTime = 0.0f) =>
            PlayerBase(soundManagerAudio, volume, loop, fadeinTime);

        /// <summary>
        ///    将AudioClip添加到Music列表中并播放它
        /// </summary>
        /// <param name="audioClip">要播放的音频剪辑</param>
        /// <param name="volume">播放music的音量</param>
        /// <param name="loop">是否循环播放音乐</param>
        /// <param name="fadeinTime">淡入时间（秒）</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        public static SoundManagerAudio PlayMusic(AudioClip audioClip, float volume = 1.0f, bool loop = false, float fadeinTime = 0.0f) =>
            PlayerBaseBuild(audioClip, MusicAudioCache, MusicAudioMixer, SoundManagerType.Music, volume, loop, false, fadeinTime);

        /// <summary>
        ///     播放AudioClip一次并在播放完成后销毁AudioSource对象
        /// </summary>
        /// <param name="audioClip">要播放的音频剪辑</param>
        /// <param name="volume">播放音量</param>
        /// <param name="mixer">用于音频的混音器</param>
        public static void PlayOnce(AudioClip audioClip, float volume = 1.0f, AudioMixerGroup mixer = null)
        {
            SoundManagerAudio soundManagerAudio = new SoundManagerAudio(audioClip, volume, false, false, SoundManagerType.OneTime, mixer);

            soundManagerAudio.Play();
        }

        /// <summary>
        ///     为SoundManagerAudio基础方法集中共享播放逻辑的通用方法
        /// </summary>
        /// <param name="soundManagerAudio"></param>
        /// <param name="volume">播放音量</param>
        /// <param name="loop">音频是否循环</param>
        /// <param name="fadeinTime">淡入时间(秒)</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        private static SoundManagerAudio PlayerBase(SoundManagerAudio soundManagerAudio, float volume, bool loop, float fadeinTime)
        {
            soundManagerAudio.Volume = volume;
            soundManagerAudio.Loop = loop;

            soundManagerAudio.Play(fadeinTime);

            return soundManagerAudio;
        }

        /// <summary>
        ///     通用方法集中共享播放逻辑AudioClip基方法
        /// </summary>
        /// <param name="audioClip">要播放的音频剪辑</param>
        /// <param name="cache">创建后添加SoundManagerAudio的字典</param>
        /// <param name="audioMixerGroup">用于播放剪辑的音频混音器组</param>
        /// <param name="type">SoundManagerAudio的类型</param>
        /// <param name="volume">剪辑的音量</param>
        /// <param name="loop">音频是否循环</param>
        /// <param name="doNotDestroy">播放后是否保留AudioSource对象</param>
        /// <param name="fadeinTime">淡入时间(秒)</param>
        /// <returns>产生的SoundManagerAudio对象</returns>
        private static SoundManagerAudio PlayerBaseBuild(AudioClip audioClip, IDictionary<int, SoundManagerAudio> cache, AudioMixerGroup audioMixerGroup, SoundManagerType type, float volume, bool loop, bool doNotDestroy, float fadeinTime)
        {
            // Re-use the PreparerBaseBuild method from the precedent section
            SoundManagerAudio soundManagerAudio = PreparerBaseBuild(audioClip, cache, audioMixerGroup, type, volume, loop, doNotDestroy);

            soundManagerAudio.Play(fadeinTime);

            return soundManagerAudio;
        }

        #endregion

        #region 暂停所有功能

        /// <summary>
        ///     暂停所有播放元素
        /// </summary>
        /// <param name="fadeoutTime">渐隐时间（秒）</param>
        /// <returns>暂停的SoundManagerAudio的列表</returns>
        public static List<SoundManagerAudio> PauseAll(float fadeoutTime = 0.0f)
        {
            List<SoundManagerAudio> pausedList = new List<SoundManagerAudio>();

            pausedList.AddRange(PauseAllEffect(fadeoutTime));
            pausedList.AddRange(PauseAllFx(fadeoutTime));
            pausedList.AddRange(PauseAllMusic(fadeoutTime));

            return pausedList;
        }

        /// <summary>
        ///     暂停所有正在播放的effects
        /// </summary>
        /// <param name="fadeoutTime">渐隐时间（秒）</param>
        /// <returns>暂停的SoundManagerAudio的列表</returns>
        public static List<SoundManagerAudio> PauseAllEffect(float fadeoutTime = 0.0f) =>
            PauseAllBase(EffectAudioCache, fadeoutTime);

        /// <summary>
        ///     暂停所有正在播放的FX
        /// </summary>
        /// <param name="fadeoutTime">渐隐时间（秒）</param>
        /// <returns>暂停的SoundManagerAudio的列表</returns>
        public static List<SoundManagerAudio> PauseAllFx(float fadeoutTime = 0.0f) =>
            PauseAllBase(FxAudioCache, fadeoutTime);

        /// <summary>
        ///     暂停所有正在播放的 music
        /// </summary>
        /// <param name="fadeoutTime">渐隐时间（秒）</param>
        /// <returns>暂停的SoundManagerAudio的列表</returns>
        public static List<SoundManagerAudio> PauseAllMusic(float fadeoutTime = 0.0f) =>
            PauseAllBase(MusicAudioCache, fadeoutTime);

        /// <summary>
        ///     在指定列表中暂停播放SoundManagerAudio播放的通用方法
        /// </summary>
        /// <param name="cache">用于收集播放元素列表的字典</param>
        /// <param name="fadeout">渐隐时间（秒）</param>
        /// <returns>暂停的SoundManagerAudio的列表</returns>
        private static List<SoundManagerAudio> PauseAllBase(Dictionary<int, SoundManagerAudio> cache, float fadeout)
        {
            List<SoundManagerAudio> pausedList = new List<SoundManagerAudio>();

            foreach (KeyValuePair<int, SoundManagerAudio> entry in cache.Where(entry => entry.Value.IsPLaying))
            {
                pausedList.Add(entry.Value);

                if (fadeout > 0.0f)
                {
                    entry.Value.Fadeout(fadeout, entry.Value.Pause);
                }
                else entry.Value.Pause();
            }

            return pausedList;
        }

        #endregion

        #region 暂停功能

        /// <summary>
        ///     通过id暂停正在播放的 effect
        /// </summary>
        /// <param name="id">The id of the effect</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseEffect(int id, float fadeoutTime = 0.0f) =>
            PauseBase(GetEffect(id), fadeoutTime);

        /// <summary>
        ///     通过AudioClip暂停正在播放的 effect
        /// </summary>
        /// <param name="audioClip">The AudioClip to pause</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseEffect(AudioClip audioClip, float fadeoutTime = 0.0f) =>
            PauseBase(FindEffect(audioClip), fadeoutTime);

        /// <summary>
        ///     通过SoundManagerAudio暂停正在播放的 effect
        /// </summary>
        /// <param name="soundManagerAudio">The SoundManagerAudio to pause</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseEffect(SoundManagerAudio soundManagerAudio, float fadeoutTime = 0.0f) =>
            PauseBase(soundManagerAudio, fadeoutTime);

        /// <summary>
        ///     通过id暂停正在播放的 FX
        /// </summary>
        /// <param name="id">The id of the effect</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseFx(int id, float fadeoutTime = 0.0f) =>
            PauseBase(GetFx(id), fadeoutTime);

        /// <summary>
        ///     通过AudioClip暂停正在播放的 FX  
        /// </summary>
        /// <param name="audioClip">The AudioClip to pause</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseFx(AudioClip audioClip, float fadeoutTime = 0.0f) =>
            PauseBase(FindFx(audioClip), fadeoutTime);

        /// <summary>
        ///     通过SoundManagerAudio暂停正在播放的 FX  
        /// </summary>
        /// <param name="soundManagerAudio">The SoundManagerAudio to pause</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseFx(SoundManagerAudio soundManagerAudio, float fadeoutTime = 0.0f) =>
            PauseBase(soundManagerAudio, fadeoutTime);

        /// <summary>
        ///     通过id暂停所有正在播放的 music 
        /// </summary>
        /// <param name="id">The id of the effect</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseMusic(int id, float fadeoutTime = 0.0f) =>
            PauseBase(GetMusic(id), fadeoutTime);

        /// <summary>
        ///     通过AudioClip暂停所有正在播放的 music 
        /// </summary>
        /// <param name="audioClip">The AudioClip to pause</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseMusic(AudioClip audioClip, float fadeoutTime = 0.0f) =>
            PauseBase(FindMusic(audioClip), fadeoutTime);

        /// <summary>
        ///     通过SoundManagerAudio暂停所有正在播放的 music 
        /// </summary>
        /// <param name="soundManagerAudio">The SoundManagerAudio to pause</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static SoundManagerAudio PauseMusic(SoundManagerAudio soundManagerAudio, float fadeoutTime = 0.0f) =>
            PauseBase(soundManagerAudio, fadeoutTime);

        /// <summary>
        ///     为SoundManagerAudio基本方法共享暂停逻辑的通用方法
        /// </summary>
        /// <param name="soundManagerAudio">The SoundManagerAudio to pause</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        private static SoundManagerAudio PauseBase(SoundManagerAudio soundManagerAudio, float fadeoutTime)
        {
            if (fadeoutTime > 0.0f)
                soundManagerAudio.Fadeout(fadeoutTime, soundManagerAudio.Pause);
            else
                soundManagerAudio.Pause();

            return soundManagerAudio;
        }

        #endregion

        #region 停止所有功能

        /// <summary>
        ///     停止播放所有元素
        /// </summary>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopAll(float fadeoutTime = 0.0f)
        {
            StopAllEffect(fadeoutTime);
            StopAllFx(fadeoutTime);
            StopAllMusic(fadeoutTime);
        }

        /// <summary>
        ///     停止正在播放的 effects
        /// </summary>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopAllEffect(float fadeoutTime = 0.0f) =>
            StopAllBase(EffectAudioCache, fadeoutTime);

        /// <summary>
        ///     停止正在播放的 FX
        /// </summary>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopAllFx(float fadeoutTime = 0.0f) =>
            StopAllBase(FxAudioCache, fadeoutTime);

        /// <summary>
        ///     停止正在播放的 music
        /// </summary>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopAllMusic(float fadeoutTime = 0.0f) =>
            StopAllBase(MusicAudioCache, fadeoutTime);

        /// <summary>
        ///     使用共享逻辑停止播放缓存中的元素的通用方法
        /// </summary>
        /// <param name="cache">查找SoundManagerAudio的字典</param>
        /// <param name="fadeout">淡出时间（秒）</param>
        private static void StopAllBase(Dictionary<int, SoundManagerAudio> cache, float fadeout)
        {
            foreach (KeyValuePair<int, SoundManagerAudio> entry in cache)
            {
                if (!entry.Value.IsPLaying) continue;

                if (fadeout > 0.0f) entry.Value.Fadeout(fadeout, entry.Value.Stop);
                else entry.Value.Stop();
            }
        }

        #endregion

        #region 停止指定功能

        /// <summary>
        ///     通过id 停止播放的 effect 
        /// </summary>
        /// <param name="id">The id of the playing effect</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopEffect(int id, float fadeoutTime = 0.0f) =>
            StopBase(GetEffect(id), fadeoutTime);

        /// <summary>
        ///     通过 AudioClip停止播放的 effect 
        /// </summary>
        /// <param name="audioClip">The AudioClip of the playing effect</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopEffect(AudioClip audioClip, float fadeoutTime = 0.0f) =>
            StopBase(FindEffect(audioClip), fadeoutTime);

        /// <summary>
        ///     通过 SoundManagerAudio停止播放的 effect 
        /// </summary>
        /// <param name="soundManagerAudio">The SoundManagerAudio to stop</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopEffect(SoundManagerAudio soundManagerAudio, float fadeoutTime = 0.0f) =>
            StopBase(soundManagerAudio, fadeoutTime);

        /// <summary>
        ///     通过 id停止播放的 FX 
        /// </summary>
        /// <param name="id">The id of the playing FX</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopFx(int id, float fadeoutTime = 0.0f) =>
            StopBase(GetFx(id), fadeoutTime);

        /// <summary>
        ///     通过 AudioClip停止播放的 FX  
        /// </summary>
        /// <param name="audioClip">The AudioClip of the playing FX</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopFx(AudioClip audioClip, float fadeoutTime = 0.0f) =>
            StopBase(FindFx(audioClip), fadeoutTime);

        /// <summary>
        ///     通过 SoundManagerAudio停止播放的 FX 
        /// </summary>
        /// <param name="soundManagerAudio">The SoundManagerAudio to stop</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopFx(SoundManagerAudio soundManagerAudio, float fadeoutTime = 0.0f) =>
            StopBase(soundManagerAudio, fadeoutTime);

        /// <summary>
        ///     通过id停止 music 
        /// </summary>
        /// <param name="id">The id of the playing music</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopMusic(int id, float fadeoutTime = 0.0f) =>
            StopBase(GetMusic(id), fadeoutTime);

        /// <summary>
        ///     通过 AudioClip停止music 
        /// </summary>
        /// <param name="audioClip">The AudioClip of the playing music</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopMusic(AudioClip audioClip, float fadeoutTime = 0.0f) =>
            StopBase(FindMusic(audioClip), fadeoutTime);

        /// <summary>
        ///     通过 SoundManagerAudio停止music 
        /// </summary>
        /// <param name="soundManagerAudio">The SoundManagerAudio to stop</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        public static void StopMusic(SoundManagerAudio soundManagerAudio, float fadeoutTime = 0.0f) =>
            StopBase(soundManagerAudio, fadeoutTime);

        /// <summary>
        ///     为SoundManagerAudio基本方法共享停止逻辑的通用方法
        /// </summary>
        /// <param name="soundManagerAudio">The SoundManagerAudio to stop</param>
        /// <param name="fadeoutTime">淡出时间（秒）</param>
        private static void StopBase(SoundManagerAudio soundManagerAudio, float fadeoutTime)
        {
            if (soundManagerAudio == null) return;

            if (fadeoutTime > 0.0f)
                soundManagerAudio.Fadeout(fadeoutTime, soundManagerAudio.Stop);
            else
                soundManagerAudio.Stop();
        }

        #endregion

        #region Get

        /// <summary>
        ///     从effect缓存中返回SoundManagerAudio
        /// </summary>
        /// <param name="id">SoundManagerAudio的id</param>
        /// <returns></returns>
        public static SoundManagerAudio GetEffect(int id) =>
            GetterBase(FindEffect(id), id.ToString());

        /// <summary>
        ///     从effect缓存中返回SoundManagerAudio
        /// </summary>
        /// <param name="audioClip">获取缓存中的AudioClip</param>
        /// <returns></returns>
        public static SoundManagerAudio GetEffect(AudioClip audioClip) =>
            GetterBase(FindEffect(audioClip), audioClip.name);

        /// <summary>
        ///     从FX缓存中返回SoundManagerAudio
        /// </summary>
        /// <param name="id">SoundManagerAudio的id</param>
        /// <returns></returns>
        public static SoundManagerAudio GetFx(int id) =>
            GetterBase(FindFx(id), id.ToString());

        /// <summary>
        ///     从FX缓存中返回SoundManagerAudio
        /// </summary>
        /// <param name="audioClip">获取缓存中的AudioClip</param>
        /// <returns></returns>
        public static SoundManagerAudio GetFx(AudioClip audioClip) =>
            GetterBase(FindFx(audioClip), audioClip.name);

        /// <summary>
        ///     从music缓存中返回SoundManagerAudio
        /// </summary>
        /// <param name="id">SoundManagerAudio的id</param>
        /// <returns></returns>
        public static SoundManagerAudio GetMusic(int id) =>
            GetterBase(FindMusic(id), id.ToString());

        /// <summary>
        ///     从music缓存中返回SoundManagerAudio
        /// </summary>
        /// <param name="audioClip">获取缓存中的AudioClip</param>
        /// <returns></returns>
        public static SoundManagerAudio GetMusic(AudioClip audioClip) =>
            GetterBase(FindMusic(audioClip), audioClip.name);

        /// <summary>
        ///     通用方法在AudioClip getter之间共享getter逻辑
        /// </summary>
        /// <param name="audio">The SoundManagerAudio</param>
        /// <param name="name">搜索元素的名称或id</param>
        /// <returns></returns>
        /// <exception cref="GetAudioException"></exception>
        private static SoundManagerAudio GetterBase(SoundManagerAudio audio, string name)
        {
            if (audio != null)
                return audio;

            throw new GetAudioException($"Can't get {name}");
        }

        #endregion

        #region 查找功能

        /// <summary>
        ///     通过id找到指定Effect
        /// </summary>
        /// <param name="id">SoundManagerAudio的id</param>
        /// <returns></returns>
        [CanBeNull]
        public static SoundManagerAudio FindEffect(int id) =>
            EffectAudioCache.TryGetValue(id, out SoundManagerAudio value) ? value : null;

        /// <summary>
        ///     通过AudioClip找到指定Effect
        /// </summary>
        /// <param name="audioClip">要在缓存中找到的AudioClip</param>
        /// <returns></returns>
        [CanBeNull]
        public static SoundManagerAudio FindEffect(AudioClip audioClip) =>
            FinderBase(audioClip, FindEffect);

        /// <summary>
        ///     通过id找到指定Fx
        /// </summary>
        /// <param name="id">SoundManagerAudio的id</param>
        /// <returns></returns>
        [CanBeNull]
        public static SoundManagerAudio FindFx(int id) =>
            FxAudioCache.TryGetValue(id, out SoundManagerAudio value) ? value : null;

        /// <summary>
        ///     通过AudioClip找到指定Fx
        /// </summary>
        /// <param name="audioClip">要在缓存中找到的AudioClip</param>
        /// <returns></returns>
        [CanBeNull]
        public static SoundManagerAudio FindFx(AudioClip audioClip) =>
            FinderBase(audioClip, FindFx);

        /// <summary>
        ///     通过id找到指定Music
        /// </summary>
        /// <param name="id"></param>
        /// <returns>SoundManagerAudio的id</returns>
        [CanBeNull]
        public static SoundManagerAudio FindMusic(int id) =>
            MusicAudioCache.TryGetValue(id, out SoundManagerAudio value) ? value : null;

        /// <summary>
        ///     通过AudioClip找到指定Music
        /// </summary>
        /// <param name="audioClip">要在缓存中找到的AudioClip</param>
        /// <returns></returns>
        [CanBeNull]
        public static SoundManagerAudio FindMusic(AudioClip audioClip) =>
            FinderBase(audioClip, FindMusic);

        /// <summary>
        ///     通用方法,通过audioClip在字典中查找
        /// </summary>
        /// <param name="audioClip">要在缓存中找到的AudioClip</param>
        /// <param name="cache">查找SoundManagerAudio的字典</param>
        /// <returns></returns>
        /// <exception cref="GetAudioException"></exception>
        private static SoundManagerAudio FinderBase(AudioClip audioClip, Func<int, SoundManagerAudio> cache)
        {
            int id = audioClip.GetInstanceID();

            return cache(id);
        }

        #endregion

        #region 清除播放列表功能
        /// <summary>
        /// 清除所有播放列表
        /// </summary>
        /// <param name="includePlaying"></param>
        /// <param name="force"></param>
        public static void ClearAll(bool includePlaying = false, bool force = false)
        {
            ClearEffect(includePlaying, force);
            ClearFx(includePlaying, force);
            ClearMusic(includePlaying, force);
        }
        /// <summary>
        /// 清除Effect
        /// </summary>
        /// <param name="includePlaying"></param>
        /// <param name="force"></param>
        public static void ClearEffect(bool includePlaying = false, bool force = false)
        {
            Dictionary<int, SoundManagerAudio> localCopy = EffectAudioCache;

            foreach (KeyValuePair<int, SoundManagerAudio> soundManagerAudio in localCopy
                .Where(soundManagerAudio => force || includePlaying || !soundManagerAudio.Value.IsPLaying)
                .Where(soundManagerAudio => force || !soundManagerAudio.Value.DoNotDestroy))
            {
                soundManagerAudio.Value.DestroyAudioSource();
            }
        }
        /// <summary>
        /// 清除FX
        /// </summary>
        /// <param name="includePlaying"></param>
        /// <param name="force"></param>
        public static void ClearFx(bool includePlaying = false, bool force = false)
        {
            Dictionary<int, SoundManagerAudio> localCopy = FxAudioCache;

            foreach (KeyValuePair<int, SoundManagerAudio> soundManagerAudio in localCopy
                .Where(soundManagerAudio => force || includePlaying || !soundManagerAudio.Value.IsPLaying)
                .Where(soundManagerAudio => force || !soundManagerAudio.Value.DoNotDestroy))
            {
                soundManagerAudio.Value.DestroyAudioSource();
            }
        }
        /// <summary>
        /// 清除Music
        /// </summary>
        /// <param name="includePlaying"></param>
        /// <param name="force"></param>
        public static void ClearMusic(bool includePlaying = false, bool force = false)
        {
            Dictionary<int, SoundManagerAudio> localCopy = MusicAudioCache;

            foreach (KeyValuePair<int, SoundManagerAudio> soundManagerAudio in localCopy
                .Where(soundManagerAudio => force || includePlaying || !soundManagerAudio.Value.IsPLaying)
                .Where(soundManagerAudio => force || !soundManagerAudio.Value.DoNotDestroy))
            {
                soundManagerAudio.Value.DestroyAudioSource();
            }
        }

        #endregion

        #region 音量功能

        private static float _mainVolume = 1.0f;
        private static float _mainEffectVolume = 1.0f;
        private static float _mainFxVolume = 1.0f;
        private static float _mainMusicVolume = 1.0f;

        /// <summary>
        ///     主音量控制
        /// </summary>
        public static float MainVolume
        {
            get => _mainVolume;
            set
            {
                _mainVolume = Mathf.Clamp(value, 0f, 1f);

                MainEffectVolume = MainEffectVolume;
                MainFxVolume = MainFxVolume;
                MainMusicVolume = MainMusicVolume;
            }
        }

        /// <summary>
        ///     effect主音量
        /// </summary>
        public static float MainEffectVolume
        {
            get => _mainEffectVolume;
            set
            {
                _mainEffectVolume = Mathf.Clamp(value, 0f, 1f);

                foreach (KeyValuePair<int, SoundManagerAudio> entry in EffectAudioCache)
                    entry.Value.Volume = entry.Value.Volume;
            }
        }

        /// <summary>
        ///     FX主音量
        /// </summary>
        public static float MainFxVolume
        {
            get => _mainFxVolume;
            set
            {
                _mainFxVolume = Mathf.Clamp(value, 0f, 1f);

                foreach (KeyValuePair<int, SoundManagerAudio> entry in FxAudioCache)
                    entry.Value.Volume = entry.Value.Volume;
            }
        }

        /// <summary>
        ///     music主音量
        /// </summary>
        public static float MainMusicVolume
        {
            get => _mainMusicVolume;
            set
            {
                _mainMusicVolume = Mathf.Clamp(value, 0f, 1f);

                foreach (KeyValuePair<int, SoundManagerAudio> entry in MusicAudioCache)
                    entry.Value.Volume = entry.Value.Volume;
            }
        }

        #endregion
    }
}