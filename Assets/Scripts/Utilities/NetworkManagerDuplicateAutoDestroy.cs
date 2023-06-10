using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkManagerDuplicateAutoDestroy : MonoBehaviour
{
    private void Awake()
    {
        NetworkManager thisNetworkManager = GetComponent<NetworkManager>();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton != thisNetworkManager)
        {
            UnityTransport thisUnityTransport = GetComponent<UnityTransport>();

            gameObject.SetActive(false);
            thisNetworkManager.enabled = false;
            thisUnityTransport.enabled = false;
            Destroy(thisNetworkManager);
            Destroy(thisUnityTransport);
            Destroy(gameObject);
        }
    }
}
