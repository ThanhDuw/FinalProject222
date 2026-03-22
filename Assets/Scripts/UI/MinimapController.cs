using UnityEngine;

/// <summary>
/// MinimapController -- attached to the MinimapCamera GameObject.
///
/// Responsibilities:
///   1. Lock camera height to a fixed value above the player
///   2. Lock rotation to always look straight down (90 degrees on X)
///
/// The camera follows the player automatically because it is a child of PlayerCore/Character.
/// NPC and Enemy dots are handled by MinimapMarker components attached directly to those entities.
/// Player dot is a UI Image (PlayerDot) centered in MinimapMask -- always at center.
///
/// Setup:
///   - Attach to MinimapCamera (child of PlayerCore/Character)
///   - Camera: Orthographic, Size=15, Depth=-2, ClearFlags=SolidColor (black)
///   - TargetTexture: MinimapRT
///   - CullingMask: must include "Minimap" layer so MinimapMarker quads are visible
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float _height = 30f;

    private void LateUpdate()
    {
        // Lock height -- XZ follows parent (PlayerCore/Character) automatically
        Vector3 pos = transform.localPosition;
        pos.y = _height;
        transform.localPosition = pos;

        // Always look straight down
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
