using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    /*
    public Image hpBar;
    public Image delayBar;
    private float maxHealth = 70f;
    private float currentHealth;

    private Coroutine delayCoroutine;

    private void start()
    {
        currentHealth = maxHealth;
        UpdateBars(1f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) TakeDamage(10f);
        if (Input.GetKeyDown(KeyCode.R)) FullHealth();
    }

    private void UpdateBars(float targetFill)
    {
        hpBar.fillAmount = targetFill;
        if(delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
        }
        delayCoroutine = StartCoroutine(DelayBarLerp(targetfill));
    }

    IEnumerator DelayBarLerp(float targetFill)
    {
        yield return delayBar.fillAmount;
        for(float t = 0; t<0.25f;)
    }

    */
}
