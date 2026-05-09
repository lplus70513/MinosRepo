using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; 
#endif

public class QuitGame : MonoBehaviour
{
    public void Exit()
    {

#if UNITY_EDITOR
        // 如果是在编辑器中，则停止播放模式
        EditorApplication.isPlaying = false;
#else
        // 如果是打包后的程序，则正常退出应用程序
        Application.Quit();
#endif
    }
}