﻿using System;
using System.Linq;
using FSParam;
using ImGuiNET;
using StudioCore.Editor;

namespace StudioCore.ParamEditor
{
    class AutoFillSearchEngine<A, B>
    {
        string id;
        SearchEngine<A, B> engine;
        private string[] _autoFillArgs;
        private bool _autoFillNotToggle;
        private bool _useAdditionalCondition;
        private AutoFillSearchEngine<A, B> _additionalCondition;
        internal AutoFillSearchEngine(string id, SearchEngine<A, B> searchEngine)
        {
            this.id = id;
            engine = searchEngine;
            _autoFillArgs = Enumerable.Repeat("", engine.AvailableCommands().Sum((x) => x.Item2.Length) + engine.defaultFilter.Item1.Length).ToArray();
            _autoFillNotToggle = false;
            _useAdditionalCondition = false;
            _additionalCondition = null;
        }
        internal string Menu(bool enableDefault, string suffix, Func<string> subMenu)
        {
            int currentArgIndex = 0;
            string result = null;
            ImGui.Checkbox("Invert selection?##meautoinputnottoggle"+id, ref _autoFillNotToggle);
            ImGui.SameLine();
            ImGui.Checkbox("Add another condition?##meautoinputadditionalcondition"+id, ref _useAdditionalCondition);
            if (_useAdditionalCondition && _additionalCondition == null)
                _additionalCondition = new AutoFillSearchEngine<A, B>(id+"0", engine);
            else if (!_useAdditionalCondition)
                _additionalCondition = null;
            foreach (var cmd in enableDefault ? engine.AvailableCommands().Append((null, engine.defaultFilter.Item1)).ToList() : engine.AvailableCommands())
            {
                int[] argIndices = new int[cmd.Item2.Length];
                bool valid = true;
                for (int i = 0; i < argIndices.Length; i++)
                {
                    argIndices[i] = currentArgIndex;
                    currentArgIndex++;
                    if (string.IsNullOrEmpty(_autoFillArgs[argIndices[i]]))
                        valid = false;
                }
                if (subMenu != null || _additionalCondition != null)
                {
                    if (ImGui.BeginMenu(cmd.Item1 == null ? "Default filter..." : cmd.Item1, valid))
                    {
                        if (_additionalCondition != null)
                            result = _additionalCondition.Menu(enableDefault, suffix, subMenu);
                        else
                            result = subMenu();
                        ImGui.EndMenu();
                    }
                }
                else
                {
                    result = ImGui.Selectable(cmd.Item1 == null ? "Default filter..." : cmd.Item1, false, valid ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled) ? suffix : null;
                }
                //ImGui.Indent();
                for (int i = 0; i < argIndices.Length; i++)
                {
                    if (i != 0)
                        ImGui.SameLine();
                    ImGui.InputTextWithHint("##meautoinput"+argIndices[i], cmd.Item2[i], ref _autoFillArgs[argIndices[i]], 256);
                    string var = AutoFill.MassEditAutoFillForVars(argIndices[i]);
                    if (var != null)
                        _autoFillArgs[argIndices[i]] = var;
                }
                //ImGui.Unindent();
                string suffixToUse = _additionalCondition != null ? " && " : suffix;

                if (result != null && valid)
                {
                    if (cmd.Item1 != null)
                    {
                        string cmdText = _autoFillNotToggle ? '!' + cmd.Item1 : cmd.Item1;
                        for (int i = 0; i < argIndices.Length; i++)
                            cmdText += " " + _autoFillArgs[argIndices[i]];
                        result = cmdText + suffixToUse + result;
                    }
                    else if (argIndices.Length > 0)
                    {
                        string argText = _autoFillArgs[argIndices[0]];
                        for (int i = 1; i < argIndices.Length; i++)
                            argText += " " + _autoFillArgs[argIndices[i]];
                        result = argText + suffixToUse + result;
                    }
                    return result;
                }
            }
            return result;
        }
    }

    class AutoFill
    {
        // Type hell. Can't omit the type.
        static AutoFillSearchEngine<ParamEditorSelectionState, (MassEditRowSource, Param.Row)> autoFillParse = new ("parse", ParamAndRowSearchEngine.parse);
        static AutoFillSearchEngine<bool, string> autoFillVse = new ("vse", VarSearchEngine.vse);
        static AutoFillSearchEngine<bool, (ParamBank, Param)> autoFillPse = new ("pse", ParamSearchEngine.pse);
        static AutoFillSearchEngine<(ParamBank, Param), Param.Row> autoFillRse = new ("rse", RowSearchEngine.rse);
        static AutoFillSearchEngine<(string, Param.Row), (PseudoColumn, Param.Column)> autoFillCse = new ("cse", CellSearchEngine.cse);
        private static string[] _autoFillArgsGop = Enumerable.Repeat("", MEGlobalOperation.globalOps.AvailableCommands().Sum((x) => x.Item2.Length)).ToArray();
        private static string[] _autoFillArgsRop = Enumerable.Repeat("", MERowOperation.rowOps.AvailableCommands().Sum((x) => x.Item2.Length)).ToArray();
        private static string[] _autoFillArgsCop = Enumerable.Repeat("", MEValueOperation.valueOps.AvailableCommands().Sum((x) => x.Item2.Length)).ToArray();
        private static string[] _autoFillArgsOa = Enumerable.Repeat("", MEOperationArgument.arg.AvailableArguments().Sum((x) => x.Item2.Length)).ToArray();
        private static string _literalArg = "";
        
