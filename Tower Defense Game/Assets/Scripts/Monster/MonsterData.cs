// MonsterData.cs
using UnityEngine;
using System.Collections.Generic;

public enum MonsterType
{
    Melee,      // ��ս����
    Ranged,     // Զ�̹���
    Boss,       // BOSS
    Elite,      // ��Ӣ��
    Summoner,   // �ٻ�ʦ
    Flying      // ���й���
}

[System.Serializable]
public class AttackAction
{
    public string name = "atk";          // ��������
    public int damage = 10;                // �ù������˺�ֵ
    public float attackRange = 2f;         // �ù����ķ�Χ
    public float cooldown = 1f;            // �ù�������ȴʱ��
    public string prefabPath = "";         // ������Ч/�ӵ�Ԥ����·��

    public AttackAction() { }

    public AttackAction(string prefabPath, int damage, float cooldown)
    {
        this.prefabPath = prefabPath;
        this.damage = damage;
        this.cooldown = cooldown;
    }
}

[System.Serializable]
public class MonsterData
{
    [Header("�ؿ���Ϣ")]
    public int levelID;          // �����ؿ�ID

    [Header("��������")]
    public int id;               // ����ΨһID
    public string name = "";     // ��������
    public MonsterType type;     // ��������

    [Header("ս������")]
    public int health = 100;         // ����ֵ
    public float moveSpeed = 3f;     // �ƶ��ٶ�
    public float detectionRange = 10f; // ���з�Χ

    [Header("��������")]
    public List<AttackAction> attacks = new List<AttackAction>(); // ���ֹ�����ʽ

    [Header("��Դ·��")]
    public string monsterPrefab = ""; // ����Ԥ����·��

    [Header("��������")]
    public int spawnCount = 1;             // �����ܴ���
    public float spawnInterval = 1f;        // ���ɼ��
    public float monsterEggProbability = 0f; // ���ɹ��ﵰ����

    [Header("����ϵͳ")]
    public int expReward = 10;              // ��ɱ���齱��
    public List<string> dropItems = new List<string>(); // ������Ʒ�б�
    public float dropItemProbability = 0.3f; // ������Ʒ����
    [Header("��������")]
    public string idleAnimation = "std";
    public string walkAnimation = "walk";
    public string attackAnimation = "atk";
    public string hitAnimation = "jifei";
    public string deathAnimation = "std2";
    // Ĭ�Ϲ��캯��
    public MonsterData()
    {
        // ���һ��Ĭ�Ϲ���
        attacks.Add(new AttackAction());
    }

    // �������Ĺ��캯��
    public MonsterData(int levelID, int id, string name, MonsterType type)
    {
        this.levelID = levelID;
        this.id = id;
        this.name = name;
        this.type = type;

        // ���һ��Ĭ�Ϲ���
        attacks.Add(new AttackAction());
    }

    // ���������Ϣ
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ID:{id} {name} [Lv.{levelID}] Type:{type}");
        sb.AppendLine($"HP:{health} SPD:{moveSpeed} Detection:{detectionRange}");
        sb.AppendLine($"Prefab:{monsterPrefab}");
        sb.AppendLine($"Attacks ({attacks.Count}):");

        foreach (var attack in attacks)
        {
            sb.AppendLine($"- {attack.name}: DMG:{attack.damage} Range:{attack.attackRange} " +
                          $"CD:{attack.cooldown}s Prefab:'{attack.prefabPath}'");
        }

        return sb.ToString();
    }
}