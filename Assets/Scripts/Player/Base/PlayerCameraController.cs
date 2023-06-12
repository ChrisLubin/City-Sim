using UnityEngine;

public class PlayerCameraController : NetworkBehaviorAutoDisable<PlayerCameraController>
{
    [SerializeField] private Camera _camera;

    private Vector2 _rotate;
    [SerializeField] private float _rotateSpeed = 30f;

    protected override void OnOwnerNetworkSpawn()
    {
        this._camera.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;

        // Ensure starting rotation is correct
        this._rotate.x = transform.localEulerAngles.y < 180 ? transform.localEulerAngles.y : transform.localEulerAngles.y - 360;
        this._rotate.y = 0;
    }

    private void Update()
    {
        this._rotate.x += Input.GetAxis("Mouse X") * this._rotateSpeed * Time.deltaTime;
        this._rotate.y += Input.GetAxis("Mouse Y") * this._rotateSpeed * Time.deltaTime;

        transform.localRotation = Quaternion.Euler(-this._rotate.y, this._rotate.x, 0);
    }
}
