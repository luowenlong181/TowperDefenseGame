// MonsterFactory.cs
using UnityEngine;

public class MonsterFactory : MonoBehaviour
{
    public static GameObject CreateMonster(MonsterData data)
    {
        // 从资源池加载预制体
        GameObject prefab = Resources.Load<GameObject>($"Prefabs/Monsters/{data.monsterPrefab}");
        if (prefab == null)
        {
            Debug.LogError($"怪物预制体未找到: {data.monsterPrefab}");
            return null;
        }

        GameObject monster = Instantiate(prefab);
        monster.name = data.name;

        // 添加对应类型的组件
        MonsterBase monsterBase = data.type switch
        {
            MonsterType.Melee => monster.AddComponent<MeleeMonster>(),
            MonsterType.Ranged => monster.AddComponent<RangedMonster>(),
            MonsterType.Boss => monster.AddComponent<BossMonster>(), // 新增Boss类型
            _ => monster.AddComponent<MeleeMonster>() // 默认使用近战
        };

        monsterBase.Initialize(data);
        return monster;
    }
}