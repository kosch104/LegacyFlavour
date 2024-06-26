﻿using cohtml.Net;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI.InGame;
using LegacyFlavour.Configuration;
using LegacyFlavour.Systems;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Entities;
using UnityEngine;
using static Game.Rendering.Debug.RenderPrefabRenderer;

namespace LegacyFlavour.Helpers
{
    public class DynamicZoneIcons
    {
        private static string UIPath = ConfigBase.MOD_PATH + "\\UI\\";
        private static string IconsPath = Application.streamingAssetsPath.Replace( "/", "\\" ) + "\\~UI~\\GameUI\\Media\\Game\\Icons";
        private static string ModIconsPath = UIPath + "\\Icons";

        private static Dictionary<string, string[]> ZoneColours = new( )
        {
            {"Commercial", new string[] { "#00c1ff", "#0089cf", "#0071a9", "#73e0fc", "#3ed2ff", "#005f95", "#03243e", "#001209", "#73d0ff", "#067cb9", "#024263", "#032f47", "#021c29", "#9edeff", "#24b5ff", "#05a3f4", "#034464" } },
            {"Industrial", new string[] { "#ffdc00", "#d6a900", "#a48700", "#fff69f", "#f29d00", "#504200", "#8a6d00", "#f1bf00" } },
            {"Residential", new string[] { "#023b0a", "#00b41a", "#004b12", "#02ff26", "#00580d", "#00ce1e", "#9dffab", "#00ff25", "#7cff62", "#03d321", "#006117", "#008620", "#008f15", "#08961c", "#024412", "#00c1ff", "#0089cf", "#0071a9", "#03243e", "#73d0ff", "#002612", "#065b12", "#01cf1e" } },
            {"Office", new string[] { "#b400ff", "#9a0ad6", "#7902ab", "#d078f5", "#64028d", "#310146", "#660090", "#c65af3" } }
        };

        private static FieldInfo toolbarUI_statusUpdate = typeof( ToolbarUISystem ).GetField( "m_UniqueAssetStatusChanged", BindingFlags.Instance | BindingFlags.NonPublic );
        private static FieldInfo infoViewUI_statusUpdate = typeof( InfoviewsUISystem ).GetField( "m_InfoviewChanged", BindingFlags.Instance | BindingFlags.NonPublic );
        private static MethodInfo infoViewUI_OnGameLoaded = typeof( InfoviewsUISystem ).GetMethod( "OnGameLoaded", BindingFlags.Instance | BindingFlags.NonPublic );

        public static List<(UIObject Instance, string Original)> _replacements = new List<(UIObject, string)>( );
        public static List<(InfoviewPrefab Instance, string Original)> _infoViewReplacements = new List<(InfoviewPrefab, string)>( );

        private List<string> IconNames
        {
            get;
            set;
        } = new List<string>( );

        private List<string> ReplacedIcons
        {
            get;
            set;
        } = new List<string>( );

        static FieldInfo m_Prefabs = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.NonPublic | BindingFlags.Instance );

        private readonly static string[] MANUAL_ICONS = new[] { "ZoneResidential", "ZoneCommercial", "ZoneOffice", "ZoneIndustrial" };        

        private readonly ZoneColourSystem _zoneColourSystem;
        private readonly PrefabSystem _prefabSystem;
        private readonly LegacyFlavourConfig _config;

        public DynamicZoneIcons( )
        {
            var world = World.DefaultGameObjectInjectionWorld;
            _prefabSystem = world.GetOrCreateSystemManaged<PrefabSystem>( );
            _zoneColourSystem = world.GetExistingSystemManaged<ZoneColourSystem>( );
            _config = world.GetExistingSystemManaged<LegacyFlavourUpdateSystem>( ).Config;
            ScanDirectory( );
            ScanPrefabs( );
        }

