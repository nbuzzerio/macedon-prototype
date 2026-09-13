using NUnit.Framework;
using UnityEngine;

namespace Macedon.PlayerTraversal.Tests
{
    public sealed class SteepSlopeRulesTests
    {
        [TestCase(0f, true)]
        [TestCase(30f, true)]
        [TestCase(50f, true)]
        [TestCase(50.01f, false)]
        [TestCase(75f, false)]
        public void ClassifiesSlopeAtInclusiveThreshold(float angle, bool expected)
        {
            Vector3 normal = Quaternion.Euler(angle, 0f, 0f) * Vector3.up;
            Assert.That(SteepSlopeRules.IsTraversable(normal, 50f), Is.EqualTo(expected));
        }

        [Test]
        public void JumpRequiresGroundAndAllowsLegalSlope()
        {
            Vector3 normal = Quaternion.Euler(35f, 0f, 0f) * Vector3.up;
            Assert.That(SteepSlopeRules.CanInitiateJump(true, true, normal, 50f), Is.True);
            Assert.That(SteepSlopeRules.CanInitiateJump(false, true, normal, 50f), Is.False);
        }

        [Test]
        public void JumpIsDeniedOnSevereSlope()
        {
            Vector3 normal = Quaternion.Euler(65f, 0f, 0f) * Vector3.up;
            Assert.That(SteepSlopeRules.CanInitiateJump(true, true, normal, 50f), Is.False);
        }

        [Test]
        public void UphillSuppressionRemovesOnlyComponentIntoSlope()
        {
            Vector3 normal = Quaternion.Euler(60f, 0f, 0f) * Vector3.up;
            Vector3 uphill = SteepSlopeRules.SuppressUphillMovement(Vector3.back * 3f + Vector3.right * 2f, normal);
            Vector3 downhill = SteepSlopeRules.SuppressUphillMovement(Vector3.forward * 3f, normal);
            Assert.That(uphill.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(uphill.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(downhill, Is.EqualTo(Vector3.forward * 3f));
        }
    }
}
