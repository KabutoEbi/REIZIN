using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour {
    public void StartEvent() {
        SceneManager.LoadScene("EventScene");
    }
}