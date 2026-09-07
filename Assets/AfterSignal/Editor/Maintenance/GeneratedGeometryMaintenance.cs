using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AfterSignal.Editor
{
    // Generated meshes have no string-based runtime loader; their GUID references
    // in scenes and prefabs are the source of truth. Authored art/audio are excluded.
    public static class GeneratedGeometryMaintenance
    {
        const string Directory = "Assets/AfterSignal/Resources/Geometry/";
        static readonly Regex GeneratedName = new Regex(@"(?:.+_batch_\d+|CityLife-(?:roof|town)-\d+-\d+)\.asset$");
        [MenuItem("AFTERSIGNAL/Maintenance/Remove unused generated geometry")]
        public static void RemoveUnused()
        {
            AssetDatabase.SaveAssets();
            var all = AssetDatabase.GetAllAssetPaths();
            var roots = all.Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) && !path.StartsWith(Directory, StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(path)).ToArray();
            var used = new HashSet<string>(AssetDatabase.GetDependencies(roots, true), StringComparer.Ordinal);
            int removed = 0;
            foreach (var path in all)
            {
                if (!path.StartsWith(Directory, StringComparison.Ordinal) || !GeneratedName.IsMatch(System.IO.Path.GetFileName(path)) || used.Contains(path))
                    continue;
                if (AssetDatabase.DeleteAsset(path))
                    removed++;
            }

            Debug.Log("Geometry cleanup: removed " + removed + " unreferenced generated meshes.");
        }
    }
}
