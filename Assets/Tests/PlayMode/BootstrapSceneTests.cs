using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PlanEconomicSimulator.Tests
{
    public sealed class BootstrapSceneTests
    {
        [UnityTest]
        public IEnumerator BootstrapSceneAttachesAndLaysOutItsDocument()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            var document = Object.FindFirstObjectByType<UIDocument>();
            Assert.That(document, Is.Not.Null);

            for (var frame = 0; frame < 60 &&
                 (document.rootVisualElement.panel == null || !(document.rootVisualElement.worldBound.width > 0)); frame++)
                yield return null;

            Assert.That(document.rootVisualElement.panel, Is.Not.Null);
            Assert.That(document.rootVisualElement.worldBound.width, Is.GreaterThan(0));
            Assert.That(document.rootVisualElement.Q<Label>("bootstrap-status").visible, Is.True);
        }
    }
}
