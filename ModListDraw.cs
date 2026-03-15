using GlobalEnums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Silksong.Modlist;

/// Class modified from github.com/hk-modding/api under MIT.
public class ModListDraw : MonoBehaviour
{
    private static readonly GUIStyle Style = new GUIStyle(GUIStyle.none);

    private const int DesignFontSize = 15;
    
    internal string MenuDrawString = "";
    internal string PauseDrawString = "";

    private string DrawString => UIManager.instance.uiState switch
    {
        UIState.MAIN_MENU_HOME => MenuDrawString,
        UIState.PAUSED => PauseDrawString,
        _ => ""
    };
    
    private void Start()
    
    {
        Style.normal.textColor = Color.white;
        Style.alignment = TextAnchor.UpperLeft;
        Style.padding = new RectOffset(5, 5, 5, 5);
    }
    
    private static float HScale => Screen.width / 1920f;
    private static float VScale => Screen.height / 1080f;
    private static float ScaleFactor => HScale < VScale ? HScale : VScale;

    private static int ScaledFontSize => (int)(DesignFontSize * ScaleFactor);
    
    public void OnGUI()
    {
        if (UIManager._instance == null)
        {
            return;
        }

        if (UIManager.instance.uiState is UIState.MAIN_MENU_HOME or UIState.PAUSED)
        {
            Style.fontSize = ScaledFontSize;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), DrawString, Style);
        }
    }
}