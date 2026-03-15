using CreatorKitCode;
using UnityEngine;

namespace CreatorKitCodeInternal
{
    public class CombatController : MonoBehaviour,
        AnimationControllerDispatcher.IAttackFrameReceiver
    {
        [Header("Tuned Combat Settings")]
        [SerializeField] private float attackConeAngle = 70f;
        [SerializeField] private float sweepRadiusMultiplier = 0.2f;
        [SerializeField] private float lungeDistance = 0.6f;
        [SerializeField] private LayerMask attackLayerMask;
        [SerializeField] private bool debugDraw = true;

        [Header("References")]
        [SerializeField] private Transform weaponLocator;
        [SerializeField] private float m_KnockbackForce = 2f;

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

        // Input buffer — allows queuing next attack during recovery
        private bool m_InputBuffered = false;

        // For gizmo debugging
        private Vector3 m_LastAttackPos = Vector3.zero;

        // Non-alloc buffers to reduce GC allocations
        private readonly RaycastHit[] m_SphereCastHits = new RaycastHit[8];
        private readonly Collider[] m_OverlapHits = new Collider[8];

        // Target tracking for UISystem
        private CharacterData m_CurrentTarget = null;
        public CharacterData CurrentTarget => m_CurrentTarget;

        private Transform m_Transform;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        void Awake()
        {
            m_Transform = transform;
            m_Animator = GetComponentInChildren<Animator>();
            m_CharacterData = GetComponent<CharacterData>();
            m_CharacterAudio = GetComponent<CharacterAudio>();
            m_CharacterController = GetComponent<CharacterController>();

            m_AttackParamID = Animator.StringToHash("Attack");

            if (attackLayerMask == 0)
                attackLayerMask = LayerMask.GetMask("Target");

            if (weaponLocator == null)
            {
                var control = GetComponent<CharacterControl>();
                if (control != null && control.WeaponLocator != null)
                    weaponLocator = control.WeaponLocator;
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
            // Reset input buffer after processing each frame
            m_InputBuffered = false;
        }

        // ── Target Tracking ───────────────────────────────────────────────────

        private void UpdateTargetTracking()
        {
            if (m_CurrentTarget != null && m_CurrentTarget.Stats.CurrentHealth <= 0)
                m_CurrentTarget = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Request an attack. If Idle, starts immediately.
        /// If in Recovery, buffers the input so the next attack fires as soon
        /// as the recovery window ends — enabling continuous attacking.
        /// </summary>
        public void TryAttack()
        {
            if (m_CurrentState == CombatState.Idle)
                StartAttack();
            else if (m_CurrentState == CombatState.Recovery)
                m_InputBuffered = true;
        }

        /// <summary>
        /// Click-to-attack on a specific target.
        /// </summary>
        public void TryAttackAt(CharacterData target)
        {
            if (target == null) return;
            if (target == m_CharacterData) return;
            if (target.Stats.CurrentHealth <= 0) return;

            m_CurrentTarget = target;
            TryAttack();
        }

        /// <summary>
        /// Reset combat state (called on respawn / death).
        /// </summary>
        public void ResetCombat()
        {
            m_CurrentState = CombatState.Idle;
            m_AttackTimer = 0f;
            m_InputBuffered = false;
            m_CurrentTarget = null;
        }

        // ── State Machine ─────────────────────────────────────────────────────

        private void StartAttack()
        {
            float weaponSpeed = GetCurrentWeaponSpeed();

            m_WindUpDuration   = 0.2f / weaponSpeed;
            m_ActiveDuration   = 0.3f / weaponSpeed;
            m_RecoveryDuration = 0.5f / weaponSpeed;

            m_AttackTimer  = 0f;
            m_CurrentState = CombatState.WindUp;

            m_Animator.SetTrigger(m_AttackParamID);

            if (m_CharacterAudio != null)
                m_CharacterAudio.Attack(m_Transform.position);
        }

        private void UpdateCombatState()
        {
            if (m_CurrentState == CombatState.Idle) return;

            m_AttackTimer += Time.deltaTime;

            switch (m_CurrentState)
            {
                case CombatState.WindUp:    UpdateWindUp();    break;
                case CombatState.Active:    UpdateActive();    break;
                case CombatState.Recovery:  UpdateRecovery();  break;
            }
        }

        private void UpdateWindUp()
        {
            // Slight forward lunge during wind-up
            Vector3 lunge = m_Transform.forward * (lungeDistance / m_WindUpDuration) * Time.deltaTime;
            if (m_CharacterController != null && m_CharacterController.enabled)
                m_CharacterController.Move(lunge);

            if (m_AttackTimer >= m_WindUpDuration)
            {
                m_CurrentState = CombatState.Active;
                m_AttackTimer  = 0f;
            }
        }

        private void UpdateActive()
        {
            // Waiting for AttackFrame animation event to deal damage
            if (m_AttackTimer >= m_ActiveDuration)
            {
                m_CurrentState = CombatState.Recovery;
                m_AttackTimer  = 0f;
            }
        }

        private void UpdateRecovery()
        {
            if (m_AttackTimer >= m_RecoveryDuration)
            {
                if (m_InputBuffered)
                {
                    // Chained attack — start next attack immediately
                    m_InputBuffered = false;
                    StartAttack();
                }
                else
                {
                    // Return to Idle — player can attack again instantly
                    m_CurrentState = CombatState.Idle;
                }
            }
        }

        // ── Attack Frame (Animation Event) ────────────────────────────────────

        /// <summary>
        /// Called by animation event. Performs damage check using cone sweep.
        /// </summary>
        public void AttackFrame()
        {
            if (m_CurrentState != CombatState.Active) return;
            if (m_CharacterData == null || m_CharacterData.Equipment.Weapon == null) return;

            Vector3 origin      = GetAttackOrigin();
            float   weaponRange = GetCurrentWeaponRange();

            // Prefer explicit click target when in range
            if (m_CurrentTarget != null && m_CurrentTarget.Stats.CurrentHealth > 0)
            {
                Vector3 toTarget = m_CurrentTarget.transform.position - m_Transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude <= weaponRange * weaponRange)
                {
                    ApplyHit(m_CurrentTarget, origin);
                    m_CurrentTarget  = null;
                    m_LastAttackPos  = origin;
                    return;
                }

                m_CurrentTarget = null;
            }

            // Debug visualization
            if (debugDraw)
            {
                Debug.DrawRay(origin, m_Transform.forward * weaponRange, Color.red, 0.25f);

                const int segments = 16;
                float step = 360f / segments;
                Vector3 prev = origin + new Vector3(0f, 0f, weaponRange);
                for (int i = 1; i <= segments; ++i)
                {
                    float rad  = step * i * Mathf.Deg2Rad;
                    Vector3 next = origin + new Vector3(Mathf.Sin(rad) * weaponRange, 0f, Mathf.Cos(rad) * weaponRange);
                    Debug.DrawLine(prev, next, Color.yellow, 0.25f);
                    prev = next;
                }
            }

            Vector3 forwardFlat = m_Transform.forward;
            forwardFlat.y = 0f;
            if (forwardFlat.sqrMagnitude > 0.0001f)
                forwardFlat.Normalize();

            float halfCone   = attackConeAngle * 0.5f;
            float sweepRadius = weaponRange * sweepRadiusMultiplier;

            CharacterData bestTarget   = null;
            float         bestDistSq   = float.MaxValue;

            void ProcessTarget(CharacterData target)
            {
                if (target == null || target == m_CharacterData) return;
                if (target.Stats.CurrentHealth <= 0) return;

                Vector3 toTarget = target.transform.position - m_Transform.position;
                toTarget.y = 0f;

                float distSq = toTarget.sqrMagnitude;
                if (distSq < 0.0001f) return;

                Vector3 dir   = toTarget / Mathf.Sqrt(distSq);
                float   angle = Vector3.Angle(forwardFlat, dir);
                if (angle > halfCone) return;

                if (distSq < bestDistSq)
                {
                    bestDistSq  = distSq;
                    bestTarget  = target;
                }
            }

            // Sweep-based detection
            int hitCount = Physics.SphereCastNonAlloc(
                origin, sweepRadius, m_Transform.forward,
                m_SphereCastHits, weaponRange, attackLayerMask);

            for (int i = 0; i < hitCount; ++i)
            {
                Collider col = m_SphereCastHits[i].collider;
                if (col != null)
                    ProcessTarget(col.GetComponentInParent<CharacterData>());
            }

            // Fallback overlap to avoid ghost misses
            if (bestTarget == null)
            {
                int overlapCount = Physics.OverlapSphereNonAlloc(
                    origin, weaponRange, m_OverlapHits, attackLayerMask);

                for (int i = 0; i < overlapCount; ++i)
                {
                    Collider col = m_OverlapHits[i];
                    if (col != null)
                        ProcessTarget(col.GetComponentInParent<CharacterData>());
                }
            }

            if (bestTarget != null)
            {
                ApplyHit(bestTarget, origin);
                m_CurrentTarget = bestTarget;
            }

            m_LastAttackPos = origin;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ApplyHit(CharacterData target, Vector3 origin)
        {
            m_CharacterData.Attack(target);

            Vector3    hitPos   = target.transform.position + Vector3.up * 0.5f;
            AudioClip  hitSound = m_CharacterData.Equipment.Weapon.GetHitSound();

            VFXManager.PlayVFX(VFXType.Hit, hitPos);
            SFXManager.PlaySound(m_CharacterAudio.UseType,
                new SFXManager.PlayData { Clip = hitSound, PitchMin = 0.8f, PitchMax = 1.2f, Position = hitPos });

            ApplyKnockback(target);
        }

        private void ApplyKnockback(CharacterData target)
        {
            if (target == null) return;
            CharacterController cc = target.GetComponent<CharacterController>();
            if (cc != null && cc.enabled)
            {
                Vector3 dir = (target.transform.position - m_Transform.position).normalized;
                cc.Move(dir * m_KnockbackForce * Time.deltaTime);
            }
        }

        private Vector3 GetAttackOrigin()
        {
            if (weaponLocator != null) return weaponLocator.position;
            return m_Transform.position + Vector3.up * 0.5f;
        }

        private float GetCurrentWeaponSpeed()
        {
            if (m_CharacterData == null || m_CharacterData.Equipment.Weapon == null) return 1f;
            return m_CharacterData.Equipment.Weapon.Stats.Speed;
        }

        private float GetCurrentWeaponRange()
        {
            if (m_CharacterData == null || m_CharacterData.Equipment.Weapon == null) return 2f;
            return m_CharacterData.Equipment.Weapon.Stats.MaxRange;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        void OnDrawGizmosSelected()
        {
            if (!debugDraw) return;

            Transform t = m_Transform != null ? m_Transform : transform;

            Vector3 attackPos   = Application.isPlaying ? GetAttackOrigin() : t.position + t.forward * 1f;
            float   weaponRange = GetCurrentWeaponRange();
            float   halfAngle   = attackConeAngle * 0.5f;

            Gizmos.color = Color.red;
            Vector3 leftDir  = Quaternion.Euler(0, -halfAngle, 0) * t.forward;
            Vector3 rightDir = Quaternion.Euler(0,  halfAngle, 0) * t.forward;
            Gizmos.DrawLine(t.position, t.position + leftDir  * weaponRange);
            Gizmos.DrawLine(t.position, t.position + rightDir * weaponRange);
            Gizmos.DrawLine(t.position + leftDir * weaponRange, t.position + rightDir * weaponRange);

            Gizmos.color = new Color(1, 1, 0, 0.3f);
            DrawWireSphere(attackPos, weaponRange, 8);
        }

        private void DrawWireSphere(Vector3 center, float radius, int segments)
        {
            float   angleStep = 360f / segments;
            Vector3 lastPoint = center + Vector3.forward * radius;
            for (int i = 1; i <= segments; i++)
            {
                float   angle    = angleStep * i * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                Gizmos.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
            }
        }

        // ── Status Getters ────────────────────────────────────────────────────

        public CombatState CurrentState => m_CurrentState;
        public bool IsAttacking => m_CurrentState != CombatState.Idle;
    }
}
