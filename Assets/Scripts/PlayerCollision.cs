using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.Splines;

public class PlayerCollision : MonoBehaviour
{
    //External variables.
    public Vector2 hitboxSize = Vector2.one;
    public Vector2 hitboxOrigin = Vector2.zero;
    public Vector2 idleSize = Vector2.one;
    public Vector2 idleOrigin = Vector2.zero;
    public Vector2 position;
    public Vector2 externalVelocity = Vector2.zero;
    public Vector2 internalVelocity = Vector2.zero;
    public float rotation;
    public bool groundSnap = true;
    public bool detectionUp = false;
    public bool detectionDown = false;
    public bool detectionRight = false;
    public bool detectionLeft = false;
    public bool detectionCrouch = false;
    public Vector2 detectionNormal = Vector2.up;

    private Vector2 targetPosition;
    private Vector2 targetPositionDisplacement;
    private float targetRotationDisplacement;

    //Internal Units and Variables
    private Vector2 globalHitbox;
    private float smallestUnit;
    private float initialScanStepSize;
    private float initialStairStepSize;
    private float stairStepGap;
    private float slopeLimit;
    private float snapDistance;
    private float pushOutDistance;
    private float displacementOffset;
    private bool collision;
    private float splineZ;

    //Local and Global conversion vectors
    private Vector2 localX;
    private Vector2 localY;
    private Vector2 globalX;
    private Vector2 globalY;

    //Ground Layers
    private int layerGround;
    private int layerSemi;
    private int layerSemiChild;
    private int layerSpline;

    //Collider Arrays
    private Collider[] resultsCollided = new Collider[10];
    private Collider[] resultsGround = new Collider[5];
    private Collider[] resultsSemi = new Collider[5];
    private Collider[] resultsSemiChild = new Collider[5];
    private Collider[] resultsSpline = new Collider[1];
    private RaycastHit[] resultsRay = new RaycastHit[5];

    //Collision ID Lists
    private List<int> collidedIDs = new List<int>();
    private List<int> collidedChildIDs = new List<int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created.
    void Awake()
    {
        //External Components Set-Up
        position = transform.position;

        //Internal Units Set-Up
        smallestUnit = .01f;
        initialScanStepSize = .5f;
        initialStairStepSize = .25f;
        stairStepGap = .02f;
        slopeLimit = 2f;
        snapDistance = .5f;
        pushOutDistance = 1f;
        displacementOffset = .1f;

        //Layer Masks Set-Up
        layerGround = LayerMask.GetMask("Ground");
        layerSemi = LayerMask.GetMask("SemiSolid");
        layerSemiChild = LayerMask.GetMask("SemiSolidChild");
        layerSpline = LayerMask.GetMask("LevelSplineCollider");
    }

    //============================================================================================================================ >> Functions <<
    public Vector2 GlobalToLocal(Vector2 input)
    {
        Vector2 output = (input.x * localX) + (input.y * localY);
        return output;
    }

    public Vector2 LocalToGlobal(Vector2 input)
    {
        Vector2 output = (input.x * globalX) + (input.y * globalY);
        return output;
    }

    void IsCollided(Vector2 collisionSize, Vector2 collisionInputPosition)
    {
        //Clear Lists and Arrays
        Array.Clear(resultsSemi, 0, resultsSemi.Length);
        Array.Clear(resultsGround, 0, resultsGround.Length);
        Array.Clear(resultsCollided, 0, resultsGround.Length + resultsSemi.Length);
        collidedIDs.Clear();

        //Calculate Collision Position
        collision = false;
        collisionInputPosition += globalHitbox;
        Vector3 collisionSize3 = new Vector3(collisionSize.x, collisionSize.y, collisionSize.x);
        Vector3 collisionPosition = new Vector3(collisionInputPosition.x, collisionInputPosition.y, splineZ);

        #region Ground Collision
        //Ground Collision Check
        int countGround = Physics.OverlapBoxNonAlloc(collisionPosition, collisionSize3 / 2, resultsGround, Quaternion.Euler(0, 0, rotation), layerGround);

        //If there was a solid object detected, there is a collision.
        if (countGround > 0)
        {
            collision = true;
            for (int i = 0; i < resultsGround.Length; i++)
            {
                resultsCollided[i] = resultsGround[i];
            }
        }
        #endregion

        #region Semi-Solid Collision
        //Semi-Solid Collision Check
        int countSemi = Physics.OverlapBoxNonAlloc(collisionPosition, collisionSize3 / 2, resultsSemi, Quaternion.Euler(0, 0, rotation), layerSemi);

        //If there's a semi-solid ID that matches an ID in the semi-solid child list, there is a collision.
        for (int i = 0; i < countSemi; i++)
        {
            SemiSolid semiSolid = resultsSemi[i].GetComponent<SemiSolid>();
            if (collidedChildIDs.Contains(semiSolid.semiSolidID))
            {
                collision = true;
                resultsCollided[i + resultsGround.Length] = resultsSemi[i];
            }
        }
        #endregion
    }

