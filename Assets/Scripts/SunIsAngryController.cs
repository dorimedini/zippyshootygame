using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunIsAngryController : MonoBehaviour
{
    public ParticleSystem[] particleSystems;

    public void Play()
    {
        foreach (var particles in particleSystems)
        {
            particles.Play();
        }
    }
}