        public static string ParamSearchBarAutoFill()
        {
            ImGui.SameLine();
            ImGui.Button($@"{ForkAwesome.CaretDown}");
            if (ImGui.BeginPopupContextItem("##rsbautoinputoapopup", ImGuiPopupFlags.MouseButtonLeft))
            {
                ImGui.TextUnformatted("Select params...");
                var result = autoFillPse.Menu(false, "", null);
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
                ImGui.TextUnformatted("Select rows...");
                var result = autoFillRse.Menu(false, "", null);
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
                ImGui.TextUnformatted("Select fields...");
                var result = autoFillCse.Menu(false, "", null);
                ImGui.EndPopup();
                return result;
            }
            return null;
        }

        public static string MassEditAutoFill()
        {
            ImGui.TextUnformatted("Add command...");
            ImGui.SameLine();
            ImGui.Button($@"{ForkAwesome.CaretDown}");
            if (ImGui.BeginPopupContextItem("##meautoinputoapopup", ImGuiPopupFlags.MouseButtonLeft))
            {
                ImGui.PushID("paramrow");
                ImGui.TextUnformatted("Select param and rows...");
                string result1 = autoFillParse.Menu(false, ": ", () => 
                {
                    ImGui.TextUnformatted("Select fields...");
                    string res1 = autoFillCse.Menu(true, ": ", () => 
                    {
                        ImGui.TextUnformatted("Select field operation...");
                        return MassEditAutoFillForOperation(MEValueOperation.valueOps, ref _autoFillArgsCop, ";", null);
                    });
                    ImGui.Separator();
                    ImGui.TextUnformatted("Select row operation...");
                    string res2 = MassEditAutoFillForOperation(MERowOperation.rowOps, ref _autoFillArgsRop, ";", null);
                    if (res1 != null)
                        return res1;
                    return res2;
                });
                ImGui.PopID();
                ImGui.Separator();
                ImGui.PushID("param");
                ImGui.TextUnformatted("Select params...");
                string result2 = autoFillPse.Menu(false, ": ", () =>
                {
                    ImGui.TextUnformatted("Select rows...");
                    return autoFillRse.Menu(false, ": ", () => 
                    {
                        ImGui.TextUnformatted("Select fields...");
                        string res1 = autoFillCse.Menu(true, ": ", () => 
                        {
                            ImGui.TextUnformatted("Select field operation...");
                            return MassEditAutoFillForOperation(MEValueOperation.valueOps, ref _autoFillArgsCop, ";", null);
                        });
                        ImGui.Separator();
                        ImGui.TextUnformatted("Select row operation...");
                        string res2 = MassEditAutoFillForOperation(MERowOperation.rowOps, ref _autoFillArgsRop, ";", null);
                        if (res1 != null)
                            return res1;
                        return res2;
                    });
                });
                ImGui.PopID();
                ImGui.Separator();
                ImGui.PushID("globalop");
                ImGui.TextUnformatted("Select global operation...");
                string result3 = MassEditAutoFillForOperation(MEGlobalOperation.globalOps, ref _autoFillArgsGop, ";", null);
                ImGui.PopID();
                string result4 = null;
                if (MassParamEdit.massEditVars.Count != 0)
                {
                    ImGui.Separator();
                    ImGui.PushID("var");
                    ImGui.TextUnformatted("Select variables...");
                    result4 = autoFillVse.Menu(false, ": ", () => 
                    {
                        ImGui.TextUnformatted("Select value operation...");
                        return MassEditAutoFillForOperation(MEValueOperation.valueOps, ref _autoFillArgsCop, ";", null);
                    });
                }
                ImGui.EndPopup();
                if (result1 != null)
                    return result1;
                if (result2 != null)
                    return result2;
                if (result3 != null)
                    return result3;
                return result4;
            }
            return null;
        }
        private static string MassEditAutoFillForOperation<A, B> (MEOperation<A, B> ops, ref string[] staticArgs, string suffix, Func<string> subMenu)
        {
            int currentArgIndex = 0;
            string result = null;
            foreach (var cmd in ops.AvailableCommands())
            {
                int[] argIndices = new int[cmd.Item2.Length];
                bool valid = true;
                for (int i = 0; i < argIndices.Length; i++)
                {
                    argIndices[i] = currentArgIndex;
                    currentArgIndex++;
                    if (string.IsNullOrEmpty(staticArgs[argIndices[i]]))
                        valid = false;
                }
                if (subMenu != null)
                {
                    if (ImGui.BeginMenu(cmd.Item1, valid))
                    {
                        result = subMenu();
                        ImGui.EndMenu();
                    }
                }
                else
                {
                    result = ImGui.Selectable(cmd.Item1, false, valid ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled) ? suffix : null;
                }
                ImGui.Indent();
                for (int i = 0; i < argIndices.Length; i++)
                {
                    if (i != 0)
                        ImGui.SameLine();
                    ImGui.InputTextWithHint("##meautoinputop"+argIndices[i], cmd.Item2[i], ref staticArgs[argIndices[i]], 256);
                    ImGui.SameLine();
                    ImGui.Button($@"{ForkAwesome.CaretDown}");
                    if (ImGui.BeginPopupContextItem("##meautoinputoapopup"+argIndices[i], ImGuiPopupFlags.MouseButtonLeft))
                    {
                        string opargResult = MassEditAutoFillForArguments(MEOperationArgument.arg, ref _autoFillArgsOa);
                        if (opargResult != null)
                            staticArgs[argIndices[i]] = opargResult;
                        ImGui.EndPopup();
                    }

                }
                ImGui.Unindent();
                if (result != null && valid)
                {
                    string argText = argIndices.Length > 0 ? staticArgs[argIndices[0]] : null;
                    for (int i = 1; i < argIndices.Length; i++)
                        argText += ":" + staticArgs[argIndices[i]];
                    result = cmd.Item1 + (argText != null ? (" " + argText + result) : result);
                    return result;
                }
            }
            return result;
        }