        /// <summary>
        /// Replaces an infoview prefab icon and caches it for updates
        /// </summary>
        private void ScanPrefabs( )
        {
            LegacyFlavourSystem.EnsureModUIFolder( );

            var prefabs = ( ( List<PrefabBase> ) m_Prefabs.GetValue( _prefabSystem ) )?
                .Where( p => p is InfoviewPrefab )
                .Select( p => ( InfoviewPrefab ) p );

            foreach ( var prefab in prefabs )
            {
                var url = prefab.m_IconPath;

                if ( _zoneColourSystem == null || !url.Contains( "Media/Game/Icons/" ) )
                    return;

                if ( _zoneColourSystem.DynamicZoneIcons?.CheckForReplacement( url, out var parsedUrl ) == true )
                {
                    prefab.m_IconPath = parsedUrl;
                    DynamicZoneIcons._infoViewReplacements.Add( (prefab, url) );
                }
            }
        }

        /// <summary>
        /// Scan the vanilla ui directory for the icons
        /// </summary>
        private void ScanDirectory( )
        {
            if ( !Directory.Exists( ModIconsPath ) )
                Directory.CreateDirectory( ModIconsPath );

            Debug.Log( "LegacyUI Scanning Zone Icons: " + IconsPath );

            var svgs = Directory.GetFiles( IconsPath, "*.svg" );

            foreach ( var svg in svgs )
            {
                var fileName = Path.GetFileNameWithoutExtension( svg );

                if ( fileName.StartsWith( "Zone" ) || fileName.StartsWith( "Zoning" ) )
                    IconNames.Add( fileName );
            }

            ModifyBasedOnColours( true );
        }

        /// <summary>
        /// Modify icons based on our zone config colours
        /// </summary>
        /// <param name="invalidateCache"></param>
        public void ModifyBasedOnColours( bool invalidateCache = false )
        {
            ReplacedIcons.Clear( );

            if ( invalidateCache )
            {
                _config.IconsID = System.Guid.NewGuid( ).ToString( ).Replace( "-", "" );
                _config.Save( );

                // Clear old icons out
                var oldIconDirectories = Directory.GetDirectories( ModIconsPath );

                if ( oldIconDirectories?.Length > 0 )
                {
                    foreach ( var iconDirectory in oldIconDirectories )
                    {
                        Directory.Delete( iconDirectory, true );
                    }
                }

                var files = Directory.GetFiles( ModIconsPath );
                if ( files?.Length > 0 )
                {
                    foreach ( var iconFile in files )
                    {
                        File.Delete( iconFile );
                    }
                }

                foreach ( var colourSet in _zoneColourSystem.AllColours )
                {
                    ProcessColours( colourSet.Item1, colourSet.Item2 );
                }
            }
            else
            {
                ProcessColours( _zoneColourSystem.colourBlindness, _zoneColourSystem.Colours );
            }

            UpdateReplacementIcons( );
        }

        /// <summary>
        /// Process colours
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="colours"></param>
        /// <param name="invalidateCache"></param>
        private void ProcessColours( ColourBlindness mode, Dictionary<string, Color> colours, bool invalidateCache = false  )
        {
            var workingSVG = IconNames.ToDictionary( i => i, i => string.Empty );

            foreach ( var iconName in IconNames )
                ProcessIcon( mode, workingSVG, colours, iconName, invalidateCache );

            foreach ( var iconName in MANUAL_ICONS )
                ProcessIcon( mode, workingSVG, colours, iconName, invalidateCache, true );
        }

        /// <summary>
        /// Update icons to be replaced
        /// </summary>
        private void UpdateReplacementIcons( )
        {
            var replacements = _replacements;

            if ( replacements?.Count > 0 )
            {
                foreach ( var replacement in replacements )
                {
                    var instance = replacement.Instance;

                    if ( instance == null )
                        continue;

                    if ( CheckForReplacement( replacement.Original, out var parsedUrl ) == true )
                    {
                        instance.m_Icon = parsedUrl;
                    }
                }
            }

            var infoViewReplacements = _infoViewReplacements;

            if ( infoViewReplacements?.Count > 0 )
            {
                foreach ( var replacement in infoViewReplacements )
                {
                    var instance = replacement.Instance;

                    if ( instance == null )
                        continue;

                    if ( CheckForReplacement( replacement.Original, out var parsedUrl ) == true )
                    {
                        instance.m_IconPath = parsedUrl;
                    }
                }
            }

            var toolbarUI = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ToolbarUISystem>( );

            if ( toolbarUI != null )
            {
                toolbarUI_statusUpdate.SetValue( toolbarUI, true );
            }

            var infoViewUI = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InfoviewsUISystem>( );

            if ( infoViewUI != null )
            {
                infoViewUI_statusUpdate.SetValue( infoViewUI, true );
                infoViewUI_OnGameLoaded.Invoke( infoViewUI, new[] { ( object ) default( Context ) } );
            }
        }

