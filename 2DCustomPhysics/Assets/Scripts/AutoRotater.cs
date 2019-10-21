using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotater : MonoBehaviour
{
    [SerializeField] private float rotationValue = 0;
    [SerializeField] private float rotationSpeed = 0;

    private void FixedUpdate() {
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * rotationSpeed) * rotationValue);
    }
}
