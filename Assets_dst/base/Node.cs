using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Node : MonoBehaviour
{
    public ExploreManager manager;

    public int layerIndex;

    public List<Node> connectedNodes=
    new List<Node>();

    public bool visited;

    Button btn;

    void Awake()
    {
        btn=
        GetComponent<Button>();

        btn.onClick.AddListener(
        Click);
    }

    void Click()
    {
        btn.onClick.AddListener(()=>{
            FindAnyObjectByType<ExploreManager>()
            .SelectNode(gameObject);
        });
    }

    public void Visit()
    {
        visited=true;
    }

    public void SetLocked(
    bool locked)
    {
        btn.interactable=
        !locked;
    }

    public void SetRandomType()
    {

    }
}