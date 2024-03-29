﻿#if UNITY_EDITOR
namespace PowerUtilities
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor.Presets;
    using UnityEngine;


    /// <summary>
    /// Generate Terrains by Heightmaps 
    /// features : 
    /// 1 split heightmaps to tile heightmaps
    /// 2 generate terrain tile by heightmaps
    /// </summary>
    public class PowerTerrainGenerator : MonoBehaviour
    {
        //[Header("Heightmap")]
        public Texture2D[] heightmaps;
        public TextureResolution heightMapResolution = TextureResolution.x256;
        public List<Texture2D> splitedHeightmapList;
        [Min(1)]public int heightMapCountInRow = 1;

        //[Header("Terrain")]
        public Vector3 terrainSize = new Vector3(1000,600,1000);
        //[Min(0)]public int updateTerrainId;
        public Terrain updateTerrainTile;
        public List<Terrain> generatedTerrainList;

        //[Header("Settings")]
        [Min(0)]public int pixelError = 100;
        public TextureResolution terrainHeightmapResolution = TextureResolution.x256;
        public string nameTemplate = "Terrain Tile [{0},{1}]";
        public int groundId = 1234;
        public bool drawInstanced = false;
        public bool allowAutoConnect = true;
        public float basemapDistance = 1000;
        public LayerMask layerMask = 1;

        //[Header("Material")]
        public Material materialTemplate;
        public TerrainLayer[] terrainLayers;

        public Texture2D[] controlMaps;
        public List<Texture2D> splitedControlMaps;// terrain tile controlMaps
        public TextureResolution controlMapResolution = TextureResolution.x256;
        [Min(1)]public int controlMapCountInRow;

        public (string title, bool fold) heightmapFold = ("Heightmaps", false);
        public (string title, bool fold) terrainFold = ("Generate Terrains", false);
        public (string title, bool fold) materialFold = ("Material", false);
        public (string title, bool fold) controlMapFold = ("Control Map", false);
        public (string title, bool fold) settingsFold = ("Terrain Settings", false);
        public (string title, bool fold) exportFold = ("Export", false);
        public (string title, bool fold) saveOptionsFold = ("Save Options",false);

        //[Header("Save Options")]
        public bool isCreateSubFolder = true;
        public string terrainDataSavePath = "Assets/Terrain";
        public string prefabSavePath = "Assets/Terrain";
        public int tileX;
        public int tileZ;

        // export heightmap,controlmaps
        public TextureResolution exportTileHeightmapResolution = TextureResolution.x512;
        public TextureResolution exportTileControlmapResolution = TextureResolution.x512;
    }
}
#endif