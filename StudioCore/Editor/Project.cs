using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using StudioCore.Platform;
using StudioCore.ParamEditor;
using System.Linq;

namespace StudioCore.Editor
{

    public enum ProjectType
    {
        /* Regular DSMS Project */
        Json,
        /* Virtual Project, such as vanilla folder, lacking json but containing files */
        Folder,
        /* Virtual Project, comprised only of param file (or other specific file if we reach that point) */
        ParamFile
    }
    
    /// <summary>
    /// Class representing a modding project.
    /// </summary>
    public class Project
    {

        public static List<Project> LoadedProjects = new();

        public ProjectType Type;
        public Project ParentProject;

        /* Settings valid for regular/json type projects */
        public ProjectSettings Settings;
        /* AssetLocator valid for regular/json and folder type projects */
        public AssetLocator AssetLocator;

        public ParamBank ParamBank;
        public ParamEditorScreen ParamEditorScreen;


        public Project(string folder)
        {
            Type = ProjectType.Folder;
            ParentProject = null;

            AssetLocator = new AssetLocator();
            AssetLocator.SetModProjectDirectory(folder);
        }

        public Project(string jsonPath, string vanillaFolder, Project parent = null)
        {
            Type = ProjectType.Json;
            ParentProject = parent ?? new Project(vanillaFolder);
            
            Settings = ProjectSettings.Deserialize(jsonPath);
            AssetLocator = new AssetLocator();
            AssetLocator.SetFromProjectSettings(Settings, Path.GetDirectoryName(jsonPath));
        }

        public Project(string paramFile, Project parent)
        {
            Type = ProjectType.ParamFile;
            ParentProject = parent;

            AssetLocator = null;
        }

        public IEnumerable<Project> GetSiblingProjects()
        {
            if (ParentProject == null)
                return new List<Project>();
            return LoadedProjects.Where((p) => p.ParentProject == ParentProject && p != this);
        }
    }
}
