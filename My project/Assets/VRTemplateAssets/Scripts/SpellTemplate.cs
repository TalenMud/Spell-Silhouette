using System.Collections.Generic;
using UnityEngine;

// Holds spell info - a very simple small class
public class SpellTemplate
{
    public string Name;
    public Vector2[] NormalizedPoints;

    public SpellTemplate(string name, List<Vector2> rawPoints)
    {
        Name = name;
        NormalizedPoints = GestureRecogniser.Normalize(rawPoints.ToArray());
    }
}
