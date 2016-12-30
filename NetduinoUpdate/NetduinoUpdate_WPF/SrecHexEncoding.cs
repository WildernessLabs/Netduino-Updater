/* SREC (Motorola SREC) Conversion Class
 * Copyright (C) 2012-2013 Secret Labs LLC. All Rights Reserved.
 * 
 * DESCRIPTION: Motorola SREC files typically contain binary code or memory contents and are used for reading/writing EEPROM and reflashing flash.
 *              This class converts SREC format into byte arrays (for reading).
 * LIMITATIONS: Motorola SREC files contain blocks of data with individual offsets.  The offset to the first data in the file may be greater than zero, as it matches the 
 *              location of the data in the actual device.  Also, there may be "holes" in the data.  With these limitations in mind, we are taking two liberties:
 *              (1) filling all unused memory locations with the byte value zero (0 | NULL); (2) providing index and count values (offset and length of data to store/retrieve)
 * FUTURE: We may want to create overloads which allow the user to specify another byte value to fill unused byte array elements.
 */

/* For more information on S19HEX (Motorola S19) format, see the following resources:
 * http://en.wikipedia.org/wiki/SREC_(file_format) (Wikipedia article)
 * http://www.techedge.com.au/utils/bincvt.htm (BITCVT article)
 */

