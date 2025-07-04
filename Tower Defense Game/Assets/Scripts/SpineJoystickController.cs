using UnityEngine;
using UnityEngine.EventSystems;
using Spine.Unity;
using UnityEngine.UI;

public class SpineJoystickController : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("ҡ������")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float handleRange = 50f;
    public float deadZone = 0.1f;
    public bool followTouch = true;

    [Header("��ɫ����")]
    public SkeletonAnimation spineCharacter;
    public float moveSpeed = 5f;
    public bool useRigidbody = false;
    public Rigidbody2D characterRigidbody;

    [Header("��������")]
    public string idleAnimation = "unit_wait";
    public string walkAnimation = "unit_walk";
    [Range(0.1f, 1f)] public float animationBlendTime = 0.2f;

    [Header("����ѡ��")]
    public bool enableJoystick = true;
    public bool enableKeyboard = true;
    public bool flipDirection = false;

    [Header("�������")]
    public Camera followCamera;

    private Vector2 inputDirection = Vector2.zero;
    private bool isDragging = false;
    private bool isKeyboardControl = false;
    private float targetScaleX = 1f;
    private Vector2 joystickOriginalPosition;
    private Canvas parentCanvas; // �ؼ��޸������游����

    private void Awake()
    {
        // �ؼ��޸�����ȡ������
        parentCanvas = joystickBackground.GetComponentInParent<Canvas>();

        // �Զ���ȡ�������
        if (followCamera == null)
            followCamera = Camera.main;

        // ȷ��ҡ�˱�������ײ���
        if (joystickBackground != null && parentCanvas != null)
        {
            // ��¼ԭʼλ�ã�ʹ����Ļ�ռ�λ�ã�
            joystickOriginalPosition = joystickBackground.anchoredPosition;

            // ǿ�������ֱ��ڱ���ǰ��ʾ
            joystickHandle.SetAsLastSibling();

            // �Ƴ����ܳ�ͻ�İ�ť���
            var buttonComponent = joystickBackground.GetComponent<UnityEngine.UI.Button>();
            if (buttonComponent != null) Destroy(buttonComponent);

            // ��ӱ�Ҫ��UI���
            if (!joystickBackground.GetComponent<Image>())
            {
                var image = joystickBackground.gameObject.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0.2f);
            }
            joystickBackground.GetComponent<Image>().raycastTarget = true;

            // ȷ���ֱ���Image���
            if (!joystickHandle.GetComponent<Image>())
            {
                joystickHandle.gameObject.AddComponent<Image>();
            }
        }
    }

    private void Start()
    {
        // ��ʼ��ҡ��λ��
        if (joystickBackground && joystickHandle)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
            joystickOriginalPosition = joystickBackground.anchoredPosition;

            // �ؼ��޸���ȷ���ֱ����赲�¼�
            var handleImage = joystickHandle.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.raycastTarget = false;
            }

            // �Ƴ��ֱ��ϵİ�ť���
            var handleButton = joystickHandle.GetComponent<Button>();
            if (handleButton != null) Destroy(handleButton);
        }

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
        HandleKeyboardInput();
        UpdateCharacterDirection();
        UpdateAnimation();
    }

    private void LateUpdate() // ʹ��LateUpdate�����������
    {
        if (followCamera != null)
        {
            // ֱ�ӹ̶������ɫλ�ã���ƫ�ƣ�
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

    // �ؼ��޸�����ȫ��д����¼�����
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableJoystick || !joystickBackground || parentCanvas == null) return;

        // �޸�����ת�� - ʹ����ȷ�Ļ�������Ⱦģʽ
        Vector2 localPoint;
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // ����ģʽֱ��ʹ����Ļ����
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                null,
                out localPoint
            );
        }
        else
        {
            // ����ģʽʹ�û������
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out localPoint
            );
        }

        if (followTouch)
        {
            // ����ҡ������Ļ��Χ��
            Rect canvasRect = (parentCanvas.transform as RectTransform).rect;
            Vector2 canvasSize = parentCanvas.GetComponent<RectTransform>().sizeDelta;

            localPoint.x = Mathf.Clamp(localPoint.x, -canvasSize.x / 2 + 100, canvasSize.x / 2 - 100);
            localPoint.y = Mathf.Clamp(localPoint.y, -canvasSize.y / 2 + 100, canvasSize.y / 2 - 100);

            joystickBackground.anchoredPosition = localPoint;
        }

        OnDrag(eventData); // ���������ֱ�λ��
    }

    // �ؼ��޸�����д��ק����
    public void OnDrag(PointerEventData eventData)
    {
        if (!enableJoystick || !joystickBackground || parentCanvas == null) return;

        // ��ȡ��������
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

        // �����ֱ��ƶ���Χ
        Vector2 handlePosition = Vector2.ClampMagnitude(
            localPointerPosition,
            handleRange
        );

        joystickHandle.anchoredPosition = handlePosition;

        // �������뷽��
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