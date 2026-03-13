using UnityEngine;
using CreatorKitCode;
using CreatorKitCodeInternal;

namespace CreatorKitCode
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class FireballProjectile : MonoBehaviour
    {
        // ==================== CONFIGURATION ====================
        [Header("Projectile Settings")]
        [SerializeField] private float m_Speed = 12f;
        [SerializeField] private float m_Damage = 30f;
        [SerializeField] private float m_MaxLifetime = 5f;

        // ==================== REFERENCES ====================
        private CharacterData m_Owner;
        private Transform m_Target;
        private Rigidbody m_Rigidbody;
        private bool m_HasHit = false;

        // =============================================================
        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.useGravity = false;
            m_Rigidbody.isKinematic = false;

            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.3f;
        }

        // ==================== LAUNCH ====================
        /// <summary>
        /// Khoi dong Fireball ve phia target.
        /// Goi ngay sau Instantiate.
        /// </summary>
        public void Launch(Transform target, CharacterData owner)
        {
            m_Target = target;
            m_Owner  = owner;

            if (m_Target != null)
            {
                Vector3 dir = (m_Target.position + Vector3.up * 1f - transform.position).normalized;
                m_Rigidbody.linearVelocity = dir * m_Speed;
                transform.forward = dir;
            }

            Destroy(gameObject, m_MaxLifetime);
        }

        // ==================== UPDATE ====================
        private void Update()
        {
            if (m_HasHit || m_Target == null) return;

            // Track target mildly (homing nhe)
            Vector3 dir = (m_Target.position + Vector3.up * 1f - transform.position).normalized;
            m_Rigidbody.linearVelocity = dir * m_Speed;
            transform.forward = dir;
        }

        // ==================== HIT DETECTION ====================
private void OnTriggerEnter(Collider other)
        {
            if (m_HasHit) return;

            CharacterData target = other.GetComponent<CharacterData>();

            // Bo qua neu cham chinh no hoac owner cua no
            if (target == null || target == m_Owner) return;

            m_HasHit = true;

            // Gay dame truc tiep qua Stats.ChangeHealth
            int dmg = Mathf.RoundToInt(m_Damage);
            target.Stats.ChangeHealth(-dmg);
            DamageUI.Instance.NewDamage(dmg, transform.position);

            // VFX
            VFXManager.PlayVFX(VFXType.FireEffect, transform.position);

            Destroy(gameObject);
        }
    }
}
