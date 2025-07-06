using UnityEngine;
using Spine.Unity;

public class MeleeMonster : MonsterBase
{
    private SkeletonAnimation skeletonAnimation;
    private Spine.AnimationState spineAnimationState;
    private Spine.Skeleton skeleton;

    // Spine动画名称 - 使用常量或从数据中获取
    private const string DEFAULT_IDLE_ANIMATION = "std";
    private const string DEFAULT_WALK_ANIMATION = "walk";
    private const string DEFAULT_ATTACK_ANIMATION = "atk";
    private const string DEFAULT_DEATH_ANIMATION = "death";

    // 攻击状态
    private bool isAttacking = false;
    private int currentAttackIndex = -1;

    public override void Initialize(MonsterData monsterData)
    {
        base.Initialize(monsterData);

        // 确保组件存在
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null)
        {
            Debug.LogError($"近战怪物 {data.name} 缺少SkeletonAnimation组件");
            return;
        }

        spineAnimationState = skeletonAnimation.AnimationState;
        skeleton = skeletonAnimation.Skeleton;

        // 设置初始动画
        SetIdleAnimation();
    }

    // 修改攻击方法，增加attackIndex参数
    public override void Attack(IDamageable target, int attackIndex)
    {
        if (isDead || target == null || target.IsDead) return;

        // 记录当前攻击索引
        currentAttackIndex = attackIndex;

        // 面向目标
        Vector3 direction = ((MonoBehaviour)target).transform.position - transform.position;
        UpdateFacingDirection(direction.x);

        // 播放攻击动画
        PlayAttackAnimation(attackIndex);
        OnAttackEvent();
        // 重置攻击冷却时间
        if (attackIndex >= 0 && attackIndex < attackCooldowns.Length)
        {
            attackCooldowns[attackIndex] = data.attacks[attackIndex].cooldown;
        }
    }

    // 动画事件方法（由Spine事件调用） - 用于造成伤害
    public void OnAttackEvent()
    {
        if (isDead || currentAttackIndex == -1) return;

        // 确保攻击索引有效
        if (currentAttackIndex >= 0 && currentAttackIndex < data.attacks.Count)
        {
            // 获取当前攻击配置
            AttackAction attack = data.attacks[currentAttackIndex];

            // 检测并伤害范围内的目标
            ApplyMeleeDamage(attack.attackRange, attack.damage);
        }
    }

    // 应用近战伤害
    private void ApplyMeleeDamage(float range, int damage)
    {
        // 检测范围内的目标
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, range, playerLayerMask);

        foreach (var collider in hitColliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    // 修改为支持多种攻击动画
    protected override void PlayAttackAnimation(int attackIndex)
    {
        if (spineAnimationState == null) return;

        // 获取攻击动画名称（支持多种攻击动画）
        string attackAnim = GetAttackAnimationName(attackIndex);

        // 调试日志 - 确保动画名称正确
        Debug.Log($"播放攻击动画: {attackAnim} (索引: {attackIndex})");

        // 使用Track 1播放攻击动画
        var attackTrack = spineAnimationState.SetAnimation(1, attackAnim, false);
        attackTrack.MixDuration = 0.1f;

        // 标记攻击状态
        isAttacking = true;

        attackTrack.Complete += track => {
            isAttacking = false;
            currentAttackIndex = -1;
            if (!isDead)
            {
                spineAnimationState.SetEmptyAnimation(1, 0.1f);
            }
        };
    }

    // 获取攻击动画名称（根据攻击索引）
    private string GetAttackAnimationName(int attackIndex)
    {
        // 默认使用基础攻击动画
        if (attackIndex < 0 || attackIndex >= data.attacks.Count || data.attacks.Count == 1)
            return DEFAULT_ATTACK_ANIMATION;

        // 如果攻击配置有指定动画名称，则使用它
        if (!string.IsNullOrEmpty(data.attacks[attackIndex].name))
            return data.attacks[attackIndex].name;

        // 否则使用默认攻击动画
        return DEFAULT_ATTACK_ANIMATION;
    }

    private void UpdateFacingDirection(float horizontalDirection)
    {
        if (skeleton == null) return;

        // 根据方向翻转角色
        if (horizontalDirection < 0)
        {
            skeleton.ScaleX = -1f;
        }
        else if (horizontalDirection > 0)
        {
            skeleton.ScaleX = 1f;
        }
    }

    // 更新移动方法
    public override void Move(Vector3 targetPosition)
    {
        if (isDead || isAttacking) return;

        // 使用更平滑的移动方式
        Vector3 direction = (targetPosition - transform.position).normalized;

        // 添加避免拥挤的逻辑
        Vector3 avoidVector = CalculateAvoidanceVector();
        if (avoidVector != Vector3.zero)
        {
            direction += avoidVector * 0.5f; // 部分避开其他怪物
            direction.Normalize();
        }

        // 使用刚体移动而不是直接设置位置
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * data.moveSpeed;
        }
        else
        {
            transform.position += direction * data.moveSpeed * Time.deltaTime;
        }

        // 更新朝向
        UpdateFacingDirection(direction.x);

        // 更新动画状态
        if (spineAnimationState != null)
        {
            var currentTrack = spineAnimationState.GetCurrent(0);
            string currentAnim = currentTrack?.Animation?.Name ?? "";

            // 如果没有移动动画或当前不是移动动画，则设置为移动动画
            if (string.IsNullOrEmpty(currentAnim) || currentAnim != GetWalkAnimationName())
            {
                SetWalkAnimation();
            }
        }
    }
    private Vector3 CalculateAvoidanceVector()
    {
        Vector3 avoidance = Vector3.zero;
        int count = 0;

        // 检测附近的怪物
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position,
            data.detectionRange * 0.5f,
            LayerMask.GetMask("Monsters")
        );

        foreach (Collider2D col in nearby)
        {
            // 排除自己
            if (col.gameObject == gameObject) continue;

            // 计算避开方向
            Vector3 diff = transform.position - col.transform.position;
            float dist = diff.magnitude;

            if (dist < 0.1f) dist = 0.1f; // 防止除以零

            avoidance += diff.normalized / dist;
            count++;
        }

        if (count > 0)
        {
            avoidance /= count;
        }

        return avoidance;
    }
    private void SetIdleAnimation()
    {
        if (spineAnimationState != null)
        {
            spineAnimationState.SetAnimation(0, GetIdleAnimationName(), true);
        }
    }

    private void SetWalkAnimation()
    {
        if (spineAnimationState != null)
        {
            spineAnimationState.SetAnimation(0, GetWalkAnimationName(), true);
        }
    }

    // 获取空闲动画名称
    private string GetIdleAnimationName()
    {
        return !string.IsNullOrEmpty(data.idleAnimation) ?
            data.idleAnimation : DEFAULT_IDLE_ANIMATION;
    }

    // 获取行走动画名称
    private string GetWalkAnimationName()
    {
        return !string.IsNullOrEmpty(data.walkAnimation) ?
            data.walkAnimation : DEFAULT_WALK_ANIMATION;
    }

    // 获取死亡动画名称
    private string GetDeathAnimationName()
    {
        return !string.IsNullOrEmpty(data.deathAnimation) ?
            data.deathAnimation : DEFAULT_DEATH_ANIMATION;
    }

    // 处理受击动画
    protected override void PlayHitAnimation()
    {
        //if (spineAnimationState != null && !string.IsNullOrEmpty(data.hitAnimation))
        //{
        //    // 使用Track 2播放受击动画
        //    var hitTrack = spineAnimationState.SetAnimation(2, data.hitAnimation, false);
        //    hitTrack.MixDuration = 0.1f;

        //    // 受击动画结束后恢复
        //    hitTrack.Complete += track => {
        //        if (!isDead)
        //        {
        //            spineAnimationState.SetEmptyAnimation(2, 0.1f);
        //        }
        //    };
        //}
    }

    // 处理死亡动画
    protected override void PlayDeathAnimation()
    {
        if (spineAnimationState != null)
        {
            // 清空所有动画轨道
            spineAnimationState.ClearTracks();

            // 获取死亡动画名称
            string deathAnim = GetDeathAnimationName();

            // 播放死亡动画（不循环）
            var deathTrack = spineAnimationState.SetAnimation(0, deathAnim, false);

            // 死亡动画结束后处理
            deathTrack.Complete += track => {
                // 确保对象被回收
                ReturnToPool();
            };
        }
        else
        {
            // 如果没有动画系统，直接回收
            ReturnToPool();
        }
    }

    // 在MonsterData中添加动画名称字段
    /*
    [System.Serializable]
    public class MonsterData
    {
        // ...其他字段...
        
        [Header("动画名称")]
        public string idleAnimation = "std";
        public string walkAnimation = "walk";
        public string attackAnimation = "atk";
        public string hitAnimation = "hit";
        public string deathAnimation = "death";
        
        // ...其他字段...
    }
    */
}