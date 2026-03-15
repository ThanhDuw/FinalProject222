using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using CreatorKitCode;
using CreatorKitCodeInternal;

namespace CreatorKitCode
{
    public class SkeletonMageBoss : MonoBehaviour
    {
        public enum BossState { IDLE, CHASING, CASTING, DEAD }

        // ==================== REFERENCES ====================
        [Header("References")]
        [SerializeField] private Transform     m_CastPoint;
        [SerializeField] private GameObject    m_DarkMagicPrefab;
        [SerializeField] private GameObject    m_DarkMagicWarningPrefab;
        [SerializeField] private CharacterData m_CharacterData;
        private LightningStrikeController m_LightningController;
        private NavMeshAgent  m_Agent;
        private Animator      m_Animator;
        private CharacterData m_Target;

        // ==================== DETECTION ====================
        [Header("Detection")]
        [SerializeField] private float     m_DetectionRadius = 15f;
        [SerializeField] private LayerMask m_PlayerLayer;

        // ==================== SKILL 1 - LIGHTNING STRIKE ====================
        [Header("Skill 1 - Lightning Strike")]
        [SerializeField] private float m_LightningRange    = 12f;
        [SerializeField] private float m_LightningDamage   = 35f;
        [SerializeField] private float m_LightningCastTime = 2.5f;
        [SerializeField] private float m_LightningCooldown = 6f;
        private float m_LightningCooldownTimer = 0f;

        // ==================== SKILL 2 - DARK MAGIC SHOOT ====================
        [Header("Skill 2 - Dark Magic Shoot")]
        [SerializeField] private AudioClip m_DarkMagicSound;
        [SerializeField] private float m_DarkMagicHitRadius   = 6f;
        [SerializeField] private float m_DarkMagicDamage      = 30f;
        [SerializeField] private float m_DarkMagicCastTime    = 1.5f;
        [SerializeField] private float m_DarkMagicCooldown    = 4f;
        [SerializeField] private float m_DarkMagicWarningTime = 1.0f;
        [SerializeField] private float m_DarkMagicDamageDelay = 0.6f;
        [SerializeField] private int   m_DarkMagicVfxCount    = 8;
        [SerializeField] private float m_DarkMagicSpawnOffset = 1.2f;
        private float m_DarkMagicCooldownTimer = 0f;

        // ==================== ANIMATOR PARAMS ====================
        private static readonly int ANIM_SPEED  = Animator.StringToHash("Speed");
        private static readonly int ANIM_SKILL1 = Animator.StringToHash("Skill1");
        private static readonly int ANIM_SKILL2 = Animator.StringToHash("Skill2");
        private static readonly int ANIM_DEATH  = Animator.StringToHash("Death");
        private static readonly int ANIM_HIT    = Animator.StringToHash("Hit");

        // ==================== RUNTIME STATE ====================
        private BossState m_CurrentState = BossState.IDLE;
        private bool      m_IsCasting    = false;

        // ==================== LIFECYCLE ====================
        private void Awake()
        {
            m_Agent               = GetComponent<NavMeshAgent>();
            m_Animator            = GetComponentInChildren<Animator>();
            m_CharacterData       = GetComponent<CharacterData>();
            m_LightningController = GetComponent<LightningStrikeController>();
        }

        private void Start()
        {
            if (m_CharacterData != null)
                m_CharacterData.Init();
        }

        private void OnDestroy() { }

        // ==================== UPDATE ====================
        private void Update()
        {
            if (m_CurrentState == BossState.DEAD) return;

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
            if (m_DarkMagicCooldownTimer > 0f) m_DarkMagicCooldownTimer -= Time.deltaTime;
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

            bool canLightning = m_LightningCooldownTimer <= 0f && dist <= m_LightningRange;
            bool canDarkMagic = m_DarkMagicCooldownTimer <= 0f && dist <= m_DarkMagicHitRadius;

            if (canLightning || canDarkMagic)
            {
                m_Agent.ResetPath();
                m_Animator.SetFloat(ANIM_SPEED, 0f);
                TransitionTo(BossState.CASTING);
                StartCoroutine(CastSkill(canLightning ? 1 : 2));
                return;
            }

            m_Agent.SetDestination(m_Target.transform.position);
            float speed = m_Agent.velocity.magnitude;
            m_Animator.SetFloat(ANIM_SPEED, speed, 0.1f, Time.deltaTime);
        }

        // ==================== SKILL CASTING ====================
        private IEnumerator CastSkill(int skillIndex)
        {
            m_IsCasting = true;
            float castTime = skillIndex == 1 ? m_LightningCastTime : m_DarkMagicCastTime;

            if (skillIndex == 1)
                m_Animator.speed = 0.5f;

            m_Animator.SetTrigger(skillIndex == 1 ? ANIM_SKILL1 : ANIM_SKILL2);

            yield return new WaitForSeconds(castTime);

            m_Animator.speed = 1f;

            if (m_CurrentState != BossState.DEAD)
            {
                if (skillIndex == 1)
                    ExecuteLightningStrike();
                else
                    StartCoroutine(ExecuteDarkMagicShoot());
            }

            m_IsCasting = false;
            TransitionTo(BossState.CHASING);
        }

