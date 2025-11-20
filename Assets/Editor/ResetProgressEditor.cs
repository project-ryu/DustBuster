using UnityEngine;
using UnityEditor;

public class ResetProgressEditor
{
    [MenuItem("Tools/Reset All Progress")]
    static void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("ALL PLAYER PROGRESS RESET!");
    }
}