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
        Debug.Log("=== ����ϵͳ��� ===");
        Debug.Log($"����ϵͳ�Ƿ�����: {Physics.autoSimulation}");
        Debug.Log($"�̶�ʱ�䲽��: {Time.fixedDeltaTime}");
        Debug.Log($"��������: {Physics.gravity}");
        Debug.Log($"Ĭ�ϽӴ�ƫ��: {Physics.defaultContactOffset}");
        Debug.Log($"��ײ���ģʽ: {Physics.defaultMaxDepenetrationVelocity}");
        Debug.Log($"�Զ�ͬ���任: {Physics.autoSyncTransforms}");

        // �����ײ����
        Debug.Log("��ײ��������:");
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                Debug.Log($"�㼶 {i} ({layerName}) ��ײ����:");
                for (int j = 0; j < 32; j++)
                {
                    if (Physics.GetIgnoreLayerCollision(i, j))
                    {
                        Debug.Log($"  - ������㼶 {j} ({LayerMask.LayerToName(j)}) ����ײ");
                    }
                }
            }
        }

        Debug.Log("=== ��Ͻ��� ===");
    }

    private void CreateTestObjects()
    {
        // ����������ײ��
        CreateTestCollider("TestSphereCollider", Vector3.forward * 3, new SphereCollider());
        CreateTestCollider("TestBoxCollider", Vector3.right * 3, new BoxCollider());
        CreateTestCollider("TestCapsuleCollider", Vector3.left * 3, new CapsuleCollider());
    }

    private void CreateTestCollider(string name, Vector3 position, Collider colliderType)
    {
        GameObject testObj = new GameObject(name);
        testObj.transform.position = position;
        testObj.transform.SetParent(transform);

        // �����ײ��
        testObj.AddComponent(colliderType.GetType());

        // ��Ӹ���
        Rigidbody rb = testObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        Debug.Log($"�������Զ���: {name} ��λ�� {position}");
    }
}