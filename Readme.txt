关于lua脚本的命名规则：
	由于lua脚本被打包到AB包后有两种方式加载到文件，一种是用全路径加载，一种是用文件夹名加载
	方案一：保留文件夹结构，用全路径去加载，但需要保证路径名准确
	方案二：直接平铺所有lua文件，用文件名加载，但要保证每个文件名称唯一，也就是将文件夹名作为文件名前缀

	当前采用方案一，使用案例：require("FrameWork.EventCenter");


关于为什么用 MVP 而不是 MVC：

	View 是 Unity 组件，生命周期在 C# 手里，Lua 无法拥有它 —— 所以做不到 MVC 那样让
	View 直接观察 Model。MVP 让 Presenter 在中间转一道，贴合"C# 管物、Lua 管事"，C# 零改动。

M / V / P 分别是什么：

	Model（数据）     Lua，LuaScripts/Lua/Model/
		只存数据、改数据、广播。不认识 UI，也不知道谁在听。

	View（视图）      C#，FrameWork/UIManager/UIBind.cs + UIPanel.cs
		只持有控件、搬运点击。不含业务判断，不认识 Model。

	Presenter（表现） Lua，LuaScripts/Lua/UI/*.lua，继承 PanelBase
		唯一同时认识 View 和 Model 的地方。

	数据流：View →(点击) Presenter →(读写) Model →(广播) Presenter →(刷新) View
			View 与 Model 之间没有引用。
