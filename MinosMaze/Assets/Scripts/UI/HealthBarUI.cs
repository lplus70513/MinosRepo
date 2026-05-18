using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Image hpBar;
    public Image delayBar;
    private float maxHealth = 70f;
    private float currentHealth;

    private Coroutine delayCoroutine;

    private void start()
    {
        currentHealth = maxHealth;
        // UpdateBars(1f);
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Q)) TakeDamage(10f);

    }
}
