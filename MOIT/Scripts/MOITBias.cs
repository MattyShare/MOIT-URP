using UnityEngine;

[CreateAssetMenu(fileName = "MOITBias", menuName = "Scriptable Objects/MOITBias")]
public class MOITBias : ScriptableObject
{
    // see supplementary document from the original paper : https://momentsingraphics.de/I3D2018.html ; Table 1

    [Header("Power Moments")]
    [Tooltip("Recommended : 6*10-5")]
    public float Moments4Half = 0.00006f;
    [Tooltip("Recommended : 5*10-7")]
    public float Moments4Single = 0.0000005f;
    [Tooltip("Recommended : 6*10-4")]
    public float Moments6Half = 0.0006f;
    [Tooltip("Recommended : 5*10-6")]
    public float Moments6Single = 0.000005f;
    [Tooltip("Recommended : 2.5*10-3")]
    public float Moments8Half = 0.0025f;
    [Tooltip("Recommended : 5*10-5")]
    public float Moments8Single = 0.00005f;

    [Header("Trigonometric Moments")]
    [Tooltip("(4 moments) Recommended : 4*10-4")]
    public float Trigonometric2Half = 0.0004f;
    [Tooltip("(4 moments) Recommended : 4*10-7")]
    public float Trigonometric2Single = 0.0000004f;
    [Tooltip("(6 moments) Recommended : 6.5*10-4")]
    public float Trigonometric3Half = 0.00065f;
    [Tooltip("(6 moments) Recommended : 8*10-7")]
    public float Trigonometric3Single = 0.0000008f;
    [Tooltip("(8 moments) Recommended : 8.5*10-4")]
    public float Trigonometric4Half = 0.00085f;
    [Tooltip("(8 moments) Recommended : 1.5*10-6")]
    public float Trigonometric4Single = 0.0000015f;
}
