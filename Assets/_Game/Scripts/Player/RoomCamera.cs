using UnityEngine;

namespace Starfall
{
    public sealed class RoomCamera : MonoBehaviour
    {
        public StarfallGame Game;
        Camera cameraComponent;
        Vector3 dampVelocity;
        Vector3 basePosition = new Vector3(0, 0, -10);
        float shakeLeft, shakeAmount;
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
            if (Game == null || !Game.Pause.IsPaused)
            {
                basePosition = Vector3.SmoothDamp(basePosition, target, ref dampVelocity, .16f, float.PositiveInfinity, Time.unscaledDeltaTime);
                shakeLeft = Mathf.Max(0, shakeLeft - Time.unscaledDeltaTime);
                float amount = shakeLeft > 0 && Game != null ? shakeAmount * Game.Settings.shake : 0;
                transform.position = basePosition + new Vector3(Mathf.Sin(Time.unscaledTime * 97) * amount, Mathf.Sin(Time.unscaledTime * 83) * amount, 0);
            }
        }
        public void Kick(float amount) { shakeLeft = .15f; shakeAmount = amount; }
        public void ResetView() { basePosition = transform.position = new Vector3(0, 0, -10); dampVelocity = Vector3.zero; shakeLeft = 0; }
    }
}
