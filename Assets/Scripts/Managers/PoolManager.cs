using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic Object Pool -- Singleton, DontDestroyOnLoad.
/// Usage:
///   Spawn:  PoolManager.Instance.Get(prefab, position, rotation)
///   Return: PoolManager.Instance.Return(instance, sourcePrefab)
///           or call pooled.ReturnToPool() on the PooledObject component.
/// Pools grow automatically when empty.
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Extra instances to create when a pool runs dry.")]
    [SerializeField] private int _growAmount = 4;

    private readonly Dictionary<int, Queue<GameObject>> _pools
        = new Dictionary<int, Queue<GameObject>>();
    private readonly Dictionary<int, int> _instanceToKey
        = new Dictionary<int, int>();
    private readonly Dictionary<int, Transform> _containers
        = new Dictionary<int, Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Pre-warm a pool before first use.</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        int key = prefab.GetInstanceID();
        EnsurePool(prefab, key);
        for (int i = 0; i < count; i++)
        {
            var obj = CreateInstance(prefab, key);
            obj.SetActive(false);
            _pools[key].Enqueue(obj);
        }
    }

    /// <summary>Get an active instance placed at position/rotation.</summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int key = prefab.GetInstanceID();
        EnsurePool(prefab, key);
        if (_pools[key].Count == 0)
            GrowPool(prefab, key);
        var inst = _pools[key].Dequeue();
        inst.transform.SetPositionAndRotation(position, rotation);
        inst.transform.SetParent(null);
        inst.SetActive(true);
        return inst;
    }

    /// <summary>Return an instance using its source prefab reference.</summary>
    public void Return(GameObject instance, GameObject sourcePrefab)
    {
        DoReturn(instance, sourcePrefab.GetInstanceID());
    }

    /// <summary>Return an instance that was spawned via Get() -- auto-finds key.</summary>
    public void Return(GameObject instance)
    {
        int id = instance.GetInstanceID();
        if (!_instanceToKey.TryGetValue(id, out int key))
        {
            Debug.LogWarning("[PoolManager] Untracked object: " + instance.name + ". Destroying.");
            Destroy(instance);
            return;
        }
        DoReturn(instance, key);
    }

    /// <summary>Return after a delay using source prefab.</summary>
    public void ReturnDelayed(GameObject instance, GameObject sourcePrefab, float delay)
    {
        StartCoroutine(CoReturnWithPrefab(instance, sourcePrefab, delay));
    }

    /// <summary>Return after a delay -- auto-finds key.</summary>
    public void ReturnDelayed(GameObject instance, float delay)
    {
        StartCoroutine(CoReturn(instance, delay));
    }

    // -- Private helpers ---------------------------------------------------------

    private void EnsurePool(GameObject prefab, int key)
    {
        if (_pools.ContainsKey(key)) return;
        _pools[key] = new Queue<GameObject>();
        var container = new GameObject("[Pool] " + prefab.name);
        container.transform.SetParent(transform);
        _containers[key] = container.transform;
    }

    private void GrowPool(GameObject prefab, int key)
    {
        for (int i = 0; i < _growAmount; i++)
        {
            var obj = CreateInstance(prefab, key);
            obj.SetActive(false);
            _pools[key].Enqueue(obj);
        }
    }

    private GameObject CreateInstance(GameObject prefab, int key)
    {
        var obj = Instantiate(prefab);
        _instanceToKey[obj.GetInstanceID()] = key;
        var pooled = obj.GetComponent<PooledObject>();
        if (pooled == null) pooled = obj.AddComponent<PooledObject>();
        pooled.Initialize(prefab);
        return obj;
    }

    private void DoReturn(GameObject instance, int key)
    {
        instance.SetActive(false);
        if (_containers.TryGetValue(key, out Transform container))
            instance.transform.SetParent(container);
        if (!_pools.ContainsKey(key)) _pools[key] = new Queue<GameObject>();
        _pools[key].Enqueue(instance);
    }

    private IEnumerator CoReturnWithPrefab(GameObject inst, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (inst != null) Return(inst, prefab);
    }

    private IEnumerator CoReturn(GameObject inst, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (inst != null) Return(inst);
    }
}