    Vector2 SlopeRayCast(Vector2 rayInputPosition, Vector2 rayDirection)
    {
        Vector3 rayPosition = new Vector3(rayInputPosition.x, rayInputPosition.y, splineZ);
        //Debug.DrawRay(rayPosition, rayDirection, Color.red);

        #region Ground Collision
        //Ground Collision Check
        System.Array.Clear(resultsRay, 0, resultsRay.Length);

        int countGround = Physics.RaycastNonAlloc(new Ray(rayPosition, rayDirection), resultsRay, rayDirection.magnitude, layerGround);

        //If there was a solid object detected, there is a collision.
        if (countGround > 0)
        {
            return resultsRay[0].normal;
        }
        #endregion

        #region Semi-Solid Collision
        //Semi-Solid Collision Check
        System.Array.Clear(resultsRay, 0, resultsRay.Length);

        int countSemi = Physics.RaycastNonAlloc(new Ray(rayPosition, rayDirection), resultsRay, rayDirection.magnitude, layerSemi);

        //Create a list of IDs from semi-solids that are collided with.
        for (int i = 0; i < countSemi; i++)
        {
            SemiSolid semiSolid = resultsRay[i].collider.GetComponent<SemiSolid>();
            if (collidedChildIDs.Contains(semiSolid.semiSolidID))
            {
                return resultsRay[i].normal;
            }
        }
        #endregion

        return Vector2.zero;
    }

    void SemiSolidCheck()
    {
        #region Semi-Solid Filter
        System.Array.Clear(resultsSemi, 0, resultsSemi.Length);
        System.Array.Clear(resultsSemiChild, 0, resultsSemiChild.Length);
        collidedIDs.Clear();
        collidedChildIDs.Clear();

        Vector3 scaleOffset = new Vector3(hitboxSize.x, hitboxSize.y, hitboxSize.x);
        Vector2 collisionInputPosition = position + globalHitbox;
        Vector3 collisionPosition = new Vector3(collisionInputPosition.x, collisionInputPosition.y, splineZ);
        int countSemi = Physics.OverlapBoxNonAlloc(collisionPosition, scaleOffset / 2, resultsSemi, Quaternion.Euler(0, 0, rotation), layerSemi);
        int countSemiChild = Physics.OverlapBoxNonAlloc(collisionPosition, scaleOffset / 2, resultsSemiChild, Quaternion.Euler(0, 0, rotation), layerSemiChild);
        //Create a list of IDs from semi-solids that are collided with.
        for (int i = 0; i < countSemi; i++)
        {
            SemiSolid semiSolid = resultsSemi[i].GetComponent<SemiSolid>();
            collidedIDs.Add(semiSolid.semiSolidID);
        }
        //Create a list of IDs from semi-solid childs that are collided with.
        for (int i = 0; i < countSemiChild; i++)
        {
            SemiSolidChild semiSolidChild = resultsSemiChild[i].GetComponent<SemiSolidChild>();
            collidedChildIDs.Add(semiSolidChild.semiSolidID);
        }

        //Filter out semi-solid child IDs when parent has matching ID.
        if ((countSemi > 0) && (countSemiChild > 0))
        {
            for (int i = 0; i < countSemi; i++)
            {
                collidedChildIDs.Remove(collidedIDs[i]);
            }
        }
        #endregion
        //Debug.Log("List Parent: ");
        //for (int i = 0; i < collidedIDs.Count; i++)
        {
            //Debug.Log(collidedIDs[i]);
        }
        //Debug.Log("List Child: ");
        //for (int i = 0; i < collidedChildIDs.Count; i++)
        {
            //Debug.Log(collidedChildIDs[i]);
        }
    }

