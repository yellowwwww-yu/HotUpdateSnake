function Game_Start()
    print("游戏开始")
end

local Input = CS.UnityEngine.Input
local KeyCode = CS.UnityEngine.KeyCode

function Game_Update()
    if UIManager == nil then return end

    if Input.GetKeyDown(KeyCode.Escape) and not UIManager:IsOpen("ExitPanel") then
        UIManager:Open("ExitPanel")
    end
end
