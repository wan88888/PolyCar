using NUnit.Framework;
using UnityEngine;

public sealed class CourseTerrainTests
{
    [TestCase(2,10f)][TestCase(7,14f)]
    public void MountainRoadsHaveRealClimbsWithoutAbruptGrades(int index,float minimumClimb)
    {
        var route=CourseDifficultyBuilder.Route(index);float min=float.MaxValue,max=float.MinValue;
        for(int i=0;i<route.Length;i++)
        {
            min=Mathf.Min(min,route[i].y);max=Mathf.Max(max,route[i].y);
            if(i==0)continue;var step=route[i]-route[i-1];
            float horizontal=new Vector2(step.x,step.z).magnitude;
            Assert.That(Mathf.Abs(step.y)/Mathf.Max(horizontal,0.001f),Is.LessThan(0.081f),"Unsafe road grade");
        }
        Assert.That(max-min,Is.GreaterThan(minimumClimb));
    }

    [TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
    public void TerrainCarvingLeavesCenterlineBelowRoad(int index)
    {
        var route=CourseDifficultyBuilder.Route(index);float width=CourseDifficultyBuilder.Widths[index];
        foreach(var p in route)
        {
            float ground=CourseTerrainBuilder.GroundHeight(index,p,route,width);
            Assert.That(ground,Is.EqualTo(p.y+(index>=8?0.035f:-0.18f)).Within(0.005f));
        }
    }

    [Test]
    public void NightCircuitElevationJoinsAtSeam()
    {
        var route=CourseDifficultyBuilder.Route(9);
        Assert.That(Vector3.Distance(route[0],route[route.Length-1]),Is.LessThan(0.001f));
    }
}
