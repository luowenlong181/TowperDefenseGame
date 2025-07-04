using UnityEngine;
using UnityEngine.EventSystems;
using Spine.Unity;
using UnityEngine.UI;

public class SpineJoystickController : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("摇杆设置")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float handleRange = 50f;
    public float deadZone = 0.1f;
    public bool followTouch = true;

    [Header("角色设置")]
    public SkeletonAnimation spineCharacter;
    public float moveSpeed = 5f;
    public bool useRigidbody = false;
    public Rigidbody2D characterRigidbody;

    [Header("动画设置")]
    public string idleAnimation = "unit_wait";
    public string walkAnimation = "unit_walk";
    [Range(0.1f, 1f)] public float animationBlendTime = 0.2f;

    [Header("控制选项")]
    public bool enableJoystick = true;
    public bool enableKeyboard = true;
    public bool flipDirection = false;

    [Header("相机跟随")]
    public Camera followCamera;

    private Vector2 inputDirection = Vector2.zero;
    private bool isDragging = false;
    private bool isKeyboardControl = false;
    private float targetScaleX = 1f;
    private Vector2 joystickOriginalPosition;
    private Canvas parentCanvas; // 关键修复：缓存父画布

    private void Awake()
    {
        // 关键修复：获取父画布
        parentCanvas = joystickBackground.GetComponentInParent<Canvas>();

        // 自动获取相机引用
        if (followCamera == null)
            followCamera = Camera.main;

        // 确保摇杆背景有碰撞检测
        if (joystickBackground != null && parentCanvas != null)
        {
            // 记录原始位置（使用屏幕空间位置）
            joystickOriginalPosition = joystickBackground.anchoredPosition;

            // 强制设置手柄在背景前显示
            joystickHandle.SetAsLastSibling();

            // 移除可能冲突的按钮组件
            var buttonComponent = joystickBackground.GetComponent<UnityEngine.UI.Button>();
            if (buttonComponent != null) Destroy(buttonComponent);

            // 添加必要的UI组件
            if (!joystickBackground.GetComponent<Image>())
            {
                var image = joystickBackground.gameObject.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0.2f);
            }
            joystickBackground.GetComponent<Image>().raycastTarget = true;

            // 确保手柄有Image组件
            if (!joystickHandle.GetComponent<Image>())
            {
                joystickHandle.gameObject.AddComponent<Image>();
            }
        }
    }

    private void Start()
    {
        // 初始化摇杆位置
        if (joystickBackground && joystickHandle)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
            joystickOriginalPosition = joystickBackground.anchoredPosition;

            // 关键修复：确保手柄不阻挡事件
            var handleImage = joystickHandle.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.raycastTarget = false;
            }

            // 移出手柄上的按钮组件
            var handleButton = joystickHandle.GetComponent<Button>();
            if (handleButton != null) Destroy(handleButton);
        }

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
        HandleKeyboardInput();
        UpdateCharacterDirection();
        UpdateAnimation();
    }

    private void LateUpdate() // 使用LateUpdate处理相机跟随
    {
        if (followCamera != null)
        {
            // 直接固定跟随角色位置（无偏移）
            followCamera.transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                followCamera.transform.position.z
            );
        }
    }

    private void FixedUpdate()
    {
        if (useRigidbody && characterRigidbody != null)
        {
            HandlePhysicsMovement();
        }
        else
        {
            HandleTransformMovement();
        }
    }

    // 关键修复：完全重写点击事件处理
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableJoystick || !joystickBackground || parentCanvas == null) return;

        // 修复坐标转换 - 使用正确的画布和渲染模式
        Vector2 localPoint;
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // 覆盖模式直接使用屏幕坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                null,
                out localPoint
            );
        }
        else
        {
            // 其他模式使用画布相机
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out localPoint
            );
        }

        if (followTouch)
        {
            // 限制摇杆在屏幕范围内
            Rect canvasRect = (parentCanvas.transform as RectTransform).rect;
            Vector2 canvasSize = parentCanvas.GetComponent<RectTransform>().sizeDelta;

            localPoint.x = Mathf.Clamp(localPoint.x, -canvasSize.x / 2 + 100, canvasSize.x / 2 - 100);
            localPoint.y = Mathf.Clamp(localPoint.y, -canvasSize.y / 2 + 100, canvasSize.y / 2 - 100);

            joystickBackground.anchoredPosition = localPoint;
        }

        OnDrag(eventData); // 立即更新手柄位置
    }

    // 关键修复：重写拖拽方法
    public void OnDrag(PointerEventData eventData)
    {
        if (!enableJoystick || !joystickBackground || parentCanvas == null) return;

        // 获取画布坐标
        Vector2 localPointerPosition;
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground,
                eventData.position,
                null,
                out localPointerPosition
            );
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground,
                eventData.position,
                parentCanvas.worldCamera,
                out localPointerPosition
            );
        }

        // 限制手柄移动范围
        Vector2 handlePosition = Vector2.ClampMagnitude(
            localPointerPosition,
            handleRange
        );

        joystickHandle.anchoredPosition = handlePosition;

        // 更新输入方向
        inputDirection = handlePosition / handleRange;
        if (inputDirection.magnitude < deadZone)
            inputDirection = Vector2.zero;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        inputDirection = Vector2.zero;

        if (joystickHandle)
            joystickHandle.anchoredPosition = Vector2.zero;

        if (followTouch && joystickBackground)
            joystickBackground.anchoredPosition = joystickOriginalPosition;
    }

    private void HandleKeyboardInput()
    {
        if (!enableKeyboard) return;

        Vector2 keyboardInput = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyboardInput.y = 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyboardInput.y = -1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyboardInput.x = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyboardInput.x = 1;

        if (keyboardInput != Vector2.zero)
        {
            isKeyboardControl = true;
            inputDirection = keyboardInput.normalized;
        }
        else if (isKeyboardControl)
        {
            isKeyboardControl = false;
            inputDirection = Vector2.zero;
        }
    }

    private void HandleTransformMovement()
    {
        if (inputDirection.magnitude > deadZone)
        {
            Vector3 moveVector = new Vector3(inputDirection.x, inputDirection.y, 0);
            transform.Translate(moveVector * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    private void HandlePhysicsMovement()
    {
        if (inputDirection.magnitude > deadZone)
        {
            characterRigidbody.velocity = inputDirection * moveSpeed;
        }
        else
        {
            characterRigidbody.velocity = Vector2.zero;
        }
    }

    private void UpdateAnimation()
    {
        if (spineCharacter == null) return;

        bool isMoving = inputDirection.magnitude > deadZone;
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
        if (spineCharacter == null) return;

        if (Mathf.Abs(inputDirection.x) > deadZone)
        {
            if (flipDirection)
            {
                targetScaleX = inputDirection.x < 0 ? -1f : 1f;
            }
            else
            {
                targetScaleX = inputDirection.x < 0 ? 1f : -1f;
            }
        }

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
        enableJoystick = enable;
        enableKeyboard = enable;

        if (!enable)
        {
            inputDirection = Vector2.zero;
            if (joystickHandle) joystickHandle.anchoredPosition = Vector2.zero;

            if (useRigidbody && characterRigidbody != null)
            {
                characterRigidbody.velocity = Vector2.zero;
            }

            if (spineCharacter != null)
                spineCharacter.AnimationState.SetAnimation(0, idleAnimation, true);
        }
    }

    public void ResetJoystickPosition()
    {
        if (joystickBackground)
        {
            joystickBackground.anchoredPosition = joystickOriginalPosition;
        }
    }
}