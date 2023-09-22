using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    public enum eBrickState
    {
        IDLE,
        WAIT_HOLD,
        HOLD,
    }
    public eBrickState BrickState;
    [SerializeField] Material transparentMat;
    private Material originalMat;
    [SerializeField] Material[] matLib;
    public BoxCollider brickCol;
    private LODGroup brickMeshes;

    public void Awake()
    {
        brickCol = GetComponent<BoxCollider>();
        brickMeshes = GetComponent<LODGroup>();
    }

    private void Start()
    {
        SetState(eBrickState.IDLE);
        int rd = Random.Range(0, matLib.Length);
        originalMat = matLib[rd];
        SetMaterial(originalMat);
    }

    public void SetState(eBrickState state)
    {
        BrickState = state;
        if (BrickState == eBrickState.IDLE)
        {
            brickCol.enabled = true;
            SetMaterial(originalMat);
        }
        else if (BrickState == eBrickState.HOLD)
        {
            brickCol.enabled = false;
            SetMaterial(transparentMat);
        }
    }


    public void SetMaterial(Material mat)
    {
        var lods = brickMeshes.GetLODs();
        for (var i = 0; i < lods.Length; i++)
        {
            for (var j = 0; j < lods[i].renderers.Length; j++)
            {
                lods[i].renderers[j].material = mat;
            }
        }
    }
}