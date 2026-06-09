using UnityEngine;

public enum FootstepSurfaceType
{
    Default,
    Grass,
    Wood,
    Metal,
    Dirt,
    Leaves,
    Concrete
}
public class FootstepSurfaceTypeDetection : MonoBehaviour
{
    public FootstepSurfaceType surfaceType = FootstepSurfaceType.Default;
}
