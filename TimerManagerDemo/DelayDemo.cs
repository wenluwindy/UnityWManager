using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WManager;
public class DelayDemo : MonoBehaviour
{
    Timer t;
    int lockNum = int.MaxValue;
    void Start()
    {
        Timer.AddTimer(5, "12545", false, false).OnCompleted(() =>
            {
                Debug.Log("定时完成");
            }).OnUpdated((v) =>
            {
                int time = Mathf.FloorToInt(v * 5);
                if (time != lockNum)
                {
                    lockNum = time;
                    Debug.Log("时间是：" + time);
                }
            });

        Time.timeScale = 0.5f;

        // Timer.AddTimer(8, "12545", true).OnCompleted(OnCompleted_test);

        //   Timer.AddTimer(3, "12545", true).OnUpdated(OnUpdate_test);
        //   t= Timer.AddTimer(1, "7421", true).OnCompleted(() => { Debug.Log("1秒延时完成！！"); });
    }

    void Update()
    {

        if (Input.GetMouseButtonDown(1))
        {
            t.Setloop(true);
        }
        if (Input.GetMouseButtonDown(2))
        {
            t.SetIgnoreTimeScale(true);
        }

        if (Input.GetMouseButtonDown(0))
        {
            Timer.DelTimer("12545"); //删除定时器
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            Timer.DelTimer(OnCompleted_test);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Timer.DelTimer(OnUpdate_test);
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            Timer.DelTimer(t);
        }
    }

    private void OnCompleted_test()
    {
        Debug.Log("8秒延时完成！");
    }
    private void OnUpdate_test(float value)
    {
        Debug.Log("------value:" + value);
    }

}
