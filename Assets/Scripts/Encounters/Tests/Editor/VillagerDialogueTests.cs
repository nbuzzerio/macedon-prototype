using Macedon.Villagers;
using Macedon.Characters;
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
        public void FollowingDialogueStateIsIndependentOfWolfCompletionPresentation()
        {
            Assert.That(VillagerDialogueLogic.StateFor(false, false), Is.EqualTo(VillagerDialogueState.Ambient));
            Assert.That(VillagerDialogueLogic.StateFor(true, false), Is.EqualTo(VillagerDialogueState.RecruitReady));
            Assert.That(VillagerDialogueLogic.StateFor(true, true), Is.EqualTo(VillagerDialogueState.Following));
        }

        [Test]
        public void RecruitmentRequiresWolfCompletionAndIsIdempotent()
        {
            var state = new VillagerRecruitmentState();
            Assert.That(state.TryRecruit(false), Is.False);
            Assert.That(state.Status, Is.EqualTo(VillagerRecruitmentStatus.Unrecruited));
            Assert.That(state.TryRecruit(true), Is.True);
            Assert.That(state.Status, Is.EqualTo(VillagerRecruitmentStatus.Following));
            Assert.That(state.TryRecruit(true), Is.False);
            Assert.That(state.Status, Is.EqualTo(VillagerRecruitmentStatus.Following));
            Assert.That(state.LeaveParty(), Is.True);
            Assert.That(state.Status, Is.EqualTo(VillagerRecruitmentStatus.Unrecruited));
        }

        [Test]
        public void PartyRegistrationAssignsDistinctStableSlotsAndSupportsLeaving()
        {
            var registry = new VillagerPartyRegistry<object>();
            var a = new object();
            var b = new object();
            var c = new object();

            Assert.That(registry.Register(a, out int slotA), Is.True);
            Assert.That(registry.Register(b, out int slotB), Is.True);
            Assert.That(registry.Register(c, out int slotC), Is.True);
            Assert.That(new[] { slotA, slotB, slotC }, Is.EquivalentTo(new[] { 0, 1, 2 }));
            Assert.That(registry.Register(a, out int repeatedA), Is.False);
            Assert.That(repeatedA, Is.EqualTo(slotA));
            Assert.That(registry.Count, Is.EqualTo(3));

            Assert.That(registry.Unregister(b), Is.True);
            Assert.That(registry.Count, Is.EqualTo(2));
            var replacement = new object();
            Assert.That(registry.Register(replacement, out int replacementSlot), Is.True);
            Assert.That(replacementSlot, Is.EqualTo(slotB));
        }

        [Test]
        public void FormationSlotsAreDistinctAndRotateWithPlayerYaw()
        {
            Vector3 a = VillagerFormationLogic.LocalOffsetForSlot(0);
            Vector3 b = VillagerFormationLogic.LocalOffsetForSlot(1);
            Vector3 c = VillagerFormationLogic.LocalOffsetForSlot(2);
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(b, Is.Not.EqualTo(c));

            Vector3 player = new Vector3(10f, 2f, 20f);
            Vector3 target = VillagerFormationLogic.WorldTarget(player, 90f, 0);
            Vector3 expected = player + Quaternion.Euler(0f, 90f, 0f) * a;
            Assert.That(Vector3.Distance(target, expected), Is.LessThan(0.0001f));
        }

        [Test]
        public void FollowDecisionUsesHorizontalTargetTolerance()
        {
            Vector3 target = new Vector3(3f, 10f, 4f);
            Assert.That(VillagerFormationLogic.ShouldMove(Vector3.zero, target, 4.9f), Is.True);
            Assert.That(VillagerFormationLogic.ShouldMove(Vector3.zero, target, 5f), Is.False);
            Assert.That(VillagerFormationLogic.ShouldMove(new Vector3(3f, -50f, 4f), target, 0.1f), Is.False);
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
                Assert.That(first.DialogueFor(VillagerDialogueState.Following), Is.EqualTo(new[] { "A following" }));
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
            SetSingleLine(serialized.FindProperty("followingDialogue"), name + " following");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static void SetSingleLine(SerializedProperty property, string value)
        {
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).stringValue = value;
        }

        [Test]
        public void NpcLocomotion_ZeroAndTinyVelocityMapToIdle()
        {
            NpcLocomotionValues zero = NpcLocomotionAnimationLogic.MapSpeed(Vector3.zero, 4f, 6f, 0.05f);
            NpcLocomotionValues tiny = NpcLocomotionAnimationLogic.MapSpeed(new Vector3(0.03f, 5f, 0f), 4f, 6f, 0.05f);
            Assert.That(zero.BlendSpeed, Is.Zero);
            Assert.That(zero.MotionSpeed, Is.Zero);
            Assert.That(tiny.BlendSpeed, Is.Zero);
            Assert.That(tiny.MotionSpeed, Is.Zero);
        }

        [Test]
        public void NpcLocomotion_MovingVelocityMapsToBlendAndNormalizedMotion()
        {
            NpcLocomotionValues values = NpcLocomotionAnimationLogic.MapSpeed(new Vector3(3f, 20f, 0f), 4f, 6f, 0.05f);
            Assert.That(values.BlendSpeed, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(values.MotionSpeed, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void NpcLocomotion_ClampsBlendAndNormalizedMotionSpeed()
        {
            NpcLocomotionValues values = NpcLocomotionAnimationLogic.MapSpeed(new Vector3(10f, 0f, 0f), 4f, 6f, 0.05f);
            Assert.That(values.BlendSpeed, Is.EqualTo(6f));
            Assert.That(values.MotionSpeed, Is.EqualTo(1f));
        }
    }
}
