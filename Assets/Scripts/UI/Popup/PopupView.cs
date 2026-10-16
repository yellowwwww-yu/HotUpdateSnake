using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using XLua;

namespace Framework.UI
{
    [CSharpCallLua]
    public delegate void UpdateCallBack();

    [LuaCallCSharp]
    [RequireComponent(typeof(UIPanel))]
    public class PopupView : MonoBehaviour
    {
        private UpdateCallBack callBack;
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        public void BindUpdateCallBack(UpdateCallBack callBack)
        {
            this.callBack = callBack;
        }

        public void UnBindUpdateCallBack()
        {
            callBack = null;
        }

        // 只查询最上层命中对象，不发送或拦截点击事件。
        public GameObject GetPointerTarget()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return null;

            var pointer = new PointerEventData(eventSystem);
            pointer.position = Input.mousePosition;

            raycastResults.Clear();
            eventSystem.RaycastAll(pointer, raycastResults);

            GameObject target = raycastResults.Count > 0
                ? raycastResults[0].gameObject
                : null;
            raycastResults.Clear();
            return target;
        }

        private void Update()
        {
            callBack?.Invoke();
        }

        private void OnDisable()
        {
            UnBindUpdateCallBack();
        }
        private void OnDestroy()
        {
            UnBindUpdateCallBack();
        }
    }
}