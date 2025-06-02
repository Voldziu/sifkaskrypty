using UnityEngine;

[System.Serializable]
public class ResourcePrefabData
{
    [Header("Resource Info")]
    public string resourceName;
    public ResourceType resourceCategory;

    [Header("Specific Types")]
    public AdditionalResourceType additionalType;
    public StrategicResourceType strategicType;
    public LuxuryResourceType luxuryType;

    [Header("Prefabs")]
    public GameObject resourcePrefab;
    public Sprite resourceIcon;

    [Header("Description")]
    public string displayName;
    public string description;
}