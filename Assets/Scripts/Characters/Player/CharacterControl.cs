using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Timers;
using CreatorKitCode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace CreatorKitCodeInternal {
    public class CharacterControl : MonoBehaviour, 
        // TÁI CẤU TRÚC COMBAT: Các frame tấn công hiện được xử lý bởi CombatController.
        AnimationControllerDispatcher.IFootstepFrameReceiver
    {
        public static CharacterControl Instance { get; protected set; }
    
        public float Speed = 10.0f;

        public CharacterData Data => m_CharacterData;
        public CharacterData CurrentTarget => m_CurrentTargetCharacterData;

        public Transform WeaponLocator;
    
        [Header("Audio")]
        public AudioClip[] SpurSoundClips;

        [Header("Camera")]
        [SerializeField] private float cameraRotateSpeed = 180f; // degrees per second when holding RMB

        [Header("Movement")]
        [SerializeField] private float rotationSpeed = 15f; // Slerp interpolation speed for character facing
    
        Animator m_Animator;
        CharacterController m_CharacterController;   // TÁI CẤU TRÚC DI CHUYỂN: Di chuyển sử dụng CharacterController
        CharacterData m_CharacterData;

        HighlightableObject m_Highlighted;

        RaycastHit[] m_RaycastHitCache = new RaycastHit[16];

        int m_SpeedParamID;
        int m_AttackParamID;
        int m_HitParamID;
        int m_FaintParamID;
        int m_RespawnParamID;

        bool m_IsKO = false;
        float m_KOTimer = 0.0f;

        int m_InteractableLayer;
        int m_LevelLayer;
        Collider m_TargetCollider;
        // m_TargetInteractable removed (unused) = null;
        Camera m_MainCamera;

        CharacterAudio m_CharacterAudio;
        CombatController m_CombatController;         // COMBAT REFACTOR: Dedicated combat controller

        // Layer được sử dụng để highlight mục tiêu lúc trước.
        int m_TargetLayer;
        CharacterData m_CurrentTargetCharacterData = null;
        // Cờ được sử dụng ở luồng nhấp-để-tấn-công cũ nhằm xóa mục tiêu sau khi đòn đánh kết thúc.


        SpawnPoint m_CurrentSpawn = null;
    
        enum State
        {
            DEFAULT,
            HIT,
            ATTACKING
        }



        Vector3 m_LastRaycastResult;

        // Theo dõi trọng lực
        float m_VerticalVelocity = 0f;
        const float Gravity = -20f;

        // Lưu cache transform để giảm chi phí truy cập thuộc tính
        Transform m_Transform;

        void Awake()
        {
            Instance = this;
            m_MainCamera = Camera.main;
            m_Transform = transform;
        }

        // Start is called before the first frame update
        void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            m_CharacterController = GetComponent<CharacterController>();
            m_Animator = GetComponentInChildren<Animator>();

            m_LastRaycastResult = m_Transform.position;

            m_SpeedParamID = Animator.StringToHash("Speed");
            m_AttackParamID = Animator.StringToHash("Attack");
            m_HitParamID = Animator.StringToHash("Hit");
            m_FaintParamID = Animator.StringToHash("Faint");
            m_RespawnParamID = Animator.StringToHash("Respawn");

            m_CharacterData = GetComponent<CharacterData>();

            m_CharacterData.Equipment.OnEquiped += item =>
            {
                if (item.Slot == (EquipmentItem.EquipmentSlot)666)
                {
                    var obj = Instantiate(item.WorldObjectPrefab, WeaponLocator, false);
                    Helpers.RecursiveLayerChange(obj.transform, LayerMask.NameToLayer("PlayerEquipment"));
                }
            };
        
            m_CharacterData.Equipment.OnUnequip += item =>
            {
                if (item.Slot == (EquipmentItem.EquipmentSlot)666)
                {
                    foreach(Transform t in WeaponLocator)
                        Destroy(t.gameObject);
                }
            };
            
            m_CharacterData.Init();
        
            m_InteractableLayer = 1 << LayerMask.NameToLayer("Interactable");
            m_LevelLayer = 1 << LayerMask.NameToLayer("Level");
            m_TargetLayer = 1 << LayerMask.NameToLayer("Target");



            m_CharacterAudio = GetComponent<CharacterAudio>();

            // TÁI CẤU TRÚC COMBAT: Lưu cache reference CombatController dùng cho input tấn công.
            m_CombatController = GetComponent<CombatController>();
        
            m_CharacterData.OnDamage += () =>
            {
                m_Animator.SetTrigger(m_HitParamID);
                m_CharacterAudio.Hit(m_Transform.position);
            };
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 pos = m_Transform.position;
        
            if (m_IsKO)
            {
                m_KOTimer += Time.deltaTime;
                if (m_KOTimer > 3.0f)
                {
                    GoToRespawn();
                }

                return;
            }

            //The update need to run, so we can check the health here.
            //Another method would be to add a callback in the CharacterData that get called
            //when health reach 0, and this class register to the callback in Start
            //(see CharacterData.OnDamage for an example)
            if (m_CharacterData.Stats.CurrentHealth == 0)
            {
                m_Animator.SetTrigger(m_FaintParamID);
                m_IsKO = true;
                m_KOTimer = 0.0f;
            
                Data.Death();
            
                m_CharacterAudio.Death(pos);
            
                return;
            }
        
            // TÁI CẤU TRÚC DI CHUYỂN: Di chuyển bằng phím WASD sử dụng CharacterController.
            if (GameInput.Instance == null) return; // Bảo vệ: chờ đến khi GameInput sẵn sàng
            Vector2 moveInput = GameInput.Instance.MoveInput;
            float h = moveInput.x;
            float v = moveInput.y;

            // Di chuyển tương đối theo góc nhìn camera cho điều khiển từ trên xuống (top-down)
            Vector3 camForward = m_MainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = m_MainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDir = (camRight * h + camForward * v).normalized;
            // Tránh cấp phát Vector2 mới chỉ để kiểm tra xem có input không
            float inputMag = (Mathf.Abs(h) > 0.001f || Mathf.Abs(v) > 0.001f) ? 1f : 0f;

            // Xoay nhân vật mượt mà theo hướng di chuyển
            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Áp dụng di chuyển thông qua CharacterController (không dùng click-to-move của NavMeshAgent)
            if (m_CharacterController != null && m_CharacterController.enabled)
            {
                Vector3 moveVelocity = moveDir * Speed * Time.deltaTime;
                float grav = m_CharacterController.isGrounded ? -2f : m_VerticalVelocity + Gravity * Time.deltaTime;
                m_VerticalVelocity = grav;
                moveVelocity.y = m_VerticalVelocity * Time.deltaTime;
                m_CharacterController.Move(moveVelocity);
            }

            float mouseWheel = GameInput.Instance.ScrollValue;
            if (!Mathf.Approximately(mouseWheel, 0.0f))
            {
                Vector3 view = m_MainCamera.ScreenToViewportPoint(Input.mousePosition);
                if(view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f)
                    CameraController.Instance.Zoom(-mouseWheel * Time.deltaTime * 20.0f);
            }
        
            // Mới: xoay camera quanh người chơi khi giữ chuột phải
            if (GameInput.Instance.CameraRotateHeld)
            {
                Vector3 view = m_MainCamera.ScreenToViewportPoint(Input.mousePosition);
                if (view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f)
                {
                    float mouseX = GameInput.Instance.CameraRotateDelta;
                    if (!Mathf.Approximately(mouseX, 0f) && CameraController.Instance != null)
                    {
                        // Xoay GameObject camera quanh trục Y của người chơi (yaw) dựa trên chuyển động của chuột.
                        CameraController.Instance.transform.RotateAround(m_Transform.position, Vector3.up, mouseX * cameraRotateSpeed * Time.deltaTime);
                    }
                }
            }
        
            // Cập nhật tham số tốc độ animator từ độ lớn của input (không từ NavMeshAgent).
            m_Animator.SetFloat(m_SpeedParamID, inputMag);

            // COMBAT INPUT: Ủy thác (delegate) tấn công cho CombatController.
            // Thay đổi: tấn công bằng phím space -> nhấp chuột trái để tấn công mục tiêu.
            if (GameInput.Instance.AttackPressed && m_CombatController != null)
            {
                // Bỏ qua các thao tác nhấp chuột khi con trỏ ở trên UI
                if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
                {
                    var target = GetClickedCharacterData();
                    if (target != null)
                    {
                        m_CombatController.TryAttackAt(target);
                        // Đồng thời thiết lập mục tiêu hiện tại cục bộ để tương thích với UI
                        m_CurrentTargetCharacterData = target;
                    }
                }
            }

            // Đồng bộ mục tiêu với CombatController để hệ thống UI hiển thị chính xác máu kẻ địch
            if (m_CombatController != null)
            {
                m_CurrentTargetCharacterData = m_CombatController.CurrentTarget;
            }
        
            // Phím tắt bàn phím
            if(GameInput.Instance.InventoryPressed)
                UISystem.Instance.ToggleInventory();
        }

        private CharacterData GetClickedCharacterData()
        {
            Ray ray = m_MainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Bắn tia quét (raycast) trên hệ thống vật lý với khoảng cách mặc định
            if (Physics.Raycast(ray, out hit, 100f))
            {
                // Thử lấy CharacterData trên đối tượng được nhấp vào hoặc các đối tượng cha của nó
                var cd = hit.collider.GetComponentInParent<CharacterData>();
                if (cd != null)
                    return cd;

                // Nếu nhấp vào một Interactable, có thể xử lý tương tác tùy kịch bản
                var interact = hit.collider.GetComponentInParent<InteractableObject>();
                if (interact != null && interact.IsInteractable)
                {
                    // If interactable is also attackable by being a CharacterData, it was already returned.
                    // Otherwise, use InteractWith to handle non-attack interactions.
                    InteractWith(interact);
                }
            }

            return null;
        }

        void GoToRespawn()
        {
            m_Animator.ResetTrigger(m_HitParamID);

            if (m_CurrentSpawn != null)
            {
                m_Transform.position = m_CurrentSpawn.transform.position;
            }
            m_IsKO = false;

            m_CurrentTargetCharacterData = null;
        // (removed: m_TargetInteractable = null)ull;

        // (removed: was m_CurrentState = State.DEFAULT)ULT;
        
            m_Animator.SetTrigger(m_RespawnParamID);
        
            m_CharacterData.Stats.ChangeHealth(m_CharacterData.Stats.stats.health);
        }

        void SwitchHighlightedObject(HighlightableObject obj)
        {
            if(m_Highlighted != null) m_Highlighted.Dehighlight();

            m_Highlighted = obj;
            if(m_Highlighted != null) m_Highlighted.Highlight();
        }


        public void SetNewRespawn(SpawnPoint point)
        {
            if(m_CurrentSpawn != null)
                m_CurrentSpawn.Deactivated();

            m_CurrentSpawn = point;
            m_CurrentSpawn.Activated();
        }

        public void InteractWith(object obj)
        {
            // Loot interaction from LootUI (button click)
            if (obj is Loot loot)
            {
                if (loot != null && loot.IsInteractable)
                {
                    loot.InteractWith(m_CharacterData);
                }
            }
            // Tương tác đa năng (không còn tự động di chuyển với NavMesh nữa, yêu cầu người chơi phải ở trong tầm)
            else if (obj is InteractableObject interactable)
            {
                if (interactable != null && interactable.IsInteractable)
                {
                    interactable.InteractWith(m_CharacterData);
                }
            }
        }

        public void FootstepFrame()
        {
            Vector3 pos = m_Transform.position;
        
            m_CharacterAudio.Step(pos);
        
            SFXManager.PlaySound(SFXManager.Use.Player, new SFXManager.PlayData()
            {
                Clip = SpurSoundClips[Random.Range(0, SpurSoundClips.Length)], 
                Position = pos,
                PitchMin = 0.8f,
                PitchMax = 1.2f,
                Volume = 0.3f
            });
        
            VFXManager.PlayVFX(VFXType.StepPuff, pos);  
        }
    }
}