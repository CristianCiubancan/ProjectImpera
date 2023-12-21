// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTradeBuddy.cs
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
    public sealed class MsgTradeBuddy : MsgBase<Client>
    {
        public enum TradeBuddyAction
        {
            RequestPartnership = 0,
            RejectRequest = 1,
            BreakPartnership = 4,
            AddPartner = 5
        }

        public MsgTradeBuddy()
        {
            Type = PacketType.MsgTradeBuddy;
        }

        public uint Identity;
        public TradeBuddyAction Action;
        public bool IsOnline;
        public int HoursLeft;
        public string Name;

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Action = (TradeBuddyAction) reader.ReadByte();
            IsOnline = reader.ReadBoolean();
            HoursLeft = reader.ReadInt32();
            reader.ReadUInt16();
            Name = reader.ReadString(16);
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Identity);
            writer.Write((byte)Action);
            writer.Write(IsOnline);
            writer.Write(HoursLeft);
            writer.Write((ushort)0);
            writer.Write(Name, 16);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = Kernel.RoleManager.GetUser(Identity);
            switch (Action)
            {
                case TradeBuddyAction.RequestPartnership:
                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTargetNotInRange);
                        return;
                    }

                    if (user.QueryRequest(RequestType.TradePartner) == target.Identity)
                    {
                        user.PopRequest(RequestType.TradePartner);
                        await user.CreateTradePartnerAsync(target);
                        return;
                    }

                    target.SetRequest(RequestType.TradePartner, user.Identity);
                    Identity = user.Identity;
                    Name = user.Name;
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);
                    break;

                case TradeBuddyAction.RejectRequest:
                    if (target == null)
                        return;

                    Identity = user.Identity;
                    Name = user.Name;
                    IsOnline = true;
                    await target.SendAsync(this);
                    break;

                case TradeBuddyAction.BreakPartnership:
                    await user.DeleteTradePartnerAsync(Identity);
                    break;
            }
        }
    }
}