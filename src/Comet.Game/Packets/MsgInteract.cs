// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgInteract.cs
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;
using Org.BouncyCastle.Asn1.Crmf;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgInteract : MsgBase<Client>
    {
        public MsgInteract()
        {
            Type = PacketType.MsgInteract;

            Timestamp = Environment.TickCount;
        }

        public int Timestamp { get; set; }
        public uint SenderIdentity { get; set; }
        public uint TargetIdentity { get; set; }
        public ushort PosX { get; set; }
        public ushort PosY { get; set; }
        public MsgInteractType Action { get; set; }
        public int Data { get; set; }
        public int Command { get; set; }

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
            Type = (PacketType) reader.ReadUInt16();
            Timestamp = reader.ReadInt32(); // 4
            SenderIdentity = reader.ReadUInt32(); // 8
            TargetIdentity = reader.ReadUInt32(); // 12
            PosX = reader.ReadUInt16(); // 16
            PosY = reader.ReadUInt16(); // 18
            Action = (MsgInteractType) reader.ReadUInt32(); // 20
            Data = reader.ReadInt32(); // 24
            Command = reader.ReadInt32(); // 28
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
            writer.Write((ushort) Type);
            writer.Write(Timestamp);
            writer.Write(SenderIdentity);
            writer.Write(TargetIdentity);
            writer.Write(PosX);
            writer.Write(PosY);
            writer.Write((uint) Action);
            writer.Write(Data);
            writer.Write(Command);
            return writer.ToArray();
        }

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (Action != (MsgInteractType) 38 && Action != MsgInteractType.AcceptGuildMember)
                user.BattleSystem.ResetBattle();

            Role target = user.Map.QueryAroundRole(client.Character, TargetIdentity);

            switch (Action)
            {
                case MsgInteractType.Attack:
                case MsgInteractType.Shoot:
                    if (!user.IsAlive)
                        return;

                    if (SenderIdentity == client.Identity)
                    {
                        if (client.Character.SetAttackTarget(target))
                            client.Character.BattleSystem.CreateBattle(TargetIdentity);
                    }
                    break;

                case MsgInteractType.MagicAttack:
                    if (!user.IsAlive)
                        return;

                    byte[] dataArray = BitConverter.GetBytes(Data);
                    ushort magicType = Convert.ToUInt16((dataArray[0] & 0xFF) | ((dataArray[1] & 0xFF) << 8));
                    magicType ^= 0x915d;
                    magicType ^= (ushort) client.Identity;
                    magicType = (ushort) ((magicType << 0x3) | (magicType >> 0xd));
                    magicType -= 0xeb42;

                    dataArray = BitConverter.GetBytes(TargetIdentity);
                    TargetIdentity = ((uint) dataArray[0] & 0xFF) | (((uint) dataArray[1] & 0xFF) << 8) |
                                     (((uint) dataArray[2] & 0xFF) << 16) | (((uint) dataArray[3] & 0xFF) << 24);
                    TargetIdentity = ((((TargetIdentity & 0xffffe000) >> 13) | ((TargetIdentity & 0x1fff) << 19)) ^
                                      0x5F2D2463 ^ client.Identity) -
                                     0x746F4AE6;

                    dataArray = BitConverter.GetBytes(PosX);
                    long xx = (dataArray[0] & 0xFF) | ((dataArray[1] & 0xFF) << 8);
                    dataArray = BitConverter.GetBytes(PosY);
                    long yy = (dataArray[0] & 0xFF) | ((dataArray[1] & 0xFF) << 8);
                    xx = xx ^ (client.Identity & 0xffff) ^ 0x2ed6;
                    xx = ((xx << 1) | ((xx & 0x8000) >> 15)) & 0xffff;
                    xx |= 0xffff0000;
                    xx -= 0xffff22ee;
                    yy = yy ^ (client.Identity & 0xffff) ^ 0xb99b;
                    yy = ((yy << 5) | ((yy & 0xF800) >> 11)) & 0xffff;
                    yy |= 0xffff0000;
                    yy -= 0xffff8922;
                    PosX = Convert.ToUInt16(xx);
                    PosY = Convert.ToUInt16(yy);

                    if (client.Character.IsAlive)
                        await client.Character.ProcessMagicAttackAsync(magicType, TargetIdentity, PosX, PosY);

                    break;

                case MsgInteractType.Chop:
                    Item targetItem = client.Character.UserPackage.GetItemByType(Item.TYPE_JAR);
                    if (targetItem == null)
                        return;

                    Command = (ushort) (targetItem.Data * 2);
                    await client.SendAsync(this);
                    break;

                case MsgInteractType.Court:
                {
                    if (target == null || target.Identity == user.Identity)
                        return;

                    if (!(target is Character targetUser))
                        return;

                    if (targetUser.MapIdentity != user.MapIdentity || user.GetDistance(targetUser) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTargetNotInRange);
                        return;
                    }

                    if (targetUser.MateIdentity != 0)
                    {
                        await user.SendAsync(Language.StrMarriageTargetNotSingle);
                        return; // target is already married
                    }

                    if (user.MateIdentity != 0)
                    {
                        await user.SendAsync(Language.StrMarriageYouNoSingle);
                        return; // you're already married
                    }

                    if (user.Gender == targetUser.Gender)
                    {
                        await user.SendAsync(Language.StrMarriageErrSameGender);
                        return; // not allow same gender
                    }

                    targetUser.SetRequest(RequestType.Marriage, user.Identity);
                    await targetUser.SendAsync(this);
                    await targetUser.SendRelationAsync(user);
                    break;
                }

                case MsgInteractType.Marry:
                {
                    if (target == null || target.Identity == user.Identity)
                        return;

                    if (!(target is Character targetUser))
                        return;

                    if (user.QueryRequest(RequestType.Marriage) != targetUser.Identity)
                    {
                        await user.SendAsync(Language.StrMarriageNotApply);
                        return;
                    }

                    user.PopRequest(RequestType.Marriage);

                    if (targetUser.MapIdentity != user.MapIdentity || user.GetDistance(targetUser) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTargetNotInRange);
                        return;
                    }

                    if (targetUser.MateIdentity != 0)
                    {
                        await user.SendAsync(Language.StrMarriageTargetNotSingle);
                        return; // target is already married
                    }

                    if (user.MateIdentity != 0)
                    {
                        await user.SendAsync(Language.StrMarriageYouNoSingle);
                        return; // you're already married
                    }

                    if (user.Gender == targetUser.Gender)
                    {
                        await user.SendAsync(Language.StrMarriageErrSameGender);
                        return; // not allow same gender
                    }

                    user.MateIdentity = targetUser.Identity;
                    user.MateName = targetUser.Name;
                    await user.SaveAsync();
                    targetUser.MateIdentity = user.Identity;
                    targetUser.MateName = user.Name;
                    await targetUser.SaveAsync();

                    await user.SendAsync(new MsgName
                    {
                        Identity = targetUser.Identity,
                        Strings = new List<string> { targetUser.Name },
                        Action = StringAction.Mate
                    });

                    await targetUser.SendAsync(new MsgName
                    {
                        Identity = user.Identity,
                        Strings = new List<string> {user.Name},
                        Action = StringAction.Mate
                    });

                    await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrMarry, targetUser.Name, user.Name), MsgTalk.TalkChannel.Center, Color.Red);
                    break;
                }

                case MsgInteractType.InitialMerchant:
                case MsgInteractType.AcceptMerchant:
                {
                    // ON ACCEPT: Sender = 1 Target = 1
                    await user.SetMerchantAsync();
                    break;
                }

                case MsgInteractType.CancelMerchant:
                {
                    await user.RemoveMerchantAsync();
                    break;
                }

                case MsgInteractType.MerchantProgress:
                {
                    break;
                }

                case MsgInteractType.PresentEmoney:
                {
                    await user.DoCardsAsync();
                    break;
                }

                case MsgInteractType.CounterKillSwitch:
                {
                    if (user.MagicData[6003] == null) // must have skill
                        return;

                    if (!user.IsAlive || user.IsWing)
                        return;

                    await user.SetScapegoatAsync(!user.Scapegoat);
                    break;
                }

                default:
                    await client.SendAsync(new MsgTalk(client.Identity, MsgTalk.TalkChannel.Service,
                        $"Missing packet {Type}, Action {Action}, Length {Length}"));
                    await Log.WriteLogAsync(LogLevel.Warning,
                        "Missing packet {0}, Action {1}, Length {2}\n{3}",
                        Type, Action, Length, PacketDump.Hex(Encode()));
                    break;
            }
        }
    }

    public enum MsgInteractType : uint
    {
        None = 0,
        Steal = 1,
        Attack = 2,
        Heal = 3,
        Poison = 4,
        Assassinate = 5,
        Freeze = 6,
        Unfreeze = 7,
        Court = 8,
        Marry = 9,
        Divorce = 10,
        PresentMoney = 11,
        PresentItem = 12,
        SendFlowers = 13,
        Kill = 14,
        JoinGuild = 15,
        AcceptGuildMember = 16,
        KickoutGuildMember = 17,
        PresentPower = 18,
        QueryInfo = 19,
        RushAttack = 20,
        Unknown21 = 21,
        AbortMagic = 22,
        ReflectWeapon = 23,
        MagicAttack = 24,
        Shoot5065 = 25,
        ReflectMagic = 26,
        Dash = 27,
        Shoot = 28,
        Quarry = 29,
        Chop = 30,
        Hustle = 31,
        Soul = 32,
        AcceptMerchant = 33,
        IncreaseJar = 36,
        PresentEmoney = 39,
        InitialMerchant = 40,
        CancelMerchant = 41,
        MerchantProgress = 42,
        CounterKill = 43,
        CounterKillSwitch = 44,
        FatalStrike = 45,
        InteractRequest = 46,
        InteractConfirm,
        Interact,
        InteractUnknown,
        InteractStop,
        AzureDmg = 55
    }

    [Flags]
    public enum InteractionEffect : ushort
    {
        None = 0x0,
        Block = 0x1, // 1
        Penetration = 0x2, // 2
        CriticalStrike = 0x4, // 4
        Breakthrough = 0x2, // 8
        MetalResist = 0x10, // 16
        WoodResist = 0x20, // 32
        WaterResist = 0x40, // 64
        FireResist = 0x80, // 128
        EarthResist = 0x100,
    }
}