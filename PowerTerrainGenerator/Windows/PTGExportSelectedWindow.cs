namespace PowerUtilities
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using System.IO;

    public class PTGExportSelectedWindow : EditorWindow
    {
        string exportFolder = "Assets/Terrain";
        private Vector2 scrollPos;

        TextureResolution tileHeightmapResolution = TextureResolution.x512;
        TextureResolution tileControlmapResolution = TextureResolution.x512;

        [MenuItem(PowerTerrainGeneratorMenu.ROOT_PATH + "/Terrain/ExportSelected")]
        static void ShowExportWindow()
        {
            var win = GetWindow<PTGExportSelectedWindow>();
            win.Show();
        }

        private void OnGUI()
        {
            var trs = Selection.transforms;
            var terrains = trs.Where(tr => tr.GetComponent<Terrain>())
                .Select(tr => tr.GetComponent<Terrain>())
                .ToArray();

            if (terrains.Length == 0)
            {
                EditorGUILayout.HelpBox("No Terrains selected", MessageType.Warning);
                return;
            }

            Array.Sort(terrains, (t1, t2) => (int)((t1.transform.position.x - t2.transform.position.x) + (t1.transform.position.z - t2.transform.position.z)));



            DrawExportUI(terrains);


        }

        private void DrawExportUI(Terrain[] terrains)
        {
            int x, z;
            CalcRowColumns(terrains, out x, out z);
            EditorGUILayout.LabelField($"{x} - {z}");

            DrawSelectedTerrainUI(terrains);

            DrawHeightmapUI();

            DrawControlmapUI();

            DrawTerrainExportUI(terrains, x, z);

        }
        void DrawControlmapUI()
        {
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label(nameof(tileControlmapResolution));
            tileControlmapResolution = (TextureResolution)EditorGUILayout.EnumPopup(tileControlmapResolution);
            GUILayout.EndHorizontal();
        }

        void DrawHeightmapUI()
        {
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label(nameof(tileHeightmapResolution));
            tileHeightmapResolution = (TextureResolution)EditorGUILayout.EnumPopup(tileHeightmapResolution);
            GUILayout.EndHorizontal();
        }

        void DrawTerrainExportUI(Terrain[] terrains, int x, int z)
        {
            GUILayout.BeginHorizontal("Box");
            EditorGUILayout.PrefixLabel("Export Folder:");
            exportFolder = EditorGUILayout.TextField(exportFolder);
            if (string.IsNullOrEmpty(exportFolder))
                exportFolder = "Assets";

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Export"))
            {
                ExportTerrains(terrains, x, z, exportFolder);
            }
        }

        void DrawSelectedTerrainUI(Terrain[] terrains)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var item in terrains)
            {
                EditorGUILayout.LabelField(item.name);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void CalcRowColumns(Terrain[] terrains, out int x, out int z)
        {
            var end = terrains[terrains.Length - 1];
            var td = terrains[0].terrainData;
            x = (int)(end.transform.position.x / td.size.x) + 1;
            z = (int)(end.transform.position.z / td.size.z) + 1;
        }

        Vector3Int CalcGridCoord(Terrain t)
        {
            var pos = t.transform.position;
            var size = t.terrainData.size;
            int x = (int)(pos.x / size.x);
            var y = (int)(pos.y / size.y);
            var z = (int)(pos.z / size.z);
            return new Vector3Int(x, y, z);
        }

        void CreateFolders(string assetFolder, out string heightmapsFolder, out string controlmapsFolder)
        {
            PathTools.CreateAbsFolderPath(assetFolder);
            heightmapsFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(assetFolder, "Heightmaps"));
            controlmapsFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(assetFolder, "Controlmaps"));
        }

        private void ExportTerrains(Terrain[] terrains, int countX, int countZ, string assetFolder)
        {
            string heightmapFolder, controlmapsFolder;
            CreateFolders(assetFolder, out heightmapFolder, out controlmapsFolder);

            ExportHeightmap(terrains, countX, countZ, (int)tileHeightmapResolution, heightmapFolder);
            ExportControlmap(terrains, countX, countZ, (int)tileControlmapResolution, controlmapsFolder);
        }

        void ExportHeightmap(Terrain[] terrains, int countX, int countZ,int tileMapResolution, string assetFoler)
        {
            var absExportFolder = PathTools.GetAssetAbsPath(assetFoler);

            var res = (int)tileMapResolution;
            var width = countX * res;
            var height = countZ * res;

            var bigMap = new Texture2D(width, height, TextureFormat.R16, false, true);
            foreach (var item in terrains)
            {
                var gridCoord = CalcGridCoord(item);
                item.terrainData.FillTextureWithHeightmap(bigMap, gridCoord.x * res, gridCoord.z * res);
            }

            File.WriteAllBytes($"{absExportFolder}/Heightmap.tga", bigMap.EncodeToTGA());
        }

        void ExportControlmap(Terrain[] terrains, int countX, int countZ, int tileMapResolution, string assetFoler)
        {
            var absExportFolder = PathTools.GetAssetAbsPath(assetFoler);

            var width = countX * tileMapResolution;
            var height = countZ * tileMapResolution;

            var bigMaps = new Texture2D[terrains[0].terrainData.alphamapTextureCount];
            for (int i = 0; i < bigMaps.Length; i++)
            {
                bigMaps[i] = new Texture2D(width, height, TextureFormat.R16, false, true);
            }

            for (int i = 0; i < terrains.Length; i++)
            {
                var t =  terrains[i];
                var td = t.terrainData;
                var gridCoord = CalcGridCoord(t);

                // read alphamaps
                for (int j = 0; j < td.alphamapTextureCount; j++)
                {
                    var alphamap = t.terrainData.GetAlphamapTexture(j);
                    bigMaps[j].SetPixels(gridCoord.x * tileMapResolution, gridCoord.z * tileMapResolution, tileMapResolution, tileMapResolution, alphamap.GetPixels());

                }
            }

            for (int i = 0; i < bigMaps.Length; i++)
            {
                File.WriteAllBytes($"{absExportFolder}/Controlmap_{i}.tga", bigMaps[i].EncodeToTGA());
            }
        }
    }
}