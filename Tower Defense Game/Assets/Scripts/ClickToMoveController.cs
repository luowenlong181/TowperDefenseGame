using UnityEngine;
using UnityEngine.EventSystems;
using Spine.Unity;

public class ClickToMoveController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("��ɫ����")]
    public SkeletonAnimation spineCharacter;
    public float moveSpeed = 5f;
    public bool useRigidbody = false;
    public Rigidbody2D characterRigidbody;
    public float stoppingDistance = 0.1f; // ֹͣ����

    [Header("��������")]
    public string idleAnimation = "unit_wait";
    public string walkAnimation = "unit_walk";
    [Range(0.1f, 1f)] public float animationBlendTime = 0.2f;

    [Header("����ѡ��")]
    public bool enableControls = true;
    public bool flipDirection = false;

    [Header("�������")]
    public Camera followCamera;

    [Header("���ָʾ��")]
    public GameObject clickIndicatorPrefab; // ���λ��ָʾ��
    public float indicatorDuration = 1f; // ָʾ����ʾʱ��

    private Vector2 targetPosition;
    private bool isMoving = false;
    private float targetScaleX = 1f;
    private GameObject currentIndicator;

    private void Awake()
    {
        // �Զ���ȡ�������
        if (followCamera == null)
            followCamera = Camera.main;
    }

    private void Start()
    {
        // ���ų�ʼ����
        if (spineCharacter != null)
            spineCharacter.AnimationState.SetAnimation(0, idleAnimation, true);

        // �Զ���ȡRigidbody2D���
        if (useRigidbody && characterRigidbody == null)
        {
            characterRigidbody = GetComponent<Rigidbody2D>();
            if (characterRigidbody)
                characterRigidbody.gravityScale = 0;
        }
    }

    private void Update()
    {
        UpdateMovement();
        UpdateCharacterDirection();
        UpdateAnimation();
    }

    private void LateUpdate()
    {
        if (followCamera != null)
        {
            // ֱ�ӹ̶������ɫλ��
            followCamera.transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                followCamera.transform.position.z
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableControls) return;

        // ����UIԪ���ϵĵ��
        if (EventSystem.current.IsPointerOverGameObject(eventData.pointerId))
            return;

        SetTargetPosition(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enableControls) return;

        // ��קʱ��������Ŀ��λ��
        SetTargetPosition(eventData.position);
    }

    private void SetTargetPosition(Vector2 screenPosition)
    {
        // ����Ļ����ת��Ϊ��������
        Vector3 worldPosition = followCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0; // ȷ��z����Ϊ0

        targetPosition = worldPosition;
        isMoving = true;

        // ��ʾ���ָʾ��
        ShowClickIndicator(worldPosition);
    }

    private void ShowClickIndicator(Vector3 position)
    {
        // �Ƴ��ɵ�ָʾ��
        if (currentIndicator != null)
            Destroy(currentIndicator);

        // �����µ�ָʾ��
        if (clickIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(clickIndicatorPrefab, position, Quaternion.identity);
            Destroy(currentIndicator, indicatorDuration);
        }
    }

    private void UpdateMovement()
    {
        if (!isMoving) return;

        // ���㵽Ŀ��ľ���ͷ���
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, targetPosition);

        // ����Ƿ񵽴�Ŀ��
        if (distance <= stoppingDistance)
        {
            isMoving = false;
            if (useRigidbody && characterRigidbody != null)
                characterRigidbody.velocity = Vector2.zero;
            return;
        }

        // �ƶ���ɫ
        if (useRigidbody && characterRigidbody != null)
        {
            characterRigidbody.velocity = direction * moveSpeed;
        }
        else
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void UpdateAnimation()
    {
        if (spineCharacter == null) return;

        var currentTrack = spineCharacter.AnimationState.GetCurrent(0);
        string currentAnimation = currentTrack?.Animation.Name;

        if (isMoving)
        {
            if (currentAnimation != walkAnimation)
            {
                spineCharacter.AnimationState.SetAnimation(0, walkAnimation, true)
                    .MixDuration = animationBlendTime;
            }
        }
        else
        {
            if (currentAnimation != idleAnimation)
            {
                spineCharacter.AnimationState.SetAnimation(0, idleAnimation, true)
                    .MixDuration = animationBlendTime;
            }
        }
    }

    private void UpdateCharacterDirection()
    {
        if (spineCharacter == null || !isMoving) return;

        // ��ȡ�ƶ�����
        Vector2 moveDirection = (targetPosition - (Vector2)transform.position).normalized;

        // ���½�ɫ����
        if (Mathf.Abs(moveDirection.x) > 0.01f)
        {
            if (flipDirection)
            {
                targetScaleX = moveDirection.x < 0 ? -1f : 1f;
            }
            else
            {
                targetScaleX = moveDirection.x < 0 ? 1f : -1f;
            }
        }

        // ƽ����ת��ɫ
        float currentScaleX = spineCharacter.Skeleton.ScaleX;
        if (Mathf.Abs(currentScaleX - targetScaleX) > 0.01f)
        {
            spineCharacter.Skeleton.ScaleX = Mathf.Lerp(
                currentScaleX,
                targetScaleX,
                10f * Time.deltaTime
            );
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void EnableControls(bool enable)
    {
        enableControls = enable;

        if (!enable)
        {
            isMoving = false;

            if (useRigidbody && characterRigidbody != null)
            {
                characterRigidbody.velocity = Vector2.zero;
            }

            if (spineCharacter != null)
                spineCharacter.AnimationState.SetAnimation(0, idleAnimation, true);
        }
    }

    // �����ã��ڳ�������ʾĿ��λ��
    private void OnDrawGizmos()
    {
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }
    }
}