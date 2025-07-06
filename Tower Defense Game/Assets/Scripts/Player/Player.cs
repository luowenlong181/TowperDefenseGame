using UnityEngine;
using Spine.Unity;
using System.Collections;
using UnityEngine.UI;

public class Player : MonoBehaviour, IDamageable
{
    [Header("角色属性")]
    [SerializeField] private int maxHealth = 10000;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float autoAttackDetectionRadius = 5f;

    [Header("2D物理设置")]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private bool ignoreTriggers = true;

    [Header("动画设置")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private Transform attackPoint;

    [Header("视觉反馈")]
    [SerializeField] private Canvas damageTextCanvas;
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private float damageTextYOffset = 1.5f;
    // 添加调试模式
    [Header("调试设置")]
    [SerializeField] private bool debugMode = true;
    // 当前状态
    private int currentHealth;
    private float lastAttackTime;
    private bool isDead;
    private bool canAttack = true;
    private IDamageable currentTarget;
    private Transform currentTargetTransform;

    // Spine组件
    private Spine.AnimationState spineAnimationState;
    private Spine.Skeleton skeleton;

    // 动画名称
    private const string IDLE_ANIMATION = "unit_wait";
    private const string WALK_ANIMATION = "unit_walk";
    private const string ATTACK_ANIMATION = "unit_attack";
    private const string HIT_ANIMATION = "unit_damage";
    private const string DEATH_ANIMATION = "unit_dead";

    // 自动攻击相关
    private Collider2D[] attackTargets = new Collider2D[10];
    private float targetDetectionTimer = 0f;
    private const float TARGET_DETECTION_INTERVAL = 0.2f;

    // 实现 IDamageable 接口
    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        // 初始化Spine组件
        if (skeletonAnimation != null)
        {
            spineAnimationState = skeletonAnimation.AnimationState;
            skeleton = skeletonAnimation.Skeleton;
        }

        // 确保有攻击点
        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        // 添加2D刚体（如果不存在）
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // 自动设置敌人层级掩码（如果未在Inspector中设置）
        if (enemyLayerMask.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                enemyLayerMask = 1 << enemyLayer;
                Debug.Log($"{name}: 自动设置 enemyLayerMask 为 Enemy 层级");
            }
            else
            {
                Debug.LogError($"{name}: 未找到 'Enemy' 层级! 请创建该层级");
            }
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        canAttack = true;

        // 初始动画
        SetIdleAnimation();
    }

    private void Update()
    {
        if (isDead) return;

        // 目标检测计时器
        targetDetectionTimer += Time.deltaTime;
        if (targetDetectionTimer >= TARGET_DETECTION_INTERVAL)
        {
            targetDetectionTimer = 0f;
            DetectAttackTargets();
        }

        // 如果有当前目标，处理攻击逻辑
        if (currentTarget != null && canAttack)
        {
            // 检查目标是否有效
            if (currentTarget.IsDead || currentTargetTransform == null ||
                !currentTargetTransform.gameObject.activeInHierarchy)
            {
                //if (debugMode) Debug.Log("目标无效，重置目标");
                currentTarget = null;
                currentTargetTransform = null;
                return;
            }

            // 获取目标位置
            Vector2 targetPosition = currentTargetTransform.position;

            // 检查距离
            float distance = Vector2.Distance(transform.position, targetPosition);

            if (distance <= attackRange)
            {
                //if (debugMode) Debug.Log($"目标在攻击范围内 ({distance:F2}/{attackRange})");

                // 在攻击范围内
                if (Time.time - lastAttackTime >= attackInterval)
                {
                    //if (debugMode) Debug.Log("满足攻击条件，发起攻击");
                    Attack(currentTarget);
                    lastAttackTime = Time.time;
                }
                else
                {
                    //if (debugMode) Debug.Log($"攻击冷却中: {Time.time - lastAttackTime:F2}/{attackInterval}");
                }
            }
            else
            {
                //if (debugMode) Debug.Log($"目标超出攻击范围 ({distance:F2}/{attackRange})");

                if (distance > autoAttackDetectionRadius * 1.2f)
                {
                    //if (debugMode) Debug.Log("目标超出检测范围，重置目标");
                    // 目标超出检测范围，重置目标
                    currentTarget = null;
                    currentTargetTransform = null;
                }
            }
        }
        else
        {
            //if (debugMode && currentTarget == null) Debug.Log("没有有效目标");
            // 没有目标，保持空闲状态
            SetIdleAnimation();
        }
    }
    #region 目标检测系统（2D版本）

