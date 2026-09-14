using System.Collections.Generic;

namespace Macedon.Villagers
{
    public sealed class VillagerPartyRegistry<T> where T : class
    {
        private readonly Dictionary<T, int> slots = new();

        public int Count => slots.Count;

        public bool Register(T follower, out int slot)
        {
            if (follower == null)
            {
                slot = -1;
                return false;
            }

            if (slots.TryGetValue(follower, out slot)) return false;
            slot = FirstAvailableSlot();
            slots.Add(follower, slot);
            return true;
        }

        public bool Unregister(T follower) => follower != null && slots.Remove(follower);

        public bool TryGetSlot(T follower, out int slot) => slots.TryGetValue(follower, out slot);

        public IEnumerable<KeyValuePair<T, int>> Entries => slots;

        private int FirstAvailableSlot()
        {
            for (int candidate = 0; ; candidate++)
            {
                if (!slots.ContainsValue(candidate)) return candidate;
            }
        }
    }
}
