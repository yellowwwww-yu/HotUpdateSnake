local PanelBase = require("UI.PanelBase")
local BagModel = require("Model.BagModel")
local UE = CS.UnityEngine
local BagCellType = typeof(CS.BagCell)

local ItemOperatePopup = PanelBase:Create()
local ItemId
local SourceRect
local PopupView
local Rect
local ParentRect
local gap = 8

function ItemOperatePopup:OnOpen(data)
    PopupView = self.view:GetComponent("PopupView")
    Rect = self.view:GetComponent("RectTransform")
    ParentRect = Rect.parent

    self.view:BindClick("Btn_Use", "OnClickUse")
    self.view:BindClick("Btn_Sell", "OnClickSell")
    self:Listen(BagModel.EVENT_CHANGED, function()
        self:OnModelChanged()
    end)
    PopupView:BindUpdateCallBack(function()
        self:OnUpdate()
    end)
    self:OnRefresh(data)
end

function ItemOperatePopup:OnRefresh(data)
    if data == nil or data.rect == nil or BagModel:GetById(data.id) == nil then
        UIManager:Close("ItemOperatePopup")
        return
    end

    ItemId = data.id
    SourceRect = data.rect
    UIManager:Close("DescriptionPopup")
    self:RefreshPosition()
end

function ItemOperatePopup:RefreshPosition()
    -- 同一 Canvas 下，把格子左侧中点转到弹窗父节点的局部空间。
    local point = SourceRect:TransformPoint(
        UE.Vector3(SourceRect.rect.xMin, SourceRect.rect.center.y, 0))
    local pos = ParentRect:InverseTransformPoint(point)
    Rect.localPosition = UE.Vector3(pos.x - gap, pos.y, 0)
end

function ItemOperatePopup:OnUpdate()
    if not UE.Input.GetMouseButtonDown(0) then
        return
    end

    local target = PopupView:GetPointerTarget()
    if target ~= nil then
        if target.transform == Rect or target.transform:IsChildOf(Rect) then
            return
        end

        local cell = target:GetComponentInParent(BagCellType)
        if cell ~= nil and cell.isActiveAndEnabled then
            return
        end
    end
    UIManager:Close("ItemOperatePopup")
end

function ItemOperatePopup:OnModelChanged()
    if ItemId ~= nil and BagModel:GetById(ItemId) == nil then
        UIManager:Close("ItemOperatePopup")
    end
end

function ItemOperatePopup:OnClickUse()
    if ItemId ~= nil then
        BagModel:Use(ItemId)
    end
end

function ItemOperatePopup:OnClickSell()
    if ItemId ~= nil then
        BagModel:Sell(ItemId)
    end
end

function ItemOperatePopup:OnClose()
    PopupView:UnBindUpdateCallBack()
    ItemId = nil
    SourceRect = nil
end

return ItemOperatePopup
