using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetduinoUpdate
{
    public class MACAddressService
    {
        private Queue<string> _addresses;
        private HashSet<string> _usedAddresses;
        private object _lock = new object();

        private string _allAddressFile = @"..\..\..\all_addresses.txt";
        private string _usedAddressFile = @"..\..\..\used_addresses.txt";

        public MACAddressService()
        {
            _addresses = new Queue<string>();
            _usedAddresses = new HashSet<string>();
            InitAddresses();
        }

        public byte[] GetNextAddress()
        {
            lock (_lock)
            {
                var item = _addresses.Dequeue();

                while (true)
                {
                    if (!_usedAddresses.Contains(item))
                    {
                        break;
                    }
                    if(_addresses.Count > 0)
                    {
                        item = _addresses.Dequeue();
                    }
                    else
                    {
                        throw new InvalidOperationException("No MAC Addresses available");
                    }
                }
                File.AppendAllLines(_usedAddressFile, new[] { item });
                _usedAddresses.Add(item);
                return ConvertMacAddressStringToByteArray(item);
            }
        }

        public void WriteAddresses(long seed, int count)
        {
            using(FileStream fs = File.Open(_allAddressFile, FileMode.OpenOrCreate))
            using (TextWriter tw = new StreamWriter(fs))
            {
                for (int i = 0; i < count; i++)
                {
                    var x = seed + i;
                    tw.WriteLine(x.ToString("X2"));
                }
            }
        }

        private byte[] ConvertMacAddressStringToByteArray(string value)
        {
            int VALID_MAC_ADDRESS_HEX_STRING_LENGTH = 12;

            string strippedMacAddress = value;

            // now grow or shrink the mac address to 12 hexadecimal digits.
            if (strippedMacAddress.Length > VALID_MAC_ADDRESS_HEX_STRING_LENGTH)
            {
                strippedMacAddress = strippedMacAddress.Substring(0, 12);
            }
            while (strippedMacAddress.Length < VALID_MAC_ADDRESS_HEX_STRING_LENGTH)
            {
                strippedMacAddress += "0";
            }

            byte[] macAddress = new byte[6];
            for (int i = 0; i < strippedMacAddress.Length; i += 2)
            {
                macAddress[i / 2] = (byte)Convert.ToInt16(strippedMacAddress.Substring(i, 2), 16);
            }

            return macAddress;
        }

        private void InitAddresses()
        {
            var dir = Directory.GetCurrentDirectory();
            if (File.Exists(_allAddressFile))
            {
                var addresses = File.ReadAllLines(_allAddressFile).ToList();
                addresses.ForEach(x => _addresses.Enqueue(x));
            }

            if (File.Exists(_usedAddressFile))
            {
                var usedAddresses = File.ReadAllLines(_usedAddressFile).ToList();
                usedAddresses.ForEach(x => _usedAddresses.Add(x));
            }
            
        }
    }
}
