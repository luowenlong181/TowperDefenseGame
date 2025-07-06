// MonsterBase.cs
using UnityEngine;
using System.Collections;
using System.Linq;

public abstract class MonsterBase : MonoBehaviour, IDamageable, IPoolable
{
    [Header("��������")]
    public MonsterData data;

    [Header("2D��������")]
    [SerializeField] public LayerMask playerLayerMask;

    [Header("Ʈ������")]
    [SerializeField] private Canvas damageTextCanvas;
    [SerializeField] private float damageTextYOffset = 1f;
    protected bool isDead;
    protected string poolTag;

    // ��ǰѪ��
    protected int currentHealth;

    // ������ȴ��ʱ��
    protected float[] attackCooldowns;

    // ʵ�� IDamageable �ӿ�
    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => data.health;

    // ����״̬
    protected enum MonsterState { Idle, Moving, Attacking, Dead }
    protected MonsterState currentState = MonsterState.Idle;

    // �ƶ��͹�����ʱ��
    protected float lastMoveTime;
    // �޸�Ŀ�����Ϊ IDamageable ����
    protected IDamageable target;
    protected Transform targetTransform; // ����λ�ø���

    public virtual void Initialize(MonsterData monsterData)
    {
        data = monsterData;
        currentHealth = data.health;
        isDead = false;
        currentState = MonsterState.Idle;
        poolTag = $"Monster_{data.id}";

        // ��ʼ��������ȴ����
        attackCooldowns = new float[data.attacks.Count];
        for (int i = 0; i < attackCooldowns.Length; i++)
        {
            attackCooldowns[i] = 0f;
        }

        // ���ü�ʱ��
        lastMoveTime = Time.time;
    }

    protected virtual void Awake()
    {
        // ȷ������ײ��
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true; // ����Ϊ����������������ײ
        }

        // ���2D����
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        // �Ż���������
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;

