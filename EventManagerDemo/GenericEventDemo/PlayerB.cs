using UnityEngine;
using WManager;

public class PlayerB : MonoBehaviour
{
    private int myCoins = 200;

    void Start()
    {
        EventManager.StartListening("PRESSED_A", MyCallBackFunction);

        EventManager.StartListening("PRESSED_S", gameObject, WhoIAmCallBack1);

        EventManager.StartListening("PRESSED_D", gameObject, WhoIAmCallBack2);
    }

    void MyCallBackFunction()
    {
        var coins = EventManager.GetInt("PRESSED_A");
        myCoins += coins;
        Debug.Log("<color=cyan>I'm PlayerB! I found " + coins + " coins and now I've got " + myCoins + " coins in total.</color>\n");
    }

    void WhoIAmCallBack1()
    {
        Debug.Log("<color=cyan>I'm PlayerB!</color>\n");
    }

    void WhoIAmCallBack2()
    {
        var sender = (GameObject)EventManager.GetSender("PRESSED_D");
        Debug.Log("<color=cyan>I'm PlayerB! The Object (sender) who emitted this event is: " + sender.name + "</color>\n");
    }
}
