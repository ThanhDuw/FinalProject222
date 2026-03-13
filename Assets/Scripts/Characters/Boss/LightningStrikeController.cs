using System.Collections;
using UnityEngine;
using CreatorKitCode;
using CreatorKitCodeInternal;

namespace CreatorKitCode
{
    public class LightningStrikeController : MonoBehaviour
    {
        // ==================== REFERENCES ====================
        [Header("VFX")]
        [SerializeField] private GameObject m_ZapPrefab;
        [SerializeField] private GameObject m_WarningDecalPrefab;

        // ==================== CONFIGURATION ====================
        [Header("Ground Detection")]
        [SerializeField] private LayerMask m_GroundLayer = ~0; // Mac dinh hit tat ca, chinh lai trong Inspector chi chon Terrain/Ground layer

        [Header("Strike Settings")]
        [SerializeField] private int   m_StrikeCount     = 4;
        [SerializeField] private float m_StrikeRadius    = 4.5f;
        [SerializeField] private float m_WarningDuration = 0.75f;
        [SerializeField] private float m_StrikeInterval  = 0.5f;
        [SerializeField] private float m_ZapGroundOffset = 0.05f;
        [SerializeField] private float m_ZapLifetime     = 2.0f;

        [Header("Damage")]
        [SerializeField] private float m_DamagePerStrike = 20f;
        [SerializeField] private float m_DamageRadius    = 1.8f;

        // ==================== RUNTIME ====================
        private bool m_IsExecuting = false;
        public bool IsExecuting => m_IsExecuting;

        // ==================== PUBLIC API ====================
        public void ExecuteSequence(Vector3 targetPosition, CharacterData owner)
        {
            if (m_IsExecuting) return;
            StartCoroutine(DoStrikeSequence(targetPosition, owner));
        }

        // ==================== COROUTINE ====================
        private IEnumerator DoStrikeSequence(Vector3 center, CharacterData owner)
        {
            m_IsExecuting = true;

            Vector3[] strikePositions = GenerateStrikePositions(center, m_StrikeCount, m_StrikeRadius);

            for (int i = 0; i < strikePositions.Length; i++)
            {
                Vector3 groundPos = GetGroundPosition(strikePositions[i]);

                // 1. Spawn warning disc sat mat dat
                GameObject warning = null;
                if (m_WarningDecalPrefab != null)
                {
                    warning = Instantiate(m_WarningDecalPrefab, groundPos + Vector3.up * 0.05f, Quaternion.identity);
                    StartCoroutine(PulseWarning(warning, m_WarningDuration));
                }

                // 2. Cho warning hien thi
                yield return new WaitForSeconds(m_WarningDuration);

                // 3. Xoa warning
                if (warning != null)
                    Destroy(warning);

                // 4. Spawn VFX set
                if (m_ZapPrefab != null)
                {
                    Vector3 zapPos = groundPos + Vector3.up * m_ZapGroundOffset;
                    GameObject zap = Instantiate(m_ZapPrefab, zapPos, Quaternion.identity);
                    Destroy(zap, m_ZapLifetime);
                }

                // 5. Gay sat thuong
                DealStrikeDamage(groundPos, owner);

                // 6. Doi truoc tia tiep theo
                if (i < strikePositions.Length - 1)
                    yield return new WaitForSeconds(m_StrikeInterval);
            }

            m_IsExecuting = false;
        }

        // ==================== HELPERS ====================
        private Vector3[] GenerateStrikePositions(Vector3 center, int count, float radius)
        {
            Vector3[] positions = new Vector3[count];
            positions[0] = center; // Tia dau luon trung tam

            for (int i = 1; i < count; i++)
            {
                float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist   = Random.Range(radius * 0.3f, radius);
                positions[i] = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            }

            return positions;
        }

        /// <summary>
        /// Raycast tu tren cao xuong, tranh hit collider cua boss/enemy.
        /// m_GroundLayer nen duoc set chi chon Terrain hoac layer mat dat.
        /// </summary>
        private Vector3 GetGroundPosition(Vector3 pos)
        {
            Vector3 rayOrigin = new Vector3(pos.x, pos.y + 30f, pos.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 40f, m_GroundLayer))
                return hit.point;

            return new Vector3(pos.x, 0f, pos.z);
        }

        /// <summary>
        /// Lam warning nhap nhay mau DO. Dung SetColor("_BaseColor") cho URP shader.
        /// </summary>
        private IEnumerator PulseWarning(GameObject warning, float duration)
        {
            if (warning == null) yield break;

            Renderer rend = warning.GetComponentInChildren<Renderer>();
            if (rend == null) yield break;

            float elapsed    = 0f;
            float pulseSpeed = 7f;

            // Dat mau do ngay lap tuc truoc khi vao loop
            rend.material.SetColor("_BaseColor", new Color(1f, 0f, 0f, 0.85f));

            while (elapsed < duration && warning != null)
            {
                elapsed += Time.deltaTime;

                float alpha = Mathf.Lerp(0.4f, 1.0f, (Mathf.Sin(elapsed * pulseSpeed) + 1f) * 0.5f);
                rend.material.SetColor("_BaseColor", new Color(1f, 0f, 0f, alpha));

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
