using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Tools
{
    public static long IntPow(long x, long e)
    {
        if (e == 0)
            return x == 0 ? 0 : 1;
        if (e == 1)
            return x;
        return IntPow(x * x, e / 2) * (e % 2 == 1 ? x : 1);
    }

    public static float FloatPow(float x, long e)
    {
        if (e == 0)
            return x == 0 ? 0 : 1;
        if (e == 1)
            return x;
        return FloatPow(x * x, e / 2) * (e % 2 == 1 ? x : 1);
    }

    // This returns true if the ADDITIVE DIFFERENCE is at most epsilon
    public static bool NearlyEqual(double a, double b, double epsilon)
    {
        double absA = System.Math.Abs(a);
        double absB = System.Math.Abs(b);
        double diff = System.Math.Abs(a - b);

        if (a.Equals(b))
        { // shortcut, handles infinities
            return true;
        }
        /*
        else if (a == 0 || b == 0 || absA + absB < 2 * epsilon)
        {
            // a or b is zero or both are extremely close to it
            // relative error is less meaningful here
            return diff < epsilon;
        }
        else
        { // use relative error
            return diff / (absA + absB) < epsilon;
        }
        */
        return diff < epsilon;
    }

    public static bool NearlyEqual(Vector3 a, Vector3 b, double epsilon)
    {
        return NearlyEqual((a - b).magnitude, 0, epsilon);
    }


}
