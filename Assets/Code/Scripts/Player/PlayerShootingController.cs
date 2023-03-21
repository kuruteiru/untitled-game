using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShootingController : MonoBehaviour
{
    #region Fields

    [SerializeField] private ScreenShake screenShaker;
    [SerializeField] private GameObject gun;

    private Gun gunScript;
    private Animator animator;
    private Transform firePoint;

    private bool isReloading = false;
    private int currentAmmo; 
    private float shootTimer = 0f;
    private float inputValue = 0f;

    #endregion Fields

    #region Rotation

    private Vector2 mouseScreenPosition;
    private Vector3 mouseWorldPosition;
    private Vector3 targetDirection;
    private float rotationAngle;

    #endregion Rotation

    private void Start() => InitializeGun();

    private void Update()
    {
        FollowCursor();
        if (inputValue > 0) Shoot();
    }

    #region Methods

    private void InitializeGun()
    {
        gunScript = gun.GetComponent<Gun>();
        animator = gun.GetComponent<Animator>();
        firePoint = gunScript.firePoint;
        currentAmmo = gunScript.gunData.magazineSize;
    }

    private void FollowCursor()
    {
        mouseScreenPosition = Mouse.current.position.ReadValue(); //Position of cursor on screen
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition); //Converts mouse cursor screen position to world (game) position
        targetDirection = mouseWorldPosition - transform.position; //Direction from current object position to the mouse world position

        rotationAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg; //Angle of rotation required to point object towards the mouse position in degrees

        transform.rotation = Quaternion.Euler(new Vector3(0, 0, rotationAngle)); //Rotates object on z axis towards the cursor

        //Rotates the object by 180 degrees depending on which side the cursor is relative to the object
        if (rotationAngle < -90 || rotationAngle > 90)
            transform.localRotation = Quaternion.Euler(new Vector3(180, 0, -rotationAngle));
    }

    private void Shoot()
    {
        if (isReloading) return;

        if (Time.time < shootTimer) return;
        shootTimer = Time.time + 1 / gunScript.gunData.fireRate;

        if (currentAmmo <= 0)
        {
            SoundManager.instance.PlaySound(gunScript.gunData.emptyMagazineSFX);
            return;
        }

        ShootBullet();
        screenShaker.Shake();
        SoundManager.instance.PlaySound(gunScript.gunData.fireSFX);
        animator.SetTrigger("Shoot");

        currentAmmo--;

        if (gunScript.gunData.gunType == GunType.SemiAutomatic) inputValue = 0;
    }

    private void ShootBullet()
    {
        GameObject bullet = Instantiate(gunScript.gunData.bullet, firePoint.position, firePoint.rotation);
        bullet.GetComponent<Rigidbody2D>().velocity = new Vector2(targetDirection.x, targetDirection.y).normalized * 40f;
    }

    private async void Reload()
    {
        isReloading = true;
        SoundManager.instance.PlaySound(gunScript.gunData.reloadSFX);

        await Task.Delay((int)(gunScript.gunData.reloadSpeed * 1000));

        currentAmmo = gunScript.gunData.magazineSize;
        isReloading = false;
    }

    #endregion Methods

    #region Input

    private void OnFire(InputValue value) => inputValue = value.Get<float>();
    private void OnReload() => Reload();

    #endregion Input
}
