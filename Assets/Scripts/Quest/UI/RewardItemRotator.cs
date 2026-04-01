using UnityEngine;

/// <summary>
/// Xoay item reward 3D theo trục Y liên tục và thêm hiệu ứng nhấp nhô (bobbing).
/// Gắn trực tiếp vào Reward_Rake prefab.
/// </summary>
public class RewardItemRotator : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 60f;   // độ/giây
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    
    [Header("Bob Effect (Optional)")]
    [SerializeField] private bool enableBob = true;
    [SerializeField] private float bobAmplitude = 0.1f;   // units
    [SerializeField] private float bobSpeed = 1.5f;       // Hz
    
    private Vector3 _startLocalPos;
    private float _bobTimer;
    
    private void Start() 
    { 
        _startLocalPos = transform.localPosition;
    }
    
    private void Update() 
    { 
        // Rotation
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);

        // Bob
        if (enableBob)
        {
            _bobTimer += Time.deltaTime;
            float newY = _startLocalPos.y + Mathf.Sin(_bobTimer * Mathf.PI * 2f * bobSpeed) * bobAmplitude;
            transform.localPosition = new Vector3(_startLocalPos.x, newY, _startLocalPos.z);
        }
    }
}
