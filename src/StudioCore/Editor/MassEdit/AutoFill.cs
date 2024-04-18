﻿using Andre.Formats;
using StudioCore.Editor;
using static Andre.Native.ImGuiBindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StudioCore.Editor.MassEdit;

internal class AutoFillSearchEngine
{
    private readonly string[] _autoFillArgs;
    private readonly TypelessSearchEngine engine;
    private readonly string id;
    private AutoFillSearchEngine _additionalCondition;
    private bool _autoFillNotToggle;
    private bool _useAdditionalCondition;

    internal AutoFillSearchEngine(string id, TypelessSearchEngine searchEngine)
    {
        this.id = id;
        engine = searchEngine;
        _autoFillArgs = Enumerable.Repeat("", engine.AllCommands().Sum(x => x.Item2.Length)).ToArray();
        _autoFillNotToggle = false;
        _useAdditionalCondition = false;
        _additionalCondition = null;
    }

    internal string Menu(bool enableComplexToggles, bool enableDefault, string suffix, string inheritedCommand,
        Func<string, string> subMenu)
    {
        return Menu(enableComplexToggles, null, enableDefault, suffix, inheritedCommand, subMenu);
    }

    internal string Menu(bool enableComplexToggles, AutoFillSearchEngine multiStageSE,
        bool enableDefault, string suffix, string inheritedCommand, Func<string, string> subMenu)
    {
        var currentArgIndex = 0;
        if (enableComplexToggles)
        {
            ImGui.Checkbox("Invert selection?##meautoinputnottoggle" + id, ref _autoFillNotToggle);
            ImGui.SameLine();
            ImGui.Checkbox("Add another condition?##meautoinputadditionalcondition" + id,
                ref _useAdditionalCondition);
        }
        else if (multiStageSE != null)
        {
            ImGui.Checkbox("Add another condition?##meautoinputadditionalcondition" + id,
                ref _useAdditionalCondition);
        }

        if (_useAdditionalCondition && _additionalCondition == null)
        {
            _additionalCondition = new AutoFillSearchEngine(id + "0", engine);
        }
        else if (!_useAdditionalCondition)
        {
            _additionalCondition = null;
        }

        foreach ((string, string[], string) cmd in engine.VisibleCommands(enableDefault))
        {
            var argIndices = new int[cmd.Item2.Length];
            var valid = true;
            for (var i = 0; i < argIndices.Length; i++)
            {
                argIndices[i] = currentArgIndex;
                currentArgIndex++;
                if (string.IsNullOrEmpty(_autoFillArgs[argIndices[i]]))
                {
                    valid = false;
                }
            }

            string subResult = null;
            var wiki = cmd.Item3;
            UIHints.AddImGuiHintButton(cmd.Item1 == null ? "hintdefault" : "hint" + cmd.Item1, ref wiki, false,
                true);
            if (subMenu != null || _additionalCondition != null)
            {
                if (ImGui.BeginMenu(cmd.Item1 == null ? "Default filter..." : cmd.Item1, valid))
                {
                    var curResult = inheritedCommand + getCurrentStepText(valid, cmd.Item1, argIndices,
                        _additionalCondition != null ? " && " : suffix);
                    if (_useAdditionalCondition && multiStageSE != null)
                    {
                        subResult = multiStageSE.Menu(enableComplexToggles, enableDefault, suffix, curResult,
                            subMenu);
                    }
                    else if (_additionalCondition != null)
                    {
                        subResult = _additionalCondition.Menu(enableComplexToggles, enableDefault, suffix,
                            curResult, subMenu);
                    }
                    else
                    {
                        subResult = subMenu(curResult);
                    }

                    ImGui.EndMenu();
                }
            }
            else
            {
                subResult = ImGui.Selectable(cmd.Item1 == null ? "Default filter..." : cmd.Item1, false,
                    valid ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled)
                    ? suffix
                    : null;
            }

            //ImGui.Indent();
            for (var i = 0; i < argIndices.Length; i++)
            {
                if (i != 0)
                {
                    ImGui.SameLine();
                }

                ImGui.InputTextWithHint("##meautoinput" + argIndices[i], cmd.Item2[i],
                    ref _autoFillArgs[argIndices[i]], 256);
                var var = AutoFill.MassEditAutoFillForVars(argIndices[i]);
                if (var != null)
                {
                    _autoFillArgs[argIndices[i]] = var;
                }
            }

            //ImGui.Unindent();
            var currentResult = getCurrentStepText(valid, cmd.Item1, argIndices,
                _additionalCondition != null ? " && " : suffix);
            if (subResult != null && valid)
            {
                return currentResult + subResult;
            }
        }

        return null;
    }

