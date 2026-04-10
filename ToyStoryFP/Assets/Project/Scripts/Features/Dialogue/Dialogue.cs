using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Sequence")]
public class Dialogue : ScriptableObject
{
    [TextArea(3, 5)] public string[] sentences = System.Array.Empty<string>();
    public string npcName;
}
