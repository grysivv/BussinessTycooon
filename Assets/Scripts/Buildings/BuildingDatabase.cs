using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Tycoon/Building Database")]
public class BuildingDatabase : ScriptableObject
{
    [SerializeField] private List<BuildingData> _buildings = new List<BuildingData>();

    private Dictionary<string, BuildingData> _index;

    public BuildingData GetById(string id)
    {
        BuildIndexIfNeeded();

        if (_index.TryGetValue(id, out var building))
            return building;

        Debug.LogWarning($"[BuildingDatabase] Budynek '{id}' nie istnieje!");
        return null;
    }

    public List<BuildingData> GetByCategory(BuildingCategory category)
    {
        var result = new List<BuildingData>();
        foreach (var b in _buildings)
            if (b.category == category)
                result.Add(b);
        return result;
    }

    public IReadOnlyList<BuildingData> AllBuildings => _buildings;

    private void BuildIndexIfNeeded()
    {
        if (_index != null) return;

        _index = new Dictionary<string, BuildingData>();

        foreach (var building in _buildings)
        {
            if (building == null || string.IsNullOrEmpty(building.id))
                continue;

            if (_index.ContainsKey(building.id))
            {
                Debug.LogError($"[BuildingDatabase] Duplikat id: '{building.id}'!");
                continue;
            }

            _index[building.id] = building;
        }

        Debug.Log($"[BuildingDatabase] Zbudowano indeks: {_index.Count} budynków.");
    }

    public void RebuildIndex()
    {
        _index = null;
        BuildIndexIfNeeded();
    }

    private void OnValidate() => RebuildIndex();
}