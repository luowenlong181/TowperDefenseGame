using UnityEngine;

public class PhysicsSystemChecker : MonoBehaviour
{
    private void Start()
    {
        CheckPhysicsSettings();
        CreateTestObjects();
    }

    private void CheckPhysicsSettings()
    {
        Debug.Log("=== 物理系统诊断 ===");
        Debug.Log($"物理系统是否启用: {Physics.autoSimulation}");
        Debug.Log($"固定时间步长: {Time.fixedDeltaTime}");
        Debug.Log($"重力设置: {Physics.gravity}");
        Debug.Log($"默认接触偏移: {Physics.defaultContactOffset}");
        Debug.Log($"碰撞检测模式: {Physics.defaultMaxDepenetrationVelocity}");
        Debug.Log($"自动同步变换: {Physics.autoSyncTransforms}");

        // 检查碰撞矩阵
        Debug.Log("碰撞矩阵设置:");
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                Debug.Log($"层级 {i} ({layerName}) 碰撞设置:");
                for (int j = 0; j < 32; j++)
                {
                    if (Physics.GetIgnoreLayerCollision(i, j))
                    {
                        Debug.Log($"  - 忽略与层级 {j} ({LayerMask.LayerToName(j)}) 的碰撞");
                    }
                }
            }
        }

        Debug.Log("=== 诊断结束 ===");
    }

    private void CreateTestObjects()
    {
        // 创建测试碰撞体
        CreateTestCollider("TestSphereCollider", Vector3.forward * 3, new SphereCollider());
        CreateTestCollider("TestBoxCollider", Vector3.right * 3, new BoxCollider());
        CreateTestCollider("TestCapsuleCollider", Vector3.left * 3, new CapsuleCollider());
    }

    private void CreateTestCollider(string name, Vector3 position, Collider colliderType)
    {
        GameObject testObj = new GameObject(name);
        testObj.transform.position = position;
        testObj.transform.SetParent(transform);

        // 添加碰撞体
        testObj.AddComponent(colliderType.GetType());

        // 添加刚体
        Rigidbody rb = testObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        Debug.Log($"创建测试对象: {name} 在位置 {position}");
    }
}