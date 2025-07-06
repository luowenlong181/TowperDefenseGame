// MonsterBase.cs
using UnityEngine;
using System.Collections;
using System.Linq;

public abstract class MonsterBase : MonoBehaviour, IDamageable, IPoolable
{
    [Header("怪物数据")]
    public MonsterData data;

    [Header("2D物理设置")]
    [SerializeField] public LayerMask playerLayerMask;

    [Header("飘字设置")]
    [SerializeField] private Canvas damageTextCanvas;
    [SerializeField] private float damageTextYOffset = 1f;
    protected bool isDead;
    protected string poolTag;

    // 当前血量
    protected int currentHealth;

    // 攻击冷却计时器
    protected float[] attackCooldowns;

    // 实现 IDamageable 接口
    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => data.health;

    // 怪物状态
    protected enum MonsterState { Idle, Moving, Attacking, Dead }
    protected MonsterState currentState = MonsterState.Idle;

    // 移动和攻击计时器
    protected float lastMoveTime;
    // 修改目标变量为 IDamageable 类型
    protected IDamageable target;
    protected Transform targetTransform; // 用于位置跟踪

    public virtual void Initialize(MonsterData monsterData)
    {
        data = monsterData;
        currentHealth = data.health;
        isDead = false;
        currentState = MonsterState.Idle;
        poolTag = $"Monster_{data.id}";

        // 初始化攻击冷却数组
        attackCooldowns = new float[data.attacks.Count];
        for (int i = 0; i < attackCooldowns.Length; i++)
        {
            attackCooldowns[i] = 0f;
        }

        // 重置计时器
        lastMoveTime = Time.time;
    }

    protected virtual void Awake()
    {
        // 确保有碰撞体
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true; // 设置为触发器避免物理碰撞
        }

        // 添加2D刚体
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        // 优化物理设置
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;

        // 自动设置玩家层级掩码（如果未在Inspector中设置）
        if (playerLayerMask.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                playerLayerMask = 1 << playerLayer;
                Debug.Log($"{name}: 自动设置 playerLayerMask 为 Player 层级");
            }
            else
            {
                Debug.LogError($"{name}: 未找到 'Player' 层级! 请创建该层级");
            }
        }
    }
    // 在游戏初始化时设置物理层碰撞关系
    public class GameInitializer : MonoBehaviour
    {
        void Start()
        {
            // 禁止怪物之间碰撞
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Enemy"),
                LayerMask.NameToLayer("Enemy")
            );

            // 允许怪物与玩家碰撞
            Physics2D.IgnoreLayerCollision(
                LayerMask.NameToLayer("Enemy"),
                LayerMask.NameToLayer("Player"),
                false
            );

            // 允许怪物与子弹碰撞
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

        // 更新攻击冷却
        UpdateAttackCooldowns();

        // 确保目标有效
        if (target != null && !target.IsDead && targetTransform != null && targetTransform.gameObject.activeInHierarchy)
        {
            float distance = Vector2.Distance(transform.position, targetTransform.position);

            // 检查是否有可用的攻击
            int attackIndex = GetAvailableAttackIndex(distance);
            if (attackIndex != -1)
            {
                // 在攻击范围内且有可用攻击
                currentState = MonsterState.Attacking;
                Attack(target, attackIndex);
                attackCooldowns[attackIndex] = data.attacks[attackIndex].cooldown;
            }
            else
            {
                // 没有可用攻击或目标在攻击范围外
                currentState = MonsterState.Moving;
                Move(targetTransform.position);
            }
        }
        else
        {
            // 没有目标，空闲状态
            currentState = MonsterState.Idle;

            // 重新检测目标
            DetectTarget();
        }
    }

    // 更新攻击冷却时间
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

    // 获取可用的攻击索引（基于距离）
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
        return -1; // 没有可用攻击
    }

    // 移动方法（子类实现）
    public abstract void Move(Vector3 targetPosition);

    // 攻击方法（子类实现） - 新增attackIndex参数
    public abstract void Attack(IDamageable target, int attackIndex);

    // 实际造成伤害（可由子类或动画事件调用）
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

        // 显示伤害飘字
        ShowDamageText(damage, Color.red);

        // 播放受击动画（如果有）
        PlayHitAnimation();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 受伤但不死亡
            StartCoroutine(RecoverFromHit());
        }
    }

    // 从受伤状态恢复
    private IEnumerator RecoverFromHit()
    {
        currentState = MonsterState.Attacking;
        yield return new WaitForSeconds(0.5f);
        if (!isDead && currentState == MonsterState.Attacking)
        {
            currentState = MonsterState.Idle;
        }
    }

    // 显示伤害飘字方法
    private void ShowDamageText(int damage, Color color)
    {
        if (damageTextCanvas == null)
        {
            // 尝试查找Canvas
            damageTextCanvas = FindObjectOfType<Canvas>();
            if (damageTextCanvas == null)
            {
                Debug.LogWarning("未找到Canvas用于显示伤害文字");
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

        // 播放死亡动画
        PlayDeathAnimation();

        // 延迟返回对象池
        StartCoroutine(DelayedReturnToPool());
    }

    private IEnumerator DelayedReturnToPool()
    {
        // 等待死亡动画播放
        yield return new WaitForSeconds(1f); // 默认1秒，子类可覆盖
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

    // 目标检测（2D）
    protected virtual bool DetectTarget()
    {
        // 如果当前目标有效，直接返回
        if (target != null && !target.IsDead && targetTransform != null &&
            targetTransform.gameObject.activeInHierarchy &&
            Vector2.Distance(transform.position, targetTransform.position) <= data.detectionRange * 1.5f)
        {
            return true;
        }

        // 重置目标
        target = null;
        targetTransform = null;

        // 2D物理检测玩家
        Collider2D playerCollider = Physics2D.OverlapCircle(
            transform.position,
            data.detectionRange,
            playerLayerMask
        );

        if (playerCollider != null)
        {
            // 检查玩家组件
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

    // IPoolable 接口实现
    public virtual void OnSpawn()
    {
        isDead = false;
        currentState = MonsterState.Idle;
        currentHealth = data.health;
        gameObject.SetActive(true);

        // 重置攻击冷却
        for (int i = 0; i < attackCooldowns.Length; i++)
        {
            attackCooldowns[i] = 0f;
        }

        // 重置计时器
        lastMoveTime = Time.time;

        // 重新检测目标
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

    #region 动画方法（子类可选实现）
    protected virtual void PlayHitAnimation()
    {
        // 子类实现具体受击动画
    }

    protected virtual void PlayDeathAnimation()
    {
        // 子类实现具体死亡动画
    }

    protected virtual void PlayAttackAnimation(int attackIndex)
    {
        // 子类实现具体攻击动画
    }
    #endregion

    #region 调试工具
    [Header("调试设置")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color detectionColor = Color.yellow;

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || data == null) return;

        // 绘制检测范围
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, data.detectionRange);

        // 绘制所有攻击范围
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