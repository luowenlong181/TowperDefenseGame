using UnityEngine;
using Spine.Unity;

public class MeleeMonster : MonsterBase
{
    private SkeletonAnimation skeletonAnimation;
    private Spine.AnimationState spineAnimationState;
    private Spine.Skeleton skeleton;

    // Spine�������� - ʹ�ó�����������л�ȡ
    private const string DEFAULT_IDLE_ANIMATION = "std";
    private const string DEFAULT_WALK_ANIMATION = "walk";
    private const string DEFAULT_ATTACK_ANIMATION = "atk";
    private const string DEFAULT_DEATH_ANIMATION = "death";

    // ����״̬
    private bool isAttacking = false;
    private int currentAttackIndex = -1;

    public override void Initialize(MonsterData monsterData)
    {
        base.Initialize(monsterData);

        // ȷ���������
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null)
        {
            Debug.LogError($"��ս���� {data.name} ȱ��SkeletonAnimation���");
            return;
        }

        spineAnimationState = skeletonAnimation.AnimationState;
        skeleton = skeletonAnimation.Skeleton;

        // ���ó�ʼ����
        SetIdleAnimation();
    }

    // �޸Ĺ�������������attackIndex����
    public override void Attack(IDamageable target, int attackIndex)
    {
        if (isDead || target == null || target.IsDead) return;

        // ��¼��ǰ��������
        currentAttackIndex = attackIndex;

        // ����Ŀ��
        Vector3 direction = ((MonoBehaviour)target).transform.position - transform.position;
        UpdateFacingDirection(direction.x);

        // ���Ź�������
        PlayAttackAnimation(attackIndex);
        OnAttackEvent();
        // ���ù�����ȴʱ��
        if (attackIndex >= 0 && attackIndex < attackCooldowns.Length)
        {
            attackCooldowns[attackIndex] = data.attacks[attackIndex].cooldown;
        }
    }

    // �����¼���������Spine�¼����ã� - ��������˺�
    public void OnAttackEvent()
    {
        if (isDead || currentAttackIndex == -1) return;

        // ȷ������������Ч
        if (currentAttackIndex >= 0 && currentAttackIndex < data.attacks.Count)
        {
            // ��ȡ��ǰ��������
            AttackAction attack = data.attacks[currentAttackIndex];

            // ��Ⲣ�˺���Χ�ڵ�Ŀ��
            ApplyMeleeDamage(attack.attackRange, attack.damage);
        }
    }

    // Ӧ�ý�ս�˺�
    private void ApplyMeleeDamage(float range, int damage)
    {
        // ��ⷶΧ�ڵ�Ŀ��
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

    // �޸�Ϊ֧�ֶ��ֹ�������
    protected override void PlayAttackAnimation(int attackIndex)
    {
        if (spineAnimationState == null) return;

        // ��ȡ�����������ƣ�֧�ֶ��ֹ���������
        string attackAnim = GetAttackAnimationName(attackIndex);

        // ������־ - ȷ������������ȷ
        Debug.Log($"���Ź�������: {attackAnim} (����: {attackIndex})");

        // ʹ��Track 1���Ź�������
        var attackTrack = spineAnimationState.SetAnimation(1, attackAnim, false);
        attackTrack.MixDuration = 0.1f;

        // ��ǹ���״̬
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

    // ��ȡ�����������ƣ����ݹ���������
    private string GetAttackAnimationName(int attackIndex)
    {
        // Ĭ��ʹ�û�����������
        if (attackIndex < 0 || attackIndex >= data.attacks.Count || data.attacks.Count == 1)
            return DEFAULT_ATTACK_ANIMATION;

        // �������������ָ���������ƣ���ʹ����
        if (!string.IsNullOrEmpty(data.attacks[attackIndex].name))
            return data.attacks[attackIndex].name;

        // ����ʹ��Ĭ�Ϲ�������
        return DEFAULT_ATTACK_ANIMATION;
    }

    private void UpdateFacingDirection(float horizontalDirection)
    {
        if (skeleton == null) return;

        // ���ݷ���ת��ɫ
        if (horizontalDirection < 0)
        {
            skeleton.ScaleX = -1f;
        }
        else if (horizontalDirection > 0)
        {
            skeleton.ScaleX = 1f;
        }
    }

    // �����ƶ�����
    public override void Move(Vector3 targetPosition)
    {
        if (isDead || isAttacking) return;

        // ʹ�ø�ƽ�����ƶ���ʽ
        Vector3 direction = (targetPosition - transform.position).normalized;

        // ��ӱ���ӵ�����߼�
        Vector3 avoidVector = CalculateAvoidanceVector();
        if (avoidVector != Vector3.zero)
        {
            direction += avoidVector * 0.5f; // ���ֱܿ���������
            direction.Normalize();
        }

        // ʹ�ø����ƶ�������ֱ������λ��
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * data.moveSpeed;
        }
        else
        {
            transform.position += direction * data.moveSpeed * Time.deltaTime;
        }

        // ���³���
        UpdateFacingDirection(direction.x);

        // ���¶���״̬
        if (spineAnimationState != null)
        {
            var currentTrack = spineAnimationState.GetCurrent(0);
            string currentAnim = currentTrack?.Animation?.Name ?? "";

            // ���û���ƶ�������ǰ�����ƶ�������������Ϊ�ƶ�����
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

        // ��⸽���Ĺ���
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position,
            data.detectionRange * 0.5f,
            LayerMask.GetMask("Monsters")
        );

        foreach (Collider2D col in nearby)
        {
            // �ų��Լ�
            if (col.gameObject == gameObject) continue;

            // ����ܿ�����
            Vector3 diff = transform.position - col.transform.position;
            float dist = diff.magnitude;

            if (dist < 0.1f) dist = 0.1f; // ��ֹ������

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

    // ��ȡ���ж�������
    private string GetIdleAnimationName()
    {
        return !string.IsNullOrEmpty(data.idleAnimation) ?
            data.idleAnimation : DEFAULT_IDLE_ANIMATION;
    }

    // ��ȡ���߶�������
    private string GetWalkAnimationName()
    {
        return !string.IsNullOrEmpty(data.walkAnimation) ?
            data.walkAnimation : DEFAULT_WALK_ANIMATION;
    }

    // ��ȡ������������
    private string GetDeathAnimationName()
    {
        return !string.IsNullOrEmpty(data.deathAnimation) ?
            data.deathAnimation : DEFAULT_DEATH_ANIMATION;
    }

    // �����ܻ�����
    protected override void PlayHitAnimation()
    {
        //if (spineAnimationState != null && !string.IsNullOrEmpty(data.hitAnimation))
        //{
        //    // ʹ��Track 2�����ܻ�����
        //    var hitTrack = spineAnimationState.SetAnimation(2, data.hitAnimation, false);
        //    hitTrack.MixDuration = 0.1f;

        //    // �ܻ�����������ָ�
        //    hitTrack.Complete += track => {
        //        if (!isDead)
        //        {
        //            spineAnimationState.SetEmptyAnimation(2, 0.1f);
        //        }
        //    };
        //}
    }

    // ������������
    protected override void PlayDeathAnimation()
    {
        if (spineAnimationState != null)
        {
            // ������ж������
            spineAnimationState.ClearTracks();

            // ��ȡ������������
            string deathAnim = GetDeathAnimationName();

            // ����������������ѭ����
            var deathTrack = spineAnimationState.SetAnimation(0, deathAnim, false);

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

    // ��MonsterData����Ӷ��������ֶ�
    /*
    [System.Serializable]
    public class MonsterData
    {
        // ...�����ֶ�...
        
        [Header("��������")]
        public string idleAnimation = "std";
        public string walkAnimation = "walk";
        public string attackAnimation = "atk";
        public string hitAnimation = "hit";
        public string deathAnimation = "death";
        
        // ...�����ֶ�...
    }
    */
}