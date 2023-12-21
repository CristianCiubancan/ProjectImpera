// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgRelation.cs
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

using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgRelation : MsgBase<Client>
    {
        public uint SenderIdentity;
        public uint TargetIdentity;
        public int Level;
        public int BattlePower;
        public bool IsSpouse;
        public bool IsTutor;
        public bool IsTradePartner;

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgRelation);
            writer.Write(SenderIdentity);
            writer.Write(TargetIdentity);
            writer.Write(Level);
            writer.Write(BattlePower);
            writer.Write(IsSpouse ? 1 : 0);
            writer.Write(IsTutor ? 1 : 0);
            writer.Write(IsTradePartner ? 1 : 0);
            return writer.ToArray();
        }
    }
}