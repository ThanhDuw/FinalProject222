using UnityEngine;

/// <summary>
/// MinimapController -- gắn vào GameObject MinimapCamera.
///
/// Nhiệm vụ:
///   1. Khóa chiều cao camera ở một giá trị cố định phía trên người chơi
///   2. Khóa góc xoay để luôn nhìn thẳng xuống (90 độ trên trục X)
///
/// Camera tự động đi theo người chơi vì nó là con của PlayerCore/Character.
/// Các chấm NPC và Kẻ địch được xử lý bởi các component MinimapMarker gắn trực tiếp vào các thực thể đó.
/// Chấm Người chơi là một UI Image (PlayerDot) nằm giữa MinimapMask -- luôn ở trung tâm.
///
/// Thiết lập:
///   - Gắn vào MinimapCamera (con của PlayerCore/Character)
///   - Camera: Orthographic, Size=15, Depth=-2, ClearFlags=SolidColor (black)
///   - TargetTexture: MinimapRT
///   - CullingMask: phải bao gồm layer "Minimap" để các hình vuông MinimapMarker có thể nhìn thấy được
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
