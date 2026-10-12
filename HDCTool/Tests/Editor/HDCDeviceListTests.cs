using System.Collections.Generic;
using System.IO;
using System.Text;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCDeviceListTests
    {
        private const string Adb =
            "* daemon not running; starting now at tcp:5037\n" +
            "* daemon started successfully\n" +
            "List of devices attached\n" +
            "emulator-5554          device product:sdk_gphone64_arm64 model:sdk_gphone64_arm64 device:emu64a transport_id:1\n" +
            "R58M123ABC             unauthorized usb:1-1 transport_id:2\n" +
            "0a1b2c3d\toffline\n\n";

        private const string Xctrace =
            "== Devices ==\n" +
            "Studio Mac (11111111-2222-3333-4444-555555555555)\n" +
            "Test iPhone (16.7.10) (0123456789abcdef0123456789abcdef01234567)\n" +
            "Second Phone (18.2) (00008020-0001234567890ABC)\n\n" +
            "== Devices Offline ==\n" +
            "Old iPhone (18.7.10) (00008020-00012D540E86002F)\n\n" +
            "== Simulators ==\n" +
            "iPhone 17 (26.4.1) (F5061B51-F209-4B51-B9C8-F23B4CFAF108)\n";

        [Test]
        public void ToolOutputKeepsNonAsciiDeviceNames()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                Assert.Ignore("Uses /bin/cat.");
            const string name = "Hi\u1EC7p \u0110\u1EB7ng";
            string file = Path.Combine(Path.GetTempPath(), "hdc-device-name.txt");
            File.WriteAllBytes(file, Encoding.UTF8.GetBytes(name));
            try
            {
                Assert.AreEqual(name, HDCProcess.Run("/bin/cat", "\"" + file + "\"", 10000).Text);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void TheAdbListKeepsEveryDeviceWithItsState()
        {
            List<HDCAndroidDevice> devices = HDCAndroidDevices.Parse(Adb);

            Assert.AreEqual(3, devices.Count);
            Assert.AreEqual("emulator-5554", devices[0].Serial);
            Assert.IsTrue(devices[0].Ready);
            Assert.AreEqual("sdk gphone64 arm64", devices[0].Title);
            Assert.IsFalse(devices[1].Ready);
            Assert.AreEqual("R58M123ABC (unauthorized)", devices[1].Title);
            Assert.AreEqual("0a1b2c3d (offline)", devices[2].Title);
        }

        [Test]
        public void AnEmptyAdbListHasNoDevice()
        {
            CollectionAssert.IsEmpty(HDCAndroidDevices.Parse("List of devices attached\n\n"));
            CollectionAssert.IsEmpty(HDCAndroidDevices.Parse(null));
        }

        private const string Simulators =
            "{\n  \"devices\" : {\n    \"com.apple.CoreSimulator.SimRuntime.watchOS-11-0\" : [\n      { \"udid\" : \"W1\", \"isAvailable\" : true, \"state\" : \"Shutdown\", \"name\" : \"Apple Watch\" }\n    ],\n" +
            "    \"com.apple.CoreSimulator.SimRuntime.iOS-26-4\" : [\n" +
            "      { \"dataPath\" : \"\\/Users\\/me\\/data\", \"udid\" : \"A1\", \"isAvailable\" : true, \"state\" : \"Shutdown\", \"name\" : \"iPad Air\" },\n" +
            "      { \"udid\" : \"B2\", \"isAvailable\" : true, \"state\" : \"Booted\", \"name\" : \"iPhone 17 Pro\" },\n" +
            "      { \"udid\" : \"C3\", \"isAvailable\" : false, \"state\" : \"Shutdown\", \"name\" : \"Broken\" }\n    ]\n  }\n}";

        [Test]
        public void IosSimulatorsAreListedWithTheRunningOneFirst()
        {
            List<HDCAppleDevice> simulators = HDCAppleDevices.ParseSimulators(Simulators);

            CollectionAssert.AreEqual(new[] { "B2", "A1" }, simulators.ConvertAll(device => device.Udid));
            Assert.AreEqual(HDCAppleTarget.Simulator, simulators[0].Kind);
            Assert.AreEqual("26.4", simulators[0].OsVersion);
            Assert.AreEqual("iPhone 17 Pro · iOS 26.4 · running", simulators[0].Title);
            Assert.AreEqual("iPad Air · iOS 26.4", simulators[1].Title);
            CollectionAssert.IsEmpty(HDCAppleDevices.ParseSimulators(""));
        }

        [Test]
        public void MyMacHasItsOwnTitle() =>
            Assert.AreEqual("My Mac (Designed for iPad)", new HDCAppleDevice { Kind = HDCAppleTarget.Mac, Name = HDCAppleDevice.MacName }.Title);

        [Test]
        public void TheAppleListKeepsOnlyConnectedDevices()
        {
            List<HDCAppleDevice> devices = HDCAppleDevices.Parse(Xctrace);

            Assert.AreEqual(2, devices.Count);
            Assert.AreEqual("Test iPhone", devices[0].Name);
            Assert.AreEqual("16.7.10", devices[0].OsVersion);
            Assert.AreEqual("0123456789abcdef0123456789abcdef01234567", devices[0].Udid);
            Assert.AreEqual("Test iPhone · iOS 16.7.10", devices[0].Title);
            Assert.AreEqual(HDCAppleTarget.Phone, devices[0].Kind);
            Assert.AreEqual("00008020-0001234567890ABC", devices[1].Udid);
        }
    }
}
