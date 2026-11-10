using NUnit.Framework;
using UnityEngine;

public sealed class EnvironmentValidationTests
{
    [Test]
    public void GradeMeasuresVerticalRiseOverHorizontalDistance()
    {
        Assert.That(EnvironmentValidation.MaximumGrade(new[] { Vector3.zero, new Vector3(30, 4, 40), new Vector3(60, 4, 80) }), Is.EqualTo(.08f).Within(.0001f));
    }

    [Test]
    public void PhysicsAuditIgnoresPickupsAndIntentionalRaceObstaclesButIncludesScenery()
    {
        var level = new GameObject("Audit level");
        try
        {
            var obstacles = new GameObject("Obstacles"); obstacles.transform.SetParent(level.transform);
            var cone = new GameObject("Cone", typeof(BoxCollider)); cone.transform.SetParent(obstacles.transform);
            var pickup = new GameObject("Coin", typeof(SphereCollider)); pickup.transform.SetParent(level.transform); pickup.GetComponent<Collider>().isTrigger = true;
            var tree = new GameObject("Scenery collision pine", typeof(BoxCollider)); tree.transform.SetParent(level.transform);
            Assert.IsFalse(EnvironmentValidation.IsEnvironmentCollider(cone.GetComponent<Collider>(), level.transform));
            Assert.IsFalse(EnvironmentValidation.IsEnvironmentCollider(pickup.GetComponent<Collider>(), level.transform));
            Assert.IsTrue(EnvironmentValidation.IsEnvironmentCollider(tree.GetComponent<Collider>(), level.transform));
        }
        finally { Object.DestroyImmediate(level); }
    }

    [Test]
    public void FlatForestWithoutItsLandmarkCannotPassEnvironmentAcceptance()
    {
        var level = new GameObject("Incomplete forest", typeof(RaceCourse));
        try
        {
            level.GetComponent<RaceCourse>().centerLine = new[] { Vector3.zero, Vector3.forward * 100 };
            var result = EnvironmentValidation.Inspect(level.transform, 2);
            Assert.That(result.issues, Has.Some.Contains("meaningful elevation"));
            Assert.That(result.issues, Has.Some.Contains("Forest lookout"));
        }
        finally { Object.DestroyImmediate(level); }
    }

    [Test]
    public void BrokenMeshIsReportedInsteadOfBeingCountedAsFinishedArt()
    {
        var level = new GameObject("Incomplete harbor", typeof(RaceCourse));
        try
        {
            level.GetComponent<RaceCourse>().centerLine = new[] { Vector3.zero, Vector3.forward * 100 };
            var model = new GameObject("Missing asset", typeof(MeshFilter)); model.transform.SetParent(level.transform);
            var result = EnvironmentValidation.Inspect(level.transform, 0);
            Assert.That(result.issues, Has.Some.Contains("Missing mesh: Missing asset"));
        }
        finally { Object.DestroyImmediate(level); }
    }
}
