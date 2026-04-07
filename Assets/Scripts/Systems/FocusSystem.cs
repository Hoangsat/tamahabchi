using UnityEngine;

public class FocusSystem
{
    private float timer = 0f;
    private float duration = 0f;
    private bool isRunning = false;

    public bool IsRunning => isRunning;

    public void StartFocus(float duration)
    {
        this.duration = duration;
        timer = 0f;
        isRunning = true;
    }

    public bool Update(float deltaTime)
    {
        if (!isRunning) return false;

        timer += deltaTime;

        if (timer >= duration)
        {
            isRunning = false;
            return true; // завершено
        }

        return false;
    }

    public float GetRemainingTime()
    {
        return duration - timer;
    }
}