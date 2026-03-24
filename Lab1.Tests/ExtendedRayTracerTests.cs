using NUnit.Framework;
using Lab1.Logic; 
using System;
using System.Collections.Generic;

namespace Lab1.Tests
{
    [TestFixture]
    public class ExtendedRayTracerTests
    {
        private Scene _scene = null!;
        private Material _redMaterial = null!;
        private Material _blueMaterial = null!;

        [SetUp]
        public void Setup()
        {
            _scene = new Scene { Name = "Advanced Test Scene" };
            _redMaterial = new Material(new Vec3(1, 0, 0), 0.5);
            _blueMaterial = new Material(new Vec3(0, 0, 1), 0.1);
        }

        [Test]
        public void Vec3_MathOperations_AreValid()
        {
            var v1 = new Vec3(1, 0, 0);
            var v2 = new Vec3(0, 1, 0);
            var cross = v1.Cross(v2);

            Assert.That(cross.Z, Is.EqualTo(1.0));                  
            Assert.That(v1.Dot(v2) == 0, Is.True);                  
            Assert.That(cross.X, Is.Zero);                          
            Assert.That(v1.Length(), Is.GreaterThanOrEqualTo(1.0)); 
        }

        [Test]
        public void Vec3_NormalizeZero_ThrowsException()
        {
            var zero = new Vec3(0, 0, 0);
            Assert.Throws<MathCalculationException>(() => zero.Normalize());
        }
        [Test]
        public void Vec3_Normalize_ReturnsUnitVector()
        {
            var v = new Vec3(3, 0, 0);
            var normalized = v.Normalize();

            Assert.That(normalized.X, Is.EqualTo(1).Within(1e-6));
            Assert.That(normalized.Y, Is.Zero);
            Assert.That(normalized.Z, Is.Zero);
        }

        [Test]
        public void Material_Reflectivity_ClampedCorrectly()
        {
            var mat = new Material(new Vec3(1, 1, 1), 5.0);
            Assert.That(mat.Reflectivity, Is.EqualTo(1.0));

            var mat2 = new Material(new Vec3(1, 1, 1), -2.0);
            Assert.That(mat2.Reflectivity, Is.EqualTo(0.0));
        }

        [Test]
        public void Renderer_EmptyScene_ThrowsInvalidSceneSetupException()
        {
            Assert.Throws<InvalidSceneSetupException>(() => new Renderer(_scene));
        }

        [Test]
        public void Scene_CollectionsAndStrings_HandledCorrectly()
        {
            var sphere = new Sphere(new Vec3(0, 0, -5), 1.0, _redMaterial);
            var light = new PointLight(new Vec3(0, 5, 0), 1.0);

            _scene.AddObject(sphere);
            _scene.AddLight(light);

            var objects = _scene.GetObjects();
            CollectionAssert.IsNotEmpty(objects);
            CollectionAssert.AllItemsAreInstancesOfType(objects, typeof(SceneObject));
            CollectionAssert.Contains(objects, sphere);

            Renderer renderer = new Renderer(_scene);
            string ppmImage = renderer.RenderToPPM(2, 2);

            StringAssert.StartsWith("P3", ppmImage);
            StringAssert.Contains("255", ppmImage);
        }

        [TestCase(0, 0, -5, 1.0, true)]
        [TestCase(0, 5, -5, 1.0, false)]
        [TestCase(0, 0, 5, 1.0, false)]
        public void Sphere_Intersection_CalculatesCorrectly(double x, double y, double z, double radius, bool expectedHit)
        {
            var sphere = new Sphere(new Vec3(x, y, z), radius, _redMaterial);
            var ray = new Ray(new Vec3(0, 0, 0), new Vec3(0, 0, -1));

            var hit = sphere.Intersect(ray);
            Assert.That(hit.IsHit, Is.EqualTo(expectedHit));
        }

        [TestCaseSource(nameof(PlaneIntersectionCases))]
        public void ScenePlane_Intersection_CalculatesCorrectly(Ray ray, bool expectedHit)
        {
            var plane = new ScenePlane(new Vec3(0, 1, 0), -2.0, _blueMaterial);
            var hit = plane.Intersect(ray);
            Assert.That(hit.IsHit, Is.EqualTo(expectedHit));
        }

        static object[] PlaneIntersectionCases =
        {
            new object[] { new Ray(new Vec3(0, 0, 0), new Vec3(0, -1, 0)), true },
            new object[] { new Ray(new Vec3(0, 0, 0), new Vec3(0, 1, 0)), false },
            new object[] { new Ray(new Vec3(0, 0, 0), new Vec3(1, 0, 0)), false }
        };

        [Test]
        public void Render_HighResolution_SkippedOnDebug()
        {
            Assume.That(!System.Diagnostics.Debugger.IsAttached, "Пропускаємо важкий тест під час дебагу.");

            _scene.AddObject(new Sphere(new Vec3(0, 0, -5), 1.0, _redMaterial));
            Renderer renderer = new Renderer(_scene);
            string result = renderer.RenderToPPM(100, 100);

            Assert.That(result, Is.Not.Empty);
        }
        [Test]
        public void ConfigFile_Parameters_AreReadCorrectly()
        {
           
            int maxBounces = TestContext.Parameters.Get("MaxRayBounces", 3);

            string environment = TestContext.Parameters.Get("RenderEnvironment", "Unknown");

            Assert.That(maxBounces, Is.EqualTo(3), "Параметр MaxRayBounces не зчитався з конфігу.");
            Assert.That(environment, Is.EqualTo("Test"), "Параметр RenderEnvironment не зчитався з конфігу.");
        }
    }
}