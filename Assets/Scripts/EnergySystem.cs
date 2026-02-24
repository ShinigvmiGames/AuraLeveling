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
            Debug.Log("Not enough energy!");
            return false;
        }

        currentEnergy -= amount;
        return true;
    }

    public int maxRechargesPerDay = 10;
    public int energyPerRecharge = 20;

    /// <summary>
    /// Buy 20 energy for 1 MC. Max 10x per day.
    /// </summary>
    public bool BuyEnergyWithMC(PlayerStats player)
    {
        if (player == null) return false;

        if (premiumEnergyBoughtToday >= maxRechargesPerDay)
        {
            Debug.Log("Daily recharge limit reached! (10/10)");
            return false;
        }

        if (!player.SpendManaCrystals(1))
        {
            Debug.Log("Not enough Mana Crystals!");
            return false;
        }

        premiumEnergyBoughtToday++;
        currentEnergy += energyPerRecharge;
        // Energy can exceed maxEnergy (no hard cap)

        Debug.Log($"Energy +{energyPerRecharge} ({premiumEnergyBoughtToday}/{maxRechargesPerDay})");
        return true;
    }

    public int GetRemainingRecharges()
    {
        return Mathf.Max(0, maxRechargesPerDay - premiumEnergyBoughtToday);
    }

    /// <summary>
    /// Daily Reset: Energy back to maxEnergy (100), recharges reset to 0.
    /// Call on day change (e.g. via DailyResetManager).
    /// </summary>
    public void DailyReset()
    {
        currentEnergy = maxEnergy;
        premiumEnergyBoughtToday = 0;
        Debug.Log("Energy Daily Reset: back to " + maxEnergy);
    }
}
