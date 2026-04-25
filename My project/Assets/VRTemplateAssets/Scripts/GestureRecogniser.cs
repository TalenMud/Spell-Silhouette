using System;
using System.Collections.Generic;
using UnityEngine;

/* =================================================================================
 * CLASS: GestureRecognizer
 * * DESCRIPTION:
 * A lightweight, highly optimized implementation of the $1 Unistroke Recognizer.
 * It compares a sequence of 2D points (drawn by a player) against a library of 
 * pre-recorded templates (we will provide this) to find the closest shape match, ignoring variations 
 * in drawing speed, size, and starting position.
-----------------------------------------------------
 * * INPUTS:
 * - rawPoints: List<Vector2> representing the path the player just drew.
 * - templates: List<SpellTemplate> containing your game's pre-recorded spells.
-----------------------------------------------
 * * OUTPUTS:
 * - RecognitionResult: A struct containing:
 * - GestureName (string): The name of the matched template.
 * - Confidence (float): A score from 0.0 to 1.0 (1.0 being a perfect match).
 * * FUNCTIONALITY:
 * 1. Resample: Converts the raw, uneven points into exactly 64 evenly spaced points.
 * 2. RotateToZero: Finds the indicative angle (from center to first point) and rotates the shape to 0 degrees.
 * 3. ScaleToSquare: Stretches/shrinks the shape uniformly to fit a standard 250x250 bounding box.
 * 4. TranslateToOrigin: Moves the shape so its centroid (center of mass) is exactly at (0,0).
 * 5. Golden Section Search: Compares the normalized drawing against templates at various angles to find the lowest distance (best match).
 * * EXAMPLE USE:
 * List<Vector2> playerDrawing = myGestureCapture.GetPoints();
 * List<SpellTemplate> spellLibrary = mySpellManager.GetTemplates();
 * * GestureRecognizer.RecognitionResult result = GestureRecognizer.Recognize(playerDrawing, spellLibrary);
 * * if (result.Confidence > 0.85f) {
 * CastSpell(result.GestureName);
 * }
 * ================================================================================= */

public static class GestureRecognizer
{
    public struct RecognitionResult
    {
        public string GestureName; // for matching and detection (casting correct spell)
        public float Confidence; // to evaluate whether to cast or not to cast - that is the question
    }

    // --- Constants for *The Algorithm* for le calculations---
    private const int NumResamplePoints = 64;
    private const float SquareSize = 250.0f;
    private const float Diagonal = 353.55f; // Math.Sqrt(250^2 + 250^2)
    private const float HalfDiagonal = 176.77f;
    
    // Golden Section Search variables for finding the best angle
    private static readonly float AngleRange = 45.0f * Mathf.Deg2Rad;
    private static readonly float AnglePrecision = 2.0f * Mathf.Deg2Rad;
    private static readonly float Phi = 0.5f * (-1.0f + Mathf.Sqrt(5.0f)); // Golden Ratio


// Execution:
    public static RecognitionResult Recognize(List<Vector2> rawPoints, List<SpellTemplate> templates)
    {
        // 1. Sanity check to prevent out-of-bounds errors on tiny accidental clicks
        if (rawPoints == null || rawPoints.Count < 10)
        {
            return new RecognitionResult { GestureName = "Too Short", Confidence = 0f };
        }

        // 2. Normalize the player's drawing
        Vector2[] candidate = Normalize(rawPoints.ToArray());

        float bestDistance = float.MaxValue;
        string bestMatchName = "No Match";

        // 3. Compare candidate against every template in the library
        foreach (SpellTemplate template in templates)
        {
            float distance = DistanceAtBestAngle(candidate, template, -AngleRange, AngleRange, AnglePrecision);
            
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatchName = template.Name;
            }
        }

        // 4. Convert the distance into a readable 0.0 to 1.0 confidence score
        float score = Mathf.Max(0f, 1.0f - (bestDistance / HalfDiagonal));

