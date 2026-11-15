using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // ATTACK / DEFENSE DATA
    // INT values are actual values
    // FLOAT values are percentages or multipliers
    public int maxhp = 1000;
    public int hp = 1000;
    public float def = 0f;
    public int atk = 50;
    public float critChance = 0.01f;
    public float critMult = 1.5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
