using System.Collections.Generic;

/// <summary>
/// Patches specific files into a .w3m/.w3x archive in place, without rebuilding the archive.
/// This exists because MpqArchiveBuilder/MpqArchive.Create() (War3Net) write a brand-new,
/// standard archive from scratch — which drops any non-standard header tricks (bogus
/// DataOffset sentinels, MpqVersion=1 "protection" flags, etc.) that map-protection tools
/// commonly rely on, and Warcraft 3's own MPQ reader refuses those clean rebuilds. This
/// patcher instead:
///   - Leaves every byte of the original file untouched EXCEPT the specific hash/block
///     table entries for the file(s) being replaced/added.
///   - Appends new file content to the physical end of the file (never overwrites/moves
///     existing file data).
///   - Only relocates the block table if a brand-new file (no existing entry) is being
///     added and the block table needs to grow; the hash table never needs to grow, since
///     new entries reuse an existing empty/deleted slot.
///
/// This is a from-scratch reimplementation of the standard (public, long-documented) MPQ
/// StormBuffer hash/encrypt/decrypt algorithm, copied to match War3Net's own
/// implementation exactly (verified against War3Net.IO.Mpq/StormBuffer.cs) — it doesn't
/// reuse War3Net's classes directly because HashTable/BlockTable/StormBuffer are internal
/// to that library.
/// </summary>
public static class MpqInPlacePatcher
{
    private const uint FileExistsFlag = 0x80000000;

    private static readonly uint[] CryptTable = BuildCryptTable();

    private static uint[] BuildCryptTable()
    {
        var table = new uint[0x500];
        uint seed = 0x100001;
        for (uint index1 = 0; index1 < 0x100; index1++)
        {
            var index2 = index1;
            for (var i = 0; i < 5; i++, index2 += 0x100)
            {
                seed = ((seed * 125) + 3) % 0x2AAAAB;
                var temp = (seed & 0xFFFF) << 16;
                seed = ((seed * 125) + 3) % 0x2AAAAB;
                table[index2] = temp | (seed & 0xFFFF);
            }
        }

        return table;
    }

    private static uint HashString(string input, int offset)
    {
        uint seed1 = 0x7FED7FED;
        uint seed2 = 0xEEEEEEEE;
        foreach (var ch in input.ToUpperInvariant())
        {
            var val = (int)ch;
            seed1 = CryptTable[offset + val] ^ (seed1 + seed2);
            seed2 = (uint)val + seed1 + seed2 + (seed2 << 5) + 3;
        }

        return seed1;
    }

    private static void DecryptBlock(byte[] data, uint seed1)
    {
        uint seed2 = 0xEEEEEEEE;
        for (var i = 0; i < data.Length - 3; i += 4)
        {
            seed2 += CryptTable[0x400 + (seed1 & 0xFF)];
            var result = BitConverter.ToUInt32(data, i);
            result ^= seed1 + seed2;
            seed1 = ((~seed1 << 21) + 0x11111111) | (seed1 >> 11);
            seed2 = result + seed2 + (seed2 << 5) + 3;
            data[i + 0] = (byte)result;
            data[i + 1] = (byte)(result >> 8);
            data[i + 2] = (byte)(result >> 16);
            data[i + 3] = (byte)(result >> 24);
        }
    }

    private static void EncryptBlock(byte[] data, uint seed1)
    {
        uint seed2 = 0xEEEEEEEE;
        for (var i = 0; i < data.Length - 3; i += 4)
        {
            seed2 += CryptTable[0x400 + (seed1 & 0xFF)];
            var unencrypted = BitConverter.ToUInt32(data, i);
            var result = unencrypted ^ (seed1 + seed2);
            seed1 = ((~seed1 << 21) + 0x11111111) | (seed1 >> 11);
            seed2 = unencrypted + seed2 + (seed2 << 5) + 3;
            data[i + 0] = (byte)result;
            data[i + 1] = (byte)(result >> 8);
            data[i + 2] = (byte)(result >> 16);
            data[i + 3] = (byte)(result >> 24);
        }
    }

    private static uint TableKey(string tableName) => HashString(tableName, 0x300);

    private struct HashEntry
    {
        public ulong Name;
        public uint Locale;
        public uint BlockIndex;
        public bool IsSentinel => BlockIndex >= 0xFFFFFFFE; // empty (0xFFFFFFFF) or deleted (0xFFFFFFFE)
    }

    private struct BlockEntry
    {
        public uint FileOffset;
        public uint CompressedSize;
        public uint FileSize;
        public uint Flags;
    }

