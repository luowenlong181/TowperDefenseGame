// MonsterDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Globalization;
using System.Text;

public class MonsterDatabase : MonoBehaviour
{
    public static MonsterDatabase Instance { get; private set; }

    private Dictionary<int, List<MonsterData>> levelMonsters =
        new Dictionary<int, List<MonsterData>>();

    [SerializeField] private string xmlFolderPath = "Data/Monsters";

    // 添加调试开关
    [Header("调试设置")]
    [SerializeField] private bool logParsingProcess = false;
    [SerializeField] private bool validateData = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllMonsterData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadAllMonsterData()
    {
        levelMonsters.Clear();

        TextAsset[] xmlFiles = Resources.LoadAll<TextAsset>(xmlFolderPath);

        if (xmlFiles.Length == 0)
        {
            Debug.LogError($"未找到XML文件: Resources/{xmlFolderPath}");
            return;
        }

        Debug.Log($"找到 {xmlFiles.Length} 个怪物数据文件");

        foreach (TextAsset xmlFile in xmlFiles)
        {
            if (logParsingProcess) Debug.Log($"解析文件: {xmlFile.name}");
            ParseWPSXml(xmlFile.text);
        }

        Debug.Log($"加载完成: {levelMonsters.Count} 个关卡的怪物数据");
        PrintAllMonsters(); // 调试输出所有数据
    }

    private void ParseWPSXml(string xmlContent)
    {
        XmlDocument xmlDoc = new XmlDocument();

        try
        {
            xmlDoc.LoadXml(xmlContent);
        }
        catch (XmlException ex)
        {
            Debug.LogError($"XML解析错误: {ex.Message}");
            return;
        }

        // 创建命名空间管理器
        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        nsManager.AddNamespace("ss", "urn:schemas-microsoft-com:office:spreadsheet");
        nsManager.AddNamespace("o", "urn:schemas-microsoft-com:office:office");
        nsManager.AddNamespace("x", "urn:schemas-microsoft-com:office:excel");
        nsManager.AddNamespace("html", "http://www.w3.org/TR/REC-html40");

        // 获取工作表
        XmlNode worksheet = xmlDoc.SelectSingleNode("//ss:Worksheet", nsManager);
        if (worksheet == null)
        {
            Debug.LogError("无效的XML格式: 缺少Worksheet节点");
            return;
        }

        // 获取表格
        XmlNode table = worksheet.SelectSingleNode("ss:Table", nsManager);
        if (table == null)
        {
            Debug.LogError("无效的XML格式: 缺少Table节点");
            return;
        }

        // 获取所有行
        XmlNodeList rows = table.SelectNodes("ss:Row", nsManager);
        if (rows == null || rows.Count < 2)
        {
            Debug.LogWarning("XML表格没有数据行");
            return;
        }

        // 解析表头
        Dictionary<int, string> columnHeaders = new Dictionary<int, string>();
        XmlNode headerRow = rows[0];
        int cellIndex = 0;

        foreach (XmlNode headerCell in headerRow.SelectNodes("ss:Cell", nsManager))
        {
            XmlNode dataNode = headerCell.SelectSingleNode("ss:Data", nsManager);
            if (dataNode != null && !string.IsNullOrWhiteSpace(dataNode.InnerText))
            {
                string headerName = dataNode.InnerText.Trim();
                columnHeaders[cellIndex] = headerName;

                if (logParsingProcess)
                    Debug.Log($"表头 [{cellIndex}]: {headerName}");
            }
            cellIndex++;
        }

        // 处理数据行
        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            XmlNode row = rows[rowIndex];
            MonsterData monsterData = new MonsterData();
            int currentCellIndex = 0;
            int cellsProcessed = 0;

            // 存储攻击配置的临时变量
            List<string> attackPrefabs = new List<string>();
            List<int> attackDamages = new List<int>();
            List<float> attackCooldowns = new List<float>();

            foreach (XmlNode cell in row.SelectNodes("ss:Cell", nsManager))
            {
                // 处理单元格索引
                if (cell.Attributes["ss:Index"] != null)
                {
                    if (int.TryParse(cell.Attributes["ss:Index"].Value, out int explicitIndex))
                    {
                        // 跳过缺失的单元格
                        while (currentCellIndex < explicitIndex - 1)
                        {
                            currentCellIndex++;
                        }
                    }
                }

                // 获取数据值 - 处理空单元格
                XmlNode dataNode = cell.SelectSingleNode("ss:Data", nsManager);
                string cellValue = dataNode != null ? dataNode.InnerText.Trim() : string.Empty;

                if (logParsingProcess)
                    Debug.Log($"行 {rowIndex}, 列 {currentCellIndex}: {cellValue}");

                // 根据列索引获取列名
                if (columnHeaders.TryGetValue(currentCellIndex, out string headerName))
                {
                    // 特殊处理攻击相关的列
                    if (headerName == "攻击预制体" || headerName == "AttackPrefab")
                    {
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            // 分割多个攻击预制体
                            string[] prefabs = cellValue.Split(';');
                            foreach (string prefab in prefabs)
                            {
                                string trimmed = prefab.Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                {
                                    attackPrefabs.Add(trimmed);
                                }
                            }
                        }
                    }
                    else if (headerName == "伤害" || headerName == "Damage")
                    {
                        // 分割多种攻击伤害值
                        string[] damages = cellValue.Split(';');
                        foreach (string dmg in damages)
                        {
                            if (int.TryParse(dmg.Trim(), out int damageValue))
                            {
                                attackDamages.Add(damageValue);
                            }
                        }
                    }
                    else if (headerName == "攻击间隔" || headerName == "AttackInterval")
                    {
                        // 分割多种攻击间隔
                        string[] intervals = cellValue.Split(';');
                        foreach (string interval in intervals)
                        {
                            if (float.TryParse(interval.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float intervalValue))
                            {
                                attackCooldowns.Add(intervalValue);
                            }
                        }
                    }
                    else
                    {
                        SetMonsterDataField(ref monsterData, headerName, cellValue);
                    }

                    cellsProcessed++;
                }

                currentCellIndex++;
            }

