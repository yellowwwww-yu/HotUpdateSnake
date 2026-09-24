local PanelBase = require("UI.PanelBase");

-- 模块级缓存，省得每次点击都走一遍 CS 命名空间查找
local SceneManager = CS.UnityEngine.SceneManagement.SceneManager;
local LoadSceneMode = CS.UnityEngine.SceneManagement.LoadSceneMode;

local LobbyPanel = PanelBase:Create();

function LobbyPanel:OnOpen(data)
    self.view:BindClick("Btn_Start", "OnClickStart");
end

function LobbyPanel:OnClickStart()
    -- Additive 是叠加加载：GameScene 进来，PersistentScene 原地保留不卸载。
    -- 用 Single 的话 PersistentScene 会被换掉，LuaManager、Downloader、UIManager 全没了。
    SceneLoader:Load("GameScene")

    -- UIManager 这个全局本身就是 C# 那边的实例（Inject 注入的就是 this），
    -- 直接当对象用，不要再点 .Instance
    UIManager:Close("LobbyPanel");
end

return LobbyPanel;
