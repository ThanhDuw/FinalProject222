using CreatorKitCode;
using UnityEngine;

namespace CreatorKitCodeInternal {
    public class CombatController : MonoBehaviour, 
        AnimationControllerDispatcher.IAttackFrameReceiver
    {
        [Header("Combat Settings")]
        [SerializeField] private float m_AttackConeAngle = 60f;  // 60° cone attack
        [SerializeField] private float m_LungeDistance = 0.5f;   // Forward movement during attack
        [SerializeField] private float m_KnockbackForce = 2f;    // Enemy knockback magnitude
        [SerializeField] private float m_SearchRadius = 8f;      // How far to search for weapon range

        private Animator m_Animator;
        private CharacterData m_CharacterData;
        private CharacterAudio m_CharacterAudio;
        private CharacterController m_CharacterController;

        private int m_AttackParamID;
        private CombatState m_CurrentState = CombatState.Idle;
        
        // Timing
        private float m_AttackTimer = 0f;
        private float m_WindUpDuration = 0f;
        private float m_ActiveDuration = 0f;
        private float m_RecoveryDuration = 0f;

        // Combo system
        private bool m_InputBuffered = false;
        private bool m_CanCombo = false;
        private int m_ComboHits = 0;
        private const int MAX_COMBO_HITS = 2;

        // For gizmo debugging
        private bool m_DrawDebugCone = true;
        private Vector3 m_LastAttackPos = Vector3.zero;

        // ========== NEW: Target tracking for UISystem ==========
        private CharacterData m_CurrentTarget = null;

        public CharacterData CurrentTarget => m_CurrentTarget;

        void Awake()
        {
            m_Animator = GetComponentInChildren<Animator>();
            m_CharacterData = GetComponent<CharacterData>();
            m_CharacterAudio = GetComponent<CharacterAudio>();
            m_CharacterController = GetComponent<CharacterController>();

            m_AttackParamID = Animator.StringToHash("Attack");
        }

        void Update()
        {
            UpdateCombatState();
            UpdateTargetTracking();
        }

        void LateUpdate()
        {
            // Reset input buffer after processing
            m_InputBuffered = false;
        }

        /// <summary>
        /// Track current target for UISystem display
        /// </summary>
        private void UpdateTargetTracking()
        {
            // Clear target if dead
            if (m_CurrentTarget != null && m_CurrentTarget.Stats.CurrentHealth <= 0)
            {
                m_CurrentTarget = null;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!m_DrawDebugCone || m_CurrentState == CombatState.Idle)
                return;

            // Draw attack cone visualization
            Vector3 attackPos = transform.position + transform.forward * 1f;
            float weaponRange = GetCurrentWeaponRange();
            float halfAngle = m_AttackConeAngle * 0.5f;

            Gizmos.color = Color.red;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * transform.forward;

            Gizmos.DrawLine(transform.position, transform.position + leftDir * weaponRange);
            Gizmos.DrawLine(transform.position, transform.position + rightDir * weaponRange);
            Gizmos.DrawLine(transform.position + leftDir * weaponRange, 
                           transform.position + rightDir * weaponRange);

            // Draw search sphere
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            DrawWireSphere(attackPos, weaponRange, 8);
        }

        private void DrawWireSphere(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 lastPoint = center + Vector3.forward * radius;

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                Gizmos.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
            }
        }

        /// <summary>
        /// Called from CharacterControl when Space is pressed
        /// </summary>
        public void TryAttack()
        {
            if (m_CurrentState == CombatState.Idle)
            {
                StartAttack();
            }
            else if (m_CurrentState == CombatState.Recovery && m_CanCombo)
            {
                // Buffer input during recovery window for combo
                m_InputBuffered = true;
            }
        }

        /// <summary>
        /// Start a new attack sequence
        /// </summary>
        private void StartAttack()
        {
            // Get weapon attack timing from equipment
            float weaponSpeed = GetCurrentWeaponSpeed();
            
            // Scale timings based on weapon speed (1.0 = normal speed)
            m_WindUpDuration = 0.2f / weaponSpeed;      // 200ms at 1.0 speed
            m_ActiveDuration = 0.3f / weaponSpeed;       // 300ms at 1.0 speed
            m_RecoveryDuration = 0.5f / weaponSpeed;     // 500ms at 1.0 speed

            m_AttackTimer = 0f;
            m_CurrentState = CombatState.WindUp;
            m_CanCombo = false;
            m_ComboHits++;

            // Trigger attack animation
            m_Animator.SetTrigger(m_AttackParamID);

            // Play attack vocalization
            if (m_CharacterAudio != null)
            {
                m_CharacterAudio.Attack(transform.position);
            }
        }

        /// <summary>
        /// Update combat state machine each frame
        /// </summary>
        private void UpdateCombatState()
        {
            if (m_CurrentState == CombatState.Idle)
                return;

            m_AttackTimer += Time.deltaTime;

            switch (m_CurrentState)
            {
                case CombatState.WindUp:
                    UpdateWindUp();
                    break;

                case CombatState.Active:
                    UpdateActive();
                    break;

                case CombatState.Recovery:
                    UpdateRecovery();
                    break;
            }
        }

