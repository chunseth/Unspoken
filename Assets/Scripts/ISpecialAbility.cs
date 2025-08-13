using UnityEngine;

public interface ISpecialAbility
{
    string AbilityName { get; }
    Sprite AbilityIcon { get; }
    float Cooldown { get; }
    bool IsOnCooldown { get; }
    void ActivateAbility();
} 