local PanelBase = require("UI.PanelBase")
local PlayerModel = require("Model.PlayerModel")
local SettingsPanel = PanelBase:Create()

local Slider_Music
local Slider_Sound
local Slider_Light
local Txt_Name
local Img_Avator

-- 加在模块顶部；组件引用沿用现有面板的文件级local风格。
local UE = CS.UnityEngine
local Storage = CS.SettingsStorage
local Toggle_Window
local Toggle_FullScreen
local Dropdown_Resolution
local Sizes
local WindowWidth
local WindowHeight
local IsFullScreen
local CanChangeDisplay

function SettingsPanel:OnOpen()
    Slider_Music = self.view:Get("Slider_Music"):GetComponent("Slider")
    Slider_Sound = self.view:Get("Slider_Sound"):GetComponent("Slider")
    self:RefreshVolume()

    self.view:BindSliderChanged("Slider_Music", "OnMusicChanged")
    self.view:BindSliderChanged("Slider_Sound", "OnSoundChanged")
    self:InitDisplay()
    self:InitLight()

    Txt_Name = self.view:Get("Txt_Name"):GetComponent("Text")
    Img_Avator = self.view:Get("Img_Avator"):GetComponent("Image")
    self:Listen(PlayerModel.EVENT_CHANGED, function()
        self:RefreshAccount()
    end)
    self:RefreshAccount()

    self.view:BindClick("Btn_LogOut", "OnClickLogOut")
    self.view:BindClick("Btn_Close", "OnClickClose")
    self.view:BindClick("Btn_Music", "OnClickMusic")
    self.view:BindClick("Btn_Image", "OnClickImage")
    self.view:BindClick("Btn_Account", "OnClickAccount")
    self:ShowPage("Settings_Music")
end

function SettingsPanel:RefreshVolume()
    Slider_Music:SetValueWithoutNotify(Audio.MusicVolume)
    Slider_Sound:SetValueWithoutNotify(Audio.SoundVolume)
end

function SettingsPanel:OnMusicChanged(value)
    Audio:SetMusicVolume(value)
    Storage.MusicVolume = Audio.MusicVolume
end

function SettingsPanel:OnSoundChanged(value)
    Audio:SetSoundVolume(value)
    Storage.SoundVolume = Audio.SoundVolume
end

function SettingsPanel:OnRefresh()
    self:RefreshVolume()
    self:RefreshDisplay()
    self:RefreshLight()
    self:RefreshAccount()
end

function SettingsPanel:OnClose()
    Storage.Save()
    self.view:UnBindSliderChanged("Slider_Music")
    self.view:UnBindSliderChanged("Slider_Sound")
    self:ReleaseDisplay()
    self:ReleaseLight()
    Txt_Name = nil
    Img_Avator = nil
end

