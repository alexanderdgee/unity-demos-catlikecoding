using System;
using UnityEngine;

public class GraphGpu : MonoBehaviour
{
    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time"),
        transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    const int maxResolution = 1000;

    public enum TransitionMode { None, Cycle, Random }

    [SerializeField, Range(10, maxResolution)]
    int resolution = 10;
    [SerializeField]
    FunctionLibrary.FunctionName function;
    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Random;
    [SerializeField, Min(0f)]
    float functionDuration = 1f, transitionDuration = 1f;
    [SerializeField]
    ComputeShader computeShader;
    [SerializeField]
    Material material;
    [SerializeField]
    Mesh mesh;

    float duration;
    ComputeBuffer positionsBuffer;
    FunctionLibrary.FunctionName transitionFunction;
    bool transitioning;

    void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
    }

    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

    private void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }
        UpdateFunctionOnGPU();
    }

    void PickNextFunction()
    {
        if (transitionMode == TransitionMode.None)
        {
            return;
        }
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }

    public void SetFunction(FunctionLibrary.FunctionName name)
    {
        function = name;
        transitioning = false;
        transitionMode = TransitionMode.None;
    }

    public void SetTransitionMode(TransitionMode mode)
    {
        transitionMode = mode;
    }

    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        if (transitioning)
        {
            computeShader.SetFloat(
                transitionProgressId,
                Mathf.SmoothStep(0f, 1f, duration / transitionDuration));
        }
        var kernelIndex = (int)function
            + (int)(transitioning ? transitionFunction : function)
            * FunctionLibrary.FunctionCount;
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);
        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(
            mesh, 0, material, bounds, resolution * resolution);
    }
}
