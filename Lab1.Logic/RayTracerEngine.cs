using System;
using System.Collections.Generic;
using System.Text;

namespace Lab1.Logic
{
    public class MathCalculationException : Exception { public MathCalculationException(string msg) : base(msg) { } }
    public class InvalidSceneSetupException : Exception { public InvalidSceneSetupException(string msg) : base(msg) { } }

    public struct Vec3
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, double s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(Vec3 a, Vec3 b) => new Vec3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

        public double Dot(Vec3 other) => X * other.X + Y * other.Y + Z * other.Z;
        public double Length() => Math.Sqrt(Dot(this));

        public Vec3 Cross(Vec3 other) => new Vec3(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X);

        public Vec3 Normalize()
        {
            double len = Length();
            if (len < 1e-8) throw new MathCalculationException("Неможливо нормалізувати нульовий вектор.");
            return this * (1.0 / len);
        }
    }

    public struct Ray
    {
        public Vec3 Origin { get; }
        public Vec3 Direction { get; }
        public Ray(Vec3 origin, Vec3 direction) { Origin = origin; Direction = direction.Normalize(); }
    }

    public class Material
    {
        public Vec3 Color { get; }
        public double Reflectivity { get; }
        public Material(Vec3 color, double reflectivity = 0.0)
        {
            Color = color;
            Reflectivity = Math.Clamp(reflectivity, 0.0, 1.0);
        }
    }

    public struct HitRecord
    {
        public bool IsHit { get; }
        public double Distance { get; }
        public Vec3 Point { get; }
        public Vec3 Normal { get; }
        public Material Material { get; }

        public HitRecord(double distance, Vec3 point, Vec3 normal, Material material)
        {
            IsHit = true; Distance = distance; Point = point; Normal = normal; Material = material;
        }
    }

    public abstract class SceneObject
    {
        public Material Material { get; set; } = null!;
        public abstract HitRecord Intersect(Ray ray);
    }

    public class Sphere : SceneObject
    {
        public Vec3 Center { get; }
        public double Radius { get; }

        public Sphere(Vec3 center, double radius, Material material)
        {
            if (radius <= 0) throw new ArgumentException("Радіус має бути додатнім.");
            Center = center; Radius = radius; Material = material;
        }

        public override HitRecord Intersect(Ray ray)
        {
            Vec3 oc = ray.Origin - Center;
            double a = ray.Direction.Dot(ray.Direction);
            double b = 2.0 * oc.Dot(ray.Direction);
            double c = oc.Dot(oc) - Radius * Radius;
            double discriminant = b * b - 4 * a * c;

            if (discriminant > 0)
            {
                double t = (-b - Math.Sqrt(discriminant)) / (2.0 * a);
                if (t > 0.001)
                {
                    Vec3 point = ray.Origin + ray.Direction * t;
                    Vec3 normal = (point - Center).Normalize();
                    return new HitRecord(t, point, normal, Material);
                }
            }
            return new HitRecord();
        }
    }

    public class ScenePlane : SceneObject
    {
        public Vec3 Normal { get; }
        public double Distance { get; }

        public ScenePlane(Vec3 normal, double distance, Material material)
        {
            Normal = normal.Normalize(); Distance = distance; Material = material;
        }

        public override HitRecord Intersect(Ray ray)
        {
            double denom = Normal.Dot(ray.Direction);
            if (Math.Abs(denom) > 1e-6)
            {
                double t = (Distance - Normal.Dot(ray.Origin)) / denom;
                if (t > 0.001)
                {
                    Vec3 point = ray.Origin + ray.Direction * t;
                    return new HitRecord(t, point, Normal, Material);
                }
            }
            return new HitRecord();
        }
    }

    public class PointLight
    {
        public Vec3 Position { get; }
        public double Intensity { get; }
        public PointLight(Vec3 position, double intensity) { Position = position; Intensity = intensity; }
    }

    public class Scene
    {
        public string Name { get; set; } = "Main Scene";
        public Vec3 BackgroundColor { get; set; } = new Vec3(0.1, 0.1, 0.1);
        private readonly List<SceneObject> _objects = new List<SceneObject>();
        private readonly List<PointLight> _lights = new List<PointLight>();

        public void AddObject(SceneObject obj) => _objects.Add(obj);
        public void AddLight(PointLight light) => _lights.Add(light);

        public IReadOnlyList<SceneObject> GetObjects() => _objects.AsReadOnly();
        public IReadOnlyList<PointLight> GetLights() => _lights.AsReadOnly();
    }

    public class Renderer
    {
        private readonly Scene _scene;
        public Renderer(Scene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            if (_scene.GetObjects().Count == 0) throw new InvalidSceneSetupException("Сцена не містить об'єктів для рендеру!");
        }

        public Vec3 TraceRay(Ray ray, int bounceDepth = 0)
        {
            if (bounceDepth > 3) return new Vec3(0, 0, 0);

            HitRecord closestHit = new HitRecord();
            double closestT = double.MaxValue;

            foreach (var obj in _scene.GetObjects())
            {
                var hit = obj.Intersect(ray);
                if (hit.IsHit && hit.Distance < closestT)
                {
                    closestT = hit.Distance;
                    closestHit = hit;
                }
            }

            if (!closestHit.IsHit) return _scene.BackgroundColor;

            Vec3 finalColor = new Vec3(0, 0, 0);
            foreach (var light in _scene.GetLights())
            {
                Vec3 lightDir = (light.Position - closestHit.Point).Normalize();
                double diffuse = Math.Max(0, closestHit.Normal.Dot(lightDir));
                finalColor += closestHit.Material.Color * (diffuse * light.Intensity);
            }

            return finalColor;
        }

        public string RenderToPPM(int width, int height)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("P3");
            sb.AppendLine($"{width} {height}");
            sb.AppendLine("255");

            Vec3 cameraOrigin = new Vec3(0, 0, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double u = (double)x / width * 2.0 - 1.0;
                    double v = (double)y / height * 2.0 - 1.0;

                    Ray ray = new Ray(cameraOrigin, new Vec3(u, -v, -1));
                    Vec3 color = TraceRay(ray);

                    int r = (int)(Math.Clamp(color.X, 0, 1) * 255);
                    int g = (int)(Math.Clamp(color.Y, 0, 1) * 255);
                    int b = (int)(Math.Clamp(color.Z, 0, 1) * 255);

                    sb.AppendLine($"{r} {g} {b}");
                }
            }
            return sb.ToString();
        }
    }
}