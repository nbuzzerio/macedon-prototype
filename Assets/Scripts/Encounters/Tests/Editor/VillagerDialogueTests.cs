using Macedon.Villagers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Macedon.Villagers.Tests
{
    public sealed class VillagerDialogueTests
    {
        [TestCase(false, VillagerDialogueState.Ambient)]
        [TestCase(true, VillagerDialogueState.RecruitReady)]
        public void StateTracksWolfCompletion(bool completed, VillagerDialogueState expected)
        {
            Assert.That(VillagerDialogueLogic.StateForWolfCompletion(completed), Is.EqualTo(expected));
        }

        [Test]
        public void DialogueSelectionCyclesWithoutImmediateRepeat()
        {
            Assert.That(VillagerDialogueLogic.NextDialogueIndex(3, -1), Is.EqualTo(0));
            Assert.That(VillagerDialogueLogic.NextDialogueIndex(3, 0), Is.EqualTo(1));
            Assert.That(VillagerDialogueLogic.NextDialogueIndex(3, 1), Is.EqualTo(2));
            Assert.That(VillagerDialogueLogic.NextDialogueIndex(3, 2), Is.EqualTo(0));
            Assert.That(VillagerDialogueLogic.NextDialogueIndex(0, -1), Is.EqualTo(-1));
        }

        [Test]
        public void ProfilesKeepAuthoredDialogueIndependent()
        {
            VillagerProfile first = CreateProfile("A", "A ambient", "A ready");
            VillagerProfile second = CreateProfile("B", "B ambient", "B ready");
            try
            {
                Assert.That(first.DialogueFor(VillagerDialogueState.Ambient), Is.EqualTo(new[] { "A ambient" }));
                Assert.That(first.DialogueFor(VillagerDialogueState.RecruitReady), Is.EqualTo(new[] { "A ready" }));
                Assert.That(second.DialogueFor(VillagerDialogueState.Ambient), Is.EqualTo(new[] { "B ambient" }));
                Assert.That(second.DialogueFor(VillagerDialogueState.RecruitReady), Is.EqualTo(new[] { "B ready" }));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        private static VillagerProfile CreateProfile(string name, string ambient, string ready)
        {
            VillagerProfile profile = ScriptableObject.CreateInstance<VillagerProfile>();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = name;
            SetSingleLine(serialized.FindProperty("ambientDialogue"), ambient);
            SetSingleLine(serialized.FindProperty("recruitReadyDialogue"), ready);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static void SetSingleLine(SerializedProperty property, string value)
        {
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).stringValue = value;
        }
    }
}
