local PanelBase = require("UI.PanelBase")
local PlayerModel = require("Model.PlayerModel")

local LoginPanel = PanelBase:Create()

local InputField = CS.UnityEngine.UI.InputField
local LocalAccountFile = CS.LocalAccountFile
local Color = CS.UnityEngine.Color

local Input_Account
local Input_Password
local Txt_Account_Tip
local Txt_Password_Tip
local Txt_Tip

local specialAccount = "[!@#$%%^&*() ]"
local specialPassword = "[!@#$%%^&*() ]"
local passwordLimit = 6

local accountAccepted = false
local passwordAccepted = false

local function FindPlayer(data, account)
    for i=0, data.players.Count - 1 do
        if data.players[i].account == account then
            return data.players[i]
        end
    end
    return nil
end

function LoginPanel:OnOpen(data)
    Input_Account = self.view:Get("Input_Account"):GetComponent("InputField")
    Input_Password = self.view:Get("Input_Password"):GetComponent("InputField")
    Txt_Account_Tip = self.view:Get("Txt_Account_Tip"):GetComponent("Text")
    Txt_Password_Tip = self.view:Get("Txt_Password_Tip"):GetComponent("Text")
    Txt_Tip = self.view:Get("Txt_Tip"):GetComponent("Text")
    
    self.view:BindInputChanged("Input_Account", "OnAccountChanged")
    self.view:BindInputChanged("Input_Password", "OnPasswordChanged")

    self.view:BindClick("Btn_Login", "OnClickLogin")
    self.view:BindClick("Btn_Register", "OnClickRegister")
    self.view:BindClick("Btn_Eye", "OnClickEye")

    Txt_Account_Tip.text = ""
    Txt_Password_Tip.text = ""
    Txt_Tip.text = ""

    Input_Password.characterLimit = passwordLimit
end

function LoginPanel:OnAccountChanged(value)
    if string.find(value, specialAccount) then
        Txt_Account_Tip.text = "账号里不能包含特殊字符"..specialAccount
        accountAccepted = false
    else
        Txt_Account_Tip.text = ""
        accountAccepted = true
    end
end

function LoginPanel:OnPasswordChanged(value)
    if string.find(value, specialPassword) then
        Txt_Password_Tip.text = "密码里不能包含特殊字符"..specialPassword
        passwordAccepted = false
    elseif string.find(value, "[\228-\233][\128-\191][\128-\191]") then
        Txt_Password_Tip.text = "密码里不能包含中文字符"
        passwordAccepted = false
    elseif #value < passwordLimit then
        Txt_Password_Tip.text = "密码长度不能少于"..passwordLimit.."个字符"
        passwordAccepted = false
    else
        Txt_Password_Tip.text = ""
        passwordAccepted = true
    end
end

function LoginPanel:OnClickLogin()
    if not accountAccepted then
        Txt_Tip.color = Color.red
        Txt_Tip.text = "账号不符合要求"
        return
    end
    if not passwordAccepted then
        Txt_Tip.color = Color.red
        Txt_Tip.text = "密码不符合要求"
        return
    end
    Txt_Tip.text = ""

    local account = Input_Account.text
    local password = Input_Password.text
    
    local data = LocalAccountFile.Load()
    local player = FindPlayer(data, account)
    if player == nil then
        Txt_Tip.color = Color.red
        Txt_Tip.text = "账号不存在，请点击注册"
        return
    end
    if player.password ~= password then
        Txt_Tip.color = Color.red
        Txt_Tip.text = "密码错误"
        return
    end
    Txt_Tip.color = Color.green
    Txt_Tip.text = "登录成功"

    PlayerModel:SetProfile(player.nick, player.level, player.avatar)

    UIManager:Open("LobbyPanel")
    if UIManager:IsOpen("LoginPanel") then
        UIManager:Close("LoginPanel")
    end
end

function LoginPanel:OnClickRegister()
    if not accountAccepted then
        Txt_Tip.color = Color.red
        Txt_Tip.text = "账号不符合要求"
        return
    end
    if not passwordAccepted then
        Txt_Tip.color = Color.red
        Txt_Tip.text = "密码不符合要求"
        return
    end
    Txt_Tip.text = ""

    local account = Input_Account.text
    local password = Input_Password.text

    local data = LocalAccountFile.Load()
    local player = FindPlayer(data, account)
    if player ~= nil then
        Txt_Tip.color = Color.red
        Txt_Tip.text = "账号已存在，请点击登录"
        return
    end

    local newPlayer = CS.LocalPlayerData()
    newPlayer.account = account
    newPlayer.password = password
    newPlayer.nick = account
    newPlayer.level = 1
    newPlayer.avatar = 1

    data.players:Add(newPlayer)
    LocalAccountFile.Save(data)
    Txt_Tip.color = Color.green
    Txt_Tip.text = "注册成功，请点击登录"
end

function LoginPanel:OnClickEye()
    if Input_Password.contentType == InputField.ContentType.Password then
        Input_Password.contentType = InputField.ContentType.Standard
    else
        Input_Password.contentType = InputField.ContentType.Password
    end
    Input_Password:ForceLabelUpdate()
end

function LoginPanel:OnClose()
    self.view:UnBindInputChanged("Input_Account")
    self.view:UnBindInputChanged("Input_Password")

    Txt_Account_Tip.text = ""
    Txt_Password_Tip.text = ""
    Txt_Tip.text = ""
    accountAccepted = false
    passwordAccepted = false
    Input_Account.text = ""
    Input_Password.text = ""
end

return LoginPanel