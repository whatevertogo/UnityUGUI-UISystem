using System;
using UnityEngine;

namespace Whatevertogo.UISystem
{
    public interface IUIViewFactory
    {
        UIView Create(Type viewType, Transform parent);
        void Release(UIView view);
    }
}
