using System;
using System.Collections.Generic;
using System.IO;

namespace Ionic.Zip
{
    public class SharedUtils
    {
        internal static int ReadInt(System.IO.Stream s)
        {
            return _ReadFourBytes(s, "Could not read block - no data!  (position 0x{0:X8})");
        }

        private static int _ReadFourBytes(System.IO.Stream s, string message)
        {
            int n = 0;
            byte[] block = new byte[4];
#if NETCF
            // workitem 9181
            // Reading here in NETCF sometimes reads "backwards". Seems to happen for
            // larger files.  Not sure why. Maybe an error in caching.  If the data is:
            //
            // 00100210: 9efa 0f00 7072 6f6a 6563 742e 6963 7750  ....project.icwP
            // 00100220: 4b05 0600 0000 0006 0006 0091 0100 008e  K...............
            // 00100230: 0010 0000 00                             .....
            //
            // ...and the stream Position is 10021F, then a Read of 4 bytes is returning
            // 50776369, instead of 06054b50. This seems to happen the 2nd time Read()
            // is called from that Position..
            //
            // submitted to connect.microsoft.com
            // https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=318918#tabs
            //
            for (int i = 0; i < block.Length; i++)
            {
                n+= s.Read(block, i, 1);
            }
#else
            n = s.Read(block, 0, block.Length);
#endif
            if (n != block.Length) throw new BadReadException(String.Format(message, s.Position));
            int data = unchecked((((block[3] * 256 + block[2]) * 256) + block[1]) * 256 + block[0]);
            return data;
        }



        /// <summary>
        ///   Finds a signature in the zip stream. This is useful for finding
        ///   the end of a zip entry, for example, or the beginning of the next ZipEntry.
        /// </summary>
        ///
        /// <remarks>
        ///   <para>
        ///     Scans through 64k at a time.
        ///   </para>
        ///
        ///   <para>
        ///     If the method fails to find the requested signature, the stream Position
        ///     after completion of this method is unchanged. If the method succeeds in
        ///     finding the requested signature, the stream position after completion is
        ///     direct AFTER the signature found in the stream.
        ///   </para>
        /// </remarks>
        ///
        /// <param name="stream">The stream to search</param>
        /// <param name="SignatureToFind">The 4-byte signature to find</param>
        /// <returns>The number of bytes read</returns>
        internal static long FindSignature(System.IO.Stream stream, int SignatureToFind)
        {
            long startingPosition = stream.Position;

            int BATCH_SIZE = 65536; //  8192;
            byte[] targetBytes = new byte[4];
            targetBytes[0] = (byte)(SignatureToFind >> 24);
            targetBytes[1] = (byte)((SignatureToFind & 0x00FF0000) >> 16);
            targetBytes[2] = (byte)((SignatureToFind & 0x0000FF00) >> 8);
            targetBytes[3] = (byte)(SignatureToFind & 0x000000FF);
            byte[] batch = new byte[BATCH_SIZE];
            int n = 0;
            bool success = false;
            do
            {
                n = stream.Read(batch, 0, batch.Length);
                if (n != 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (batch[i] == targetBytes[3])
                        {
                            long curPosition = stream.Position;
                            stream.Seek(i - n, System.IO.SeekOrigin.Current);
                            // workitem 10178
                            Workaround_Ladybug318918(stream);

                            // workitem 7711
                            int sig = ReadSignature(stream);

                            success = (sig == SignatureToFind);
                            if (!success)
                            {
                                stream.Seek(curPosition, System.IO.SeekOrigin.Begin);
                                // workitem 10178
                                Workaround_Ladybug318918(stream);
                            }
                            else
                                break; // out of for loop
                        }
                    }
                }
                else break;
                if (success) break;

            } while (true);

            if (!success)
            {
                stream.Seek(startingPosition, System.IO.SeekOrigin.Begin);
                // workitem 10178
                Workaround_Ladybug318918(stream);
                return -1;  // or throw?
            }

            // subtract 4 for the signature.
            long bytesRead = (stream.Position - startingPosition) - 4;

