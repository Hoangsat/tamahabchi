using UnityEngine;

public class PetSystem
{
    private PetData petData;

    public PetSystem(PetData petData)
    {
        this.petData = petData;
    }

    public void UpdateHunger(float deltaTime, float hungerDrain)
    {
        if (petData.isDead) return;

        petData.hunger -= hungerDrain * deltaTime;
        petData.hunger = Mathf.Clamp(petData.hunger, 0f, 100f);

        if (petData.hunger <= 0)
        {
            petData.isDead = true;
            petData.statusText = "Dead";
        }
    }

    public void Feed(float amount)
    {
        if (petData.isDead) return;

        petData.hunger += amount;
        petData.hunger = Mathf.Clamp(petData.hunger, 0f, 100f);
    }

    public void UpdateStatus()
    {
        if (petData.isDead) return;

        if (petData.hunger > 70)
            petData.statusText = "Happy";
        else if (petData.hunger > 30)
            petData.statusText = "Focusing";
        else
            petData.statusText = "Hungry";
    }
}