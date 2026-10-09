-- Model：排行榜的数据层。只存数据、改数据、广播。
-- 不认识 View，也不认识 Presenter，也不知道谁在听。
local EventCenter = require("FrameWork.EventCenter")

local RankModel = {}

-- 事件名常量。订阅方一律用 RankModel.EVENT_CHANGED，
-- 不许在 Presenter 里写字面量 "RankModel.Changed"。
RankModel.EVENT_CHANGED = "RankModel.Changed"

-- 假数据。W3 接网络后由服务器下发，只需把 Request 换成 HTTP 请求、
-- 回包走 SetPlayers，其余代码一行不用改。
-- score 存数值不存字符串：排序要拿它比大小，存成字符串会按字典序比
-- （"900" 会被当成比 "3000" 大）。
-- 这里的顺序是打乱的，名次靠排序算出来，不是写死的。
local MOCK_PLAYERS = {
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
}

-- 当前榜单：按分数从高到低排好的名次表。Get() 返回的就是它。
-- 文件局部变量 = 真私有，只能经下面的方法访问。
local ranked = {}

local function byScoreDesc(a, b)
    return a.score > b.score
end

-- 请求榜单。现在直接喂假数据；W3 换成异步 HTTP，回包后调 SetPlayers。
function RankModel:Request()
    self:SetPlayers(MOCK_PLAYERS)
end

-- 数据入口：本地假数据和服务器回包都走这里。改完数据广播一次。
function RankModel:SetPlayers(players)
    ranked = self:Sort(players)
    EventCenter.Fire(self.EVENT_CHANGED, ranked)
end

-- Presenter 只通过这个口读数据
function RankModel:Get()
    return ranked
end

-- 按分数从高到低排。用副本排，不动传进来的那张表，也不动 MOCK_PLAYERS
-- （同一份源数据被 Request 多次也不能排乱）。
function RankModel:Sort(players)
    local sorted = {}

    for i = 1, #players do
        sorted[i] = players[i]
    end

    table.sort(sorted, byScoreDesc)

    return sorted
end

return RankModel
