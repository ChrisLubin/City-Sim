using UnityEngine;

public class PlayerMovementController : NetworkBehaviorAutoDisable<PlayerMovementController>
{
    [SerializeField] private float _walkSpeed = 5f;

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * Time.deltaTime * this._walkSpeed;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            transform.position += -transform.right * Time.deltaTime * this._walkSpeed;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position += -transform.forward * Time.deltaTime * this._walkSpeed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * Time.deltaTime * this._walkSpeed;
        }
    }
}
