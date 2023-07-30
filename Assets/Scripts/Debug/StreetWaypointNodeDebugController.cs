using System;
using TMPro;
using UnityEngine;

public class StreetWaypointNodeDebugController : MonoBehaviour
{
    private TextMeshPro _text;

    private void Awake()
    {
        this._text = GetComponentInChildren<TextMeshPro>();

        if (!Debug.isDebugBuild)
        {
            this.enabled = false;
            this._text.gameObject.SetActive(false);
            Destroy(this._text.gameObject);
            Destroy(this);
            return;
        }
    }

    private void Start() => this._text.text = $"({Math.Round(transform.position.x, 2)}, {Math.Round(transform.position.z, 2)})";
}