    private void DetectAttackTargets()
    {
        // 如果当前目标有效，直接返回
        if (currentTarget != null && !currentTarget.IsDead && currentTargetTransform != null)
        {
            if (debugMode) Debug.Log("已有有效目标，跳过检测");
            return;
        }

        // 重置当前目标
        currentTarget = null;
        currentTargetTransform = null;

        // 创建接触过滤器
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(enemyLayerMask);
        contactFilter.useTriggers = !ignoreTriggers;

        if (debugMode) Debug.Log($"开始检测敌人: 半径={autoAttackDetectionRadius}, 层级={enemyLayerMask.value}");

        // 检测范围内的所有敌人（2D物理）
        int numTargets = Physics2D.OverlapCircle(
            transform.position,
            autoAttackDetectionRadius,
            contactFilter,
            attackTargets
        );

        if (debugMode) Debug.Log($"检测到 {numTargets} 个碰撞体");

        if (numTargets == 0)
        {
            if (debugMode) Debug.Log("未检测到敌人");
            return;
        }

        // 寻找最近的敌人
        float closestDistance = float.MaxValue;
        IDamageable closestTarget = null;
        Transform closestTransform = null;
        bool foundValidTarget = false;

        for (int i = 0; i < numTargets; i++)
        {
            Collider2D col = attackTargets[i];
            if (col == null) continue;

            if (debugMode) Debug.Log($"检测到碰撞体: {col.name}");

            // 检查碰撞体是否有IDamageable组件
            IDamageable target = col.GetComponent<IDamageable>();
            if (target == null)
            {
                // 尝试在父对象中查找
                target = col.GetComponentInParent<IDamageable>();
                if (debugMode) Debug.Log($"在 {col.name} 中未找到 IDamageable，尝试父对象: {target != null}");
            }

            // 检查目标是否有效
            if (target == null)
            {
                if (debugMode) Debug.Log($"目标 {col.name} 没有 IDamageable 组件");
                continue;
            }

            if (target.IsDead)
            {
                if (debugMode) Debug.Log($"目标 {col.name} 已死亡");
                continue;
            }

            float distance = Vector2.Distance(transform.position, col.transform.position);
            if (debugMode) Debug.Log($"有效目标: {col.name}, 距离: {distance:F2}");

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
                closestTransform = col.transform;
                foundValidTarget = true;
            }
        }

