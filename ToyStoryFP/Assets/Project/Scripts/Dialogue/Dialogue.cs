using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Sequence")]
public class Dialogue : ScriptableObject
{
    [TextArea(3, 5)] public string[] sentences;
    public string npcName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
