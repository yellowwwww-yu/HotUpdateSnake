local PanelBase = require("UI/PanelBase")
local UE = CS.UnityEngine
local DescriptionPopup = PanelBase:Create()

local offset = { x = 16, y = 16 }

local Txt_Name
local Txt_Description
local PopupView
local Rect
local ParentRect

function DescriptionPopup:OnOpen(data)
    Txt_Name = self.view:Get("Txt_Name"):GetComponent("Text")
    Txt_Description = self.view:Get("Txt_Description"):GetComponent("Text")
    PopupView = self.view:GetComponent("PopupView")
    Rect = self.view:GetComponent("RectTransform")
    ParentRect = Rect.parent

    PopupView:BindUpdateCallBack(function()
        self:OnUpdate()
    end)
    self:OnRefresh(data)
end

function DescriptionPopup:OnUpdate()
    local mousePos = UE.Input.mousePosition
    local screenPos = UE.Vector2(mousePos.x, mousePos.y)
    -- 当前 Canvas 为 Screen Space - Overlay，因此 camera 传 nil。
    local ok, localPos = UE.RectTransformUtility.ScreenPointToLocalPointInRectangle(
        ParentRect, screenPos, nil)
    if ok then
        Rect.localPosition = UE.Vector3(localPos.x + offset.x, localPos.y + offset.y, 0)
    end
end

function DescriptionPopup:OnRefresh(data)
    Txt_Name.text = data.name
    Txt_Description.text = data.desc
    self:OnUpdate()
end

function DescriptionPopup:OnClose()
    PopupView:UnBindUpdateCallBack()
    Txt_Name.text = ""
    Txt_Description.text = ""
end

return DescriptionPopup
