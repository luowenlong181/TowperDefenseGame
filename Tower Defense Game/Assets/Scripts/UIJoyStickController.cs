using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIJoyStickController : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("摇杆组件")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float handleRange = 50f;
    public float deadZone = 0.1f;
    public bool followTouch = true;

    [Header("目标角色控制器")]
    public PlayerCharacterController playerController;

    private Vector2 inputDirection = Vector2.zero;
    private Vector2 joystickOriginalPosition;
    private Canvas parentCanvas;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        joystickOriginalPosition = joystickBackground.anchoredPosition;

        // 确保手柄不阻挡事件
        var handleImage = joystickHandle.GetComponent<Image>();
        if (handleImage != null) handleImage.raycastTarget = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (followTouch)
        {
            Vector2 localPoint;
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    eventData.position,
                    null,
                    out localPoint
                );
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    eventData.position,
                    parentCanvas.worldCamera,
                    out localPoint
                );
            }

            // 限制在屏幕范围内
            Rect canvasRect = (parentCanvas.transform as RectTransform).rect;
            Vector2 canvasSize = parentCanvas.GetComponent<RectTransform>().sizeDelta;

            localPoint.x = Mathf.Clamp(localPoint.x, -canvasSize.x / 2 + 100, canvasSize.x / 2 - 100);
            localPoint.y = Mathf.Clamp(localPoint.y, -canvasSize.y / 2 + 100, canvasSize.y / 2 - 100);

            joystickBackground.anchoredPosition = localPoint;
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
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

        Vector2 handlePosition = Vector2.ClampMagnitude(
            localPointerPosition,
            handleRange
        );

        joystickHandle.anchoredPosition = handlePosition;

        inputDirection = handlePosition / handleRange;
        if (inputDirection.magnitude < deadZone)
            inputDirection = Vector2.zero;

        // 将输入方向传递给角色控制器
        if (playerController != null)
            playerController.SetInputDirection(inputDirection);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        inputDirection = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;

        if (followTouch)
            joystickBackground.anchoredPosition = joystickOriginalPosition;

        // 通知角色控制器输入停止
        if (playerController != null)
            playerController.SetInputDirection(Vector2.zero);
    }
}