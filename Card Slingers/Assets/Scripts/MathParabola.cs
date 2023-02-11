using UnityEngine;

public class MathParabola
{
    public static Vector3 Parabola(Vector3 start, Vector3 end, float height, float time)
    {
        System.Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

        var midPoint = Vector3.Lerp(start, end, time);

        return new Vector3(midPoint.x, f(time) + Mathf.Lerp(start.y, end.y, time), midPoint.z);
    }

    public static Vector3 Parabola(Vector3 start, Vector3 end, float time)
    {
        float height = Vector3.Distance(start, end) * 0.25f;

        System.Func<float, float> f = x => -4 * height * x * x + 4 * height * x;

        var midPoint = Vector3.Lerp(start, end, time);

        return new Vector3(midPoint.x, f(time) + Mathf.Lerp(start.y, end.y, time), midPoint.z);
    }
}
