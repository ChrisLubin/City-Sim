using UnityEngine;

public class PlayerInteractorController : CharacterInteractorController
{
    private void Update()
    {
        if (!this.IsOwner) { return; }

        if (Input.GetKeyDown(KeyCode.E))
        {
            this.TryDoInteraction();
        }
    }
}
