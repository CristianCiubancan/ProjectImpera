// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTitle.cs
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
    public sealed class MsgTitle : MsgBase<Client>
    {
        public uint Identity;
        public byte Title;
        public TitleAction Action;
        public byte Count;
        public List<byte> Titles = new List<byte>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Title = reader.ReadByte();
            Action = (TitleAction) reader.ReadByte();
            Count = reader.ReadByte();
            for (int i = 0; i < Count; i++)
                Titles.Add(reader.ReadByte());
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgTitle);
            writer.Write(Identity); // 4
            writer.Write(Title);
            writer.Write((byte) Action);
            writer.Write(Count = (byte) Titles.Count);
            foreach (var b in Titles)
                writer.Write(b);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user == null)
                return;

            switch (Action)
            {
                case TitleAction.Query:
                {
                    await user.SendTitlesAsync();
                    break;
                }

                case TitleAction.Select:
                {
                    if (Title != 0 && !user.HasTitle((Character.UserTitles) Title))
                        return;

                    user.UserTitle = Title;
                    await user.BroadcastRoomMsgAsync(this, true);
                    await user.SaveAsync();
                    break;
                }
            }
        }

        public enum TitleAction : byte
        {
            Hide = 0,
            Add = 1,
            Remove = 2,
            Select = 3,
            Query = 4
        }
    }
}