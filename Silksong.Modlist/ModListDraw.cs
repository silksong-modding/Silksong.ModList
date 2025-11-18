using GlobalEnums;
using UnityEngine;

namespace Silksong.Modlist;

/// Class modified from github.com/hk-modding/api under MIT.
public class ModListDraw : MonoBehaviour
{
    private static readonly GUIStyle Style = new GUIStyle(GUIStyle.none);
    
    public string? drawString;
    private void Start()
    
    {
        Style.normal.textColor = Color.white;
        Style.alignment = TextAnchor.UpperLeft;
        Style.padding = new RectOffset(5, 5, 5, 5);
        Style.fontSize = 15;
    }
    
    public void OnGUI()
    {
        if (UIManager._instance == null)
        {
            return;
        }

        if (drawString != null && UIManager.instance.uiState is UIState.MAIN_MENU_HOME or UIState.PAUSED)
        {
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), drawString, Style);
        }
    }
}