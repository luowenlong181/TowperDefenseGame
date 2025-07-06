using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [Header("Ʈ��Ч������")]
    [SerializeField] private float floatSpeed = 100f;        // �ϸ��ٶȣ�����/�룩
    [SerializeField] private float floatDuration = 1f;       // �ϸ�����ʱ��
    [SerializeField] private float fadeDuration = 0.5f;      // ��������ʱ��
    [SerializeField] private float scaleUpAmount = 0.5f;     // ��ʼ�Ŵ�Ч��
    [SerializeField] private float scaleDuration = 0.2f;     // ���Ŷ�������ʱ��
    [SerializeField] private float followOffset = 50f;       // ����Ŀ���ƫ����

    private Text textComponent;
    private RectTransform rectTransform;
    private float timer;
    private Vector3 originalScale;
    private Color originalColor;
    private Transform target; // �����Ŀ��
    private Vector3 worldOffset; // �����Ŀ���ƫ��

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

        // ��ʼ״̬
        rectTransform.localScale = Vector3.zero;

        StartCoroutine(TextAnimation());
    }

    private void Update()
    {
        if (target != null)
        {
            // ����λ�ø���Ŀ��
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(target.position + worldOffset);
            rectTransform.position = screenPosition;
        }
    }

    private IEnumerator TextAnimation()
    {
        // ���Ŷ���
        float scaleTimer = 0f;
        while (scaleTimer < scaleDuration)
        {
            scaleTimer += Time.deltaTime;
            float progress = scaleTimer / scaleDuration;
            rectTransform.localScale = originalScale * (1 + scaleUpAmount * progress);
            yield return null;
        }

        // �ϸ�����������Ļ�ռ䣩
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, floatSpeed * floatDuration);

        // ��������
        timer = 0f;
        while (timer < floatDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / floatDuration;

            // �ϸ�Ч��������Ļ�ռ䣩
            rectTransform.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                progress
            );

            // ���Żָ�����
            if (progress < 0.5f)
            {
                float scaleProgress = progress * 2f;
                rectTransform.localScale = Vector3.Lerp(
                    originalScale * (1 + scaleUpAmount),
                    originalScale,
                    scaleProgress
                );
            }

            // ����Ч��
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
            Debug.LogError("DamageText����ʧ�ܣ�δָ������Canvas");
            return null;
        }

        // �����¶���
        GameObject damageTextObj = new GameObject("DamageText", typeof(RectTransform));

        // ���ø�����
        damageTextObj.transform.SetParent(parent, false);
        RectTransform rt = damageTextObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 50);

        // ��ʼλ��
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        rt.position = screenPosition;

        // ���Text���
        Text text = damageTextObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value.ToString();
        text.color = color ?? Color.red;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;

        // ���DamageText���
        DamageText damageText = damageTextObj.AddComponent<DamageText>();

        // ��������ռ�ƫ�ƣ�����Ŀ��λ�ã�
        Vector3 offset = Vector3.zero;
        if (target != null)
        {
            // ȷ��ƫ������Ŀ���Ϸ�
            offset = Vector3.up * 1.5f;
        }

        damageText.Initialize(value, color, target, offset);
        return damageText;
    }
}