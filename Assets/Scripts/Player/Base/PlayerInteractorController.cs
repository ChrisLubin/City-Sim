using System;
using UnityEngine;

public class PlayerInteractorController : NetworkBehaviorAutoDisable<PlayerInteractorController>
{
    [SerializeField] private Camera _camera;

    public event Action<InteractionType> OnDidInteraction;

    [SerializeField] private float _maxInteractDistance = 2f;

    protected override void OnOwnerNetworkSpawn()
    {
        this.OnDidInteraction += this._OnDidInteraction;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!this.IsOwner) { return; }

        this.OnDidInteraction -= this._OnDidInteraction;
    }

    private void Update()
    {
        if (!this.IsOwner) { return; }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector3 maxInteractDistancePoint = this._camera.transform.position + this._camera.transform.forward * this._maxInteractDistance;

            Debug.DrawLine(this._camera.transform.position, maxInteractDistancePoint, Color.black, 10f);
            bool didFindInteractable = Physics.Linecast(this._camera.transform.position, maxInteractDistancePoint, out RaycastHit hit, LayerMask.GetMask(Constants.LayerNames.Interactable));
            if (!didFindInteractable) { return; }

            bool didFindComponent = hit.collider.TryGetComponent(out IInteractable interactable);
            if (!didFindComponent) { return; }

            interactable.DoInteract(this);
        }
    }

    private void _OnDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this.enabled = false;
                break;
            case InteractionType.ExitVehicle:
                this.enabled = true;
                break;
            default:
                break;
        }
    }

    public void DidInteraction(InteractionType interaction) => this.OnDidInteraction?.Invoke(interaction);
}

public enum InteractionType
{
    EnterVehicle,
    ExitVehicle,
}