        private static string MassEditAutoFillForArguments(MEOperationArgument oa, ref string[] staticArgs)
        {
            int currentArgIndex = 0;
            string result = null;
            foreach (var arg in oa.AvailableArguments())
            {
                int[] argIndices = new int[arg.Item2.Length];
                bool valid = true;
                for (int i = 0; i < argIndices.Length; i++)
                {
                    argIndices[i] = currentArgIndex;
                    currentArgIndex++;
                    if (string.IsNullOrEmpty(staticArgs[argIndices[i]]))
                        valid = false;
                }
                bool selected = false;
                if (ImGui.Selectable(arg.Item1, selected, valid ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled))
                {
                    result = arg.Item1;
                    string argText = "";
                    for (int i = 0; i < argIndices.Length; i++)
                        argText += " " + staticArgs[argIndices[i]];
                    return arg.Item1 + argText;
                }
                ImGui.Indent();
                for (int i = 0; i < argIndices.Length; i++)
                {
                    if (i != 0)
                        ImGui.SameLine();
                    ImGui.InputTextWithHint("##meautoinputoa"+argIndices[i], arg.Item2[i], ref staticArgs[argIndices[i]], 256);
                    string var = AutoFill.MassEditAutoFillForVars(argIndices[i]);
                    if (var != null)
                        staticArgs[argIndices[i]] = var;
                }
                ImGui.Unindent();
            }
            if (MassParamEdit.massEditVars.Count != 0)
            {
                ImGui.Separator();
                ImGui.TextUnformatted("Defined variables...");
                foreach (var pair in MassParamEdit.massEditVars)
                {
                    if (ImGui.Selectable(pair.Key + "("+pair.Value+")"))
                        return '$'+pair.Key;
                }
            }
            ImGui.Separator();
            if (ImGui.Selectable("Exactly..."))
                result = '"'+_literalArg+'"';
            ImGui.InputTextWithHint("##meautoinputoaExact", "literal value...", ref _literalArg, 256);
            return result;
        }
        internal static string MassEditAutoFillForVars(int id)
        {
            if (MassParamEdit.massEditVars.Count == 0)
                return null;
            ImGui.SameLine();
            ImGui.Button($@"{ForkAwesome.CaretDown}");
            if (ImGui.BeginPopupContextItem("##meautoinputvarpopup"+id, ImGuiPopupFlags.MouseButtonLeft))
            {
                ImGui.TextUnformatted("Defined variables...");
                ImGui.Separator();
                foreach (var pair in MassParamEdit.massEditVars)
                {
                    if (ImGui.Selectable(pair.Key + "("+pair.Value+")"))
                    {
                        ImGui.EndPopup();
                        return '$'+pair.Key;
                    }
                }
                ImGui.EndPopup();
            }
            return null;
        }
    }
}
