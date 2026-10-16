local EventCenter = require("FrameWork.EventCenter")

local BagModel = {}

BagModel.EVENT_CHANGED = "BagModel.Changed"

local Mock_Items = {
    { id = 1, name = "道具1", count = 10, desc = "这是道具1的描述" },
    { id = 2, name = "道具2", count = 5, desc = "这是道具2的描述" },
    { id = 3, name = "道具3", count = 20, desc = "这是道具3的描述" },
    { id = 4, name = "道具4", count = 15, desc = "这是道具4的描述" },
    { id = 5, name = "道具5", count = 8, desc = "这是道具5的描述" },
    { id = 6, name = "道具6", count = 12, desc = "这是道具6的描述" },
    { id = 7, name = "道具7", count = 3, desc = "这是道具7的描述" },
    { id = 8, name = "道具8", count = 18, desc = "这是道具8的描述" },
    { id = 9, name = "道具9", count = 7, desc = "这是道具9的描述" },
    { id = 10, name = "道具10", count = 25, desc = "这是道具10的描述" },
    { id = 11, name = "道具11", count = 14, desc = "这是道具11的描述" },
    { id = 12, name = "道具12", count = 6, desc = "这是道具12的描述" },
    { id = 13, name = "道具13", count = 9, desc = "这是道具13的描述" },
    { id = 14, name = "道具14", count = 11, desc = "这是道具14的描述" },
    { id = 15, name = "道具15", count = 4, desc = "这是道具15的描述" },
    { id = 16, name = "道具16", count = 13, desc = "这是道具16的描述" },
    { id = 17, name = "道具17", count = 2, desc = "这是道具17的描述" },
    { id = 18, name = "道具18", count = 17, desc = "这是道具18的描述" },
    { id = 19, name = "道具19", count = 1, desc = "这是道具19的描述" },
    { id = 20, name = "道具20", count = 19, desc = "这是道具20的描述" },
    { id = 21, name = "道具21", count = 16, desc = "这是道具21的描述" },
    { id = 22, name = "道具22", count = 22, desc = "这是道具22的描述" },
    { id = 23, name = "道具23", count = 24, desc = "这是道具23的描述" },
    { id = 24, name = "道具24", count = 21, desc = "这是道具24的描述" },
    { id = 25, name = "道具25", count = 23, desc = "这是道具25的描述" },
}

function BagModel:Get()
    return Mock_Items
end

function BagModel:GetById(id)
    for index, item in ipairs(Mock_Items) do
        if item.id == id then
            return item, index
        end
    end
end

function BagModel:SetItems(items)
    Mock_Items = items or {}
    EventCenter.Fire(self.EVENT_CHANGED, Mock_Items)
end

function BagModel:Request()
    self:SetItems(Mock_Items)
end

local function Consume(id)
    local item, index = BagModel:GetById(id)
    if item == nil or item.count <= 0 then
        return false
    end

    item.count = item.count - 1
    local removed = item.count == 0
    if removed then
        table.remove(Mock_Items, index)
    end
    EventCenter.Fire(BagModel.EVENT_CHANGED, Mock_Items, id, removed)
    return true
end

function BagModel:Use(id)
    return Consume(id)
end

function BagModel:Sell(id)
    return Consume(id)
end

return BagModel
