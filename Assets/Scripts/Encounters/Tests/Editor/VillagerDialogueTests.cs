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
            Assert.That(state.Status, Is.EqualTo(VillagerRecruitmentStatus.ReturningHome));
            Assert.That(state.ArriveHome(), Is.True);
            Assert.That(state.Status, Is.EqualTo(VillagerRecruitmentStatus.RejoinReady));
            Assert.That(state.TryRecruit(true), Is.True);
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

        [Test]
        public void NpcLocomotionHierarchyValidation_AcceptsOnlySelfOrDescendants()
        {
            var root = new GameObject("VillagerRoot");
            var child = new GameObject("Visual");
            var unrelated = new GameObject("OtherVillagerVisual");
            child.transform.SetParent(root.transform);

            try
            {
                Assert.That(NpcLocomotionAnimationLogic.IsInHierarchy(root.transform, root.transform), Is.True);
                Assert.That(NpcLocomotionAnimationLogic.IsInHierarchy(root.transform, child.transform), Is.True);
                Assert.That(NpcLocomotionAnimationLogic.IsInHierarchy(root.transform, unrelated.transform), Is.False);
                Assert.That(NpcLocomotionAnimationLogic.IsInHierarchy(null, child.transform), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(unrelated);
            }
        }

        [Test]
        public void AuthoredJumpInterpolation_HitsEndpointsAndRaisesArc()
        {
            Vector3 start = new Vector3(1f, 2f, 3f);
            Vector3 end = new Vector3(5f, 2f, 7f);
            Assert.That(AuthoredTraversalLogic.JumpPosition(start, end, 0f, 1f), Is.EqualTo(start));
            Assert.That(AuthoredTraversalLogic.JumpPosition(start, end, 1f, 1f), Is.EqualTo(end));
            Assert.That(AuthoredTraversalLogic.JumpPosition(start, end, 0.5f, 1f).y, Is.GreaterThan(2f));
        }

        [Test]
        public void AuthoredTravelRotation_FacesHorizontalJumpDirection()
        {
            Quaternion rotation = AuthoredTraversalLogic.TravelRotation(
                new Vector3(2f, 1f, 3f),
                new Vector3(6f, 5f, 3f),
                Quaternion.Euler(0f, 180f, 0f));

            Assert.That(Vector3.Angle(rotation * Vector3.forward, Vector3.right), Is.LessThan(0.01f));
            Assert.That(Vector3.Angle(rotation * Vector3.up, Vector3.up), Is.LessThan(0.01f));
        }

        [Test]
        public void AuthoredTravelRotation_KeepsFallbackWithoutHorizontalTravel()
        {
            Quaternion fallback = Quaternion.Euler(0f, 37f, 0f);
            Quaternion rotation = AuthoredTraversalLogic.TravelRotation(Vector3.zero, Vector3.up * 2f, fallback);

            Assert.That(Quaternion.Angle(rotation, fallback), Is.LessThan(0.01f));
        }

        [Test]
        public void AuthoredEntryArrival_UsesSampledDestinationNotAuthoredPoint()
        {
            Vector3 authored = Vector3.zero;
            Vector3 sampled = new Vector3(1f, 0f, 0f);
            Vector3 follower = new Vector3(1.1f, 0f, 0f);

            Assert.That(AuthoredTraversalLogic.HasArrived(follower, sampled, 0.45f), Is.True);
            Assert.That(AuthoredTraversalLogic.HasArrived(follower, authored, 0.45f), Is.False);
            Assert.That(AuthoredTraversalLogic.HasArrived(follower, sampled, -1f), Is.False);
        }

        [Test]
        public void AuthoredTraversalPointOrder_ReversesByCrossingSide()
        {
            Assert.That(AuthoredTraversalLogic.PointOrder(4, TraversalSide.A), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(AuthoredTraversalLogic.PointOrder(4, TraversalSide.B), Is.EqualTo(new[] { 3, 2, 1, 0 }));
        }

        [Test]
        public void MovementOwnership_TraversalReleasesOnlyWhenMovementIsReady()
        {
            var ownership = new VillagerMovementOwnership();
            Assert.That(ownership.Mode, Is.EqualTo(VillagerMovementMode.FormationFollowing));
            Assert.That(ownership.TryBeginTraversal(), Is.True);
            Assert.That(ownership.TryBeginTraversal(), Is.False);
            Assert.That(ownership.Mode, Is.EqualTo(VillagerMovementMode.Traversal));
            Assert.That(ownership.TryResumeFormation(false), Is.False);
            Assert.That(ownership.Mode, Is.EqualTo(VillagerMovementMode.Traversal));
            Assert.That(ownership.TryResumeFormation(true), Is.True);
            Assert.That(ownership.Mode, Is.EqualTo(VillagerMovementMode.FormationFollowing));
        }

        [Test]
        public void TraversalZones_ProgressIdleToStagingToCrossingToRunning()
        {
            var state = new AuthoredTraversalState();
            Assert.That(state.Enter(TraversalZone.A), Is.True);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Staging));
            Assert.That(state.OwnsFollowers, Is.True);
            state.Exit(TraversalZone.A);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Staging));
            Assert.That(state.OwnsFollowers, Is.True);
            Assert.That(state.Enter(TraversalZone.A), Is.False);
            Assert.That(state.Enter(TraversalZone.B), Is.True);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Crossing));
            Assert.That(state.OwnsFollowers, Is.True);
            state.Exit(TraversalZone.B);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Crossing));
            Assert.That(state.OwnsFollowers, Is.True);
            Assert.That(state.Enter(TraversalZone.C), Is.True);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Running));
            Assert.That(state.OwnsFollowers, Is.True);
            Assert.That(state.CanStartNextFollower, Is.True);
        }

        [Test]
        public void TraversalZones_CancellationPreventsAnotherFollowerStarting()
        {
            var state = new AuthoredTraversalState();
            state.Enter(TraversalZone.A);
            state.Enter(TraversalZone.B);
            state.Enter(TraversalZone.C);
            Assert.That(state.Enter(TraversalZone.B), Is.True);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Cancelling));
            Assert.That(state.CanStartNextFollower, Is.False);
        }

        [Test]
        public void TraversalState_CompletionReturnsIdleUnlessRecoveryIsPending()
        {
            var state = new AuthoredTraversalState();
            state.Enter(TraversalZone.A);
            state.Enter(TraversalZone.B);
            state.Enter(TraversalZone.C);
            state.Complete(false);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Idle));
            Assert.That(state.OwnsFollowers, Is.False);
            state.Enter(TraversalZone.A);
            state.Complete(true);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.RecoveryPending));
            Assert.That(state.OwnsFollowers, Is.True);
        }

        [Test]
        public void TraversalState_NewAndRuntimeResetAreIdleWithoutOwnership()
        {
            var state = new AuthoredTraversalState();
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Idle));
            Assert.That(state.OwnsFollowers, Is.False);
            Assert.That(state.Enter(TraversalZone.A), Is.True);
            Assert.That(state.OwnsFollowers, Is.True);

            state.ResetForRuntimeSession();

            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Idle));
            Assert.That(state.OwnsFollowers, Is.False);
            Assert.That(state.Enter(TraversalZone.A), Is.True);
            Assert.That(state.Phase, Is.EqualTo(AuthoredTraversalPhase.Staging));
        }

        [Test]
        public void RouteDeviation_WarnsInOrderAndAbandonsOnce()
        {
            var state = new RouteDeviationState();
            Assert.That(state.Update(true, false, 5f, 5f, 8f, 10f), Is.EqualTo(RouteDeviationStage.Warning1));
            Assert.That(state.Update(true, false, 8f, 5f, 8f, 10f), Is.EqualTo(RouteDeviationStage.Warning2));
            Assert.That(state.Update(true, false, 10f, 5f, 8f, 10f), Is.EqualTo(RouteDeviationStage.Abandon));
            Assert.That(state.Update(true, false, 1f, 5f, 8f, 10f), Is.Not.EqualTo(RouteDeviationStage.Abandon));
        }

        [Test]
        public void RouteDeviation_OnRouteResetsEpisode()
        {
            var state = new RouteDeviationState();
            state.Update(true, false, 4f, 5f, 8f, 10f);
            Assert.That(state.Update(true, true, 1f, 5f, 8f, 10f), Is.EqualTo(RouteDeviationStage.OnRoute));
            Assert.That(state.Elapsed, Is.Zero);
            Assert.That(state.Update(true, false, 1f, 5f, 8f, 10f), Is.EqualTo(RouteDeviationStage.WaitingForWarning1));
        }
    }
}
