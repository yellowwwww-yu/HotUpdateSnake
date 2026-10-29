local PanelBase = require("UI.PanelBase")

local ExitPanel = PanelBase:Create()
local Application = CS.UnityEngine.Application

function ExitPanel:OnOpen()
    self.view:BindClick("Btn_Yes", "OnClickYes")
    self.view:BindClick("Btn_No", "OnClickNo")
end

function ExitPanel:OnClickYes()
    if Application.isEditor then
        CS.UnityEditor.EditorApplication.isPlaying = false
    else
        Application.Quit()
    end
end

function ExitPanel:OnClickNo()
    UIManager:Close("ExitPanel")
end

return ExitPanel
