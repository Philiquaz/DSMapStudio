using Andre.Formats;
using static Andre.Native.ImGuiBindings;
using SoulsFormats;
using StudioCore.ParamEditor;
using StudioCore.TextEditor;
using StudioCore.Utilities;
using StudioCore.Editor.MassEdit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace StudioCore.Editor;

public interface EditorReference
{

}

public interface EditorContextObject<T>
{
    static abstract EditorContextObject<T> of(T obj);
    object getValueForKey(string key);
}
