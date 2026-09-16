using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FootstepSurfaceTrigger : MonoBehaviour
{
    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerFootsteps footsteps = other.GetComponent<PlayerFootsteps>();
            if (footsteps != null)
            {
                footsteps.SetIndoorZone(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerFootsteps footsteps = other.GetComponent<PlayerFootsteps>();
            if (footsteps != null)
            {
                footsteps.SetIndoorZone(false);
            }
        }
    }
}
