using UnityEngine;
using WManager;

public class EnemyA : MonoBehaviour
{
    void Start()
    {
        EventManager.StartListening("PRESSED_A", MyCallBackFunction);
        
        EventManager.StartListening("PRESSED_G", gameObject, WhoIAmCallBack);
    }

    void MyCallBackFunction()
    {
        var points = EventManager.GetInt("PRESSED_A");
        Debug.Log("<color=orange>I'm EnemyA! Someone hit me: I've lost " + points + " points.</color>\n");
    }

    void WhoIAmCallBack()
    {
        Debug.Log("<color=orange>I'm EnemyA!</color>\n");
    }
}
