local EventCenter = require("FrameWork.EventCenter")
local LocalBagFile = CS.LocalBagFile

local BagModel = {}
BagModel.EVENT_CHANGED = "BagModel.Changed"
BagModel.EVENT_ERROR = "BagModel.Error"

-- 物品资料和首次发放模板；运行时不会修改这张表。
local InitialItems = {
    { id = 1, name = "道具1", count = 10, desc = "这是道具1的描述", iconKey = "Items_0" },
    { id = 2, name = "道具2", count = 5, desc = "这是道具2的描述", iconKey = "Items_1" },
    { id = 3, name = "道具3", count = 20, desc = "这是道具3的描述", iconKey = "Items_2" },
    { id = 4, name = "道具4", count = 15, desc = "这是道具4的描述", iconKey = "Items_3" },
    { id = 5, name = "道具5", count = 8, desc = "这是道具5的描述", iconKey = "Items_4" },
    { id = 6, name = "道具6", count = 12, desc = "这是道具6的描述", iconKey = "Items_5" },
    { id = 7, name = "道具7", count = 3, desc = "这是道具7的描述", iconKey = "Items_6" },
    { id = 8, name = "道具8", count = 18, desc = "这是道具8的描述", iconKey = "Items_7" },
    { id = 9, name = "道具9", count = 7, desc = "这是道具9的描述", iconKey = "Items_8" },
    { id = 10, name = "道具10", count = 25, desc = "这是道具10的描述", iconKey = "Items_9" },
    { id = 11, name = "道具11", count = 14, desc = "这是道具11的描述", iconKey = "Items_10" },
    { id = 12, name = "道具12", count = 6, desc = "这是道具12的描述", iconKey = "Items_11" },
    { id = 13, name = "道具13", count = 9, desc = "这是道具13的描述", iconKey = "Items_12" },
    { id = 14, name = "道具14", count = 11, desc = "这是道具14的描述", iconKey = "Items_13" },
    { id = 15, name = "道具15", count = 4, desc = "这是道具15的描述", iconKey = "Items_14" },
    { id = 16, name = "道具16", count = 13, desc = "这是道具16的描述", iconKey = "Items_15" },
    { id = 17, name = "道具17", count = 2, desc = "这是道具17的描述", iconKey = "Items_16" },
    { id = 18, name = "道具18", count = 17, desc = "这是道具18的描述", iconKey = "Items_17" },
    { id = 19, name = "道具19", count = 1, desc = "这是道具19的描述", iconKey = "Items_18" },
    { id = 20, name = "道具20", count = 19, desc = "这是道具20的描述", iconKey = "Items_19" },
    { id = 21, name = "道具21", count = 16, desc = "这是道具21的描述", iconKey = "Items_20" },
    { id = 22, name = "道具22", count = 22, desc = "这是道具22的描述", iconKey = "Items_21" },
    { id = 23, name = "道具23", count = 24, desc = "这是道具23的描述", iconKey = "Items_22" },
    { id = 24, name = "道具24", count = 21, desc = "这是道具24的描述", iconKey = "Items_23" },
    { id = 25, name = "道具25", count = 23, desc = "这是道具25的描述", iconKey = "Items_24" },
}

local Definitions = {}
for _, item in ipairs(InitialItems) do
    Definitions[item.id] = item
end

local Items = {}
local Account

local function CopyItems(source)
    local copied = {}
    local ids = {}
    for _, item in ipairs(source) do
        local definition = Definitions[item.id]
        if definition == nil then
            return nil, "找不到物品定义，ID：" .. tostring(item.id)
        end
        if type(item.count) ~= "number" or item.count <= 0
            or item.count % 1 ~= 0 or item.count > 2147483647 then
            return nil, "物品数量无效，ID：" .. tostring(item.id)
        end
        if ids[item.id] then
            return nil, "物品ID重复：" .. tostring(item.id)
        end
        ids[item.id] = true
        copied[#copied + 1] = {
            id = item.id,
            name = definition.name,
            count = item.count,
            desc = definition.desc,
            iconKey = definition.iconKey
        }
    end
    return copied
end

local function SaveItems(account, items)
    local data = LocalBagFile.Create(account)
    for _, item in ipairs(items) do
        local entry = CS.LocalBagItemData()
        entry.id = item.id
        entry.count = item.count
        data.items:Add(entry)
    end
    return LocalBagFile.Save(data)
end

local function Fail(message)
    EventCenter.Fire(BagModel.EVENT_ERROR, message)
    return false, message
end

local function Commit(source, id, removed)
    if Account == nil then
        return Fail("背包尚未加载，请重新登录")
    end

    local candidate, error = CopyItems(source)
    if candidate == nil then
        return Fail(error)
    end
    error = SaveItems(Account, candidate)
    if error ~= nil then
        return Fail(error)
    end

    -- 文件保存成功后，才提交内存数据并通知界面。
    Items = candidate
    EventCenter.Fire(BagModel.EVENT_CHANGED, Items, id, removed)
    return true
end

function BagModel:Clear()
    Account = nil
    Items = {}
    EventCenter.Fire(self.EVENT_CHANGED, Items)
end

function BagModel:Load(account)
    -- 先清理旧账号，读取失败时不能继续操作之前的背包。
    self:Clear()
    local result = LocalBagFile.Load(account)
    if not result.success then
        return false, result.error
    end

    local source = InitialItems
    if result.data ~= nil then
        source = {}
        for index = 0, result.data.items.Count - 1 do
            local item = result.data.items[index]
            source[#source + 1] = { id = item.id, count = item.count }
        end
    end

    local loaded, error = CopyItems(source)
    if loaded == nil then
        return false, error
    end
    if result.data == nil then
        error = SaveItems(account, loaded)
        if error ~= nil then
            return false, error
        end
    end

    Account = account
    Items = loaded
    EventCenter.Fire(self.EVENT_CHANGED, Items)
    return true
end

function BagModel:Get()
    return Items
end

function BagModel:GetById(id)
    for index, item in ipairs(Items) do
        if item.id == id then
            return item, index
        end
    end
end

function BagModel:SetItems(items)
    return Commit(items or {})
end

function BagModel:Request()
    -- 打开面板只展示已加载的数据，不读盘、不重新发放物品。
    EventCenter.Fire(self.EVENT_CHANGED, Items)
end

local function Consume(id)
    if Account == nil then
        return Fail("背包尚未加载，请重新登录")
    end
    local selected = BagModel:GetById(id)
    if selected == nil or selected.count <= 0 then
        return Fail("物品不存在或数量不足")
    end

    local candidate = {}
    for _, item in ipairs(Items) do
        local count = item.count
        if item.id == id then
            count = count - 1
        end
        if count > 0 then
            candidate[#candidate + 1] = { id = item.id, count = count }
        end
    end
    return Commit(candidate, id, selected.count == 1)
end

function BagModel:Use(id)
    return Consume(id)
end

function BagModel:Sell(id)
    return Consume(id)
end

return BagModel
