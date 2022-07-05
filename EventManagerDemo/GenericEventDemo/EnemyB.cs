using UnityEngine;
using WManager;

public class EnemyB : MonoBehaviour
{
    void Start()
    {
        EventManager.StartListening("PRESSED_G", gameObject, WhoIAmCallBack);
    }

    void WhoIAmCallBack()
    {
        Debug.Log("<color=orange>I'm EnemyB!</color>\n");
    }
}
