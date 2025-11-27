using UnityEngine;
using System.Collections;
using TMPro;

public class MessageBox : MonoBehaviour
{
    public static MessageBox instance;

    void Awake()
    {
        instance = this;
    }

    public void AddMessage(string newMessage)
    {
        if (SimulatorHUD.Instance != null)
        {
            SimulatorHUD.Instance.Log(newMessage);
        }
        else
        {
            Debug.Log($"[MessageBox] {newMessage}");
        }
    }

    public void ResetMessage()
    {
        // No-op for now as HUD log is persistent or handled by HUD
    }
}
