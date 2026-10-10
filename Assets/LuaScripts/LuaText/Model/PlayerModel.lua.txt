-- Model：玩家资料。只存数据、改数据、广播。
-- 不认识 View，也不认识 Presenter，也不知道谁在听。
local EventCenter = require("FrameWork.EventCenter")

local PlayerModel = {}

-- 事件名常量。订阅方一律用 PlayerModel.EVENT_CHANGED，
-- 不许在 Presenter 里写字面量 "PlayerModel.Changed"。
PlayerModel.EVENT_CHANGED = "PlayerModel.Changed"

-- 本地假数据。11/12 登录接真服务器后，由登录回包调 SetProfile 填充。
-- avatar 存的是头像编号（业务数据），不是资源名——资源名属于表现层，在 Presenter 里映射。
local NICK   = "蛙蛙"
local LEVEL  = 1
local AVATAR = 1

-- 数据入口：改动一律走这里，改完广播一次
function PlayerModel:SetProfile(nick, level, avatar)
    NICK   = nick
    LEVEL  = level
    AVATAR = avatar

    EventCenter.Fire(self.EVENT_CHANGED, self:Get())
end

-- Presenter 只通过这个口读数据
function PlayerModel:Get()
    return { nick = NICK, level = LEVEL, avatar = AVATAR }
end

return PlayerModel
