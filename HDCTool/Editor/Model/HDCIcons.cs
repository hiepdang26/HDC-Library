using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCIcons
    {
        internal static Texture2D DefaultIcon
        {
            get => PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any).FirstOrDefault(texture => texture != null);
            set
            {
                Object settings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
                if (settings != null)
                    Undo.RecordObject(settings, "Default Icon");
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { value }, IconKind.Any);
            }
        }
    }
}
