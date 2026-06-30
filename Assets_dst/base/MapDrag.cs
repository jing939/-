using UnityEngine;

public class MapDrag : MonoBehaviour
{
    public RectTransform map;

    Vector2 lastMouse;

    bool dragging;

    public float minX=-900;

    public float maxX=900;

    void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            dragging=true;

            lastMouse=
            Input.mousePosition;
        }

        if(Input.GetMouseButtonUp(1))
        dragging=false;

        if(dragging)
        {
            Vector2 delta=
            (Vector2)Input.mousePosition
            -lastMouse;

            map.anchoredPosition+=
            new Vector2(delta.x,0);

            float x=
            Mathf.Clamp(
            map.anchoredPosition.x,
            minX,
            maxX);

            map.anchoredPosition=
            new Vector2(x,0);

            lastMouse=
            Input.mousePosition;
        }
    }
}