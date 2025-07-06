using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    [Header("�ӵ�����")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private ParticleSystem hitEffect;

    private int damage;
    private Transform target;
    private string poolTag = "Projectile";

    // ׷��Ŀ��
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
            // ���û��Ŀ�꣬���Գ�����¼��λ���ƶ�
            if (Vector3.Distance(transform.position, lastTargetPosition) < 0.2f)
            {
                ReturnToPool();
                return;
            }

            Vector3 direction1 = (lastTargetPosition - transform.position).normalized;
            transform.position += direction1 * speed * Time.deltaTime;

            return;
        }

        // ��������¼��λ��
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

        // ����������Ч
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

    // IPoolable �ӿ�ʵ��
    public void OnSpawn()
    {
        // ����״̬
        target = null;
        damage = 0;
        lastTargetPosition = Vector3.zero;

        // ȷ���ӵ��ɼ�
        gameObject.SetActive(true);
    }

    public void OnReturn()
    {
        // ����״̬
        target = null;
        damage = 0;
        lastTargetPosition = Vector3.zero;
    }
}