        return new RecognitionResult { GestureName = bestMatchName, Confidence = score };
    }

    // =======================================================================
    // NORMALIZATION PIPELINE
    // =======================================================================

    public static Vector2[] Normalize(Vector2[] points)
    {
        points = Resample(points, NumResamplePoints);
        points = RotateToZero(points);
        points = ScaleToSquare(points, SquareSize);
        points = TranslateToOrigin(points);
        return points;
    }

    private static Vector2[] Resample(Vector2[] points, int n)
    {
        float interval = PathLength(points) / (n - 1);
        float D = 0f;
        List<Vector2> newPoints = new List<Vector2> { points[0] };

        for (int i = 1; i < points.Length; i++)
        {
            float d = Vector2.Distance(points[i - 1], points[i]);
            if (D + d >= interval)
            {
                // Find the exact point along the line segment
                float t = (interval - D) / d;
                Vector2 q = Vector2.Lerp(points[i - 1], points[i], t);
                newPoints.Add(q);
                
                // Insert 'q' into the array to be the new starting point for the next segment
                List<Vector2> tempList = new List<Vector2>(points);
                tempList.Insert(i, q);
                points = tempList.ToArray();
                D = 0f;
            }
            else
            {
                D += d;
            }
        }

        // Catch rounding errors to ensure we always have exactly 'n' points
        while (newPoints.Count < n)
        {
            newPoints.Add(points[points.Length - 1]);
        }
        
        return newPoints.ToArray();
    }

    private static Vector2[] RotateToZero(Vector2[] points)
    {
        Vector2 c = Centroid(points);
        float angle = Mathf.Atan2(c.y - points[0].y, c.x - points[0].x);
        return RotateBy(points, -angle);
    }

    private static Vector2[] ScaleToSquare(Vector2[] points, float size)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        // Find bounding box
        foreach (Vector2 p in points)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        float width = maxX - minX;
        float height = maxY - minY;
        Vector2[] newPoints = new Vector2[points.Length];

        // Uniformly scale points to fit the reference square
        for (int i = 0; i < points.Length; i++)
        {
            float newX = (width == 0) ? points[i].x : points[i].x * (size / width);
            float newY = (height == 0) ? points[i].y : points[i].y * (size / height);
            newPoints[i] = new Vector2(newX, newY);
        }
        return newPoints;
    }

    private static Vector2[] TranslateToOrigin(Vector2[] points)
    {
        Vector2 c = Centroid(points);
        Vector2[] newPoints = new Vector2[points.Length];
        
        // Subtract centroid from all points to move center to (0,0)
        for (int i = 0; i < points.Length; i++)
        {
            newPoints[i] = new Vector2(points[i].x - c.x, points[i].y - c.y);
        }
        return newPoints;
    }

    // =======================================================================
    // MATH & COMPARISON HELPERS
    // =======================================================================

    private static float DistanceAtBestAngle(Vector2[] candidate, SpellTemplate template, float a, float b, float threshold)
    {
        // Implements Golden Section Search to efficiently find the angle that yields the lowest distance
        float x1 = Phi * a + (1.0f - Phi) * b;
        float f1 = DistanceAtAngle(candidate, template, x1);
        
        float x2 = (1.0f - Phi) * a + Phi * b;
        float f2 = DistanceAtAngle(candidate, template, x2);

        while (Mathf.Abs(b - a) > threshold)
        {
            if (f1 < f2)
            {
                b = x2;
                x2 = x1;
                f2 = f1;
                x1 = Phi * a + (1.0f - Phi) * b;
                f1 = DistanceAtAngle(candidate, template, x1);
            }
            else
            {
                a = x1;
                x1 = x2;
                f1 = f2;
                x2 = (1.0f - Phi) * a + Phi * b;
                f2 = DistanceAtAngle(candidate, template, x2);
            }
        }
        return Mathf.Min(f1, f2);
    }

    private static float DistanceAtAngle(Vector2[] points, SpellTemplate template, float angle)
    {
        Vector2[] rotatedPoints = RotateBy(points, angle);
        return PathDistance(rotatedPoints, template.NormalizedPoints);
    }

    private static Vector2[] RotateBy(Vector2[] points, float angle)
    {
        Vector2 c = Centroid(points);
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        Vector2[] newPoints = new Vector2[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            float dx = points[i].x - c.x;
            float dy = points[i].y - c.y;
            // 2D Rotation Matrix
            newPoints[i] = new Vector2(dx * cos - dy * sin + c.x, dx * sin + dy * cos + c.y);
        }
        return newPoints;
    }

    private static float PathDistance(Vector2[] a, Vector2[] b)
    {
        float d = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            d += Vector2.Distance(a[i], b[i]);
        }
        return d / a.Length; // Average distance per point
    }

    private static float PathLength(Vector2[] points)
    {
        float d = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            d += Vector2.Distance(points[i - 1], points[i]);
        }
        return d;
    }

    private static Vector2 Centroid(Vector2[] points)
    {
        Vector2 c = Vector2.zero;
        foreach (Vector2 p in points)
        {
            c += p;
        }
        return c / points.Length;
    }
}