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

    public void BuyEnergyWithGold()
    {
        if (premiumEnergyBoughtToday >= 10)
        {
            Debug.Log("Tageslimit erreicht!");
            return;
        }

        premiumEnergyBoughtToday++;
        currentEnergy += 20;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy + 200);

        Debug.Log("20 Energie gekauft");
    }
}
