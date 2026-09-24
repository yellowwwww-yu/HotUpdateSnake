local EventCenter = require("FrameWork.EventCenter")

local PanelBase = {}
PanelBase.__index = PanelBase

function PanelBase:Create()
    local panel = {}
    setmetatable(panel, PanelBase)
    panel.view = nil
    panel.events = {}
    return panel
end

function PanelBase:Listen(name, fn)
    self.events[#self.events + 1] = {name,fn}
    EventCenter.On(name, fn)
end

function PanelBase:ReleaseEvents()
    for _, event in ipairs(self.events) do
        EventCenter.Off(event[1], event[2])
    end
    self.events = {}
end

function PanelBase:OnOpen(data) end
function PanelBase:OnRefresh(data) end
function PanelBase:OnClose() end

return PanelBase