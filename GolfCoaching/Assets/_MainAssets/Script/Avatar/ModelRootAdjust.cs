using System.Collections;
using Unity.Hierarchy;
using UnityEngine;

public class ModelRootAdjust : MonoBehaviour
{
    [SerializeField] Transform StartFoot;
    [SerializeField] Transform TargetDeformPro;
    [SerializeField] Transform TargetDeformUser;
    float Anchorvalue;
    //float value;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);

        Anchorvalue = StartFoot.position.y;
        Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!! " + Anchorvalue);

        StartCoroutine(CoUpdate());

    }

    // Update is called once per frame
    IEnumerator CoUpdate()
    {
        while (true)
        {
            //Debug.Log($"{Anchorvalue} -{StartFoot.position.y}");
            
            Vector3 newPos = new Vector3(TargetDeformPro.position.x,
                TargetDeformPro.position.y + (Anchorvalue - StartFoot.position.y),
                TargetDeformPro.position.z);

            TargetDeformPro.position = newPos;
            TargetDeformUser.position = newPos;
            
            yield return null;
        }
    }
}
