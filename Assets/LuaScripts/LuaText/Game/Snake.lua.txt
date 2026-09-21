-- 蛇的纯逻辑：位置、朝向、速度、长度。不引用任何 Unity 对象，可以脱离引擎单独跑。
-- 约定：角度统一用弧度，0 表示朝 +X，逆时针为正；位置是连续浮点，不做任何取整。

local Snake = {}
Snake.__index = Snake

local TWO_PI = math.pi * 2

-- 归一化到 (-pi, pi]，用来算「最短转向方向」
local function NormalizeAngle(a)
    a = a % TWO_PI
    if a > math.pi then
        a = a - TWO_PI
    end
    return a
end

function Snake.new(cfg)
    cfg = cfg or {}

    local self = setmetatable({}, Snake)
    self.x = cfg.x or 0
    self.y = cfg.y or 0
    self.angle = cfg.angle or 0
    self.targetAngle = self.angle
    self.speed = cfg.speed or 3.0                -- 单位/秒
    self.maxTurnRate = cfg.maxTurnRate or 3.0    -- 最大转向速度，弧度/秒
    self.spacing = cfg.spacing or 0.22           -- 相邻节点间距
    self.nodeCount = cfg.nodeCount or 20         -- 节点数即长度
    self.radius = cfg.radius or 0.11             -- 节点半径，只给表现层用
    return self
end

-- 外部只需要给目标朝向，蛇自己按转速上限转过去；「连续转向」的连续感就来自这个上限
function Snake:SetTargetAngle(a)
    self.targetAngle = a
end

function Snake:Update(dt)
    -- ① 限转速转向
    local diff = NormalizeAngle(self.targetAngle - self.angle)
    local maxStep = self.maxTurnRate * dt
    if diff > maxStep then
        diff = maxStep
    elseif diff < -maxStep then
        diff = -maxStep
    end
    self.angle = NormalizeAngle(self.angle + diff)

    -- ② 沿当前朝向连续前进
    self.x = self.x + math.cos(self.angle) * self.speed * dt
    self.y = self.y + math.sin(self.angle) * self.speed * dt

    -- 这里故意不写自碰撞：规则是「头撞自己不死」，
    -- 所以头可以自由穿过身体，不截断、不判死、也不需要补救逻辑。
end

function Snake:Grow(n)
    self.nodeCount = self.nodeCount + (n or 5)
end

return Snake
