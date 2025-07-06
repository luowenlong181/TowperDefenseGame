using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [Header("飘字效果设置")]
    [SerializeField] private float floatSpeed = 100f;        // 上浮速度（像素/秒）
    [SerializeField] private float floatDuration = 1f;       // 上浮持续时间
    [SerializeField] private float fadeDuration = 0.5f;      // 淡出持续时间
    [SerializeField] private float scaleUpAmount = 0.5f;     // 初始放大效果
    [SerializeField] private float scaleDuration = 0.2f;     // 缩放动画持续时间
    [SerializeField] private float followOffset = 50f;       // 跟随目标的偏移量

    private Text textComponent;
    private RectTransform rectTransform;
    private float timer;
    private Vector3 originalScale;
    private Color originalColor;
    private Transform target; // 跟随的目标
    private Vector3 worldOffset; // 相对于目标的偏移

    private void Awake()
    {
        textComponent = GetComponent<Text>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        if (textComponent != null)
        {
            originalColor = textComponent.color;
        }
    }

    public void Initialize(int value, Color? color = null, Transform target = null, Vector3 worldOffset = default)
    {
        if (textComponent == null) return;

        this.target = target;
        this.worldOffset = worldOffset;

        textComponent.text = value.ToString();
        textComponent.color = color ?? originalColor;
        textComponent.canvasRenderer.SetAlpha(1f);

        // 初始状态
        rectTransform.localScale = Vector3.zero;

        StartCoroutine(TextAnimation());
    }

    private void Update()
    {
        if (target != null)
        {
            // 更新位置跟随目标
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(target.position + worldOffset);
            rectTransform.position = screenPosition;
        }
    }

    private IEnumerator TextAnimation()
    {
        // 缩放动画
        float scaleTimer = 0f;
        while (scaleTimer < scaleDuration)
        {
            scaleTimer += Time.deltaTime;
            float progress = scaleTimer / scaleDuration;
            rectTransform.localScale = originalScale * (1 + scaleUpAmount * progress);
            yield return null;
        }

        // 上浮动画（在屏幕空间）
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, floatSpeed * floatDuration);

        // 淡出动画
        timer = 0f;
        while (timer < floatDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / floatDuration;

            // 上浮效果（在屏幕空间）
            rectTransform.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                progress
            );

            // 缩放恢复正常
            if (progress < 0.5f)
            {
                float scaleProgress = progress * 2f;
                rectTransform.localScale = Vector3.Lerp(
                    originalScale * (1 + scaleUpAmount),
                    originalScale,
                    scaleProgress
                );
            }

            // 淡出效果
            if (progress > 1 - (fadeDuration / floatDuration))
            {
                float fadeProgress = (progress - (1 - fadeDuration / floatDuration)) /
                                   (fadeDuration / floatDuration);
                textComponent.canvasRenderer.SetAlpha(1 - fadeProgress);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    public static DamageText Create(Transform parent, Vector3 worldPosition, int value,
                                  Color? color = null, Transform target = null)
    {
        if (parent == null)
        {
            Debug.LogError("DamageText创建失败：未指定父级Canvas");
            return null;
        }

        // 创建新对象
        GameObject damageTextObj = new GameObject("DamageText", typeof(RectTransform));

        // 设置父对象
        damageTextObj.transform.SetParent(parent, false);
        RectTransform rt = damageTextObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 50);

        // 初始位置
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        rt.position = screenPosition;

        // 添加Text组件
        Text text = damageTextObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value.ToString();
        text.color = color ?? Color.red;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;

        // 添加DamageText组件
        DamageText damageText = damageTextObj.AddComponent<DamageText>();

        // 计算世界空间偏移（基于目标位置）
        Vector3 offset = Vector3.zero;
        if (target != null)
        {
            // 确保偏移量在目标上方
            offset = Vector3.up * 1.5f;
        }

        damageText.Initialize(value, color, target, offset);
        return damageText;
    }
}