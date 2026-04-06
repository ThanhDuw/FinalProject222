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

        [Header("References")]
        [SerializeField] private Transform     m_CastPoint;
        [SerializeField] private GameObject    m_DarkMagicPrefab;
        [SerializeField] private GameObject    m_DarkMagicWarningPrefab;
        [SerializeField] private CharacterData m_CharacterData;
        private LightningStrikeController m_LightningController;
        private NavMeshAgent  m_Agent;
        private Animator      m_Animator;
        private CharacterData m_Target;

        [Header("Detection")]
        [SerializeField] private float     m_DetectionRadius = 15f;
        [SerializeField] private LayerMask m_PlayerLayer;

        [Header("Skill 1 - Lightning Strike")]
        [SerializeField] private float m_LightningRange    = 12f;
        [SerializeField] private float m_LightningDamage   = 35f;
        [SerializeField] private float m_LightningCastTime = 2.5f;
        [SerializeField] private float m_LightningCooldown = 6f;
        private float m_LightningCooldownTimer = 0f;

        [Header("Skill 2 - Dark Magic")]
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

        [Header("Boss Audio")]
        [SerializeField] private AudioClip m_BossDeathSound;
        [SerializeField] private AudioClip m_AttackLaughSound;
        [SerializeField] private AudioClip m_BossThemeMusic;

        private static readonly int ANIM_SPEED  = Animator.StringToHash("Speed");
        private static readonly int ANIM_SKILL1 = Animator.StringToHash("Skill1");
        private static readonly int ANIM_SKILL2 = Animator.StringToHash("Skill2");
        private static readonly int ANIM_DEATH  = Animator.StringToHash("Death");
        private static readonly int ANIM_HIT    = Animator.StringToHash("Hit");

        private BossState m_CurrentState = BossState.IDLE;
        private bool      m_IsCasting    = false;
        private AudioSource m_ThemeSource;
        private bool      m_ThemePlaying = false;

        private void Awake()
        {
            m_Agent               = GetComponent<NavMeshAgent>();
            m_Animator            = GetComponentInChildren<Animator>();
            m_CharacterData       = GetComponent<CharacterData>();
            m_LightningController = GetComponent<LightningStrikeController>();

            // Thiết lập AudioSource cho nhạc nền Boss
            m_ThemeSource = gameObject.AddComponent<AudioSource>();
            m_ThemeSource.loop = true;
            m_ThemeSource.playOnAwake = false;
            m_ThemeSource.volume = AudioVolumeController.MusicVolume;
            AudioVolumeController.OnMusicVolumeChanged += OnMusicVolumeChanged;
        }

        private void Start()
        {
            if (m_CharacterData != null) m_CharacterData.Init();
        }

        private void OnDestroy()
        {
            AudioVolumeController.OnMusicVolumeChanged -= OnMusicVolumeChanged;
        }

        private void OnMusicVolumeChanged(float volume)
        {
            if (m_ThemeSource != null)
                m_ThemeSource.volume = volume;
        }

        private void Update()
        {
            if (m_CurrentState == BossState.DEAD) return;
            if (m_CharacterData != null && m_CharacterData.Stats.CurrentHealth <= 0) { HandleDeath(); return; }
            if (m_IsCasting) return;
            if (m_LightningCooldownTimer > 0f) m_LightningCooldownTimer -= Time.deltaTime;
            if (m_DarkMagicCooldownTimer > 0f) m_DarkMagicCooldownTimer -= Time.deltaTime;

            if (m_CurrentState == BossState.IDLE)
            {
                // Phát hiện người chơi trong tầm nhìn
                Collider[] hits = Physics.OverlapSphere(transform.position, m_DetectionRadius, m_PlayerLayer);
                if (hits.Length > 0)
                {
                    m_Target = hits[0].GetComponent<CharacterData>();
                    if (m_Target != null)
                    {
                        m_CurrentState = BossState.CHASING;
                        PlayBossTheme();
                    }
                }
            }
            else if (m_CurrentState == BossState.CHASING)
            {
                if (m_Target == null || m_Target.gameObject == null)
                {
                    m_CurrentState = BossState.IDLE;
                    m_Agent.ResetPath();
                    m_Animator.SetFloat(ANIM_SPEED, 0f);
                    return;
                }
                float dist = Vector3.Distance(transform.position, m_Target.transform.position);
                bool  canL = m_LightningCooldownTimer <= 0f && dist <= m_LightningRange;
                bool  canD = m_DarkMagicCooldownTimer <= 0f && dist <= m_DarkMagicHitRadius;
                if (canL || canD)
                {
                    m_Agent.ResetPath();
                    m_Animator.SetFloat(ANIM_SPEED, 0f);
                    m_CurrentState = BossState.CASTING;
                    StartCoroutine(CastSkill(canL ? 1 : 2));
                    return;
                }
                m_Agent.SetDestination(m_Target.transform.position);
                m_Animator.SetFloat(ANIM_SPEED, m_Agent.velocity.magnitude, 0.1f, Time.deltaTime);
            }
        }

        private IEnumerator CastSkill(int idx)
        {
            m_IsCasting = true;
            // Phát âm thanh Attack_Laugh khi Boss tấn công
            if (m_AttackLaughSound != null)
                SFXManager.PlaySound(SFXManager.Use.Enemies, new SFXManager.PlayData { Clip = m_AttackLaughSound, Position = transform.position, Volume = 1f });
            float castTime = (idx == 1) ? m_LightningCastTime : m_DarkMagicCastTime;
            if (idx == 1) m_Animator.speed = 0.5f;
            m_Animator.SetTrigger((idx == 1) ? ANIM_SKILL1 : ANIM_SKILL2);
            yield return new WaitForSeconds(castTime);
            m_Animator.speed = 1f;
            if (m_CurrentState != BossState.DEAD)
            {
                if (idx == 1)
                {
                    // Kích hoạt LightningStrike (Sét Đánh)
                    if (m_Target != null)
                    {
                        if (m_LightningController != null) m_LightningController.ExecuteSequence(m_Target.transform.position, m_CharacterData);
                        else { int d = Mathf.RoundToInt(m_LightningDamage); m_Target.Stats.ChangeHealth(-d); DamageUI.Instance.NewDamage(d, m_Target.transform.position); }
                        m_LightningCooldownTimer = m_LightningCooldown;
                    }
                }
                else
                {
                    StartCoroutine(FireDarkMagic());
                }
            }
            m_IsCasting = false;
            m_CurrentState = BossState.CHASING;
        }

        private IEnumerator FireDarkMagic()
        {
            if (m_DarkMagicPrefab == null || m_CastPoint == null) yield break;
            CharacterData snap = m_Target;
            GameObject warn = null;
            if (m_DarkMagicWarningPrefab != null)
            {
                Vector3 wp = new Vector3(transform.position.x, transform.position.y + 0.05f, transform.position.z);
                warn = SpawnPooled(m_DarkMagicWarningPrefab, wp, Quaternion.identity);
                float diam = m_DarkMagicHitRadius * 2f;
                warn.transform.localScale = new Vector3(diam, 0.01f, diam);
                StartCoroutine(PulseWarn(warn, m_DarkMagicWarningTime));
            }
            yield return new WaitForSeconds(m_DarkMagicWarningTime);
            if (warn != null) ReturnPooled(warn, m_DarkMagicWarningPrefab, 0f);
            Vector3 sp  = m_CastPoint.position;
            int     cnt = Mathf.Max(1, m_DarkMagicVfxCount);
            float   ang = 360f / cnt;
            if (m_DarkMagicSound != null)
                SFXManager.PlaySound(SFXManager.Use.Enemies, new SFXManager.PlayData { Clip = m_DarkMagicSound, Position = sp, Volume = 1f, PitchMin = 0.9f, PitchMax = 1.1f });
            for (int i = 0; i < cnt; i++)
            {
                float   a   = i * ang;
                Vector3 dir = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0f, Mathf.Cos(a * Mathf.Deg2Rad));
                GameObject v = SpawnPooled(m_DarkMagicPrefab, sp + dir * m_DarkMagicSpawnOffset, Quaternion.LookRotation(dir));
                ReturnPooled(v, m_DarkMagicPrefab, 3f);
            }
            yield return new WaitForSeconds(m_DarkMagicDamageDelay);
            if (snap != null && snap.Stats.CurrentHealth > 0)
            {
                float dd = Vector3.Distance(transform.position, snap.transform.position);
                if (dd <= m_DarkMagicHitRadius) { int d = Mathf.RoundToInt(m_DarkMagicDamage); snap.Stats.ChangeHealth(-d); DamageUI.Instance.NewDamage(d, snap.transform.position); }
            }
            m_DarkMagicCooldownTimer = m_DarkMagicCooldown;
        }

        private IEnumerator PulseWarn(GameObject obj, float dur)
        {
            if (obj == null) yield break;
            Renderer r = obj.GetComponentInChildren<Renderer>();
            if (r == null) yield break;
            float e = 0f;
            r.material.SetColor("_BaseColor", new Color(1f, 0f, 0f, 0.85f));
            while (e < dur && obj != null)
            {
                e += Time.deltaTime;
                r.material.SetColor("_BaseColor", new Color(1f, 0f, 0f, Mathf.Lerp(0.4f, 1f, (Mathf.Sin(e * 7f) + 1f) * 0.5f)));
                yield return null;
            }
        }

        private void HandleDeath()
        {
            if (m_CurrentState == BossState.DEAD) return;
            m_CurrentState = BossState.DEAD; m_IsCasting = false; StopAllCoroutines();
            // Phát âm thanh Boss_Death và dừng hoạt cảnh/nhạc nền Boss_Theme
            if (m_BossDeathSound != null)
                SFXManager.PlaySound(SFXManager.Use.Enemies, new SFXManager.PlayData { Clip = m_BossDeathSound, Position = transform.position, Volume = 1f });
            StopBossTheme();
            m_Agent.enabled = false; m_Animator.speed = 1f;
            GameEvents.RaiseEnemyKilled(m_CharacterData.CharacterName);
            m_Animator.SetTrigger(ANIM_DEATH);
            GetComponent<Collider>().enabled = false;
            Destroy(gameObject, 5f);
        }

        private GameObject SpawnPooled(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (PoolManager.Instance != null) return PoolManager.Instance.Get(prefab, pos, rot);
            return Instantiate(prefab, pos, rot);
        }

        private void ReturnPooled(GameObject go, GameObject prefab, float delay)
        {
            if (go == null) return;
            if (PoolManager.Instance != null) { PoolManager.Instance.ReturnDelayed(go, prefab, delay); return; }
            if (delay > 0f) Destroy(go, delay); else Destroy(go);
        }

        private void PlayBossTheme()
        {
            if (m_ThemePlaying || m_BossThemeMusic == null) return;
            m_ThemePlaying = true;

            // Tạm dừng nhạc nền của bản đồ
            var bgm = FindFirstObjectByType<RandomBGMPlayer>();
            if (bgm != null)
            {
                var bgmSource = bgm.GetComponent<AudioSource>();
                if (bgmSource != null) bgmSource.Pause();
            }

            m_ThemeSource.clip = m_BossThemeMusic;
            m_ThemeSource.volume = AudioVolumeController.MusicVolume;
            m_ThemeSource.Play();
        }

        private void StopBossTheme()
        {
            if (!m_ThemePlaying) return;
            m_ThemePlaying = false;

            if (m_ThemeSource != null) m_ThemeSource.Stop();

            // Tiếp tục phát nhạc nền của bản đồ
            var bgm = FindFirstObjectByType<RandomBGMPlayer>();
            if (bgm != null)
            {
                var bgmSource = bgm.GetComponent<AudioSource>();
                if (bgmSource != null) bgmSource.UnPause();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, m_DetectionRadius);
            Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(transform.position, m_LightningRange);
            Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, m_DarkMagicHitRadius);
        }
    }
}
