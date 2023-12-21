// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgEquipLock.cs
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
using Comet.Game.States.Items;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgEquipLock : MsgBase<Client>
    {
        public enum LockMode : byte
        {
            RequestLock = 0,
            RequestUnlock = 1,
            UnlockDate = 2,
            UnlockedItem = 3
        }


        public MsgEquipLock()
        {
            Type = PacketType.MsgEquipLock;
        }

        public uint Identity { get; set; }
        public LockMode Action { get; set; }
        public byte Mode { get; set; }
        public uint Param { get; set; }

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Action = (LockMode) reader.ReadByte();
            Mode = reader.ReadByte();
            reader.ReadUInt16();
            Param = reader.ReadUInt32();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Identity);
            writer.Write((byte) Action);
            writer.Write(Mode);
            writer.Write((ushort) 0);
            writer.Write(Param);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            Item item = client.Character.UserPackage.FindByIdentity(Identity);
            if (item == null)
            {
                await client.Character.SendAsync(Language.StrItemNotFound);
                return;
            }

            switch (Action)
            {
                case LockMode.RequestLock:
                    if (item.IsLocked() && !item.IsUnlocking())
                    {
                        await client.Character.SendAsync(Language.StrEquipLockAlreadyLocked);
                        return;
                    }

                    if (!item.IsEquipment() && !item.IsMount())
                    {
                        await client.Character.SendAsync(Language.StrEquipLockCantLock);
                        return;
                    }

                    await item.SetLockAsync();
                    await client.SendAsync(this);
                    await client.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                    break;
                case LockMode.RequestUnlock:

                    if (!item.IsLocked())
                    {
                        await client.Character.SendAsync(Language.StrEquipLockNotLocked);
                        return;
                    }

                    if (item.IsUnlocking())
                    {
                        await client.Character.SendAsync(Language.StrEquipLockAlreadyUnlocking);
                        return;
                    }

                    await item.SetUnlockAsync();
                    await client.SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
                    await item.TryUnlockAsync();
                    break;
            }
        }
    }
}