    /// <summary>
    /// Locates the real MPQ header. Checks for a formal "MPQ\x1B" user-data block first
    /// (and reads its HeaderOffset field directly, no guessing needed); otherwise scans for
    /// "MPQ\x1A" and validates the candidate by checking that BlockTableOffset lines up with
    /// HashTableOffset + HashTableSize * 16 (as War3Net's own parser does in DEBUG builds),
    /// so an incidental byte match earlier in a custom pre-header block can't be mistaken
    /// for the real header.
    /// </summary>
    private static int FindHeaderOffset(byte[] data)
    {
        if (data.Length >= 12 && data[0] == 'M' && data[1] == 'P' && data[2] == 'Q' && data[3] == 0x1B)
        {
            var headerOffset = (int)BitConverter.ToUInt32(data, 8);
            if (IsPlausibleHeader(data, headerOffset))
                return headerOffset;
        }

        for (var i = 0; i <= data.Length - 32; i++)
        {
            if (data[i] == 'M' && data[i + 1] == 'P' && data[i + 2] == 'Q' && data[i + 3] == 0x1A && IsPlausibleHeader(data, i))
                return i;
        }

        throw new InvalidOperationException("Could not locate a valid MPQ header in the original map file.");
    }

    private static bool IsPlausibleHeader(byte[] data, int headerOffset)
    {
        if (headerOffset < 0 || headerOffset + 32 > data.Length)
            return false;

        var hashTableOffset = BitConverter.ToUInt32(data, headerOffset + 16);
        var blockTableOffset = BitConverter.ToUInt32(data, headerOffset + 20);
        var hashTableSize = BitConverter.ToUInt32(data, headerOffset + 24);

        return blockTableOffset == hashTableOffset + (hashTableSize * 16);
    }

