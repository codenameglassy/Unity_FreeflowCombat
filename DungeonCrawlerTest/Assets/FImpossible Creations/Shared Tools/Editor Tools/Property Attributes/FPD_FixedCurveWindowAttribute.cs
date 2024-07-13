using UnityEngine;

public class FPD_FixedCurveWindowAttribute : PropertyAttribute
{
    public float StartTime;
    public float EndTime;
    public float StartValue;
    public float EndValue;
    public Color Color;

    public FPD_FixedCurveWindowAttribute(float startTime = 0f, float startValue = 0f, float endTime = 1f, float endValue = 1f, float r = 0f, float g = 1f, float b = 1f, float a = 1f)
    {
        StartTime = startTime;
        StartValue = startValue;
        EndTime = endTime;
        EndValue = endValue;
        Color = new Color(r, g, b, a);
    }

}
