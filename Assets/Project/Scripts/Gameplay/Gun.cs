
using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour
{
    public float m_damage = 10f;
    public float m_range = 100f;

    public float m_fireRate = 15f;
    private float m_nextTimeToFire = 0f;

    public int m_maxAmmo = 10;
    private int m_currentAmmo = -1;
    public float m_reloadTime = 1f;
    private bool m_isReloading = false;

    public Camera m_fpsCam;
    public ParticleSystem m_muzzleFlash;
    public ParticleSystem m_impactEffect;
    public Animator m_gunAnimator;
    public ItemComponent m_playerItems;
    public AudioSource m_gunShotSoundEffect;
    public AudioSource m_emptyGunfireSoundEffect;
    public AudioSource m_gunReloadSoundEffect;

    void Start()
    {
        if (m_currentAmmo == -1)
            m_currentAmmo = m_maxAmmo;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_isReloading)
            return;

        if (Input.GetButton("Fire1") && Time.time >= m_nextTimeToFire && m_currentAmmo > 0)
        {
            m_nextTimeToFire = Time.time + 1f / m_fireRate;
            Shoot();
        }
        else if (Input.GetButton("Fire1") && m_currentAmmo <= 0)
        {
            m_emptyGunfireSoundEffect.Play();
        }
        else if (Input.GetKeyDown(KeyCode.R) && m_currentAmmo != m_maxAmmo && m_playerItems.numOfBullet > 0)
        {
            StartCoroutine(Reload());
        }
    }

    public int CurrentAmmoInGun()
    {
        return m_currentAmmo;
    }

    IEnumerator Reload()
    {
        Debug.Log("Reloading...");
        m_isReloading = true;
        m_gunAnimator.SetBool("Reloading", true);
        m_gunReloadSoundEffect.Play();
        
        yield return new WaitForSeconds(m_reloadTime - 0.25f);
        m_gunAnimator.SetBool("Reloading", false);
        
        yield return new WaitForSeconds(0.25f);
        m_currentAmmo += m_playerItems.GetBulletToUse(m_maxAmmo - m_currentAmmo);
        m_isReloading = false;
    }

    void Shoot()
    {
        m_muzzleFlash.Play();
        m_gunShotSoundEffect.Play();
        --m_currentAmmo;

        RaycastHit hit;

        // layer 10 is the player layer
        int layerMask = 1 << 10;
        if (Physics.Raycast(m_fpsCam.transform.position, m_fpsCam.transform.forward, out hit, m_range, ~layerMask))
        {
            Debug.Log("Hit " + hit.transform.name);

            HealthComponent health = hit.transform.GetComponentInParent<HealthComponent>();
            if (health != null && health.alive)
            {
                health.ChangeHealth(-m_damage);
                hit.transform.GetComponentInChildren<Animator>()?.SetTrigger("Hurt");
            }

            m_impactEffect.transform.position = hit.point;
            m_impactEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
            m_impactEffect.Play();
        }
    }
}