/* STRUCTURE OF AN SREC-encoded file:
 * 
 * An SREC-encoded file (.HEX) is made up of multiple records.  Each record describes data and that data's offset.  A final EOF record specifying zero bytes finishes the file.
 * 
 * Each line of the file (i.e. each record) is structured as follows, using ASCII encoding:
 * SRBB{aa..aa}{dd..dd}CC{\r\n}
 * Details on segments of the record (data line)
 * S <-- start code: each line starts with an (ASCII) S
 * R <-- record type: 1 (16-bit-address data record), 2 (24-bit-address data record), 3 (32-bit-address data record) or 0 (not yet supported) or 5 (not yet supported) or 7-9 (not yet supported)
 * BB <-- byte count: two hex digits, specifying the number of bytes (hex digit pairs) in this record.  Usually 16 (0x10) or 32 (0x20) for nice formatting.
 * {aa..aa} <-- offset address: the unsigned 16-/24-/32-bit address of the data specified by this record
 * {dd.dd} <-- data (hex digit pairs), two for each byte for a total character count of (byte count * 2)
 * * CC <-- checksum (least significant byte of the two's complement of the record starting after the record type and up to but not including the checksum)
 * {\r\n} <-- Optional but traditional, a CrLf (0xd, 0xa)
 * 
 * LIMITATIONS: Record types 0, 5, and 7-9 are ignored at this time.  
 *              Accordingly, any address in the 7-9 EOF record (traditionally used as an execution starting address) is ignored.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetduinoUpdate
{
    public static class SrecHexEncoding
    {
        /* byte[] GetBytes(string, int, int)
         * DESC: This function accepts an SREC-encoded string and returns a byte array.
         *       The data is stored at offsets in the byte array identical to the offset locations specified in the SREC data.
         *       Unused elements in the byte array are zero-initialized. */
        public static byte[] GetBytes(string s, UInt32 baseAddress)
        {
            // get all data from the I8HEX-encoded string as a byte array and return it to the caller
            return ConvertSrecHexToByteArray(s, baseAddress);
        }

        /* byte[] GetBytes(string, int, int)
         * DESC: This function accepts an SREC-encoded string, index and count parameters and returns a byte array.
         *       The data is stored at offsets in the byte array identical to the offset locations specified in the SCREC data--minus the index.
         *       Unused elements in the byte array are zero-initialized. */
        public static byte[] GetBytes(string s, UInt32 index, UInt32 count, UInt32 baseAddress)
        {
            // verify that we are not being asked for bytes at offsets greater than can be represented in an SREC file
            if (index > UInt32.MaxValue)
                throw new ArgumentOutOfRangeException("index");
            if (index + count - 1 > UInt32.MaxValue)
                throw new ArgumentOutOfRangeException("count");

            // get all data from the SREC-encoded string as a byte array
            byte[] allBytes = ConvertSrecHexToByteArray(s, baseAddress);
            // retrieve the desired bytes and return them to the caller
            byte[] returnValue = new byte[count];
            // initialize the byte array to all zeros
            // NOTE: nothing needed here, as .NET automatically initializes the array to all zeros; if we want a different value in the future, we must manually initialize
            Array.Copy(allBytes, index, returnValue, 0, Math.Min(count, allBytes.Length - index));
            return returnValue;
        }

        /* byte[] ConvertSrecHexToByteArray(string)
         * DESC: This function accepts an SREC-encoded string and returns a byte array.
         *       The data is stored at offsets in the byte array identical to the offset locations specified in the SREC data.
         *       Unused elements in the byte array are zero-initialized. 
         *       
         * * param baseAddress indicates the expected baseAddress of the data (which we'll subtract from the data offsets; add baseAddress to the return byte array's index values to calculate actual offset)
         * 
         * EXCEPTIONS: ArgumentOutOfRangeException if string contains invalid data */
        private static byte[] ConvertSrecHexToByteArray(string data, UInt32 baseAddress)
        {
            /* NOTE: One of the tricky things here is that we're creating an array of bytes but have no idea how long it should be.  
             *       We do know that it will not be longer than 2^16 (65,536) bytes. */

            const int ALLOCATE_MEMORY_IN_BLOCKS_OF_NUM_BYTES = 1024; // allocate memory in blocks of this number of bytes (for an efficiency vs memory usage tradeoff)

            // if our data string has no data records, then it is not a valid string: return an empty array.
            if (data.IndexOf("S") < 0)
                return new byte[0];

            // create a byte array to hold our return value; we'll default it to start at ALLOCATE_MEMORY_IN_BLOCKS_OF_NUM_BYTES bytes.
            byte[] returnValue = new byte[ALLOCATE_MEMORY_IN_BLOCKS_OF_NUM_BYTES];
            // and create a value which indicates the highest array index we've used
            UInt32 highestArrayIndexUsed = 0;

            // parse through our data string, pulling out data one line at a time; each record may or may not be separated by whitespace (\r\n, etc.)
            int pos = 0;
            while (true)
            {
                // proceed to an ASCII colon (start of record) or end of file
                while (pos < data.Length)
                {
                    char tChar = data.Substring(pos, 1)[0];
                    if (tChar == 'S')
                    {
                        // start of record; continue parsing/verifying record
                        break;
                    }
                    else if (tChar == '\r' || tChar == '\n')
                    {
                        // allowed white space; move to next character and then resume loop...
                        pos++;
                        continue;
                    }
                    else
                    {
                        // invalid character; throw exception
                        throw new ArgumentException("Supplied hex data is corrupt", "data");
                    }
                }

                // if we have parsed all data, exit.
                if (pos >= data.Length)
                    break;

                // verify that the record contains at least ten additional characters (start code (S), record type, byte count, address, and checksum) and then move to the byte count
                if (pos + 10 > data.Length)
                    throw new ArgumentException("Supplied hex data is corrupt", "data"); // incomplete record; throw exception
                pos++; // move to the record type (address length)

                // retrieve our record type
                int recordType = Convert.ToByte(data.Substring(pos, 1));

                // verify that our record type is valid (0x00 or 0x01) and then progress to the data
                if ((recordType < 1 || recordType > 3) && (recordType < 7 || recordType > 9))
                    throw new ArgumentException("Invalid record type in hex data", "data"); // invalid record type; throw exception
                pos += 1; // move to the byte count

                // calculate address length based on record type
                int addressLength;
                switch (recordType)
                {
                    case 1:
                    case 9:
                        addressLength = 4; // 16-bit
                        break;
                    case 2:
                    case 8:
                        addressLength = 6; // 24-bit
                        break;
                    case 3:
                    case 7:
                        addressLength = 8; // 32-bit
                        break;
                    default:
                        throw new ArgumentException("Invalid record type in hex data", "data");
                }

                // each record then starts with the byte count; store it
                int byteCount = Convert.ToByte(data.Substring(pos, 2), 16) - (addressLength / 2) - (2 / 2); // in Motorola SREC files, the byte length includes the address and data and checksum...and we only want to count the data bytes.

                // verify that the record is long enough to contain at least addressLength additional character plus two ASCII characters per byte, calculate our checksum, and then move onto the address
                if (pos + addressLength + 2 + (byteCount * 2) > data.Length) // we add an extra two because we haven't progressed beyond the byte count
                    throw new ArgumentException("Supplied hex data is corrupt", "data"); // incomplete record; throw exception
                // calculate checksum
                byte calculatedChecsksum = CalculateSrecHexRecordChecksum(data.Substring(pos, addressLength + 2 + (byteCount * 2))); // record, not including the start byte colon or checksum
                // move onto the address
                pos += 2;

                // retrieve the address offset of this data (and subtract our baseAddress)
                uint offset = Convert.ToUInt32(data.Substring(pos, addressLength), 16) - baseAddress;

                // verify that the address plus byte count is valid for a 32-bit range (0 - 65,536) and then progress onto our record type
                if (offset + byteCount > UInt32.MaxValue)
                    throw new ArgumentException("Supplied hex data is corrupt", "data"); // incomplete record; throw exception
                pos += addressLength;

                // retrieve our data as an array
                byte[] recordData = new byte[byteCount];
                for (int byteNum = 0; byteNum < byteCount; byteNum++)
                {
                    recordData[byteNum] = Convert.ToByte(data.Substring(pos, 2), 16);
                    pos += 2;
                }

                // retrieve our checksum and move our position forward
                byte checksum = Convert.ToByte(data.Substring(pos, 2), 16);
                pos += 2;

                // verify that our checksum is valid
                if (calculatedChecsksum != checksum)
                    throw new ArgumentException("Checksum failed in hex data", "data"); // checksum failed; throw exception

                // finally, process our record (as our checksum matched)
                if (recordType >= 1 && recordType <= 3)
                { 
                    // if our data will not fit in our current array, extend our array to fit.
                    while (returnValue.Length < offset + byteCount)
                    {
                        // NOTE: we grow our return value array in size as needed, ALLOCATE_MEMORY_IN_BLOCKS_OF_NUM_BYTES bytes at a time
                        // NOTE: we are zero-initializing the array; if we should intialize it to another value in the future, we should modify this code here
                        byte[] newArray = new byte[returnValue.Length + ALLOCATE_MEMORY_IN_BLOCKS_OF_NUM_BYTES];
                        Array.Copy(returnValue, newArray, returnValue.Length);
                        returnValue = newArray;
                    }

                    // save our data to our array
                    Array.Copy(recordData, 0, returnValue, offset, byteCount);

                    // and see if we need to update our highestArrayIndexUsed value
                    if (highestArrayIndexUsed < offset + byteCount - 1)
                        highestArrayIndexUsed = (UInt32)(offset + byteCount - 1);
                }
            }

            // if necessary, shrink our return value to its actual size
            if (returnValue.Length != highestArrayIndexUsed + 1)
            {
                byte[] newArray = new byte[highestArrayIndexUsed + 1];
                Array.Copy(returnValue, newArray, newArray.Length);
                returnValue = newArray;
            }

            // return the completed byte array to our caller
            return returnValue;
        }

        /* byte CalculateSrecHexChecksum(string)
         * DESC: This function calculates the checksum on SREC record data.  
         *       The checksum is calculated as follows:
         *       1. All hex digit pair values are added together
         *       2. Store the least-significant byte of the sum (and discard all other bytes)
         *       3. Subtract the least-significant byte from 0x100 (two's complement); this is our checksum
         *       
         * NOTE: To verify a checksum, add the data in a record line and also the checksum value; if the least significant byte is 0x00 then the checksum matches. */
        private static byte CalculateSrecHexRecordChecksum(string data)
        {
            // step 1: add all hex digit pair values together
            int sum = CalculateHexDigitPairsSum(data);
            // step 2: store the least-significant byte of the sum (and discard all other bytes)
            int lsb = sum & 0xFF; // step 2
            // step 3: xor the least-significant byte with 0xFF (one's complement)
            int lsbinverted = lsb ^ 0xFF;

            // return the checksum
            return (byte)(lsbinverted & 0xFF);
        }

        /* int CalculateHexDigitPairsSum
         * DESC: This function adds all hex digit pairs in a string and returns the result */
        private static int CalculateHexDigitPairsSum(string data)
        {
            int returnValue = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                returnValue += Convert.ToByte(data.Substring(i, 2), 16);
            }

            return returnValue;
        }
    }
}
