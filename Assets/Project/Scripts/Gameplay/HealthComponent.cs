using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Health Component can be attached to any game object or its children object
// but note that one object tree should only contain one health component
public class HealthComponent : MonoBehaviour
{
    // properties
    public float health { get; private set; }
    public float healthPercentage { get; private set; }
    public bool alive { get; private set; }

    public AudioSource m_hurtSound;
    // member variables
    [SerializeField] float m_maxHealth = 100f;

    // Start is called before the first frame update
    void Start()
    {
        alive = true;
        health = m_maxHealth;
        Refresh();
    }

    // if taking damage, delta should be a negative number
    // if healing, delta should be positive
    public void ChangeHealth(float delta)
    {
        if (!alive)
            return;
            
        if (delta < 0 && m_hurtSound != null && !m_hurtSound.isPlaying)
            m_hurtSound.Play();

        health += delta;

        if (health > m_maxHealth)
            health = m_maxHealth;
        else if (health <= 0f)
            Die();

        Refresh();
    }

    public void Die()
    {
        alive = false;
        health = 0f;
        Refresh();
    }

    public void SetHealth(float newHealth)
    {
        m_maxHealth = newHealth;
        health = newHealth;
        CanvasManager.s_instance.AddDisplayText("Cheater gets " + newHealth.ToString() + " health.");
    }

    // refresh health scale
    private void Refresh()
    {
        healthPercentage = health / m_maxHealth;
    }
}
