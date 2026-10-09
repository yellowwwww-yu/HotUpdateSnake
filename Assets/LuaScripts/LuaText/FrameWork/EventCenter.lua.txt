local EventCenter = {}
local Liseners = {}

function EventCenter.On(name, callback)
    if not name or type(callback) ~= "function" then
        return
    end

    local set = Liseners[name]
    if not set then
        set = {}
        Liseners[name] = set
    end
    set[callback] = callback    
end

function EventCenter.Off(name, callback)
    if not name or type(callback) ~= "function" then
        return
    end

    local set = Liseners[name]
    if not set then
        return
    end

    set[callback] = nil
    
    if next(set) == nil then
        Liseners[name] = nil
    end
end

function EventCenter.Fire(name, ...)
    if not name then
        return
    end

    local set = Liseners[name]

    if not set then
        return
    end

    local snapshot = {}
    for k, v in pairs(set) do
        snapshot[k] = v
    end

    for _, callback in pairs(snapshot) do
        local ok, err = pcall(callback, ...)
        if not ok then
            print("EventCenter.Fire error: " .. err)
        end
    end
end

return EventCenter