using System.Collections.Generic;
using UnityEngine;
//using static Spine.Pool<T>;
public interface IPoolable
{
    void OnSpawn();   // 从对象池取出时调用
    void OnReturn();  // 返回对象池时调用
}
public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public bool expandable = true;
    }

    public static ObjectPool Instance;
    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolInfo;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolInfo = new Dictionary<string, Pool>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
            poolInfo.Add(pool.tag, pool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        // 尝试从池中获取可用对象
        GameObject objectToSpawn = null;
        Queue<GameObject> poolQueue = poolDictionary[tag];

        if (poolQueue.Count > 0)
        {
            objectToSpawn = poolQueue.Dequeue();
        }
        // 如果池为空且可扩展，创建新对象
        else if (poolInfo[tag].expandable)
        {
            objectToSpawn = Instantiate(poolInfo[tag].prefab);
            objectToSpawn.transform.SetParent(transform);
        }
        // 如果池为空且不可扩展，使用最早的对象
        else if (poolQueue.Count == 0 && !poolInfo[tag].expandable)
        {
            // 这里简单处理，实际项目中可能需要更复杂的策略
            objectToSpawn = poolQueue.Dequeue();
        }

        if (objectToSpawn == null) return null;

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // 调用对象的OnSpawn方法（如果实现了IPoolable接口）
        IPoolable poolable = objectToSpawn.GetComponent<IPoolable>();
        poolable?.OnSpawn();

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return;
        }

        // 调用对象的OnReturn方法（如果实现了IPoolable接口）
        IPoolable poolable = objectToReturn.GetComponent<IPoolable>();
        poolable?.OnReturn();

        objectToReturn.SetActive(false);
        objectToReturn.transform.SetParent(transform);
        poolDictionary[tag].Enqueue(objectToReturn);
    }
}