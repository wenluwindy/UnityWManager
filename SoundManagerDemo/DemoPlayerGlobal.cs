using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using WManager;

public class DemoPlayerGlobal : MonoBehaviour
{
    private List<SoundManager.SoundManagerAudio> _pauseList = new List<SoundManager.SoundManagerAudio>();
    
    [FormerlySerializedAs("globalFadeOutTime")]
    [SerializeField]
    [Range(0, 10f)]
    private float _globalFadeOutTime = 2f;

    public void MainVolume(float mainVolume)
    {
        SoundManager.MainVolume = mainVolume;
    }
    
    public void ClearAll()
    {
        SoundManager.ClearAll();
    }
    
    public void ForceClearAll()
    {
        SoundManager.ClearAll(true, true);
    }
    
    public void StopAll()
    {
        SoundManager.StopAll(_globalFadeOutTime);
    }

    public void PauseAll()
    {
        _pauseList = SoundManager.PauseAll(_globalFadeOutTime);
    }
    
    public void ResumeAll()
    {
        _pauseList.ForEach(x => x.Play());
        _pauseList.Clear();
    }
}