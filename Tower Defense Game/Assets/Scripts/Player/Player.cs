using UnityEngine;
using Spine.Unity;
using System.Collections;
using UnityEngine.UI;

public class Player : MonoBehaviour, IDamageable
{
    [Header("��ɫ����")]
    [SerializeField] private int maxHealth = 10000;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float autoAttackDetectionRadius = 5f;

    [Header("2D��������")]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private bool ignoreTriggers = true;

    [Header("��������")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private Transform attackPoint;

    [Header("�Ӿ�����")]
    [SerializeField] private Canvas damageTextCanvas;
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private float damageTextYOffset = 1.5f;
    // ��ӵ���ģʽ
    [Header("��������")]
    [SerializeField] private bool debugMode = true;
    // ��ǰ״̬
    private int currentHealth;
    private float lastAttackTime;
    private bool isDead;
    private bool canAttack = true;
    private IDamageable currentTarget;
    private Transform currentTargetTransform;

    // Spine���
    private Spine.AnimationState spineAnimationState;
    private Spine.Skeleton skeleton;

    // ��������
    private const string IDLE_ANIMATION = "unit_wait";
    private const string WALK_ANIMATION = "unit_walk";
    private const string ATTACK_ANIMATION = "unit_attack";
    private const string HIT_ANIMATION = "unit_damage";
    private const string DEATH_ANIMATION = "unit_dead";

    // �Զ��������
    private Collider2D[] attackTargets = new Collider2D[10];
    private float targetDetectionTimer = 0f;
    private const float TARGET_DETECTION_INTERVAL = 0.2f;

    // ʵ�� IDamageable �ӿ�
    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        // ��ʼ��Spine���
        if (skeletonAnimation != null)
        {
            spineAnimationState = skeletonAnimation.AnimationState;
            skeleton = skeletonAnimation.Skeleton;
        }

        // ȷ���й�����
        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        // ���2D���壨��������ڣ�
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // �Զ����õ��˲㼶���루���δ��Inspector�����ã�
        if (enemyLayerMask.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
            {
                enemyLayerMask = 1 << enemyLayer;
                Debug.Log($"{name}: �Զ����� enemyLayerMask Ϊ Enemy �㼶");
            }
            else
            {
                Debug.LogError($"{name}: δ�ҵ� 'Enemy' �㼶! �봴���ò㼶");
            }
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        canAttack = true;

        // ��ʼ����
        SetIdleAnimation();
    }

    private void Update()
    {
        if (isDead) return;

        // Ŀ�����ʱ��
        targetDetectionTimer += Time.deltaTime;
        if (targetDetectionTimer >= TARGET_DETECTION_INTERVAL)
        {
            targetDetectionTimer = 0f;
            DetectAttackTargets();
        }

        // ����е�ǰĿ�꣬�������߼�
        if (currentTarget != null && canAttack)
        {
            // ���Ŀ���Ƿ���Ч
            if (currentTarget.IsDead || currentTargetTransform == null ||
                !currentTargetTransform.gameObject.activeInHierarchy)
            {
                //if (debugMode) Debug.Log("Ŀ����Ч������Ŀ��");
                currentTarget = null;
                currentTargetTransform = null;
                return;
            }

            // ��ȡĿ��λ��
            Vector2 targetPosition = currentTargetTransform.position;

            // ������
            float distance = Vector2.Distance(transform.position, targetPosition);

            if (distance <= attackRange)
            {
                //if (debugMode) Debug.Log($"Ŀ���ڹ�����Χ�� ({distance:F2}/{attackRange})");

                // �ڹ�����Χ��
                if (Time.time - lastAttackTime >= attackInterval)
                {
                    //if (debugMode) Debug.Log("���㹥�����������𹥻�");
                    Attack(currentTarget);
                    lastAttackTime = Time.time;
                }
                else
                {
                    //if (debugMode) Debug.Log($"������ȴ��: {Time.time - lastAttackTime:F2}/{attackInterval}");
                }
            }
            else
            {
                //if (debugMode) Debug.Log($"Ŀ�곬��������Χ ({distance:F2}/{attackRange})");

                if (distance > autoAttackDetectionRadius * 1.2f)
                {
                    //if (debugMode) Debug.Log("Ŀ�곬����ⷶΧ������Ŀ��");
                    // Ŀ�곬����ⷶΧ������Ŀ��
                    currentTarget = null;
                    currentTargetTransform = null;
                }
            }
        }
        else
        {
            //if (debugMode && currentTarget == null) Debug.Log("û����ЧĿ��");
            // û��Ŀ�꣬���ֿ���״̬
            SetIdleAnimation();
        }
    }
    #region Ŀ����ϵͳ��2D�汾��

    private void DetectAttackTargets()
    {
        // �����ǰĿ����Ч��ֱ�ӷ���
        if (currentTarget != null && !currentTarget.IsDead && currentTargetTransform != null)
        {
            if (debugMode) Debug.Log("������ЧĿ�꣬�������");
            return;
        }

        // ���õ�ǰĿ��
        currentTarget = null;
        currentTargetTransform = null;

        // �����Ӵ�������
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(enemyLayerMask);
        contactFilter.useTriggers = !ignoreTriggers;

        if (debugMode) Debug.Log($"��ʼ������: �뾶={autoAttackDetectionRadius}, �㼶={enemyLayerMask.value}");

        // ��ⷶΧ�ڵ����е��ˣ�2D����
        int numTargets = Physics2D.OverlapCircle(
            transform.position,
            autoAttackDetectionRadius,
            contactFilter,
            attackTargets
        );

        if (debugMode) Debug.Log($"��⵽ {numTargets} ����ײ��");

        if (numTargets == 0)
        {
            if (debugMode) Debug.Log("δ��⵽����");
            return;
        }

        // Ѱ������ĵ���
        float closestDistance = float.MaxValue;
        IDamageable closestTarget = null;
        Transform closestTransform = null;
        bool foundValidTarget = false;

        for (int i = 0; i < numTargets; i++)
        {
            Collider2D col = attackTargets[i];
            if (col == null) continue;

            if (debugMode) Debug.Log($"��⵽��ײ��: {col.name}");

            // �����ײ���Ƿ���IDamageable���
            IDamageable target = col.GetComponent<IDamageable>();
            if (target == null)
            {
                // �����ڸ������в���
                target = col.GetComponentInParent<IDamageable>();
                if (debugMode) Debug.Log($"�� {col.name} ��δ�ҵ� IDamageable�����Ը�����: {target != null}");
            }

            // ���Ŀ���Ƿ���Ч
            if (target == null)
            {
                if (debugMode) Debug.Log($"Ŀ�� {col.name} û�� IDamageable ���");
                continue;
            }

            if (target.IsDead)
            {
                if (debugMode) Debug.Log($"Ŀ�� {col.name} ������");
                continue;
            }

            float distance = Vector2.Distance(transform.position, col.transform.position);
            if (debugMode) Debug.Log($"��ЧĿ��: {col.name}, ����: {distance:F2}");

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
                closestTransform = col.transform;
                foundValidTarget = true;
            }
        }

        // ���õ�ǰĿ��
        if (foundValidTarget && closestTarget != null && closestTransform != null)
        {
            if (debugMode) Debug.Log($"����Ŀ��: {closestTransform.name}, ����: {closestDistance:F2}");
            currentTarget = closestTarget;
            currentTargetTransform = closestTransform;
            FaceTarget(currentTarget);
        }
        else
        {
            if (debugMode) Debug.Log("δ�ҵ���ЧĿ��");
        }
    }

