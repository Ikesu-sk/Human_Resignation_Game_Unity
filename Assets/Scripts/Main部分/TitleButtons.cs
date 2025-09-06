using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleBtton : MonoBehaviour
{
    public void StartBtn()
    {
        SceneManager.LoadScene("Main");
    }
}
