using UnityEngine;
using WManager;

public class EnemyC : MonoBehaviour
{
    void Start()
    {
        EventManager.StartListening("PRESSED_G", gameObject, WhoIAmCallBack);

        EventManager.StartListening("PRESSED_F", WhoIAmCallBack);
    }

    void WhoIAmCallBack()
    {
        Debug.Log("<color=orange>I'm EnemyC! Emitter said: " + EventManager.GetString("PRESSED_F") + "</color>\n");
    }
}
