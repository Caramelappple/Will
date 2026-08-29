using _Scripts.LDY;
using _Scripts.LSO.Ability;

/// <summary>Marks the owner's will as enhanced.</summary>
public sealed class DLJ_WillEnhancement : LSO_IAbility
{
    public static bool IsActive(LDY_Animal animal)
    {
        if (animal == null || animal.Abilities == null)
            return false;

        foreach (LSO_IAbility ability in animal.Abilities)
        {
            if (ability is DLJ_WillEnhancement)
                return true;
        }

        return false;
    }
}
