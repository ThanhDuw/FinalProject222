using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Tốc độ bay bình thường (W, A, S, D)")]
    public float normalSpeed = 10f;
    [Tooltip("Tốc độ bay khi nhấn giữ Shift")]
    public float fastSpeed = 30f;
    [Tooltip("Độ mượt khi tăng/giảm tốc. Số càng nhỏ càng bay lướt (cinematic)")]
    public float movementDamping = 3f;

    [Header("Look Settings")]
    [Tooltip("Độ nhạy của chuột")]
    public float lookSensitivity = 2.5f;
    [Tooltip("Độ mượt khi xoay camera")]
    public float lookDamping = 8f;

    private Vector3 currentVelocity;
    private Vector3 targetVelocity;

    private Vector2 currentRotation;
    private Vector2 targetRotation;

    private void Start()
    {
        // Khởi tạo góc quay gốc dựa theo Transform của Camera trên Scene
        Vector3 angles = transform.eulerAngles;
        // Chuyển đổi góc X của Unity từ 0..360 thành -180..180
        float startPitch = angles.x;
        if (startPitch > 180f) startPitch -= 360f;

        targetRotation.y = angles.y;
        targetRotation.x = startPitch;
        currentRotation = targetRotation;
    }

    private void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    private void HandleRotation()
    {
        // Chỉ cho phép xoay camera khi người dùng giữ Chuột Phải
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * lookSensitivity;

            targetRotation.y += mouseX;
            targetRotation.x -= mouseY;

            // Chặn không cho ngửa/gục đầu quá 90 độ (lộn ngược camera)
            targetRotation.x = Mathf.Clamp(targetRotation.x, -89f, 89f);
        }

        // Nội suy để xoay mượt mà (Cinematic Smoothing)
        currentRotation = Vector2.Lerp(currentRotation, targetRotation, Time.deltaTime * lookDamping);
        transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
    }

    private void HandleMovement()
    {
        // Kiểm tra xem có đang giữ Shift để bay nhanh không
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : normalSpeed;

        // Trục nằm ngang (A, D) và trục dọc (W, S)
        Vector3 inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        
        // Căn cứ theo trục xoay hiện tại của camera (bay theo hướng đang nhìn)
        Vector3 moveDirection = transform.forward * inputDir.z + transform.right * inputDir.x;

        // Điều khiển độ cao tuyệt đối (Q / E)
        if (Input.GetKey(KeyCode.E))
        {
            moveDirection.y += 1f; // Lên
        }
        if (Input.GetKey(KeyCode.Q))
        {
            moveDirection.y -= 1f; // Xuống
        }

        // Chuẩn hóa vector hướng bay để bay xéo không bị nhanh hơn
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        targetVelocity = moveDirection * speed;

        // Nội suy chuyển động mượt mà (Cinematic gliding effect)
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * movementDamping);

        // Áp dụng vị trí mới
        transform.position += currentVelocity * Time.deltaTime;
    }
}
