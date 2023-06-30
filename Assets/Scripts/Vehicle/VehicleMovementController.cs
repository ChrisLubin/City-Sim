using Unity.Netcode;
using UnityEngine;

public class VehicleMovementController : NetworkBehaviour
{
    private VehicleSeatController _seatController;

    [SerializeField] private float _forwardSpeed = 10f;
    [SerializeField] private float _rotateSpeed = 60f;

    private void Awake()
    {
        this._seatController = GetComponent<VehicleSeatController>();
    }

    private void Update()
    {
        if (!this.IsOwner || !this._seatController.HasPlayerInDriverSeat) { return; }

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * this._forwardSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position += -transform.forward * this._forwardSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(-Vector3.up * this._rotateSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up * this._rotateSpeed * Time.deltaTime);
        }
    }
}
