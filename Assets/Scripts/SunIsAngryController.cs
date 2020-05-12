using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunIsAngryController : MonoBehaviour
{
    public ParticleSystem[] particleSystems;
    public AudioSource sunWarmupSound;

    public void Play()
    {
        foreach (var particles in particleSystems)
        {
            particles.Play();
        }
        // FIXME: This should depend on the user-constant defining the warmup time
        sunWarmupSound.Play();
    }
}
