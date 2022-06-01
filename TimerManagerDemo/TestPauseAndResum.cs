using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WManager;
/// <summary>
/// 在使用此定时器时，如果回调参数中包含场景游戏对象，且游戏对象总是频繁显示隐藏的，建议同步暂停和恢复运行中的计时器
/// 本例，在游戏对象隐藏时计数暂停，显示则计数恢复 (运行后，自己尝试显示隐藏这个脚本所在的游戏对象并观察输出)
/// </summary>
public class TestPauseAndResum : MonoBehaviour {
    private int lockNum = int.MaxValue;
    private Timer t;
    void Start () {
      t=  Timer.AddTimer(5, "TestForPauseAndResum",true).OnUpdated((v)=> 
        {
            int time = Mathf.FloorToInt(v * 5);
            if (time != lockNum)
            {
                lockNum = time;
                Debug.Log("时间是：" + time);
            }

        }).OnCompleted(()=> 
        {
            Debug.Log("TestForPause_5秒_计时完成！");
        });
        Timer.AddTimer(3, "123",true).OnCompleted(qq);
    }
	
	// Update is called once per frame
	void Update () {
        

    }
    private void OnEnable()
    {
        //if (null!=t)t.IsPause = false; 
        //或者使用这句：
        Timer.ResumTimer("TestForPauseAndResum");

    }
    private void OnDisable()
    {
        //if (null!=t)t.IsPause = true; 
        //或者使用这句：
        Timer.PauseTimer("TestForPauseAndResum");
    }
    void qq()
    {
        Debug.Log("ceshi");
    }
}
