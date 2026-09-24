local PanelBase = require("UI.PanelBase")

local LoginPanel = PanelBase:Create()

local num = 0

function LoginPanel:OnOpen(data)
    self.view:Get("Txt_Btn_Login"):GetComponent("Text").text = "Login"
    self.view:BindClick("Btn_Login", "OnClickLogin");
end

function LoginPanel:OnClickLogin()
    local testText = self.view:Get("Txt_Test"):GetComponent("Text").text
    num = num + 1
    testText = testText .. " " .. num
    self.view:Get("Txt_Test"):GetComponent("Text").text = testText
end

return LoginPanel