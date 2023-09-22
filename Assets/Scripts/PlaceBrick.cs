using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceBrick : MonoBehaviour
{
    public enum ePlaceBrickState
    {
        FIND,
        HOLD,
        FIX,
    }
    public ePlaceBrickState placeBrickState;
    [SerializeField] GameObject PrefabBrick;

    private Brick CurrentBrick;
    private bool PositionOk;


    private void SetState(ePlaceBrickState state)
    {
        placeBrickState = state;
    }

    void OnTriggerEnter(Collider other)
    {
        if (placeBrickState == ePlaceBrickState.FIND)
        {
            if (other.CompareTag("Brick"))
            {
                Brick brick = other.GetComponent<Brick>();
                if (brick.BrickState == Brick.eBrickState.IDLE)
                {
                    SetState(ePlaceBrickState.HOLD);
                    SetNextBrick(brick);
                }
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (placeBrickState == ePlaceBrickState.FIX)
        {
            if (other.CompareTag("Brick"))
            {
                SetState(ePlaceBrickState.FIND);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (placeBrickState == ePlaceBrickState.FIND)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                SpawnBrick();
            }
        }
        else if (placeBrickState == ePlaceBrickState.HOLD)
        {
            var position = LegoLogic.SnapToGrid(transform.position);

            //try to find a collision free position
            var placePosition = position;
            PositionOk = false;
            while (!PositionOk)
            {
                var collider = Physics.OverlapBox(placePosition + CurrentBrick.transform.rotation * CurrentBrick.brickCol.center, CurrentBrick.brickCol.size / 2, CurrentBrick.transform.rotation, LegoLogic.LayerMaskLego);
                PositionOk = collider.Length == 0;
                if (PositionOk)
                    break;
                else
                    placePosition.y += LegoLogic.Grid.y;
            }

            if (PositionOk)
                CurrentBrick.transform.position = placePosition;
            else
                CurrentBrick.transform.position = position;

            //Place the brick
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                CurrentBrick.SetState(Brick.eBrickState.IDLE);
                SetState(ePlaceBrickState.FIX);
                CurrentBrick = null;
            }
        }



        //Rotate Brick
        //if (Input.GetKeyDown(KeyCode.E))
        //    CurrentBrick.transform.Rotate(Vector3.up, 90);

        ////Delete Brick
        //if (Input.GetMouseButtonDown(1))
        //{
        //    if (Physics.Raycast(Camera.main.transform.position + Vector3.up * 0.1f * Controller.CameraDistance, Camera.main.transform.forward, out var hitInfo, float.MaxValue, LegoLogic.LayerMaskLego))
        //    {
        //        var brick = hitInfo.collider.GetComponent<Brick>();
        //        if (brick != null)
        //        {
        //            GameObject.DestroyImmediate(brick.gameObject);
        //        }

        //    }
        //}

    }

    public void SetNextBrick(Brick brick)
    {
        CurrentBrick = brick;
        CurrentBrick.SetState(Brick.eBrickState.HOLD);
    }
    public void SpawnBrick()
    {
        float rotationY = Random.Range(0, 2) == 0 ? 0 : 90;
        GameObject brick = Instantiate(PrefabBrick, transform.position, Quaternion.Euler(0, rotationY, 0));
    }
}
