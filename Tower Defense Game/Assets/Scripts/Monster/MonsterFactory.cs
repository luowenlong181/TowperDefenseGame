// MonsterFactory.cs
using UnityEngine;

public class MonsterFactory : MonoBehaviour
{
    public static GameObject CreateMonster(MonsterData data)
    {
        // ����Դ�ؼ���Ԥ����
        GameObject prefab = Resources.Load<GameObject>($"Prefabs/Monsters/{data.monsterPrefab}");
        if (prefab == null)
        {
            Debug.LogError($"����Ԥ����δ�ҵ�: {data.monsterPrefab}");
            return null;
        }

        GameObject monster = Instantiate(prefab);
        monster.name = data.name;

        // ��Ӷ�Ӧ���͵����
        MonsterBase monsterBase = data.type switch
        {
            MonsterType.Melee => monster.AddComponent<MeleeMonster>(),
            MonsterType.Ranged => monster.AddComponent<RangedMonster>(),
            MonsterType.Boss => monster.AddComponent<BossMonster>(), // ����Boss����
            _ => monster.AddComponent<MeleeMonster>() // Ĭ��ʹ�ý�ս
        };

        monsterBase.Initialize(data);
        return monster;
    }
}