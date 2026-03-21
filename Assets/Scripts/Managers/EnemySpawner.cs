using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies using PoolManager. Drop on an empty GameObject in the scene.
/// Pre-placed scene enemies (no EnemyPoolTracker) keep the original Destroy path.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab and Pool")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private int        _prewarmCount  = 5;

    [Header("Spawn Settings")]
    [SerializeField] private int   _maxAlive      = 5;
    [SerializeField] private float _spawnInterval = 4f;
    [SerializeField] private float _spawnRadius   = 3f;
    [SerializeField] private bool  _autoSpawn     = true;

    private readonly List<GameObject> _alive = new List<GameObject>();
    private float _nextSpawnTime;

    private void Start()
    {
        if (_enemyPrefab == null || PoolManager.Instance == null) return;
        PoolManager.Instance.Prewarm(_enemyPrefab, _prewarmCount);
        _nextSpawnTime = Time.time + _spawnInterval;
    }

    private void Update()
    {
        if (!_autoSpawn || Time.time < _nextSpawnTime) return;
        _nextSpawnTime = Time.time + _spawnInterval;

        _alive.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (_alive.Count >= _maxAlive || PoolManager.Instance == null || _enemyPrefab == null) return;

        Vector2 rand     = Random.insideUnitCircle * _spawnRadius;
        float   spawnX   = transform.position.x + rand.x;
        float   spawnY   = transform.position.y;
        float   spawnZ   = transform.position.z + rand.y;
        var     spawnPos = new Vector3(spawnX, spawnY, spawnZ);

        GameObject inst = PoolManager.Instance.Get(_enemyPrefab, spawnPos, Quaternion.identity);
        _alive.Add(inst);

        EnemyPoolTracker tr = inst.GetComponent<EnemyPoolTracker>();
        if (tr == null) tr  = inst.AddComponent<EnemyPoolTracker>();
        tr.Init(this, inst);
    }

    public void OnEnemyReturned(GameObject enemy)
    {
        _alive.Remove(enemy);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, _spawnRadius);
    }
}
