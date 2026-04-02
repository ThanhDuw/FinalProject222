using UnityEngine;

/// <summary>
/// MinimapMarker -- gắn vào bất kỳ GameObject nào muốn hiển thị dưới dạng một chấm màu trên minimap.
///
/// Tạo một hình vuông (quad) nhỏ làm con khi chạy, đặt phía trên thực thể ở layer "Minimap".
/// MinimapCamera (cullingMask bao gồm layer Minimap) sẽ hiển thị nó một cách tự nhiên như một phần của cảnh.
/// Main Camera (cullingMask loại trừ layer Minimap) sẽ không bao giờ nhìn thấy nó.
///
/// Đây là cùng khái niệm với PlayerDot nhưng ở không gian thế giới (world-space):
///   - Không cần tính toán toán học để xác định vị trí
///   - Luôn nằm chính xác vị trí của thực thể trên bản đồ
///   - Tự động biến mất khi thực thể bị hủy
///
/// Quy ước màu sắc:
///   NPC    = Xanh dương (0, 0.4, 1)
///   Kẻ địch = Đỏ         (1, 0, 0)
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
