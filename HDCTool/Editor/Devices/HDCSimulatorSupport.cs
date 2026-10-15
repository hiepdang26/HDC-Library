using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace HDC.Tool.Editor
{
    internal enum HDCArm64Code
    {
        None,
        Simulator,
        Device,
        Unknown,
    }

    internal static class HDCSimulatorSupport
    {
        internal const string LibrariesFolder = "Libraries";

        private const uint FatMagic = 0xCAFEBABE;
        private const uint MachO64Magic = 0xFEEDFACF;
        private const uint CpuArm64 = 0x0100000C;
        private const uint BuildVersionCommand = 0x32;
        private const uint DevicePlatform = 2;
        private const uint SimulatorPlatform = 7;
        private const int MemberHeader = 60;
        private static readonly byte[] ArchiveMagic = Encoding.ASCII.GetBytes("!<arch>\n");

        private static readonly Dictionary<string, (DateTime Changed, HDCArm64Code Code)> Cache = new Dictionary<string, (DateTime, HDCArm64Code)>();
        private static Dictionary<string, HDCArm64Code> lastFound = new Dictionary<string, HDCArm64Code>();
        private static bool stale = true;
        private static bool listening;

        internal static List<string> DeviceOnlyLibraries() => Libraries(HDCArm64Code.Device);

        internal static List<string> BlockingLibraries() => Libraries(HDCArm64Code.Unknown);

        internal static int Scans { get; private set; }

        internal static void Rescan() => stale = true;

        internal static HDCArm64Code Inspect(byte[] library)
        {
            bool any = false;
            bool device = false;
            bool unknown = false;
            foreach (int machO in Arm64Objects(library))
            {
                any = true;
                uint[] platforms = PlatformFields(library, machO).Select(field => Read(library, field)).ToArray();
                if (platforms.Contains(SimulatorPlatform))
                    return HDCArm64Code.Simulator;
                if (platforms.Length > 0 && platforms.All(platform => platform == DevicePlatform))
                    device = true;
                else
                    unknown = true;
            }

            return !any ? HDCArm64Code.None : unknown ? HDCArm64Code.Unknown : device ? HDCArm64Code.Device : HDCArm64Code.None;
        }

        internal static int MarkForSimulator(byte[] library)
        {
            if (Inspect(library) != HDCArm64Code.Device)
                return 0;
            int marked = 0;
            foreach (int machO in Arm64Objects(library).ToList())
            {
                foreach (int field in PlatformFields(library, machO).ToList())
                {
                    Write(library, field, SimulatorPlatform);
                    marked++;
                }
            }

            return marked;
        }

        internal static List<string> MarkExportForSimulator(string exportFolder)
        {
            var marked = new List<string>();
            string libraries = Path.Combine(exportFolder ?? "", LibrariesFolder);
            if (!Directory.Exists(libraries))
                return marked;
            foreach (string file in Directory.EnumerateFiles(libraries, "*.a", SearchOption.AllDirectories))
            {
                byte[] bytes = File.ReadAllBytes(file);
                if (MarkForSimulator(bytes) == 0)
                    continue;
                File.WriteAllBytes(file, bytes);
                marked.Add(Path.GetFileName(file));
            }

            return marked.Distinct().OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        private static List<string> Libraries(HDCArm64Code code)
        {
            if (!listening)
            {
                listening = true;
                EditorApplication.projectChanged += Rescan;
            }

            if (stale)
            {
                lastFound = Scan();
                stale = false;
            }

            return lastFound.Where(pair => pair.Value == code).Select(pair => pair.Key).OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        private static Dictionary<string, HDCArm64Code> Scan()
        {
            Scans++;
            var found = new Dictionary<string, HDCArm64Code>(StringComparer.Ordinal);
            foreach (PluginImporter importer in PluginImporter.GetAllImporters())
            {
                string path = importer.assetPath;
                if (!path.EndsWith(".a", StringComparison.OrdinalIgnoreCase) || !importer.GetCompatibleWithPlatform(BuildTarget.iOS) || !File.Exists(path))
                    continue;
                found[Path.GetFileName(path)] = Code(Path.GetFullPath(path));
            }

            return found;
        }

        private static HDCArm64Code Code(string file)
        {
            DateTime changed = File.GetLastWriteTimeUtc(file);
            if (Cache.TryGetValue(file, out (DateTime Changed, HDCArm64Code Code) known) && known.Changed == changed)
                return known.Code;
            HDCArm64Code code;
            try
            {
                code = Inspect(File.ReadAllBytes(file));
            }
            catch (IOException)
            {
                code = HDCArm64Code.Unknown;
            }

            Cache[file] = (changed, code);
            return code;
        }

        private static IEnumerable<int> Arm64Objects(byte[] bytes)
        {
            foreach ((int offset, int length) in Archives(bytes))
            {
                foreach (int machO in Objects(bytes, offset, length))
                {
                    if (Read(bytes, machO + 4) == CpuArm64)
                        yield return machO;
                }
            }
        }

        private static IEnumerable<(int Offset, int Length)> Archives(byte[] bytes)
        {
            if (bytes.Length < 8 || ReadBig(bytes, 0) != FatMagic)
            {
                yield return (0, bytes.Length);
                yield break;
            }

            uint count = ReadBig(bytes, 4);
            for (int i = 0; i < count && 8 + i * 20 + 20 <= bytes.Length; i++)
            {
                int entry = 8 + i * 20;
                if (ReadBig(bytes, entry) == CpuArm64)
                    yield return ((int)ReadBig(bytes, entry + 8), (int)ReadBig(bytes, entry + 12));
            }
        }

        private static IEnumerable<int> Objects(byte[] bytes, int offset, int length)
        {
            int end = Math.Min(bytes.Length, offset + length);
            if (offset + 4 <= end && Read(bytes, offset) == MachO64Magic)
            {
                yield return offset;
                yield break;
            }

            if (offset + ArchiveMagic.Length > end || !ArchiveMagic.Select((value, i) => bytes[offset + i] == value).All(same => same))
                yield break;
            int position = offset + ArchiveMagic.Length;
            while (position + MemberHeader <= end)
            {
                string name = Encoding.ASCII.GetString(bytes, position, 16).Trim();
                if (!int.TryParse(Encoding.ASCII.GetString(bytes, position + 48, 10).Trim(), out int size) || size < 0)
                    yield break;
                int data = position + MemberHeader;
                if (name.StartsWith("#1/", StringComparison.Ordinal) && int.TryParse(name.Substring(3), out int nameLength))
                    data += nameLength;
                if (data + 32 <= end && Read(bytes, data) == MachO64Magic)
                    yield return data;
                position += MemberHeader + size;
                if ((position - offset) % 2 != 0)
                    position++;
            }
        }

        private static IEnumerable<int> PlatformFields(byte[] bytes, int machO)
        {
            uint count = Read(bytes, machO + 16);
            int command = machO + 32;
            for (int i = 0; i < count && command + 12 <= bytes.Length; i++)
            {
                uint kind = Read(bytes, command);
                uint size = Read(bytes, command + 4);
                if (kind == BuildVersionCommand)
                    yield return command + 8;
                if (size < 8)
                    yield break;
                command += (int)size;
            }
        }

        private static uint Read(byte[] bytes, int at) =>
            at + 4 > bytes.Length ? 0u : (uint)(bytes[at] | bytes[at + 1] << 8 | bytes[at + 2] << 16 | bytes[at + 3] << 24);

        private static uint ReadBig(byte[] bytes, int at) =>
            at + 4 > bytes.Length ? 0u : (uint)(bytes[at] << 24 | bytes[at + 1] << 16 | bytes[at + 2] << 8 | bytes[at + 3]);

        private static void Write(byte[] bytes, int at, uint value)
        {
            bytes[at] = (byte)value;
            bytes[at + 1] = (byte)(value >> 8);
            bytes[at + 2] = (byte)(value >> 16);
            bytes[at + 3] = (byte)(value >> 24);
        }
    }
}
