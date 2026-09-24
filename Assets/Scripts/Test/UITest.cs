using System.Collections;
using System.Collections.Generic;
using Framework.UI;
using UnityEngine;

public class UITest : MonoBehaviour
{
    private void Start()
    {
        Downloader.Instance.onBundlesLoaded += Test;
    }
    void Test()
    {
        UIManager.Instance.Open("LoginPanel");
    }

    private void OnDestroy()
    {
        if (Downloader.Instance != null)
        {
            Downloader.Instance.onBundlesLoaded -= Test;
        }        
    }
}
