using UnityEngine;
using UnityEngine.EventSystems;
using Spine.Unity;

public class ClickToMoveController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("角色设置")]
    public SkeletonAnimation spineCharacter;
    public float moveSpeed = 5f;
    public bool useRigidbody = false;
    public Rigidbody2D characterRigidbody;
    public float stoppingDistance = 0.1f; // 停止距离

    [Header("动画设置")]
    public string idleAnimation = "unit_wait";
    public string walkAnimation = "unit_walk";
    [Range(0.1f, 1f)] public float animationBlendTime = 0.2f;

    [Header("控制选项")]
    public bool enableControls = true;
    public bool flipDirection = false;

    [Header("相机跟随")]
    public Camera followCamera;

    [Header("点击指示器")]
    public GameObject clickIndicatorPrefab; // 点击位置指示器
    public float indicatorDuration = 1f; // 指示器显示时间

    private Vector2 targetPosition;
    private bool isMoving = false;
    private float targetScaleX = 1f;
    private GameObject currentIndicator;

    private void Awake()
    {
        // 自动获取相机引用
        if (followCamera == null)
            followCamera = Camera.main;
    }

    private void Start()
    {
        // 播放初始动画
        if (spineCharacter != null)
            spineCharacter.AnimationState.SetAnimation(0, idleAnimation, true);

        // 自动获取Rigidbody2D组件
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
            // 直接固定跟随角色位置
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

        // 忽略UI元素上的点击
        if (EventSystem.current.IsPointerOverGameObject(eventData.pointerId))
            return;

        SetTargetPosition(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enableControls) return;

        // 拖拽时持续更新目标位置
        SetTargetPosition(eventData.position);
    }

    private void SetTargetPosition(Vector2 screenPosition)
    {
        // 将屏幕坐标转换为世界坐标
        Vector3 worldPosition = followCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0; // 确保z坐标为0

        targetPosition = worldPosition;
        isMoving = true;

        // 显示点击指示器
        ShowClickIndicator(worldPosition);
    }

    private void ShowClickIndicator(Vector3 position)
    {
        // 移除旧的指示器
        if (currentIndicator != null)
            Destroy(currentIndicator);

        // 创建新的指示器
        if (clickIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(clickIndicatorPrefab, position, Quaternion.identity);
            Destroy(currentIndicator, indicatorDuration);
        }
    }

    private void UpdateMovement()
    {
        if (!isMoving) return;

        // 计算到目标的距离和方向
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, targetPosition);

        // 检查是否到达目标
        if (distance <= stoppingDistance)
        {
            isMoving = false;
            if (useRigidbody && characterRigidbody != null)
                characterRigidbody.velocity = Vector2.zero;
            return;
        }

        // 移动角色
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

        // 获取移动方向
        Vector2 moveDirection = (targetPosition - (Vector2)transform.position).normalized;

        // 更新角色朝向
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

        // 平滑旋转角色
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

    // 调试用：在场景中显示目标位置
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