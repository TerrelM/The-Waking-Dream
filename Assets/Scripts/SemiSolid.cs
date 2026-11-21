using UnityEngine;

public class SemiSolid : MonoBehaviour
{
    public int semiSolidID;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        semiSolidID = Random.Range(1, 1000000000);
    }
}
