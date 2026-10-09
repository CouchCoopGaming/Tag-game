using System.IO;
using NUnit.Framework;
using Tag.Level;
using UnityEngine;

/// <summary>
/// Edit Mode. Resolves every district placement through Resources, which is the
/// player-build path. AssetDatabase is not consulted.
/// </summary>
public class WorldPropTableTests
{
    [Test]
    public void DistrictPlacementsResolveOutsideTheEditorPath()
    {
        string bootstrap = File.ReadAllText("Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
        Assert.IsFalse(
            bootstrap.Contains("UnityEditor.AssetDatabase"),
            "district props still load through the editor asset database");

        WorldPropTable table = WorldPropTable.Load();
        Assert.IsNotNull(table, "Resources/World/WorldPropTable is missing. A player build would spawn nothing.");

        MegaParkWorldDistrict.Place[] places = MegaParkWorldDistrict.AllPlaces();
        Assert.Greater(places.Length, 0);
        for (int i = 0; i < places.Length; i++)
        {
            GameObject prefab = table.Find(places[i].Prefab);
            Assert.IsNotNull(prefab, "player path missed " + places[i].Name + " " + places[i].Prefab);
        }
    }
}
