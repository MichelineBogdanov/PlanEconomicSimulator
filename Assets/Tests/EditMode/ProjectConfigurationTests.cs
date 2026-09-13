using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanEconomicSimulator.Tests
{
    public sealed class ProjectConfigurationTests
    {
        [Test]
        public void EditorAndWindowsBackendMatchTheSupportedToolchain()
        {
            Assert.That(Application.unityVersion, Is.EqualTo("6000.6.0f1"));
            Assert.That(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone), Is.EqualTo(ScriptingImplementation.Mono2x));
            Assert.That(PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone), Is.EqualTo(ApiCompatibilityLevel.NET_Standard));
        }

        [Test]
        public void PlanningOfficeDocumentImportsWithItsStatusLabel()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Game/UI/PlanningOffice.uxml");
            Assert.That(template, Is.Not.Null, "The UXML importer must resolve the document and its stylesheet.");
            var root = template.CloneTree();
            Assert.That(root.Q<Label>("bootstrap-status"), Is.Not.Null);
        }
    }
}
