-- 演示：一条蛇 + 鼠标转向。只做三件事：建蛇、把输入转成目标朝向、每帧推状态给表现层。

local Snake = require('Snake')

local snake

function Snake_Start()
    snake = Snake.new({
        x = 0, y = 0,
        angle = 0,
        speed = 3.0,          -- 每秒走 3 个单位
        maxTurnRate = 3.5,    -- 每秒最多转 3.5 弧度（约 201°），调大就接近瞬时转向
        spacing = 0.5,
        nodeCount = 20,
        radius = 0.11,
    })

    -- SnakeView 是 C# 侧注入的表现层。
    -- 参数顺序必须和 SnakeView.SetState(x, y, spacing, nodeCount, angle) 一一对应。
    SnakeView:SetState(snake.x, snake.y, snake.spacing, snake.nodeCount, snake.angle)
end

function Snake_Input(angle)
    if snake then
        snake:SetTargetAngle(angle)
    end
end

function Snake_Update(dt)
    if not snake then
        return
    end

    snake:Update(dt)
    SnakeView:SetState(snake.x, snake.y, snake.spacing, snake.nodeCount, snake.angle)
end