    void HorizontalSlopeSlide(Vector2 collisionPosition)
    {
        #region Floor Slope Check
        //Detect if there's a valid slope to slide on. If there is then slide.
        Vector2 slopeCheckSize = new Vector2(.1f, hitboxSize.x / 2 * slopeLimit);
        Vector2 slopeCheckPosition = collisionPosition + LocalToGlobal(new Vector2(0, -(hitboxSize.y + slopeCheckSize.y) / 2));

        IsCollided(slopeCheckSize, slopeCheckPosition);
        #endregion

        if (collision)
        {
            #region Slope Sliding
            IsCollided(hitboxSize, collisionPosition);

            //If the path forward is obstructed check if there is an available step that can be taken.
            if (collision)
            {
                float stairStep = 0;
                Vector2 stairStepCheck = Vector2.zero;

                for (int i = 0; i < 2; i++)
                {
                    if (i == 0)
                    {
                        stairStep = initialStairStepSize;
                    }
                    else
                    {
                        stairStep = -initialStairStepSize;
                    }

                    stairStepCheck = LocalToGlobal(new Vector2(0, stairStep));

                    IsCollided(hitboxSize, collisionPosition + stairStepCheck);

                    if (collision == false)
                    {
                        //If there's a possible valid free space, take it.
                        int stairStepTimesLooped = 0;
                        float stairStepSize = stairStep;
                        float previousStairStepSize;

                        while ((Mathf.Abs(stairStep) >= smallestUnit) && (stairStepTimesLooped < 100))
                        {
                            stairStepTimesLooped += 1;

                            previousStairStepSize = stairStepSize;
                            stairStepSize -= stairStep;
                            stairStepCheck = LocalToGlobal(new Vector2(0, stairStepSize));

                            IsCollided(hitboxSize, collisionPosition + stairStepCheck);

                            if (collision)
                            {
                                stairStep /= 2;
                                stairStepSize = previousStairStepSize;
                            }
                        }
                        position += LocalToGlobal(new Vector2(0, stairStepSize));
                        break;
                    }
                }
            }
            #endregion
        }
    }

    void VerticalSlopeSlide(Vector2 collisionPosition)
    {
        #region Floor Slope Check
        //Detect if there's a valid slope to slide on. If there is then slide.
        Vector2 slopeCheckSize = new Vector2(.1f, hitboxSize.x / 2 * slopeLimit);
        Vector2 slopeCheckPosition = collisionPosition + LocalToGlobal(new Vector2(0, Mathf.Sign(externalVelocity.y) * (hitboxSize.y + slopeCheckSize.y) / 2));

        IsCollided(slopeCheckSize, slopeCheckPosition);
        #endregion

        if (collision == false)
        {
            #region Slope Sliding
            IsCollided(hitboxSize, collisionPosition);

            //If the path forward is obstructed check if there is an available step that can be taken.
            if (collision)
            {
                float stairStep = 0;
                Vector2 stairStepCheck = Vector2.zero;

                for (int i = 0; i < 2; i++)
                {
                    if (i == 0)
                    {
                        stairStep = initialStairStepSize;
                    }
                    else
                    {
                        stairStep = -initialStairStepSize;
                    }

                    stairStepCheck = LocalToGlobal(new Vector2(stairStep, 0));

                    IsCollided(hitboxSize, collisionPosition + stairStepCheck);

                    if (collision == false)
                    {
                        //If there's a possible valid free space, take it.
                        int stairStepTimesLooped = 0;
                        float stairStepSize = stairStep;
                        float previousStairStepSize;

                        while ((Mathf.Abs(stairStep) >= smallestUnit) && (stairStepTimesLooped < 100))
                        {
                            stairStepTimesLooped += 1;

                            previousStairStepSize = stairStepSize;
                            stairStepSize -= stairStep;
                            stairStepCheck = LocalToGlobal(new Vector2(stairStepSize, 0));

                            IsCollided(hitboxSize, collisionPosition + stairStepCheck);

                            if (collision)
                            {
                                stairStep /= 2;
                                stairStepSize = previousStairStepSize;
                            }
                        }
                        position += LocalToGlobal(new Vector2(stairStepSize, 0));
                        break;
                    }
                }
            }
            #endregion
        }
    }

