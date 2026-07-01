using UnityEngine;

public class CamTypeTest : MonoBehaviour
{
    bool orthographic = false;
    [SerializeField] Camera camFront;
    [SerializeField] Camera camSide;

    public void OnClickSwap()
    {
        orthographic = !orthographic;

        camFront.orthographic = orthographic;
        camSide.orthographic = orthographic;
    }
}