            // 创建攻击动作
            CreateAttackActions(monsterData, attackPrefabs, attackDamages, attackCooldowns);

            // 数据验证
            if (validateData && !ValidateMonsterData(monsterData, rowIndex))
            {
                continue; // 跳过无效数据
            }

            // 添加到数据库
            if (!levelMonsters.ContainsKey(monsterData.levelID))
            {
                levelMonsters[monsterData.levelID] = new List<MonsterData>();
            }
            levelMonsters[monsterData.levelID].Add(monsterData);

            if (logParsingProcess)
                Debug.Log($"添加怪物: {monsterData}");
        }
    }

    private void CreateAttackActions(MonsterData monsterData, List<string> prefabs, List<int> damages, List<float> cooldowns)
    {
        // 确保至少有一种攻击方式
        int attackCount = Mathf.Max(1, prefabs.Count, damages.Count, cooldowns.Count);
        monsterData.attacks.Clear();

        for (int i = 0; i < attackCount; i++)
        {
            var attack = new AttackAction();

            // 设置预制体路径
            if (i < prefabs.Count) attack.prefabPath = prefabs[i];

            // 设置伤害值
            if (i < damages.Count) attack.damage = damages[i];
            else if (damages.Count > 0) attack.damage = damages[0]; // 使用第一个伤害值

            // 设置冷却时间
            if (i < cooldowns.Count) attack.cooldown = cooldowns[i];
            else if (cooldowns.Count > 0) attack.cooldown = cooldowns[0]; // 使用第一个冷却时间

            // 设置攻击名称
            attack.name = attackCount > 1 ? $"Attack {i + 1}" : "Attack";

            monsterData.attacks.Add(attack);
        }
    }

    private bool ValidateMonsterData(MonsterData data, int rowIndex)
    {
        List<string> missingFields = new List<string>();

        if (data.levelID <= 0) missingFields.Add("关卡ID");
        if (data.id <= 0) missingFields.Add("怪物ID");
        if (string.IsNullOrEmpty(data.monsterPrefab)) missingFields.Add("敌人预制体");
        if (data.attacks.Count == 0) missingFields.Add("攻击配置");

        if (missingFields.Count > 0)
        {
            Debug.LogWarning($"跳过无效怪物数据 (行 {rowIndex}): 缺少 {string.Join(", ", missingFields)}");
            return false;
        }

        // 验证攻击配置
        foreach (var attack in data.attacks)
        {
            if (attack.damage <= 0)
            {
                Debug.LogWarning($"怪物 {data.id} 的攻击伤害无效: {attack.damage}");
            }

            if (attack.cooldown <= 0)
            {
                Debug.LogWarning($"怪物 {data.id} 的攻击间隔无效: {attack.cooldown}");
            }
        }

        return true;
    }

    // 使用更健壮的类型转换方法
    private void SetMonsterDataField(ref MonsterData data, string columnName, string value)
    {
        try
        {
            // 处理空值
            if (string.IsNullOrWhiteSpace(value))
            {
                if (logParsingProcess)
                    Debug.LogWarning($"空值: {columnName}");
                return;
            }

            switch (columnName)
            {
                case "关卡ID":
                case "LevelID":
                    if (int.TryParse(value, out int levelID))
                        data.levelID = levelID;
                    break;

                case "怪物ID":
                case "MonsterID":
                    if (int.TryParse(value, out int id))
                        data.id = id;
                    break;

                case "名字":
                case "Name":
                    data.name = value;
                    break;

                case "怪物类型":
                case "MonsterType":
                    if (System.Enum.TryParse(value, true, out MonsterType type))
                        data.type = type;
                    break;

                case "血量":
                case "Health":
                    if (int.TryParse(value, out int health))
                        data.health = health;
                    break;

                case "移动速度":
                case "MoveSpeed":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float moveSpeed))
                        data.moveSpeed = moveSpeed;
                    break;

                case "索敌范围":
                case "DetectionRange":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float detectionRange))
                        data.detectionRange = detectionRange;
                    break;

                case "敌人预制体":
                case "MonsterPrefab":
                    data.monsterPrefab = value;
                    break;

                case "掉落经验":
                case "ExpReward":
                    if (int.TryParse(value, out int exp))
                        data.expReward = exp;
                    break;

                case "掉落物品":
                case "DropItems":
                    if (!string.IsNullOrEmpty(value))
                    {
                        string[] items = value.Split(';');
                        foreach (string item in items)
                        {
                            string trimmed = item.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                data.dropItems.Add(trimmed);
                            }
                        }
                    }
                    break;

                case "掉落物品概率":
                case "DropProbability":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float prob))
                        data.dropItemProbability = Mathf.Clamp01(prob);
                    break;
                case "生成总次数":
                case "SpawnCount":
                    if (int.TryParse(value, out int spawnCount))
                        data.spawnCount = spawnCount;
                    break;

                case "生成间隔":
                case "SpawnInterval":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float spawnInterval))
                        data.spawnInterval = spawnInterval;
                    break;
                case "生成怪物蛋概率":
                case "MonsterEggProbability":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float eggProb))
                        data.monsterEggProbability = Mathf.Clamp01(eggProb);
                    break;
                // 添加默认值
                default:
                    // 设置默认值
                    if (data.health == 0) data.health = 100;
                    if (data.moveSpeed == 0) data.moveSpeed = 2f;
                    if (data.detectionRange == 0) data.detectionRange = 10f;
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置字段错误 {columnName} = '{value}': {e.Message}");
        }
    }

    // 添加新方法：获取所有怪物数据（用于对象池初始化）
    public Dictionary<int, List<MonsterData>> GetAllMonsters()
    {
        return levelMonsters;
    }

    public List<MonsterData> GetMonstersForLevel(int levelID)
    {
        if (levelMonsters.TryGetValue(levelID, out List<MonsterData> monsters))
        {
            return monsters;
        }
        Debug.LogWarning($"未找到关卡 {levelID} 的怪物");
        return new List<MonsterData>();
    }

    public MonsterData GetMonsterByID(int id)
    {
        foreach (var level in levelMonsters)
        {
            MonsterData monster = level.Value.FirstOrDefault(m => m.id == id);
            if (monster != null) return monster;
        }
        return null;
    }

    // 调试方法：打印所有怪物数据
    public void PrintAllMonsters()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== 怪物数据库 =====");

        foreach (var level in levelMonsters.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"关卡 {level.Key} 的怪物:");
            foreach (var monster in level.Value)
            {
                sb.AppendLine(monster.ToString());
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }
}