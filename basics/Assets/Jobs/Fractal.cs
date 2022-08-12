using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using quaternion = Unity.Mathematics.quaternion;
using UnityEngine;

using static Unity.Mathematics.math;

public class Fractal : MonoBehaviour
{
    static quaternion[] rotations = {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };

    static readonly int colorAId = Shader.PropertyToID("_ColorA");
    static readonly int colorBId = Shader.PropertyToID("_ColorB");
    static readonly int matricesId = Shader.PropertyToID("_Matrices");
    static readonly int sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");

    static MaterialPropertyBlock propertyBlock;

    struct FractalPart
    {
        public float3 worldPosition;
        public quaternion rotation, worldRotation;
        public float maxSagAngle, spinAngle, spinVelocity;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor
    {
        public float scale;
        public float deltaTime;

        [ReadOnly]
        public NativeArray<FractalPart> parents;

        public NativeArray<FractalPart> parts;

        [WriteOnly]
        public NativeArray<float3x4> matrices;

        public void Execute(int i)
        {
            int parentIndex = i / 5;
            FractalPart parent = parents[parentIndex];
            FractalPart part = parts[i];
            part.spinAngle += part.spinVelocity * deltaTime;
            float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
            float3 sagAxis = cross(up(), upAxis);
            float sagMagnitude = length(sagAxis);
            quaternion baseRotation;
            if (sagMagnitude > 0f)
            {
                sagAxis /= sagMagnitude;
                quaternion sagRotation = quaternion.AxisAngle(sagAxis, part.maxSagAngle * sagMagnitude);
                baseRotation = mul(sagRotation, parent.worldRotation);
            }
            else
            {
                baseRotation = parent.worldRotation;
            }
            part.worldRotation = mul(baseRotation,
                mul(part.rotation, quaternion.RotateY(part.spinAngle)));
            part.worldPosition = parent.worldPosition
                + mul(part.worldRotation, float3(0f, 1.5f * scale, 0f));
            parts[i] = part;

            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    }

    [SerializeField, Range(3, 8)]
    int depth = 4;
    [SerializeField]
    Mesh mesh, leafMesh;
    [SerializeField]
    Material material;
    [SerializeField]
    Gradient gradientA, gradientB;
    [SerializeField]
    Color leafColorA, leafColorB;
    [SerializeField, Range(0f, 90f)]
    float maxSagAngleA = 15f, maxSagAngleB = 25f;
    [SerializeField, Range(0f, 90f)]
    float spinSpeedA = 20f, spinSpeedB = 25f;
    [SerializeField, Range(0f, 1f)]
    float reverseSpinChance = 0.25f;

    NativeArray<FractalPart>[] parts;

    NativeArray<float3x4>[] matrices;

    ComputeBuffer[] matricesBuffers;

    Vector4[] sequenceNumbers;

    void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        sequenceNumbers = new Vector4[depth];
        int stride = 12 * 4;
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
            sequenceNumbers[i] = new Vector4(UnityEngine.Random.value, UnityEngine.Random.value,
                UnityEngine.Random.value, UnityEngine.Random.value);
        }
        parts[0][0] = CreatePart(0);
        for (int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
        {
            var level = parts[levelIndex];
            for (int levelIterator = 0; levelIterator < level.Length; levelIterator += 5)
            {
                for (int childIndex = 0; childIndex < 5; childIndex++)
                {
                    level[levelIterator + childIndex]
                        = CreatePart(childIndex);
                }
            }
        }
        propertyBlock ??= new MaterialPropertyBlock();
    }

    void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }
        parts = null;
        matrices = null;
        matricesBuffers = null;
        sequenceNumbers = null;
    }

    void OnValidate()
    {
        if (parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    FractalPart CreatePart(int childIndex) => new FractalPart
    {
        maxSagAngle = radians(UnityEngine.Random.Range(maxSagAngleA, maxSagAngleB)),
        rotation = rotations[childIndex],
        spinVelocity = (UnityEngine.Random.value < reverseSpinChance ? -1f : 1f)
            * radians(UnityEngine.Random.Range(spinSpeedA, spinSpeedB))
    };

    void Update()
    {
        float deltaTime = Time.deltaTime;
        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += rootPart.spinVelocity * deltaTime;
        rootPart.worldRotation = mul(transform.rotation,
            mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
        rootPart.worldPosition = transform.position;
        parts[0][0] = rootPart;
        float objectScale = transform.lossyScale.x;
        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);
        float scale = objectScale;
        JobHandle jobHandle = default;
        for (int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
        {
            scale *= 0.5f;
            jobHandle = new UpdateFractalLevelJob
            {
                deltaTime = deltaTime,
                scale = scale,
                parents = parts[levelIndex - 1],
                parts = parts[levelIndex],
                matrices = matrices[levelIndex]
            }.ScheduleParallel(parts[levelIndex].Length, 5, jobHandle);
        }
        jobHandle.Complete();
        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        int leafIndex = matricesBuffers.Length - 1;
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            Color colorA, colorB;
            Mesh instanceMesh;
            if (i == leafIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafMesh;
            }
            else
            {
                float gradientInterpolator = i / (matricesBuffers.Length - 1f);
                colorA = gradientA.Evaluate(gradientInterpolator);
                colorB = gradientB.Evaluate(gradientInterpolator);
                instanceMesh = mesh;
            }
            propertyBlock.SetColor(colorAId, colorA);
            propertyBlock.SetColor(colorBId, colorB);
            propertyBlock.SetBuffer(matricesId, buffer);
            propertyBlock.SetVector(sequenceNumbersId, sequenceNumbers[i]);
            Graphics.DrawMeshInstancedProcedural(
                instanceMesh, 0, material, bounds, buffer.count, propertyBlock);
        }
    }
}