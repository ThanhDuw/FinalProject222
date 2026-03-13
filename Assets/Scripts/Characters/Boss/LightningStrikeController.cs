using System.Collections;
using UnityEngine;
using CreatorKitCode;
using CreatorKitCodeInternal;

namespace CreatorKitCode
{
    /// <summary>
    /// Xu ly trinh tu ky nang Lightning Strike cua Skeleton Mage Boss.
    /// Cac tia set lan luot roi xuong, moi tia co vung canh bao do tren mat dat truoc khi danh.
    /// </summary>
    public class LightningStrikeController : MonoBehaviour
    {
        // ==================== REFERENCES ====================
        [Header("VFX")]
        [SerializeField] private GameObject m_ZapPrefab;         // VFX_Zap_02_Blue
        [SerializeField] private GameObject m_WarningDecalPrefab; // LightningWarning prefab

        // ==================== CONFIGURATION ====================
        [Header("Strike Settings")]
        [SerializeField] private int   m_StrikeCount      = 4;    // So tia set
        [SerializeField] private float m_StrikeRadius     = 4.5f; // Ban kinh rung quanh muc tieu
        [SerializeField] private float m_WarningDuration  = 0.75f; // Thoi gian canh bao (s)
        [SerializeField] private float m_StrikeInterval   = 0.5f;  // Khoang cach giua cac tia set (s)
        [SerializeField] private float m_ZapSpawnHeight   = 8f;    // Chieu cao spawn VFX tu tren xuong
        [SerializeField] private float m_ZapLifetime      = 2.0f;  // Thoi gian song cua VFX

        [Header("Damage")]
        [SerializeField] private float m_DamagePerStrike  = 20f;   // Sát thuong moi tia
        [SerializeField] private float m_DamageRadius     = 1.8f;  // Ban kinh sát thuong

        // ==================== RUNTIME ====================
        private bool m_IsExecuting = false;
        public bool IsExecuting => m_IsExecuting;

        // ==================== PUBLIC API ====================
        /// <summary>
        /// Bat dau trinh tu lightning. Goi tu SkeletonMageBoss sau khi cast xong.
        /// </summary>
        public void ExecuteSequence(Vector3 targetPosition, CharacterData owner)
        {
            if (m_IsExecuting) return;
            StartCoroutine(DoStrikeSequence(targetPosition, owner));
        }

        // ==================== COROUTINE ====================
        private IEnumerator DoStrikeSequence(Vector3 center, CharacterData owner)
        {
            m_IsExecuting = true;

            // Tao danh sach vi tri ngau nhien quanh muc tieu
            Vector3[] strikePositions = GenerateStrikePositions(center, m_StrikeCount, m_StrikeRadius);

            for (int i = 0; i < strikePositions.Length; i++)
            {
                Vector3 groundPos = GetGroundPosition(strikePositions[i]);

                // --- 1. Spawn canh bao do ---
                GameObject warning = null;
                if (m_WarningDecalPrefab != null)
                {
                    warning = Instantiate(m_WarningDecalPrefab, groundPos, Quaternion.identity);
                    StartCoroutine(PulseWarning(warning, m_WarningDuration));
                }

                // --- 2. Cho canh bao hien thi ---
                yield return new WaitForSeconds(m_WarningDuration);

                // --- 3. Xoa canh bao ---
                if (warning != null)
                    Destroy(warning);

                // --- 4. Spawn VFX_Zap_02_Blue ---
                if (m_ZapPrefab != null)
                {
                    Vector3 zapSpawnPos = groundPos + Vector3.up * m_ZapSpawnHeight;
                    GameObject zap = Instantiate(m_ZapPrefab, zapSpawnPos, Quaternion.identity);
                    Destroy(zap, m_ZapLifetime);
                }

                // --- 5. Gay sát thuong ---
                DealStrikeDamage(groundPos, owner);

                // --- 6. Khoang cach giua cac tia set ---
                if (i < strikePositions.Length - 1)
                    yield return new WaitForSeconds(m_StrikeInterval);
            }

            m_IsExecuting = false;
        }

        // ==================== HELPERS ====================

        private Vector3[] GenerateStrikePositions(Vector3 center, int count, float radius)
        {
            Vector3[] positions = new Vector3[count];

            // Tia dau tien luon nham vao vi tri muc tieu
            positions[0] = center;

            for (int i = 1; i < count; i++)
            {
                float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist   = Random.Range(radius * 0.3f, radius);
                positions[i] = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            }

            return positions;
        }

        /// <summary>Raycast xuong de tim mat dat, fallback ve centerY neu khong thay.</summary>
        private Vector3 GetGroundPosition(Vector3 pos)
        {
            Vector3 rayOrigin = pos + Vector3.up * 10f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f))
                return hit.point;

            return new Vector3(pos.x, pos.y, pos.z);
        }

        /// <summary>Lam cho warning nhap nhay de tang cam giac nguy hiem.</summary>
        private IEnumerator PulseWarning(GameObject warning, float duration)
        {
            if (warning == null) yield break;

            Renderer rend = warning.GetComponentInChildren<Renderer>();
            float elapsed = 0f;
            float pulseSpeed = 6f;

            while (elapsed < duration && warning != null)
            {
                elapsed += Time.deltaTime;

                if (rend != null)
                {
                    // Alpha nhap nhay: dao dong giua 0.3 va 0.8
                    float alpha = Mathf.Lerp(0.3f, 0.85f, (Mathf.Sin(elapsed * pulseSpeed) + 1f) * 0.5f);
                    Color c = rend.material.color;
                    c.a = alpha;
                    rend.material.color = c;
                }

                yield return null;
            }
        }

        private void DealStrikeDamage(Vector3 groundPos, CharacterData owner)
        {
            Collider[] hits = Physics.OverlapSphere(groundPos, m_DamageRadius);
            foreach (var col in hits)
            {
                CharacterData cd = col.GetComponent<CharacterData>();
                if (cd == null || cd == owner) continue;

                int dmg = Mathf.RoundToInt(m_DamagePerStrike);
                cd.Stats.ChangeHealth(-dmg);
                DamageUI.Instance.NewDamage(dmg, cd.transform.position);
            }
        }

        // ==================== GIZMOS ====================
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, m_StrikeRadius);
        }
    }
}
