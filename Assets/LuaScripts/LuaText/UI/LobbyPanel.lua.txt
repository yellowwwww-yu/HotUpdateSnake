-- Presenter：唯一同时认识 View 和 Model 的地方。
-- 控件名只出现在这里；业务数据只放 Model。
local PanelBase = require("UI.PanelBase")
local PlayerModel = require("Model.PlayerModel")

local LobbyPanel = PanelBase:Create()

-- 头像所在的 AB 包。Downloader 只从已加载的包里借资源，
-- 所以这个包必须先进 CommonBundleLoader 的常驻列表。
local SPRITE_BUNDLE = "ui_sprite"

-- 头像编号 -> ui_sprite 包里的资源名。
-- 现状：包里只有一张 Avatars_01.png（spriteMode = Single，不是图集），编号 0 就是它。
-- 以后按 Avatar_0 / Avatar_1 命名的话，只改这一行。
-- 这是表现层的知识（资源怎么命名），不进 Model。
local function AvatarAssetName(id)
    return string.format("Avatars_%02d", id)
end

-- UIManager 打开面板 → UIPanel.CallOpen → 到这里
function LobbyPanel:OnOpen(data)
    self:PlayLobbyMusic()
    -- Presenter → View：绑定控件
    self.view:BindClick("Btn_Start", "OnClickStart")
    self.view:BindClick("Btn_Rank", "OnClickRank")
    self.view:BindClick("Btn_Bag", "OnClickBag")
    self.view:BindClick("Btn_Set", "OnClickSettings")

    -- Model → Presenter：订阅。
    -- EventCenter 广播时只传数据、不传 self（内部是 pcall(callback, data)），
    -- 所以这里用闭包把面板带进去，回调里的 self 才是对的。
    -- 退订由 UIPanel.CallClose → OnCloseInternal → ReleaseEvents 兜底。
    self:Listen(PlayerModel.EVENT_CHANGED, function()
        self:OnModelChanged()
    end)

    self:Refresh()
end

-- 面板还开着时被再 Open 一次走这条
function LobbyPanel:OnRefresh(data)
    self:PlayLobbyMusic()
    self:Refresh()
end

-- 音乐随大厅生命周期；普通弹窗叠加不会关闭大厅。
function LobbyPanel:PlayLobbyMusic()
    if Audio == nil or AudioResources == nil then return end

    local clip = AudioResources:GetClip("common", "lobby_bgm_loop")
    if clip ~= nil then
        -- 同曲重复请求由 AudioManager 保持进度，不从头播放。
        Audio:PlayMusic(clip)
    end
end

-- View → Presenter 的入口（方法名必须和 BindClick 第二参一致）
function LobbyPanel:OnClickRank()
    UIManager:Open("RankPanel")
end

function LobbyPanel:OnClickBag()
    UIManager:Open("BagPanel")
end

function LobbyPanel:OnClickSettings()
    UIManager:Open("SettingsPanel")
end

function LobbyPanel:OnClickStart()
    -- Additive 是叠加加载：GameScene 进来，PersistentScene 原地保留不卸载。
    -- 用 Single 的话 PersistentScene 会被换掉，LuaManager、Downloader、UIManager 全没了。
    SceneLoader:Load("GameScene")

    -- UIManager 这个全局本身就是 C# 那边的实例（Inject 注入的就是 this），
    -- 直接当对象用，不要再点 .Instance
    UIManager:Close("LobbyPanel")
end

-- Model → Presenter 的入口
function LobbyPanel:OnModelChanged()
    self:Refresh()
end

-- Presenter → View：把 Model 的资料搬到控件上
function LobbyPanel:Refresh()
    local profile = PlayerModel:Get()

    self:SetText("Txt_Nick", profile.nick)
    self:SetText("Txt_Level", "Lv." .. profile.level)
    self:SetAvatar(profile.avatar)

    -- 剩下三个数据节点这轮不接，它们缺的不是代码而是数据源：
    --   Txt_Online / Img_OnlineState → 等 11 月网络
    --   Txt_Version                  → 等 10/30 阶段③的 version.json
end

-- Presenter → View：写文本控件
function LobbyPanel:SetText(key, text)
    local node = self.view:Get(key)

    if node == nil then
        return
    end

    local label = node:GetComponent("Text")

    if label == nil then
        return
    end

    label.text = text
end

-- Presenter → View：设头像图。Downloader 只从已加载的包借资源，不加载也不计数
function LobbyPanel:SetAvatar(id)
    local node = self.view:Get("Img_Avatar")

    if node == nil then
        return
    end

    local image = node:GetComponent("Image")

    if image == nil then
        return
    end

    image.sprite = Downloader:GetSprite(SPRITE_BUNDLE, AvatarAssetName(id))
end

function LobbyPanel:OnClose()
    if Audio ~= nil then
        Audio:StopMusic()
    end
end

return LobbyPanel
