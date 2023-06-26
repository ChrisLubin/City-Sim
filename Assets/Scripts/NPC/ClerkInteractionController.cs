using System;
using UnityEngine;

public class ClerkInteractionController : MonoBehaviour, IInteractable
{
    // Update is called once per frame
    void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Store inventory closed");
        }*/
    }

    public void DoInteract(PlayerInteractorController player)
    {
        Debug.Log("Interacting with Clerk");
        player.DidInteraction(InteractionType.PurchaseFromClerk);
    }
}
