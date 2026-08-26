using System;
using NUnit.Framework;
using UnityEngine;

namespace Whatevertogo.UISystem.Tests
{
    public class TestNormalViewA : UIView
    {
        public int CoveredCount { get; private set; }
        public int ResumedCount { get; private set; }
        public int RefreshedCount { get; private set; }

        protected override void OnCovered() => CoveredCount++;
        protected override void OnResumed() => ResumedCount++;
        protected override void OnRefreshed(UIArguments arguments) => RefreshedCount++;
    }

    public class TestNormalViewB : UIView
    {
    }

    public class TestExclusiveView : UIView
    {
        public override bool Exclusive => true;
    }

    public sealed class UIManagerTests
    {
        private sealed class TestFactory : IUIViewFactory
        {
            public UIView Create(Type viewType, Transform parent)
            {
                var instance = new GameObject(viewType.Name);
                instance.transform.SetParent(parent, false);
                return (UIView)instance.AddComponent(viewType);
            }

            public void Release(UIView view)
            {
                UnityEngine.Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void ManagerMaintainsCoverResumeRefreshExclusiveAndBackSemantics()
        {
            var managerObject = new GameObject("UI Manager");
            managerObject.SetActive(false);
            var manager = managerObject.AddComponent<UIManager>();
            manager.SetFactory(new TestFactory());
            managerObject.SetActive(true);

            var first = manager.Open<TestNormalViewA>();
            manager.Open<TestNormalViewB>();
            Assert.That(first.CoveredCount, Is.EqualTo(1));
            Assert.That(manager.OpenViewCount, Is.EqualTo(2));

            Assert.That(manager.HandleBack(), Is.True);
            Assert.That(first.ResumedCount, Is.EqualTo(1));
            Assert.That(manager.IsOpen<TestNormalViewB>(), Is.False);

            Assert.That(manager.Open<TestNormalViewA>(), Is.SameAs(first));
            Assert.That(first.RefreshedCount, Is.EqualTo(1));

            manager.Open<TestExclusiveView>();
            Assert.That(manager.IsOpen<TestNormalViewA>(), Is.False);
            Assert.That(manager.IsOpen<TestExclusiveView>(), Is.True);
            Assert.That(manager.OpenViewCount, Is.EqualTo(1));

            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }
}
