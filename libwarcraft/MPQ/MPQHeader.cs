//
//  MPQHeader.cs
//
//  Copyright (c) 2018 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Text;

using Warcraft.Core.Interfaces;
using Warcraft.MPQ.Tables.Block;
using Warcraft.MPQ.Tables.Hash;

namespace Warcraft.MPQ
{
    /// <summary>
    /// This class represents the header of an MPQ archive. It contains information about
    /// the data contained in the archive, such as offsets to tables, file count, and the
    /// format of the archive.
    /// </summary>
    public class MPQHeader : IBinarySerializable
    {
        /*
            Fields present in Basic Format (v0)
        */

        /// <summary>
        /// The binary signature of an MPQ file. This is the four first bytes of all
        /// valid MPQ archives.
        /// </summary>
        public const string ArchiveSignature = "MPQ\x1a";

        /// <summary>
        /// Gets the size of this header in bytes. Stored in the archive, and varies between
        /// format versions.
        /// </summary>
        public uint HeaderSize
        {
            get;
            private set;
        }

        private uint _basicArchiveSize;

        /// <summary>
        /// Gets the size of the full archive in bytes.
        /// </summary>
        public ulong ArchiveSize => GetArchiveSize();

        private MPQFormat _format;
        private ushort _sectorSizeExponent;

        private uint _hashTableOffset;
        private uint _blockTableOffset;

        /// <summary>
        /// Gets the number of hash table entries stored in this archive.
        /// </summary>
        public uint HashTableEntryCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of block table entries stored in this archive.
        /// </summary>
        public uint BlockTableEntryCount
        {
            get;
            private set;
        }

        /*
            Fields present in ExtendedIO Format (v1)
        */
        private ulong _extendedBlockTableOffset;
        private ushort _extendedFormatHashTableOffsetBits;
        private ushort _extendedFormatBlockTableOffsetBits;

        /*
            Fields present in ExtendedIO Format (v2)
        */
        private ulong _longArchiveSize;
        private ulong _betTableOffset;
        private ulong _hetTableOffset;

        /*
            Fields present in ExtendedIO Format (v3)
        */
        private ulong _compressedHashTableSize;
        private ulong _compressedBlockTableSize;
        private ulong _compressedExtendedBlockTableSize;
        private ulong _compressedHetTableSize;
        private ulong _compressedBetTableSize;

        private uint _chunkSizeForHashing;

        private string _md5BlockTable;
        private string _md5HashTable;
        private string _md5ExtendedBlockTable;
        private string _md5BetTable;
        private string _md5HetTable;
        private string _md5Header;

        // The MD5_Header is calculated from the start of the signature to the end of the MD5_HETTable

