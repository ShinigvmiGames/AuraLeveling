using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    public int maxEnergy = 100;
    public int currentEnergy = 100;

    public int premiumEnergyBoughtToday = 0;

    public bool UseEnergy(int amount)
    {
        if (currentEnergy < amount)
        {
            Debug.Log("Nicht genug Energie!");
            return false;
        }

        currentEnergy -= amount;
        return true;
    }

    public int maxRechargesPerDay = 10;
    public int energyPerRecharge = 20;

    /// <summary>
    /// Buy 20 energy for 1 MC. Max 10x pro Tag.
    /// </summary>
    public bool BuyEnergyWithMC(PlayerStats player)
    {
        if (player == null) return false;

        if (premiumEnergyBoughtToday >= maxRechargesPerDay)
        {
            Debug.Log("Tageslimit erreicht! (10/10)");
            return false;
        }

        if (!player.SpendManaCrystals(1))
        {
            Debug.Log("Nicht genug Mana Crystals!");
            return false;
        }

        premiumEnergyBoughtToday++;
        currentEnergy += energyPerRecharge;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy + 200);

        Debug.Log($"Energy +{energyPerRecharge} ({premiumEnergyBoughtToday}/{maxRechargesPerDay})");
        return true;
    }

    public int GetRemainingRecharges()
    {
        return Mathf.Max(0, maxRechargesPerDay - premiumEnergyBoughtToday);
    }
}
