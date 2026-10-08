local PanelBase = require("UI.PanelBase");

local RankPanel = PanelBase:Create();

-- 前三名的名次颜色
local RANK_COLOR = { "BA7517", "5F5E5A", "993C1D" };

-- 玩家表，排行榜唯一的数据源。
-- score 存数值不存字符串：排序要拿它比大小，存成字符串会按字典序比
-- （"900" 会被当成比 "3000" 大）。
-- 这里的顺序是打乱的，名次靠排序算出来，不是写死的。
-- W3 接网络后这张表换成服务器下发的数据，其余代码一行不用改。
local PLAYERS = {
    { name = "李四",   score = 3210 },
    { name = "张三",   score = 2860 },
    { name = "王五",   score = 2640 },
    { name = "赵六",   score = 2490 },
    { name = "钱七",   score = 2050 },
    { name = "孙八",   score = 1780 },
    { name = "周九",   score = 1520 },
    { name = "吴十",   score = 1330 },
    { name = "郑一鸣", score = 1180 },
    { name = "冯晓雨", score = 970 },
    { name = "陈嘉树", score = 860 },
    { name = "褚南风", score = 740 },
    { name = "卫东阳", score = 620 },
    { name = "蒋川",   score = 510 },
    { name = "沈舟",   score = 380 },
};

function RankPanel:OnOpen(data)
    self.view:BindClick("Btn_Close", "OnClickClose");
    self:RefreshData();
end

-- 面板还开着时被再 Open 一次走这条，不重刷就会看到上一次的榜单
function RankPanel:OnRefresh(data)
    self:RefreshData();
end

function RankPanel:OnClickClose()
    UIManager:Close("RankPanel");
end

-- 建玩家表 + 交给列表，OnOpen / OnRefresh 共用这一条路径
function RankPanel:RefreshData()
    self.players = self:BuildPlayers();

    local list = self.view:Get("List");

    if list == nil then
        return;
    end

    local scroll = list:GetComponent("InfiniteScroll");

    if scroll == nil then
        print("[Rank] List 节点上没有挂 InfiniteScroll");
        return;
    end

    scroll:SetDatas(self:ToViewDatas());
end

-- 按分数从高到低排出名次，结果存到面板上由自己持有。
-- 用副本排，不动模块级那张表
function RankPanel:BuildPlayers()
    local sorted = {};

    for i = 1, #PLAYERS do
        sorted[i] = PLAYERS[i];
    end

    table.sort(sorted, function(a, b)
        return a.score > b.score;
    end);

    return sorted;
end

-- 玩家表 -> 列表要的一行三列：名次、名字、分数。
-- 只有这一步做数值到文本的转换，C# 那侧拿到的全是字符串
function RankPanel:ToViewDatas()
    local datas = {};

    for i = 1, #self.players do
        local player = self.players[i];

        datas[i] = {
            self:RankText(i),
            player.name,
            tostring(player.score),
        };
    end

    return datas;
end

function RankPanel:RankText(rank)
    local color = RANK_COLOR[rank];

    if color == nil then
        return tostring(rank);
    end

    return string.format("<color=#%s>%d</color>", color, rank);
end

return RankPanel;
