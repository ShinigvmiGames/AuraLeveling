using UnityEngine;
/// <summary>
/// Simple deterministic combat resolver for Gates.
/// Compares player power vs gate difficulty.
/// </summary>
public static class CombatResolver
{
    public struct CombatResult
    {
        public bool win;
        public float playerPower;
        public float gatePower;
    }

    public static CombatResult Resolve(PlayerStats player, GateData gate, long seed = 0)
    {
        var result = new CombatResult();

        // Player power = Aura
        result.playerPower = player.GetAura();

        // Gate power based on rank (tune these to your balance)
        result.gatePower = GetGatePower(gate.rank);

        // Seeded randomness for deterministic outcome
        Random.State oldState = Random.state;
        if (seed != 0)
            Random.InitState((int)(seed % int.MaxValue));

        // Player gets a +-15% variance roll
        float roll = Random.Range(0.85f, 1.15f);
        result.playerPower *= roll;

        Random.state = oldState;

        result.win = result.playerPower >= result.gatePower;

        return result;
    }

    static float GetGatePower(GateRank rank)
    {
        switch (rank)
        {
            case GateRank.ERank: return 20f;
            case GateRank.DRank: return 60f;
            case GateRank.CRank: return 150f;
            case GateRank.BRank: return 400f;
            case GateRank.ARank: return 1000f;
            case GateRank.SRank: return 2500f;
            default: return 20f;
        }
    }
}
