/// <summary>
/// Keeps this GameObject's rotation aligned with the main camera every frame,
/// so it always faces the player regardless of NPC orientation.
/// Attach to the InteractPrompt_E canvas root.
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

        // Face toward camera position (no tilt, Y axis only)
        UnityEngine.Vector3 dir = transform.position - _cam.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = UnityEngine.Quaternion.LookRotation(dir);
    }
}
