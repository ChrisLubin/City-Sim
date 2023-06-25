using System;
using UnityEngine;

public class PlayerInteractorController : NetworkBehaviorAutoDisable<PlayerInteractorController>
{
    [SerializeField] private Camera _camera;
    private Rigidbody _rigidBody;
    private BoxCollider _collider;

    public event Action OnPlayerEnterVehicle;
    public event Action OnPlayerExitVehicle;

    private void Awake()
    {
        this._rigidBody = GetComponent<Rigidbody>();
        this._collider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        if (!this.IsOwner) { return; }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector3 maxInteractDistancePoint = this._camera.transform.position + this._camera.transform.forward * 2f;

            Debug.DrawLine(this._camera.transform.position, maxInteractDistancePoint, Color.black, 10f);
            bool didFindInteractable = Physics.Linecast(this._camera.transform.position, maxInteractDistancePoint, out RaycastHit hit, LayerMask.GetMask(Constants.LayerNames.Interactable));
            if (!didFindInteractable) { return; }

            bool didFindComponent = hit.collider.TryGetComponent(out IInteractable interactable);
            if (!didFindComponent) { return; }

            interactable.DoInteract(this);
        }
    }

    public void PlayerHasEnteredVehicle()
    {
        this._rigidBody.useGravity = false;
        this._collider.enabled = false;
        this.OnPlayerEnterVehicle?.Invoke();
    }
    public void PlayerHasExitedVehicle()
    {
        this._rigidBody.useGravity = true;
        this._collider.enabled = true;
        this.OnPlayerExitVehicle?.Invoke();
    }
}
