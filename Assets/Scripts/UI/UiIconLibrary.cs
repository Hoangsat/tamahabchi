using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UiIconEntry
{
    public string id = string.Empty;
    public Sprite sprite;
}

[CreateAssetMenu(fileName = "UiIconLibrary", menuName = "Tamahabchi/UI/Icon Library")]
public sealed class UiIconLibrary : ScriptableObject
{
    public List<UiIconEntry> entries = new List<UiIconEntry>();
}
