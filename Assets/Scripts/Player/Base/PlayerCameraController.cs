using UnityEngine;

public class PlayerCameraController : NetworkBehaviorAutoDisable<PlayerCameraController>
{
    private PlayerInteractorController _playerInteractorController;

    [SerializeField] private Camera _camera;

    private Vector2 _rotate;
    [SerializeField] private float _rotateSpeed = 30f;

    private bool _shouldRotatePlayer = true;

    private void Awake()
    {
        this._playerInteractorController = GetComponent<PlayerInteractorController>();
        this._playerInteractorController.OnPlayerEnterVehicle += this.OnPlayerEnterVehicle;
        this._playerInteractorController.OnPlayerExitVehicle += this.OnPlayerExitVehicle;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._playerInteractorController.OnPlayerEnterVehicle -= this.OnPlayerEnterVehicle;
        this._playerInteractorController.OnPlayerExitVehicle -= this.OnPlayerExitVehicle;
    }

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
        if (!this.IsOwner) { return; }

        this._rotate.x += Input.GetAxis("Mouse X") * this._rotateSpeed * Time.deltaTime;
        this._rotate.y += Input.GetAxis("Mouse Y") * this._rotateSpeed * Time.deltaTime;

        if (this._shouldRotatePlayer)
        {
            this._camera.gameObject.transform.localRotation = Quaternion.Euler(-this._rotate.y, 0, 0);
            transform.localRotation = Quaternion.Euler(0, this._rotate.x, 0);
        }
        else
        {
            this._camera.gameObject.transform.localRotation = Quaternion.Euler(-this._rotate.y, this._rotate.x, 0);
        }
    }

    private void OnPlayerEnterVehicle() => this._shouldRotatePlayer = false;
    private void OnPlayerExitVehicle()
    {
        this._rotate.x = transform.localEulerAngles.y < 180 ? transform.localEulerAngles.y : transform.localEulerAngles.y - 360;
        this._shouldRotatePlayer = true;
    }
}