    internal string getCurrentStepText(bool valid, string command, int[] argIndices, string suffixToUse)
    {
        if (!valid)
        {
            return null;
        }

        if (command != null)
        {
            var cmdText = _autoFillNotToggle ? '!' + command : command;
            for (var i = 0; i < argIndices.Length; i++)
            {
                cmdText += " " + _autoFillArgs[argIndices[i]];
            }

            return cmdText + suffixToUse;
        }

        if (argIndices.Length > 0)
        {
            var argText = _autoFillArgs[argIndices[0]];
            for (var i = 1; i < argIndices.Length; i++)
            {
                argText += " " + _autoFillArgs[argIndices[i]];
            }

            return argText + suffixToUse;
        }

        return null;
    }
}

internal class AutoFill
{
    private static readonly AutoFillSearchEngine autoFillPrsse = new("prsse", TypelessSearchEngine.paramRowSelection);
    private static readonly AutoFillSearchEngine autoFillPrcse = new("prcse", TypelessSearchEngine.paramRowClipboard);
    private static readonly AutoFillSearchEngine autoFillPse = new("pse", TypelessSearchEngine.paramRowSelection);
    private static readonly AutoFillSearchEngine autoFillRse = new("rse", TypelessSearchEngine.row);
    private static readonly AutoFillSearchEngine autoFillCse = new("cse", TypelessSearchEngine.cell);
    private static readonly AutoFillSearchEngine autoFillVse = new("vse", TypelessSearchEngine.var);

    private static string[] _autoFillArgsGop = Enumerable.Repeat("", MEGlobalOperation.globalOps.AllCommands().Sum((x) => x.Value.argNames.Length)).ToArray();
    private static string[] _autoFillArgsRop = Enumerable.Repeat("", MERowOperation.rowOps.AllCommands().Sum((x) => x.Value.argNames.Length)).ToArray();
    private static string[] _autoFillArgsCop = Enumerable.Repeat("", MECellOperation.cellOps.AllCommands().Sum((x) => x.Value.argNames.Length)).ToArray();
    private static string[] _autoFillArgsOa =
        Enumerable.Repeat("", MEOperationArgument.arg.AllArguments().Sum(x => x.Item2.Length)).ToArray();

    private static string _literalArg = "";

    internal static Vector4 HINTCOLOUR = new(0.3f, 0.5f, 1.0f, 1.0f);
    internal static Vector4 PREVIEWCOLOUR = new(0.65f, 0.75f, 0.65f, 1.0f);

    public static string ParamSearchBarAutoFill()
    {
        ImGui.SameLine();
        ImGui.Button($@"{ForkAwesome.CaretDown}");
        if (ImGui.BeginPopupContextItem("##psbautoinputoapopup", ImGuiPopupFlags.MouseButtonLeft))
        {
            ImGui.TextColored(HINTCOLOUR, "Select params...");
            var result = autoFillPse.Menu(true, false, "", null, null);
            ImGui.EndPopup();
            return result;
        }

        return null;
    }

    public static string RowSearchBarAutoFill()
    {
        ImGui.SameLine();
        ImGui.Button($@"{ForkAwesome.CaretDown}");
        if (ImGui.BeginPopupContextItem("##rsbautoinputoapopup", ImGuiPopupFlags.MouseButtonLeft))
        {
            ImGui.TextColored(HINTCOLOUR, "Select rows...");
            var result = autoFillRse.Menu(true, false, "", null, null);
            ImGui.EndPopup();
            return result;
        }

        return null;
    }