    // ����Ŀ�꣨2D��
    private void FaceTarget(IDamageable target)
    {
        if (skeleton == null || currentTargetTransform == null) return;

        Vector2 direction = currentTargetTransform.position - transform.position;

        // �����ƶ�����ת��ɫ
        if (direction.x < 0)
        {
            skeleton.ScaleX = -1f; // ������
            if (debugMode) Debug.Log("������");
        }
        else if (direction.x > 0)
        {
            skeleton.ScaleX = 1f;  // ������
            if (debugMode) Debug.Log("������");
        }
    }
    #endregion
    #region ս��ϵͳ

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // ��ʾ�˺�Ʈ��
        ShowDamageText(damage, Color.red);

        // �����ܻ�����
        PlayHitAnimation();

        // �����ܻ���Ч
        PlayHitEffect();

        // �������
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ��������
    private void Attack(IDamageable target)
    {
        if (isDead || target == null) return;

        // ����Ŀ��
        FaceTarget(target);

        // ���Ź�������
        PlayAttackAnimation();

        // ʵ������˺����ڶ����¼��д�����
        DealDamage();
    }

    // ʵ������˺����ɶ����¼����ã�
    public void DealDamage()
    {
        if (currentTarget != null && !currentTarget.IsDead)
        {
            currentTarget.TakeDamage(attackDamage);
        }
        else
        {
            // Ŀ�������������õ�ǰĿ��
            currentTarget = null;
            currentTargetTransform = null;
        }
    }

    private void Die()
    {
        isDead = true;
        canAttack = false;

        // ������������
        PlayDeathAnimation();

        // ��Ϸ�����߼�
        Debug.Log("�������!");
        // GameManager.Instance.GameOver();
    }

    #endregion

    #region Ŀ����ϵͳ��2D�汾��



    #endregion

    #region �Ӿ�����ϵͳ

    // ��ʾ�˺�Ʈ��
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

    // �����ܻ���Ч
    private void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            hitEffect.transform.position = transform.position + Vector3.up * 0.5f;
            hitEffect.Play();
        }
    }

    #endregion

    #region Spine��������

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
            // ʹ��Track 1���Ź������������ǻ���������
            var attackTrack = spineAnimationState.SetAnimation(1, ATTACK_ANIMATION, false);
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
        }
    }

    #endregion

    #region ���Թ��ߣ�2D��

    [Header("��������")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color detectionColor = new Color(1, 1, 0, 0.5f);
    [SerializeField] private Color attackRangeColor = new Color(1, 0, 0, 0.3f);

    // �ڳ�����ͼ�п��ӻ���ⷶΧ��2D��
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // ���Ƽ�ⷶΧ��2DԲ�Σ�
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, autoAttackDetectionRadius);

        // ���ƹ�����Χ��2DԲ�Σ�
        Gizmos.color = attackRangeColor;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    #endregion
}