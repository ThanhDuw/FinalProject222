using UnityEngine;

/// <summary>
/// MinimapMarker -- attach to any GameObject that should appear as a colored dot on the minimap.
///
/// Creates a small flat quad child at runtime, placed above the entity on the "Minimap" layer.
/// The MinimapCamera (cullingMask includes Minimap layer) renders it naturally as part of the scene.
/// The Main Camera (cullingMask excludes Minimap layer) never sees it.
///
/// This is the same concept as PlayerDot but in world-space:
///   - No script math needed for positioning
///   - Always exactly where the entity is on the map
///   - Disappears automatically when the entity is destroyed
///
/// Color convention:
///   NPC    = Blue  (0, 0.4, 1)
///   Enemy  = Red   (1, 0, 0)
/// </summary>
public class MinimapMarker : MonoBehaviour
{
    [Header("Marker Settings")]
    [SerializeField] private Color _color  = Color.blue;
    [SerializeField] private float _height = 3f;    // world units above entity pivot
    [SerializeField] private float _size   = 2.5f;  // world-unit width/depth of the quad

    private void Awake()
    {
        int minimapLayer = LayerMask.NameToLayer("Minimap");
        if (minimapLayer < 0)
        {
            Debug.LogWarning("[MinimapMarker] 'Minimap' layer not found. Add it in Edit > Project Settings > Tags and Layers.");
            return;
        }

        // Create the flat quad that the MinimapCamera will render
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name      = "MinimapDot";
        quad.layer     = minimapLayer;
        quad.transform.SetParent(transform, false);
        quad.transform.localPosition = new Vector3(0f, _height, 0f);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face up (top-down view)
        quad.transform.localScale    = new Vector3(_size, _size, 1f);

        // Remove the collider -- this is a visual-only marker
        Destroy(quad.GetComponent<Collider>());

        // Assign an unlit material so lighting doesn't affect the dot color
        Renderer rend = quad.GetComponent<Renderer>();
        Material mat  = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", _color);
        rend.material = mat;
    }
}
