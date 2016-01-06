﻿// This file is part of PosiStageDotNet.
// 
// PosiStageDotNet is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// PosiStageDotNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with PosiStageDotNet.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Imp.PosiStageDotNet.Serialization;

namespace Imp.PosiStageDotNet
{
	public class PsnDataPacket : IPsnPacketId
	{
		const int HeaderByteLength = 12;

		public PsnDataPacket(ulong timestamp, int versionHigh, int versionLow, int frameId, int framePacketCount,
			IDictionary<ushort, IEnumerable<IDataTrackerId>> dataTrackers)
		{
			TimeStamp = timestamp;

			if (versionHigh < 0 || versionHigh > 255)
				throw new ArgumentOutOfRangeException(nameof(versionHigh), "versionHigh must be between 0 and 255");

			VersionHigh = versionHigh;

			if (versionLow < 0 || versionLow > 255)
				throw new ArgumentOutOfRangeException(nameof(versionLow), "versionLow must be between 0 and 255");

			VersionLow = versionLow;

			if (frameId < 0 || frameId > 255)
				throw new ArgumentOutOfRangeException(nameof(frameId), "frameId must be between 0 and 255");

			FrameId = frameId;

			if (framePacketCount < 0 || framePacketCount > 255)
				throw new ArgumentOutOfRangeException(nameof(framePacketCount), "framePacketCount must be between 0 and 255");

			FramePacketCount = framePacketCount;

			if (dataTrackers == null)
				throw new ArgumentNullException(nameof(dataTrackers));

			DataTrackers = dataTrackers.ToDictionary(p => p.Key, p => (IReadOnlyList<IDataTrackerId>)p.Value.ToList());
		}

		public PsnChunkId Id => PsnChunkId.PsnDataPacket;

		public ulong TimeStamp { get; }

		public int VersionHigh { get; }
		public int VersionLow { get; }
		public int FrameId { get; }
		public int FramePacketCount { get; }

		public IReadOnlyDictionary<ushort, IReadOnlyList<IDataTrackerId>> DataTrackers { get; }
			

		public byte[] ToByteArray()
		{
			int trackerListChunkByteLength = DataTrackers.Sum(p => PsnBinaryWriter.ChunkHeaderByteLength 
												+ p.Value.Sum(t => PsnBinaryWriter.ChunkHeaderByteLength + t.ByteLength));

			int rootChunkByteLength = PsnBinaryWriter.ChunkHeaderByteLength + HeaderByteLength + trackerListChunkByteLength;

			using (var ms = new MemoryStream())
			using (var writer = new PsnBinaryWriter(ms))
			{
				writer.WriteChunkHeader((ushort)Id, rootChunkByteLength, true);

				// Write header
				writer.WriteChunkHeader((ushort)PsnDataChunkId.PsnDataPacketHeader, HeaderByteLength, false);
				writer.Write(TimeStamp);
				writer.Write(VersionHigh);
				writer.Write(VersionLow);
				writer.Write(FrameId);
				writer.Write(FramePacketCount);

				// Write tracker List
				writer.WriteChunkHeader((ushort)PsnDataChunkId.PsnDataTrackerList, trackerListChunkByteLength, true);

				foreach (var pair in DataTrackers)
				{
					writer.WriteChunkHeader(pair.Key, pair.Value.Sum(t => t.ByteLength), true);

					foreach (var tracker in pair.Value)
						tracker.Serialize(writer);
				}

				return ms.ToArray();
			}
		}

		
	}
}