    // Update is called once per fixed frame.
    private void FixedUpdate()
    {
        //======================================================================================================================= >> Variable Setup <<
        externalVelocity = GlobalToLocal(externalVelocity);
        internalVelocity = GlobalToLocal(internalVelocity);
        globalHitbox = LocalToGlobal(hitboxOrigin);
        rotation = transform.eulerAngles.z;

        //Local and Global Vectors
        localX = new Vector2(Mathf.Cos(Mathf.Deg2Rad * rotation), -1 * Mathf.Sin(Mathf.Deg2Rad * rotation));
        localY = new Vector2(-1 * Mathf.Cos(Mathf.Deg2Rad * (rotation + 90)), Mathf.Sin(Mathf.Deg2Rad * (rotation + 90)));
        globalX = new Vector2(Mathf.Cos(Mathf.Deg2Rad * (-rotation)), -1 * Mathf.Sin(Mathf.Deg2Rad * (-rotation)));
        globalY = new Vector2(Mathf.Cos(Mathf.Deg2Rad * (-rotation - 90)), -1 * Mathf.Sin(Mathf.Deg2Rad * (-rotation - 90)));

        //Calculate New Velocity
        Vector2 distance = (externalVelocity) * Time.fixedDeltaTime;

        //Collision Setup.
        Vector2 distanceTraveled = Vector2.zero;
        Vector2 previousDistance = Vector2.zero;
        Vector2 globalDistance = Vector2.zero;
        Vector2 collisionPosition = Vector2.zero;




        //===================================================================================================================== >> Spline Detection <<

        #region Spline Detection
        SplineContainer spline;
        collisionPosition = position;

        Vector3 splineSize = new Vector3(hitboxSize.x, hitboxSize.y, hitboxSize.x);

        Array.Clear(resultsSpline, 0, resultsSpline.Length);
        int splineHit = Physics.OverlapBoxNonAlloc(collisionPosition, splineSize / 2, resultsSpline, Quaternion.Euler(0, 0, rotation), layerSpline);

        if (splineHit > 0)
        {
            Transform parent = resultsSpline[0].transform.parent;
            SplineContainer childSpline;
            for (int i = 0; i < parent.childCount; i++)
            {
                childSpline = parent.GetChild(i).GetComponent<SplineContainer>();
                if (childSpline != null)
                {
                    spline = childSpline;

                    float3 splinePosition = (float3)spline.transform.InverseTransformPoint(collisionPosition);
                    SplineUtility.GetNearestPoint(spline.Spline, splinePosition, out float3 closestPoint, out float t);

                    splineZ = ((Vector3)closestPoint).z;
                    break;
                }
            }
        }
        #endregion



        //========================================================================================================================= >> Displacement <<
        System.Array.Clear(resultsCollided, 0, resultsCollided.Length);
        targetPositionDisplacement = Vector2.zero;
        targetRotationDisplacement = 0;
        targetPosition = Vector2.zero;

        collisionPosition = position;
        Vector2 displacementSize = hitboxSize + (displacementOffset * Vector2.one);

        #region Displacement
        //Find all valid ground and semi-solid collisions then search for any with a displacement script.
        IsCollided(displacementSize, collisionPosition);

        for (int i = 0; i < resultsCollided.Length; i++)
        {
            if (resultsCollided[i] != null)
            {
                DisplacementScript displacementScript = resultsCollided[i].GetComponent<DisplacementScript>();

                if (displacementScript != null)
                {
                    targetPositionDisplacement = displacementScript.positionDisplacement;
                    targetRotationDisplacement = -displacementScript.rotationDisplacement;
                    targetPosition = displacementScript.position;
                    break;
                }
            }
        }

        Vector2 relativePosition = position - targetPosition;

        Vector2 rotationX = new Vector2(Mathf.Cos(Mathf.Deg2Rad * targetRotationDisplacement), -1 * Mathf.Sin(Mathf.Deg2Rad * targetRotationDisplacement));
        Vector2 rotationY = new Vector2(-1 * Mathf.Cos(Mathf.Deg2Rad * (targetRotationDisplacement + 90)), Mathf.Sin(Mathf.Deg2Rad * (targetRotationDisplacement + 90)));
        Vector2 rotationPosition = (relativePosition.x * rotationX) + (relativePosition.y * rotationY);

        position = rotationPosition + targetPosition + targetPositionDisplacement;
        #endregion



        //============================================================================================================================= >> Push Out <<
        collisionPosition = position;

        IsCollided(hitboxSize, collisionPosition);

        if (collision)
        {
            #region Up
            //Push Up Set-Up
            int pushOutTimesLooped = 0;
            float pushOutStep = snapDistance;
            float pushOutStepSize = 0;
            float previousPushOutStepSize;
            Vector2 pushOutStepCheck;
            Vector2 pushUpPosition;

            //Search for free space to start push out.
            while ((pushOutStep >= smallestUnit) && (pushOutTimesLooped < 100))
            {
                pushOutTimesLooped += 1;
                previousPushOutStepSize = pushOutStepSize;

                pushOutStepSize += pushOutStep;
                pushOutStepCheck = LocalToGlobal(new Vector2(0, pushOutStepSize));

                IsCollided(hitboxSize, collisionPosition + pushOutStepCheck);

                if (collision == false)
                {
                    break;
                }

                if (pushOutStepSize > pushOutDistance)
                {
                    pushOutStepSize = 0;
                    pushOutStep /= 2;
                }
            }

            //If there's free space, start from there or else the push out is invalid.
            if ((pushOutStep < smallestUnit) || (pushOutTimesLooped >= 100))
            {
                pushUpPosition = Vector2.one;
            }
            else
            {
                pushOutTimesLooped = 0;

                while ((pushOutStep >= smallestUnit) && (pushOutTimesLooped < 100))
                {
                    pushOutTimesLooped += 1;
                    previousPushOutStepSize = pushOutStepSize;

                    pushOutStepSize -= pushOutStep;
                    pushOutStepCheck = LocalToGlobal(new Vector2(0, pushOutStepSize));

                    IsCollided(hitboxSize, collisionPosition + pushOutStepCheck);

                    if (collision)
                    {
                        pushOutStepSize = previousPushOutStepSize;
                        pushOutStep /= 2;
                    }
                }

                pushUpPosition = new Vector2(0, pushOutStepSize);
            }
            #endregion

            #region Down
            //Push Down Set-Up
            pushOutTimesLooped = 0;
            pushOutStep = snapDistance;
            pushOutStepSize = 0;
            Vector2 pushDownPosition;

            //Search for free space to start push out.
            while ((pushOutStep >= smallestUnit) && (pushOutTimesLooped < 100))
            {
                pushOutTimesLooped += 1;
                previousPushOutStepSize = pushOutStepSize;

                pushOutStepSize += pushOutStep;
                pushOutStepCheck = LocalToGlobal(new Vector2(0, -pushOutStepSize));

                IsCollided(hitboxSize, collisionPosition + pushOutStepCheck);

                if (collision == false)
                {
                    break;
                }

                if (pushOutStepSize > pushOutDistance)
                {
                    pushOutStepSize = 0;
                    pushOutStep /= 2;
                }
            }

            //If there's free space, start from there or else the push out is invalid.
            if ((pushOutStep < smallestUnit) || (pushOutTimesLooped >= 100))
            {
                pushDownPosition = Vector2.one;
            }
            else
            {
                pushOutTimesLooped = 0;

                while ((pushOutStep >= smallestUnit) && (pushOutTimesLooped < 100))
                {
                    pushOutTimesLooped += 1;
                    previousPushOutStepSize = pushOutStepSize;

                    pushOutStepSize -= pushOutStep;
                    pushOutStepCheck = LocalToGlobal(new Vector2(0, -pushOutStepSize));

                    IsCollided(hitboxSize, collisionPosition + pushOutStepCheck);

                    if (collision)
                    {
                        pushOutStepSize = previousPushOutStepSize;
                        pushOutStep /= 2;
                    }
                }

                pushDownPosition = new Vector2(0, -pushOutStepSize);
            }
            #endregion

            #region Right
            //Push Right Set-Up
            pushOutTimesLooped = 0;
            pushOutStep = snapDistance;
            pushOutStepSize = 0;
            Vector2 pushRightPosition;

            //Search for free space to start push out.
            while ((pushOutStep >= smallestUnit) && (pushOutTimesLooped < 100))
            {
                pushOutTimesLooped += 1;
                previousPushOutStepSize = pushOutStepSize;

                pushOutStepSize += pushOutStep;
                pushOutStepCheck = LocalToGlobal(new Vector2(pushOutStepSize, 0));

                IsCollided(new Vector2(hitboxSize.x, hitboxSize.y - (2 * initialStairStepSize)), collisionPosition + pushOutStepCheck);

                if (collision == false)
                {
                    break;
                }

                if (pushOutStepSize > pushOutDistance)
                {
                    pushOutStepSize = 0;
                    pushOutStep /= 2;
                }
            }

            //If there's free space, start from there or else the push out is invalid.
            if ((pushOutStep < smallestUnit) || (pushOutTimesLooped >= 100))
            {
                pushRightPosition = Vector2.one;
            }
            else
            {
                pushOutTimesLooped = 0;

                while ((pushOutStep >= smallestUnit) && (pushOutTimesLooped < 100))
                {
                    pushOutTimesLooped += 1;
                    previousPushOutStepSize = pushOutStepSize;

                    pushOutStepSize -= pushOutStep;
                    pushOutStepCheck = LocalToGlobal(new Vector2(pushOutStepSize, 0));

                    IsCollided(new Vector2(hitboxSize.x, hitboxSize.y - (2 * initialStairStepSize)), collisionPosition + pushOutStepCheck);

                    if (collision)
                    {
                        pushOutStepSize = previousPushOutStepSize;
                        pushOutStep /= 2;
                    }
                }

                pushRightPosition = new Vector2(pushOutStepSize, 0);
            }
            #endregion

            #region Left
            //Push Left Set-Up
            pushOutTimesLooped = 0;
            pushOutStep = snapDistance;
            pushOutStepSize = 0;
            Vector2 pushLeftPosition;

            //Search for free space to start push out.
            while ((pushOutStep >= smallestUnit) && (pushOutTimesLooped < 100))
            {
                pushOutTimesLooped += 1;
                previousPushOutStepSize = pushOutStepSize;

                pushOutStepSize += pushOutStep;
                pushOutStepCheck = LocalToGlobal(new Vector2(-pushOutStepSize, 0));

                IsCollided(new Vector2(hitboxSize.x, hitboxSize.y - (2 * initialStairStepSize)), collisionPosition + pushOutStepCheck);

                if (collision == false)
                {
                    break;
                }

                if (pushOutStepSize > pushOutDistance)
                {
                    pushOutStepSize = 0;
                    pushOutStep /= 2;
                }
            }

            //If there's free space, start from there or else the push out is invalid.
            if ((pushOutStep < smallestUnit) || (pushOutTimesLooped >= 100))
            {
                pushLeftPosition = Vector2.one;
            }
            else
            {
                pushOutTimesLooped = 0;

                while ((pushOutStep >= smallestUnit) && (pushOutTimesLooped < 100))
                {
                    pushOutTimesLooped += 1;
                    previousPushOutStepSize = pushOutStepSize;

                    pushOutStepSize -= pushOutStep;
                    pushOutStepCheck = LocalToGlobal(new Vector2(-pushOutStepSize, 0));

                    IsCollided(new Vector2(hitboxSize.x, hitboxSize.y - (2 * initialStairStepSize)), collisionPosition + pushOutStepCheck);

                    if (collision)
                    {
                        pushOutStepSize = previousPushOutStepSize;
                        pushOutStep /= 2;
                    }
                }

                pushLeftPosition = new Vector2(-pushOutStepSize, 0);
            }
            #endregion

            #region Push Out Resolution
            //Find smallest push Position.
            Vector2 pushOutPosition = pushUpPosition;

            if (pushOutPosition.magnitude > pushDownPosition.magnitude)
            {
                pushOutPosition = pushDownPosition;
            }
            if (pushOutPosition.magnitude > pushLeftPosition.magnitude)
            {
                pushOutPosition = pushLeftPosition;
            }
            if (pushOutPosition.magnitude > pushRightPosition.magnitude)
            {
                pushOutPosition = pushRightPosition;
            }

            if (pushOutPosition.magnitude > pushOutDistance)
            {
                pushOutPosition = Vector2.zero;
            }

            position += LocalToGlobal(pushOutPosition);
            collisionPosition = position;
            HorizontalSlopeSlide(collisionPosition);
            #endregion
        }



        //================================================================================================================= >> Horizontal Collision <<
        float scanStepSize = initialScanStepSize;
        int scanTimesLooped = 0;

        while ((externalVelocity.x != 0) && (scanStepSize >= smallestUnit) && (scanTimesLooped < 100))
        {
            scanTimesLooped += 1;

            previousDistance.x = distanceTraveled.x;
            distanceTraveled.x += scanStepSize * Mathf.Sign(externalVelocity.x);
            globalDistance = LocalToGlobal(new Vector2(distanceTraveled.x, 0));

            #region Horizontal Ground Snapping
            //If the player is moving at high speeds in the x velocity make sure they stay connected to the ground.
            if (groundSnap && (Mathf.Round(externalVelocity.y * 100) / 100 <= 0))
            {
                collisionPosition = position;

                IsCollided(new Vector2(hitboxSize.x / 2, hitboxSize.y), collisionPosition + LocalToGlobal(new Vector2(0, -2 * smallestUnit)));

                if (collision == false)
                {
                    //Check if ground beneath is legible for ground snap before calculating ground snap.
                    if (Mathf.Abs(distanceTraveled.x) > Mathf.Abs(distance.x))
                    {
                        collisionPosition = position + LocalToGlobal(new Vector2(distance.x, 0));
                    }
                    else
                    {
                        collisionPosition = position + LocalToGlobal(new Vector2(distanceTraveled.x, 0));
                    }

                    IsCollided(hitboxSize, collisionPosition + LocalToGlobal(new Vector2(0, -snapDistance)));

                    //If there's ground underneat, snap to it.
                    if (collision)
                    {
                        int snapStepTimesLooped = 0;
                        float snapStep = snapDistance;
                        float snapStepSize = 0;
                        float previousSnapStepSize;
                        Vector2 snapStepCheck;

                        while ((snapStep >= smallestUnit) && (snapStepTimesLooped < 100))
                        {
                            snapStepTimesLooped += 1;

                            previousSnapStepSize = snapStepSize;
                            snapStepSize -= snapStep;
                            snapStepCheck = LocalToGlobal(new Vector2(0, snapStepSize));

                            IsCollided(hitboxSize, collisionPosition + snapStepCheck);

                            if (collision)
                            {
                                snapStepSize = previousSnapStepSize;
                                snapStep /= 2;
                            }
                        }
                        position += LocalToGlobal(new Vector2(0, snapStepSize));
                    }
                }
            }
            #endregion

            #region Slope Sliding Resolution
            //Find the position of the position of the next collision check before calculating the next stair step.
            if (Mathf.Abs(distanceTraveled.x) > Mathf.Abs(distance.x))
            {
                collisionPosition = position + LocalToGlobal(new Vector2(distance.x + (stairStepGap * Mathf.Sign(distance.x)), 0));
            }
            else
            {
                collisionPosition = position + LocalToGlobal(new Vector2(distanceTraveled.x + (stairStepGap * Mathf.Sign(distanceTraveled.x)), 0));
            }

            HorizontalSlopeSlide(collisionPosition);
            #endregion

            #region Collision Resolution
            //If a collision occured resolve it.
            collisionPosition = position + globalDistance;

            IsCollided(hitboxSize, collisionPosition);

            if (collision)
            {
                distanceTraveled.x = previousDistance.x;
                scanStepSize /= 2;
                if (scanStepSize < smallestUnit)
                {
                    position += LocalToGlobal(new Vector2(distanceTraveled.x, 0));
                    internalVelocity.x = 0;
                    externalVelocity.x = 0;
                    break;
                }
            }

            //If distance traveled exceeds the desired. distance.
            if (Mathf.Abs(distanceTraveled.x) > Mathf.Abs(distance.x))
            {
                position += LocalToGlobal(new Vector2(distance.x, 0));
                break;
            }
            #endregion
        }



        //=================================================================================================================== >> Vertical Collision <<
        scanStepSize = initialScanStepSize;
        scanTimesLooped = 0;

        while ((externalVelocity.y != 0) && (scanStepSize >= smallestUnit) && (scanTimesLooped < 100))
        {
            scanTimesLooped += 1;

            previousDistance.y = distanceTraveled.y;
            distanceTraveled.y += scanStepSize * Mathf.Sign(externalVelocity.y);
            globalDistance = LocalToGlobal(new Vector2(0, distanceTraveled.y));

            #region Slope Sliding Resolution
            //Find the position of the position of the next collision check before calculating the next stair step.
            if (Mathf.Abs(distanceTraveled.y) > Mathf.Abs(distance.y))
            {
                collisionPosition = position + LocalToGlobal(new Vector2(0, distance.y + (stairStepGap * Mathf.Sign(distance.y))));
            }
            else
            {
                collisionPosition = position + LocalToGlobal(new Vector2(0, distanceTraveled.y + (stairStepGap * Mathf.Sign(distanceTraveled.y))));
            }

            VerticalSlopeSlide(collisionPosition);
            #endregion

            #region Collision Resolution
            //If a collision occured, resolve it.
            collisionPosition = position + globalDistance;

            IsCollided(hitboxSize, collisionPosition);

            if (collision)
            {
                distanceTraveled.y = previousDistance.y;
                scanStepSize /= 2;
                if (scanStepSize < smallestUnit)
                {
                    position += LocalToGlobal(new Vector2(0, distanceTraveled.y));
                    internalVelocity.y = 0;
                    externalVelocity.y = 0;
                    break;
                }
            }

            //If distance traveled exceeds the desired. distance.
            if (Mathf.Abs(distanceTraveled.y) > Mathf.Abs(distance.y))
            {
                position += LocalToGlobal(new Vector2(0, distance.y));
                break;
            }
            #endregion
        }



        //====================================================================================================================== >> Ground Snapping <<
        #region Ground Snapping
        if (groundSnap && (Mathf.Round(externalVelocity.y * 100) / 100 <= 0))
        {
            collisionPosition = position;

            IsCollided(new Vector2(hitboxSize.x / 2, hitboxSize.y), collisionPosition + LocalToGlobal(new Vector2(0, -2 * smallestUnit)));

            if (collision == false)
            {
                IsCollided(hitboxSize, collisionPosition + LocalToGlobal(new Vector2(0, -snapDistance)));

                //If there's ground underneat, snap to it.
                if (collision)
                {
                    int snapStepTimesLooped = 0;
                    float snapStep = snapDistance;
                    float snapStepSize = 0;
                    float previousSnapStepSize;
                    Vector2 snapStepCheck;

                    while ((snapStep >= smallestUnit) && (snapStepTimesLooped < 100))
                    {
                        snapStepTimesLooped += 1;

                        previousSnapStepSize = snapStepSize;
                        snapStepSize -= snapStep;
                        snapStepCheck = LocalToGlobal(new Vector2(0, snapStepSize));

                        IsCollided(hitboxSize, collisionPosition + snapStepCheck);

                        if (collision)
                        {
                            snapStepSize = previousSnapStepSize;
                            snapStep /= 2;
                        }
                    }
                    position += LocalToGlobal(new Vector2(0, snapStepSize));
                }
            }
        }
        #endregion



        //======================================================================================================================== >> Apply Changes <<
        externalVelocity = LocalToGlobal(externalVelocity);
        internalVelocity = LocalToGlobal(internalVelocity);
        transform.position = new Vector3(position.x, position.y, splineZ);



        //============================================================================================================================== >> Sensors <<
        #region Directional Detection
        //Up Detection
        collisionPosition = position + LocalToGlobal(new Vector2(0, hitboxSize.y / 2));
        Vector2 collisionSize = new Vector2(hitboxSize.x, .1f);

        IsCollided(collisionSize, collisionPosition);

        if (collision)
        {
            detectionUp = true;
        }
        else
        {
            detectionUp = false;
        }

        //Down Detection
        collisionPosition = position + LocalToGlobal(new Vector2(0, -hitboxSize.y / 2));
        collisionSize = new Vector2(hitboxSize.x, .1f);

        IsCollided(collisionSize, collisionPosition);

        if (collision)
        {
            detectionDown = true;
        }
        else
        {
            detectionDown = false;
        }

        //Right Detection
        collisionPosition = position + LocalToGlobal(new Vector2(hitboxSize.x / 2, 0));
        collisionSize = new Vector2(.1f, hitboxSize.y / 2);

        IsCollided(collisionSize, collisionPosition);

        if (collision)
        {
            detectionRight = true;
        }
        else
        {
            detectionRight = false;
        }

        //Left Detection
        collisionPosition = position + LocalToGlobal(new Vector2(-hitboxSize.x / 2, 0));
        collisionSize = new Vector2(.1f, hitboxSize.y / 2);

        IsCollided(collisionSize, collisionPosition);

        if (collision)
        {
            detectionLeft = true;
        }
        else
        {
            detectionLeft = false;
        }
        #endregion

        #region Floor Normal Detection
        Vector2 normal1;
        Vector2 normal2;
        Vector2 detectedNormal;
        Vector2 collisionDirection = LocalToGlobal(new Vector2(0, -.1f));

        //Right Floor Ray
        collisionPosition = position + LocalToGlobal(new Vector2(hitboxSize.x / 2, -hitboxSize.y / 2));
        normal1 = GlobalToLocal(SlopeRayCast(collisionPosition, collisionDirection));

        //Left Floor Ray
        collisionPosition = position + LocalToGlobal(new Vector2(-hitboxSize.x / 2, -hitboxSize.y / 2));
        normal2 = GlobalToLocal(SlopeRayCast(collisionPosition, collisionDirection));

        //If both rays are valid average them. If one is valid average the valid ray.
        if ((normal1 != Vector2.zero) && (normal2 != Vector2.zero))
        {
            detectedNormal = (normal1 + normal2) / 2;
        }
        else if (normal1 != Vector2.zero)
        {
            detectedNormal = normal1;
        }
        else
        {
            detectedNormal = normal2;
        }

        //If there's not a valid normal value use the standard ground normal value.
        if (detectedNormal == Vector2.zero)
        {
            detectedNormal = Vector2.up;
        }

        detectionNormal = detectedNormal;
        #endregion

        #region Crouch Detection
        //Crouch Detection
        collisionPosition = position + idleOrigin;
        collisionSize = idleSize;

        IsCollided(collisionSize, collisionPosition);

        if (collision)
        {
            detectionCrouch = true;
        }
        else
        {
            detectionCrouch = false;
        }
        #endregion

        //================================================================================================== >> Previous Frame Semi-Solid Detection <<
        SemiSolidCheck();
    }
}