using UnityEngine;

public class MapLine : MonoBehaviour
{
    public RectTransform startNode;

    public RectTransform endNode;

    RectTransform rect;

    void Start()
    {
        rect=
        GetComponent<RectTransform>();

        UpdateLine();
    }

    void UpdateLine()
    {
        Vector3 start=
        startNode.position;

        Vector3 end=
        endNode.position;

        Vector3 mid=
        (start+end)/2;

        rect.position=mid;

        float distance=
        Vector3.Distance(start,end);

        rect.sizeDelta=
        new Vector2(5,distance);

        float angle=
        Mathf.Atan2(
        end.y-start.y,
        end.x-start.x)
        *Mathf.Rad2Deg;

        rect.rotation=
        Quaternion.Euler(
        0,0,angle-90);
    }
}