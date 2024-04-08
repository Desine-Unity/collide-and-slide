using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// https://youtu.be/YR6Q7dUz2uk?si=lxioSsyEBnbE1Ea5

// how to use:
//transform.position += collideAndSlide.Move(transform.position, moveVelocity, gravityPass: false);
//transform.position += collideAndSlide.Move(transform.position, gravityVelocity, gravityPass: true);
public class CollideAndSlide : MonoBehaviour
{
    [SerializeField] LayerMask moveCollideLayerMask = ~0;
    [SerializeField] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField, Min(0)] int maxClimbAngle = 60;

    private float skinWidth = 0.01f;
    private int maxBounces = 3;

    private CapsuleCollider capsuleCollider;


    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
    }



    public Vector3 Move(Vector3 position, Vector3 velocity, bool gravityPass, int currentBounce = 0, Vector3 initialVelocity = new())
    {
        if (currentBounce == 0) initialVelocity = velocity;


        if (currentBounce >= maxBounces) return Vector3.zero;

        if (CastSelf(position, velocity, out RaycastHit hit))
        {
            Vector3 snapToSurface = velocity.normalized * (hit.distance - skinWidth);
            Vector3 remaining = velocity - snapToSurface;
            float angleOfNormal = Vector3.Angle(Vector3.up, hit.normal);

            if (snapToSurface.magnitude <= skinWidth) snapToSurface = Vector3.zero;

            // nomal ground / slope
            if (angleOfNormal <= maxClimbAngle)
            {
                if (gravityPass) return snapToSurface;

                remaining = ProjectAndScale(remaining, hit.normal);
            }
            // wall or steep slope
            else
            {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(initialVelocity.x, 0, initialVelocity.z).normalized
                );

                if (IsGrounded() && !gravityPass)
                {
                    remaining = ProjectAndScale(
                        new Vector3(remaining.x, 0, remaining.z),
                        new Vector3(hit.normal.x, 0, hit.normal.z)
                    );
                    remaining *= scale;
                }
                else remaining = ProjectAndScale(remaining, hit.normal) * scale;
            }

            return snapToSurface + Move(position + snapToSurface, remaining, gravityPass, currentBounce + 1, initialVelocity);
        }

        return velocity;
    }
    private Vector3 ProjectAndScale(Vector3 vector, Vector3 plane)
    {
        float mag = vector.magnitude;
        vector = Vector3.ProjectOnPlane(vector, plane).normalized;
        vector *= mag;

        return vector;
    }
    public bool IsGrounded()
    {
        float groundedDistance = 0.1f;
        if (CastSelf(transform.position, Vector3.down * groundedDistance, out RaycastHit hit))        
            if (Vector3.Angle(Vector3.up, hit.normal) <= maxClimbAngle) return true;
        
        return false;
    }
    private bool CastSelf(Vector3 position, Vector3 direction, out RaycastHit hit)
    {
        Vector3 center = position + capsuleCollider.transform.localPosition + capsuleCollider.center;
        float radius = capsuleCollider.radius;
        float height = capsuleCollider.height;

        Vector3 bottom = center + Vector3.down * (height / 2 - radius - skinWidth);
        Vector3 top = center + Vector3.up * (height / 2 - radius - skinWidth);

        IEnumerable<RaycastHit> hits = Physics.CapsuleCastAll(top, bottom, radius - skinWidth, 
            direction.normalized, direction.magnitude + skinWidth, moveCollideLayerMask, QueryTriggerInteraction.Ignore)
            .Where(hit => hit.collider != capsuleCollider);
        bool didHit = hits.Count() > 0;

        float closestDist = didHit ? Enumerable.Min(hits.Select(hit => hit.distance)) : 0;
        hit = hits.Where(hit => hit.distance == closestDist).FirstOrDefault();
        return didHit;
    }

}
