using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using PRIMITIVE = SharpGLTF.Geometry.PrimitiveBuilder<
    SharpGLTF.Materials.MaterialBuilder,
    SharpGLTF.Geometry.VertexTypes.VertexPosition,
    SharpGLTF.Geometry.VertexTypes.VertexEmpty,
    SharpGLTF.Geometry.VertexTypes.VertexEmpty>;
using MESH = SharpGLTF.Geometry.MeshBuilder<
    SharpGLTF.Materials.MaterialBuilder,
    SharpGLTF.Geometry.VertexTypes.VertexPosition,
    SharpGLTF.Geometry.VertexTypes.VertexEmpty,
    SharpGLTF.Geometry.VertexTypes.VertexEmpty>;
using VERTEX = SharpGLTF.Geometry.VertexBuilder<
    SharpGLTF.Geometry.VertexTypes.VertexPosition,
    SharpGLTF.Geometry.VertexTypes.VertexEmpty,
    SharpGLTF.Geometry.VertexTypes.VertexEmpty>;

namespace sp26se058_3dprintshop_be.Infrastructure.OpenRouter;

public sealed class AiPrimitiveJson
{
    public string? Type { get; set; }
    public float[]? Center { get; set; }
    public float[]? Size { get; set; }
    public float Radius { get; set; }
    public float Height { get; set; }
    public int Segments { get; set; }
    public float[]? Color { get; set; }
    public float[]? RotationEulerDeg { get; set; }
}

public sealed class AiSceneJson
{
    public List<AiPrimitiveJson>? Primitives { get; set; }
}

internal static class AiSceneToGlbComposer
{
    public static byte[] Build(AiSceneJson? scene)
    {
        var sceneBuilder = new SceneBuilder("openrouter-scene");
        var primitives = scene?.Primitives?.Where(p => p != null).ToList() ?? new List<AiPrimitiveJson>();
        if (primitives.Count == 0)
        {
            AddGround(sceneBuilder);
        }
        else
        {
            foreach (var p in primitives)
            {
                AddPrimitive(sceneBuilder, p);
            }
        }

        var model = sceneBuilder.ToGltf2();
        var seg = model.WriteGLB();
        return seg.AsSpan().ToArray();
    }

    private static void AddGround(SceneBuilder scene)
    {
        var mesh = new MESH("ground");
        var mat = CreateMaterial(0.35f, 0.35f, 0.38f);
        var prim = mesh.UsePrimitive(mat);
        var m = Matrix4x4.CreateTranslation(0f, -0.01f, 0f) * Matrix4x4.CreateScale(4f, 0.02f, 4f);
        AddUnitBoxTriangles(prim, m);
        scene.AddRigidMesh(mesh, Matrix4x4.Identity);
    }

    private static void AddPrimitive(SceneBuilder scene, AiPrimitiveJson p)
    {
        var type = (p.Type ?? "box").Trim().ToLowerInvariant();
        var center = ToVec3(p.Center, Vector3.Zero);
        var rot = ToVec3(p.RotationEulerDeg, Vector3.Zero);
        var color = ToColorRgb(p.Color);

        switch (type)
        {
            case "sphere":
                AddSphere(scene, center, rot, color, MathF.Max(0.02f, p.Radius <= 0 ? 0.25f : p.Radius), Math.Max(8, p.Segments > 0 ? p.Segments : 20));
                break;
            case "cylinder":
                AddCylinder(scene, center, rot, color,
                    MathF.Max(0.02f, p.Radius <= 0 ? 0.2f : p.Radius),
                    MathF.Max(0.02f, p.Height <= 0 ? 0.4f : p.Height),
                    Math.Max(8, p.Segments > 0 ? p.Segments : 16));
                break;
            default:
                AddBox(scene, center, rot, color, ToVec3(p.Size, new Vector3(0.4f, 0.4f, 0.4f)));
                break;
        }
    }

    private static void AddBox(SceneBuilder scene, Vector3 center, Vector3 rotDeg, Vector3 color, Vector3 size)
    {
        var mesh = new MESH("box");
        var mat = CreateMaterial(color.X, color.Y, color.Z);
        var prim = mesh.UsePrimitive(mat);
        var m = BuildMatrix(center, rotDeg, size);
        AddUnitBoxTriangles(prim, m);
        scene.AddRigidMesh(mesh, Matrix4x4.Identity);
    }

    private static void AddSphere(SceneBuilder scene, Vector3 center, Vector3 rotDeg, Vector3 color, float radius, int segments)
    {
        var mesh = new MESH("sphere");
        var mat = CreateMaterial(color.X, color.Y, color.Z);
        var prim = mesh.UsePrimitive(mat);
        var lat = Math.Max(6, segments / 2);
        var lon = segments;
        var m = BuildMatrix(center, rotDeg, new Vector3(radius, radius, radius));

        for (var i = 0; i < lat; i++)
        {
            var v0 = (float)i / lat;
            var v1 = (float)(i + 1) / lat;
            var theta0 = v0 * MathF.PI;
            var theta1 = v1 * MathF.PI;

            for (var j = 0; j < lon; j++)
            {
                var u0 = (float)j / lon;
                var u1 = (float)(j + 1) / lon;
                var phi0 = u0 * MathF.PI * 2f;
                var phi1 = u1 * MathF.PI * 2f;

                var p00 = SpherePoint(theta0, phi0);
                var p01 = SpherePoint(theta0, phi1);
                var p10 = SpherePoint(theta1, phi0);
                var p11 = SpherePoint(theta1, phi1);

                AddTri(prim, m, p00, p10, p11);
                AddTri(prim, m, p00, p11, p01);
            }
        }

        scene.AddRigidMesh(mesh, Matrix4x4.Identity);
    }

