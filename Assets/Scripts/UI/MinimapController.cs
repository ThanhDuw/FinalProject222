using UnityEngine;

/// <summary>
/// MinimapController -- attached to the MinimapCamera GameObject.
///
/// The camera is a child of PlayerCore/Character, so it follows the
/// player automatically. This script only handles:
///   - Keeping the camera at a fixed height above the player
///   - Keeping rotation locked to look straight down
///
/// Setup:
///   1. Attach this script to MinimapCamera
///   2. MinimapCamera must be a child of the Player Character GO
///   3. Assign a RenderTexture to the Camera component's Target Texture
///   4. Set Camera to Orthographic
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float _height = 30f;

    private void LateUpdate()
    {
        // Keep fixed height and always look straight down
        // XZ position is inherited from parent (Character)
        Vector3 pos = transform.localPosition;
        pos.y = _height;
        transform.localPosition = pos;

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
