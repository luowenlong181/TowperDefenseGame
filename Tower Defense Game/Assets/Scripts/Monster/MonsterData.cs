// MonsterData.cs
using UnityEngine;
using System.Collections.Generic;

public enum MonsterType
{
    Melee,      // 近战怪物
    Ranged,     // 远程怪物
    Boss,       // BOSS
    Elite,      // 精英怪
    Summoner,   // 召唤师
    Flying      // 飞行怪物
}

[System.Serializable]
public class AttackAction
{
    public string name = "atk";          // 攻击名称
    public int damage = 10;                // 该攻击的伤害值
    public float attackRange = 2f;         // 该攻击的范围
    public float cooldown = 1f;            // 该攻击的冷却时间
    public string prefabPath = "";         // 攻击特效/子弹预制体路径

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
    [Header("关卡信息")]
    public int levelID;          // 所属关卡ID

    [Header("基础属性")]
    public int id;               // 怪物唯一ID
    public string name = "";     // 怪物名称
    public MonsterType type;     // 怪物类型

    [Header("战斗属性")]
    public int health = 100;         // 生命值
    public float moveSpeed = 3f;     // 移动速度
    public float detectionRange = 10f; // 索敌范围

    [Header("攻击配置")]
    public List<AttackAction> attacks = new List<AttackAction>(); // 多种攻击方式

    [Header("资源路径")]
    public string monsterPrefab = ""; // 怪物预制体路径

    [Header("生成设置")]
    public int spawnCount = 1;             // 生成总次数
    public float spawnInterval = 1f;        // 生成间隔
    public float monsterEggProbability = 0f; // 生成怪物蛋概率

    [Header("掉落系统")]
    public int expReward = 10;              // 击杀经验奖励
    public List<string> dropItems = new List<string>(); // 掉落物品列表
    public float dropItemProbability = 0.3f; // 掉落物品概率
    [Header("动画名称")]
    public string idleAnimation = "std";
    public string walkAnimation = "walk";
    public string attackAnimation = "atk";
    public string hitAnimation = "jifei";
    public string deathAnimation = "std2";
    // 默认构造函数
    public MonsterData()
    {
        // 添加一个默认攻击
        attacks.Add(new AttackAction());
    }

    // 带参数的构造函数
    public MonsterData(int levelID, int id, string name, MonsterType type)
    {
        this.levelID = levelID;
        this.id = id;
        this.name = name;
        this.type = type;

        // 添加一个默认攻击
        attacks.Add(new AttackAction());
    }

    // 输出调试信息
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