    private static Vector3 SpherePoint(float theta, float phi)
    {
        var st = MathF.Sin(theta);
        return new Vector3(st * MathF.Cos(phi), MathF.Cos(theta), st * MathF.Sin(phi));
    }

    private static void AddCylinder(SceneBuilder scene, Vector3 center, Vector3 rotDeg, Vector3 color, float radius, float height, int segments)
    {
        var mesh = new MESH("cylinder");
        var mat = CreateMaterial(color.X, color.Y, color.Z);
        var prim = mesh.UsePrimitive(mat);
        var m = BuildMatrix(center, rotDeg, new Vector3(radius, height, radius));
        var h = 1f;

        for (var i = 0; i < segments; i++)
        {
            var a0 = (float)i / segments * MathF.PI * 2f;
            var a1 = (float)(i + 1) / segments * MathF.PI * 2f;
            var x0 = MathF.Cos(a0);
            var z0 = MathF.Sin(a0);
            var x1 = MathF.Cos(a1);
            var z1 = MathF.Sin(a1);

            var b0 = new Vector3(x0, -h * 0.5f, z0);
            var b1 = new Vector3(x1, -h * 0.5f, z1);
            var t0 = new Vector3(x0, h * 0.5f, z0);
            var t1 = new Vector3(x1, h * 0.5f, z1);

            AddTri(prim, m, b0, b1, t1);
            AddTri(prim, m, b0, t1, t0);
        }

        var capCenterB = new Vector3(0, -h * 0.5f, 0);
        var capCenterT = new Vector3(0, h * 0.5f, 0);
        for (var i = 0; i < segments; i++)
        {
            var a0 = (float)i / segments * MathF.PI * 2f;
            var a1 = (float)(i + 1) / segments * MathF.PI * 2f;
            var e0 = new Vector3(MathF.Cos(a0), -h * 0.5f, MathF.Sin(a0));
            var e1 = new Vector3(MathF.Cos(a1), -h * 0.5f, MathF.Sin(a1));
            AddTri(prim, m, capCenterB, e1, e0);
        }

        for (var i = 0; i < segments; i++)
        {
            var a0 = (float)i / segments * MathF.PI * 2f;
            var a1 = (float)(i + 1) / segments * MathF.PI * 2f;
            var e0 = new Vector3(MathF.Cos(a0), h * 0.5f, MathF.Sin(a0));
            var e1 = new Vector3(MathF.Cos(a1), h * 0.5f, MathF.Sin(a1));
            AddTri(prim, m, capCenterT, e0, e1);
        }

        scene.AddRigidMesh(mesh, Matrix4x4.Identity);
    }

    private static void AddUnitBoxTriangles(PRIMITIVE prim, Matrix4x4 m)
    {
        var u = 0.5f;
        Vector3[] corners =
        {
            new(-u, -u, -u), new(u, -u, -u), new(u, u, -u), new(-u, u, -u),
            new(-u, -u, u), new(u, -u, u), new(u, u, u), new(-u, u, u),
        };

        int[][] faces =
        {
            new[] { 0, 1, 2, 3 },
            new[] { 5, 4, 7, 6 },
            new[] { 4, 0, 3, 7 },
            new[] { 1, 5, 6, 2 },
            new[] { 3, 2, 6, 7 },
            new[] { 4, 5, 1, 0 },
        };

        foreach (var f in faces)
        {
            var a = Vector3.Transform(corners[f[0]], m);
            var b = Vector3.Transform(corners[f[1]], m);
            var c = Vector3.Transform(corners[f[2]], m);
            var d = Vector3.Transform(corners[f[3]], m);
            prim.AddTriangle(V(a), V(b), V(c));
            prim.AddTriangle(V(a), V(c), V(d));
        }
    }

    private static void AddTri(PRIMITIVE prim, Matrix4x4 m, Vector3 a, Vector3 b, Vector3 c)
    {
        prim.AddTriangle(
            V(Vector3.Transform(a, m)),
            V(Vector3.Transform(b, m)),
            V(Vector3.Transform(c, m)));
    }

    private static VERTEX V(Vector3 p) => new(new VertexPosition(p.X, p.Y, p.Z));

    private static Matrix4x4 BuildMatrix(Vector3 center, Vector3 rotDeg, Vector3 scale)
    {
        var rad = rotDeg * (MathF.PI / 180f);
        var r = Matrix4x4.CreateRotationZ(rad.Z) * Matrix4x4.CreateRotationY(rad.Y) * Matrix4x4.CreateRotationX(rad.X);
        var s = Matrix4x4.CreateScale(scale);
        var t = Matrix4x4.CreateTranslation(center);
        return t * r * s;
    }

    private static MaterialBuilder CreateMaterial(float r, float g, float b)
    {
        return new MaterialBuilder()
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader()
            .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(r, g, b, 1f));
    }

    private static Vector3 ToVec3(float[]? arr, Vector3 def)
    {
        if (arr is not { Length: >= 3 }) return def;
        return new Vector3(arr[0], arr[1], arr[2]);
    }

    private static Vector3 ToColorRgb(float[]? arr)
    {
        if (arr is not { Length: >= 3 }) return new Vector3(0.65f, 0.65f, 0.7f);
        return new Vector3(Clamp01(arr[0]), Clamp01(arr[1]), Clamp01(arr[2]));
    }

    private static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
}
