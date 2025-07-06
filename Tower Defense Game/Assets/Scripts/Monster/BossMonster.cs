// BossMonster.cs (新增)
using UnityEngine;
using Spine.Unity;

public class BossMonster : MonsterBase
{
    private SkeletonAnimation skeletonAnimation;
    private Spine.AnimationState spineAnimationState;
    private Spine.Skeleton skeleton;

    // Spine动画名称
    private const string IDLE_ANIMATION = "idle";
    private const string WALK_ANIMATION = "walk";
    private const string HIT_ANIMATION = "hit";
    private const string DEATH_ANIMATION = "death";

    // 攻击效果位置
    [SerializeField] private Transform[] projectileSpawnPoints;

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
            Debug.LogError($"Boss {data.name} 缺少SkeletonAnimation组件");
        }

        // 确保有子弹生成点
        if (projectileSpawnPoints == null || projectileSpawnPoints.Length == 0)
        {
            projectileSpawnPoints = new Transform[] { transform };
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

    // Boss特有的攻击逻辑
    public override void Attack(IDamageable target, int attackIndex)
    {
        if (isDead) return;

        // 保持朝向目标
        if (target != null)
        {
            Vector3 direction = ((MonoBehaviour)target).transform.position - transform.position;
            UpdateFacingDirection(direction.x);
        }

        // 执行特定攻击
        AttackAction attack = data.attacks[attackIndex];

        switch (attack.name.ToLower())
        {
            case "melee":
                PerformMeleeAttack(attackIndex);
                break;
            case "projectile":
                ShootProjectile(target, attackIndex);
                break;
            case "aoe":
                PerformAoeAttack(attackIndex);
                break;
            default:
                ShootProjectile(target, attackIndex);
                break;
        }

        // 播放攻击动画
        PlayAttackAnimation(attackIndex);
    }

    protected override void PlayAttackAnimation(int attackIndex)
    {
        if (spineAnimationState != null)
        {
            // 获取攻击动画名称
            string attackAnim = GetAttackAnimationName(attackIndex);

            // 使用Track 1播放攻击动画
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
        if (attackIndex < 0 || attackIndex >= data.attacks.Count)
            return "attack_default";

        AttackAction attack = data.attacks[attackIndex];

        // 根据攻击类型返回不同的动画名称
        switch (attack.name.ToLower())
        {
            case "melee":
                return "attack_melee";
            case "projectile":
                return "attack_projectile";
            case "aoe":
                return "attack_aoe";
            default:
                return "attack_default";
        }
    }

    #region 攻击方法
    private void PerformMeleeAttack(int attackIndex)
    {
        AttackAction attack = data.attacks[attackIndex];

        // 在攻击动画中调用DealDamage方法
        // 动画事件会触发实际伤害
    }

    private void ShootProjectile(IDamageable target, int attackIndex)
    {
        AttackAction attack = data.attacks[attackIndex];

        if (string.IsNullOrEmpty(attack.prefabPath))
        {
            Debug.LogWarning($"Boss {data.name} 未设置子弹预制体路径");
            return;
        }

        if (target == null || !(target is MonoBehaviour))
        {
            Debug.LogWarning("无效的攻击目标");
            return;
        }

        // 随机选择一个生成点
        Transform spawnPoint = projectileSpawnPoints[Random.Range(0, projectileSpawnPoints.Length)];

        // 使用对象池获取子弹
        GameObject projectile = ObjectPool.Instance.SpawnFromPool(
            attack.prefabPath,
            spawnPoint.position,
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

        Debug.Log($"{data.name} 发射 {attack.name} 子弹!");
    }

    // 在 BossMonster 类中修改 PerformAoeAttack 方法
    private void PerformAoeAttack(int attackIndex)
    {
        AttackAction attack = data.attacks[attackIndex];

        // 在当前位置创建AOE效果
        GameObject aoeEffect = ObjectPool.Instance.SpawnFromPool(
            attack.prefabPath,
            transform.position,
            Quaternion.identity
        );

        if (aoeEffect != null)
        {
            AoeDamage aoe = aoeEffect.GetComponent<AoeDamage>();
            if (aoe != null)
            {
                aoe.Initialize(attack.damage, attack.attackRange, attack.cooldown);
            }
            else
            {
                Debug.LogError("AOE预制体缺少AoeDamage组件");
            }
        }
        else
        {
            Debug.LogError($"无法生成AOE效果: {attack.prefabPath}");
        }

        Debug.Log($"{data.name} 施放 {attack.name} AOE攻击!");
    }
    #endregion

    // 其他方法与RangedMonster类似...
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
}