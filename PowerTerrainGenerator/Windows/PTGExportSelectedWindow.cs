namespace PowerUtilities
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    /// <summary>
    /// export selected terrain's controlMaps and heightmaps
    /// </summary>
    public class PTGExportSelectedWindow : EditorWindow
    {
        string exportFolder = "Assets/TerrainMaps";
        private Vector2 scrollPos;

        TextureResolution tileHeightmapResolution = TextureResolution.x512;
        TextureResolution tileControlmapResolution = TextureResolution.x512;

        Vector3Int gridBoundMin, gridBoundMax,gridBoundMaxNormalized;
        string heightmapFolder, controlmapsFolder;

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
            CalcGridCoordBounds(terrains);
            
            DrawSelectedTerrainUI(terrains);

            DrawHeightmapUI();

            DrawControlmapUI();

            DrawTerrainExportUI(terrains);

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

        void DrawTerrainExportUI(Terrain[] terrains)
        {
            GUILayout.BeginHorizontal("Box");
            EditorGUILayout.PrefixLabel("Export Folder:");
            exportFolder = EditorGUILayout.TextField(exportFolder);
            if (string.IsNullOrEmpty(exportFolder))
                exportFolder = "Assets";

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Export"))
            {
                CreateFolders(exportFolder, out heightmapFolder, out controlmapsFolder);

                ExportTerrains(terrains);
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

        void CalcGridCoordBounds(Terrain[] terrains)
        {
            // find (min,max )world position
            var worldPosMin = new Vector3();
            var worldPosMax = new Vector3();

            for (int i = 0; i < terrains.Length; i++)
            {
                var t = terrains[i];
                if(i == 0)
                {
                    worldPosMax = worldPosMin = t.transform.position;
                    continue;
                }

                var pos = t.transform.position;
                if (pos.x < worldPosMin.x)
                    worldPosMin.x = pos.x;
                if (pos.z < worldPosMin.z)
                    worldPosMin.z = pos.z;

                if (pos.x > worldPosMax.x)
                    worldPosMax.x = pos.x;
                if (pos.z > worldPosMax.z)
                    worldPosMax.z = pos.z;
            }

            // transfer to grid coord
            var terrainSize = terrains[0].terrainData.size;

            gridBoundMin = new Vector3Int();
            gridBoundMax = new Vector3Int();

            gridBoundMax.x = (int)(worldPosMax.x / terrainSize.x) + 1;
            gridBoundMax.z = (int)(worldPosMax.z / terrainSize.z) + 1;
            gridBoundMin.x = (int)(worldPosMin.x / terrainSize.x);
            gridBoundMin.z = (int)(worldPosMin.z / terrainSize.z);

            gridBoundMaxNormalized.x = gridBoundMax.x - gridBoundMin.x;
            gridBoundMaxNormalized.z = gridBoundMax.z - gridBoundMin.z;
        }

        Vector3Int CalcGridCoord(Terrain t)
        {
            var pos = t.transform.position;
            var size = t.terrainData.size;
            int x = (int)(pos.x / size.x);
            var y = (int)(pos.y / size.y);
            var z = (int)(pos.z / size.z);
            var coord = new Vector3Int(x - gridBoundMin.x, y, z - gridBoundMin.z);
            return coord;
        }

        void CreateFolders(string assetFolder, out string heightmapsFolder, out string controlmapsFolder)
        {
            PathTools.CreateAbsFolderPath(assetFolder);
            AssetDatabase.Refresh();

            heightmapsFolder = assetFolder;
            controlmapsFolder = assetFolder;

            //heightmapsFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(assetFolder, "Heightmaps"));
            //controlmapsFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(assetFolder, "Controlmaps"));
        }

        private void ExportTerrains(Terrain[] terrains)
        {
            ExportHeightmap(terrains, (int)tileHeightmapResolution, heightmapFolder);
            ExportControlmap(terrains, (int)tileControlmapResolution, controlmapsFolder);

            AssetDatabase.Refresh();
        }

        void ExportHeightmap(Terrain[] terrains,int tileMapResolution, string assetFoler)
        {
            var absExportFolder = PathTools.GetAssetAbsPath(assetFoler);

            var res = (int)tileMapResolution;
            var width = gridBoundMaxNormalized.x * res;
            var height = gridBoundMaxNormalized.z * res;

            if(width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
            {
                throw new Exception("heightmap out of max texture size (16384)");
            }

            var bigMap = new Texture2D(width, height, TextureFormat.R16, false, true);
            foreach (var item in terrains)
            {
                var gridCoord = CalcGridCoord(item);
                item.terrainData.FillTextureWithHeightmap(bigMap, gridCoord.x * res, gridCoord.z * res);
            }

            File.WriteAllBytes($"{absExportFolder}/Heightmap.tga", bigMap.EncodeToTGA());
        }

        void ExportControlmap(Terrain[] terrains, int tileMapResolution, string assetFoler)
        {
            var absExportFolder = PathTools.GetAssetAbsPath(assetFoler);

            var width = gridBoundMaxNormalized.x * tileMapResolution;
            var height = gridBoundMaxNormalized.z * tileMapResolution;

            if (width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
            {
                throw new Exception("big controlmap out of max texture size (16384)");
            }

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