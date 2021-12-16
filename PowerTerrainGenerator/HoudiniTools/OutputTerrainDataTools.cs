//#define HOUDINI_ENGINE

namespace HoudiniTools
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
#if HOUDINI_ENGINE
    using HoudiniEngineUnity;
#endif

#if UNITY_EDITOR
    using UnityEditor;
    using System;

    [CustomEditor(typeof(OutputTerrainDataTools))]
    public class OutputTerrainDataEditor : Editor
    {
        private bool isAllTerrain;

        TerrainData[] lastTerrainDatas;

        void OutputTerrainData(Terrain[] terrains)
        {
            var path = "Assets/TerrainDatas";
            AssetDatabase.DeleteAsset(path);
            if (!AssetDatabase.IsValidFolder(path))
            {
                path = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder("Assets", "TerrainDatas"));
            }

            var totalCount = terrains.Length;
            var curCount = 1;

            for (int i = 0; i < totalCount; i++)
            {
                var terrain = terrains[i];
                var dataLast = terrain.terrainData;
                var dataClone = Instantiate(dataLast);

                terrain.terrainData = dataClone;
                var tc = terrain.GetComponent<TerrainCollider>();
                if (tc)
                    tc.terrainData = dataClone;

                var go = terrain.transform.parent ?? terrain.transform;
                var tdPath = $"{path}/{go.name}.asset";

                // save -> reimport -> write data -> save
                AssetDatabase.CreateAsset(dataClone, tdPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(tdPath);
                dataClone = AssetDatabase.LoadAssetAtPath<TerrainData>(tdPath);
                dataClone.SetAlphamaps(0, 0, dataLast.GetAlphamaps(0, 0, dataLast.alphamapWidth, dataLast.alphamapHeight));
                AssetDatabase.SaveAssets();


                EditorUtility.DisplayProgressBar("Warning", "Output Terrain Datas", (curCount++) / totalCount);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }
#if HOUDINI_ENGINE
        void ResyncTerrains(HEU_BaseSync[] syncScripts)
        {
            var totalCount = syncScripts.Length;
            for (int i = 0; i < totalCount; i++)
            {
                EditorUtility.DisplayProgressBar("Warning", "Resync Terrains", i / (float)totalCount);
                var item = syncScripts[i];
                item.Resync();
            }
            EditorUtility.ClearProgressBar();
        }
#endif
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            var inst = target as OutputTerrainDataTools;

            GUILayout.BeginVertical("Box");
            if (GUILayout.Button("Export TerrainDatas"))
            {
                var terrains = inst.GetComponentsInChildren<Terrain>();
                OutputTerrainData(terrains);

                lastTerrainDatas = Array.ConvertAll(terrains, terrain => terrain.terrainData);
            }
            GUILayout.EndVertical();

#if HOUDINI_ENGINE
            GUILayout.BeginVertical("Box");
            if (GUILayout.Button("Resync Houdini caches"))
            {
                var syncScripts = inst.GetComponentsInChildren<HEU_BaseSync>();
                ResyncTerrains(syncScripts);
            }
            GUILayout.EndVertical();
#endif

            GUILayout.BeginVertical("Box");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainLayers"));
            isAllTerrain = GUILayout.Toggle(isAllTerrain, "All Terrains");


            if(GUILayout.Button("Reassign Terrain Layers"))
            {
                ReassignTerrainLayers(inst);
            }
            GUILayout.EndVertical();
        }

        private void ReassignTerrainLayers(OutputTerrainDataTools inst)
        {
            var terrains = inst.GetComponentsInChildren<Terrain>();
            if (isAllTerrain)
            {
                terrains = Array.ConvertAll(FindObjectsOfType<Terrain>(true), go => go.GetComponent<Terrain>());
            }
            foreach (var item in terrains)
            {
                if (item == null)
                    continue;
                var td = item.terrainData;
                if (!td)
                    continue;
                td.terrainLayers = inst.terrainLayers;
            }
        }
    }
#endif

    public class OutputTerrainDataTools : MonoBehaviour
    {
        //public Terrainlay
        public TerrainLayer[] terrainLayers;
    }
}