    public static string ColumnSearchBarAutoFill()
    {
        ImGui.SameLine();
        ImGui.Button($@"{ForkAwesome.CaretDown}");
        if (ImGui.BeginPopupContextItem("##csbautoinputoapopup", ImGuiPopupFlags.MouseButtonLeft))
        {
            ImGui.TextColored(HINTCOLOUR, "Select fields...");
            var result = autoFillCse.Menu(true, false, "", null, null);
            ImGui.EndPopup();
            return result;
        }

        return null;
    }

    public static string MassEditCompleteAutoFill()
    {
        ImGui.TextUnformatted("Add command...");
        ImGui.SameLine();
        ImGui.Button($@"{ForkAwesome.CaretDown}");
        if (ImGui.BeginPopupContextItem("##meautoinputoapopup", ImGuiPopupFlags.MouseButtonLeft))
        {
            /*special case*/
            ImGui.PushID("paramrowselection");
            ImGui.TextColored(HINTCOLOUR, "Select param and rows...");
            var result0 = autoFillPrsse.Menu(false, autoFillRse, false, ": ", null, inheritedCommand =>
            {
                if (inheritedCommand != null)
                {
                    ImGui.TextColored(PREVIEWCOLOUR, inheritedCommand);
                }

                ImGui.TextColored(HINTCOLOUR, "Select fields...");
                var res1 = autoFillCse.Menu(true, true, ": ", inheritedCommand, inheritedCommand2 =>
                {
                    if (inheritedCommand2 != null)
                    {
                        ImGui.TextColored(PREVIEWCOLOUR, inheritedCommand2);
                    }

                    ImGui.TextColored(HINTCOLOUR, "Select field operation...");
                    return MassEditAutoFillForOperation(MECellOperation.cellOps, ref _autoFillArgsCop, ";", null);
                });
                ImGui.Separator();
                ImGui.TextColored(HINTCOLOUR, "Select row operation...");
                var res2 = MassEditAutoFillForOperation(MERowOperation.rowOps, ref _autoFillArgsRop, ";", null);
                if (res1 != null)
                {
                    return res1;
                }

                return res2;
            });
            ImGui.PopID();
            ImGui.PushID("paramrowclipboard");
            var result1 = autoFillPrcse.Menu(false, autoFillRse, false, ": ", null, inheritedCommand =>
            {
                if (inheritedCommand != null)
                {
                    ImGui.TextColored(PREVIEWCOLOUR, inheritedCommand);
                }

                ImGui.TextColored(HINTCOLOUR, "Select fields...");
                var res1 = autoFillCse.Menu(true, true, ": ", inheritedCommand, inheritedCommand2 =>
                {
                    if (inheritedCommand2 != null)
                    {
                        ImGui.TextColored(PREVIEWCOLOUR, inheritedCommand2);
                    }

                    ImGui.TextColored(HINTCOLOUR, "Select field operation...");
                    return MassEditAutoFillForOperation(MECellOperation.cellOps, ref _autoFillArgsCop, ";", null);
                });
                ImGui.Separator();
                ImGui.TextColored(HINTCOLOUR, "Select row operation...");
                var res2 = MassEditAutoFillForOperation(MERowOperation.rowOps, ref _autoFillArgsRop, ";", null);
                if (res1 != null)
                {
                    return res1;
                }

                return res2;
            });
            ImGui.PopID();
            /*end special case*/
            ImGui.Separator();
            ImGui.PushID("param");
            ImGui.TextColored(HINTCOLOUR, "Select params...");
            var result2 = autoFillPse.Menu(true, false, ": ", null, inheritedCommand =>
            {
                if (inheritedCommand != null)
                {
                    ImGui.TextColored(PREVIEWCOLOUR, inheritedCommand);
                }

                ImGui.TextColored(HINTCOLOUR, "Select rows...");
                return autoFillRse.Menu(true, false, ": ", inheritedCommand, inheritedCommand2 =>
                {
                    if (inheritedCommand2 != null)
                    {
                        ImGui.TextColored(PREVIEWCOLOUR, inheritedCommand2);
                    }

                    ImGui.TextColored(HINTCOLOUR, "Select fields...");
                    var res1 = autoFillCse.Menu(true, true, ": ", inheritedCommand2, inheritedCommand3 =>
                    {
                        if (inheritedCommand3 != null)
                        {
                            ImGui.TextColored(PREVIEWCOLOUR, inheritedCommand3);
                        }

                        ImGui.TextColored(HINTCOLOUR, "Select field operation...");
                        return MassEditAutoFillForOperation(MECellOperation.cellOps, ref _autoFillArgsCop, ";",
                            null);
                    });
                    string res2 = null;
                    if (CFG.Current.Param_AdvancedMassedit)
                    {
                        ImGui.Separator();
                        ImGui.TextColored(HINTCOLOUR, "Select row operation...");
                        res2 = MassEditAutoFillForOperation(MERowOperation.rowOps, ref _autoFillArgsRop, ";", null);
                    }

                    if (res1 != null)
                    {
                        return res1;
                    }

                    return res2;
                });
            });
            ImGui.PopID();
            string result3 = null;
            string result4 = null;
            if (CFG.Current.Param_AdvancedMassedit)
            {
                ImGui.Separator();
                ImGui.PushID("globalop");
                ImGui.TextColored(HINTCOLOUR, "Select global operation...");
                result3 = MassEditAutoFillForOperation(MEGlobalOperation.globalOps, ref _autoFillArgsGop, ";",
                    null);
                ImGui.PopID();
                if (MassParamEdit.massEditVars.Count != 0)
                {
                    ImGui.Separator();
                    ImGui.PushID("var");
                    ImGui.TextColored(HINTCOLOUR, "Select variables...");
                    result4 = autoFillVse.Menu(false, false, ": ", null, inheritedCommand =>
                    {
                        if (inheritedCommand != null)
                        {
                            ImGui.TextColored(PREVIEWCOLOUR, inheritedCommand);
                        }

                        ImGui.TextColored(HINTCOLOUR, "Select value operation...");
                        return MassEditAutoFillForOperation(MECellOperation.cellOps, ref _autoFillArgsCop, ";",
                            null);
                    });
                }
            }

            ImGui.EndPopup();
            if (result1 != null)
            {
                return result1;
            }

            if (result2 != null)
            {
                return result2;
            }

            if (result3 != null)
            {
                return result3;
            }

            return result4;
        }

        return null;
    }

