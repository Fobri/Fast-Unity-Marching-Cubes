using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDeformer : MonoBehaviour
{
    public WorldGeneration.WorldBase worldSetup;
    public float deformRadius;
    Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if(Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                worldSetup.ModifyTerrainBallShape(hit.point, deformRadius, 1f);
            }
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                worldSetup.ModifyTerrainBallShape(hit.point, deformRadius, -1f);
            }
        }
    }
}