    /// <summary>
    /// Writes the given files into the archive at <paramref name="mapPath"/>, producing
    /// <paramref name="outputMapPath"/>. Existing files are replaced in place; files with no
    /// existing entry are appended and given a new block-table entry (and, if needed, the
    /// block table is relocated to make room — the hash table never needs to move).
    /// </summary>
    public static void Patch(string mapPath, string outputMapPath, IReadOnlyDictionary<string, byte[]> filesToWrite)
    {
        var data = File.ReadAllBytes(mapPath);
        var headerOffset = FindHeaderOffset(data);

        uint ReadU32(int pos) => BitConverter.ToUInt32(data, pos);
        ushort ReadU16(int pos) => BitConverter.ToUInt16(data, pos);

        var hashTableOffsetRel = ReadU32(headerOffset + 16);
        var blockTableOffsetRel = ReadU32(headerOffset + 20);
        var hashTableSize = ReadU32(headerOffset + 24);
        var blockTableSize = ReadU32(headerOffset + 28);

        var hashTableAbs = headerOffset + (int)hashTableOffsetRel;
        var blockTableAbs = headerOffset + (int)blockTableOffsetRel;

        var hashBytes = new byte[hashTableSize * 16];
        Array.Copy(data, hashTableAbs, hashBytes, 0, hashBytes.Length);
        DecryptBlock(hashBytes, TableKey("(hash table)"));

        var blockBytes = new byte[blockTableSize * 16];
        Array.Copy(data, blockTableAbs, blockBytes, 0, blockBytes.Length);
        DecryptBlock(blockBytes, TableKey("(block table)"));

        var hashEntries = new List<HashEntry>((int)hashTableSize);
        for (var i = 0; i < hashTableSize; i++)
        {
            var o = i * 16;
            hashEntries.Add(new HashEntry
            {
                Name = BitConverter.ToUInt64(hashBytes, o),
                Locale = BitConverter.ToUInt32(hashBytes, o + 8),
                BlockIndex = BitConverter.ToUInt32(hashBytes, o + 12),
            });
        }

        var blockEntries = new List<BlockEntry>((int)blockTableSize);
        for (var i = 0; i < blockTableSize; i++)
        {
            var o = i * 16;
            blockEntries.Add(new BlockEntry
            {
                FileOffset = BitConverter.ToUInt32(blockBytes, o),
                CompressedSize = BitConverter.ToUInt32(blockBytes, o + 4),
                FileSize = BitConverter.ToUInt32(blockBytes, o + 8),
                Flags = BitConverter.ToUInt32(blockBytes, o + 12),
            });
        }

        var appendCursor = (uint)data.Length;
        var appendedChunks = new List<byte[]>();
        var hashTableMask = hashTableSize - 1;
        var blockTableGrew = false;

        foreach (var kvp in filesToWrite)
        {
            var fileName = kvp.Key;
            var content = kvp.Value;

            var name1 = HashString(fileName, 0x100);
            var name2 = HashString(fileName, 0x200);
            var nameHash = name1 | ((ulong)name2 << 32);

            var hashIdx = hashEntries.FindIndex(h => !h.IsSentinel && h.Name == nameHash);
            uint blockIndex;

            if (hashIdx >= 0)
            {
                blockIndex = hashEntries[hashIdx].BlockIndex;
            }
            else
            {
                // Brand-new file (e.g. war3mapMisc.txt didn't exist before): add a block
                // entry and slot it into the first available (empty/deleted) hash slot via
                // standard linear probing.
                blockIndex = (uint)blockEntries.Count;
                blockEntries.Add(default);
                blockTableGrew = true;

                var probe = HashString(fileName, 0) & hashTableMask;
                var placed = false;
                for (var step = 0u; step <= hashTableMask; step++)
                {
                    var idx = (int)((probe + step) & hashTableMask);
                    if (hashEntries[idx].IsSentinel)
                    {
                        hashEntries[idx] = new HashEntry { Name = nameHash, Locale = 0, BlockIndex = blockIndex };
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                    throw new InvalidOperationException($"Hash table has no free slot to add '{fileName}'.");
            }

            blockEntries[(int)blockIndex] = new BlockEntry
            {
                FileOffset = appendCursor - (uint)headerOffset,
                CompressedSize = (uint)content.Length,
                FileSize = (uint)content.Length,
                Flags = FileExistsFlag, // stored raw: uncompressed, unencrypted, no sector table needed
            };

            appendedChunks.Add(content);
            appendCursor += (uint)content.Length;
        }

        var newHashBytes = new byte[hashEntries.Count * 16];
        for (var i = 0; i < hashEntries.Count; i++)
        {
            var o = i * 16;
            BitConverter.GetBytes(hashEntries[i].Name).CopyTo(newHashBytes, o);
            BitConverter.GetBytes(hashEntries[i].Locale).CopyTo(newHashBytes, o + 8);
            BitConverter.GetBytes(hashEntries[i].BlockIndex).CopyTo(newHashBytes, o + 12);
        }

        var newBlockBytes = new byte[blockEntries.Count * 16];
        for (var i = 0; i < blockEntries.Count; i++)
        {
            var o = i * 16;
            BitConverter.GetBytes(blockEntries[i].FileOffset).CopyTo(newBlockBytes, o);
            BitConverter.GetBytes(blockEntries[i].CompressedSize).CopyTo(newBlockBytes, o + 4);
            BitConverter.GetBytes(blockEntries[i].FileSize).CopyTo(newBlockBytes, o + 8);
            BitConverter.GetBytes(blockEntries[i].Flags).CopyTo(newBlockBytes, o + 12);
        }

        EncryptBlock(newHashBytes, TableKey("(hash table)"));
        EncryptBlock(newBlockBytes, TableKey("(block table)"));

        using var output = new MemoryStream();
        output.Write(data, 0, data.Length);

        // Hash table size never changes (new entries reuse an existing empty/deleted slot),
        // so it always gets patched back in at its original position.
        output.Position = hashTableAbs;
        output.Write(newHashBytes, 0, newHashBytes.Length);

        uint finalBlockTableOffsetRel = blockTableOffsetRel;
        uint finalBlockTableSize = (uint)blockEntries.Count;

        if (!blockTableGrew)
        {
            // Same entry count -> patch in place, nothing else needs to move.
            output.Position = blockTableAbs;
            output.Write(newBlockBytes, 0, newBlockBytes.Length);
        }

        output.Position = output.Length;
        foreach (var chunk in appendedChunks)
            output.Write(chunk, 0, chunk.Length);

        if (blockTableGrew)
        {
            // No room was reserved for the extra entries at the old location — relocate the
            // block table to just past the newly-appended file data instead of overwriting
            // anything. The old block table bytes are simply left behind as unused space,
            // same as how a deleted file leaves a gap in a normal MPQ.
            finalBlockTableOffsetRel = (uint)(output.Length - headerOffset);
            output.Position = output.Length;
            output.Write(newBlockBytes, 0, newBlockBytes.Length);
            finalBlockTableSize = (uint)blockEntries.Count;
        }

        // Patch the header's BlockTableOffset/BlockTableSize and ArchiveSize fields.
        // HeaderOffset, DataOffset, HashTableOffset/Size and the entire pre-header are left
        // completely untouched, which is exactly what keeps any anti-tamper tricks intact.
        var finalBytes = output.ToArray();
        void WriteU32(int pos, uint value) => BitConverter.GetBytes(value).CopyTo(finalBytes, pos);

        WriteU32(headerOffset + 8, (uint)(finalBytes.Length - headerOffset)); // ArchiveSize
        WriteU32(headerOffset + 20, finalBlockTableOffsetRel);               // BlockTableOffset
        WriteU32(headerOffset + 28, finalBlockTableSize);                    // BlockTableSize

        File.WriteAllBytes(outputMapPath, finalBytes);
    }
}
