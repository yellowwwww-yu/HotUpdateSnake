local PanelBase = require("UI.PanelBase")
local BagModel = require("Model.BagModel")

local BagPanel = PanelBase:Create()
local BagView
local Txt_Tip

function BagPanel:OnOpen()
    BagView = self.view:Get("List"):GetComponent("BagListView")
    Txt_Tip = self.view:Get("Txt_Tip"):GetComponent("Text")
    self.view:BindClick("Btn_Close", "OnClickClose")

    BagView:BindBeforeMove(function()
        UIManager:Close("DescriptionPopup")
        UIManager:Close("ItemOperatePopup")
    end)

    self:Listen(BagModel.EVENT_CHANGED, function(items, id, removed)
        self:OnModelChanged(items, id, removed)
    end)
    self:Listen(BagModel.EVENT_ERROR, function(message)
        Txt_Tip.text = message
    end)
    BagModel:Request()
end

function BagPanel:OnRefresh()
    BagModel:Request()
end

function BagPanel:OnModelChanged(items, id, removed)
    Txt_Tip.text = ""
    if id ~= nil and not removed then
        local item = BagModel:GetById(id)
        if item ~= nil then
            BagView:UpdateCount(id, item.count)
        end
    else
        self:Refresh(items)
    end
end

function BagPanel:Refresh(items)
    -- 删除或整体更新时重排，保留合法的滚动位置。
    BagView:SetDatas(items, true)
end

function BagPanel:OnClickClose()
    UIManager:Close("BagPanel")
end

function BagPanel:OnClose()
    Txt_Tip.text = ""
    BagView:UnBindBeforeMove()
    UIManager:Close("DescriptionPopup")
    UIManager:Close("ItemOperatePopup")
end

return BagPanel
