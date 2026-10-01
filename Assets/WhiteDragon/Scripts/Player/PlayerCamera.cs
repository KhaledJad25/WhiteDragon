using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Field of view and smoothed head bob for the player camera (a child of the Head).</summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour
    {
        [Tooltip("Horizontal field of view in degrees.")]
        public float fieldOfView = 90f;
        public bool headBob = true;
        public float bobAmount = 0.02f;
        [Tooltip("Bobs per meter travelled.")]
        public float bobFrequency = 0.4f;
        public float bobSmoothing = 15f;
        public PlayerController controller;

        Camera cam;
        float phase;
        Vector3 offset;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (controller == null) controller = GetComponentInParent<PlayerController>();
        }

        void LateUpdate()
        {
            cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(fieldOfView, cam.aspect);

            float dt = Time.deltaTime;
            Vector3 target = Vector3.zero;
            if (headBob && controller != null && controller.IsGrounded)
            {
                float speed = controller.HorizontalVelocity.magnitude;
                if (speed > 0.1f)
                {
                    phase += speed * dt * bobFrequency;
                    target = new Vector3(Mathf.Sin(phase * Mathf.PI) * bobAmount * 0.5f,
                                         Mathf.Sin(phase * Mathf.PI * 2f) * bobAmount, 0f);
                }
            }
            offset = Vector3.Lerp(offset, target, 1f - Mathf.Exp(-bobSmoothing * dt));
            transform.localPosition = offset;
        }
    }
}
