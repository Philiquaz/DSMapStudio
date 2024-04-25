using Andre.Formats;
using static Andre.Native.ImGuiBindings;
using Microsoft.Extensions.Logging;
using SoulsFormats;
using StudioCore.Banks;
using StudioCore.Editor;
using StudioCore.ParamEditor;
using StudioCore.Scene;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace StudioCore.MsbEditor;

public class HavokPropertyEditor
{
    private readonly PropertyCache _propCache;


    private object _changingObject;
    private object _changingPropery;
    private Action _lastUncommittedAction;

    public ActionManager ContextActionManager;
    public FieldInfo RequestedSearchProperty = null;

    public HavokPropertyEditor(ActionManager manager, PropertyCache propCache)
    {
        ContextActionManager = manager;
        _propCache = propCache;
    }

    private (bool, bool) PropertyRow(Type typ, object oldval, out object newval, FieldInfo prop)
    {
        ImGui.SetNextItemWidth(-1);

        newval = null;
        var isChanged = false;
        if (typ == typeof(long))
        {
            var val = (long)oldval;
            var strval = $@"{val}";
            if (ImGui.InputText("##value", ref strval, 99))
            {
                var res = long.TryParse(strval, out val);
                if (res)
                {
                    newval = val;
                    isChanged = true;
                }
            }
        }
        else if (typ == typeof(int))
        {
            var val = (int)oldval;
            if (ImGui.InputInt("##value", ref val))
            {
                newval = val;
                isChanged = true;
            }
        }
        else if (typ == typeof(uint))
        {
            var val = (uint)oldval;
            var strval = $@"{val}";
            if (ImGui.InputText("##value", ref strval, 16))
            {
                var res = uint.TryParse(strval, out val);
                if (res)
                {
                    newval = val;
                    isChanged = true;
                }
            }
        }
        else if (typ == typeof(short))
        {
            int val = (short)oldval;
            if (ImGui.InputInt("##value", ref val))
            {
                newval = (short)val;
                isChanged = true;
            }
        }
        else if (typ == typeof(ushort))
        {
            var val = (ushort)oldval;
            var strval = $@"{val}";
            if (ImGui.InputText("##value", ref strval, 5))
            {
                var res = ushort.TryParse(strval, out val);
                if (res)
                {
                    newval = val;
                    isChanged = true;
                }
            }
        }
        else if (typ == typeof(sbyte))
        {
            int val = (sbyte)oldval;
            if (ImGui.InputInt("##value", ref val))
            {
                newval = (sbyte)val;
                isChanged = true;
            }
        }
        else if (typ == typeof(byte))
        {
            var val = (byte)oldval;
            var strval = $@"{val}";
            if (ImGui.InputText("##value", ref strval, 3))
            {
                var res = byte.TryParse(strval, out val);
                if (res)
                {
                    newval = val;
                    isChanged = true;
                }
            }
            /*
            // TODO: Set Next Unique Value
            // (needs prop search to scan through structs)
            if (obj != null && ImGui.BeginPopupContextItem(propname))
            {
                if (ImGui.Selectable("Set Next Unique Value"))
                {
                    newval = obj.Container.GetNextUnique(propname, val);
                    _forceCommit = true;
                    ImGui.EndPopup();
                    edited = true;
                }
                ImGui.EndPopup();
            }
            */
        }
        else if (typ == typeof(bool))
        {
            var val = (bool)oldval;
            if (ImGui.Checkbox("##value", ref val))
            {
                newval = val;
                isChanged = true;
            }
        }
        else if (typ == typeof(float))
        {
            var val = (float)oldval;
            if (ImGui.DragFloat("##value", ref val, 0.1f, float.MinValue, float.MaxValue,
                    Utils.ImGui_InputFloatFormat(val)))
            {
                newval = val;
                isChanged = true;
            }
        }
        else if (typ == typeof(string))
        {
            var val = (string)oldval;
            if (val == null)
            {
                val = "";
            }

            if (ImGui.InputText("##value", ref val, 99))
            {
                newval = val;
                isChanged = true;
            }
        }
        else if (typ == typeof(Vector2))
        {
            var val = (Vector2)oldval;
            if (ImGui.DragFloat2("##value", ref val, 0.1f))
            {
                newval = val;
                isChanged = true;
            }
        }
        else if (typ == typeof(Vector3))
        {
            var val = (Vector3)oldval;
            if (ImGui.DragFloat3("##value", ref val, 0.1f))
            {
                newval = val;
                isChanged = true;
            }
        }
        else if (typ.BaseType == typeof(Enum))
        {
            Array enumVals = typ.GetEnumValues();
            var enumNames = typ.GetEnumNames();
            var intVals = new int[enumVals.Length];

            if (typ.GetEnumUnderlyingType() == typeof(byte))
            {
                for (var i = 0; i < enumVals.Length; i++)
                {
                    intVals[i] = (byte)enumVals.GetValue(i);
                }

                if (Utils.EnumEditor(enumVals, enumNames, oldval, out var val, intVals))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else if (typ.GetEnumUnderlyingType() == typeof(int))
            {
                for (var i = 0; i < enumVals.Length; i++)
                {
                    intVals[i] = (int)enumVals.GetValue(i);
                }

                if (Utils.EnumEditor(enumVals, enumNames, oldval, out var val, intVals))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else if (typ.GetEnumUnderlyingType() == typeof(uint))
            {
                for (var i = 0; i < enumVals.Length; i++)
                {
                    intVals[i] = (int)(uint)enumVals.GetValue(i);
                }

                if (Utils.EnumEditor(enumVals, enumNames, oldval, out var val, intVals))
                {
                    newval = val;
                    isChanged = true;
                }
            }
            else
            {
                ImGui.Text("ImplementMe");
            }
        }
        else if (typ == typeof(Color))
        {
            var att = prop?.GetCustomAttribute<SupportsAlphaAttribute>();
            if (att != null)
            {
                if (att.Supports == false)
                {
                    var color = (Color)oldval;
                    Vector3 val = new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
                    if (ImGui.ColorEdit3("##value", ref val))
                    {
                        Color newColor = Color.FromArgb((int)(val.X * 255.0f), (int)(val.Y * 255.0f),
                            (int)(val.Z * 255.0f));
                        newval = newColor;
                        isChanged = true;
                    }
                }
                else
                {
                    var color = (Color)oldval;
                    Vector4 val = new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                    if (ImGui.ColorEdit4("##value", ref val))
                    {
                        Color newColor = Color.FromArgb((int)(val.W * 255.0f), (int)(val.X * 255.0f),
                            (int)(val.Y * 255.0f), (int)(val.Z * 255.0f));
                        newval = newColor;
                        isChanged = true;
                    }
                }
            }
            else
            {
                // SoulsFormats does not define if alpha should be exposed. Expose alpha by default.
                TaskLogs.AddLog(
                    $"Color property in \"{prop.DeclaringType}\" does not declare if it supports Alpha. Alpha will be exposed by default",
                    LogLevel.Warning, TaskLogs.LogPriority.Low);

                var color = (Color)oldval;
                Vector4 val = new(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                if (ImGui.ColorEdit4("##value", ref val))
                {
                    Color newColor = Color.FromArgb((int)(val.W * 255.0f), (int)(val.X * 255.0f),
                        (int)(val.Y * 255.0f), (int)(val.Z * 255.0f));
                    newval = newColor;
                    isChanged = true;
                }
            }
        }
        else
        {
            ImGui.Text("ImplementMe");
        }

        var isDeactivatedAfterEdit = ImGui.IsItemDeactivatedAfterEdit() || !ImGui.IsAnyItemActive();

        return (isChanged, isDeactivatedAfterEdit);
    }

    // Many parameter options, which may be simplified.
    private void PropEditorPropInfoRow(object rowOrWrappedObject, FieldInfo prop, string visualName, ref int id,
        Entity nullableSelection)
    {
        PropEditorPropRow(prop.GetValue(rowOrWrappedObject), ref id, visualName, prop.FieldType, null, null,
            prop, rowOrWrappedObject, nullableSelection);
    }

    private void PropEditorPropRow(object oldval, ref int id, string visualName, Type propType,
        Entity nullableEntity, string nullableName, FieldInfo proprow, object paramRowOrCell,
        Entity nullableSelection)
    {
        ImGui.PushID(id);
        ImGui.AlignTextToFramePadding();
        ImGui.Text(visualName);
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);

        object newval;
        // Property Editor UI
        (bool, bool) propEditResults = PropertyRow(propType, oldval, out newval, proprow);
        var changed = propEditResults.Item1;
        var committed = propEditResults.Item2;
        //UpdateProperty(proprow, nullableSelection, paramRowOrCell, newval, changed, committed);
        ImGui.NextColumn();
        ImGui.PopID();
        id++;
    }

    /// <summary>
    /// Overlays ImGui selectable over prop name text for use as a selectable.
    /// </summary>
    private static void PropContextRowOpener()
    {
        ImGui.Selectable("", false, ImGuiSelectableFlags.AllowOverlap);
        ImGui.SameLine();
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("MsbPropContextMenu");
        }
    }

    /// <summary>
    /// Displays property context menu.
    /// </summary>
    private void DisplayPropContextMenu(FieldInfo prop, object obj)
    {
    }

    private void PropEditorFlverLayout(Entity selection, FLVER2.BufferLayout layout)
    {
        foreach (FLVER.LayoutMember l in layout)
        {
            ImGui.Text(l.Semantic.ToString());
            ImGui.NextColumn();
            ImGui.Text(l.Type.ToString());
            ImGui.NextColumn();
        }
    }

    private void PropEditorGeneric(object selection)
    {
        var scale = MapStudioNew.GetUIScale();
        var obj = selection;
        Type type = obj.GetType();

        FieldInfo[] properties = type.GetFields();
        

        ImGui.Columns(2);
        ImGui.Separator();
        ImGui.Text("Object Type");
        ImGui.NextColumn();
        ImGui.Text(type.Name);
        ImGui.NextColumn();

        // Custom editors
        var id = 0;
        foreach (FieldInfo prop in properties)
        {
            ImGui.PushID(id);
            ImGui.AlignTextToFramePadding();
            Type typ = prop.FieldType;

            if (typ.IsArray)
            {
                var a = (Array)prop.GetValue(obj);
                for (var i = 0; i < a.Length; i++)
                {
                    ImGui.PushID(i);

                    Type arrtyp = typ.GetElementType();
                    if (arrtyp.IsClass && arrtyp != typeof(string) && !arrtyp.IsArray)
                    {
                        var open = ImGui.TreeNodeEx($@"{prop.Name}[{i}]");
                        ImGui.NextColumn();
                        ImGui.SetNextItemWidth(-1);
                        var o = a.GetValue(i);
                        ImGui.Text(o.GetType().Name);
                        ImGui.NextColumn();
                        if (open)
                        {
                            PropEditorGeneric(selection);
                            ImGui.TreePop();
                        }

                        ImGui.PopID();
                    }
                    else
                    {
                        PropContextRowOpener();
                        ImGui.Text($@"{prop.Name}[{i}]");
                        ImGui.NextColumn();
                        ImGui.SetNextItemWidth(-1);
                        var oldval = a.GetValue(i);
                        object newval = null;

                        // Property Editor UI
                        (bool, bool) propEditResults =
                            PropertyRow(typ.GetElementType(), oldval, out newval, prop);
                        var changed = propEditResults.Item1;
                        var committed = propEditResults.Item2;
                        DisplayPropContextMenu(prop, obj);
                        if (ImGui.IsItemActive() && !ImGui.IsWindowFocused(0))
                        {
                            ImGui.SetItemDefaultFocus();
                        }

                        //UpdateProperty(prop, entSelection, newval, changed, committed, i);

                        ImGui.NextColumn();
                        ImGui.PopID();
                    }
                }
            }
            else if (typ.IsGenericType && typ.GetGenericTypeDefinition() == typeof(List<>))
            {
                var l = prop.GetValue(obj);
                if (l != null)
                {
                    PropertyInfo itemprop = l.GetType().GetProperty("Item");
                    var count = (int)l.GetType().GetProperty("Count").GetValue(l);
                    for (var i = 0; i < count; i++)
                    {
                        ImGui.PushID(i);

                        Type arrtyp = typ.GetGenericArguments()[0];
                        if (arrtyp.IsClass && arrtyp != typeof(string) && !arrtyp.IsArray)
                        {
                            var o = itemprop.GetValue(l, new object[] { i });
                            if (o != null)
                            {
                                var open = ImGui.TreeNodeEx($@"{prop.Name}[{i}]");
                                ImGui.NextColumn();
                                ImGui.SetNextItemWidth(-1);
                                ImGui.Text(o.GetType().Name);
                                ImGui.NextColumn();
                                if (open)
                                {
                                    PropEditorGeneric(o);
                                    ImGui.TreePop();
                                }
                            }
                        }
                        else
                        {
                            PropContextRowOpener();
                            ImGui.Text($@"{prop.Name}[{i}]");
                            ImGui.NextColumn();
                            ImGui.SetNextItemWidth(-1);
                            var oldval = itemprop.GetValue(l, new object[] { i });
                            object newval = null;

                            // Property Editor UI
                            (bool, bool) propEditResults = PropertyRow(arrtyp, oldval, out newval, prop);
                            var changed = propEditResults.Item1;
                            var committed = propEditResults.Item2;
                            DisplayPropContextMenu(prop, obj);
                            if (ImGui.IsItemActive() && !ImGui.IsWindowFocused(0))
                            {
                                ImGui.SetItemDefaultFocus();
                            }

                            //UpdateProperty(prop, entSelection, newval, changed, committed, i, classIndex);

                            ImGui.NextColumn();
                        }

                        ImGui.PopID();
                    }
                }
            }
            else if (typ.IsClass && typ != typeof(string) && !typ.IsArray)
            {
                var o = prop.GetValue(obj);
                if (o != null)
                {
                    var open = ImGui.TreeNodeEx(prop.Name);
                    ImGui.NextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.Text(o.GetType().Name);
                    ImGui.NextColumn();
                    if (open)
                    {
                        PropEditorGeneric(o);
                        ImGui.TreePop();
                    }
                }
            }
            else
            {
                PropContextRowOpener();
                ImGui.Text(prop.Name);
                ImGui.NextColumn();
                ImGui.SetNextItemWidth(-1);
                var oldval = prop.GetValue(obj);
                object newval = null;

                // Property Editor UI
                (bool, bool) propEditResults = PropertyRow(typ, oldval, out newval, prop);
                var changed = propEditResults.Item1;
                var committed = propEditResults.Item2;
                DisplayPropContextMenu(prop, obj);
                if (ImGui.IsItemActive() && !ImGui.IsWindowFocused(0))
                {
                    ImGui.SetItemDefaultFocus();
                }

                //UpdateProperty(prop, entSelection, newval, changed, committed, -1);

                ImGui.NextColumn();
            }

            id++;
            ImGui.PopID();
        }
    }

    public unsafe void OnGui(object selection, string id, float w, float h)
    {
        var scale = MapStudioNew.GetUIScale();

        ImGui.PushStyleColorVec4(ImGuiCol.ChildBg, new Vector4(0.145f, 0.145f, 0.149f, 1.0f));
        ImGui.SetNextWindowSize(new Vector2(350, h - 80) * scale, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(w - 370, 20) * scale, ImGuiCond.FirstUseEver, default);
        ImGui.BeginChild("propedit");
        
        ImGui.Text($" Map: {selection.GetType().Name}");
        PropEditorGeneric(selection);

        ImGui.EndChild();
        ImGui.PopStyleColor(1);
    }
}
