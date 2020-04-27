using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class RopeController : MonoBehaviour
{
    public LineRenderer rend;

    Transform ropeSourceTransform;
    Vector3 ropeTarget;

    // Update is called once per frame
    void Update()
    {
        // Each meter in world coordinates should have it's own segment.
        // Get the ceiling value of the distance between source and target, create that many segments.
        // Hopefully not too bad even with 12 ropes active at once...
        // No need to handle destruction of the rope, rope should be destroyed by RopeManager or something
        // TODO: The segment-thing. As is the rope is stretched all the way.
        OrientRope();
    }

    // Source of rope may change location but target is fixed. Source of rope should be a player hand, so
    // it has a transform anyway.
    public void Init(Transform source, Vector3 target)
    {
        ropeSourceTransform = source;
        ropeTarget = target;
        OrientRope();
    }

    void OrientRope()
    {
        transform.position = ropeSourceTransform.position;
        transform.LookAt(ropeTarget);
        rend.SetPosition(1, new Vector3(0, 0, (transform.position - ropeTarget).magnitude));
    }
}
