using NUnit.Framework;
using UnityEngine;

public sealed class DifficultyProgressionTests
{
    [Test]
    public void ProgressionHasNoAbruptWidthOrObstacleJump()
    {
        for(int i=1;i<10;i++)
        {
            Assert.That(CourseDifficultyBuilder.Widths[i-1]-CourseDifficultyBuilder.Widths[i],Is.InRange(0.25f,1f));
            Assert.That(CourseDifficultyBuilder.Obstacles[i]-CourseDifficultyBuilder.Obstacles[i-1],Is.InRange(1,2));
            Assert.That(CourseDifficultyBuilder.Gold[i],Is.GreaterThan(CourseDifficultyBuilder.Gold[i-1]));
            Assert.That(CourseDifficultyBuilder.Silver[i],Is.LessThan(CourseDifficultyBuilder.Gold[i]));
        }
        Assert.That(CourseDifficultyBuilder.Widths[1],Is.GreaterThanOrEqualTo(10));
        Assert.That(CourseDifficultyBuilder.Obstacles[1],Is.LessThanOrEqualTo(2));
    }

    [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
    public void ObstaclesLeaveStartFinishGatesAndEachOtherClear(int index)
    {
        var route=CourseDifficultyBuilder.Route(index);float length=0;
        for(int i=1;i<route.Length;i++)length+=Vector3.Distance(route[i-1],route[i]);
        var distances=CourseDifficultyBuilder.ObstacleDistances(route,CourseDifficultyBuilder.Obstacles[index],10+index);
        Assert.That(distances.Length,Is.EqualTo(CourseDifficultyBuilder.Obstacles[index]));
        for(int i=0;i<distances.Length;i++)
        {
            Assert.That(distances[i],Is.InRange(24,length-20));
            if(i>0)Assert.That(distances[i]-distances[i-1],Is.GreaterThanOrEqualTo(15));
            for(int g=1;g<=10+index;g++)Assert.That(Mathf.Abs(distances[i]-length*g/(10+index)),Is.GreaterThanOrEqualTo(7));
        }
    }

    [Test]
    public void SecondRouteUsesSmoothTurnsInsteadOfSharpJunctions()
    {
        var route=CourseDifficultyBuilder.Route(1);
        for(int i=1;i<route.Length-1;i++)Assert.That(Vector3.Angle(route[i]-route[i-1],route[i+1]-route[i]),Is.LessThan(8));
    }

    [Test]
    public void LegacyRecordsArePreservedButDoNotCompeteWithRevisedRoutes()
    {
        const string legacy="PolyCar_BestTime_999",current="PolyCar_v2_BestTime_999";
        bool hadOld=PlayerPrefs.HasKey(legacy),hadNew=PlayerPrefs.HasKey(current);
        float old=PlayerPrefs.GetFloat(legacy),fresh=PlayerPrefs.GetFloat(current);
        try
        {
            PlayerPrefs.SetFloat(legacy,12);PlayerPrefs.DeleteKey(current);
            Assert.That(SaveManager.GetBestTime(999),Is.Zero);
            SaveManager.RecordTime(999,65);
            Assert.That(SaveManager.GetBestTime(999),Is.EqualTo(65));
            Assert.That(PlayerPrefs.GetFloat(legacy),Is.EqualTo(12));
        }
        finally
        {
            if(hadOld)PlayerPrefs.SetFloat(legacy,old);else PlayerPrefs.DeleteKey(legacy);
            if(hadNew)PlayerPrefs.SetFloat(current,fresh);else PlayerPrefs.DeleteKey(current);
            PlayerPrefs.Save();
        }
    }
}
