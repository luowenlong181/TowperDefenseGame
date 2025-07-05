using UnityEngine;
using Spine.Unity;

public class PlayerCharacterController : MonoBehaviour
{
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
    public bool enableKeyboard = true;
    public bool flipDirection = false;

    [Header("�������")]
    public Camera followCamera;

    private Vector2 inputDirection = Vector2.zero;
    private bool isKeyboardControl = false;
    private float targetScaleX = 1f;
    private Vector2 keyboardInputDirection = Vector2.zero;

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

        // ȷ���������
        if (followCamera == null)
            followCamera = Camera.main;
    }

    public void SetInputDirection(Vector2 direction)
    {
        inputDirection = direction;
    }

    private void Update()
    {
        HandleKeyboardInput();
        UpdateCharacterDirection();
        UpdateAnimation();

        // �ϲ�ҡ�˺ͼ�������
        Vector2 finalInput = inputDirection;
        if (finalInput.magnitude == 0 && keyboardInputDirection.magnitude > 0)
        {
            finalInput = keyboardInputDirection;
        }

        // �����ƶ�
        if (finalInput.magnitude > 0.1f)
        {
            Vector3 moveVector = new Vector3(finalInput.x, finalInput.y, 0);
            transform.Translate(moveVector * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    private void LateUpdate()
    {
        if (followCamera != null)
        {
            // ��������ɫ
            Vector3 characterPos = transform.position;
            followCamera.transform.position = new Vector3(
                characterPos.x,
                characterPos.y,
                followCamera.transform.position.z
            );
        }
    }

    private void HandleKeyboardInput()
    {
        if (!enableKeyboard)
        {
            keyboardInputDirection = Vector2.zero;
            return;
        }

        keyboardInputDirection = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyboardInputDirection.y = 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyboardInputDirection.y = -1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyboardInputDirection.x = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyboardInputDirection.x = 1;

        keyboardInputDirection = keyboardInputDirection.normalized;
    }

    private void UpdateAnimation()
    {
        if (spineCharacter == null) return;

        bool isMoving = inputDirection.magnitude > 0.1f || keyboardInputDirection.magnitude > 0.1f;
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

        Vector2 direction = inputDirection;
        if (direction.magnitude == 0) direction = keyboardInputDirection;

        if (Mathf.Abs(direction.x) > 0.1f)
        {
            if (flipDirection)
            {
                targetScaleX = direction.x < 0 ? -1f : 1f;
            }
            else
            {
                targetScaleX = direction.x < 0 ? 1f : -1f;
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
}