using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    public float floatStrength = 0.01f; 
    public float floatSpeed = 1f; 
    
    private Vector3 startPosition;
    
    void Start()
    {
        startPosition = transform.position; 
    }

    void Update()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatStrength;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}