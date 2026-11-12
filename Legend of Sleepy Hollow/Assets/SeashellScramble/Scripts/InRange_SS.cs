using UnityEngine;

public class InRange_SS : MonoBehaviour
{
    Renderer[] _targetRenderers;

    void Awake()
    {
        // TODO Slice 3.1: cache every child renderer and begin unselected.
        _targetRenderers = GetComponentsInChildren<Renderer>();
        
        SetInRange(false);
    }

    public void SetInRange(bool isInRange)
    {
        // TODO Slice 3.2: set _Highlight_Enabled on each material that supports it. </> end of Slice 3
        foreach (var renderer in _targetRenderers)
            renderer.material.SetFloat("_Highlight_Enabled", isInRange ? 1f : 0f);
    }
}
