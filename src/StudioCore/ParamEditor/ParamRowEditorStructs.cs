using Andre.Formats;
using static Andre.Native.ImGuiBindings;
using SoulsFormats;
using StudioCore.Editor;
using StudioCore.Editor.MassEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using DotNext.Collections.Specialized;
using StudioCore.TextEditor;

namespace StudioCore.ParamEditor;

public partial class ParamRowEditor
{
    internal struct PropertyRowEntry<T>
    {
        internal int index;
        internal bool isDummy;
        internal FieldInfoEntry field;
        internal CellInfoEntry<T> cell;
        internal CellInfoEntry<T> vanilla;
        internal CellInfoEntry<T>[] aux;
        internal CellInfoEntry<T> compare;
    }
    internal struct FieldInfoEntry
    {
        internal Param.Column? col;
        internal FieldMetaData meta;
        internal Type propType;
        internal PropertyInfo proprow;
        internal string displayText;
        internal string internalName;
        internal string wiki;
        internal bool displayBool;
        internal FieldReferences references;
    }
    internal partial struct FieldReferences
    {
        internal bool isParamRef;
        internal string inactiveParamRefText;
        internal string activeParamRefText;
        internal bool isFMGRef;
        internal string inactiveFmgRefText;
        internal string activeFmgRefText;
        internal bool isEnum;
        internal string enumText;
        internal string extRefText;
        internal string virtualRef;
        internal bool isRef => isParamRef || isFMGRef; //Fix this
    }
    internal struct CellInfoEntry<T>
    {
        internal bool isNull;
        internal T obj;
        internal Param.Row row; //Still here for legacy reasons
        internal object oldval;
        internal bool diffVanilla;
        internal bool conflictOrDiffPrimary;
        internal bool matchDefault;
        internal CellReferences references;
    }
    internal partial struct CellReferences
    {
        internal string paramRefText;
        internal string fmgRefText;
        internal string enumText;
    }
    internal struct ParamRowEditorContextObject : EditorContextObject<Param.Row>
    {
        Param.Row row;
        public static EditorContextObject<Param.Row> of(Param.Row obj)
        {
            return new ParamRowEditorContextObject{row = obj};
        }
        public object getValueForKey(string key)
        {
            Param.Column col = row.Columns.FirstOrDefault(cell => cell.Def.InternalName == key);
            if (col == null)
                return null;
            return row[col].Value;
        }
    }
}
