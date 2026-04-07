
using UnityEngine;
using TMPro;

public class HUDUI : MonoBehaviour
{
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;

    private GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        gameManager.OnCoinsChanged += UpdateCoins;
        gameManager.OnPetChanged += UpdatePet;
        gameManager.OnInventoryChanged += UpdateInventory;
        gameManager.OnProgressionChanged += UpdateProgression;

        // сразу обновить UI
        UpdateCoins();
        UpdatePet();
        UpdateInventory();
        UpdateProgression();
    }

    void UpdateCoins()
    {
        if (coinsText != null) coinsText.text = "Coins: " + gameManager.currencyData.coins;
    }

    void UpdatePet()
    {
        if (hungerText != null) hungerText.text = "Hunger: " + gameManager.petData.hunger;
        if (statusText != null) statusText.text = gameManager.petData.statusText;
    }

    void UpdateInventory()
    {
        if (foodText != null) foodText.text = "Food: " + gameManager.inventoryData.food;
    }

    void UpdateProgression()
    {
        if (levelText != null) levelText.text = "Level: " + gameManager.progressionData.level;
        if (xpText != null) xpText.text = "XP: " + gameManager.progressionData.xp + " / 10";
    }

    void OnDestroy()
    {
        gameManager.OnCoinsChanged -= UpdateCoins;
        gameManager.OnPetChanged -= UpdatePet;
        gameManager.OnInventoryChanged -= UpdateInventory;
        gameManager.OnProgressionChanged -= UpdateProgression;
    }
}