            return bytesRead;
        }

        internal static int ReadSignature(System.IO.Stream s)
        {
            int x = 0;
            try { x = _ReadFourBytes(s, "n/a"); }
            catch (BadReadException) { }
            return x;
        }

        [System.Diagnostics.Conditional("NETCF")]
        public static void Workaround_Ladybug318918(Stream s)
        {
            // This is a workaround for this issue:
            // https://connect.microsoft.com/VisualStudio/feedback/details/318918
            // It's required only on NETCF.
            s.Flush();
        }




        internal static bool IsNotValidZipDirEntrySig(int signature)
        {
            return (signature != ZipConstants.ZipDirEntrySignature);
        }

        public static ZipEntry ReadDirEntry(ZipFile zf,
                                              Dictionary<String, Object> previouslySeen)
        {
            System.IO.Stream s = zf.ReadStream;
            System.Text.Encoding expectedEncoding = (zf.AlternateEncodingUsage == ZipOption.Always)
                ? zf.AlternateEncoding
                : ZipFile.DefaultEncoding;

            while (true)
            {
                int signature = Ionic.Zip.SharedUtils.ReadSignature(s);
                // return null if this is not a local file header signature
                if (IsNotValidZipDirEntrySig(signature))
                {
                    s.Seek(-4, System.IO.SeekOrigin.Current);
                    // workitem 10178
                    Ionic.Zip.SharedUtils.Workaround_Ladybug318918(s);

                    // Getting "not a ZipDirEntry signature" here is not always wrong or an
                    // error.  This can happen when walking through a zipfile.  After the
                    // last ZipDirEntry, we expect to read an
                    // EndOfCentralDirectorySignature.  When we get this is how we know
                    // we've reached the end of the central directory.
                    if (signature != ZipConstants.EndOfCentralDirectorySignature &&
                        signature != ZipConstants.Zip64EndOfCentralDirectoryRecordSignature &&
                        signature != ZipConstants.ZipEntrySignature  // workitem 8299
                        )
                    {
                        throw new BadReadException(String.Format("  Bad signature (0x{0:X8}) at position 0x{1:X8}", signature, s.Position));
                    }
                    return null;
                }

                int bytesRead = 42 + 4;
                byte[] block = new byte[42];
                int n = s.Read(block, 0, block.Length);
                if (n != block.Length) return null;

                int i = 0;
                var zde = new ZipEntry();
                zde.AlternateEncoding = expectedEncoding;
                zde._Source = ZipEntrySource.ZipFile;
                zde._container = new ZipContainer(zf);

                unchecked
                {
                    zde._VersionMadeBy = (short)(block[i++] + block[i++] * 256);
                    zde._VersionNeeded = (short)(block[i++] + block[i++] * 256);
                    zde._BitField = (short)(block[i++] + block[i++] * 256);
                    zde._CompressionMethod = (Int16)(block[i++] + block[i++] * 256);
                    zde._TimeBlob = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                    zde._LastModified = Ionic.Zip.SharedUtilities.PackedToDateTime(zde._TimeBlob);
                    zde._timestamp |= ZipEntryTimestamp.DOS;

                    zde._Crc32 = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                    zde._CompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                    zde._UncompressedSize = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                }

                // preserve
                zde._CompressionMethod_FromZipFile = zde._CompressionMethod;

                zde._filenameLength = (short)(block[i++] + block[i++] * 256);
                zde._extraFieldLength = (short)(block[i++] + block[i++] * 256);
                zde._commentLength = (short)(block[i++] + block[i++] * 256);
                zde._diskNumber = (UInt32)(block[i++] + block[i++] * 256);

                zde._InternalFileAttrs = (short)(block[i++] + block[i++] * 256);
                zde._ExternalFileAttrs = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

                zde._RelativeOffsetOfLocalHeader = (uint)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);

                // workitem 7801
                zde.IsText = ((zde._InternalFileAttrs & 0x01) == 0x01);

                block = new byte[zde._filenameLength];
                n = s.Read(block, 0, block.Length);
                bytesRead += n;
                if ((zde._BitField & 0x0800) == 0x0800)
                {
                    // UTF-8 is in use
                    zde._FileNameInArchive = Ionic.Zip.SharedUtilities.Utf8StringFromBuffer(block);
                }
                else
                {
                    zde._FileNameInArchive = Ionic.Zip.SharedUtilities.StringFromBuffer(block, expectedEncoding);
                }

                // workitem 10330
                // insure unique entry names
                while (!zf.IgnoreDuplicateFiles && previouslySeen.ContainsKey(zde._FileNameInArchive))
                {
                    zde._FileNameInArchive = CopyHelper.AppendCopyToFileName(zde._FileNameInArchive);
                    zde._metadataChanged = true;
                }

                if (zde.AttributesIndicateDirectory)
                    zde.MarkAsDirectory();  // may append a slash to filename if nec.
                                            // workitem 6898
                else if (zde._FileNameInArchive.EndsWith("/")) zde.MarkAsDirectory();

                zde._CompressedFileDataSize = zde._CompressedSize;
                if ((zde._BitField & 0x01) == 0x01)
                {
                    // this may change after processing the Extra field
                    zde._Encryption_FromZipFile = zde._Encryption =
                        EncryptionAlgorithm.PkzipWeak;
                    zde._sourceIsEncrypted = true;
                }

                if (zde._extraFieldLength > 0)
                {
                    zde._InputUsesZip64 = (zde._CompressedSize == 0xFFFFFFFF ||
                          zde._UncompressedSize == 0xFFFFFFFF ||
                          zde._RelativeOffsetOfLocalHeader == 0xFFFFFFFF);

                    // Console.WriteLine("  Input uses Z64?:      {0}", zde._InputUsesZip64);

                    bytesRead += zde.ProcessExtraField(s, zde._extraFieldLength);
                    zde._CompressedFileDataSize = zde._CompressedSize;
                }

                // we've processed the extra field, so we know the encryption method is set now.
                if (zde._Encryption == EncryptionAlgorithm.PkzipWeak)
                {
                    // the "encryption header" of 12 bytes precedes the file data
                    zde._CompressedFileDataSize -= 12;
                }
#if AESCRYPTO
                else if (zde.Encryption == EncryptionAlgorithm.WinZipAes128 ||
                            zde.Encryption == EncryptionAlgorithm.WinZipAes256)
                {
                    zde._CompressedFileDataSize = zde.CompressedSize -
                        (ZipEntry.GetLengthOfCryptoHeaderBytes(zde.Encryption) + 10);
                        zde._LengthOfTrailer = 10;
                }
#endif

                // tally the trailing descriptor
                if ((zde._BitField & 0x0008) == 0x0008)
                {
                    // sig, CRC, Comp and Uncomp sizes
                    if (zde._InputUsesZip64)
                        zde._LengthOfTrailer += 24;
                    else
                        zde._LengthOfTrailer += 16;
                }

                // workitem 12744
                zde.AlternateEncoding = ((zde._BitField & 0x0800) == 0x0800)
                    ? System.Text.Encoding.UTF8
                    : expectedEncoding;

                zde.AlternateEncodingUsage = ZipOption.Always;

                if (zde._commentLength > 0)
                {
                    block = new byte[zde._commentLength];
                    n = s.Read(block, 0, block.Length);
                    bytesRead += n;
                    if ((zde._BitField & 0x0800) == 0x0800)
                    {
                        // UTF-8 is in use
                        zde._Comment = Ionic.Zip.SharedUtilities.Utf8StringFromBuffer(block);
                    }
                    else
                    {
                        zde._Comment = Ionic.Zip.SharedUtilities.StringFromBuffer(block, expectedEncoding);
                    }
                }
                //zde._LengthOfDirEntry = bytesRead;
                if (zf.IgnoreDuplicateFiles && previouslySeen.ContainsKey(zde._FileNameInArchive))
                {
                    continue;
                }
                return zde;
            }
        }

    }
}