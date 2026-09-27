using UnityEngine;
using UnityEngine.UI;

public class DetectionIndicator : MonoBehaviour
{
    public DroneAI drone;
    public Camera cam;
    public RectTransform indicator;    // the radial's RectTransform
    public Image fillImage;            // the radial Image (Filled, Radial 360)
    public float edgePadding = 50f;
    public Vector2 onScreenOffset = new Vector2(0f, 60f); // hover above the drone
    public Color startColor = Color.yellow;
    public Color fullColor = Color.red;

    private void Update()
    {
        float progress = drone.detectionMeter / 100f ;

        if (progress <= 0f)
        {
            indicator.gameObject.SetActive(false);
            return;
        }

        indicator.gameObject.SetActive(true);
        fillImage.fillAmount = progress;
        fillImage.color = Color.Lerp(startColor, fullColor, progress);

        Vector3 screenPos = cam.WorldToScreenPoint(drone.transform.position);
        bool isBehind = screenPos.z < 0f;

        bool isOnScreen = !isBehind &&
                          screenPos.x > 0f && screenPos.x < Screen.width &&
                          screenPos.y > 0f && screenPos.y < Screen.height;

        if (isOnScreen)
        {
            indicator.position = (Vector2)screenPos + onScreenOffset;
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
        Vector2 direction = (Vector2)screenPos - screenCenter;

        // Positions behind the camera are mirrored, so flip the direction
        if (isBehind) direction = -direction;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.down;

        // Scale the direction so it lands exactly on the padded screen edge
        Vector2 bounds = screenCenter - Vector2.one * edgePadding;
        float scale = Mathf.Min(
            bounds.x / Mathf.Abs(direction.x),
            bounds.y / Mathf.Abs(direction.y));

        indicator.position = screenCenter + direction * scale;
    }
}