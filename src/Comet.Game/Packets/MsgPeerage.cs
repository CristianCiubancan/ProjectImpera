// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgNobility.cs
// Description:
// 
// Creator: FELIPEVIEIRAVENDRAMI [FELIPE VIEIRA VENDRAMINI]
// 
// Developed by:
// Felipe Vieira Vendramini <felipevendramini@live.com>
// 
// Programming today is a race between software engineers striving to build bigger and better
// idiot-proof programs, and the Universe trying to produce bigger and better idiots.
// So far, the Universe is winning.
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#region References

using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgPeerage : MsgBase<Client>
    {
        public MsgPeerage()
        {
            Type = PacketType.MsgPeerage;
            Data = 0;
        }

        public MsgPeerage(NobilityAction action, ushort maxPerPage, ushort maxPages)
        {
            Type = PacketType.MsgPeerage;
            Action = action;
            DataLow2 = maxPages;
            DataHigh = maxPerPage;
        }

        public NobilityAction Action { get; set; }
        public ulong Data { get; set; }

        public uint DataLow
        {
            get => (uint)(Data - ((ulong) DataHigh << 32));
            set => Data = (ulong) DataHigh << 32 | value;
        }

        public ushort DataLow1
        {
            get => (ushort)(DataLow - (DataLow2 << 16));
            set => DataLow = (uint)(DataLow2 << 16 | value);
        }

        public ushort DataLow2
        {
            get => (ushort)(DataLow >> 16);
            set => DataLow = (uint)(value << 16) | DataLow;
        }

        public uint DataHigh
        {
            get => (uint)(Data >> 32);
            set => Data = ((ulong) value << 32) | Data;
        }

        public ushort DataHigh1
        {
            get => (ushort)(DataHigh - (DataHigh2 << 16));
            set => DataHigh = (uint)(DataHigh2 << 16 | value);
        }

        public ushort DataHigh2
        {
            get => (ushort)(DataHigh >> 16);
            set => DataHigh = (uint)(value << 16) | DataHigh;
        }

        public uint Data1 { get; set; }
        public uint Data2 { get; set; }
        public uint Data3 { get; set; }
        public uint Data4 { get; set; }
        
        public List<string> Strings = new List<string>();
        public List<NobilityStruct> Rank = new List<NobilityStruct>();

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (NobilityAction) reader.ReadUInt32();
            Data = reader.ReadUInt64();
            Data1 = reader.ReadUInt32();
            Data2 = reader.ReadUInt32();
            Data3 = reader.ReadUInt32();
             Data4 = reader.ReadUInt32();
            Strings = reader.ReadStrings();
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort)Type);
            writer.Write((uint) Action); // 4
            writer.Write(Data); // 8
            writer.Write(Data1); // 16
            writer.Write(Data2); // 20
            writer.Write(Data3); // 24
            writer.Write(Data4); // 28

            if (Action == NobilityAction.List)
            {
                foreach (var rank in Rank)
                {
                    writer.Write(rank.Identity);
                    writer.Write(rank.LookFace);
                    writer.Write(rank.LookFace);
                    writer.Write(rank.Name, 16);
                    writer.Write(0);
                    writer.Write(rank.Donation);
                    writer.Write((uint) rank.Rank);
                    writer.Write(rank.Position);
                }
            }
            else
            {
                writer.Write(Strings);
            }

            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
#if NOBILITY
            Character user = client.Character;

            switch (Action)
            {
                case NobilityAction.Donate:
                    if (user.Level < 70)
                    {
                        await user.SendAsync(Language.StrPeerageDonateErrBelowLevel);
                        return;
                    }

                    if (Data < 3000000)
                    {
                        await user.SendAsync(Language.StrPeerageDonateErrBelowUnderline);
                        return;
                    }

                    if (Data <= user.Silvers)
                    {
                        if (!await user.SpendMoneyAsync((int) Data, true))
                            return;
                    }
                    else
                    {
                        if (!await user.SpendConquerPointsAsync((int)(Data/50000), true))
                            return;
                    }

                    await Kernel.PeerageManager.DonateAsync(user, Data);
                    break;
                case NobilityAction.List:
                    await Kernel.PeerageManager.SendRankingAsync(user, DataLow1);
                    break;
                case NobilityAction.QueryRemainingSilver:
                    Data = (uint) Kernel.PeerageManager.GetNextRankSilver((NobilityRank) DataLow1, user.NobilityDonation);
                    Data2 = 60;
                    Data3 = (uint) user.NobilityPosition;
                    await user.SendAsync(this);
                    break;
            }
#endif
        }

        public struct NobilityStruct
        {
            public uint Identity;
            public uint LookFace;
            public string Name;
            public ulong Donation;
            public NobilityRank Rank;
            public int Position;
        }
    }

    public enum NobilityAction : uint
    {
        None,
        Donate,
        List,
        Info,
        QueryRemainingSilver
    };

    public enum NobilityRank : byte
    {
        Serf,
        Knight,
        Baron = 3,
        Earl = 5,
        Duke = 7,
        Prince = 9,
        King = 12
    };
}