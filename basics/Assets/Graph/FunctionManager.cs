using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionManager : MonoBehaviour
{
    [SerializeField]
    Graph graph;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            graph.SetFunction(FunctionLibrary.FunctionName.Wave);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            graph.SetFunction(FunctionLibrary.FunctionName.MultiWave);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            graph.SetFunction(FunctionLibrary.FunctionName.Ripple);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            graph.SetFunction(FunctionLibrary.FunctionName.SphereCollapsing);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha5))
        {
            graph.SetFunction(FunctionLibrary.FunctionName.SphereBandedRotating);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha6))
        {
            graph.SetFunction(FunctionLibrary.FunctionName.TorusStar);
        }
    }
}