        /// <summary>
        /// Process a matched icon, converting it and caching if necessary
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="workingSVG"></param>
        /// <param name="colours"></param>
        /// <param name="iconName"></param>
        /// <param name="invalidateCache"></param>
        /// <param name="isRootIcon"></param>
        private void ProcessIcon( ColourBlindness mode, Dictionary<string, string> workingSVG, Dictionary<string, Color> colours, string iconName, bool invalidateCache = false, bool isRootIcon = false )
        {
            string matchingKey;

            if ( isRootIcon )
                matchingKey = colours.Keys.FirstOrDefault( k => ( "Zone" + k.Replace( " ", "" ) ).StartsWith( iconName ) );
            else
                matchingKey = colours.Keys.FirstOrDefault( k => "Zone" + k.Replace( " ", "" ) == iconName );

            if ( string.IsNullOrEmpty( matchingKey ) )
                return;

            var colourMatch = colours[matchingKey];
            var copyPath = ModIconsPath + "\\" + _config.IconsID + "\\" + iconName + "_" + mode.ToString( ) + ".svg";

            Directory.CreateDirectory( ModIconsPath + "\\" + _config.IconsID );

            if ( invalidateCache || !File.Exists( copyPath ) )
            {
                var srcPath = IconsPath + "\\" + iconName + ".svg";
                workingSVG[iconName] = File.ReadAllText( srcPath );

                var svg = workingSVG[iconName];

                if ( _config.OverrideIcons )
                    ConvertSVG( iconName, "#" + ColorUtility.ToHtmlStringRGB( colourMatch ), ref svg );
                
                workingSVG[iconName] = svg;
                SaveSVG( iconName, copyPath, svg );
            }
            else
            {
                workingSVG[iconName] = File.ReadAllText( copyPath );
                ReplacedIcons.Add( iconName );
            }
        }

        /// <summary>
        /// Save the SVG and add it to the replaced icons cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="filePath"></param>
        /// <param name="svg"></param>
        private void SaveSVG( string key, string filePath, string svg )
        {
            if ( string.IsNullOrEmpty( svg ) )
                return;

            File.WriteAllText( filePath, svg, Encoding.UTF8 );
            ReplacedIcons.Add( key );
        }

        /// <summary>
        /// Convert an SVG to our colour scheme
        /// </summary>
        /// <param name="iconName"></param>
        /// <param name="targetHex"></param>
        /// <param name="svg"></param>
        private void ConvertSVG( string iconName, string targetHex, ref string svg )
        {
            foreach ( var zoneColourGroup in ZoneColours )
            {
                if ( !iconName.StartsWith( "Zone" + zoneColourGroup.Key ) || 
                    zoneColourGroup.Value == null ||
                    zoneColourGroup.Value.Length == 0 )
                    continue;

                foreach ( var colour in zoneColourGroup.Value )
                {
                    var adjustedHue = ColourHelpers.MatchHue( colour, targetHex );
                    svg = svg.Replace( colour, adjustedHue );
                }
            }
        }

        /// <summary>
        /// Check an incoming SVG icon for a matched zone icon URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parsedUrl"></param>
        /// <returns></returns>
        public bool CheckForReplacement( string url, out string parsedUrl )
        {
            parsedUrl = url;

            var fileName = Path.GetFileNameWithoutExtension( url );
            var mode = _zoneColourSystem.colourBlindness.ToString( );
            foreach ( var iconName in ReplacedIcons )
            {
                if ( fileName != iconName )
                    continue;

                // When config enabled ensure original icons get used
                if ( !_config.Enabled )
                    return true;

                parsedUrl = "coui://legacyflavourui/Icons/" + _config.IconsID + "/" + iconName + "_" + mode + ".svg";
                return true;
            }

            return false;
        }
    }
}
