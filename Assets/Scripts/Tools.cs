﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Tools
{
    private static System.Random random = new System.Random();
    private static string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateRandomString(int length)
    {
        return new string(Enumerable.Repeat(charset, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }

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
        double diff = System.Math.Abs(a - b);
        if (a.Equals(b))
        { // shortcut, handles infinities
            return true;
        }
        return diff < epsilon;
    }

    public static bool NearlyEqual(Vector3 a, Vector3 b, double epsilon)
    {
        return NearlyEqual((a - b).magnitude, 0, epsilon);
    }

    public static string ToString<K,T>(Dictionary<K,T> dict)
    {
        List<string> kvps = new List<string>();
        foreach (var kvp in dict)
            kvps.Add("(" + kvp.Key.ToString() + ":" + kvp.Value.ToString() + ")");
        return "[" + string.Join(",", kvps) + "]";
    }

    public static string NullToEmptyString(string s) { return s == null ? "" : s; }

    public static long Timestamp() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

    public static class Geometry
    {
        public static Vector3 RandomDirectionOnPlane(Vector3 normal)
        {
            Vector3 randomPoint;
            do
            {
                randomPoint = Vector3.Cross(UnityEngine.Random.insideUnitSphere, normal);
            } while (randomPoint == Vector3.zero);
            return randomPoint.normalized;
        }

        public static Quaternion UpRotation(Vector3 up)
        {
            return Quaternion.LookRotation(RandomDirectionOnPlane(up), up);
        }
    }

}
