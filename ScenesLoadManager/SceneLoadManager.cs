using System.Net.Http.Headers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// 加载场景管理器
/// </summary>
[AddComponentMenu("管理器/场景加载管理器")]
public class SceneLoadManager : MonoBehaviour
{
    [InfoBox("除了使用激活时跳转场景功能，还可在其它脚本或按钮上调用\nCoroutineLoadScene( )异步加载\nFastLoadScene( )瞬间跳转")]
    [LabelText("激活脚本时就执行跳转场景")]
    public bool isStart = false;
    [ShowIf("isStart")]
    [LabelText("激活时跳转的场景名称")]
    [Required("必须输入场景名称")]
    public string SceneName; //目标场景名称
    [ShowIf("isStart")]
    [EnumToggleButtons,LabelText("加载方式")]
    public SEnum LoadMode;
    
    [HideIf("isStart")]
    [LabelText("使用进度条及加载百分比")]
    [Space(10)]
    public bool UsingSlider = false;
    [ShowIf("@(isStart && LoadMode == SEnum.Slow) || UsingSlider")]
    [LabelText("要加载的进度条")]
    public Slider slider; //滑动条（公开变量以手动指定进度条，或用Start()查找）
    [ShowIf("@(isStart && LoadMode == SEnum.Slow) || UsingSlider")]
    [LabelText("进度百分比")]
    public Text LoadText;
    int currentProgress; //当前进度
    int targetProgress;  //目标进度
    
    public enum SEnum
    {
        //异步加载方法
        Slow = 0, 
        //快速跳转方法
        Fast = 1
    }
    
    /// <summary>
    /// 设置进度为0，初始化跳转选项
    /// </summary>
    private void Start()
    {
        currentProgress = 0;
        targetProgress  = 0;
        //slider = GameObject.Find("Slider").GetComponent<Slider>(); //找到场景中为"Slider"的滑动条
        
        if (isStart)
        {
            switch (LoadMode)
            {
                case SEnum.Slow:
                    //打开Slider和Text
                    slider.gameObject.SetActive (true);
                    LoadText.gameObject.SetActive (true);
                    StartCoroutine(LoadingScene()); //开启协成
                    break;
                case SEnum.Fast:
                    SceneManager.LoadScene(SceneName);
                    break;
            }
            
            
            
        }
        
    }
    /// <summary>
    /// 异步指定加载
    /// </summary>
    /// <param name="a"></param>
    public void CoroutineLoadScene(string a)
    {
        //打开Slider和Text
        slider.gameObject.SetActive (true);
        LoadText.gameObject.SetActive (true);
        SceneName = a;
        StartCoroutine(LoadingScene()); //开启协成
    }
    /// <summary>
    /// 瞬间跳转
    /// </summary>
    /// <param name="a"></param>
    public void FastLoadScene(string a)
    {
        SceneName = a;
        SceneManager.LoadScene(a);
    }
    /// <summary>
    /// 更新进度条
    /// </summary>
    private void Update() 
    {
        if (LoadText == null)
        {
            return;
        }
        else
        {
            LoadText.text = (slider.value * 100).ToString() + "%";
        }
    }
    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <returns>协成</returns>
    private IEnumerator LoadingScene()
    {
        AsyncOperation asyncOperation       = SceneManager.LoadSceneAsync(SceneName); //异步加载1号场景
        asyncOperation.allowSceneActivation = false;                          //不允许场景立即激活//异步进度在 allowSceneActivation= false时，会卡在0.89999的一个值，这里乘以100转整形
        while (asyncOperation.progress < 0.9f)                                //当异步加载小于0.9f的时候
        {
            targetProgress = (int) (asyncOperation.progress * 100); //异步进度在 allowSceneActivation= false时，会卡在0.89999的一个值，这里乘以100转整形
            yield return LoadProgress();
        }
        targetProgress = 100; //循环后，当前进度已经为90了，所以需要设置目标进度到100；继续循环
        yield return LoadProgress();
        asyncOperation.allowSceneActivation = true; //加载完毕，这里激活场景 —— 跳转场景成功
        
    }
    /// <summary>
    /// 由于需要两次调用，在这里进行简单封装
    /// </summary>
    /// <returns>等一帧</returns>
    private IEnumerator<WaitForEndOfFrame> LoadProgress()
    {
        while (currentProgress < targetProgress) //当前进度 < 目标进度时
        {
            ++currentProgress;                            //当前进度不断累加 （温馨提示，如果场景很小，可以调整这里的值 例如：+=10 +=20，来调节加载速度）
            slider.value = (float) currentProgress / 100; //给UI进度条赋值
            yield return new WaitForEndOfFrame();         //等一帧
        }
    }
}