        // 设置当前目标
        if (foundValidTarget && closestTarget != null && closestTransform != null)
        {
            if (debugMode) Debug.Log($"设置目标: {closestTransform.name}, 距离: {closestDistance:F2}");
            currentTarget = closestTarget;
            currentTargetTransform = closestTransform;
            FaceTarget(currentTarget);
        }
        else
        {
            if (debugMode) Debug.Log("未找到有效目标");
        }
    }

    // 面向目标（2D）
    private void FaceTarget(IDamageable target)
    {
        if (skeleton == null || currentTargetTransform == null) return;

        Vector2 direction = currentTargetTransform.position - transform.position;

        // 根据移动方向翻转角色
        if (direction.x < 0)
        {
            skeleton.ScaleX = -1f; // 面向左
            if (debugMode) Debug.Log("面向左");
        }
        else if (direction.x > 0)
        {
            skeleton.ScaleX = 1f;  // 面向右
            if (debugMode) Debug.Log("面向右");
        }
    }
    #endregion
    #region 战斗系统

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // 显示伤害飘字
        ShowDamageText(damage, Color.red);

        // 播放受击动画
        PlayHitAnimation();

        // 播放受击特效
        PlayHitEffect();

        // 检查死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 攻击方法
    private void Attack(IDamageable target)
    {
        if (isDead || target == null) return;

        // 面向目标
        FaceTarget(target);

        // 播放攻击动画
        PlayAttackAnimation();

        // 实际造成伤害（在动画事件中触发）
        DealDamage();
    }

    // 实际造成伤害（由动画事件调用）
    public void DealDamage()
    {
        if (currentTarget != null && !currentTarget.IsDead)
        {
            currentTarget.TakeDamage(attackDamage);
        }
        else
        {
            // 目标已死亡，重置当前目标
            currentTarget = null;
            currentTargetTransform = null;
        }
    }

    private void Die()
    {
        isDead = true;
        canAttack = false;

        // 播放死亡动画
        PlayDeathAnimation();

        // 游戏结束逻辑
        Debug.Log("玩家死亡!");
        // GameManager.Instance.GameOver();
    }

    #endregion

    #region 目标检测系统（2D版本）



    #endregion

    #region 视觉反馈系统

    // 显示伤害飘字
    private void ShowDamageText(int damage, Color color)
    {
        if (damageTextCanvas == null)
        {
            damageTextCanvas = FindObjectOfType<Canvas>();
            if (damageTextCanvas == null) return;
        }

        Vector3 worldPosition = transform.position + Vector3.up * damageTextYOffset;
        DamageText.Create(damageTextCanvas.transform, worldPosition, damage, color, transform);
    }

    // 播放受击特效
    private void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            hitEffect.transform.position = transform.position + Vector3.up * 0.5f;
            hitEffect.Play();
        }
    }

    #endregion

    #region Spine动画控制

    private void SetIdleAnimation()
    {
        if (spineAnimationState != null)
        {
            spineAnimationState.SetAnimation(0, IDLE_ANIMATION, true);
        }
    }

    private void PlayAttackAnimation()
    {
        if (spineAnimationState != null)
        {
            // 使用Track 1播放攻击动画（覆盖基础动画）
            var attackTrack = spineAnimationState.SetAnimation(1, ATTACK_ANIMATION, false);
            attackTrack.MixDuration = 0.1f;

            // 攻击动画结束后恢复基础动画
            attackTrack.Complete += track => {
                if (!isDead)
                {
                    spineAnimationState.SetEmptyAnimation(1, 0.1f);
                }
            };
        }
    }

    private void PlayHitAnimation()
    {
        if (spineAnimationState != null)
        {
            // 使用Track 2播放受击动画
            var hitTrack = spineAnimationState.SetAnimation(2, HIT_ANIMATION, false);
            hitTrack.MixDuration = 0.1f;

            // 受击动画结束后恢复
            hitTrack.Complete += track => {
                if (!isDead)
                {
                    spineAnimationState.SetEmptyAnimation(2, 0.1f);
                }
            };
        }
    }

    private void PlayDeathAnimation()
    {
        if (spineAnimationState != null)
        {
            // 清空所有动画轨道
            spineAnimationState.ClearTracks();

            // 播放死亡动画（不循环）
            var deathTrack = spineAnimationState.SetAnimation(0, DEATH_ANIMATION, false);
        }
    }

    #endregion

    #region 调试工具（2D）

    [Header("调试设置")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color detectionColor = new Color(1, 1, 0, 0.5f);
    [SerializeField] private Color attackRangeColor = new Color(1, 0, 0, 0.3f);

    // 在场景视图中可视化检测范围（2D）
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // 绘制检测范围（2D圆形）
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, autoAttackDetectionRadius);

        // 绘制攻击范围（2D圆形）
        Gizmos.color = attackRangeColor;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    #endregion
}