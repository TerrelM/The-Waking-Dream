using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

public class DisplacementScript : MonoBehaviour
{
    public Vector2 positionDisplacement;
    public float rotationDisplacement;
    public Vector2 position;
    private Vector2 previousPosition;
    private float rotation;
    private float previousRotation;

    void FixedUpdate()
    {
        position = transform.position;
        rotation = transform.eulerAngles.z;

        positionDisplacement = position - previousPosition;
        rotationDisplacement = rotation - previousRotation;

        previousPosition = position;
        previousRotation = rotation;
    }
}