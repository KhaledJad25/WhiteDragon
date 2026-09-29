using System;
using System.Collections.Generic;

// Add new heart types here later (bone, eternal, and so on).
public enum HeartType { Red, Soul, Dark }

// All values are in half-heart units (2 = one full heart).
public class HealthState
{
    public int RedContainers;
    public int RedCurrent;
    public readonly List<HeartType> Overlay = new();

    public event Action Changed;
    public event Action<HeartType> OverlayBroke;

    public bool IsDead => RedCurrent <= 0 && Overlay.Count == 0;

    public void Damage(int halfHearts)
    {
        while (halfHearts > 0 && Overlay.Count > 0)
        {
            int last = Overlay.Count - 1;
            var type = Overlay[last];
            Overlay.RemoveAt(last);
            OverlayBroke?.Invoke(type);
            halfHearts--;
        }
        RedCurrent = Math.Max(0, RedCurrent - halfHearts);
        Changed?.Invoke();
    }

    public void Heal(int halfHearts)
    {
        RedCurrent = Math.Min(RedContainers, RedCurrent + halfHearts);
        Changed?.Invoke();
    }

    public void AddContainer(int halfHearts = 2)
    {
        RedContainers += halfHearts;
        RedCurrent += halfHearts;
        Changed?.Invoke();
    }

    public void AddOverlay(HeartType type, int halfHearts)
    {
        for (int i = 0; i < halfHearts; i++) Overlay.Add(type);
        Changed?.Invoke();
    }
}