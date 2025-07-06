// RangedMonster.cs
using UnityEngine;
using Spine.Unity;

public class RangedMonster : MonsterBase
{
    private SkeletonAnimation skeletonAnimation;
    private Spine.AnimationState spineAnimationState;
    private Spine.Skeleton skeleton;

    // Spine��������
    private const string IDLE_ANIMATION = "idle";
    private const string WALK_ANIMATION = "walk";
    private const string ATTACK_ANIMATION = "attack";
    private const string HIT_ANIMATION = "hit";
    private const string DEATH_ANIMATION = "death";

    // ����Ч��λ��
    [SerializeField] private Transform projectileSpawnPoint;

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
            Debug.LogError($"Զ�̹��� {data.name} ȱ��SkeletonAnimation���");
        }

        // ȷ�����ӵ����ɵ�
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

    // �޸Ĺ�������������attackIndex����
    public override void Attack(IDamageable target, int attackIndex)
    {
        if (isDead) return;

        // ���ֳ���Ŀ��
        if (target != null)
        {
            Vector3 direction = ((MonoBehaviour)target).transform.position - transform.position;
            UpdateFacingDirection(direction.x);
        }

        // ִ�й���
        ShootProjectile(target, attackIndex);

        // ���Ź�������
        PlayAttackAnimation(attackIndex);
    }

    public override void TakeDamage(int damage)
    {
        if (isDead) return;

        base.TakeDamage(damage);

        // �����ܻ�����
        PlayHitAnimation();
    }

    protected override void Die()
    {
        base.Die();

        // ������������
        PlayDeathAnimation();
    }

    #region Spine��������

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

    // �޸�Ϊ֧�ֶ��ֹ�������
    protected override void PlayAttackAnimation(int attackIndex)
    {
        if (spineAnimationState != null)
        {
            // ��ȡ�����������ƣ�֧�ֶ��ֹ���������
            string attackAnim = GetAttackAnimationName(attackIndex);

            // ʹ��Track 1���Ź������������ǻ���������
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
        // Ĭ��ʹ�û�����������
        if (attackIndex < 0 || attackIndex >= data.attacks.Count)
            return ATTACK_ANIMATION;

        // �������������ָ���������ƣ���ʹ����
        if (!string.IsNullOrEmpty(data.attacks[attackIndex].name))
            return data.attacks[attackIndex].name;

        // ����ʹ��Ĭ�Ϲ�������
        return ATTACK_ANIMATION;
    }

    private void PlayHitAnimation()
    {
        if (spineAnimationState != null)
        {
            // ʹ��Track 2�����ܻ�����
            var hitTrack = spineAnimationState.SetAnimation(2, HIT_ANIMATION, false);
            hitTrack.MixDuration = 0.1f;

            // �ܻ�����������ָ�
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
            // ������ж������
            spineAnimationState.ClearTracks();

            // ����������������ѭ����
            var deathTrack = spineAnimationState.SetAnimation(0, DEATH_ANIMATION, false);

            // ����������������
            deathTrack.Complete += track => {
                // ȷ�����󱻻���
                ReturnToPool();
            };
        }
        else
        {
            // ���û�ж���ϵͳ��ֱ�ӻ���
            ReturnToPool();
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

    #endregion

    #region Զ�̹���ϵͳ

    // �޸�Ϊ֧�ֶ��ֹ�����ʽ
    private void ShootProjectile(IDamageable target, int attackIndex)
    {
        if (attackIndex < 0 || attackIndex >= data.attacks.Count)
        {
            Debug.LogWarning($"��Ч�Ĺ�������: {attackIndex}");
            return;
        }

        AttackAction attack = data.attacks[attackIndex];

        if (string.IsNullOrEmpty(attack.prefabPath))
        {
            Debug.LogWarning($"Զ�̹��� {data.name} δ�����ӵ�Ԥ����·��");
            return;
        }

        if (target == null || !(target is MonoBehaviour))
        {
            Debug.LogWarning("��Ч�Ĺ���Ŀ��");
            return;
        }

        // ʹ�ö���ػ�ȡ�ӵ�
        GameObject projectile = ObjectPool.Instance.SpawnFromPool(
            attack.prefabPath, // ʹ�ù������õ�Ԥ����·����Ϊ�ر�ǩ
            projectileSpawnPoint.position,
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

        // ��ѡ����ӷ�����Ч
        PlayShootEffect(attackIndex);

        Debug.Log($"{data.name} ���� {attack.name} �ӵ�!");
    }

    private void PlayShootEffect(int attackIndex)
    {
        // ������Ը��ݹ���������Ӳ�ͬ����Ч
        // ���磺GameObject shootEffect = ObjectPool.Instance.SpawnFromPool("MuzzleFlash", projectileSpawnPoint.position, Quaternion.identity);
        // shootEffect.GetComponent<ParticleSystem>().Play();
    }

    #endregion
}