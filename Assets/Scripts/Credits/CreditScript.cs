using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditScript : MonoBehaviour
{
    public float scrollSpeed = 50f; // Speed at which the credits scroll
    [Tooltip("Vị trí Y mà file chữ được coi là chạy xong để về Main Menu")]
    public float endPositionY = 2500f; 

    private RectTransform rectTransorm;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rectTransorm = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        // If the credits have scrolled past a certain point, reset to the starting position
        float currentSpeed = scrollSpeed;
        
        // Cho phép tua nhanh khi giữ chuột hoặc dấu cách
        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
        {
            currentSpeed *= 3f;
        }

        rectTransorm.anchoredPosition += new Vector2(0, currentSpeed * Time.deltaTime);
        
        // Khi chạy hết màn hình hoặc ấn ESC thì Load về MainMenu (Scene 0)
        if (rectTransorm.anchoredPosition.y >= endPositionY || Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
        }
    }
}
