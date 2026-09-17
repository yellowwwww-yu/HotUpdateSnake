using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BundleInfo
{
    public string name;
    public string hash;
}

[Serializable]
public class HotUpdateManifest
{
    public int version = 1;
    public List<BundleInfo> bundles;
}