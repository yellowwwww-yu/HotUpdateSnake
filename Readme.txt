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


关于精灵资源的 AB 分包：

	建议按资源组，将对应的 SpriteCatalog、SpriteAtlas 和源图片（含 Sprite 切片）放在同一个 AB 包中，便于统一加载、释放和更新。
	例如 ui_items 包含 ItemSpriteCatalog、Items 图集和 Items.png；头像等其他资源组单独分包。
	切片随源图片设置包归属，无需逐个分包；多个系统共用的素材统一放入共享资源组。


关于 SpriteBundleSettings 的字段命名与填写：

	id：自定义资源组标识，非空且在设置中唯一，区分大小写；建议使用小写英文加下划线，如 items。
	bundle：填写真实 AB 包名，与资源的包标签一致，如 ui_items。
	catalog：直接填写对应 SpriteCatalog 的资产名，不带路径和 .asset，如 ItemSpriteCatalog；同一 AB 内的目录资产名保持唯一。
	物品组示例：id = items，bundle = ui_items，catalog = ItemSpriteCatalog。
	调用 GetSprite("items", "Items_0") 时，第一个参数对应组 id，第二个参数对应目录条目的 key；两者均区分大小写。
	设置文件自身保持名为 SpriteBundleSettings.asset，所属 AB 包为 sprite_bundlesettings，与当前加载器中的固定地址对应。
