using UnityEngine;

/// <summary>
/// Centralized zero-allocation PT / EN / ES Localization helper for Baía do Avesso.
/// Synced automatically with GirlPowerOrla3DGameFlow.LanguageIndex (0=PT, 1=EN, 2=ES).
/// </summary>
public static class GirlPowerOrla3DLocalization
{
    public static int Lang { get; set; } // 0 = PT, 1 = EN, 2 = ES

    public static string T(string pt, string en, string es)
    {
        switch (Lang)
        {
            case 1: return en;
            case 2: return es;
            default: return pt;
        }
    }

    public static string CoinsLabel(int amount)
    {
        switch (Lang)
        {
            case 1: return amount + " coins";
            case 2: return amount + " monedas";
            default: return amount + " moedas";
        }
    }
}
