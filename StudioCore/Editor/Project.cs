using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using StudioCore.Platform;
using StudioCore.ParamEditor;

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

        public ProjectType Type;
        public Project ParentProject;

        public ProjectSettings Settings;
        public AssetLocator AssetLocator;

        public ParamBank ParamBank;


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
            
            AssetLocator = new AssetLocator();
            //TODO
            AssetLocator.SetFromProjectSettings(null, null);
        }

        public Project(string paramFile, Project parent)
        {
            Type = ProjectType.ParamFile;
            ParentProject = parent;
            
            AssetLocator = null;
        }
    }
}