-- 在OnOpen完成音量初始化后调用self:InitDisplay()。
function SettingsPanel:InitDisplay()
    Toggle_Window = self.view:Get("Toggle_Window"):GetComponent("Toggle")
    Toggle_FullScreen = self.view:Get("Toggle_FullScreen"):GetComponent("Toggle")
    Dropdown_Resolution = self.view:Get("Dropdown_Resolution"):GetComponent("Dropdown")

    CanChangeDisplay = UE.Application.platform == UE.RuntimePlatform.WindowsPlayer
    IsFullScreen = UE.Screen.fullScreen
    local desktop = UE.Screen.currentResolution
    WindowWidth = IsFullScreen and math.min(WindowWidth or Storage.WindowWidth, desktop.width) or UE.Screen.width
    WindowHeight = IsFullScreen and math.min(WindowHeight or Storage.WindowHeight, desktop.height) or UE.Screen.height

    Sizes = {}
    local function Add(width, height)
        if width <= 0 or height <= 0 or width > desktop.width or height > desktop.height then return end
        for _, size in ipairs(Sizes) do
            if size[1] == width and size[2] == height then return end
        end
        Sizes[#Sizes + 1] = { width, height }
    end
    Add(1280, 720)
    Add(1920, 1080)
    Add(2560, 1440)
    Add(3840, 2160)
    Add(WindowWidth, WindowHeight)
    Add(desktop.width, desktop.height)

    Dropdown_Resolution:ClearOptions()
    for _, size in ipairs(Sizes) do
        local option = UE.UI.Dropdown.OptionData(size[1] .. " * " .. size[2])
        Dropdown_Resolution.options:Add(option)
    end
    self:RefreshDisplay()
    self.view:BindToggleChanged("Toggle_Window", "OnWindowChanged")
    self.view:BindToggleChanged("Toggle_FullScreen", "OnFullScreenChanged")
    self.view:BindDropdownChanged("Dropdown_Resolution", "OnResolutionChanged")
end

function SettingsPanel:RefreshDisplay()
    Toggle_Window:SetIsOnWithoutNotify(not IsFullScreen)
    Toggle_FullScreen:SetIsOnWithoutNotify(IsFullScreen)
    Toggle_Window.interactable = CanChangeDisplay
    Toggle_FullScreen.interactable = CanChangeDisplay
    Dropdown_Resolution.interactable = CanChangeDisplay and not IsFullScreen

    local width, height = WindowWidth, WindowHeight
    if IsFullScreen then
        local desktop = UE.Screen.currentResolution
        width, height = desktop.width, desktop.height
    end
    for index, size in ipairs(Sizes) do
        if size[1] == width and size[2] == height then
            Dropdown_Resolution:SetValueWithoutNotify(index - 1)
            break
        end
    end
    Dropdown_Resolution:RefreshShownValue()
end

function SettingsPanel:ApplyDisplay()
    if not CanChangeDisplay then return end
    local width, height = WindowWidth, WindowHeight
    local mode = UE.FullScreenMode.Windowed
    if IsFullScreen then
        local desktop = UE.Screen.currentResolution
        width, height = desktop.width, desktop.height
        mode = UE.FullScreenMode.FullScreenWindow
    end
    UE.Screen.SetResolution(width, height, mode)
    Storage.SetDisplay(WindowWidth, WindowHeight, IsFullScreen)
    self:RefreshDisplay()
end

function SettingsPanel:OnWindowChanged(value)
    if not value or not CanChangeDisplay then return end
    IsFullScreen = false
    self:ApplyDisplay()
end

function SettingsPanel:OnFullScreenChanged(value)
    if not value or not CanChangeDisplay then return end
    IsFullScreen = true
    self:ApplyDisplay()
end

function SettingsPanel:OnResolutionChanged(value)
    if not CanChangeDisplay or IsFullScreen then return end
    local size = Sizes[value + 1]
    if size == nil then return end
    WindowWidth, WindowHeight = size[1], size[2]
    self:ApplyDisplay()
end

-- 在OnClose音量解绑之后调用self:ReleaseDisplay()。
function SettingsPanel:ReleaseDisplay()
    self.view:UnBindToggleChanged("Toggle_Window")
    self.view:UnBindToggleChanged("Toggle_FullScreen")
    self.view:UnBindDropdownChanged("Dropdown_Resolution")
    Toggle_Window = nil
    Toggle_FullScreen = nil
    Dropdown_Resolution = nil
    Sizes = nil
end


-- 在OnOpen里调用self:InitLight()。
function SettingsPanel:InitLight()
    Slider_Light = self.view:Get("Slider_Light"):GetComponent("Slider")
    self:RefreshLight()
    self.view:BindSliderChanged("Slider_Light", "OnLightChanged")
end

function SettingsPanel:RefreshLight()
    Slider_Light:SetValueWithoutNotify(Brightness.Value)
end

function SettingsPanel:OnLightChanged(value)
    Brightness:SetValue(value)
    Storage.Brightness = Brightness.Value
end

-- 在OnClose里调用self:ReleaseLight()。
function SettingsPanel:ReleaseLight()
    self.view:UnBindSliderChanged("Slider_Light")
    Slider_Light = nil
end


function SettingsPanel:RefreshAccount()
    local profile = PlayerModel:Get()
    Txt_Name.text = profile.nick
    Img_Avator.sprite = Downloader:GetSprite(
        "ui_sprite", string.format("Avatars_%02d", profile.avatar))
end

function SettingsPanel:ShowPage(name)
    self.view:Get("Settings_Music"):SetActive(name == "Settings_Music")
    self.view:Get("Settings_Image"):SetActive(name == "Settings_Image")
    self.view:Get("Settings_Account"):SetActive(name == "Settings_Account")
end

function SettingsPanel:OnClickMusic()
    self:ShowPage("Settings_Music")
end

function SettingsPanel:OnClickImage()
    self:ShowPage("Settings_Image")
end

function SettingsPanel:OnClickAccount()
    self:ShowPage("Settings_Account")
end

function SettingsPanel:OnClickClose()
    UIManager:Close("SettingsPanel")
end

function SettingsPanel:OnClickLogOut()
    UIManager:Open("LoginPanel")
    if not UIManager:IsOpen("LoginPanel") then return end

    UIManager:Close("SettingsPanel")
    UIManager:Close("LobbyPanel")
end

return SettingsPanel
