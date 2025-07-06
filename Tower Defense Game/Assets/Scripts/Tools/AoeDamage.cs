using UnityEngine;

public class AoeDamage : MonoBehaviour
{
    [Header("伤害设置")]
    public int damage = 30;                 // 基础伤害值
    public float radius = 5f;               // 影响范围
    public float duration = 2f;             // 效果持续时间
    public float tickInterval = 0.5f;       // 伤害间隔（秒）
    public LayerMask targetLayers;          // 影响的目标层级

    [Header("视觉效果")]
    public ParticleSystem effectParticles;  // 粒子特效

    private float startTime;
    private float nextTickTime;
    private bool isActive;

    // 初始化AOE效果
    public void Initialize(int damageValue, float radiusValue, float durationValue = -1)
    {
        damage = damageValue;
        radius = radiusValue;

        if (durationValue > 0)
            duration = durationValue;

        startTime = Time.time;
        nextTickTime = Time.time;
        isActive = true;

        // 调整碰撞体大小
        var collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.radius = radius;
        }

        // 播放特效
        if (effectParticles != null)
        {
            effectParticles.Play();
        }
    }

    private void Update()
    {
        if (!isActive) return;

        // 检查持续时间是否结束
        if (Time.time - startTime >= duration)
        {
            Deactivate();
            return;
        }

        // 定期造成伤害
        if (Time.time >= nextTickTime)
        {
            ApplyDamage();
            nextTickTime = Time.time + tickInterval;
        }
    }

    // 应用伤害
    private void ApplyDamage()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius, targetLayers);

        foreach (var collider in hitColliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    // 停用AOE效果
    public void Deactivate()
    {
        isActive = false;

        // 停止特效
        if (effectParticles != null)
        {
            effectParticles.Stop();
        }

        // 延迟销毁或返回对象池
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(gameObject.name, gameObject);
        }
        else
        {
            Destroy(gameObject, 1f); // 给特效时间消失
        }
    }

    // 调试显示范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}