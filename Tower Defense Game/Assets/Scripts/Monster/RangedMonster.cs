// RangedMonster.cs
using UnityEngine;
using Spine.Unity;

public class RangedMonster : MonsterBase
{
    private SkeletonAnimation skeletonAnimation;
    private Spine.AnimationState spineAnimationState;
    private Spine.Skeleton skeleton;

    // Spine动画名称
    private const string IDLE_ANIMATION = "idle";
    private const string WALK_ANIMATION = "walk";
    private const string ATTACK_ANIMATION = "attack";
    private const string HIT_ANIMATION = "hit";
    private const string DEATH_ANIMATION = "death";

    // 攻击效果位置
    [SerializeField] private Transform projectileSpawnPoint;

    public override void Initialize(MonsterData monsterData)
    {
        base.Initialize(monsterData);

        // 获取Spine组件
        skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (skeletonAnimation != null)
        {
            spineAnimationState = skeletonAnimation.AnimationState;
            skeleton = skeletonAnimation.Skeleton;

            // 设置初始动画
            SetIdleAnimation();
        }
        else
        {
            Debug.LogError($"远程怪物 {data.name} 缺少SkeletonAnimation组件");
        }

        // 确保有子弹生成点
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
    }

    public override void Move(Vector3 targetPosition)
    {
        if (isDead) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * data.moveSpeed * Time.deltaTime;

        // 更新朝向
        UpdateFacingDirection(direction.x);

        // 更新动画状态
        if (spineAnimationState != null)
        {
            // 检查是否正在播放移动动画
            var currentTrack = spineAnimationState.GetCurrent(0);
            if (currentTrack == null || currentTrack.Animation.Name != WALK_ANIMATION)
            {
                SetWalkAnimation();
            }
        }
    }

    // 修改攻击方法，增加attackIndex参数
    public override void Attack(IDamageable target, int attackIndex)
    {
        if (isDead) return;

        // 保持朝向目标
        if (target != null)
        {
            Vector3 direction = ((MonoBehaviour)target).transform.position - transform.position;
            UpdateFacingDirection(direction.x);
        }

        // 执行攻击
        ShootProjectile(target, attackIndex);

        // 播放攻击动画
        PlayAttackAnimation(attackIndex);
    }

    public override void TakeDamage(int damage)
    {
        if (isDead) return;

        base.TakeDamage(damage);

        // 播放受击动画
        PlayHitAnimation();
    }

    protected override void Die()
    {
        base.Die();

        // 播放死亡动画
        PlayDeathAnimation();
    }

    #region Spine动画控制

    private void SetIdleAnimation()
    {
        if (spineAnimationState != null)
        {
            spineAnimationState.SetAnimation(0, IDLE_ANIMATION, true);
        }
    }

    private void SetWalkAnimation()
    {
        if (spineAnimationState != null)
        {
            spineAnimationState.SetAnimation(0, WALK_ANIMATION, true);
        }
    }

    // 修改为支持多种攻击动画
    protected override void PlayAttackAnimation(int attackIndex)
    {
        if (spineAnimationState != null)
        {
            // 获取攻击动画名称（支持多种攻击动画）
            string attackAnim = GetAttackAnimationName(attackIndex);

            // 使用Track 1播放攻击动画（覆盖基础动画）
            var attackTrack = spineAnimationState.SetAnimation(1, attackAnim, false);
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

    // 获取攻击动画名称（根据攻击索引）
    private string GetAttackAnimationName(int attackIndex)
    {
        // 默认使用基础攻击动画
        if (attackIndex < 0 || attackIndex >= data.attacks.Count)
            return ATTACK_ANIMATION;

        // 如果攻击配置有指定动画名称，则使用它
        if (!string.IsNullOrEmpty(data.attacks[attackIndex].name))
            return data.attacks[attackIndex].name;

        // 否则使用默认攻击动画
        return ATTACK_ANIMATION;
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

    private void UpdateFacingDirection(float moveDirection)
    {
        if (skeleton != null)
        {
            // 根据移动方向翻转角色
            if (moveDirection < 0)
            {
                skeleton.ScaleX = -1f; // 面向左
            }
            else if (moveDirection > 0)
            {
                skeleton.ScaleX = 1f;  // 面向右
            }
            // 注意：如果原始资源是朝左的，可能需要反转这些值
        }
    }

    #endregion

    #region 远程攻击系统

    // 修改为支持多种攻击方式
    private void ShootProjectile(IDamageable target, int attackIndex)
    {
        if (attackIndex < 0 || attackIndex >= data.attacks.Count)
        {
            Debug.LogWarning($"无效的攻击索引: {attackIndex}");
            return;
        }

        AttackAction attack = data.attacks[attackIndex];

        if (string.IsNullOrEmpty(attack.prefabPath))
        {
            Debug.LogWarning($"远程怪物 {data.name} 未设置子弹预制体路径");
            return;
        }

        if (target == null || !(target is MonoBehaviour))
        {
            Debug.LogWarning("无效的攻击目标");
            return;
        }

        // 使用对象池获取子弹
        GameObject projectile = ObjectPool.Instance.SpawnFromPool(
            attack.prefabPath, // 使用攻击配置的预制体路径作为池标签
            projectileSpawnPoint.position,
            Quaternion.identity
        );

        if (projectile == null)
        {
            Debug.LogError($"无法从对象池获取子弹实例: {attack.prefabPath}");
            return;
        }

        // 配置子弹
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.SetDamage(attack.damage);
            projScript.SetTarget(((MonoBehaviour)target).transform);
        }
        else
        {
            Debug.LogError("子弹预制体缺少Projectile组件");
        }

        // 可选：添加发射特效
        PlayShootEffect(attackIndex);

        Debug.Log($"{data.name} 发射 {attack.name} 子弹!");
    }

    private void PlayShootEffect(int attackIndex)
    {
        // 这里可以根据攻击类型添加不同的特效
        // 例如：GameObject shootEffect = ObjectPool.Instance.SpawnFromPool("MuzzleFlash", projectileSpawnPoint.position, Quaternion.identity);
        // shootEffect.GetComponent<ParticleSystem>().Play();
    }

    #endregion
}