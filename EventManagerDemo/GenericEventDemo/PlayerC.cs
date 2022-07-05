using UnityEngine;
using WManager;

public class PlayerC : MonoBehaviour
{
    void Start()
    {
        EventManager.StartListening("PRESSED_A", MyCallBackFunction);

        EventManager.StartListening("PRESSED_S", gameObject, WhoIAmCallBack1);

        EventManager.StartListening("PRESSED_D", gameObject, WhoIAmCallBack2);
    }

    void MyCallBackFunction()
    {
        var powerup = EventManager.GetInt("PRESSED_A");
        Debug.Log("<color=cyan>I'm PlayerC! I found a power up and now my Strenght is " + powerup + ".</color>\n");
    }

    void WhoIAmCallBack1()
    {
        Debug.Log("<color=cyan>I'm PlayerC!</color>\n");
    }

    void WhoIAmCallBack2()
    {
        var sender = (GameObject)EventManager.GetSender("PRESSED_D");
        Debug.Log("<color=cyan>I'm PlayerC! The Object (sender) who emitted this event is: " + sender.name + "</color>\n");
    }
}
