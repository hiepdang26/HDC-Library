using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HDC.Tool.Editor;
using NUnit.Framework;

namespace HDC.Tool.Tests
{
    public class HDCSimulatorSupportTests
    {
        private const uint Arm64 = 0x0100000C;
        private const uint X86 = 0x01000007;
        private const uint Device = 2;
        private const uint Simulator = 7;

        [Test]
        public void ADeviceOnlyArm64SliceIsMarkedForTheSimulator()
        {
            byte[] x86 = Archive(Machine(X86, Simulator));
            byte[] library = Fat((X86, x86), (Arm64, Archive(Machine(Arm64, Device), Machine(Arm64, Device))));
            byte[] before = (byte[])library.Clone();

            Assert.AreEqual(HDCArm64Code.Device, HDCSimulatorSupport.Inspect(library));
            Assert.AreEqual(2, HDCSimulatorSupport.MarkForSimulator(library));

            Assert.AreEqual(HDCArm64Code.Simulator, HDCSimulatorSupport.Inspect(library));
            Assert.AreEqual(0, HDCSimulatorSupport.MarkForSimulator(library), "a marked library is left alone");
            Assert.AreEqual(before.Length, library.Length);
            Assert.AreEqual(before.Take(x86.Length + 64).ToArray(), library.Take(x86.Length + 64).ToArray(), "the x86_64 slice does not change");
        }

        [Test]
        public void LibrariesWithSimulatorCodeAreLeftAlone()
        {
            byte[] library = Archive(Machine(Arm64, Simulator));

            Assert.AreEqual(HDCArm64Code.Simulator, HDCSimulatorSupport.Inspect(library));
            Assert.AreEqual(0, HDCSimulatorSupport.MarkForSimulator(library));
        }

        [Test]
        public void AnX86OnlyLibraryHasNoArm64Code() =>
            Assert.AreEqual(HDCArm64Code.None, HDCSimulatorSupport.Inspect(Fat((X86, Archive(Machine(X86, Simulator))))));

        [Test]
        public void ObjectsWithoutABuildVersionCannotBeMarked()
        {
            byte[] library = Archive(Machine(Arm64), Machine(Arm64, Device));

            Assert.AreEqual(HDCArm64Code.Unknown, HDCSimulatorSupport.Inspect(library));
            Assert.AreEqual(0, HDCSimulatorSupport.MarkForSimulator(library));
        }

        [Test]
        public void OnlyTheExportCopyIsMarked()
        {
            string folder = Path.Combine(Path.GetTempPath(), "hdc-simulator-" + Guid.NewGuid().ToString("N"));
            string firebase = Path.Combine(folder, HDCSimulatorSupport.LibrariesFolder, "Plugins", "iOS", "Firebase");
            Directory.CreateDirectory(firebase);
            try
            {
                File.WriteAllBytes(Path.Combine(firebase, "libFirebaseCppApp.a"), Fat((Arm64, Archive(Machine(Arm64, Device)))));
                File.WriteAllBytes(Path.Combine(folder, HDCSimulatorSupport.LibrariesFolder, "libReady.a"), Archive(Machine(Arm64, Simulator)));

                List<string> marked = HDCSimulatorSupport.MarkExportForSimulator(folder);

                CollectionAssert.AreEqual(new[] { "libFirebaseCppApp.a" }, marked);
                Assert.AreEqual(HDCArm64Code.Simulator, HDCSimulatorSupport.Inspect(File.ReadAllBytes(Path.Combine(firebase, "libFirebaseCppApp.a"))));
                CollectionAssert.IsEmpty(HDCSimulatorSupport.MarkExportForSimulator(folder));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        private static byte[] Machine(uint cpu, params uint[] platforms)
        {
            var bytes = new List<byte>();
            void Add(uint value) => bytes.AddRange(BitConverter.GetBytes(value));
            Add(0xFEEDFACF);
            Add(cpu);
            Add(0);
            Add(1);
            Add((uint)platforms.Length);
            Add((uint)(24 * platforms.Length));
            Add(0);
            Add(0);
            foreach (uint platform in platforms)
            {
                Add(0x32);
                Add(24);
                Add(platform);
                Add(0x000F0000);
                Add(0x00110000);
                Add(0);
            }

            return bytes.ToArray();
        }

        private static byte[] Archive(params byte[][] objects)
        {
            var bytes = new List<byte>(Encoding.ASCII.GetBytes("!<arch>\n"));
            Member(bytes, "__.SYMDEF SORTED", new byte[8]);
            for (int i = 0; i < objects.Length; i++)
                Member(bytes, "object" + i + ".o", objects[i]);
            return bytes.ToArray();
        }

        private static void Member(List<byte> bytes, string name, byte[] data)
        {
            byte[] longName = Encoding.ASCII.GetBytes(name.PadRight((name.Length + 8) / 8 * 8, '\0'));
            string header = ("#1/" + longName.Length).PadRight(16) + "0".PadRight(12) + "0".PadRight(6) + "0".PadRight(6) + "644".PadRight(8) +
                            (longName.Length + data.Length).ToString().PadRight(10) + "`\n";
            bytes.AddRange(Encoding.ASCII.GetBytes(header));
            bytes.AddRange(longName);
            bytes.AddRange(data);
            if (bytes.Count % 2 != 0)
                bytes.Add((byte)'\n');
        }

        private static byte[] Fat(params (uint Cpu, byte[] Slice)[] slices)
        {
            var bytes = new List<byte>();
            void Big(uint value) => bytes.AddRange(BitConverter.GetBytes(value).Reverse());
            Big(0xCAFEBABE);
            Big((uint)slices.Length);
            int offset = 64;
            var offsets = new List<int>();
            foreach ((uint cpu, byte[] slice) in slices)
            {
                offsets.Add(offset);
                Big(cpu);
                Big(0);
                Big((uint)offset);
                Big((uint)slice.Length);
                Big(3);
                offset += (slice.Length + 7) / 8 * 8;
            }

            for (int i = 0; i < slices.Length; i++)
            {
                while (bytes.Count < offsets[i])
                    bytes.Add(0);
                bytes.AddRange(slices[i].Slice);
            }

            return bytes.ToArray();
        }
    }
}
