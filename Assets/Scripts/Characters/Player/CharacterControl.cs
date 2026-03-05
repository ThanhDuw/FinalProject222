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
        // COMBAT REFACTOR: Attack frames are now handled by CombatController.
        AnimationControllerDispatcher.IFootstepFrameReceiver
    {
        public static CharacterControl Instance { get; protected set; }
    
        public float Speed = 10.0f;

        public CharacterData Data => m_CharacterData;
        public CharacterData CurrentTarget => m_CurrentTargetCharacterData;

        public Transform WeaponLocator;
    
        [Header("Audio")]
        public AudioClip[] SpurSoundClips;
    
        Animator m_Animator;
        CharacterController m_CharacterController;   // MOVEMENT REFACTOR: CharacterController-based movement
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
        InteractableObject m_TargetInteractable = null;
        Camera m_MainCamera;

        CharacterAudio m_CharacterAudio;
        CombatController m_CombatController;         // COMBAT REFACTOR: Dedicated combat controller

        // Legacy target highlighting layer.
        int m_TargetLayer;
        CharacterData m_CurrentTargetCharacterData = null;
        // Flag used by legacy click-to-attack flow to clear target after attack completes.
        bool m_ClearPostAttack = false;

        SpawnPoint m_CurrentSpawn = null;
    
        enum State
        {
            DEFAULT,
            HIT,
            ATTACKING
        }

        State m_CurrentState;

        Vector3 m_LastRaycastResult;

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
            m_Animator = GetComponentInChildren<Animator>();

            m_LastRaycastResult = transform.position;

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

            m_CurrentState = State.DEFAULT;

            m_CharacterAudio = GetComponent<CharacterAudio>();

            // COMBAT REFACTOR: Cache CombatController reference for attack input.
            m_CombatController = GetComponent<CombatController>();
        
            m_CharacterData.OnDamage += () =>
            {
                m_Animator.SetTrigger(m_HitParamID);
                m_CharacterAudio.Hit(transform.position);
            };
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 pos = transform.position;
        
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
        
            // MOVEMENT REFACTOR: WASD-based movement using CharacterController.
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Camera-relative movement for top-down control
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

            // Apply movement via CharacterController (no NavMeshAgent click-to-move)
            if (m_CharacterController != null && m_CharacterController.enabled)
            {
                Vector3 moveVelocity = moveDir * Speed * Time.deltaTime;
                m_CharacterController.Move(moveVelocity);
            }

            float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
            if (!Mathf.Approximately(mouseWheel, 0.0f))
            {
                Vector3 view = m_MainCamera.ScreenToViewportPoint(Input.mousePosition);
                if(view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f)
                    CameraController.Instance.Zoom(-mouseWheel * Time.deltaTime * 20.0f);
            }
        
            // Update animator speed parameter from input magnitude (not NavMeshAgent).
            m_Animator.SetFloat(m_SpeedParamID, inputMag);

            // COMBAT INPUT: Delegate attack to CombatController.
            if (Input.GetKeyDown(KeyCode.Space) && m_CombatController != null)
            {
                m_CombatController.TryAttack();
            }
        
            //Keyboard shortcuts
            if(Input.GetKeyUp(KeyCode.B))
                UISystem.Instance.ToggleInventory();
        }

        void GoToRespawn()
        {
            m_Animator.ResetTrigger(m_HitParamID);

            if (m_CurrentSpawn != null)
            {
                transform.position = m_CurrentSpawn.transform.position;
            }
            m_IsKO = false;

            m_CurrentTargetCharacterData = null;
            m_TargetInteractable = null;

            m_CurrentState = State.DEFAULT;
        
            m_Animator.SetTrigger(m_RespawnParamID);
        
            m_CharacterData.Stats.ChangeHealth(m_CharacterData.Stats.stats.health);
        }

        void SwitchHighlightedObject(HighlightableObject obj)
        {
            if(m_Highlighted != null) m_Highlighted.Dehighlight();

            m_Highlighted = obj;
            if(m_Highlighted != null) m_Highlighted.Highlight();
        }

        // Legacy click-to-move / click-to-attack helpers have been removed with the NavMesh refactor.

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
            // Generic interactable support (no NavMesh auto-move anymore, requires player to be in range)
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