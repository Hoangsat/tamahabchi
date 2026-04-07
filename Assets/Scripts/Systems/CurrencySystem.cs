using UnityEngine;

public class CurrencySystem
{
    private CurrencyData currencyData;

    public CurrencySystem(CurrencyData data)
    {
        currencyData = data;
    }

    public void AddCoins(int amount)
    {
        currencyData.coins += amount;
    }

    public bool SpendCoins(int amount)
    {
        if (currencyData.coins < amount)
            return false;

        currencyData.coins -= amount;
        return true;
    }
}