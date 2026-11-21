using UnityEngine;

public class SemiSolidChild : MonoBehaviour
{
    private SemiSolid parent;
    public int semiSolidID;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parent = GetComponentInParent<SemiSolid>();
        semiSolidID = parent.semiSolidID;
    }
}
