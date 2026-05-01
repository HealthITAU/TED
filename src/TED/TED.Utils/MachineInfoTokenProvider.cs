using System;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using Microsoft.VisualBasic.Devices;

namespace TED.Utils
{
    internal static class MachineInfoTokenProvider
    {
        public static string GetUserName()
        {
            return WindowsIdentity.GetCurrent().Name;
        }

        public static string GetMachineName()
        {
            return Environment.MachineName;
        }

        public static string GetOsVersion()
        {
            return Environment.OSVersion.ToString();
        }

        public static string GetOsName()
        {
            return new ComputerInfo().OSFullName;
        }

        public static string GetMachineSerial()
        {
            return GetWmiProperty("Win32_BIOS", "SerialNumber");
        }

        public static string GetManufacturer()
        {
            return GetWmiProperty("Win32_ComputerSystem", "Manufacturer");
        }

        public static string GetModel()
        {
            return GetWmiProperty("Win32_ComputerSystem", "Model");
        }

        public static string GetPrimaryIpAddress()
        {
            try
            {
                return GetUsableNetworkInterfaces()
                    .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
                    .Select(address => address.Address)
                    .FirstOrDefault(IsUsableIpAddress)
                    ?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetPrimaryMacAddress()
        {
            try
            {
                return GetUsableNetworkInterfaces()
                    .Select(networkInterface => FormatMacAddress(networkInterface.GetPhysicalAddress()))
                    .FirstOrDefault(address => !string.IsNullOrEmpty(address)) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetWmiProperty(string className, string propertyName)
        {
            try
            {
                var query = new ObjectQuery($"SELECT {propertyName} FROM {className}");
                using var searcher = new ManagementObjectSearcher(query);
                using var results = searcher.Get();

                foreach (ManagementObject result in results)
                {
                    using (result)
                    {
                        var value = result[propertyName]?.ToString()?.Trim();

                        if (!string.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                }
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private static IOrderedEnumerable<NetworkInterface> GetUsableNetworkInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(networkInterface =>
                    networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderByDescending(networkInterface => networkInterface.GetIPProperties().GatewayAddresses.Count > 0)
                .ThenBy(networkInterface => networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 1 : 0);
        }

        private static bool IsUsableIpAddress(IPAddress address)
        {
            return address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address);
        }

        private static string FormatMacAddress(PhysicalAddress physicalAddress)
        {
            var bytes = physicalAddress.GetAddressBytes();

            return bytes.Length == 0
                ? string.Empty
                : string.Join(":", bytes.Select(value => value.ToString("X2")));
        }
    }
}