    public static string MassEditOpAutoFill()
    {
        return MassEditAutoFillForOperation(MECellOperation.cellOps, ref _autoFillArgsCop, ";", null);
    }

    private static string MassEditAutoFillForOperation<R, IO, IF, B>(MEOperation<R, IO, IF, B> ops, ref string[] staticArgs,
        string suffix, Func<string> subMenu)
    {
        var currentArgIndex = 0;
        string result = null;
        foreach (KeyValuePair<string, METypelessOperationDef> cmd in ops.AllCommands())
        {
            string cmdName = cmd.Key;
            METypelessOperationDef cmdData = cmd.Value;
            var argIndices = new int[cmdData.argNames.Length];
            var valid = true;
            for (var i = 0; i < argIndices.Length; i++)
            {
                argIndices[i] = currentArgIndex;
                currentArgIndex++;
                if (string.IsNullOrEmpty(staticArgs[argIndices[i]]))
                {
                    valid = false;
                }
            }
            if (cmdData.shouldShow != null && !cmdData.shouldShow())
                continue;

            var wiki = cmdData.wiki;
            UIHints.AddImGuiHintButton(cmdName, ref wiki, false, true);
            if (subMenu != null)
            {
                if (ImGui.BeginMenu(cmdName, valid))
                {
                    result = subMenu();
                    ImGui.EndMenu();
                }
            }
            else
            {
                result = ImGui.Selectable(cmdName, false,
                    valid ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled)
                    ? suffix
                    : null;
            }

            ImGui.Indent();
            for (var i = 0; i < argIndices.Length; i++)
            {
                if (i != 0)
                {
                    ImGui.SameLine();
                }

                ImGui.InputTextWithHint("##meautoinputop" + argIndices[i], cmdData.argNames[i],
                    ref staticArgs[argIndices[i]], 256);
                ImGui.SameLine();
                ImGui.Button($@"{ForkAwesome.CaretDown}");
                if (ImGui.BeginPopupContextItem("##meautoinputoapopup" + argIndices[i],
                        ImGuiPopupFlags.MouseButtonLeft))
                {
                    var opargResult = MassEditAutoFillForArguments(MEOperationArgument.arg, ref _autoFillArgsOa);
                    if (opargResult != null)
                    {
                        staticArgs[argIndices[i]] = opargResult;
                    }

                    ImGui.EndPopup();
                }
            }

            ImGui.Unindent();
            if (result != null && valid)
            {
                var argText = argIndices.Length > 0 ? staticArgs[argIndices[0]] : null;
                for (var i = 1; i < argIndices.Length; i++)
                {
                    argText += ":" + staticArgs[argIndices[i]];
                }

                result = cmdName + (argText != null ? " " + argText + result : result);
                return result;
            }
        }

        return result;
    }

