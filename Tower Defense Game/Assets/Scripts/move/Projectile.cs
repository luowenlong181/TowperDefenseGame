using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    [Header("子弹设置")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private ParticleSystem hitEffect;

    private int damage;
    private Transform target;
    private string poolTag = "Projectile";

    // 追踪目标
    private Vector3 lastTargetPosition;

    public void SetDamage(int dmg) => damage = dmg;

    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
        if (target != null)
        {
            lastTargetPosition = target.position;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            // 如果没有目标，尝试朝最后记录的位置移动
            if (Vector3.Distance(transform.position, lastTargetPosition) < 0.2f)
            {
                ReturnToPool();
                return;
            }

            Vector3 direction1 = (lastTargetPosition - transform.position).normalized;
            transform.position += direction1 * speed * Time.deltaTime;

            return;
        }

        // 更新最后记录的位置
        lastTargetPosition = target.position;

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            OnHitTarget();
            ReturnToPool();
        }
    }

    private void OnHitTarget()
    {
        if (target != null && target.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
        }

        // 播放命中特效
        PlayHitEffect();
    }

    private void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            ParticleSystem effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration);
        }
    }

    private void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // IPoolable 接口实现
    public void OnSpawn()
    {
        // 重置状态
        target = null;
        damage = 0;
        lastTargetPosition = Vector3.zero;

        // 确保子弹可见
        gameObject.SetActive(true);
    }

    public void OnReturn()
    {
        // 清理状态
        target = null;
        damage = 0;
        lastTargetPosition = Vector3.zero;
    }
}