        private void UpdateWindUp()
        {
            // Apply slight forward lunge during wind-up
            Vector3 lungeVelocity = transform.forward * (m_LungeDistance / m_WindUpDuration) * Time.deltaTime;
            if (m_CharacterController != null && m_CharacterController.enabled)
            {
                m_CharacterController.Move(lungeVelocity);
            }

            if (m_AttackTimer >= m_WindUpDuration)
            {
                m_CurrentState = CombatState.Active;
                m_AttackTimer = 0f;
            }
        }

        private void UpdateActive()
        {
            // Active phase: waiting for animation event (AttackFrame) to deal damage
            if (m_AttackTimer >= m_ActiveDuration)
            {
                m_CurrentState = CombatState.Recovery;
                m_AttackTimer = 0f;
                m_CanCombo = true;  // Allow combo input during recovery window
            }
        }

        private void UpdateRecovery()
        {
            if (m_AttackTimer >= m_RecoveryDuration)
            {
                // Check if combo was buffered
                if (m_InputBuffered && m_ComboHits < MAX_COMBO_HITS)
                {
                    m_InputBuffered = false;
                    StartAttack();
                }
                else
                {
                    // Return to idle
                    m_CurrentState = CombatState.Idle;
                    m_ComboHits = 0;
                    m_CanCombo = false;
                }
            }
        }

        /// <summary>
        /// Called by animation event (same as old AttackFrame)
        /// Performs damage check using cone attack + overlap sphere
        /// </summary>
        public void AttackFrame()
        {
            if (m_CurrentState != CombatState.Active)
                return;

            if (m_CharacterData == null || m_CharacterData.Equipment.Weapon == null)
                return;

            Vector3 attackOrigin = transform.position + Vector3.up * 0.5f;
            float weaponRange = GetCurrentWeaponRange();

            // Find all potential targets in attack range on Target layer
            int targetLayer = LayerMask.NameToLayer("Target");
            Collider[] hits = Physics.OverlapSphere(attackOrigin, weaponRange, 1 << targetLayer);

            CharacterData nearestTarget = null;
            float bestDistance = float.MaxValue;
            float halfConeAngle = m_AttackConeAngle * 0.5f;

            for (int i = 0; i < hits.Length; i++)
            {
                CharacterData target = hits[i].GetComponentInParent<CharacterData>();
                
                if (target == null || target == m_CharacterData)
                    continue;

                if (target.Stats.CurrentHealth <= 0)
                    continue;

                Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

                // Check if target is within cone
                if (angleToTarget <= halfConeAngle)
                {
                    float distance = Vector3.Distance(transform.position, target.transform.position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearestTarget = target;
                    }
                }
            }

            // Apply damage to nearest target in cone
            if (nearestTarget != null)
            {
                m_CharacterData.Attack(nearestTarget);

                Vector3 hitPos = nearestTarget.transform.position + Vector3.up * 0.5f;
                VFXManager.PlayVFX(VFXType.Hit, hitPos);
                
                // Get hit sound and play with correct SFXManager.Use type
                AudioClip hitSound = m_CharacterData.Equipment.Weapon.GetHitSound();
                SFXManager.PlaySound(m_CharacterAudio.UseType, 
                    new SFXManager.PlayData() 
                    { 
                        Clip = hitSound, 
                        PitchMin = 0.8f, 
                        PitchMax = 1.2f, 
                        Position = hitPos 
                    });

                // Apply knockback
                ApplyKnockback(nearestTarget);

                // ========== NEW: Track current target for UI ==========
                m_CurrentTarget = nearestTarget;
            }

            m_LastAttackPos = attackOrigin;
        }

        private void ApplyKnockback(CharacterData target)
        {
            if (target == null)
                return;

            CharacterController targetController = target.GetComponent<CharacterController>();
            if (targetController != null && targetController.enabled)
            {
                Vector3 knockbackDir = (target.transform.position - transform.position).normalized;
                targetController.Move(knockbackDir * m_KnockbackForce * Time.deltaTime);
            }
        }
            
        /// <summary>
        /// Reset combat state (called on respawn/death)
        /// </summary>
        public void ResetCombat()
        {
            m_CurrentState = CombatState.Idle;
            m_AttackTimer = 0f;
            m_InputBuffered = false;
            m_CanCombo = false;
            m_ComboHits = 0;
            m_CurrentTarget = null;
        }

        /// <summary>
        /// Get weapon speed from Weapon.Stats.Speed (float)
        /// </summary>
        private float GetCurrentWeaponSpeed()
        {
            if (m_CharacterData == null || m_CharacterData.Equipment.Weapon == null)
                return 1f;

            return m_CharacterData.Equipment.Weapon.Stats.Speed;
        }

        /// <summary>
        /// Get weapon range from Weapon.Stats.MaxRange (float)
        /// </summary>
        private float GetCurrentWeaponRange()
        {
            if (m_CharacterData == null || m_CharacterData.Equipment.Weapon == null)
                return 2f;

            return m_CharacterData.Equipment.Weapon.Stats.MaxRange;
        }

        // Getters for debugging/UI
        public CombatState CurrentState => m_CurrentState;
        public bool IsAttacking => m_CurrentState != CombatState.Idle;
        public int ComboCount => m_ComboHits;
    }
}
