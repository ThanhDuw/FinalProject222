using CreatorKitCode;
using UnityEngine;

namespace CreatorKitCodeInternal {
    public class CombatController : MonoBehaviour, 
        AnimationControllerDispatcher.IAttackFrameReceiver
    {
        [Header("Tuned Combat Settings")]
        [SerializeField] private float attackConeAngle = 70f;          // Full cone angle (degrees)
        [SerializeField] private float sweepRadiusMultiplier = 0.2f;   // Sweep radius relative to weapon range
        [SerializeField] private float lungeDistance = 0.6f;           // Forward movement during wind-up
        [SerializeField] private LayerMask attackLayerMask;            // Layers that can be hit
        [SerializeField] private bool debugDraw = true;                // Toggle debug gizmos & rays

        [Header("References")]
        [SerializeField] private Transform weaponLocator;              // Optional explicit weapon origin
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
        private Vector3 m_LastAttackPos = Vector3.zero;

        // Non-alloc buffers to reduce GC allocations
        private readonly RaycastHit[] m_SphereCastHits = new RaycastHit[8];
        private readonly Collider[] m_OverlapHits = new Collider[8];

        // ========== NEW: Target tracking for UISystem ==========
        private CharacterData m_CurrentTarget = null;

        public CharacterData CurrentTarget => m_CurrentTarget;

        // Cache transform to avoid repeated property access
        private Transform m_Transform;

        void Awake()
        {
            m_Transform = transform;
            m_Animator = GetComponentInChildren<Animator>();
            m_CharacterData = GetComponent<CharacterData>();
            m_CharacterAudio = GetComponent<CharacterAudio>();
            m_CharacterController = GetComponent<CharacterController>();

            m_AttackParamID = Animator.StringToHash("Attack");

            // Default attack layer: "Target"
            if (attackLayerMask == 0)
            {
                attackLayerMask = LayerMask.GetMask("Target");
            }

            // Try to auto-find weapon locator
            if (weaponLocator == null)
            {
                // Prefer CharacterControl's WeaponLocator if present
                var control = GetComponent<CharacterControl>();
                if (control != null && control.WeaponLocator != null)
                {
                    weaponLocator = control.WeaponLocator;
                }
                else
                {
                    Transform found = m_Transform.Find("LocatorWeapon");
                    if (found != null)
                        weaponLocator = found;
                }
            }
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
            if (!debugDraw)
                return;

            // Draw attack cone visualization
            Vector3 attackPos = Application.isPlaying ? GetAttackOrigin() : m_Transform.position + m_Transform.forward * 1f;
            float weaponRange = GetCurrentWeaponRange();
            float halfAngle = attackConeAngle * 0.5f;

            Gizmos.color = Color.red;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * m_Transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * m_Transform.forward;

            Gizmos.DrawLine(m_Transform.position, m_Transform.position + leftDir * weaponRange);
            Gizmos.DrawLine(m_Transform.position, m_Transform.position + rightDir * weaponRange);
            Gizmos.DrawLine(m_Transform.position + leftDir * weaponRange, 
                           m_Transform.position + rightDir * weaponRange);

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
        /// Compute the origin of the attack sweep.
        /// Prefer an explicit weapon locator if available, otherwise fall back to body center.
        /// </summary>
        private Vector3 GetAttackOrigin()
        {
            if (weaponLocator != null)
                return weaponLocator.position;

            return m_Transform.position + Vector3.up * 0.5f;
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
        /// Try attack at a specific target (click-to-attack).
        /// </summary>
        public void TryAttackAt(CharacterData target)
        {
            if (target == null)
                return;
            if (target == m_CharacterData)
                return;
            if (target.Stats.CurrentHealth <= 0)
                return;

            m_CurrentTarget = target;
            TryAttack();
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
                m_CharacterAudio.Attack(m_Transform.position);
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
            Vector3 lungeVelocity = m_Transform.forward * (lungeDistance / m_WindUpDuration) * Time.deltaTime;
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
        /// Performs damage check using sweep-based cone attack to better match melee swings.
        /// </summary>
        public void AttackFrame()
        {
            if (m_CurrentState != CombatState.Active)
                return;

            if (m_CharacterData == null || m_CharacterData.Equipment.Weapon == null)
                return;

            Vector3 origin = GetAttackOrigin();
            float weaponRange = GetCurrentWeaponRange();

            // If a specific target was requested (click-to-attack), prefer it when in range
            if (m_CurrentTarget != null && m_CurrentTarget.Stats.CurrentHealth > 0)
            {
                Vector3 toTarget = m_CurrentTarget.transform.position - m_Transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude <= weaponRange * weaponRange)
                {
                    m_CharacterData.Attack(m_CurrentTarget);

                    Vector3 hitPos = m_CurrentTarget.transform.position + Vector3.up * 0.5f;
                    VFXManager.PlayVFX(VFXType.Hit, hitPos);
                    
                    AudioClip hitSound = m_CharacterData.Equipment.Weapon.GetHitSound();
                    SFXManager.PlaySound(m_CharacterAudio.UseType, 
                        new SFXManager.PlayData() 
                        { 
                            Clip = hitSound, 
                            PitchMin = 0.8f, 
                            PitchMax = 1.2f, 
                            Position = hitPos 
                        });

                    ApplyKnockback(m_CurrentTarget);

                    // Clear requested target after hit
                    m_CurrentTarget = null;
                    m_LastAttackPos = origin;
                    return;
                }
                else
                {
                    // Target out of range - clear requested target so fallback detection can run
                    m_CurrentTarget = null;
                }
            }

            // Debug visualization of direction & origin
            if (debugDraw)
            {
                Debug.DrawRay(origin, m_Transform.forward * weaponRange, Color.red, 0.25f);

                const int segments = 16;
                float step = 360f / segments;
                Vector3 prev = origin + new Vector3(0f, 0f, weaponRange);
                for (int i = 1; i <= segments; ++i)
                {
                    float angleRad = step * i * Mathf.Deg2Rad;
                    Vector3 next = origin + new Vector3(Mathf.Sin(angleRad) * weaponRange, 0f, Mathf.Cos(angleRad) * weaponRange);
                    Debug.DrawLine(prev, next, Color.yellow, 0.25f);
                    prev = next;
                }
            }

            // Flattened forward for cone calculations (ignore Y)
            Vector3 forwardFlat = m_Transform.forward;
            forwardFlat.y = 0f;
            if (forwardFlat.sqrMagnitude > 0.0001f)
                forwardFlat.Normalize();

            float halfCone = attackConeAngle * 0.5f;
            float sweepRadius = weaponRange * sweepRadiusMultiplier;

            CharacterData bestTarget = null;
            float bestDistanceSq = float.MaxValue;

            // Local method to evaluate a potential target without allocations
            void ProcessTarget(CharacterData target)
            {
                if (target == null || target == m_CharacterData)
                    return;

                if (target.Stats.CurrentHealth <= 0)
                    return;

                Vector3 toTarget = target.transform.position - m_Transform.position;
                toTarget.y = 0f;

                float distSq = toTarget.sqrMagnitude;
                if (distSq < 0.0001f)
                    return;

                Vector3 dirToTarget = toTarget / Mathf.Sqrt(distSq);
                float angle = Vector3.Angle(forwardFlat, dirToTarget);
                if (angle > halfCone)
                    return;

                if (distSq < bestDistanceSq)
                {
                    bestDistanceSq = distSq;
                    bestTarget = target;
                }
            }

            // 1) Sweep-based detection along the swing path
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                sweepRadius,
                m_Transform.forward,
                m_SphereCastHits,
                weaponRange,
                attackLayerMask);

            for (int i = 0; i < hitCount; ++i)
            {
                Collider col = m_SphereCastHits[i].collider;
                if (col == null)
                    continue;

                CharacterData target = col.GetComponentInParent<CharacterData>();
                ProcessTarget(target);
            }

            // 2) Fallback: instant OverlapSphere to avoid "ghost misses"
            if (bestTarget == null)
            {
                int overlapCount = Physics.OverlapSphereNonAlloc(
                    origin,
                    weaponRange,
                    m_OverlapHits,
                    attackLayerMask);

                for (int i = 0; i < overlapCount; ++i)
                {
                    Collider col = m_OverlapHits[i];
                    if (col == null)
                        continue;

                    CharacterData target = col.GetComponentInParent<CharacterData>();
                    ProcessTarget(target);
                }
            }

            // Apply damage to nearest target in cone
            if (bestTarget != null)
            {
                m_CharacterData.Attack(bestTarget);

                Vector3 hitPos = bestTarget.transform.position + Vector3.up * 0.5f;
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
                ApplyKnockback(bestTarget);

                // ========== NEW: Track current target for UI ==========
                m_CurrentTarget = bestTarget;
            }

            m_LastAttackPos = origin;
        }

        private void ApplyKnockback(CharacterData target)
        {
            if (target == null)
                return;

            CharacterController targetController = target.GetComponent<CharacterController>();
            if (targetController != null && targetController.enabled)
            {
                Vector3 knockbackDir = (target.transform.position - m_Transform.position).normalized;
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