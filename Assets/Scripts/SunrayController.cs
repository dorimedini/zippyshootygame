using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunrayController : MonoBehaviour
{
    public LineRenderer sunrayLine;
    public float initialLineWidth, maxLineWidth;

    private Transform target;
    private string shooterId;
    private float timeActive, targetingTime;

    public void GetAngryAt(Transform target, string shooterId, float targetingTime)
    {
        // FIXME: Current design direction may trigger explosion in location not consistent with remote player's sunray target graphic. Is this bad?

        this.target = target;
        this.shooterId = shooterId;
        this.targetingTime = targetingTime;
        timeActive = 0;

        // Stretch to maximal length, graphic should pass all the way to the edge
        sunrayLine.SetPosition(1, new Vector3(0, 0, UserDefinedConstants.sphereRadius + 10));

        // TODO: Start closing a cage of lines around the target to indicate he's being targeted by the sun.
        // TODO: Do this for targetingTime time

        // TODO: After that, stop changing location. We've locked a direction.
        // TODO: At this point, display a thin line that rapidly expands.
        
        // TODO: After expansion is done, trigger explosions (broadcast them if this is the shooter, we have a shooter ID).
        // TODO: Explosions should occur at the top of the hit pillar, and all players in between.
        // TODO: Also at this point maybe add a flash of light...?
        // TODO: Also at this point play a sound

        //sunrayLine.startWidth = initialLineWidth;
    }
}
