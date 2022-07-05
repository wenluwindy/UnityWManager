using UnityEngine;
using WManager;

public class MainPlayer : MonoBehaviour
{
    EventsGroup Listeners = new EventsGroup();

    int coins = 0;

    void Start()
    {

        Listeners.Add("ON_ENEMY_SPAWNED", OnEnemySpawned);
        Listeners.Add("ON_ENEMY_KILLED", OnEnemyKilled);
        Listeners.Add("ON_COIN_TAKEN", OnCoinTaken);

        Listeners.StartListening();

    }

    void OnEnemySpawned()
    {
        var eventData = EventManager.GetDataGroup("ON_ENEMY_SPAWNED");

        if (eventData != null)
        Debug.Log("<color=red>一个新的敌人诞生了: " + 
            "id " + eventData[0].ToInt() + 
            ", 类型: " + eventData[1].ToString() + 
            ", 生命: " + eventData[2].ToInt() + 
            ", 力量: " + eventData[3].ToInt() + 
            ", 飞行能力: " + eventData[4].ToBool() + 
            "</color>\n");
    }

    void OnEnemyKilled()
    {
        var eventData = EventManager.GetIndexedDataGroup("ON_ENEMY_KILLED");

        Debug.Log("<color=cyan>你杀死一个敌人并获得: " + 
            eventData.ToInt("points") + " 分, " + 
            eventData.ToInt("coins") + " 金币和一个 " + 
            eventData.ToString("bonus") + "!</color>\n");
    }

    void OnCoinTaken()
    {
        coins += EventManager.GetInt("ON_COIN_TAKEN");
        Debug.Log("<color=yellow>你得到了一些金币!现在你已经有了: " + coins + " 金币.</color>\n");

    }

    void OnDestroy()
    {
        Listeners.StopListening();
    }


}
