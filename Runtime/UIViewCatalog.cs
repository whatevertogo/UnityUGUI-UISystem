using System.Collections.Generic;
using UnityEngine;

namespace Whatevertogo.UISystem
{
    [CreateAssetMenu(fileName = "UIViewCatalog", menuName = "UI/View Catalog")]
    public sealed class UIViewCatalog : ScriptableObject
    {
        [SerializeField] private List<UIView> prefabs = new List<UIView>();

        public IUIViewFactory CreateFactory()
        {
            return new PrefabUIViewFactory(prefabs);
        }
    }
}
