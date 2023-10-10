using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using SoulsFormats;

namespace StudioCore
{
    /// <summary>
    /// Exposes an interface to retrieve studio assets for the various souls games.
    /// </summary>
    public class StudioFileLocator
    {
        /* Instantiate to manage multiple games at once? */
        public static GameType Type { get; set; } = GameType.Undefined;
        
        public static GameType GetGameTypeForExePath(string exePath)
        {
            GameType type = GameType.Undefined;
            if (exePath.ToLower().Contains("darksouls.exe"))
            {
                type = GameType.DarkSoulsPTDE;
            }
            else if (exePath.ToLower().Contains("darksoulsremastered.exe"))
            {
                type = GameType.DarkSoulsRemastered;
            }
            else if (exePath.ToLower().Contains("darksoulsii.exe"))
            {
                type = GameType.DarkSoulsIISOTFS;
            }
            else if (exePath.ToLower().Contains("darksoulsiii.exe"))
            {
                type = GameType.DarkSoulsIII;
            }
            else if (exePath.ToLower().Contains("eboot.bin"))
            {
                var path = Path.GetDirectoryName(exePath);
                if (Directory.Exists($@"{path}\dvdroot_ps4"))
                {
                    type = GameType.Bloodborne;
                }
                else
                {
                    type = GameType.DemonsSouls;
                }
            }
            else if (exePath.ToLower().Contains("sekiro.exe"))
            {
                type = GameType.Sekiro;
            }
            else if (exePath.ToLower().Contains("eldenring.exe"))
            {
                type = GameType.EldenRing;
            }
            else if (exePath.ToLower().Contains("armoredcore6.exe"))
            {
                type = GameType.ArmoredCoreVI;
            }
            return type;
        }

        public static bool CheckFilesExpanded(string gamepath, GameType game)
        {
            if (game == GameType.EldenRing)
            {
                if (!Directory.Exists($@"{gamepath}\map"))
                {
                    return false;
                }
                if (!Directory.Exists($@"{gamepath}\asset"))
                {
                    return false;
                }
            }
            if (game is GameType.DarkSoulsPTDE or GameType.DarkSoulsIII or GameType.Sekiro)
            {
                if (!Directory.Exists($@"{gamepath}\map"))
                {
                    return false;
                }
                if (!Directory.Exists($@"{gamepath}\obj"))
                {
                    return false;
                }
            }
            if (game == GameType.DarkSoulsIISOTFS)
            {
                if (!Directory.Exists($@"{gamepath}\map"))
                {
                    return false;
                }
                if (!Directory.Exists($@"{gamepath}\model\obj"))
                {
                    return false;
                }
            }
            if (game == GameType.ArmoredCoreVI)
            {
                //TODO AC6
            }
            return true;
        }
        private static string GetGameIDForDir()
        {
            switch (Type)
            {
                case GameType.DemonsSouls:
                    return "DES";
                case GameType.DarkSoulsPTDE:
                    return "DS1";
                case GameType.DarkSoulsRemastered:
                    return "DS1R";
                case GameType.DarkSoulsIISOTFS:
                    return "DS2S";
                case GameType.Bloodborne:
                    return "BB";
                case GameType.DarkSoulsIII:
                    return "DS3";
                case GameType.Sekiro:
                    return "SDT";
                case GameType.EldenRing:
                    return "ER";
                case GameType.ArmoredCoreVI:
                    return "AC6";
                default:
                    throw new Exception("Game type not set");
            }
        }
    	public static string GetAliasAssetsDir()
        {
            return  $@"Assets\Aliases\{GetGameIDForDir()}";
        }

    	public static string GetScriptAssetsDir()
        {
            return  $@"Assets\MassEditScripts\{GetGameIDForDir()}";
        }

    	public static string GetUpgraderAssetsDir()
        {
            return  $@"{GetParamAssetsDir()}\Upgrader";
        }

        public static string GetGameOffsetsAssetsDir()
        {
            return  $@"Assets\GameOffsets\{GetGameIDForDir()}";
        }

        public static string GetParamAssetsDir()
        {
            return  $@"Assets\Paramdex\{GetGameIDForDir()}";
        }

        public static string GetParamdefDir()
        {
            return $@"{GetParamAssetsDir()}\Defs";
        }

        public static string GetTentativeParamTypePath()
        {
            return $@"{GetParamAssetsDir()}\Defs\TentativeParamType.csv";
        }

        public static ulong[] GetParamdefPatches()
        {
            if (Directory.Exists($@"{GetParamAssetsDir()}\DefsPatch"))
            {
                var entries = Directory.GetFileSystemEntries($@"{GetParamAssetsDir()}\DefsPatch");
                return entries.Select(e => ulong.Parse(Path.GetFileNameWithoutExtension(e))).ToArray();
            }
            return new ulong[]  { };
        }
        
        public static string GetParamdefPatchDir(ulong patch)
        {
            return $@"{GetParamAssetsDir()}\DefsPatch\{patch}";
        }

        public static string GetParammetaDir()
        {
            return $@"{GetParamAssetsDir()}\Meta";
        }

        public static string GetParamNamesDir()
        {
            return $@"{GetParamAssetsDir()}\Names";
        }

        public static PARAMDEF GetParamdefForParam(string paramType)
        {
            PARAMDEF pd = PARAMDEF.XmlDeserialize($@"{GetParamdefDir()}\{paramType}.xml");
            return pd;
        }
    }
}
