using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ExploreManager : MonoBehaviour
{
    public GameObject nodePrefab;

    public RectTransform nodesParent;
    public RectTransform linesParent;

    public RectTransform playerIcon;

    public TextMeshProUGUI countText;
    public TextMeshProUGUI resultText;

    public GameObject entryPanel;
    public GameObject dicePanel;
    public GameObject resultPanel;

    public TextMeshProUGUI entryText;
    public TextMeshProUGUI diceText;
    public TextMeshProUGUI resultUIText;

    public int minLayer = 5;
    public int maxLayer = 8;

    List<List<Node>> layers =
    new List<List<Node>>();

    Node currentNode;
    Node selectedNode;

    int resource;
    int food;
    int record;

    void Start()
    {
        entryPanel.SetActive(false);
        dicePanel.SetActive(false);
        resultPanel.SetActive(false);

        GenerateMap();

        currentNode=layers[0][0];

        playerIcon.anchoredPosition=
        currentNode.GetComponent
        <RectTransform>()
        .anchoredPosition;

        UpdateNodeStates();

        UpdateUI();
    }

    void GenerateMap()
    {
        int layerCount=
        Random.Range(
        minLayer,
        maxLayer+1);

        float xGap=350f;
        float yGap=180f;

        for(int i=0;i<layerCount;i++)
        {
            layers.Add(
            new List<Node>());

            int nodeCount=
            i==0 ?
            1 :
            Random.Range(1,4);

            for(int j=0;j<nodeCount;j++)
            {
                GameObject obj=
                Instantiate(
                nodePrefab,
                nodesParent);

                RectTransform rt=
                obj.GetComponent
                <RectTransform>();

                float yPos=
                (j-(nodeCount-1)/2f)
                *yGap;

                rt.anchoredPosition=
                new Vector2(
                i*xGap,
                yPos);

                Node node=
                obj.GetComponent<Node>();

                node.manager=this;

                node.SetRandomType();

                layers[i].Add(node);
            }
        }

        ConnectNodes();

        DrawLines();
    }

    void ConnectNodes()
    {
        for(int i=0;
        i<layers.Count-1;
        i++)
        {
            var current=
            layers[i];

            var next=
            layers[i+1];

            bool topStart=
            Random.value>0.5f;

            for(int j=0;
            j<current.Count;
            j++)
            {
                Node node=
                current[j];

                int connectCount=
                Mathf.Min(
                Random.Range(1,3),
                next.Count);

                List<int> used=
                new List<int>();

                int start=
                topStart ? 0 :
                next.Count-1;

                int dir=
                topStart ? 1 : -1;

                int index=start;

                for(int k=0;
                k<connectCount;
                k++)
                {
                    if(index<0)
                    index=0;

                    if(index>=next.Count)
                    index=
                    next.Count-1;

                    Node target=
                    next[index];

                    if(!node.connectedNodes
                    .Contains(target))
                    {
                        node.connectedNodes
                        .Add(target);
                    }

                    if(!target.connectedNodes
                    .Contains(node))
                    {
                        target.connectedNodes
                        .Add(node);
                    }

                    index+=dir;
                }

                topStart=!topStart;
            }
        }
    }

    void DrawLines()
    {
        foreach(List<Node> layer
        in layers)
        {
            foreach(Node node
            in layer)
            {
                foreach(Node next
                in node.connectedNodes)
                {
                    if(next.layerIndex>
                    node.layerIndex)
                    {
                        CreateLine(node,next);
                    }
                }
            }
        }
    }

    void CreateLine(
    Node a,
    Node b)
    {
        GameObject line=
        new GameObject("Line");

        line.transform.SetParent(
        linesParent);

        LineRenderer lr=
        line.AddComponent
        <LineRenderer>();

        lr.startWidth=5f;
        lr.endWidth=5f;

        lr.positionCount=2;

        lr.useWorldSpace=false;

        lr.material=
        new Material(
        Shader.Find(
        "Sprites/Default"));

        lr.startColor=
        Color.white;

        lr.endColor=
        Color.white;

        Vector3 posA=
        a.GetComponent
        <RectTransform>()
        .anchoredPosition;

        Vector3 posB=
        b.GetComponent
        <RectTransform>()
        .anchoredPosition;

        lr.SetPosition(0,posA);
        lr.SetPosition(1,posB);
    }

    public void SelectNode(
    GameObject obj)
    {
        Node node=
        obj.GetComponent<Node>();

        if(node==currentNode)
        return;

        if(!currentNode
        .connectedNodes
        .Contains(node))
        return;

        selectedNode=node;

        entryPanel.SetActive(true);

        entryText.text=
        "탐색을 진행하시겠습니까?";
    }

    public void EnterNode()
    {
        entryPanel.SetActive(false);

        dicePanel.SetActive(true);

        RollDice();
    }

    public void CancelNode()
    {
        entryPanel.SetActive(false);
    }

    void RollDice()
    {
        int d1=
        Random.Range(1,7);

        int d2=
        Random.Range(1,7);

        diceText.text=
        "주사위 : "
        +d1+" / "+d2;

        StartCoroutine(
        DiceResult(d1,d2));
    }

    IEnumerator DiceResult(
    int d1,
    int d2)
    {
        yield return
        new WaitForSeconds(1.5f);

        dicePanel.SetActive(false);

        resultPanel.SetActive(true);

        currentNode=
        selectedNode;

        currentNode.Visit();

        if(d1==d2 || d1+d2>=8)
        {
            int gain=
            Random.Range(1,4);

            resource+=gain;

            resultUIText.text=
            "성공!\n자재 +"
            +gain;
        }
        else
        {
            if(Random.value>0.5f)
            {
                int loss=
                Random.Range(1,3);

                resource=
                Mathf.Max(
                0,
                resource-loss);

                resultUIText.text=
                "실패!\n자재 -"
                +loss;
            }
            else
            {
                resultUIText.text=
                "실패!\n피해 발생";
            }
        }

        UpdateNodeStates();

        UpdateUI();

        StartCoroutine(
        MoveIcon(
        currentNode
        .GetComponent
        <RectTransform>()
        .anchoredPosition));
    }

    public void CloseResult()
    {
        resultPanel.SetActive(false);
    }

    IEnumerator MoveIcon(
    Vector2 target)
    {
        Vector2 start=
        playerIcon.anchoredPosition;

        float t=0;

        while(t<1)
        {
            t+=Time.deltaTime*3f;

            playerIcon.anchoredPosition=
            Vector2.Lerp(
            start,
            target,
            t);

            yield return null;
        }
    }

    void UpdateNodeStates()
    {
        foreach(var layer
        in layers)
        {
            foreach(var node
            in layer)
            {
                node.SetLocked(true);
            }
        }

        foreach(Node next
        in currentNode.connectedNodes)
        {
            next.SetLocked(false);
        }
    }

    void UpdateUI()
    {
        countText.text=
        "자재 : "+resource+
        "\n식량 : "+food+
        "\n기록 : "+record;
    }
}