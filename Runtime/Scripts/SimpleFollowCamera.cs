using System;
using UnityEngine;

public class SimpleFollowCamera : MonoBehaviour
{
    public Transform target; // کاراکتر
    public Vector3 offset = new Vector3(0, 3, -5);
    public float smoothSpeed = 10f;

    private void Start()
    {
        
           // target = GameObject.Find("Cube").transform;
        
        Vector3 desiredPos = target.position + offset; transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        //Vector3 desiredPos = target.position + offset;
        
        transform.LookAt(target);
    }
}