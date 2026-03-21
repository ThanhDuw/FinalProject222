using UnityEngine;

/// <summary>
/// Lightweight component on every pool-managed GameObject.
/// Added automatically by PoolManager. Call ReturnToPool() to release.
/// </summary>
public class PooledObject : MonoBehaviour
{
    private GameObject _src;
    private float      _releaseAt = -1f;
    private bool       _pending;

    public void Initialize(GameObject source)
    {
        _src = source;
    }

    public void ReturnToPool(float delay = 0f)
    {
        if (delay <= 0f)
        {
            _pending = false;
            if (PoolManager.Instance != null && _src != null)
                PoolManager.Instance.Return(gameObject, _src);
            else
                gameObject.SetActive(false);
            return;
        }
        _pending   = true;
        _releaseAt = Time.time + delay;
    }

    public void CancelReturn()
    {
        _pending = false;
    }

    private void OnDisable()
    {
        _pending = false;
    }

    private void Update()
    {
        if (!_pending) return;
        if (Time.time < _releaseAt) return;
        _pending = false;
        if (PoolManager.Instance != null && _src != null)
            PoolManager.Instance.Return(gameObject, _src);
        else
            gameObject.SetActive(false);
    }
}
