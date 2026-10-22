using Framework.Audio;
using Framework.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XLua;

public class BagCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField]private Image Img_Bg;
    [SerializeField]private Image Img_Item;
    [SerializeField]private Text Txt_Num;

    private int itemId;
    private bool hovering;
    private string itemName;
    private string desc;

    [SerializeField]private Vector2 descOffset = new Vector2(0,0);

    public void Init(Sprite bg, Sprite item, int count, string itemName, string desc)
    {
        Img_Bg.sprite = bg;
        SetImg_Item(item);
        Txt_Num.text = count.ToString();
        this.itemName = itemName;
        this.desc = desc;
    }

    public void SetImg_Bg(Sprite sprite)
    {
        Img_Bg.sprite = sprite;
    }

    public void SetImg_Item(Sprite sprite)
    {
        Img_Item.sprite = sprite;
        Img_Item.preserveAspect = true;
        Img_Item.enabled = sprite != null;
    }

    public void SetTxt_Num(int count)
    {
        Txt_Num.text = count.ToString();
    }

    public void SetId(int id)
    {
        itemId = id;
    }

    public void SetName(string itemName)
    {
        this.itemName = itemName;
    }

    public void SetDesc(string desc)
    {
        this.desc = desc;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemId <= 0) return;
        hovering = true;
        if (UIManager.Instance.IsOpen("ItemOperatePopup")) return;
        using(LuaTable data = LuaManager.Instance.NewLuaTable())
        {
            data.Set("name",itemName);
            data.Set("desc",desc);

            UIManager.Instance.Open("DescriptionPopup",data);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        UIManager.Instance.Close("DescriptionPopup");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || itemId <= 0) return;
        using (LuaTable data = LuaManager.Instance.NewLuaTable())
        {
            data.Set("id", itemId);
            data.Set("rect", (RectTransform)transform);
            UIManager.Instance.Open("ItemOperatePopup", data);
            AudioClip clickClip = AudioBundleLoader.Instance.GetClip("common", "ui_button_click");
            AudioManager.Instance.PlaySound(clickClip);
        }
    }

    private void OnDisable()
    {
        if (hovering && UIManager.Instance != null)
        {
            UIManager.Instance.Close("DescriptionPopup");
        }
        hovering = false;
        itemId = 0;
        if (Img_Item != null) SetImg_Item(null);
    }
}
