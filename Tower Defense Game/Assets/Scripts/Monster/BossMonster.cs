// BossMonster.cs (����)
using UnityEngine;
using Spine.Unity;

public class BossMonster : MonsterBase
{
    private SkeletonAnimation skeletonAnimation;
    private Spine.AnimationState spineAnimationState;
    private Spine.Skeleton skeleton;

    // Spine��������
    private const string IDLE_ANIMATION = "idle";
    private const string WALK_ANIMATION = "walk";
    private const string HIT_ANIMATION = "hit";
    private const string DEATH_ANIMATION = "death";

    // ����Ч��λ��
    [SerializeField] private Transform[] projectileSpawnPoints;

    public override void Initialize(MonsterData monsterData)
    {
        base.Initialize(monsterData);

        // ��ȡSpine���
        skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (skeletonAnimation != null)
        {
            spineAnimationState = skeletonAnimation.AnimationState;
            skeleton = skeletonAnimation.Skeleton;

            // ���ó�ʼ����
            SetIdleAnimation();
        }
        else
        {
            Debug.LogError($"Boss {data.name} ȱ��SkeletonAnimation���");
        }

        // ȷ�����ӵ����ɵ�
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

        // ���³���
        UpdateFacingDirection(direction.x);

        // ���¶���״̬
        if (spineAnimationState != null)
        {
            // ����Ƿ����ڲ����ƶ�����
            var currentTrack = spineAnimationState.GetCurrent(0);
            if (currentTrack == null || currentTrack.Animation.Name != WALK_ANIMATION)
            {
                SetWalkAnimation();
            }
        }
    }

    // Boss���еĹ����߼�
    public override void Attack(IDamageable target, int attackIndex)
    {
        if (isDead) return;

        // ���ֳ���Ŀ��
        if (target != null)
        {
            Vector3 direction = ((MonoBehaviour)target).transform.position - transform.position;
            UpdateFacingDirection(direction.x);
        }

        // ִ���ض�����
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

        // ���Ź�������
        PlayAttackAnimation(attackIndex);
    }

    protected override void PlayAttackAnimation(int attackIndex)
    {
        if (spineAnimationState != null)
        {
            // ��ȡ������������
            string attackAnim = GetAttackAnimationName(attackIndex);

            // ʹ��Track 1���Ź�������
            var attackTrack = spineAnimationState.SetAnimation(1, attackAnim, false);
            attackTrack.MixDuration = 0.1f;

            // ��������������ָ���������
            attackTrack.Complete += track => {
                if (!isDead)
                {
                    spineAnimationState.SetEmptyAnimation(1, 0.1f);
                }
            };
        }
    }

    // ��ȡ�����������ƣ����ݹ���������
    private string GetAttackAnimationName(int attackIndex)
    {
        if (attackIndex < 0 || attackIndex >= data.attacks.Count)
            return "attack_default";

        AttackAction attack = data.attacks[attackIndex];

        // ���ݹ������ͷ��ز�ͬ�Ķ�������
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

    #region ��������
    private void PerformMeleeAttack(int attackIndex)
    {
        AttackAction attack = data.attacks[attackIndex];

        // �ڹ��������е���DealDamage����
        // �����¼��ᴥ��ʵ���˺�
    }

    private void ShootProjectile(IDamageable target, int attackIndex)
    {
        AttackAction attack = data.attacks[attackIndex];

        if (string.IsNullOrEmpty(attack.prefabPath))
        {
            Debug.LogWarning($"Boss {data.name} δ�����ӵ�Ԥ����·��");
            return;
        }

        if (target == null || !(target is MonoBehaviour))
        {
            Debug.LogWarning("��Ч�Ĺ���Ŀ��");
            return;
        }

        // ���ѡ��һ�����ɵ�
        Transform spawnPoint = projectileSpawnPoints[Random.Range(0, projectileSpawnPoints.Length)];

        // ʹ�ö���ػ�ȡ�ӵ�
        GameObject projectile = ObjectPool.Instance.SpawnFromPool(
            attack.prefabPath,
            spawnPoint.position,
            Quaternion.identity
        );

        if (projectile == null)
        {
            Debug.LogError($"�޷��Ӷ���ػ�ȡ�ӵ�ʵ��: {attack.prefabPath}");
            return;
        }

        // �����ӵ�
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.SetDamage(attack.damage);
            projScript.SetTarget(((MonoBehaviour)target).transform);
        }
        else
        {
            Debug.LogError("�ӵ�Ԥ����ȱ��Projectile���");
        }

        Debug.Log($"{data.name} ���� {attack.name} �ӵ�!");
    }

    // �� BossMonster �����޸� PerformAoeAttack ����
    private void PerformAoeAttack(int attackIndex)
    {
        AttackAction attack = data.attacks[attackIndex];

        // �ڵ�ǰλ�ô���AOEЧ��
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
                Debug.LogError("AOEԤ����ȱ��AoeDamage���");
            }
        }
        else
        {
            Debug.LogError($"�޷�����AOEЧ��: {attack.prefabPath}");
        }

        Debug.Log($"{data.name} ʩ�� {attack.name} AOE����!");
    }
    #endregion

    // ����������RangedMonster����...
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
            // �����ƶ�����ת��ɫ
            if (moveDirection < 0)
            {
                skeleton.ScaleX = -1f; // ������
            }
            else if (moveDirection > 0)
            {
                skeleton.ScaleX = 1f;  // ������
            }
            // ע�⣺���ԭʼ��Դ�ǳ���ģ�������Ҫ��ת��Щֵ
        }
    }
}