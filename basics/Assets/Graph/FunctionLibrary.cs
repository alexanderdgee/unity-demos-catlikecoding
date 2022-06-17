using UnityEngine;

public static class FunctionLibrary
{
    public delegate Vector3 Function(float u, float v, float t);

    public enum FunctionName {
        Wave,
        MultiWave,
        Ripple,
        SphereCollapsing,
        SphereBandedRotating,
        TorusStar
    }

    static Function[] functions = {
        Wave,
        MultiWave,
        Ripple,
        SphereCollapsing,
        SphereBandedRotating,
        TorusStar
    };

    public static Function GetFunction(FunctionName name)
    {
        int index = (int) name;
        if (index < 0 || index > functions.Length)
        {
            index = 0;
        }
        return functions[index];
    }

    public static Vector3 Wave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Mathf.Sin(Mathf.PI * (u + v + t));
        p.z = v;
        return p;
    }

    public static Vector3 MultiWave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Mathf.Sin(Mathf.PI * (u + 0.5f * t));
        p.y += 0.5f * Mathf.Sin(2f * Mathf.PI * (v + t));
        p.y += Mathf.Sin(Mathf.PI * (u + v + 0.25f * t));
        p.y *= 2f / 5f;
        p.z = v;
        return p;
    }

    public static Vector3 Ripple(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        float d = Mathf.Sqrt(u * u + v * v);
        p.y = Mathf.Sin(Mathf.PI * (4f * d - t));
        p.y /= (1f + 10f * d);
        p.z = v;
        return p;
    }

    public static Vector3 Sphere(float u, float v, float t)
    {
        float r = Mathf.Cos(0.5f * Mathf.PI * v);
        Vector3 p;
        p.x = r * Mathf.Sin(Mathf.PI * u);
        p.y = Mathf.Sin(Mathf.PI * 0.5f * v);
        p.z = r * Mathf.Cos(Mathf.PI * u);
        return p;
    }

    public static Vector3 SphereCollapsing(float u, float v, float t)
    {
        float r = 0.5f + 0.5f * Mathf.Sin(Mathf.PI * t);
        float s = r * Mathf.Cos(0.5f * Mathf.PI * v);
        Vector3 p;
        p.x = s * Mathf.Sin(Mathf.PI * u);
        p.y = r * Mathf.Sin(Mathf.PI * 0.5f * v);
        p.z = s * Mathf.Cos(Mathf.PI * u);
        return p;
    }

    public static Vector3 SphereBandedRotating(float u, float v, float t)
    {
        float r = 0.9f + 0.1f * Mathf.Sin(Mathf.PI * (6f * u + 4f * v + t));
        float s = r * Mathf.Cos(0.5f * Mathf.PI * v);
        Vector3 p;
        p.x = s * Mathf.Sin(Mathf.PI * u);
        p.y = r * Mathf.Sin(Mathf.PI * 0.5f * v);
        p.z = s * Mathf.Cos(Mathf.PI * u);
        return p;
    }

    public static Vector3 TorusRing(float u, float v, float t)
    {
        float r1 = 0.75f;
        float r2 = 0.25f;
        float s = r1 + r2 * Mathf.Cos(Mathf.PI * v);
        Vector3 p;
        p.x = s * Mathf.Sin(Mathf.PI * u);
        p.y = r2 * Mathf.Sin(Mathf.PI * v);
        p.z = s * Mathf.Cos(Mathf.PI * u);
        return p;
    }

    public static Vector3 TorusStar(float u, float v, float t)
    {
        float r1 = 0.7f + 0.1f * Mathf.Sin(Mathf.PI * (6f * u + 0.5f * t));
        float r2 = 0.15f + 0.05f * Mathf.Sin(Mathf.PI * (8f * u + 4f * v + 2f * t));
        float s = r1 + r2 * Mathf.Cos(Mathf.PI * v);
        Vector3 p;
        p.x = s * Mathf.Sin(Mathf.PI * u);
        p.y = r2 * Mathf.Sin(Mathf.PI * v);
        p.z = s * Mathf.Cos(Mathf.PI * u);
        return p;
    }
}
