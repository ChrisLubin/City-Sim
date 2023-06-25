using System;
using UnityEngine;

public class PlayerInteractorController : NetworkBehaviorAutoDisable<PlayerInteractorController>
{
    [SerializeField] private Camera _camera;

    public event Action<InteractionType> OnDidInteraction;

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

    public void DidInteraction(InteractionType interaction) => this.OnDidInteraction?.Invoke(interaction);
}

public enum InteractionType
{
    EnterVehicle,
    ExitVehicle,
}
