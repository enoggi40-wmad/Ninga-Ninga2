using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToEndScene : MonoBehaviour
{
    public string playerTag = "Player";
    public string endSceneName = "End";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        SceneManager.LoadScene(endSceneName);
    }
}