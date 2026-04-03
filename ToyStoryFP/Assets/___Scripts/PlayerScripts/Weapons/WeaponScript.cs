using UnityEngine;

public class WeaponScript : MonoBehaviour
{

    [Header("Weapon Setup")]
    public Camera _camera;

    [Header("Weapon Stats")]
    public float fireRate = 10f;
    public int damagePerShot = 20;


    [Header("Weapon Effects")]
    public GameObject muzzleFlashPrefab;
    public AudioClip fireSound;

    private float timeUntilAllowNextShot;

    void Start()
    {
        
    }

    void Update()
    {

        timeUntilAllowNextShot = Mathf.Max(0, timeUntilAllowNextShot - Time.deltaTime);

        if (Input.GetButton("Fire1") && timeUntilAllowNextShot <= 0)
        {
            HitScanShoot();
            timeUntilAllowNextShot = 1 / fireRate;
        }
    }

    void HitScanShoot()
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        RaycastHit hit;

        if (Physics.Raycast(ray.origin, ray.direction, out hit, 100f))
        {
            if (muzzleFlashPrefab != null)
            {
                Instantiate(muzzleFlashPrefab, hit.point, Quaternion.identity);
            }

            if (hit.transform.gameObject.GetComponent<PlayerHealthScript>() != null)
            {
                hit.transform.gameObject.GetComponent<PlayerHealthScript>().TakeDamage(damagePerShot);
            }
        }
    }
}
