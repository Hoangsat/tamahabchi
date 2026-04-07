
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
        coinsText.text = "Coins: " + gameManager.currencyData.coins;
    }

    void UpdatePet()
    {
        hungerText.text = "Hunger: " + gameManager.petData.hunger;
        statusText.text = gameManager.petData.statusText;
    }

    void UpdateInventory()
    {
        foodText.text = "Food: " + gameManager.inventoryData.food;
    }

    void UpdateProgression()
    {
        levelText.text = "Level: " + gameManager.progressionData.level;
        xpText.text = "XP: " + gameManager.progressionData.xp + " / 10";
    }

    void OnDestroy()
    {
        gameManager.OnCoinsChanged -= UpdateCoins;
        gameManager.OnPetChanged -= UpdatePet;
        gameManager.OnInventoryChanged -= UpdateInventory;
        gameManager.OnProgressionChanged -= UpdateProgression;
    }
}