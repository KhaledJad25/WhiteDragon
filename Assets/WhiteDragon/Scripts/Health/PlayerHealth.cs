using UnityEngine;

namespace WhiteDragon
{
    /// <summary>The player's hearts. Logic lives in HealthState; this adds timing, feedback and death.</summary>
    public class PlayerHealth : MonoBehaviour
    {
        public int startingContainers = 3;

        HealthState state;

        public HealthState State => state ??= new HealthState(startingContainers);

        void OnEnable() => State.Died += OnDied;
        void OnDisable() => State.Died -= OnDied;

        /// <summary>Damage in half hearts. Respects invincibility frames.</summary>
        public bool Damage(int halves)
        {
            if (!State.TryDamage(halves, Time.time)) return false;
            GameFeel.OnPlayerHurt(transform.position + Vector3.up);
            ActorStateEvents.For(gameObject).Raise(State.IsDead ? ActorState.Die : ActorState.Hit);
            return true;
        }

        void OnDied()
        {
            ActorStateEvents.For(gameObject).Raise(ActorState.Die);
            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;
            var thrower = GetComponent<RockThrower>();
            if (thrower != null) thrower.enabled = false;
            RunSession.EndRun();
        }
    }
}