        // �Զ�������Ҳ㼶���루���δ��Inspector�����ã�
        if (playerLayerMask.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                playerLayerMask = 1 << playerLayer;
                Debug.Log($"{name}: �Զ����� playerLayerMask Ϊ Player �㼶");
            }
            else
            {
                Debug.LogError($"{name}: δ�ҵ� 'Player' �㼶! �봴���ò㼶");
            }
        }
    }
    // ����Ϸ��ʼ��ʱ�����������ײ��ϵ
    public class GameInitializer : MonoBehaviour
    {
        void Start()
        {
            // ��ֹ����֮����ײ
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Enemy"),
                LayerMask.NameToLayer("Enemy")
            );

            // ��������������ײ
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Enemy"),
                LayerMask.NameToLayer("Player"),
                false
            );

            // ����������ӵ���ײ
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Enemy"),
                LayerMask.NameToLayer("Projectiles"),
                false
            );
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // ���¹�����ȴ
        UpdateAttackCooldowns();

        // ȷ��Ŀ����Ч
        if (target != null && !target.IsDead && targetTransform != null && targetTransform.gameObject.activeInHierarchy)
        {
            float distance = Vector2.Distance(transform.position, targetTransform.position);

            // ����Ƿ��п��õĹ���
            int attackIndex = GetAvailableAttackIndex(distance);
            if (attackIndex != -1)
            {
                // �ڹ�����Χ�����п��ù���
                currentState = MonsterState.Attacking;
                Attack(target, attackIndex);
                attackCooldowns[attackIndex] = data.attacks[attackIndex].cooldown;
            }
            else
            {
                // û�п��ù�����Ŀ���ڹ�����Χ��
                currentState = MonsterState.Moving;
                Move(targetTransform.position);
            }
        }
        else
        {
            // û��Ŀ�꣬����״̬
            currentState = MonsterState.Idle;

            // ���¼��Ŀ��
            DetectTarget();
        }
    }

    // ���¹�����ȴʱ��
    protected virtual void UpdateAttackCooldowns()
    {
        for (int i = 0; i < attackCooldowns.Length; i++)
        {
            if (attackCooldowns[i] > 0)
            {
                attackCooldowns[i] -= Time.deltaTime;
            }
        }
    }

    // ��ȡ���õĹ������������ھ��룩
    protected virtual int GetAvailableAttackIndex(float distance)
    {
        for (int i = 0; i < data.attacks.Count; i++)
        {
            var attack = data.attacks[i];
            if (distance <= attack.attackRange && attackCooldowns[i] <= 0)
            {
                return i;
            }
        }
        return -1; // û�п��ù���
    }

    // �ƶ�����������ʵ�֣�
    public abstract void Move(Vector3 targetPosition);

    // ��������������ʵ�֣� - ����attackIndex����
    public abstract void Attack(IDamageable target, int attackIndex);

    // ʵ������˺�����������򶯻��¼����ã�
    public virtual void DealDamage(int attackIndex)
    {
        if (target != null && !target.IsDead && attackIndex >= 0 && attackIndex < data.attacks.Count)
        {
            target.TakeDamage(data.attacks[attackIndex].damage);
        }
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // ��ʾ�˺�Ʈ��
        ShowDamageText(damage, Color.red);

        // �����ܻ�����������У�
        PlayHitAnimation();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // ���˵�������
            StartCoroutine(RecoverFromHit());
        }
    }

    // ������״̬�ָ�
    private IEnumerator RecoverFromHit()
    {
        currentState = MonsterState.Attacking;
        yield return new WaitForSeconds(0.5f);
        if (!isDead && currentState == MonsterState.Attacking)
        {
            currentState = MonsterState.Idle;
        }
    }

    // ��ʾ�˺�Ʈ�ַ���
    private void ShowDamageText(int damage, Color color)
    {
        if (damageTextCanvas == null)
        {
            // ���Բ���Canvas
            damageTextCanvas = FindObjectOfType<Canvas>();
            if (damageTextCanvas == null)
            {
                Debug.LogWarning("δ�ҵ�Canvas������ʾ�˺�����");
                return;
            }
        }

        Vector3 worldPosition = transform.position + Vector3.up * damageTextYOffset;
        DamageText.Create(damageTextCanvas.transform, worldPosition, damage, color, transform);
    }

    protected virtual void Die()
    {
        isDead = true;
        currentState = MonsterState.Dead;

        // ������������
        PlayDeathAnimation();

        // �ӳٷ��ض����
        StartCoroutine(DelayedReturnToPool());
    }

    private IEnumerator DelayedReturnToPool()
    {
        // �ȴ�������������
        yield return new WaitForSeconds(1f); // Ĭ��1�룬����ɸ���
        ReturnToPool();
    }

    public void ReturnToPool()
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

    // Ŀ���⣨2D��
    protected virtual bool DetectTarget()
    {
        // �����ǰĿ����Ч��ֱ�ӷ���
        if (target != null && !target.IsDead && targetTransform != null &&
            targetTransform.gameObject.activeInHierarchy &&
            Vector2.Distance(transform.position, targetTransform.position) <= data.detectionRange * 1.5f)
        {
            return true;
        }

        // ����Ŀ��
        target = null;
        targetTransform = null;

        // 2D���������
        Collider2D playerCollider = Physics2D.OverlapCircle(
            transform.position,
            data.detectionRange,
            playerLayerMask
        );

        if (playerCollider != null)
        {
            // ���������
            IDamageable player = playerCollider.GetComponent<IDamageable>();
            if (player != null && !player.IsDead)
            {
                target = player;
                targetTransform = playerCollider.transform;
                return true;
            }
        }

        return false;
    }

    // IPoolable �ӿ�ʵ��
    public virtual void OnSpawn()
    {
        isDead = false;
        currentState = MonsterState.Idle;
        currentHealth = data.health;
        gameObject.SetActive(true);

        // ���ù�����ȴ
        for (int i = 0; i < attackCooldowns.Length; i++)
        {
            attackCooldowns[i] = 0f;
        }

        // ���ü�ʱ��
        lastMoveTime = Time.time;

        // ���¼��Ŀ��
        DetectTarget();
    }

    public virtual void OnReturn()
    {
        target = null;
        isDead = true;
        currentState = MonsterState.Dead;
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    #region ���������������ѡʵ�֣�
    protected virtual void PlayHitAnimation()
    {
        // ����ʵ�־����ܻ�����
    }

    protected virtual void PlayDeathAnimation()
    {
        // ����ʵ�־�����������
    }

    protected virtual void PlayAttackAnimation(int attackIndex)
    {
        // ����ʵ�־��幥������
    }
    #endregion

    #region ���Թ���
    [Header("��������")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color detectionColor = Color.yellow;

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || data == null) return;

        // ���Ƽ�ⷶΧ
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, data.detectionRange);

        // �������й�����Χ
        if (data.attacks != null)
        {
            foreach (var attack in data.attacks)
            {
                Gizmos.color = attack.attackRange > data.detectionRange ? Color.red : Color.magenta;
                Gizmos.DrawWireSphere(transform.position, attack.attackRange);
            }
        }
    }
    #endregion
}