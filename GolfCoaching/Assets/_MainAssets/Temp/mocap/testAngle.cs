using UnityEngine;

public class testAngle : MonoBehaviour
{
    [SerializeField] Transform from;
    [SerializeField] Transform to;
    [SerializeField] TMPro.TextMeshPro text;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 vector = to.position - from.position;
        vector.z = 0;
        //float angle = Vector3.SignedAngle(-Vector3.up, vector , Vector3.forward);
        float angle = Quaternion.FromToRotation(-Vector3.up, vector).eulerAngles.z;
        text.text = angle.ToString("0.00");
    }
}
