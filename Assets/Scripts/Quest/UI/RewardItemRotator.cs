using UnityEngine;

/// <summary>
/// Xoay item reward 3D theo trục Y liên tục (turntable).
/// Gắn trực tiếp vào Reward_Rake prefab.
/// </summary>
public class RewardItemRotator : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 60f;   // độ/giây
    
    private void Update() 
    { 
        // Turntable: chỉ xoay quanh World Y, giữ X/Z rotation cố định
        // transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
