using System.Collections.Generic;
using UnityEngine;

public class DamageTextManager : Singleton<DamageTextManager>
{
    [SerializeField] private DamageFloatText damageTextPrefab;
    [SerializeField] private int poolSize = 8;

    private Queue<DamageFloatText> pool = new();
    private Transform poolRoot;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
        InitPool();
    }

    private void InitPool()
    {
        if (!ValidatePrefab()) return;

        poolRoot = new GameObject("DamageTextPool").transform;
        poolRoot.SetParent(transform);

        for (int i = 0; i < poolSize; i++)
        {
            CreatePooled();
        }
    }

    private bool ValidatePrefab()
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("[DamageTextManager] damageTextPrefab 未赋值，无法显示伤害跳字。请创建 DamageFloatText 预制体并拖入 DamageTextManager 的 damageTextPrefab 字段。");
            return false;
        }
        return true;
    }

    private DamageFloatText CreatePooled()
    {
        DamageFloatText instance = Instantiate(damageTextPrefab, poolRoot);
        instance.gameObject.SetActive(false);
        pool.Enqueue(instance);
        return instance;
    }

    public void ShowDamage(Vector3 worldPosition, int amount)
    {
        if (!ValidatePrefab()) return;

        DamageFloatText text = pool.Count > 0 ? pool.Dequeue() : CreatePooled();
        text.Show(worldPosition, amount, OnTextComplete);
    }

    private void OnTextComplete(DamageFloatText text)
    {
        text.gameObject.SetActive(false);
        pool.Enqueue(text);
    }
}