        /// <summary>
        /// Initializes a new instance of the <see cref="Warcraft.MPQ.MPQHeader"/> class.
        /// This creates a default header for an empty archive.
        /// </summary>
        /// <param name="inFormat">The format.</param>
        public MPQHeader(MPQFormat inFormat)
        {
            if (inFormat == MPQFormat.Basic)
            {
                HeaderSize = 32;
            }
            else if (inFormat == MPQFormat.ExtendedV1)
            {
                HeaderSize = 44;
            }
            else
            {
                throw new NotImplementedException();
            }

            _basicArchiveSize = HeaderSize;
            _format = inFormat;
            _sectorSizeExponent = 3;

            if (_format > MPQFormat.ExtendedV1)
            {
                _longArchiveSize = HeaderSize;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Warcraft.MPQ.MPQHeader"/> class.
        /// </summary>
        /// <param name="data">A byte array containing the header data of the archive.</param>
        /// <exception cref="FileLoadException">A FileLoadException may be thrown if the archive was not
        /// an MPQ file starting with the string "MPQ\x1a".</exception>
        public MPQHeader(byte[] data)
        {
            using (MemoryStream dataStream = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(dataStream))
                {
                    string dataSignature = new string(br.ReadChars(4));
                    if (dataSignature != ArchiveSignature)
                    {
                        throw new FileLoadException("The provided file was not an MPQ file.");
                    }

                    HeaderSize = br.ReadUInt32();
                    _basicArchiveSize = br.ReadUInt32();
                    _format = (MPQFormat)br.ReadUInt16();
                    _sectorSizeExponent = br.ReadUInt16();
                    _hashTableOffset = br.ReadUInt32();
                    _blockTableOffset = br.ReadUInt32();
                    HashTableEntryCount = br.ReadUInt32();
                    BlockTableEntryCount = br.ReadUInt32();

                    if (_format >= MPQFormat.ExtendedV1)
                    {
                        _extendedBlockTableOffset = br.ReadUInt64();
                        _extendedFormatHashTableOffsetBits = br.ReadUInt16();
                        _extendedFormatBlockTableOffsetBits = br.ReadUInt16();
                    }

                    if (_format >= MPQFormat.ExtendedV2)
                    {
                        _longArchiveSize = br.ReadUInt64();
                        _betTableOffset = br.ReadUInt64();
                        _hetTableOffset = br.ReadUInt64();
                    }

                    if (_format >= MPQFormat.ExtendedV3)
                    {
                        _compressedHashTableSize = br.ReadUInt64();
                        _compressedBlockTableSize = br.ReadUInt64();
                        _compressedExtendedBlockTableSize = br.ReadUInt64();
                        _compressedHetTableSize = br.ReadUInt64();
                        _compressedBetTableSize = br.ReadUInt64();

                        _chunkSizeForHashing = br.ReadUInt32();

                        _md5BlockTable = BitConverter.ToString(br.ReadBytes(16));
                        _md5HashTable = BitConverter.ToString(br.ReadBytes(16));
                        _md5ExtendedBlockTable = BitConverter.ToString(br.ReadBytes(16));
                        _md5BetTable = BitConverter.ToString(br.ReadBytes(16));
                        _md5HetTable = BitConverter.ToString(br.ReadBytes(16));
                        _md5Header = BitConverter.ToString(br.ReadBytes(16));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the size of the full hash table.
        /// </summary>
        /// <returns>The hash table size.</returns>
        public ulong GetHashTableSize()
        {
            return (ulong)(HashTableEntryCount * HashTableEntry.GetSize());
        }

        /// <summary>
        /// Gets the size of the hash table in compressed form (if it is compressed).
        /// </summary>
        /// <returns>The size of the compressed table.</returns>
        public ulong GetCompressedHashTableSize()
        {
            return _compressedHashTableSize;
        }

        /// <summary>
        /// Determinest whether or not the hash table is compressed.
        /// </summary>
        /// <returns><value>true</value> if the hash table is compressed; otherwise, <value>false</value>.</returns>
        public bool IsHashTableCompressed()
        {
            return _compressedHashTableSize > GetHashTableSize();
        }

        /// <summary>
        /// Gets the size of the block table.
        /// </summary>
        /// <returns>The block table size.</returns>
        public ulong GetBlockTableSize()
        {
            return (ulong)(BlockTableEntryCount * BlockTableEntry.GetSize());
        }

        /// <summary>
        /// Gets the size of the block table in compressed form (if it is compressed).
        /// </summary>
        /// <returns>The size of the compressed table.</returns>
        public ulong GetCompressedBlockTableSize()
        {
            return _compressedBlockTableSize;
        }

        /// <summary>
        /// Determinest whether or not the block table is compressed.
        /// </summary>
        /// <returns><value>true</value> if the block table is compressed; otherwise, <value>false</value>.</returns>
        public bool IsBlockTableCompressed()
        {
            return _compressedBlockTableSize > GetBlockTableSize();
        }

        /// <summary>
        /// Gets the size of the extended block table.
        /// </summary>
        /// <returns>The extended block table size.</returns>
        public ulong GetExtendedBlockTableSize()
        {
            return (ulong)BlockTableEntryCount * sizeof(ushort);
        }

        /// <summary>
        /// Gets the offset of the hash table, adjusted using the high bits if neccesary.
        /// </summary>
        /// <returns>The hash table offset.</returns>
        public ulong GetHashTableOffset()
        {
            if (_format == MPQFormat.Basic)
            {
                return _hashTableOffset;
            }
            else
            {
                return MergeHighBits(GetBaseHashTableOffset(), GetExtendedHashTableOffsetBits());
            }
        }

        /// <summary>
        /// Gets the base hash table offset.
        /// </summary>
        /// <returns>The base hash table offset.</returns>
        private uint GetBaseHashTableOffset()
        {
            return _hashTableOffset;
        }

        /// <summary>
        /// Gets the offset of the block table, adjusted using the high bits if neccesary.
        /// </summary>
        /// <returns>The block table offset.</returns>
        public ulong GetBlockTableOffset()
        {
            if (_format == MPQFormat.Basic)
            {
                return _blockTableOffset;
            }
            else
            {
                return MergeHighBits(GetBaseBlockTableOffset(), GetExtendedBlockTableOffsetBits());
            }
        }

        /// <summary>
        /// Gets the base block table offset.
        /// </summary>
        /// <returns>The base block table offset.</returns>
        private uint GetBaseBlockTableOffset()
        {
            return _blockTableOffset;
        }

        /// <summary>
        /// Gets the number of entries in the hash table.
        /// </summary>
        /// <returns>The number of hash table entries.</returns>
        public uint GetHashTableEntryCount()
        {
            return HashTableEntryCount;
        }

        /// <summary>
        /// Gets the number of entries in the block table.
        /// </summary>
        /// <returns>The number of block table entries.</returns>
        public uint GetBlockTableEntryCount()
        {
            return BlockTableEntryCount;
        }

        /// <summary>
        /// Gets the format of the MPQ archive.
        /// </summary>
        /// <returns>The format of the archive.</returns>
        public MPQFormat GetFormat()
        {
            return _format;
        }

        /// <summary>
        /// Gets the offset to the extended block table.
        /// </summary>
        /// <returns>The extended block table offset.</returns>
        public ulong GetExtendedBlockTableOffset()
        {
            return _extendedBlockTableOffset;
        }

        /// <summary>
        /// Gets the high 16 bits of the extended hash table offset.
        /// </summary>
        /// <returns>The extended hash table offset bits.</returns>
        public ushort GetExtendedHashTableOffsetBits()
        {
            return _extendedFormatHashTableOffsetBits;
        }

        /// <summary>
        /// Gets the high 16 bits of the extended block table offset.
        /// </summary>
        /// <returns>The extended block table offset bits.</returns>
        public ushort GetExtendedBlockTableOffsetBits()
        {
            return _extendedFormatBlockTableOffsetBits;
        }

        /// <summary>
        /// Gets the size of the archive.
        /// </summary>
        /// <returns>The archive size.</returns>
        public ulong GetArchiveSize()
        {
            if (_format == MPQFormat.Basic)
            {
                return _basicArchiveSize;
            }
            else if (_format == MPQFormat.ExtendedV1)
            {
                // Calculate the size from the start of the header to the end of the
                // hash table, block table or extended block table (whichever is furthest away)
                ulong hashTableOffset = GetHashTableOffset();
                ulong blockTableOffset = GetBlockTableOffset();
                ulong extendedBlockTableOffset = GetExtendedBlockTableOffset();

                ulong furthestOffset = 0;
                ulong archiveSize = 0;

                // Sort the sizes
                if (hashTableOffset > furthestOffset)
                {
                    furthestOffset = hashTableOffset;

                    archiveSize = furthestOffset + GetHashTableSize();
                }

                if (blockTableOffset > furthestOffset)
                {
                    furthestOffset = blockTableOffset;

                    archiveSize = furthestOffset + GetBlockTableSize();
                }

                if (extendedBlockTableOffset <= furthestOffset)
                {
                    return archiveSize;
                }

                furthestOffset = extendedBlockTableOffset;
                archiveSize = furthestOffset + GetExtendedBlockTableSize();

                return archiveSize;
            }
            else
            {
                return _longArchiveSize;
            }
        }

        /// <summary>
        /// Gets the sector size exponent.
        /// </summary>
        /// <returns>The sector size exponent.</returns>
        public uint GetSectorSizeExponent()
        {
            return _sectorSizeExponent;
        }

        /// <summary>
        /// Merges a base 32-bit number with a 16-bit number, forming a 64-bit number where the uppermost 16 bits are 0.
        /// </summary>
        /// <returns>The final number.</returns>
        /// <param name="baseBits">Base bits.</param>
        /// <param name="highBits">High bits.</param>
        public static ulong MergeHighBits(uint baseBits, ushort highBits)
        {
            ulong lower32Bits = baseBits;
            ulong high16Bits = (ulong)highBits << 32 & 0x0000FFFF00000000;

            ulong mergedBits = (high16Bits + lower32Bits) & 0x0000FFFFFFFFFFFF;
            return mergedBits;
        }

        /// <summary>
        /// Serializes the current object into a byte array.
        /// </summary>
        /// <inheritdoc/>
        public byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    // Write the archive signature
                    foreach (char c in ArchiveSignature)
                    {
                        bw.Write(c);
                    }

                    bw.Write(HeaderSize);
                    bw.Write(ArchiveSize);
                    bw.Write((ushort)_format);
                    bw.Write(_sectorSizeExponent);
                    bw.Write(_hashTableOffset);
                    bw.Write(_blockTableOffset);
                    bw.Write(HashTableEntryCount);
                    bw.Write(BlockTableEntryCount);

                    if (_format > MPQFormat.Basic)
                    {
                        bw.Write(_extendedBlockTableOffset);
                        bw.Write(_extendedFormatHashTableOffsetBits);
                        bw.Write(_extendedFormatBlockTableOffsetBits);
                    }

                    if (_format > MPQFormat.ExtendedV1)
                    {
                        bw.Write(_longArchiveSize);
                        bw.Write(_betTableOffset);
                        bw.Write(_hetTableOffset);
                    }

                    if (_format > MPQFormat.ExtendedV2)
                    {
                        bw.Write(_compressedHashTableSize);
                        bw.Write(_compressedBlockTableSize);
                        bw.Write(_compressedExtendedBlockTableSize);
                        bw.Write(_compressedHetTableSize);
                        bw.Write(_compressedBetTableSize);

                        bw.Write(_chunkSizeForHashing);

                        bw.Write(Encoding.UTF8.GetBytes(_md5BlockTable));
                        bw.Write(Encoding.UTF8.GetBytes(_md5HashTable));
                        bw.Write(Encoding.UTF8.GetBytes(_md5ExtendedBlockTable));
                        bw.Write(Encoding.UTF8.GetBytes(_md5BetTable));
                        bw.Write(Encoding.UTF8.GetBytes(_md5HetTable));
                        bw.Write(Encoding.UTF8.GetBytes(_md5Header));
                    }
                }

                return ms.ToArray();
            }
        }
    }
}
