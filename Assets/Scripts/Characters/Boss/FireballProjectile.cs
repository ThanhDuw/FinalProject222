using UnityEngine;
using CreatorKitCode;
using CreatorKitCodeInternal;

namespace CreatorKitCode
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class FireballProjectile : MonoBehaviour
    {

        [Header("Projectile Settings")]
        [SerializeField] private float m_Speed = 12f;
        [SerializeField] private float m_Damage = 30f;
        [SerializeField] private float m_MaxLifetime = 5f;


        private CharacterData m_Owner;
        private Transform m_Target;
        private Rigidbody m_Rigidbody;
        private bool m_HasHit = false;


        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.useGravity = false;
            m_Rigidbody.isKinematic = false;

            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.3f;
        }


        /// <summary>
        /// Khởi động Fireball bay về phía mục tiêu.
        /// Được gọi ngay sau khi Instantiate.
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


        private void Update()
        {
            if (m_HasHit || m_Target == null) return;

            // Theo dõi mục tiêu nhẹ nhàng (homing)
            Vector3 dir = (m_Target.position + Vector3.up * 1f - transform.position).normalized;
            m_Rigidbody.linearVelocity = dir * m_Speed;
            transform.forward = dir;
        }


private void OnTriggerEnter(Collider other)
        {
            if (m_HasHit) return;

            CharacterData target = other.GetComponent<CharacterData>();

            // Bỏ qua nếu chạm vào chính nó hoặc chủ sở hữu của nó
            if (target == null || target == m_Owner) return;

            m_HasHit = true;

            // Gây sát thương trực tiếp qua Stats.ChangeHealth
            int dmg = Mathf.RoundToInt(m_Damage);
            target.Stats.ChangeHealth(-dmg);
            DamageUI.Instance.NewDamage(dmg, transform.position);

            // VFX
            VFXManager.PlayVFX(VFXType.FireEffect, transform.position);

            Destroy(gameObject);
        }
    }
}
