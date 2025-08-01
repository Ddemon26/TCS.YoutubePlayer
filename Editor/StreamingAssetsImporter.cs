/*using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;


namespace TCS.YoutubePlayer.Editor {
    [InitializeOnLoad] internal static class StreamingAssetsImporter {
        // Configuration:
        // PACKAGE_DISPLAY_NAME: Used in log messages.
        // PACKAGE_TARGET_FOLDER_PREFIX: The prefix for the folder created in Assets/StreamingAssets/.
        // The final folder name will be "[PACKAGE_TARGET_FOLDER_PREFIX].[PackageVersion]".
        const string PACKAGE_DISPLAY_NAME = "TCS.YoutubePlayer";
        const string PACKAGE_TARGET_FOLDER_PREFIX = "TCS.YoutubePlayer";
        const string STREAMING_ASSETS_DIR_NAME = "StreamingAssets"; // Standard StreamingAssets folder name
        const string SOURCE_STREAMING_ASSETS_SUBDIR = "StreamingAssets~"; // Convention for package's source SA
        const string VERSION_MARKER_FILENAME = ".sa_copied_version"; // Marker file to track a copied version

        // Static fields automatically initialized by the static constructor
        static readonly string PackageVersion;
        static readonly string PackageRootPath; // Absolute path to the root of the package or asset
        static readonly string SourceStreamingAssetsFullPath; // Full path to the source StreamingAssets~ folder

        [System.Serializable] class Config {
            // For parsing package.json
            [JsonProperty( "name" )] public string Name { get; set; }
            [JsonProperty( "version" )] public string Version { get; set; }
        }

        static StreamingAssetsImporter() {
            string[] guids = AssetDatabase.FindAssets( $"t:Script {nameof(StreamingAssetsImporter)}" );
            if ( guids.Length == 0 ) {
                Debug.LogError( $"[{PACKAGE_DISPLAY_NAME}] {nameof(StreamingAssetsImporter)} script not found. This should not happen." );
                return;
            }

            // If multiple scripts have the same name, prefer the one associated with this namespace/assembly if possible,
            // or use the first one found. For simplicity, using the first.
            //string scriptPath = AssetDatabase.GUIDToAssetPath( guids[0] );

            string scriptPath;
            string[] candidatePaths = guids.Select( AssetDatabase.GUIDToAssetPath ).ToArray();
            if ( candidatePaths.Length > 1 ) {
                // Try to find the script in our namespace
                scriptPath = candidatePaths
                                 .FirstOrDefault( path =>
                                                      File.Exists( path ) &&
                                                      File.ReadAllText( path ).Contains( "namespace TCS.YoutubePlayer" )
                                 )
                             // Fallback to the first match
                             ?? candidatePaths[0];
            }
            else {
                scriptPath = candidatePaths[0];
            }

            var packageInfo = PackageInfo.FindForAssetPath( scriptPath );

            var detectedVersion = "0.0.0"; // Fallback version
            string detectedPackageRoot = null;
            string detectedJsonPath = null;

            if ( packageInfo != null ) {
                // Script is part of a UPM package
                detectedVersion = packageInfo.version;
                detectedPackageRoot = packageInfo.resolvedPath; // Absolute path to the package root
                Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] Detected UPM package: Name='{packageInfo.name}', Version='{detectedVersion}', Root='{detectedPackageRoot}'." );
            }
            else { // Script is likely in Assets/
                //Debug.LogWarning( $"[{PACKAGE_DISPLAY_NAME}] Script at '{scriptPath}' does not appear to be part of a UPM package. Attempting to locate asset root and package.json manually." );
                string scriptDirectory = Path.GetDirectoryName( scriptPath );
                if ( !string.IsNullOrEmpty( scriptDirectory ) ) {
                    // Path.GetDirectoryName(scriptDirectory) = "Assets\TCS-YoutubePlayer\TCS YoutubePlayer" (this is the root)
                    // Go up from ".../Editor" → ".../TCS YoutubePlayer" → ".../TCS-YoutubePlayer"
                    detectedPackageRoot = Path.GetDirectoryName( scriptDirectory );
                    // detectedJsonPath = Path.GetDirectoryName(
                    //     Path.GetDirectoryName( scriptDirectory )
                    // );
                    
                }

                if ( string.IsNullOrEmpty( detectedPackageRoot ) ) {
                    Debug.LogError( $"[{PACKAGE_DISPLAY_NAME}] Could not determine a valid root directory for the asset containing the script at '{scriptPath}'." );
                    return;
                }

                // if ( string.IsNullOrEmpty( detectedJsonPath ) ) {
                //     Debug.LogError( $"[{PACKAGE_DISPLAY_NAME}] Could not determine a valid package.json path for the asset at '{scriptPath}'. Ensure the script is in a valid package or asset folder structure." );
                //     return;
                // }

                string localPackageJsonPath = Path.Combine( detectedPackageRoot, "package.json" );
                if ( File.Exists( localPackageJsonPath ) ) {
                    try {
                        string json = File.ReadAllText( localPackageJsonPath );
                        var manifest = JsonConvert.DeserializeObject<Config>( json );
                        if ( !string.IsNullOrEmpty( manifest.Version ) ) {
                            detectedVersion = manifest.Version;
                        }else {
                            Debug.LogWarning( $"[{PACKAGE_DISPLAY_NAME}] Version not found in local package.json ('{localPackageJsonPath}'), using fallback '{detectedVersion}'." );
                        }
                    }
                    catch (System.Exception e) {
                        Debug.LogError( $"[{PACKAGE_DISPLAY_NAME}] Error parsing local package.json at '{localPackageJsonPath}': {e.Message}. Using fallback version '{detectedVersion}'." );
                    }
                }
                else {
                    Debug.LogWarning( $"[{PACKAGE_DISPLAY_NAME}] No local package.json found at '{localPackageJsonPath}'. Using fallback version '{detectedVersion}'. Ensure '{SOURCE_STREAMING_ASSETS_SUBDIR}' is in '{detectedPackageRoot}'." );
                }
            }

            PackageVersion = detectedVersion;
            PackageRootPath = detectedPackageRoot;

            if ( string.IsNullOrEmpty( PackageRootPath ) ) {
                Debug.LogError( $"[{PACKAGE_DISPLAY_NAME}] Critical: Package Root Path could not be determined. Aborting StreamingAssets import." );
                return;
            }

            if ( string.IsNullOrEmpty( PackageVersion ) ) {
                Debug.LogWarning( $"[{PACKAGE_DISPLAY_NAME}] Critical: Package Version could not be determined. Using 'unknown'." );
                PackageVersion = "unknown"; // Prevent null issues, indicates an error state
            }

            SourceStreamingAssetsFullPath = Path.Combine( PackageRootPath, SOURCE_STREAMING_ASSETS_SUBDIR );

           //Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] Initialized: Version='{PackageVersion}', PackageRoot='{PackageRootPath}', SourceStreamingAssets='{SourceStreamingAssetsFullPath}'" );
            CopyPackageStreamingAssets();
        }

        static void CopyPackageStreamingAssets() {
            if ( string.IsNullOrEmpty( PackageVersion ) || PackageVersion == "unknown" || string.IsNullOrEmpty( PackageRootPath ) ) {
                Debug.LogError( $"[{PACKAGE_DISPLAY_NAME}] Cannot proceed with copying StreamingAssets due to missing package information (Version or RootPath)." );
                return;
            }

            if ( !Directory.Exists( SourceStreamingAssetsFullPath ) ) {
                // This is not an error, the package might not have StreamingAssets to copy.
                Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] Source StreamingAssets folder '{SourceStreamingAssetsFullPath}' not found. Nothing to copy." );
                return;
            }

            string targetStreamingAssetsRootPath = Path.Combine( Application.dataPath, STREAMING_ASSETS_DIR_NAME );
            var versionedFolderName = $"{PACKAGE_TARGET_FOLDER_PREFIX}.{PackageVersion}";
            string currentTargetVersionFolder = Path.Combine( targetStreamingAssetsRootPath, versionedFolderName );
            string markerFilePath = Path.Combine( currentTargetVersionFolder, VERSION_MARKER_FILENAME );

            // Check if already copied and up to date
            if ( Directory.Exists( currentTargetVersionFolder ) && File.Exists( markerFilePath ) ) {
                try {
                    string copiedVersion = File.ReadAllText( markerFilePath ).Trim();
                    if ( copiedVersion == PackageVersion ) {
                        //Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] StreamingAssets for version {PackageVersion} already up to date in '{currentTargetVersionFolder}'." );
                        return;
                    }

                    Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] Version mismatch: Found '{copiedVersion}', expected '{PackageVersion}' in '{currentTargetVersionFolder}'. Re-copying." );
                }
                catch (System.Exception ex) {
                    Debug.LogWarning( $"[{PACKAGE_DISPLAY_NAME}] Could not read version marker file at '{markerFilePath}'. Re-copying. Error: {ex.Message}" );

                }
            }

            // Ensure the parent StreamingAssets folder (Assets/StreamingAssets) exists
            if ( !Directory.Exists( targetStreamingAssetsRootPath ) ) {
                Directory.CreateDirectory( targetStreamingAssetsRootPath );
                // No AssetDatabase.Refresh() needed here yet, will be called after all operations.
            }

            // Clean up: Remove other versioned folders for THIS package
            var parentDirInfo = new DirectoryInfo( targetStreamingAssetsRootPath );
            if ( parentDirInfo.Exists ) {
                var prefixToClean = $"{PACKAGE_TARGET_FOLDER_PREFIX}.";
                foreach (var dir in parentDirInfo.GetDirectories()) {
                    if ( dir.Name.StartsWith( prefixToClean ) && dir.FullName != currentTargetVersionFolder ) {
                        Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] Deleting old version folder: '{dir.FullName}'" );
                        FileUtil.DeleteFileOrDirectory( dir.FullName );
                        FileUtil.DeleteFileOrDirectory( dir.FullName + ".meta" ); // Attempt to delete .meta too

                    }
                }
            }

            // If the current target version folder exists (but was deemed outdated), delete it for a clean copy.
            if ( Directory.Exists( currentTargetVersionFolder ) ) {
                Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] Deleting existing target folder for fresh copy: '{currentTargetVersionFolder}'" );
                FileUtil.DeleteFileOrDirectory( currentTargetVersionFolder );
                FileUtil.DeleteFileOrDirectory( currentTargetVersionFolder + ".meta" );
            }

            // Copy new version
            Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] Copying StreamingAssets from '{SourceStreamingAssetsFullPath}' to '{currentTargetVersionFolder}'." );
            FileUtil.CopyFileOrDirectory( SourceStreamingAssetsFullPath, currentTargetVersionFolder );

            // Create the marker file
            try {
                // Ensure a directory exists before writing the file (CopyFileOrDirectory should create it)
                if ( !Directory.Exists( currentTargetVersionFolder ) ) {
                    Directory.CreateDirectory( currentTargetVersionFolder );
                }

                File.WriteAllText( markerFilePath, PackageVersion );
            }
            catch (System.Exception ex) {
                Debug.LogError( $"[{PACKAGE_DISPLAY_NAME}] Failed to write version marker file at '{markerFilePath}'. The assets were copied, but future updates might not be detected correctly. Error: {ex.Message}" );
            }
            
            var manifest = new Config {
                Name = PACKAGE_TARGET_FOLDER_PREFIX,
                Version = PackageVersion
            };
            
            manifest.Name = PACKAGE_TARGET_FOLDER_PREFIX;
            string minimalJson = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            string minimalJsonPath = Path.Combine(PackageRootPath, "config.json");
            File.WriteAllText(minimalJsonPath, minimalJson);

            AssetDatabase.Refresh(); // Refresh AssetDatabase to show changes in the Project window

            Debug.Log( $"[{PACKAGE_DISPLAY_NAME}] Successfully copied StreamingAssets for version {PackageVersion} to '{currentTargetVersionFolder}'." );
        }
    }
}*/