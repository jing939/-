using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public int food=50;

    public int material=30;

    public int medicine=10;

    void Awake()
    {
        if(instance==null)
        {
            instance=this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}