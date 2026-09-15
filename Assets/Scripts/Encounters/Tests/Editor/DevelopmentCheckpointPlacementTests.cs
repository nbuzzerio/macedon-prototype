using Macedon.Villagers;
using NUnit.Framework;
using UnityEngine;

namespace Macedon.Encounters.Tests
{
    public sealed class DevelopmentCheckpointPlacementTests
    {
        [Test]
        public void FollowerOffsets_AreDeterministicAndUniqueBySlot()
        {
            Vector3 origin = new(10f, 2f, 20f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
            Vector3 first = DevelopmentCheckpointPlacement.FollowerPosition(origin, rotation, 0);

            Assert.That(DevelopmentCheckpointPlacement.FollowerPosition(origin, rotation, 0), Is.EqualTo(first));
            Assert.That(DevelopmentCheckpointPlacement.FollowerPosition(origin, rotation, 1), Is.Not.EqualTo(first));
            Assert.That(DevelopmentCheckpointPlacement.FollowerPosition(origin, rotation, 2), Is.Not.EqualTo(first));
        }

        [Test]
        public void PartyRegistry_RepeatedRegistrationPreservesOneMemberAndSlot()
        {
            var registry = new VillagerPartyRegistry<object>();
            var follower = new object();
            Assert.That(registry.Register(follower, out int initialSlot), Is.True);
            Assert.That(registry.Register(follower, out int repeatedSlot), Is.False);
            Assert.That(repeatedSlot, Is.EqualTo(initialSlot));
            Assert.That(registry.Count, Is.EqualTo(1));
        }
    }
}
