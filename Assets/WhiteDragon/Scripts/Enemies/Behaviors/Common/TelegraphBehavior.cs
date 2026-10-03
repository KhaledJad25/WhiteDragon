using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// The wind-up every attack needs: blinks the enemy, plays a sound, raises the Windup animation state,
    /// optionally freezes it and keeps it facing the player. Put it in the state before the attack state.
    /// </summary>
    [EnemyBehaviorInfo("Wind-up before an attack: blink, sound, Windup animation, optional freeze and facing. Finishes after the duration.", "Attack")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Telegraph", fileName = "Telegraph")]
    public class TelegraphBehavior : EnemyBehavior<TelegraphBehavior.State>
    {
        public class State
        {
            public float Time;
            public float BlinkTimer;
        }

        [Tooltip("Seconds of wind-up.")]
        [Min(0f)] public float duration = 0.6f;
        [Tooltip("Stand still during the wind-up.")]
        public bool freezeMovement = true;
        [Tooltip("Keep facing the player during the wind-up.")]
        public bool faceTarget = true;
        [Header("Feedback")]
        public Color blinkColor = new Color(1f, 0.85f, 0.6f);
        [Tooltip("Seconds between blinks.")]
        [Min(0.02f)] public float blinkInterval = 0.15f;
        [Tooltip("Played once at the start. Empty = the generated throw whoosh.")]
        public AudioClip sound;
        [Range(0f, 1f)] public float volume = 0.7f;

        protected override void Enter(EnemyContext ctx, State s)
        {
            s.Time = 0f;
            s.BlinkTimer = 0f;
            ctx.Events.Raise(ActorState.Windup);
            Blink(ctx);
            if (!Application.isPlaying) return;
            var clip = sound != null ? sound : GameFeel.ClipFor(GameSound.Throw);
            if (clip != null && GameFeel.SoundVolume > 0f) AudioSource.PlayClipAtPoint(clip, ctx.Position, volume * GameFeel.SoundVolume);
        }

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            s.Time += dt;
            if (freezeMovement) ctx.Freeze();
            if (faceTarget && ctx.HasTarget) ctx.FaceTarget();
            s.BlinkTimer += dt;
            while (s.BlinkTimer >= blinkInterval)
            {
                s.BlinkTimer -= blinkInterval;
                Blink(ctx);
            }
            return s.Time >= duration - 1e-4f;
        }

        void Blink(EnemyContext ctx) => RendererTint.For(ctx.Enemy.gameObject).Flash(blinkColor, blinkInterval * 0.5f);
    }
}
