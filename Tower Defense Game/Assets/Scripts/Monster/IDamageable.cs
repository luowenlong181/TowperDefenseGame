public interface IDamageable
{
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    void TakeDamage(int damage);

    /// <summary>
    /// 当前是否死亡
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// 当前生命值
    /// </summary>
    int CurrentHealth { get; }

    /// <summary>
    /// 最大生命值
    /// </summary>
    int MaxHealth { get; }
}