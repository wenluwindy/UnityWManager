using UnityEngine;
using WManager;

public class DemoGroupEmitter : MonoBehaviour
{

    void Start()
    {
        Debug.Log("Event Manager, 版本2.3!\n");
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            Debug.Log("<color=green>我是发射器: 我发射了 <b>ON_ENEMY_SPAWNED</b> <i>事件</i> 随机数据由<b>SetDataGroup</b> 方法管理.</color>\n");

            var randomNumber = Random.Range(1, 5);
            if (randomNumber == 1) EventManager.SetDataGroup("ON_ENEMY_SPAWNED", randomNumber, "怪兽", 1200, 600, false);
            if (randomNumber == 2) EventManager.SetDataGroup("ON_ENEMY_SPAWNED", randomNumber, "恶魔", 1400, 1000, true);
            if (randomNumber == 3) EventManager.SetDataGroup("ON_ENEMY_SPAWNED", randomNumber, "龙", 2000, 1500, true);
            if (randomNumber == 4) EventManager.SetDataGroup("ON_ENEMY_SPAWNED", randomNumber, "巨魔", 2200, 2000, false);

            EventManager.EmitEvent("ON_ENEMY_SPAWNED");
        }

        if (Input.GetKeyUp(KeyCode.S))
        {
            Debug.Log("<color=green>我是发射器: 我发射了 <b>ON_ENEMY_KILLED</b> <i>事件</i> 一些数据由 <b>SetIndexedDataGroup</b> 方法管理.</color>\n");

            EventManager.SetIndexedDataGroup(
                "ON_ENEMY_KILLED",
                new EventManager.DataGroup { id = "points", data = 100 },
                new EventManager.DataGroup { id = "coins", data = 250 },
                new EventManager.DataGroup { id = "bonus", data = "铁盾" }
                );

            EventManager.EmitEvent("ON_ENEMY_KILLED");
        }

        if (Input.GetKeyUp(KeyCode.D))
        {
            Debug.Log("<color=green>我是发射器: 我发射了 <b>ON_COIN_TAKEN</b> <i>事件</i> 和50的值并使用 <b>EmitEventData</b> 方法管理.</color>\n");

            EventManager.EmitEventData("ON_COIN_TAKEN", 50);
        }

    }
}
