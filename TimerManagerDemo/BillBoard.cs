using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WManager;

public class BillBoard : MonoBehaviour {
    public Text parm;
    public Text log;
    public int setTime = 3;
    public bool loop = true;
    public bool ignore = true;


    //计数锁
    int lockNum = int.MaxValue;

    void Start () {
        parm.text = string.Format("定时【{0}】秒，循环【{1}】，忽略scale【{2}】",setTime,loop.ToString(),ignore.ToString());
        
        Time.timeScale = 0.5f; //设置了timescale

        Timer.AddTimer(setTime, "testignore",loop,ignore).OnUpdated((v)=> 
        {
            int time = Mathf.FloorToInt(v * setTime);
            if (time != lockNum)
            {
                lockNum = time;
                if (time!=0)
                {
                    log.text = time.ToString(); ;
                }
            }
        }).OnCompleted(()=> 
        {
           log.text =  string.Format( "{0}秒定时完成!", setTime);

            TestFunc("dadfaf",9897);
        });
	}

    private void TestFunc(string a,int b)
    {
        Debug.Log(a+":"+b.ToString());
    }
}
