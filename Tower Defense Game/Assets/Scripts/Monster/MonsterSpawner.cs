using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class MonsterSpawner : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float globalSpawnInterval = 1f; // 全局生成间隔
    [SerializeField] private bool spawnInOrder = true;       // 是否按顺序生成
    [SerializeField] private bool showDebugInfo = true;      // 显示调试信息
    [SerializeField] private int startLevel = 1;             // 起始关卡

    [Header("UI设置")]
    [SerializeField] private bool showMonsterCounter = true; // 是否显示怪物计数器
    [SerializeField] private GameObject counterUIPrefab;     // 计数器UI预制体
    [SerializeField] private Vector3 counterUIPosition = new Vector3(0, 50, 0); // UI位置

    private int currentLevel;
    private List<MonsterData> monstersToSpawn;
    private int totalMonstersToSpawn;
    private int monstersSpawned;
    private bool isSpawning;
    private GameObject counterUIInstance;
    private TMPro.TextMeshProUGUI counterText;

    // 事件：所有怪物生成完毕
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
            Debug.Log($"关卡 {currentLevel} 怪物生成器初始化");
            Debug.Log($"将生成 {monstersToSpawn.Count} 种怪物，总共 {totalMonstersToSpawn} 只");
        }

        // 初始化UI
        InitializeCounterUI();

        StartSpawning();
    }

    private void InitializeCounterUI()
    {
        if (!showMonsterCounter) return;

        if (counterUIPrefab != null)
        {
            // 创建UI实例
            counterUIInstance = Instantiate(counterUIPrefab);
            counterUIInstance.transform.SetParent(FindObjectOfType<Canvas>().transform, false);
            counterUIInstance.transform.localPosition = counterUIPosition;

            // 获取文本组件
            counterText = counterUIInstance.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (counterText != null)
            {
                counterText.text = $"怪物: 0 / {totalMonstersToSpawn}";
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("未设置计数器UI预制体，无法显示怪物计数器");
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

    // 计算需要生成的总怪物数量
    private int CalculateTotalSpawnCount()
    {
        int total = 0;
        foreach (var monster in monstersToSpawn)
        {
            // 如果没有spawnCount字段，使用默认值1
            total += monster.spawnCount > 0 ? monster.spawnCount : 1;
        }
        return total;
    }

    private IEnumerator SpawnMonsters()
    {
        isSpawning = true;
        monstersSpawned = 0;

        if (showDebugInfo)
            Debug.Log("开始生成怪物...");

        // 按顺序生成每种怪物
        foreach (var monsterData in monstersToSpawn)
        {
            // 如果没有spawnCount字段，使用默认值1
            int spawnCount = monsterData.spawnCount > 0 ? monsterData.spawnCount : 1;

            if (spawnCount <= 0) continue;

            if (showDebugInfo)
                Debug.Log($"生成怪物: {monsterData.name} x{spawnCount}");

            // 生成指定数量的怪物
            for (int i = 0; i < spawnCount; i++)
            {
                if (!isSpawning) yield break;

                // 随机选择生成点
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

                // 创建怪物并设置位置
                GameObject monster = MonsterFactory.CreateMonster(monsterData);
                if (monster != null)
                {
                    monster.transform.position = spawnPoint.position;
                    monstersSpawned++;

                    // 更新UI
                    UpdateMonsterCounter();
                }
                else
                {
                    Debug.LogError($"无法生成怪物: {monsterData.name}");
                }

                // 等待生成间隔（使用怪物自己的生成间隔或全局间隔）
                float interval = monsterData.spawnInterval > 0 ?
                    monsterData.spawnInterval : globalSpawnInterval;

                yield return new WaitForSeconds(interval);
            }

            // 如果不是按顺序生成，则等待全局间隔
            if (!spawnInOrder)
            {
                yield return new WaitForSeconds(globalSpawnInterval);
            }
        }

        isSpawning = false;
        if (showDebugInfo)
            Debug.Log($"关卡 {currentLevel} 所有怪物生成完毕");

        // 触发事件
        OnAllMonstersSpawned?.Invoke();
    }

    private void UpdateMonsterCounter()
    {
        if (!showMonsterCounter || counterText == null) return;

        counterText.text = $"怪物: {monstersSpawned} / {totalMonstersToSpawn}";
    }

    // 调试工具
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