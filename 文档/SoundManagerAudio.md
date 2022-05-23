作者：纹路风

SoundManager.SoundManagerAudio

版本：V1.0

<h3>简介</h3>

这个类被<b>SoundManager</b>脚本用来包装Unity <a href="https://docs.unity3d.com/ScriptReference/AudioClip.html" target="_blank">AudioClip</a> 类，并添加更多的高级功能。 

<h4>属性</h4>

这些属性都是只读（get）属性，不可修改，一般用作判断。 <tbl> {{名称{}类型{}说明}} {AudioClip!<a href="https://docs.unity3d.com/ScriptReference/AudioClip.html" target="_blank">AudioClip</a>!当使用<b>SoundManagerAudio</b>对象时将播放的AudioClip。} {DoNotDestroy!bool!<b>SoundManagerAudio</b>对象在播放完成后会自动删除本身。} {Duration!float!AudioClip的播放时长。} {IsPLaying!bool!判断<b>SoundManagerAudio</b>当前是否正在播放。} 

<h4>示例</h4>

// 引用WManager命名空间 

using WManager; 

public class Demo : MonoBehaviour 
{ 
public AudioClip BGM;
void Start() 
{ 
    // 创建一个SoundManagerAudio，并在SoundManager下初始化为Music类型 
    SoundManager.SoundManagerAudio audio = oundManager.PrepareMusic(BGM);
    // 打印出要播放的audio的名字、是否播放后删除、播放时长
    Debug.Log(audio.AudioClip.name); 
    Debug.Log(audio.DoNotDestroy); 
    Debug.Log(audio.Duration); 
} 
    // 一个调用后既能暂停，又能播放的事件 
    public void PlayPauseBGM() 
    { 
    // 找到名为BGM的AudioClip 
    var a = SoundManager.FindMusic(BGM); 
    // 判断其是否为空与是否正在播放 
    if (a != null &amp;&amp; a.IsPLaying) 
    { 
        // 暂停音乐 
        SoundManager.PauseMusic(BGM); 
    } 
    else 
    { 
        // 播放音乐 
        SoundManager.PlayMusic(BGM); 
    } 
} 
} 
