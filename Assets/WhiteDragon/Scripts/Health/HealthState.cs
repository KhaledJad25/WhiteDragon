using System;
using System.Collections.Generic;

namespace WhiteDragon
{
    /// <summary>
    /// Isaac-style hearts in half-heart units. Red containers hold main health; soul and dark hearts
    /// are an overlay that takes damage first (most recently added first). Plain C# for unit tests.
    /// </summary>
    public class HealthState
    {
        public class OverlayHeart
        {
            public HeartKind Kind;
            public int Halves;
        }

        public float InvincibilitySeconds = 1f;

        readonly List<OverlayHeart> overlay = new List<OverlayHeart>();
        float invincibleUntil = float.NegativeInfinity;

        public int RedContainers { get; private set; }
        public int MaxRed => RedContainers * 2;
        public int Red { get; private set; }
        public bool IsDead { get; private set; }
        public IReadOnlyList<OverlayHeart> Overlay => overlay;
        public int Soul => SumOverlay(HeartKind.Soul);
        public int Dark => SumOverlay(HeartKind.Dark);

        public event Action Changed;
        public event Action<int> Damaged;
        public event Action DarkHeartBroken;
        public event Action Died;

        public HealthState(int redContainers = 3)
        {
            RedContainers = Math.Max(0, redContainers);
            Red = MaxRed;
        }

        public bool IsInvincible(float now) => now < invincibleUntil;

        /// <summary>Damage in half hearts. Returns false if ignored (dead, invincible, or no damage).</summary>
        public bool TryDamage(int halves, float now)
        {
            if (IsDead || halves <= 0 || IsInvincible(now)) return false;

            int remaining = halves;
            while (remaining > 0 && overlay.Count > 0)
            {
                var top = overlay[overlay.Count - 1];
                int take = Math.Min(remaining, top.Halves);
                top.Halves -= take;
                remaining -= take;
                if (top.Halves > 0) continue;
                overlay.RemoveAt(overlay.Count - 1);
                if (top.Kind == HeartKind.Dark) DarkHeartBroken?.Invoke();
            }
            Red = Math.Max(0, Red - remaining);
            invincibleUntil = now + InvincibilitySeconds;

            Damaged?.Invoke(halves);
            Changed?.Invoke();
            CheckDeath();
            return true;
        }

        public void Heal(int halves)
        {
            if (IsDead || halves <= 0) return;
            Red = Math.Min(MaxRed, Red + halves);
            Changed?.Invoke();
        }

        /// <summary>Adds red containers; filled by default.</summary>
        public void AddContainers(int count, bool fill = true)
        {
            if (IsDead || count <= 0) return;
            RedContainers += count;
            if (fill) Red = Math.Min(MaxRed, Red + count * 2);
            Changed?.Invoke();
        }

        public void AddSoul(int halves) => AddOverlay(HeartKind.Soul, halves);
        public void AddDark(int halves) => AddOverlay(HeartKind.Dark, halves);

        public void Kill()
        {
            if (IsDead) return;
            overlay.Clear();
            Red = 0;
            Changed?.Invoke();
            CheckDeath();
        }

        void AddOverlay(HeartKind kind, int halves)
        {
            if (IsDead || halves <= 0) return;
            while (halves > 0)
            {
                var top = overlay.Count > 0 ? overlay[overlay.Count - 1] : null;
                if (top != null && top.Kind == kind && top.Halves == 1)
                {
                    top.Halves = 2;
                    halves--;
                    continue;
                }
                int add = Math.Min(2, halves);
                overlay.Add(new OverlayHeart { Kind = kind, Halves = add });
                halves -= add;
            }
            Changed?.Invoke();
        }

        void CheckDeath()
        {
            if (IsDead || Red > 0 || overlay.Count > 0) return;
            IsDead = true;
            Died?.Invoke();
        }

        int SumOverlay(HeartKind kind)
        {
            int n = 0;
            foreach (var h in overlay)
                if (h.Kind == kind) n += h.Halves;
            return n;
        }
    }
}
