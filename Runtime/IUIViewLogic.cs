namespace Whatevertogo.UISystem
{
    public interface IUIViewLogic
    {
        void Bind(UIView view);
        void OnOpen(UIArguments arguments);
        void OnRefresh(UIArguments arguments);
        void OnCovered();
        void OnResumed();
        void OnClose();
    }
}
