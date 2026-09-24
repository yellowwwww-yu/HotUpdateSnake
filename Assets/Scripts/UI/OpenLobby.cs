using System.Collections;
using System.Collections.Generic;
using Framework.UI;
using UnityEngine;

public class OpenLobby : MonoBehaviour
{
    private void Start()
    {
        Downloader.Instance.onBundlesLoaded += Open;
    }

    private void Open()
    {
        UIManager.Instance.Open("LobbyPanel");
    }

    private void OnDestroy()
    {
        if (Downloader.Instance != null)
        {
            Downloader.Instance.onBundlesLoaded -= Open;
        }
    }
}
