using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PariclePoolItem : PoolItemMonobehaviour
{
    [SerializeField]private ParticleSystem myParticle;

    void Update()
    {
        if (!myParticle)
            Despawn();
    }

    public override void OnDespawn()
    {
        
    }

    public override void OnSpawn()
    {
        
    }
}
