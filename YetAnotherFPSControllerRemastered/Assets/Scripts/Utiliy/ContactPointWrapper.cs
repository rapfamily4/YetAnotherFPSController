using UnityEngine;

public struct ContactPointWrapper {
    // --- Internal members
    private ContactPoint m_contactPoint;
    private Vector3 m_relativeVelocity;

    // --- Public interface
    public Vector3 point => m_contactPoint.point;
    public Vector3 normal => m_contactPoint.normal;
    public Vector3 relativeVelocity => m_relativeVelocity;
    public Collider thisCollider => m_contactPoint.thisCollider;
    public Collider otherCollider => m_contactPoint.otherCollider;
    public float separation => m_contactPoint.separation;


    // --- Constructor
    public ContactPointWrapper(ContactPoint contactPoint, Vector3 relativeVelocity) {
        m_contactPoint = contactPoint;
        m_relativeVelocity = relativeVelocity;
    }
}
