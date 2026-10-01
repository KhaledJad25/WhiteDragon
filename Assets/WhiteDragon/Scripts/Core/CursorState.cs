using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Single place that locks or frees the mouse cursor.</summary>
    public static class CursorState
    {
        public static bool Locked { get; private set; }

        public static void SetLocked(bool locked)
        {
            Locked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Locked = false;
    }
}
