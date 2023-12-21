// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgUserAttrib.cs
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
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgUserAttrib : MsgBase<Client>
    {
        private readonly List<UserAttribute> Attributes = new List<UserAttribute>();

        public MsgUserAttrib(uint idRole, ClientUpdateType type, ulong value)
        {
            Type = PacketType.MsgUserAttrib;

            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, value));
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, uint value0, uint value1)
        {
            Type = PacketType.MsgUserAttrib;

            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint)type, value0, value1));
        }

        public uint Identity { get; set; }

        public int Amount { get; set; }

        public void Append(ClientUpdateType type, ulong data)
        {
            Amount++;
            Attributes.Add(new UserAttribute((uint) type, data));
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Identity);
            Amount = Attributes.Count;
            writer.Write(Amount);
            for (int i = 0; i < Amount; i++)
            {
                writer.Write(Attributes[i].Type);
                writer.Write(Attributes[i].Data);
            }

            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            await Log.WriteLogAsync(LogLevel.Warning, "Unhandled MsgUserAttrib::Process call");
        }

        private readonly struct UserAttribute
        {
            public UserAttribute(uint type, ulong data)
            {
                Type = type;
                Data = data;
            }

            public UserAttribute(uint type, uint left, uint right)
            {
                Type = type;
                Data = (ulong) left << 32 | right;
            }

            public readonly uint Type;
            public readonly ulong Data;
        }
    }

    public enum ClientUpdateType
    {
        Hitpoints = 0,
        MaxHitpoints = 1,
        Mana = 2,
        MaxMana = 3,
        Money = 4,
        Experience = 5,
        PkPoints = 6,
        Class = 7,
        Stamina = 8,
        Atributes = 10,
        Mesh,
        Level,
        Spirit,
        Vitality,
        Strength,
        Agility,
        HeavensBlessing,
        DoubleExpTimer,
        CursedTimer = 20,
        Reborn = 22,
        StatusFlag = 25,
        HairStyle = 26,
        XpCircle = 27,
        LuckyTimeTimer = 28,
        ConquerPoints = 29,
        OnlineTraining = 31,
        ExtraBattlePower = 36,
        Merchant = 38,
        VipLevel = 39,
        QuizPoints = 40,
        EnlightenPoints = 41,
        FamilySharedBattlePower = 42,
        TotemPoleBattlePower = 44,

        Vigor = 10000
    }
}