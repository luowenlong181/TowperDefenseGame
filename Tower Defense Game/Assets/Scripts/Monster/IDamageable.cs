public interface IDamageable
{
    /// <summary>
    /// �ܵ��˺�
    /// </summary>
    /// <param name="damage">�˺�ֵ</param>
    void TakeDamage(int damage);

    /// <summary>
    /// ��ǰ�Ƿ�����
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// ��ǰ����ֵ
    /// </summary>
    int CurrentHealth { get; }

    /// <summary>
    /// �������ֵ
    /// </summary>
    int MaxHealth { get; }
}