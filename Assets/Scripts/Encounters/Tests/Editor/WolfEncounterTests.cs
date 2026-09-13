using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Macedon.Encounters.Tests
{
    public sealed class WolfEncounterTests
    {
        [Test]
        public void StateTransitionsOnlyInOrder()
        {
            var state = new WolfEncounterState();
            Assert.That(state.Complete(), Is.False);
            Assert.That(state.Begin(), Is.True);
            Assert.That(state.Phase, Is.EqualTo(WolfEncounterPhase.Active));
            Assert.That(state.Complete(), Is.True);
            Assert.That(state.Phase, Is.EqualTo(WolfEncounterPhase.Completed));
        }

        [Test]
        public void CompletionEventFiresOnce()
        {
            var state = new WolfEncounterState(); int completed = 0;
            state.Changed += phase => { if (phase == WolfEncounterPhase.Completed) completed++; };
            state.Begin(); state.Complete(); state.Complete();
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void BoundaryTracksStateAndEnablesConfiguredBlockers()
        {
            var owner = new GameObject("Boundary"); var blockerObject = new GameObject("Blocker");
            try
            {
                var boundary = owner.AddComponent<EncounterBoundary>();
                var blocker = blockerObject.AddComponent<BoxCollider>(); blocker.enabled = false;
                var serialized = new SerializedObject(boundary);
                SerializedProperty array = serialized.FindProperty("blockers"); array.arraySize = 1;
                array.GetArrayElementAtIndex(0).objectReferenceValue = blocker; serialized.ApplyModifiedPropertiesWithoutUndo();
                boundary.SetEncounterActive(true);
                Assert.That(boundary.IsBlocking, Is.True); Assert.That(blocker.enabled, Is.True);
                boundary.SetEncounterActive(false);
                Assert.That(boundary.IsBlocking, Is.False); Assert.That(blocker.enabled, Is.False);
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(blockerObject); }
        }

        [Test]
        public void CameraShakeEnvelopeEndsAtZeroAndOffsetIsDeterministic()
        {
            Assert.That(CameraShakeMath.Envelope(0f, 1f), Is.EqualTo(1f));
            Assert.That(CameraShakeMath.Envelope(1f, 1f), Is.Zero);
            Vector3 first = CameraShakeMath.Offset(0.25f, 1f, 2f, 20f, 7f);
            Assert.That(CameraShakeMath.Offset(0.25f, 1f, 2f, 20f, 7f), Is.EqualTo(first));
            Assert.That(CameraShakeMath.Offset(1f, 1f, 2f, 20f, 7f), Is.EqualTo(Vector3.zero));
        }
    }
}
