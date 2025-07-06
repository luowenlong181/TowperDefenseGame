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

    // ��ӵ��Կ���
    [Header("��������")]
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
            Debug.LogError($"δ�ҵ�XML�ļ�: Resources/{xmlFolderPath}");
            return;
        }

        Debug.Log($"�ҵ� {xmlFiles.Length} �����������ļ�");

        foreach (TextAsset xmlFile in xmlFiles)
        {
            if (logParsingProcess) Debug.Log($"�����ļ�: {xmlFile.name}");
            ParseWPSXml(xmlFile.text);
        }

        Debug.Log($"�������: {levelMonsters.Count} ���ؿ��Ĺ�������");
        PrintAllMonsters(); // ���������������
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
            Debug.LogError($"XML��������: {ex.Message}");
            return;
        }

        // ���������ռ������
        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        nsManager.AddNamespace("ss", "urn:schemas-microsoft-com:office:spreadsheet");
        nsManager.AddNamespace("o", "urn:schemas-microsoft-com:office:office");
        nsManager.AddNamespace("x", "urn:schemas-microsoft-com:office:excel");
        nsManager.AddNamespace("html", "http://www.w3.org/TR/REC-html40");

        // ��ȡ������
        XmlNode worksheet = xmlDoc.SelectSingleNode("//ss:Worksheet", nsManager);
        if (worksheet == null)
        {
            Debug.LogError("��Ч��XML��ʽ: ȱ��Worksheet�ڵ�");
            return;
        }

        // ��ȡ���
        XmlNode table = worksheet.SelectSingleNode("ss:Table", nsManager);
        if (table == null)
        {
            Debug.LogError("��Ч��XML��ʽ: ȱ��Table�ڵ�");
            return;
        }

        // ��ȡ������
        XmlNodeList rows = table.SelectNodes("ss:Row", nsManager);
        if (rows == null || rows.Count < 2)
        {
            Debug.LogWarning("XML���û��������");
            return;
        }

        // ������ͷ
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
                    Debug.Log($"��ͷ [{cellIndex}]: {headerName}");
            }
            cellIndex++;
        }

        // ����������
        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            XmlNode row = rows[rowIndex];
            MonsterData monsterData = new MonsterData();
            int currentCellIndex = 0;
            int cellsProcessed = 0;

            // �洢�������õ���ʱ����
            List<string> attackPrefabs = new List<string>();
            List<int> attackDamages = new List<int>();
            List<float> attackCooldowns = new List<float>();

            foreach (XmlNode cell in row.SelectNodes("ss:Cell", nsManager))
            {
                // ����Ԫ������
                if (cell.Attributes["ss:Index"] != null)
                {
                    if (int.TryParse(cell.Attributes["ss:Index"].Value, out int explicitIndex))
                    {
                        // ����ȱʧ�ĵ�Ԫ��
                        while (currentCellIndex < explicitIndex - 1)
                        {
                            currentCellIndex++;
                        }
                    }
                }

                // ��ȡ����ֵ - ����յ�Ԫ��
                XmlNode dataNode = cell.SelectSingleNode("ss:Data", nsManager);
                string cellValue = dataNode != null ? dataNode.InnerText.Trim() : string.Empty;

                if (logParsingProcess)
                    Debug.Log($"�� {rowIndex}, �� {currentCellIndex}: {cellValue}");

                // ������������ȡ����
                if (columnHeaders.TryGetValue(currentCellIndex, out string headerName))
                {
                    // ���⴦������ص���
                    if (headerName == "����Ԥ����" || headerName == "AttackPrefab")
                    {
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            // �ָ�������Ԥ����
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
                    else if (headerName == "�˺�" || headerName == "Damage")
                    {
                        // �ָ���ֹ����˺�ֵ
                        string[] damages = cellValue.Split(';');
                        foreach (string dmg in damages)
                        {
                            if (int.TryParse(dmg.Trim(), out int damageValue))
                            {
                                attackDamages.Add(damageValue);
                            }
                        }
                    }
                    else if (headerName == "�������" || headerName == "AttackInterval")
                    {
                        // �ָ���ֹ������
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

            // ������������
            CreateAttackActions(monsterData, attackPrefabs, attackDamages, attackCooldowns);

            // ������֤
            if (validateData && !ValidateMonsterData(monsterData, rowIndex))
            {
                continue; // ������Ч����
            }

            // ��ӵ����ݿ�
            if (!levelMonsters.ContainsKey(monsterData.levelID))
            {
                levelMonsters[monsterData.levelID] = new List<MonsterData>();
            }
            levelMonsters[monsterData.levelID].Add(monsterData);

            if (logParsingProcess)
                Debug.Log($"��ӹ���: {monsterData}");
        }
    }

    private void CreateAttackActions(MonsterData monsterData, List<string> prefabs, List<int> damages, List<float> cooldowns)
    {
        // ȷ��������һ�ֹ�����ʽ
        int attackCount = Mathf.Max(1, prefabs.Count, damages.Count, cooldowns.Count);
        monsterData.attacks.Clear();

        for (int i = 0; i < attackCount; i++)
        {
            var attack = new AttackAction();

            // ����Ԥ����·��
            if (i < prefabs.Count) attack.prefabPath = prefabs[i];

            // �����˺�ֵ
            if (i < damages.Count) attack.damage = damages[i];
            else if (damages.Count > 0) attack.damage = damages[0]; // ʹ�õ�һ���˺�ֵ

            // ������ȴʱ��
            if (i < cooldowns.Count) attack.cooldown = cooldowns[i];
            else if (cooldowns.Count > 0) attack.cooldown = cooldowns[0]; // ʹ�õ�һ����ȴʱ��

            // ���ù�������
            attack.name = attackCount > 1 ? $"Attack {i + 1}" : "Attack";

            monsterData.attacks.Add(attack);
        }
    }

    private bool ValidateMonsterData(MonsterData data, int rowIndex)
    {
        List<string> missingFields = new List<string>();

        if (data.levelID <= 0) missingFields.Add("�ؿ�ID");
        if (data.id <= 0) missingFields.Add("����ID");
        if (string.IsNullOrEmpty(data.monsterPrefab)) missingFields.Add("����Ԥ����");
        if (data.attacks.Count == 0) missingFields.Add("��������");

        if (missingFields.Count > 0)
        {
            Debug.LogWarning($"������Ч�������� (�� {rowIndex}): ȱ�� {string.Join(", ", missingFields)}");
            return false;
        }

        // ��֤��������
        foreach (var attack in data.attacks)
        {
            if (attack.damage <= 0)
            {
                Debug.LogWarning($"���� {data.id} �Ĺ����˺���Ч: {attack.damage}");
            }

            if (attack.cooldown <= 0)
            {
                Debug.LogWarning($"���� {data.id} �Ĺ��������Ч: {attack.cooldown}");
            }
        }

        return true;
    }

    // ʹ�ø���׳������ת������
    private void SetMonsterDataField(ref MonsterData data, string columnName, string value)
    {
        try
        {
            // �����ֵ
            if (string.IsNullOrWhiteSpace(value))
            {
                if (logParsingProcess)
                    Debug.LogWarning($"��ֵ: {columnName}");
                return;
            }

            switch (columnName)
            {
                case "�ؿ�ID":
                case "LevelID":
                    if (int.TryParse(value, out int levelID))
                        data.levelID = levelID;
                    break;

                case "����ID":
                case "MonsterID":
                    if (int.TryParse(value, out int id))
                        data.id = id;
                    break;

                case "����":
                case "Name":
                    data.name = value;
                    break;

                case "��������":
                case "MonsterType":
                    if (System.Enum.TryParse(value, true, out MonsterType type))
                        data.type = type;
                    break;

                case "Ѫ��":
                case "Health":
                    if (int.TryParse(value, out int health))
                        data.health = health;
                    break;

                case "�ƶ��ٶ�":
                case "MoveSpeed":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float moveSpeed))
                        data.moveSpeed = moveSpeed;
                    break;

                case "���з�Χ":
                case "DetectionRange":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float detectionRange))
                        data.detectionRange = detectionRange;
                    break;

                case "����Ԥ����":
                case "MonsterPrefab":
                    data.monsterPrefab = value;
                    break;

                case "���侭��":
                case "ExpReward":
                    if (int.TryParse(value, out int exp))
                        data.expReward = exp;
                    break;

                case "������Ʒ":
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

                case "������Ʒ����":
                case "DropProbability":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float prob))
                        data.dropItemProbability = Mathf.Clamp01(prob);
                    break;
                case "�����ܴ���":
                case "SpawnCount":
                    if (int.TryParse(value, out int spawnCount))
                        data.spawnCount = spawnCount;
                    break;

                case "���ɼ��":
                case "SpawnInterval":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float spawnInterval))
                        data.spawnInterval = spawnInterval;
                    break;
                case "���ɹ��ﵰ����":
                case "MonsterEggProbability":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float eggProb))
                        data.monsterEggProbability = Mathf.Clamp01(eggProb);
                    break;
                // ���Ĭ��ֵ
                default:
                    // ����Ĭ��ֵ
                    if (data.health == 0) data.health = 100;
                    if (data.moveSpeed == 0) data.moveSpeed = 2f;
                    if (data.detectionRange == 0) data.detectionRange = 10f;
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"�����ֶδ��� {columnName} = '{value}': {e.Message}");
        }
    }

    // ����·�������ȡ���й������ݣ����ڶ���س�ʼ����
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
        Debug.LogWarning($"δ�ҵ��ؿ� {levelID} �Ĺ���");
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

    // ���Է�������ӡ���й�������
    public void PrintAllMonsters()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== �������ݿ� =====");

        foreach (var level in levelMonsters.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"�ؿ� {level.Key} �Ĺ���:");
            foreach (var monster in level.Value)
            {
                sb.AppendLine(monster.ToString());
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }
}