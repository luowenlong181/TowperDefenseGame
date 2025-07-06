using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class MonsterSpawner : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float globalSpawnInterval = 1f; // ȫ�����ɼ��
    [SerializeField] private bool spawnInOrder = true;       // �Ƿ�˳������
    [SerializeField] private bool showDebugInfo = true;      // ��ʾ������Ϣ
    [SerializeField] private int startLevel = 1;             // ��ʼ�ؿ�

    [Header("UI����")]
    [SerializeField] private bool showMonsterCounter = true; // �Ƿ���ʾ���������
    [SerializeField] private GameObject counterUIPrefab;     // ������UIԤ����
    [SerializeField] private Vector3 counterUIPosition = new Vector3(0, 50, 0); // UIλ��

    private int currentLevel;
    private List<MonsterData> monstersToSpawn;
    private int totalMonstersToSpawn;
    private int monstersSpawned;
    private bool isSpawning;
    private GameObject counterUIInstance;
    private TMPro.TextMeshProUGUI counterText;

    // �¼������й����������
    public event Action OnAllMonstersSpawned;

    private void Start()
    {
        InitializeSpawner(startLevel);
    }

    public void InitializeSpawner(int level)
    {
        currentLevel = level;
        monstersToSpawn = MonsterDatabase.Instance.GetMonstersForLevel(currentLevel);
        totalMonstersToSpawn = CalculateTotalSpawnCount();

        if (showDebugInfo)
        {
            Debug.Log($"�ؿ� {currentLevel} ������������ʼ��");
            Debug.Log($"������ {monstersToSpawn.Count} �ֹ���ܹ� {totalMonstersToSpawn} ֻ");
        }

        // ��ʼ��UI
        InitializeCounterUI();

        StartSpawning();
    }

    private void InitializeCounterUI()
    {
        if (!showMonsterCounter) return;

        if (counterUIPrefab != null)
        {
            // ����UIʵ��
            counterUIInstance = Instantiate(counterUIPrefab);
            counterUIInstance.transform.SetParent(FindObjectOfType<Canvas>().transform, false);
            counterUIInstance.transform.localPosition = counterUIPosition;

            // ��ȡ�ı����
            counterText = counterUIInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (counterText != null)
            {
                counterText.text = $"����: 0 / {totalMonstersToSpawn}";
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("δ���ü�����UIԤ���壬�޷���ʾ���������");
        }
    }

    public void StartSpawning()
    {
        if (isSpawning) return;

        StartCoroutine(SpawnMonsters());
    }

    public void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    // ������Ҫ���ɵ��ܹ�������
    private int CalculateTotalSpawnCount()
    {
        int total = 0;
        foreach (var monster in monstersToSpawn)
        {
            // ���û��spawnCount�ֶΣ�ʹ��Ĭ��ֵ1
            total += monster.spawnCount > 0 ? monster.spawnCount : 1;
        }
        return total;
    }

    private IEnumerator SpawnMonsters()
    {
        isSpawning = true;
        monstersSpawned = 0;

        if (showDebugInfo)
            Debug.Log("��ʼ���ɹ���...");

        // ��˳������ÿ�ֹ���
        foreach (var monsterData in monstersToSpawn)
        {
            // ���û��spawnCount�ֶΣ�ʹ��Ĭ��ֵ1
            int spawnCount = monsterData.spawnCount > 0 ? monsterData.spawnCount : 1;

            if (spawnCount <= 0) continue;

            if (showDebugInfo)
                Debug.Log($"���ɹ���: {monsterData.name} x{spawnCount}");

            // ����ָ�������Ĺ���
            for (int i = 0; i < spawnCount; i++)
            {
                if (!isSpawning) yield break;

                // ���ѡ�����ɵ�
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                // �������ﲢ����λ��
                GameObject monster = MonsterFactory.CreateMonster(monsterData);
                if (monster != null)
                {
                    monster.transform.position = spawnPoint.position;
                    monstersSpawned++;

                    // ����UI
                    UpdateMonsterCounter();
                }
                else
                {
                    Debug.LogError($"�޷����ɹ���: {monsterData.name}");
                }

                // �ȴ����ɼ����ʹ�ù����Լ������ɼ����ȫ�ּ����
                float interval = monsterData.spawnInterval > 0 ?
                    monsterData.spawnInterval : globalSpawnInterval;

                yield return new WaitForSeconds(interval);
            }

            // ������ǰ�˳�����ɣ���ȴ�ȫ�ּ��
            if (!spawnInOrder)
            {
                yield return new WaitForSeconds(globalSpawnInterval);
            }
        }

        isSpawning = false;
        if (showDebugInfo)
            Debug.Log($"�ؿ� {currentLevel} ���й����������");

        // �����¼�
        OnAllMonstersSpawned?.Invoke();
    }

    private void UpdateMonsterCounter()
    {
        if (!showMonsterCounter || counterText == null) return;

        counterText.text = $"����: {monstersSpawned} / {totalMonstersToSpawn}";
    }

    // ���Թ���
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.green;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.5f);
                Gizmos.DrawLine(point.position, point.position + point.forward * 1f);
            }
        }
    }
}