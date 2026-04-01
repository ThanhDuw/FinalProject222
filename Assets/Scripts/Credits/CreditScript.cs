using UnityEngine;

public class CreditScript : MonoBehaviour
{
    public float scrollSpeed = 20f; // Speed at which the credits scroll

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
        rectTransorm.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);
        
    }
}
