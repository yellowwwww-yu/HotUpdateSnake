using System;
using System.Collections.Generic;

/// <summary>
/// 清单里的单个 AB 包：包名 + 内容 MD5，客户端用它判断本地缓存要不要更新。
/// </summary>
[Serializable]
public class BundleInfo
{
    public string name;
    public string hash;
}

/// <summary>
/// 热更新清单。打包后生成并上传到服务器，客户端下载后和本地记录做对比。
/// </summary>
[Serializable]
public class HotUpdateManifest
{
    public int version = 1;
    public List<BundleInfo> bundles;
}