        // ==================== SKILL 1 ====================
        private void ExecuteLightningStrike()
        {
            if (m_Target == null) return;

            if (m_LightningController != null)
            {
                m_LightningController.ExecuteSequence(m_Target.transform.position, m_CharacterData);
            }
            else
            {
                int dmg = Mathf.RoundToInt(m_LightningDamage);
                m_Target.Stats.ChangeHealth(-dmg);
                DamageUI.Instance.NewDamage(dmg, m_Target.transform.position);
            }

            m_LightningCooldownTimer = m_LightningCooldown;
        }

        // ==================== SKILL 2 ====================
        private IEnumerator ExecuteDarkMagicShoot()
        {
            if (m_DarkMagicPrefab == null || m_CastPoint == null) yield break;

            CharacterData targetSnapshot = m_Target;

            // --- 1. Spawn warning disk ---
            GameObject warning = null;
            if (m_DarkMagicWarningPrefab != null)
            {
                Vector3 warningPos = new Vector3(transform.position.x, transform.position.y + 0.05f, transform.position.z);
                float diameter = m_DarkMagicHitRadius * 2f;
                warning = Instantiate(m_DarkMagicWarningPrefab, warningPos, Quaternion.identity);
                warning.transform.localScale = new Vector3(diameter, 0.01f, diameter);
                StartCoroutine(PulseWarning(warning, m_DarkMagicWarningTime));
            }

            // --- 2. Doi warning ---
            yield return new WaitForSeconds(m_DarkMagicWarningTime);

            // --- 3. Xoa warning ---
            if (warning != null)
                Destroy(warning);

            // --- 4. Spawn VFX xung quanh + Play SFX 1 lan duy nhat ---
            Vector3 spawnPos = m_CastPoint.position;
            int count        = Mathf.Max(1, m_DarkMagicVfxCount);
            float angleStep  = 360f / count;

            if (m_DarkMagicSound != null)
            {
                SFXManager.PlaySound(SFXManager.Use.Enemies, new SFXManager.PlayData
                {
                    Clip     = m_DarkMagicSound,
                    Position = spawnPos,
                    Volume   = 1.0f,
                    PitchMin = 0.9f,
                    PitchMax = 1.1f
                });
            }

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;
                Vector3 dir = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad),
                    0f,
                    Mathf.Cos(angle * Mathf.Deg2Rad)
                );

                Quaternion rot    = Quaternion.LookRotation(dir);
                Vector3    vfxPos = spawnPos + dir * m_DarkMagicSpawnOffset;
                GameObject vfx   = Instantiate(m_DarkMagicPrefab, vfxPos, rot);
                Destroy(vfx, 3f);
            }

            // --- 5. Doi visual kip hien thi truoc khi damage ---
            yield return new WaitForSeconds(m_DarkMagicDamageDelay);

            // --- 6. Kiem tra khoang cach va gay sat thuong ---
            if (targetSnapshot != null && targetSnapshot.Stats.CurrentHealth > 0)
            {
                float distToTarget = Vector3.Distance(transform.position, targetSnapshot.transform.position);
                if (distToTarget <= m_DarkMagicHitRadius)
                {
                    int dmg = Mathf.RoundToInt(m_DarkMagicDamage);
                    targetSnapshot.Stats.ChangeHealth(-dmg);
                    DamageUI.Instance.NewDamage(dmg, targetSnapshot.transform.position);
                }
            }

            m_DarkMagicCooldownTimer = m_DarkMagicCooldown;
        }

        // ==================== PULSE WARNING ====================
        private IEnumerator PulseWarning(GameObject warning, float duration)
        {
            if (warning == null) yield break;

            Renderer rend = warning.GetComponentInChildren<Renderer>();
            if (rend == null) yield break;

            float elapsed    = 0f;
            float pulseSpeed = 7f;

            rend.material.SetColor("_BaseColor", new Color(1f, 0f, 0f, 0.85f));

            while (elapsed < duration && warning != null)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.4f, 1.0f, (Mathf.Sin(elapsed * pulseSpeed) + 1f) * 0.5f);
                rend.material.SetColor("_BaseColor", new Color(1f, 0f, 0f, alpha));
                yield return null;
            }
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
            m_Animator.speed = 1f;
            // Notify Quest System that this boss has been killed
            GameEvents.RaiseEnemyKilled(m_CharacterData.CharacterName);

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
            Gizmos.DrawWireSphere(transform.position, m_DarkMagicHitRadius);
        }
    }
}