    private static string MassEditAutoFillForArguments(MEOperationArgument oa, ref string[] staticArgs)
    {
        var currentArgIndex = 0;
        string result = null;
        foreach ((string, string, string[]) arg in oa.VisibleArguments())
        {
            var argIndices = new int[arg.Item3.Length];
            var valid = true;
            for (var i = 0; i < argIndices.Length; i++)
            {
                argIndices[i] = currentArgIndex;
                currentArgIndex++;
                if (string.IsNullOrEmpty(staticArgs[argIndices[i]]))
                {
                    valid = false;
                }
            }

            var selected = false;
            var wiki = arg.Item2;
            UIHints.AddImGuiHintButton(arg.Item1, ref wiki, false, true);
            if (ImGui.Selectable(arg.Item1, selected,
                    valid ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled))
            {
                result = arg.Item1;
                var argText = "";
                for (var i = 0; i < argIndices.Length; i++)
                {
                    argText += " " + staticArgs[argIndices[i]];
                }

                return arg.Item1 + argText;
            }

            ImGui.Indent();
            for (var i = 0; i < argIndices.Length; i++)
            {
                if (i != 0)
                {
                    ImGui.SameLine();
                }

                ImGui.InputTextWithHint("##meautoinputoa" + argIndices[i], arg.Item3[i],
                    ref staticArgs[argIndices[i]], 256);
                var var = MassEditAutoFillForVars(argIndices[i]);
                if (var != null)
                {
                    staticArgs[argIndices[i]] = var;
                }
            }

            ImGui.Unindent();
        }

        if (MassParamEdit.massEditVars.Count != 0)
        {
            ImGui.Separator();
            ImGui.TextUnformatted("Defined variables...");
            foreach (KeyValuePair<string, object> pair in MassParamEdit.massEditVars)
            {
                if (ImGui.Selectable(pair.Key + "(" + pair.Value + ")"))
                {
                    return '$' + pair.Key;
                }
            }
        }

        ImGui.Separator();
        if (ImGui.Selectable("Exactly..."))
        {
            result = '"' + _literalArg + '"';
        }

        ImGui.InputTextWithHint("##meautoinputoaExact", "literal value...", ref _literalArg, 256);
        return result;
    }

    internal static string MassEditAutoFillForVars(int id)
    {
        if (MassParamEdit.massEditVars.Count == 0)
        {
            return null;
        }

        ImGui.SameLine();
        ImGui.Button("$");
        if (ImGui.BeginPopupContextItem("##meautoinputvarpopup" + id, ImGuiPopupFlags.MouseButtonLeft))
        {
            ImGui.TextUnformatted("Defined variables...");
            ImGui.Separator();
            foreach (KeyValuePair<string, object> pair in MassParamEdit.massEditVars)
            {
                if (ImGui.Selectable(pair.Key + "(" + pair.Value + ")"))
                {
                    ImGui.EndPopup();
                    return '$' + pair.Key;
                }
            }

            ImGui.EndPopup();
        }

        return null;
    }
}
