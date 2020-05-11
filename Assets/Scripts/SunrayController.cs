using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunrayController : MonoBehaviour
{
    public LineRenderer sunrayLine;
    public float initialLineWidth, maxLineWidth;
    public SunrayCageController cageCtrl;

    private Transform target;
    private string shooterId;
    private float timeActiveInCurrentState;

    private enum SunrayState { IDLE, WARMUP, LOCKED, FIRED }

    private SunrayState currentState;

    void Start()
    {
        AdvanceToState(SunrayState.IDLE);
        HideRay();
    }

    public void GetAngryAt(Transform target, string shooterId)
    {
        // FIXME: Current design direction may trigger explosion in location not consistent with remote player's sunray target graphic. Is this bad?
        // FIXME: Current design doesn't damage the shooter with explosions, even if shooter is targeted
        if (currentState != SunrayState.IDLE)
        {
            // Do nothing if sun is already firing a sunray
            return;
        }

        this.target = target;
        this.shooterId = shooterId;
        InitRay();
        AdvanceToState(SunrayState.WARMUP);

        // Activate cage lines. Give it a little extra time, the cage disappears immediately after the time given
        cageCtrl.CageForDuration(target, UserDefinedConstants.sunrayWarningTime + 0.05f);
    }

    void Update()
    {
        timeActiveInCurrentState += Time.deltaTime;
        switch (currentState)
        {
            case SunrayState.IDLE:
                break;
            case SunrayState.WARMUP:
                if (timeActiveInCurrentState > UserDefinedConstants.sunrayWarningTime)
                {
                    AdvanceToState(SunrayState.LOCKED);
                    // TODO: On a separate thread, decide on explosion locations
                    break;
                }
                sunrayLine.transform.rotation = Quaternion.LookRotation(target.position);
                break;
            case SunrayState.LOCKED:
                if (timeActiveInCurrentState > UserDefinedConstants.sunrayFireDelay)
                {
                    AdvanceToState(SunrayState.FIRED);
                    // TODO: Trigger explosions (broadcast them if this is the shooter, we have a shooter ID).
                    // TODO: Explosions should occur at the top of the hit pillar, and all players in between.
                    // TODO: Also at this point maybe add a flash of light...?
                    // TODO: Also at this point play a sound
                    break;
                }
                SetRayWidth(Mathf.Lerp(initialLineWidth, maxLineWidth, timeActiveInCurrentState / UserDefinedConstants.sunrayFireDelay));
                break;
            case SunrayState.FIRED:
                // Give, say, 1 second decay. It's just a graphic, no issue
                if (timeActiveInCurrentState > 1)
                {
                    AdvanceToState(SunrayState.IDLE);
                    HideRay();
                    break;
                }
                SetRayWidth(Mathf.Lerp(maxLineWidth, 0, timeActiveInCurrentState));
                break;
        }
    }

    void InitRay()
    {
        SetRayWidth(0);
        sunrayLine.SetPosition(1, new Vector3(0, 0, UserDefinedConstants.sphereRadius + 10));
        sunrayLine.transform.rotation = Quaternion.LookRotation(target.position);
    }

    void HideRay()
    {
        sunrayLine.SetPosition(1, Vector3.zero);
    }

    void AdvanceToState(SunrayState state)
    {
        timeActiveInCurrentState = 0;
        currentState = state;
    }

    void SetRayWidth(float width)
    {
        sunrayLine.endWidth = sunrayLine.startWidth = width;
    }
}
