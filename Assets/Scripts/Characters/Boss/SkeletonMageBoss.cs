using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using CreatorKitCode;
using CreatorKitCodeInternal;

namespace CreatorKitCode
{
    public class SkeletonMageBoss : MonoBehaviour
    {
        // ==================== ENUMS ====================
        public enum BossState { IDLE, CHASING, CASTING, DEAD }

        // ==================== REFERENCES ====================
        [Header("References")]
        [SerializeField] private Transform m_CastPoint;
        [SerializeField] private GameObject m_FireballPrefab;
        [SerializeField] private CharacterData m_CharacterData;
        private LightningStrikeController m_LightningController;
        private NavMeshAgent m_Agent;
        private Animator m_Animator;
        private CharacterData m_Target;

        // ==================== DETECTION ====================
        [Header("Detection")]
        [SerializeField] private float m_DetectionRadius = 15f;
        [SerializeField] private LayerMask m_PlayerLayer;

        // ==================== SKILL 1 — LIGHTNING STRIKE ====================
        [Header("Skill 1 — Lightning Strike")]
        [SerializeField] private float m_LightningRange = 12f;
        [SerializeField] private float m_LightningDamage = 35f;
        [SerializeField] private float m_LightningCastTime = 1.2f;
        [SerializeField] private float m_LightningCooldown = 6f;
        private float m_LightningCooldownTimer = 0f;

        // ==================== SKILL 2 — FIREBALL ====================
        [Header("Skill 2 — Fireball")]
        [SerializeField] private float m_FireballRange = 10f;
        [SerializeField] private float m_FireballCastTime = 0.8f;
        [SerializeField] private float m_FireballCooldown = 4f;
        private float m_FireballCooldownTimer = 0f;

        // ==================== RUNTIME STATE ====================
        private BossState m_CurrentState = BossState.IDLE;
        private bool m_IsCasting = false;

        // ==================== ANIMATOR PARAMS (CONSTANTS) ====================
        private static readonly int ANIM_SPEED   = Animator.StringToHash("Speed");
        private static readonly int ANIM_SKILL1  = Animator.StringToHash("Skill1");
        private static readonly int ANIM_SKILL2  = Animator.StringToHash("Skill2");
        private static readonly int ANIM_DEATH   = Animator.StringToHash("Death");
        private static readonly int ANIM_HIT     = Animator.StringToHash("Hit");

        // =============================================================
private void Awake()
        {
            m_Agent               = GetComponent<NavMeshAgent>();
            m_Animator            = GetComponent<Animator>();
            m_CharacterData       = GetComponent<CharacterData>();
            m_LightningController = GetComponent<LightningStrikeController>();
        }

private void Start() { }

private void OnDestroy() { }

        // ==================== UPDATE LOOP ====================
private void Update()
        {
            if (m_CurrentState == BossState.DEAD) return;

            // Poll death — CharacterData has no OnDeath event
            if (m_CharacterData != null && m_CharacterData.Stats.CurrentHealth <= 0)
            {
                HandleDeath();
                return;
            }

            if (m_IsCasting) return;

            TickCooldowns();

            switch (m_CurrentState)
            {
                case BossState.IDLE:    UpdateIdle();    break;
                case BossState.CHASING: UpdateChasing(); break;
            }
        }

        private void TickCooldowns()
        {
            if (m_LightningCooldownTimer > 0f) m_LightningCooldownTimer -= Time.deltaTime;
            if (m_FireballCooldownTimer  > 0f) m_FireballCooldownTimer  -= Time.deltaTime;
        }

        // ==================== STATE MACHINE ====================
        private void UpdateIdle()
        {
            m_Target = DetectPlayer();
            if (m_Target != null)
                TransitionTo(BossState.CHASING);
        }

        private void UpdateChasing()
        {
            if (m_Target == null || m_Target.gameObject == null)
            {
                TransitionTo(BossState.IDLE);
                return;
            }

            float dist = Vector3.Distance(transform.position, m_Target.transform.position);

            // --- Check skill range ---
            bool canLightning = m_LightningCooldownTimer <= 0f && dist <= m_LightningRange;
            bool canFireball  = m_FireballCooldownTimer  <= 0f && dist <= m_FireballRange;

            if (canLightning || canFireball)
            {
                m_Agent.ResetPath();
                m_Animator.SetFloat(ANIM_SPEED, 0f);
                TransitionTo(BossState.CASTING);
                StartCoroutine(CastSkill(canLightning ? 1 : 2));
                return;
            }

            // --- Chase player ---
            m_Agent.SetDestination(m_Target.transform.position);
            float speed = m_Agent.velocity.magnitude;
            m_Animator.SetFloat(ANIM_SPEED, speed, 0.1f, Time.deltaTime);
        }

        // ==================== SKILL CASTING ====================
        private IEnumerator CastSkill(int skillIndex)
        {
            m_IsCasting = true;
            float castTime = skillIndex == 1 ? m_LightningCastTime : m_FireballCastTime;

            m_Animator.SetTrigger(skillIndex == 1 ? ANIM_SKILL1 : ANIM_SKILL2);

            yield return new WaitForSeconds(castTime);

            if (m_CurrentState != BossState.DEAD)
            {
                if (skillIndex == 1) ExecuteLightningStrike();
                else                 ExecuteFireball();
            }

            m_IsCasting = false;
            TransitionTo(BossState.CHASING);
        }

private void ExecuteLightningStrike()
        {
            if (m_Target == null) return;

            if (m_LightningController != null)
            {
                // Delegate toan bo trinh tu strike cho LightningStrikeController
                m_LightningController.ExecuteSequence(m_Target.transform.position, m_CharacterData);
            }
            else
            {
                // Fallback neu khong co controller: chi gay 1 hit don gian
                int dmg = Mathf.RoundToInt(m_LightningDamage);
                m_Target.Stats.ChangeHealth(-dmg);
                DamageUI.Instance.NewDamage(dmg, m_Target.transform.position);
            }

            m_LightningCooldownTimer = m_LightningCooldown;
        }

        private void ExecuteFireball()
        {
            if (m_FireballPrefab == null || m_CastPoint == null) return;

            GameObject fb = Instantiate(m_FireballPrefab, m_CastPoint.position, Quaternion.identity);
            FireballProjectile proj = fb.GetComponent<FireballProjectile>();
            if (proj != null)
                proj.Launch(m_Target.transform, m_CharacterData);

            m_FireballCooldownTimer = m_FireballCooldown;
        }

        // ==================== DETECTION ====================
        private CharacterData DetectPlayer()
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, m_DetectionRadius, m_PlayerLayer);
            if (cols.Length == 0) return null;
            return cols[0].GetComponent<CharacterData>();
        }

        // ==================== TRANSITIONS ====================
        private void TransitionTo(BossState newState)
        {
            m_CurrentState = newState;

            if (newState == BossState.IDLE)
            {
                m_Agent.ResetPath();
                m_Animator.SetFloat(ANIM_SPEED, 0f);
            }
        }

        // ==================== DEATH ====================
        private void HandleDeath()
        {
            if (m_CurrentState == BossState.DEAD) return;

            m_CurrentState = BossState.DEAD;
            m_IsCasting    = false;
            StopAllCoroutines();

            m_Agent.enabled = false;
            m_Animator.SetTrigger(ANIM_DEATH);

            GetComponent<Collider>().enabled = false;
            Destroy(gameObject, 5f);
        }

        // ==================== GIZMOS ====================
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_DetectionRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, m_LightningRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_FireballRange);
        }
    }
}
