using UnityEngine;

public class AoeDamage : MonoBehaviour
{
    [Header("�˺�����")]
    public int damage = 30;                 // �����˺�ֵ
    public float radius = 5f;               // Ӱ�췶Χ
    public float duration = 2f;             // Ч������ʱ��
    public float tickInterval = 0.5f;       // �˺�������룩
    public LayerMask targetLayers;          // Ӱ���Ŀ��㼶

    [Header("�Ӿ�Ч��")]
    public ParticleSystem effectParticles;  // ������Ч

    private float startTime;
    private float nextTickTime;
    private bool isActive;

    // ��ʼ��AOEЧ��
    public void Initialize(int damageValue, float radiusValue, float durationValue = -1)
    {
        damage = damageValue;
        radius = radiusValue;

        if (durationValue > 0)
            duration = durationValue;

        startTime = Time.time;
        nextTickTime = Time.time;
        isActive = true;

        // ������ײ���С
        var collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.radius = radius;
        }

        // ������Ч
        if (effectParticles != null)
        {
            effectParticles.Play();
        }
    }

    private void Update()
    {
        if (!isActive) return;

        // ������ʱ���Ƿ����
        if (Time.time - startTime >= duration)
        {
            Deactivate();
            return;
        }

        // ��������˺�
        if (Time.time >= nextTickTime)
        {
            ApplyDamage();
            nextTickTime = Time.time + tickInterval;
        }
    }

    // Ӧ���˺�
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

    // ͣ��AOEЧ��
    public void Deactivate()
    {
        isActive = false;

        // ֹͣ��Ч
        if (effectParticles != null)
        {
            effectParticles.Stop();
        }

        // �ӳ����ٻ򷵻ض����
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(gameObject.name, gameObject);
        }
        else
        {
            Destroy(gameObject, 1f); // ����Чʱ����ʧ
        }
    }

    // ������ʾ��Χ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}