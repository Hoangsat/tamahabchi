using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class GameManager : MonoBehaviour
{
    public int hunger = 50;
    public int coins = 0;

    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI coinsText;
    public Button buyButton;

    void Start()
    {
        UpdateUI();
    }

    public void OnFeedButton()
    {
        hunger += 10;
        if (hunger > 100) hunger = 100;

        coins += 5;

        UpdateUI();
    }

    public void OnBuyButton()
    {
        if (coins >= 10)
        {
            coins -= 10;
            Debug.Log("Item bought");
        }
        else
        {
            Debug.Log("Not enough coins");
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        hungerText.text = "Hunger: " + hunger;
        coinsText.text = "Coins: " + coins;
        buyButton.interactable = coins >= 10;
    }
}
