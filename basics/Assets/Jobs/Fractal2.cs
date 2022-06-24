using UnityEngine;

namespace FractalTwo
{
    public class Fractal2 : MonoBehaviour
    {
        static Vector3[] directions = {
            Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
        };

        static Quaternion[] rotations = {
            Quaternion.identity,
            Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
            Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
        };

        struct FractalPart
        {
            public Vector3 direction;
            public Quaternion rotation;
            public Transform transform;
        }

        [SerializeField, Range(1, 8)]
        int depth = 4;
        [SerializeField]
        Mesh mesh;
        [SerializeField]
        Material material;

        FractalPart[][] parts;

        void Awake()
        {
            parts = new FractalPart[depth][];
            for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
            {
                parts[i] = new FractalPart[length];
            }
            float scale = 1f;
            parts[0][0] = CreatePart(0, 0, scale);
            for (int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
            {
                scale *= 0.5f;
                FractalPart[] level = parts[levelIndex];
                for (int levelIterator = 0; levelIterator < level.Length; levelIterator += 5)
                {
                    for (int childIndex = 0; childIndex < 5; childIndex++)
                    {
                        level[levelIterator + childIndex]
                            = CreatePart(levelIndex, childIndex, scale);
                    }
                }
            }
        }

        FractalPart CreatePart(int levelIndex, int childIndex, float scale)
        {
            var go = new GameObject("Fractal Part L" + levelIndex + " C" + childIndex);
            go.transform.localScale = scale * Vector3.one;
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().material = material;
            return new FractalPart
            {
                direction = directions[childIndex],
                rotation = rotations[childIndex],
                transform = go.transform
            };
        }

        void Update()
        {
            Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);
            FractalPart rootPart = parts[0][0];
            rootPart.rotation *= deltaRotation;
            rootPart.transform.localRotation = rootPart.rotation;
            parts[0][0] = rootPart;
            for (int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
            {
                FractalPart[] parentParts = parts[levelIndex - 1];
                FractalPart[] level = parts[levelIndex];
                for (int levelIterator = 0; levelIterator < level.Length; levelIterator++)
                {
                    int parentIndex = levelIterator / 5;
                    Transform parentTransform = parentParts[parentIndex].transform;
                    FractalPart part = level[levelIterator];
                    part.rotation *= deltaRotation;
                    part.transform.localRotation =
                        parentTransform.localRotation * part.rotation;
                    part.transform.localPosition =
                        parentTransform.localPosition +
                        parentTransform.localRotation *
                        (1.5f * part.transform.localScale.x * part.direction);
                    level[levelIterator] = part;
                }
            }
        }
    }
}
