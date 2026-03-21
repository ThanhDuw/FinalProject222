using UnityEngine;
using UnityEngine.AI;
using CreatorKitCode;

/// <summary>
/// Attached by EnemySpawner to each pooled enemy instance.
/// Resets state when re-enabled, returns to pool on death.
/// </summary>
public class EnemyPoolTracker : MonoBehaviour
{
    private EnemySpawner _spawner;
    private GameObject   _self;

    public void Init(EnemySpawner spawner, GameObject self)
    {
        _spawner = spawner;
        _self    = self;
    }

    private void OnEnable()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        CharacterData cd = GetComponent<CharacterData>();
        if (cd != null) cd.Init();
    }

    public void OnEnemyDied()
    {
        if (_spawner != null)
            _spawner.OnEnemyReturned(_self);

        NavMeshAgent ag = GetComponent<NavMeshAgent>();
        if (ag != null) ag.enabled = false;

        Collider c = GetComponent<Collider>();
        if (c != null) c.enabled = false;

        PooledObject pooled = GetComponent<PooledObject>();
        if (pooled != null)
            pooled.ReturnToPool(3f);
        else if (PoolManager.Instance != null)
            PoolManager.Instance.ReturnDelayed(gameObject, 3f);
        else
            gameObject.SetActive(false);
    }
}
