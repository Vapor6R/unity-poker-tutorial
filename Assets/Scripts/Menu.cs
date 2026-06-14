using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public Button myButton;
    public GameObject targetObject;

    void Start()
    {
        myButton.onClick.AddListener(ToggleObject);
    }

    public void ToggleObject()
    {
        targetObject.SetActive(!targetObject.activeSelf);
    }
}