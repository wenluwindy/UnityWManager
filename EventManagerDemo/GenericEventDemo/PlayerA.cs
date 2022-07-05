using UnityEngine;
using WManager;

public class PlayerA : MonoBehaviour
{
    void Start()
    {
        EventManager.StartListening("PRESSED_S", gameObject, WhoIAmCallBack1);

        EventManager.StartListening("PRESSED_D", gameObject, WhoIAmCallBack2);

        EventManager.StartListening("PRESSED_F", OnceListenerCallBack, "ONCE");
    }

    void OnceListenerCallBack()
    {
        Debug.Log("<color=cyan>I am PlayerA! Emitter said: " + EventManager.GetString("PRESSED_F")  + "</color>\n");
        Debug.Log("<color=cyan>If you press [F] again, I won't reply because I stopped the listening, just specifying the callBack ID.</color>\n");

        EventManager.StopListening("PRESSED_F", "ONCE");
    }

    void WhoIAmCallBack1()
    {
        Debug.Log("<color=cyan>I'm PlayerA!</color>\n");
    }

    void WhoIAmCallBack2()
    {
        var sender = (GameObject)EventManager.GetSender("PRESSED_D");
        Debug.Log("<color=cyan>I'm PlayerA! The Object (sender) who emitted this event is: " + sender.name + "</color>\n");
    }
}
