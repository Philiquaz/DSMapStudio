using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Numerics;
using SoulsFormats;
using ImGuiNET;
using Veldrid;
using System.Net.Http.Headers;
using System.Security;
using System.Text.RegularExpressions;
using FSParam;
using StudioCore;
using StudioCore.Editor;

namespace StudioCore.ParamEditor
{
    public interface IDataOwningRoot<C,O,I,F,H>
    {
        public bool DataLoaded();
        public IEnumerable<(string, C)> GetPrincipleObjectCategories();
        public IEnumerable<O> GetPrincipleObjects(C categoryObject);
        public O GetPrincipleObject(C categoryObject, object identifier);
        public IEnumerable<I> GetValueIndexers(O principleObject);
        public H GetValueHolder(O principleObject, I valueIndexer);
        public string GetObjectName(O principleObject);
        public IEnumerable<F> GetValueIndexArchetypes(C categoryObject);
    }
}