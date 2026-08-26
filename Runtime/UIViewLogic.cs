using UnityEngine;

namespace Whatevertogo.UISystem
{
    public abstract class UIViewLogic : MonoBehaviour, IUIViewLogic
    {
        protected UIView View { get; private set; }

        public virtual void Bind(UIView view)
        {
            View = view;
        }

        public virtual void OnOpen(UIArguments arguments) { }
        public virtual void OnRefresh(UIArguments arguments) { }
        public virtual void OnCovered() { }
        public virtual void OnResumed() { }
        public virtual void OnClose() { }
    }
}
