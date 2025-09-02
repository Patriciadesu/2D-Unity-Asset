using NaughtyAttributes;
using UnityEngine;
using System.Collections;
[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/ItemDatainventory", order = 1)]
public class ItemData : ScriptableObject
{
    public Sprite itemimage; // Image of the item
    [SerializeReference]
    public ItemType type;
    public IEnumerator Use() => type.OnUse();
}

[System.Serializable]
public class ItemType
{
    public virtual IEnumerator OnUse()
    {
        yield return null;
    }
}
public class Heal : ItemType
{
    public float healAmount;
    public override IEnumerator OnUse()
    {
        base.OnUse();
        Player.Instance.currenthealth += healAmount;
        yield return null;
    }
}
public class Stamina : ItemType
{
    public int addStaminaAmount;
    public override IEnumerator OnUse()
    {
        base.OnUse();
        Player.Instance.currentstamina += addStaminaAmount;
        yield return null;
    }
}
public class DoDamageToPlayer : ItemType
{
    public int damageAmount;
    public override IEnumerator OnUse()
    {
        base.OnUse();
        Player.Instance.TakeDamage(damageAmount);
        yield return null;
    }
}
public class SpeedBoost : ItemType
{
    public int speedAmount;
    public float useTime;

    public override IEnumerator OnUse()
    {
        base.OnUse();
        Player.Instance.additionalSpeed = speedAmount;
        yield return new WaitForSeconds(useTime);
        Player.Instance.additionalSpeed = 0;
    }
}

