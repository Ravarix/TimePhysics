using UnityEngine;


public class GUICrosshair : MonoBehaviour
{
    public Texture2D CrosshairTex;
    private Vector2 _windowSize; //More like "last known window size".
    private Rect _crosshairRect;

    private void Start()
    {
        if(CrosshairTex == null)
            CrosshairTex = new Texture2D(2, 2);
        _windowSize = new Vector2(Screen.width, Screen.height);
        CalculateRect();
    }

    private void Update()
    {
        if (!Mathf.Approximately(_windowSize.x, Screen.width) || !Mathf.Approximately(_windowSize.y, Screen.height))
            CalculateRect();
    }

    private void CalculateRect()
    {
        _windowSize = new Vector2(Screen.width, Screen.height);
        _crosshairRect = new Rect( (_windowSize.x - CrosshairTex.width)/2.0f,
            (_windowSize.y - CrosshairTex.height)/2.0f,
            CrosshairTex.width, CrosshairTex.height);
    }

    private void OnGUI()
    {
        GUI.DrawTexture(_crosshairRect, CrosshairTex);
    }
}