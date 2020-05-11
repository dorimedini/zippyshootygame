using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Linq;
using Photon.Pun;

public class SunrayController : MonoBehaviour
{
    private enum SunrayState { IDLE, WARMUP, LOCKED, FIRED }

    public ExplosionController explosionCtrl;
    public PillarExtensionController pillarCtrl;
    public LineRenderer sunrayLine;
    public float initialLineWidth, maxLineWidth;
    public SunrayCageController cageCtrl;

    private Transform target;
    private string shooterId;
    private float timeActiveInCurrentState;
    private SunrayState currentState;
    private List<Vector3> explosionPositions;
    private int hitPillarId;

    void Start()
    {
        AdvanceToState(SunrayState.IDLE);
        HideRay();
    }

    public void GetAngryAt(Transform target, string shooterId)
    {
        // FIXME: Current design direction may trigger explosion in location not consistent with remote player's sunray target graphic. Is this bad?
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
                    ComputeExplosionPositions();
                    // Cage should disappear on it's own
                    break;
                }
                sunrayLine.transform.rotation = Quaternion.LookRotation(target.position);
                break;
            case SunrayState.LOCKED:
                if (timeActiveInCurrentState > UserDefinedConstants.sunrayFireDelay)
                {
                    AdvanceToState(SunrayState.FIRED);
                    TriggerExplosions();
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

    void ComputeExplosionPositions()
    {
        // We're in RPC context here (someone broadcasted a sun hit). We only broadcast an explosion if we're the shooter so otherwise we don't care
        if (shooterId != Tools.NullToEmptyString(PhotonNetwork.LocalPlayer.UserId))
            return;

        // Find all objects in the sunray. Should always hit a pillar, at least.
        RaycastHit[] hits = Physics.RaycastAll(Vector3.zero, target.position);
        if (hits.Length == 0)
        {
            Debug.LogError("Sunray found nothing to hit with explosion! Exploding in the sun...");
            explosionPositions = new List<Vector3> { Vector3.zero };
            return;
        }

        // Find the highest hit pillar
        PillarBehaviour hitPillar = null;
        Vector3 hitPillarPosition = Vector3.zero;
        foreach (var hit in hits)
        {
            // Ignore non-pillar objects
            PillarBehaviour pillar = hit.collider.GetComponent<PillarBehaviour>();
            if (pillar == null)
                continue;
            // Only take the highest hit pillar (with hitpoint closest to (0,0,0))
            if (hitPillar == null || hitPillarPosition.magnitude > hit.point.magnitude)
            {
                hitPillar = pillar;
                hitPillarPosition = hit.point;
            }
        }

        // Add the hit pillar location to the explosion list, and find all hit players above the pillar
        explosionPositions = new List<Vector3> { hitPillarPosition };
        hitPillarId = hitPillar.id;
        foreach (var hit in hits)
        {
            // Ignore the sun itself, and all objects below the height of the hit pillar
            if (hit.point.magnitude > hitPillarPosition.magnitude || hit.collider.gameObject.name == "Sun")
                continue;
            // If this is a network character closer to the sun than the top of the pillar, add the hitpoint.
            // However, if player is grounded, don't trigger an explosion; player should be taking damage from the explosion on the pillar
            bool isNetChar = hit.collider.GetComponent<NetworkCharacter>() != null;
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            bool grounded = rb != null && GeoPhysics.IsPlayerGrounded(rb);
            if (isNetChar && !grounded)
                explosionPositions.Add(hit.point);
        }
    }

    void TriggerExplosions()
    {
        // We're in RPC context here (someone broadcasted a sun hit). Only broadcast an explosion if we're the shooter
        if (shooterId != Tools.NullToEmptyString(PhotonNetwork.LocalPlayer.UserId))
            return;
        // Trigger explosions on all locations
        foreach (var position in explosionPositions)
            explosionCtrl.BroadcastExplosion(position, shooterId, true);
        // Trigger pillar extension on hit pillar
        pillarCtrl.BroadcastHitPillar(hitPillarId);
    }
}
