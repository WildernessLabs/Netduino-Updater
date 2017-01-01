using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WildernessLabs.Netduino.Device
{
    public class Firmware
    {
        byte _productID;
        string _productName;
        Version _version;
        string _versionBetaSuffix;

        string _folderPath;

        bool _hasMacAddress;

        public class FirmwareRegion
        {
            public uint BaseAddress { get; set; } // TODO: read in the HEX files dynamically to only write out bytes included in the HEX files...and then remove this vestigal property.
            public string Name { get; set; }
            public string Filename { get; set; }
            public List<uint> SectorBaseAddresses = new List<uint>();
        }
        List<FirmwareRegion> _firmwareRegions = new List<FirmwareRegion>();

        public Firmware(string path)
        {
            FirmwareRegion currentFirmwareRegion = null;
            bool currentFirmwareRegionBaseAddressIsExplicit = false;
            XmlReader reader = new XmlTextReader(path);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (string.Compare(reader.Name, "Firmware", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        _hasMacAddress = (reader.GetAttribute("HasMacAddress") != null ? bool.Parse(reader.GetAttribute("HasMacAddress")) : false);
                        _productID = byte.Parse(reader.GetAttribute("ProductID"));
                        _productName = reader.GetAttribute("ProductName");
                        _version = Version.Parse(reader.GetAttribute("Version"));
                        _versionBetaSuffix = reader.GetAttribute("VersionBetaSuffix");
                    }
                    else if (string.Compare(reader.Name, "FirmwareRegion", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        currentFirmwareRegion = new FirmwareRegion();
                        uint explicitBaseAddress = 0;
                        currentFirmwareRegionBaseAddressIsExplicit = uint.TryParse(reader.GetAttribute("BaseAddress"), System.Globalization.NumberStyles.HexNumber, null, out explicitBaseAddress);
                        currentFirmwareRegion.BaseAddress = explicitBaseAddress;
                        currentFirmwareRegion.Name = reader.GetAttribute("Name");
                        currentFirmwareRegion.Filename = reader.GetAttribute("Filename");
                    }
                    else if (string.Compare(reader.Name, "FlashSector", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        currentFirmwareRegion.SectorBaseAddresses.Add(uint.Parse(reader.GetAttribute("BaseAddress"), System.Globalization.NumberStyles.HexNumber));
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (string.Compare(reader.Name, "FirmwareRegion", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (!currentFirmwareRegionBaseAddressIsExplicit && currentFirmwareRegion.SectorBaseAddresses.Count > 0)
                        {
                            currentFirmwareRegion.BaseAddress = currentFirmwareRegion.SectorBaseAddresses[0];
                        }
                        _firmwareRegions.Add(currentFirmwareRegion);
                        currentFirmwareRegion = null;
                    }
                }
            }

            _folderPath = System.IO.Path.GetDirectoryName(path);
        }

        public List<FirmwareRegion> FirmwareRegions
        {
            get
            {
                return _firmwareRegions;
            }
        }

        public string FolderPath
        {
            get
            {
                return _folderPath;
            }
        }

        public bool HasMacAddress
        {
            get
            {
                return _hasMacAddress;
            }
        }

        public byte ProductID
        {
            get
            {
                return _productID;
            }
        }

        public string ProductName
        {
            get
            {
                return _productName;
            }
        }

        public Version Version
        {
            get
            {
                return _version;
            }
        }

        public string VersionBetaSuffix
        {
            get
            {
                return _versionBetaSuffix;
            }
        }

    }
}
