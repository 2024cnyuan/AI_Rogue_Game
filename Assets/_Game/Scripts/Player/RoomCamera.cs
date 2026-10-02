using UnityEngine;

namespace Starfall
{
    public sealed class RoomCamera : MonoBehaviour
    {
        public StarfallGame Game;
        Camera cameraComponent;
        Vector3 dampVelocity;
        void Awake() { cameraComponent = GetComponent<Camera>(); }
        void LateUpdate()
        {
            cameraComponent.orthographicSize = Mathf.Max(9.3f, 13.4f / Mathf.Max(.5f, cameraComponent.aspect));
            Vector3 target = new Vector3(0, 0, -10);
            if (Game != null && Game.Player != null && Game.CanAct)
            {
                Vector2 desired = Game.Player.Body.position + Game.Player.Aim * .5f;
                float halfHeight = cameraComponent.orthographicSize, halfWidth = halfHeight * cameraComponent.aspect;
                target.x = Mathf.Clamp(desired.x, -Mathf.Max(0, 14 - halfWidth), Mathf.Max(0, 14 - halfWidth));
                target.y = Mathf.Clamp(desired.y, -Mathf.Max(0, 10 - halfHeight), Mathf.Max(0, 10 - halfHeight));
            }
            // Pause leaves the exact view and smoothing state intact.
            if (Game == null || !Game.Pause.IsPaused) transform.position = Vector3.SmoothDamp(transform.position, target, ref dampVelocity, .16f, float.PositiveInfinity, Time.unscaledDeltaTime);
        }
        public void ResetView() { transform.position = new Vector3(0, 0, -10); dampVelocity = Vector3.zero; }
    }
}
