// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgFriend.cs
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

using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgFriend : MsgBase<Client>
    {
        public MsgFriend()
        {
            Type = PacketType.MsgFriend;
        }

        public uint Identity { get; set; }
        public MsgFriendAction Action { get; set; }
        public bool Online { get; set; }
        public string Name { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16(); // 0
            Type = (PacketType)reader.ReadUInt16(); // 2
            Identity = reader.ReadUInt32(); // 4
            Action = (MsgFriendAction) reader.ReadByte(); // 8
            Online = reader.ReadBoolean(); // 9
            reader.ReadInt16(); // 10
            reader.ReadInt32(); // 12
            reader.ReadInt32(); // 16
            Name = reader.ReadString(16); // 20
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
            writer.Write(Identity);
            writer.Write((byte) Action);
            writer.Write(Online);
            writer.Write((ushort) 0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(Name, 16);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            Character target = null;
            switch (Action)
            {
                case MsgFriendAction.RequestFriend:
                    target = Kernel.RoleManager.GetUser(Identity);
                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTargetNotOnline);
                        return;
                    }

                    if (user.FriendAmount >= user.MaxFriendAmount)
                    {
                        await user.SendAsync(Language.StrFriendListFull);
                        return;
                    }

                    if (target.FriendAmount >= target.MaxFriendAmount)
                    {
                        await user.SendAsync(Language.StrTargetFriendListFull);
                        return;
                    }

                    uint request = target.QueryRequest(RequestType.Friend);
                    if (request == user.Identity)
                    {
                        target.PopRequest(RequestType.Friend);
                        await target.CreateFriendAsync(user);
                    }
                    else
                    {
                        user.SetRequest(RequestType.Friend, target.Identity);
                        await target.SendAsync(new MsgFriend
                        {
                            Action = MsgFriendAction.RequestFriend,
                            Identity = user.Identity,
                            Name = user.Name
                        });
                        await user.SendAsync(Language.StrMakeFriendSent);
                    }
                    break;

                case MsgFriendAction.RemoveFriend:
                    await user.DeleteFriendAsync(Identity, true);
                    break;

                case MsgFriendAction.RemoveEnemy:
                    await user.DeleteEnemyAsync(Identity);
                    break;
            }
        }

        public enum MsgFriendAction : byte
        {
            RequestFriend = 10,
            NewFriend = 11,
            SetOnlineFriend = 12,
            SetOfflineFriend = 13,
            RemoveFriend = 14,
            AddFriend = 15,
            SetOnlineEnemy = 16,
            SetOfflineEnemy = 17,
            RemoveEnemy = 18,
            AddEnemy = 19,
        }
    }
}