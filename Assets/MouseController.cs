using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    public float SpeedMove = 1f;
    private void Update()
    {
        Vector3 pos = transform.position;
        if (Input.GetKey(KeyCode.A))
        {
            pos.x -= SpeedMove * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            pos.x += SpeedMove * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.W))
        {
            pos.z += SpeedMove * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            pos.z -= SpeedMove * Time.deltaTime;
        }

        transform.position = pos;
    }
}
