using UnityEngine;

namespace Fractal3
{
    public class Fractal : MonoBehaviour
    {
        static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

        static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

        static readonly int matricesId = Shader.PropertyToID("_Matrices");

        static MaterialPropertyBlock propertyBlock;

        struct FractalPart
        {
            public Vector3 direction, worldPosition;
            public Quaternion rotation, worldRotation;
            public float spinAngle;
        }

        [SerializeField, Range(1, 8)]
        int depth = 4;
        [SerializeField]
        Mesh mesh;
        [SerializeField]
        Material material;

        FractalPart[][] parts;

        Matrix4x4[][] matrices;

        ComputeBuffer[] matricesBuffers;

        void OnEnable()
        {
            parts = new FractalPart[depth][];
            matrices = new Matrix4x4[depth][];
            matricesBuffers = new ComputeBuffer[depth];
            int stride = 16 * 4;
            for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
            {
                parts[i] = new FractalPart[length];
                matrices[i] = new Matrix4x4[length];
                matricesBuffers[i] = new ComputeBuffer(length, stride);
            }
            parts[0][0] = CreatePart(0);
            for (int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
            {
                FractalPart[] level = parts[levelIndex];
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
            }
            parts = null;
            matrices = null;
            matricesBuffers = null;
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
            direction = directions[childIndex],
            rotation = rotations[childIndex]
        };

        void Update()
        {
            float spinAngleDelta = 22.5f * Time.deltaTime;
            FractalPart rootPart = parts[0][0];
            rootPart.spinAngle += spinAngleDelta;
            rootPart.worldRotation = transform.rotation
                * (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
            rootPart.worldPosition = transform.position;
            parts[0][0] = rootPart;
            float objectScale = transform.lossyScale.x;
            matrices[0][0] = Matrix4x4.TRS(
                rootPart.worldPosition, rootPart.worldRotation, objectScale * Vector3.one);
            float scale = objectScale;
            for (int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
            {
                scale *= 0.5f;
                FractalPart[] parentParts = parts[levelIndex - 1];
                FractalPart[] level = parts[levelIndex];
                Matrix4x4[] levelMatrices = matrices[levelIndex];
                for (int levelIterator = 0; levelIterator < level.Length; levelIterator++)
                {
                    int parentIndex = levelIterator / 5;
                    FractalPart parent = parentParts[parentIndex];
                    FractalPart part = level[levelIterator];
                    part.spinAngle += spinAngleDelta;
                    part.worldRotation = parent.worldRotation
                        * (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
                    part.worldPosition =
                        parent.worldPosition
                        + parent.worldRotation * (1.5f * scale * part.direction);
                    level[levelIterator] = part;
                    levelMatrices[levelIterator] = Matrix4x4.TRS(
                        part.worldPosition, part.worldRotation, scale * Vector3.one);
                }
            }
            var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
            for (int i = 0; i < matricesBuffers.Length; i++)
            {
                ComputeBuffer buffer = matricesBuffers[i];
                buffer.SetData(matrices[i]);
                propertyBlock.SetBuffer(matricesId, buffer);
                Graphics.DrawMeshInstancedProcedural(
                    mesh, 0, material, bounds, buffer.count, propertyBlock);
            }
        }
    }
}
