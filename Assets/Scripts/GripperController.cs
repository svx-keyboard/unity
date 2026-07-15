using UnityEngine;

public class GripperController : MonoBehaviour
{
    [Header("References")]
    public JointController gripperJoint;

    public Transform gripperPoint;

    public Collider finger1;
    public Collider finger2;
    public Collider arm;

    [Header("Detection")]
    public float grabRadius = 0.03f;

    public LayerMask grabbableMask;

    [Header("Debug")]
    public bool drawSphere = true;

    GameObject candidateObject;
    Rigidbody heldBody;
    Collider heldCollider;

    bool holding = false;

    void Update()
    {
        if (!holding)
        {
            SearchCandidate();

            if (candidateObject != null &&
                gripperJoint.IsClosed)
            {
                Grab();
            }
        }
        else
        {
            if (!gripperJoint.IsClosed)
            {
                Release();
            }
        }
    }
    void LateUpdate()
    {
        if (holding && heldBody != null)
        {
            heldBody.transform.position = gripperPoint.position;
            heldBody.transform.rotation = gripperPoint.rotation;
        }
    }

    void SearchCandidate()
    {
        candidateObject = null;

        Collider[] hits =
            Physics.OverlapSphere(
                gripperPoint.position,
                grabRadius,
                grabbableMask);

        if (hits.Length > 0)
            candidateObject = hits[0].gameObject;
    }

    void Grab()
    {
        heldBody = candidateObject.GetComponent<Rigidbody>();

        heldCollider = candidateObject.GetComponent<Collider>();

        if (heldBody == null ||
            heldCollider == null)
            return;

        Physics.IgnoreCollision(finger1, heldCollider, true);
        Physics.IgnoreCollision(finger2, heldCollider, true);
        Physics.IgnoreCollision(arm, heldCollider, true);

        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;

        heldBody.isKinematic = true;

        heldBody.transform.SetParent(gripperPoint);

        heldBody.transform.localPosition = Vector3.zero;
        heldBody.transform.localRotation = Quaternion.identity;

        holding = true;
    }

    void Release()
    {
        heldBody.transform.SetParent(null);

        heldBody.isKinematic = false;

        Physics.IgnoreCollision(
            finger1,
            heldCollider,
            false);

        Physics.IgnoreCollision(
            finger2,
            heldCollider,
            false);

        holding = false;

        heldBody = null;
        heldCollider = null;
        candidateObject = null;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawSphere || gripperPoint == null)
            return;

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            gripperPoint.position,
            grabRadius);
    }

    public bool HasObject => holding;
}
