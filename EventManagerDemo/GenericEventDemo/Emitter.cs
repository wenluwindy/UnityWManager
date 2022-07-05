using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WManager;
using System.Linq;

/// <summary>
/// 事件发射器
/// </summary>
public class Emitter : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            Debug.Log("<color=green>发射事件：<b>PRESSED_A</b> ， 保存整数 <b>100</b>.</color>\n");
            EventManager.SetData("PRESSED_A", 100);
            EventManager.EmitEvent("PRESSED_A");
        }

        if (Input.GetKeyUp(KeyCode.S))
        {
            Debug.Log("<color=green>发射事件：<b>PRESSED_S</b> ， <b>两秒后</b>, 只发送给<b>'Player' 标签</b> 的监听者</color>\n");
            EventManager.EmitEvent("PRESSED_S", "tag:Player", 2);
        }

        if (Input.GetKeyUp(KeyCode.D))
        {
            Debug.Log("<color=green>发射事件：<b>PRESSED_D</b> ,只发送给<b>'Water' 图层</b> 的监听者, 并传入发件人。</color>\n");
            EventManager.EmitEvent("PRESSED_D", "layer:4", 0, gameObject);
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            Debug.Log("<color=green>发射事件：<b>PRESSED_F</b> ， EmitEvenData 方法('HELLO!' string 数据).</color>\n");
            EventManager.EmitEventData("PRESSED_F", "HELLO!");
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            Debug.Log("<color=green>发射事件：<b>PRESSED_G</b> ， 只发送给<b>名字包含'emy'</b> 和 <b>标签开头为'Enemy'</b>, 在<b>'Default' 图层</b> 的监听者</color>\n");
            EventManager.EmitEvent("PRESSED_G", "name:*emy*;tag:Enemy*;layer:0");
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            EventManager.EmitEvent("整", "name:*了*;tag:测试;layer:5");
        }
    }
}
