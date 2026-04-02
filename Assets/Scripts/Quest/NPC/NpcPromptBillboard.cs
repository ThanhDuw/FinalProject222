/// <summary>
/// Cập nhật góc quay của GameObject này để luôn hướng về camera chính mỗi khung hình,
/// giúp nó luôn hướng về phía người chơi bất kể hướng của NPC.
/// Gắn vào root canvas của InteractPrompt_E.
/// </summary>
public class NpcPromptBillboard : UnityEngine.MonoBehaviour
{
    private UnityEngine.Transform _cam;

    private void Start()
    {
        var mc = UnityEngine.Camera.main;
        if (mc != null) _cam = mc.transform;
    }

    private void LateUpdate()
    {
        if (_cam == null)
        {
            var mc = UnityEngine.Camera.main;
            if (mc != null) _cam = mc.transform;
            else return;
        }

        // Hướng về vị trí camera (không nghiêng, chỉ quay trục Y)
        UnityEngine.Vector3 dir = transform.position - _cam.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = UnityEngine.Quaternion.LookRotation(dir);
    }
}
