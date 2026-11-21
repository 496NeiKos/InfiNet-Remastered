using System;

[Serializable]
public class COCScoreData
{
    public bool       isPlayed;
    public Difficulty difficulty;
    public float      performancePts;
    public float      accuracyPts;
    public float      timePts;      // -1f = N/A (Normal difficulty)
    public float      totalPts;
    public float      elapsedSeconds;
}

public enum Difficulty { Normal, Hard }
