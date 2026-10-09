-- Presenter：唯一同时认识 View 和 Model 的地方。
-- 控件名只出现在这里；业务数据只放 Model。
local PanelBase = require("UI.PanelBase")
local RankModel = require("Model.RankModel")

local RankPanel = PanelBase:Create()

-- 前三名的名次颜色。这是表现层的事（<color> 是富文本），不进 Model
local RANK_COLOR = { "BA7517", "5F5E5A", "993C1D" }

-- UIManager 打开面板 → UIPanel.CallOpen → 到这里
function RankPanel:OnOpen(data)
    -- Presenter → View：绑定控件
    self.view:BindClick("Btn_Close", "OnClickClose")

    -- Model → Presenter：订阅。
    -- EventCenter 广播时只传数据、不传 self（内部是 pcall(callback, data)），
    -- 所以这里用闭包把面板带进去，回调里的 self 才是对的。
    -- 退订由 UIPanel.CallClose → OnCloseInternal → ReleaseEvents 兜底。
    self:Listen(RankModel.EVENT_CHANGED, function()
        self:OnModelChanged()
    end)

    -- Presenter → Model：请求数据，数据回来走 OnModelChanged
    RankModel:Request()
end

-- 面板还开着时被再 Open 一次走这条，重新拉一次榜单
function RankPanel:OnRefresh(data)
    RankModel:Request()
end

-- View → Presenter 的入口（方法名必须和 BindClick 第二参一致）
function RankPanel:OnClickClose()
    UIManager:Close("RankPanel")
end

-- Model → Presenter 的入口
function RankPanel:OnModelChanged()
    self:Refresh()
end

-- Presenter → View：把 Model 的榜单搬到列表上
function RankPanel:Refresh()
    local list = self.view:Get("List")

    if list == nil then
        return
    end

    local scroll = list:GetComponent("InfiniteScroll")

    if scroll == nil then
        print("[Rank] List 节点上没有挂 InfiniteScroll")
        return
    end

    scroll:SetDatas(self:ToViewDatas(RankModel:Get()))
end

-- 榜单 -> 列表要的一行三列：名次、名字、分数。
-- 只有这一步做数值到文本的转换，C# 那侧拿到的全是字符串。
-- 数据本身在 Model，这里只做搬运和样式
function RankPanel:ToViewDatas(players)
    local datas = {}

    for i = 1, #players do
        local player = players[i]

        datas[i] = {
            self:RankText(i),
            player.name,
            tostring(player.score),
        }
    end

    return datas
end

function RankPanel:RankText(rank)
    local color = RANK_COLOR[rank]

    if color == nil then
        return tostring(rank)
    end

    return string.format("<color=#%s>%d</color>", color, rank)
end

function RankPanel:OnClose()
    -- 订阅已由 ReleaseEvents 统一退掉，这里没有额外要清的
end

return RankPanel
