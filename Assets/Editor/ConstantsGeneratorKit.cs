using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Prime31.Editor
{
	//Note: This class uses UnityEditorInternal which is an undocumented internal feature
	public class ConstantsGeneratorKit : MonoBehaviour
	{
		private const string FOLDER_LOCATION = "scripts/auto-generated/";
		private const string NAMESPACE = "k";
		private static string[] IGNORE_RESOURCES_IN_SUBFOLDERS = new string[] { "ProCore", "2DToolkit" };

	    private const string TAGS_FILE_NAME = "Tags.cs";
	    private const string LAYERS_FILE_NAME = "Layers.cs";
		private const string SCENES_FILE_NAME = "Scenes.cs";
		private const string RESOURCE_PATHS_FILE_NAME = "Resources.cs";


		[MenuItem( "Edit/Generate Constants Classes..." )]
	    static void RebuildTagsAndLayersClasses()
	    {
	        var folderPath = Application.dataPath + "/" + FOLDER_LOCATION;
	        if( !Directory.Exists(folderPath ) )
	            Directory.CreateDirectory( folderPath );

			File.WriteAllText( folderPath + TAGS_FILE_NAME, getClassContent( TAGS_FILE_NAME.Replace( ".cs", string.Empty ), UnityEditorInternal.InternalEditorUtility.tags ) );
			File.WriteAllText( folderPath + LAYERS_FILE_NAME, getLayerClassContent( LAYERS_FILE_NAME.Replace( ".cs", string.Empty ), UnityEditorInternal.InternalEditorUtility.layers ) );
			File.WriteAllText( folderPath + SCENES_FILE_NAME, getClassContent( SCENES_FILE_NAME.Replace( ".cs", string.Empty ), editorBuildSettingsScenesToNameStrings( EditorBuildSettings.scenes ) ) );
			File.WriteAllText( folderPath + RESOURCE_PATHS_FILE_NAME, getResourcePathsContent( RESOURCE_PATHS_FILE_NAME.Replace( ".cs", string.Empty ) ) );

	        AssetDatabase.ImportAsset( "Assets/" + FOLDER_LOCATION + TAGS_FILE_NAME, ImportAssetOptions.ForceUpdate );
	        AssetDatabase.ImportAsset( "Assets/" + FOLDER_LOCATION + LAYERS_FILE_NAME, ImportAssetOptions.ForceUpdate );
	  		AssetDatabase.ImportAsset( "Assets/" + FOLDER_LOCATION + SCENES_FILE_NAME, ImportAssetOptions.ForceUpdate );
			AssetDatabase.ImportAsset( "Assets/" + FOLDER_LOCATION + RESOURCE_PATHS_FILE_NAME, ImportAssetOptions.ForceUpdate );

			Debug.Log( "ConstantsGeneratorKit complete. Constants classes built to " + FOLDER_LOCATION );
	    }


	    private static string[] editorBuildSettingsScenesToNameStrings( EditorBuildSettingsScene[] scenes )
	    {
	        var sceneNames = new string[scenes.Length];
	        for( var n = 0; n < sceneNames.Length; n++ )
	            sceneNames[n] = System.IO.Path.GetFileNameWithoutExtension( scenes[n].path );

	        return sceneNames;
	    }


	    private static string getClassContent( string className, string[] labelsArray )
	    {
	        var output = "";
	        output += "//This class is auto-generated do not modify\n";
			output += "namespace " + NAMESPACE + "\n";
			output += "{\n";
			output += "\tpublic static class " + className + "\n";
			output += "\t{\n";

	        foreach( var label in labelsArray )
				output += "\t\t"+ buildConstVariable( label ) + "\n";

			output += "\t}\n";
			output += "}";

	        return output;
	    }


		private class Resource
		{
			public string name;
			public string path;


			public Resource( string path )
			{
				// get the path from the Resources folder root
				var parts = path.Split( new string[] { "Resources/" }, System.StringSplitOptions.RemoveEmptyEntries );

				// strip the extension from the path
				this.path = parts[1].Replace( Path.GetFileName( parts[1] ), Path.GetFileNameWithoutExtension( parts[1] ) );
				this.name = Path.GetFileNameWithoutExtension( parts[1] );
			}
		}


	    private static string getResourcePathsContent( string className )
	    {
			var output = "";
			output += "//This class is auto-generated do not modify\n";
			output += "namespace " + NAMESPACE + "\n";
			output += "{\n";
			output += "\tpublic static class " + className + "\n";
			output += "\t{\n";


			// find all our Resources folders
			var dirs = Directory.GetDirectories( Application.dataPath, "Resources", SearchOption.AllDirectories );
			var resources = new List<Resource>();

			foreach( var dir in dirs )
			{
				// limit our ignored folders
				var shouldAddFolder = true;
				foreach( var ignoredDir in IGNORE_RESOURCES_IN_SUBFOLDERS )
				{
					if( dir.Contains( ignoredDir ) )
					{
						Debug.LogWarning( "DONT ADD FOLDER + " + dir );
						shouldAddFolder = false;
						continue;
					}
				}

				if( shouldAddFolder )
					resources.AddRange( getAllResourcesAtPath( dir ) );
			}

			var resourceNamesAdded = new List<string>();
			foreach( var res in resources )
			{
				if( resourceNamesAdded.Contains( res.name ) )
				{
					Debug.LogWarning( "multiple resources with name " + res.name + " found. Skipping " + res.path );
					continue;
				}

				output += "\t\t" + buildConstVariable( res.name, "", res.path ) + "\n";
				resourceNamesAdded.Add( res.name );
			}


			output += "\t}\n";
			output += "}";

			return output;
	    }


	    private static List<Resource> getAllResourcesAtPath( string path )
	    {
			var resources = new List<Resource>();

			// handle files
			var files = Directory.GetFiles( path, "*", SearchOption.AllDirectories );
			foreach( var f in files )
			{
				if( f.EndsWith( ".meta" ) || f.EndsWith( ".db" ) || f.EndsWith( ".DS_Store" ) )
				   continue;

				resources.Add( new Resource( f ) );
			}

			return resources;
	    }


	    private static string getLayerClassContent( string className, string[] labelsArray )
	    {
	        var output = "";
	        output += "// This class is auto-generated do not modify\n";
			output += "namespace " + NAMESPACE + "\n";
			output += "{\n";
	        output += "\tpublic static class " + className + "\n";
			output += "\t{\n";

	        foreach( var label in labelsArray )
				output += "\t\t" + "public const int " + toUpperCaseWithUnderscores( label ) + " = " + LayerMask.NameToLayer( label ) + ";\n";

			output += "\n\n";
			output += @"		public static int onlyIncluding( params int[] layers )
		{
			int mask = 0;
			for( var i = 0; i < layers.Length; i++ )
				mask |= ( 1 << layers[i] );

			return mask;
		}


		public static int everythingBut( params int[] layers )
		{
			return ~onlyIncluding( layers );
		}";

			output += "\n";
			output += "\t}\n";
			output += "}";

	        return output;
	    }


	    private static string buildConstVariable( string varName, string suffix = "", string value = null )
	    {
	    	value = value ?? varName;
	        return "public const string " + toUpperCaseWithUnderscores( varName ) + suffix + " = " + '"' + value + '"' + ";";
	    }


	    private static string toUpperCaseWithUnderscores( string input )
	    {
	    	input = input.Replace( "-", "_" );
	        var output = "" + input[0];

	        for( var n = 1; n < input.Length; n++ )
	        {
	            if( ( char.IsUpper( input[n] ) || input[n] == ' ' ) && !char.IsUpper( input[n - 1] ) && input[n - 1] != '_' && input[n - 1] != ' ' )
	                output += "_";

	            if( input[n] != ' ' && input[n] != '_' )
	                output += input[n];
	        }

	        return output.ToUpper();
	    }
	}
}