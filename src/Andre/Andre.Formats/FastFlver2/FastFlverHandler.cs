#nullable enable

using System.Runtime.CompilerServices;

namespace Andre.Formats;
public interface IFastFlverHandler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeforeReadingVertices(FlverSubMesh mesh);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AfterReadingVertices(FlverSubMesh mesh);
}
