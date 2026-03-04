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
        AnimationControllerDispatcher.IFootstepFrameReceiver
    {
        public static CharacterControl Instance { get; protected set; }
    
        public float Speed = 10.0f;

        public CharacterData Data => m_CharacterData;
        public CharacterData CurrentTarget => m_CombatController != null ? m_CombatController.CurrentTarget : null;

        public Transform WeaponLocator;
    
        [Header("Audio")]
        public AudioClip[] SpurSoundClips;
    
        Animator m_Animator;
        CharacterController m_CharacterController;
        CharacterData m_CharacterData;

        RaycastHit[] m_RaycastHitCache = new RaycastHit[16];

        int m_SpeedParamID;
        int m_HitParamID;
        int m_FaintParamID;
        int m_RespawnParamID;

        bool m_IsKO = false;
        float m_KOTimer = 0.0f;

        Camera m_MainCamera;
        CharacterAudio m_CharacterAudio;

        SpawnPoint m_CurrentSpawn = null;
        
        // ========== RESTORED: Interactable tracking ==========
        Collider m_TargetCollider;
        InteractableObject m_TargetInteractable = null;
    
        enum LocalState
        {
            DEFAULT,
            HIT,
            DEAD
        }

        LocalState m_CurrentState;

        // Combat Controller reference
        CombatController m_CombatController;

        void Awake()
        {
            Instance = this;
            m_MainCamera = Camera.main;
        }

        // Start is called before the first frame update
        void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        
            m_CharacterController = GetComponent<CharacterController>();
            if (m_CharacterController == null)
            {
                Debug.LogError("CharacterControl: CharacterController component not found! Please add CharacterController to this GameObject.");
            }

            m_Animator = GetComponentInChildren<Animator>();
        
            m_SpeedParamID = Animator.StringToHash("Speed");
            m_HitParamID = Animator.StringToHash("Hit");
            m_FaintParamID = Animator.StringToHash("Faint");
            m_RespawnParamID = Animator.StringToHash("Respawn");

            m_CharacterData = GetComponent<CharacterData>();

            // Equipment system - UNCHANGED
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

            m_CurrentState = LocalState.DEFAULT;

            m_CharacterAudio = GetComponent<CharacterAudio>();
        
            // Damage callback - UNCHANGED
            m_CharacterData.OnDamage += () =>
            {
                m_Animator.SetTrigger(m_HitParamID);
                m_CharacterAudio.Hit(transform.position);
            };

            // Initialize Combat Controller
            m_CombatController = GetComponent<CombatController>();
            if (m_CombatController == null)
            {
                Debug.LogError("CharacterControl: CombatController component not found!");
            }
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 pos = transform.position;
        
            // KO state - UNCHANGED
            if (m_IsKO)
            {
                m_KOTimer += Time.deltaTime;
                if (m_KOTimer > 3.0f)
                {
                    GoToRespawn();
                }
                return;
            }

            // Health check - UNCHANGED (modified for no NavMesh)
            if (m_CharacterData.Stats.CurrentHealth == 0)
            {
                m_Animator.SetTrigger(m_FaintParamID);
                m_IsKO = true;
                m_KOTimer = 0.0f;
            
                Data.Death();
                m_CharacterAudio.Death(pos);
            
                return;
            }

            // ========== RESTORED: Check interactable range ==========
            if (m_TargetInteractable != null)
            {
                CheckInteractableRange();
            }

            // ========== REFACTORED: WASD Movement System ==========
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Camera-relative movement for top-down (Y axis ignored)
            Vector3 camForward = m_MainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            
            Vector3 camRight = m_MainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDir = (camRight * h + camForward * v).normalized;
            float inputMag = new Vector2(h, v).magnitude > 0f ? 1f : 0f;

            // Rotate character to face movement direction
            if (moveDir.sqrMagnitude > 0.001f)
            {
                transform.forward = moveDir;
            }

            // Apply movement via CharacterController
            Vector3 moveVelocity = moveDir * Speed * Time.deltaTime;
            if (m_CharacterController != null && m_CharacterController.enabled)
            {
                m_CharacterController.Move(moveVelocity);
            }

            // Update animator speed parameter
            m_Animator.SetFloat(m_SpeedParamID, inputMag);

            // ========== Camera Zoom (UNCHANGED) ==========
            float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
            if (!Mathf.Approximately(mouseWheel, 0.0f))
            {
                Vector3 view = m_MainCamera.ScreenToViewportPoint(Input.mousePosition);
                if (view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f)
                    CameraController.Instance.Zoom(-mouseWheel * Time.deltaTime * 20.0f);
            }

            // ========== Combat Input (delegated to CombatController) ==========
            if (Input.GetKeyDown(KeyCode.Space) && m_CombatController != null)
            {
                m_CombatController.TryAttack();
            }

            // ========== Keyboard Shortcuts (UNCHANGED) ==========
            if (Input.GetKeyUp(KeyCode.I))
                UISystem.Instance.ToggleInventory();
        }

        void GoToRespawn()
        {
            m_Animator.ResetTrigger(m_HitParamID);
        
            if (m_CurrentSpawn != null)
            {
                transform.position = m_CurrentSpawn.transform.position;
            }

            m_CurrentState = LocalState.DEFAULT;
        
            m_Animator.SetTrigger(m_RespawnParamID);
        
            m_CharacterData.Stats.ChangeHealth(m_CharacterData.Stats.stats.health);

            // Reset combat state
            if (m_CombatController != null)
            {
                m_CombatController.ResetCombat();
            }

            // ========== RESTORED: Clear interactable on respawn ==========
            m_TargetInteractable = null;
        }

        public void SetNewRespawn(SpawnPoint point)
        {
            if (m_CurrentSpawn != null)
                m_CurrentSpawn.Deactivated();

            m_CurrentSpawn = point;
            m_CurrentSpawn.Activated();
        }

        /// <summary>
        /// RESTORED: Handle both InteractableObject and Loot interactions
        /// Originally handled click-to-interact, now called directly:
        /// - From LootUI when player clicks loot button
        /// - Can be extended for other InteractableObject types
        /// </summary>
        public void InteractWith(object obj)
        {
            // Handle Loot interaction (from LootUI)
            if (obj is Loot loot)
            {
                if (loot != null && loot.IsInteractable)
                {
                    loot.InteractWith(m_CharacterData);
                }
            }   
            // Handle InteractableObject interaction (legacy support)
            else if (obj is InteractableObject interactable)
            {
                if (interactable != null && interactable.IsInteractable)
                {
                    m_TargetCollider = interactable.GetComponentInChildren<Collider>();
                    m_TargetInteractable = interactable;
                }
            }
        }

        /// <summary>
        /// RESTORED: Check if player is close enough to interactable to trigger it
        /// </summary>
        void CheckInteractableRange()
        {
            if (m_TargetInteractable == null)
                return;

            Vector3 distance = m_TargetCollider.ClosestPointOnBounds(transform.position) - transform.position;

            if (distance.sqrMagnitude < 1.5f * 1.5f)
            {
                m_TargetInteractable.InteractWith(m_CharacterData);
                m_TargetInteractable = null;
            }
        }

        public void FootstepFrame()
        {
            Vector3 pos = transform.position;
        
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