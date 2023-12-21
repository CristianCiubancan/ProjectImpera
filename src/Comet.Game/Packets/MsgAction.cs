// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgAction.cs
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
using System.Drawing;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Events;
using Comet.Game.States.Items;
using Comet.Game.States.Relationship;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;
using SColor = System.Drawing.Color;

#endregion

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1010</remarks>
    /// <summary>
    ///     Message containing a general action being performed by the client. Commonly used
    ///     as a request-response protocol for question and answer like exchanges. For example,
    ///     walk requests are responded to with an answer as to if the step is legal or not.
    /// </summary>
    public sealed class MsgAction : MsgBase<Client>
    {
        public MsgAction()
        {
            Timestamp = (uint) Environment.TickCount;
        }

        // Packet Properties
        public uint Timestamp { get; set; }
        public uint Identity { get; set; }
        public uint Data { get; set; }
        public uint Command { get; set; }

        public ushort CommandX
        {
            get => (ushort) (Command - (CommandY << 16));
            set => Command = (uint) (CommandY << 16 | value);
        }

        public ushort CommandY
        {
            get => (ushort) (Command >> 16);
            set => Command = (uint) (value << 16) | Command;
        }

        public uint Argument { get; set; }

        public ushort ArgumentX
        {
            get => (ushort)(Argument - (ArgumentY << 16));
            set => Argument = (uint)(ArgumentY << 16 | value);
        }

        public ushort ArgumentY
        {
            get => (ushort)(Argument >> 16);
            set => Argument = (uint)(value << 16) | Argument;
        }
        public ushort Direction { get; set; }
        public ActionType Action { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public uint Map { get; set; }
        public uint Color { get; set; }

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
            Identity = reader.ReadUInt32();
            Command = reader.ReadUInt32();
            Argument = reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
            Action = (ActionType)reader.ReadUInt16();
            Direction = reader.ReadUInt16();
            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
            Map = reader.ReadUInt32();
            Color = reader.ReadUInt32();
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
            writer.Write((ushort)PacketType.MsgAction);
            writer.Write(Identity);
            writer.Write(Command);
            writer.Write(Argument);
            writer.Write(Timestamp);
            writer.Write((ushort)Action);
            writer.Write(Direction);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Map);
            writer.Write(Color);
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

            Role target = null;
            Character targetUser = Kernel.RoleManager.GetUser(Command);

            switch (Action)
            {
                case ActionType.CharacterDirection: // 79
                case ActionType.CharacterEmote: // 81
                case ActionType.CharacterObservation: // 117
                case ActionType.FriendObservation: // 310
                    user.BattleSystem.ResetBattle();
                    await user.MagicData.AbortMagicAsync(true);
                    break;
            }

            switch (Action)
            {
                case ActionType.LoginSpawn: // 74
                    Identity = client.Character.Identity;

                    if (user.IsOfflineTraining)
                    {
                        client.Character.MapIdentity = 601;
                        client.Character.MapX = 61;
                        client.Character.MapY = 54;
                    }

                    GameMap targetMap = Kernel.MapManager.GetMap(client.Character.MapIdentity);
                    if (targetMap == null)
                    {
                        await user.SavePositionAsync(1002, 430, 378);
                        client.Disconnect();
                        return;
                    }

                    Command = targetMap.MapDoc;
                    X = client.Character.MapX;
                    Y = client.Character.MapY;

                    await client.Character.EnterMapAsync();
                    await client.SendAsync(this);

                    await GameAction.ExecuteActionAsync(1000000, user, null, null, "");

                    if (client.Character.VipLevel > 0)
                        await client.Character.SendAsync(
                            string.Format(Language.StrVipNotify, client.Character.VipLevel,
                                client.Character.VipExpiration.ToString("U")), MsgTalk.TalkChannel.Talk);

                    if (user.Life == 0)
                        await user.SetAttributesAsync(ClientUpdateType.Hitpoints, 10);

                    user.Connection = Character.ConnectionStage.Ready; // set user ready to be processed.
                    break;

                case ActionType.LoginInventory: // 75
                    await user.UserPackage.CreateAsync();
                    await user.UserPackage.SendAsync();
                    await user.SendDetainRewardAsync();
                    await user.SendDetainedEquipmentAsync();
                    await user.Statistic.InitializeAsync();
                    await user.TaskDetail.InitializeAsync();
                    await user.LoadTitlesAsync();
                    await client.SendAsync(this);
                    break;

                case ActionType.LoginRelationships: // 76
                    foreach (var dbFriend in await FriendRepository.GetAsync(user.Identity))
                    {
                        Friend friend = new Friend(user);
                        await friend.CreateAsync(dbFriend);
                        user.AddFriend(friend);
                    }
                    await user.SendAllFriendAsync();

                    foreach (var dbEnemy in await EnemyRepository.GetAsync(user.Identity))
                    {
                        if (dbEnemy.TargetIdentity == user.Identity)
                        {
                            await BaseRepository.DeleteAsync(dbEnemy);
                            continue;
                        }
                        Enemy enemy = new Enemy(user);
                        await enemy.CreateAsync(dbEnemy);
                        user.AddEnemy(enemy);
                    }
                    await user.SendAllEnemiesAsync();

                    if (user.MateIdentity != 0)
                    {
                        Character mate = Kernel.RoleManager.GetUser(user.MateIdentity);
                        if (mate != null)
                        {
                            await mate.SendAsync(user.Gender == 1
                                ? Language.StrMaleMateLogin
                                : Language.StrFemaleMateLogin);
                        }
                    }

                    await user.LoadGuideAsync();
                    await user.LoadTradePartnerAsync();
                    await user.LoadMonsterKillsAsync();
                    await client.SendAsync(this);
                    break;

                case ActionType.LoginProficiencies: // 77
                    await client.Character.WeaponSkill.InitializeAsync();
                    await client.Character.WeaponSkill.SendAsync();
                    await client.SendAsync(this);
                    break;

                case ActionType.LoginSpells: // 78
                    await client.Character.MagicData.InitializeAsync();
                    await client.Character.MagicData.SendAllAsync();
                    await client.SendAsync(this);
                    break;

                case ActionType.CharacterDirection: // 79
                    await client.Character.SetDirectionAsync((FacingDirection) (Direction % 8), false);
                    await client.Character.BroadcastRoomMsgAsync(this, true);
                    break;

                case ActionType.CharacterEmote: // 81
                    await client.Character.SetActionAsync((EntityAction) Command, false);

                    if (user.Action == EntityAction.Cool)
                    {
                        int effect = 0;
                        for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                            pos <= Item.ItemPosition.EquipmentEnd;
                            pos++)
                        {
                            Item item = user.UserPackage[pos];
                            if (item != null && 
                                (item.IsHelmet() || item.IsNeck() || item.IsRing() || item.IsWeapon() || item.IsArmor() || item.IsShield() || item.IsShoes()))
                            {
                                effect += item.GetQuality()%5;
                                if (item.IsBackswordType() || item.GetItemSort() == Item.ItemSort.ItemsortWeaponDoubleHand)
                                    effect += item.GetQuality() % 5;
                            }
                        }

                        if (effect == 7*4)
                        {
                            Command |= ((uint)user.Profession * 0x10000 + 0x01000000);
                        }
                        else if (effect >= 7*3)
                        {
                            Command |= ((uint)user.Profession * 0x10000);
                        }
                    }

                    await user.BroadcastRoomMsgAsync(this, true);
                    break;

                case ActionType.MapPortal: // 85
                    uint idMap = 0;
                    Point tgtPos = new Point();
                    Point sourcePos = new Point(client.Character.MapX, client.Character.MapY);
                    if (!client.Character.Map.GetPassageMap(ref idMap, ref tgtPos, ref sourcePos))
                    {
                        client.Character.Map.GetRebornMap(ref idMap, ref tgtPos);
                    }
                    await client.Character.FlyMapAsync(idMap, tgtPos.X, tgtPos.Y);
                    break;

                case ActionType.SpellAbortXp: // 93
                    if (client.Character.QueryStatus(StatusSet.START_XP) != null)
                        await client.Character.DetachStatusAsync(StatusSet.START_XP);
                    break;

                case ActionType.CharacterRevive: // 94
                    if (user.IsAlive || !user.CanRevive())
                        return;

                    await user.RebornAsync(Command == 0);
                    break;

                case ActionType.CharacterDelete:
                    if (user.SecondaryPassword != Command)
                        return;

                    if (await user.DeleteCharacterAsync())
                        await Kernel.RoleManager.KickOutAsync(user.Identity, "DELETED");
                    break;

                case ActionType.CharacterPkMode: // 96
                    if (!Enum.IsDefined(typeof(PkModeType), (int) Command))
                        Command = (uint) PkModeType.Capture;

                    client.Character.PkMode = (PkModeType) Command;
                    await client.SendAsync(this);
                    break;

                case ActionType.LoginGuild: // 97
                    client.Character.Syndicate = Kernel.SyndicateManager.FindByUser(client.Identity);
                    await client.Character.SendSyndicateAsync();
                    if (client.Character.Syndicate != null)
                        await client.Character.Syndicate.SendRelationAsync(client.Character);

                    await client.Character.LoadFamilyAsync();
                    await client.SendAsync(this);
                    break;

                case ActionType.MapMine: // 99
                    if (!user.IsAlive)
                    {
                        await user.SendAsync(Language.StrDead);
                        return;
                    }

                    if (!user.Map.IsMineField())
                    {
                        await user.SendAsync(Language.StrNoMine);
                        return;
                    }

                    user.StartMining();
                    break;

                case ActionType.MapTeamLeaderStar: // 101
                    if (user.Team == null || user.Team.Leader.MapIdentity != user.MapIdentity)
                        return;

                    targetUser = user.Team.Leader;
                    X = targetUser.MapX;
                    Y = targetUser.MapY;
                    await user.SendAsync(this);
                    break;

                case ActionType.MapQuery: // 102
                    if (targetUser != null)
                        await targetUser.SendSpawnToAsync(user);
                    break;

                case ActionType.MapTeamMemberStar: // 106
                    if (user.Team == null || targetUser == null || !user.Team.IsMember(targetUser.Identity) || targetUser.MapIdentity != user.MapIdentity)
                        return;

                    Command = targetUser.RecordMapIdentity;
                    X = targetUser.MapX;
                    Y = targetUser.MapY;
                    await user.SendAsync(this);
                    break;

                case ActionType.BoothSpawn:
                    if (await user.CreateBoothAsync())
                    {
                        Command = user.Booth.Identity;
                        X = user.Booth.MapX;
                        Y = user.Booth.MapY;
                        await user.SendAsync(this);
                    }
                    break;

                case ActionType.BoothLeave: // 114
                    await user.DestroyBoothAsync();
                    await user.Screen.SynchroScreenAsync();
                    break;

                case ActionType.CharacterObservation: // 117
                    targetUser = Kernel.RoleManager.GetUser(Command);
                    if (targetUser == null)
                        return;

                    for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; 
                        pos <= Item.ItemPosition.EquipmentEnd;
                        pos++)
                    {
                        if (targetUser.UserPackage[pos] != null)
                        {
                            await user.SendAsync(new MsgItemInfo(targetUser.UserPackage[pos], MsgItemInfo.ItemMode.View));
                        }
                    }

                    await targetUser.SendAsync(string.Format(Language.StrObservingEquipment, user.Name));
                    break;

                case ActionType.SpellAbortTransform: // 118
                    if (user.Transformation != null)
                        await user.ClearTransformationAsync();
                    break;

                case ActionType.SpellAbortFlight: // 120
                    if (user.QueryStatus(StatusSet.FLY) != null)
                        await user.DetachStatusAsync(StatusSet.FLY);
                    break;

                case ActionType.RelationshipsEnemy: // 123
                    Enemy fetchEnemy = user.GetEnemy(Command);
                    if (fetchEnemy == null)
                    {
                        await user.SendAsync(this);
                        return;
                    }

                    await fetchEnemy.SendInfoAsync();
                    break;

                case ActionType.LoginComplete: // 130
                    int bonusCount = await user.BonusCountAsync();
                    if (bonusCount > 0)
                        await user.SendAsync(string.Format(Language.StrBonus, bonusCount), MsgTalk.TalkChannel.Center, SColor.Red);

                    if (await user.CardsCountAsync() > 0)
                        await user.SendAsync(new MsgAction { Action = ActionType.ClientCommand, Command = 1197u, Identity = user.Identity });

                    if (user.Gender == 1 && 
                        (user.SendFlowerTime == null 
                         || int.Parse(DateTime.Now.ToString("yyyyMMdd")) > int.Parse(user.SendFlowerTime.Value.ToString("yyyyMMdd"))))
                    {
                        await user.SendAsync(new MsgFlower
                        {
                            Mode = MsgFlower.RequestMode.QueryIcon
                        });
                    }

                    await user.CheckPkStatusAsync();
                    await user.LoadStatusAsync();
                    await client.Character.SendNobilityInfoAsync();
                    await client.Character.SendMultipleExpAsync();
                    await client.Character.SendBlessAsync();
                    await client.Character.SendLuckAsync();
                    await user.Screen.SynchroScreenAsync();
                    await Kernel.PigeonManager.SendToUserAsync(user);
                    await user.SendMerchantAsync();

                    if (user.VipLevel > 0)
                    {
                        if (!user.HasTitle(Character.UserTitles.Vip))
                            await user.AddTitleAsync(Character.UserTitles.Vip, user.VipExpiration);
                    }

                    Kernel.EventThread.GetEvent<QuizShow>()?.Enter(user);

                    await client.SendAsync(this);
                    break;

                case ActionType.MapJump: // 137
                    if (user != null) // todo handle ai 
                    {
                        if (!user.IsAlive)
                        {
                            await user.SendAsync(Language.StrDead, MsgTalk.TalkChannel.System, SColor.Red);
                            return;
                        }

                        ushort newX = (ushort) Command;
                        ushort newY = (ushort) (Command >> 16);

                        if (user.GetDistance(newX, newY) >= 2 * Screen.VIEW_SIZE)
                        {
                            await user.SendAsync(Language.StrInvalidMsg, MsgTalk.TalkChannel.System, SColor.Red);
                            await Kernel.RoleManager.KickOutAsync(user.Identity, "big jump");
                            return;
                        }

                        await user.ProcessOnMoveAsync();
                        await user.JumpPosAsync(newX, newY);

                        X = user.MapX;
                        Y = user.MapY;

                        await user.SendAsync(this);
                        await user.Screen.UpdateAsync(this);
                    }

                    break;

                case ActionType.RelationshipsFriend: // 140
                    Friend fetchFriend = user.GetFriend(Command);
                    if (fetchFriend == null)
                    {
                        await user.SendAsync(this);
                        return;
                    }
                    await fetchFriend.SendInfoAsync();
                    break;

                case ActionType.CharacterDead: // 145
                    if (user.IsAlive)
                        return;

                    await user.SetGhostAsync();
                    break;

                case ActionType.CharacterAvatar:
                    if (user.Gender == 1 && Command >= 200 || user.Gender == 2 && Command < 200)
                        return;
                    
                    user.Avatar = (ushort) Command;
                    await user.BroadcastRoomMsgAsync(this, true);
                    await user.SaveAsync();
                    break;

                case ActionType.QueryTradeBuddy: // 143
                    TradePartner partner = user.GetTradePartner(Command);
                    if (partner == null)
                    {
                        await user.SendAsync(this);
                        return;
                    }

                    await partner.SendInfoAsync();
                    break;

                case ActionType.ItemDetained:
                    {
                        break;
                    }

                case ActionType.Away:
                {
                    user.IsAway = Data != 0;

                    if (user.IsAway && user.Action != EntityAction.Sit)
                        await user.SetActionAsync(EntityAction.Sit);
                    else if (!user.IsAway && user.Action == EntityAction.Sit)
                        await user.SetActionAsync(EntityAction.Stand);

                    await user.BroadcastRoomMsgAsync(this, true);
                    break;
                }

                case ActionType.FriendObservation: // 310
                    targetUser = Kernel.RoleManager.GetUser(Command);
                    if (targetUser == null)
                        return;

                    await targetUser.SendWindowToAsync(user);
                    break;

                default:
                    await client.SendAsync(this);
                    if (client.Character.IsPm())
                    {
                        await client.SendAsync(new MsgTalk(client.Identity, MsgTalk.TalkChannel.Service,
                            $"Missing packet {Type}, Action {Action}, Length {Length}"));
                    }

                    await Log.WriteLogAsync(LogLevel.Warning,
                        "Missing packet {0}, Action {1}, Length {2}\n{3}",
                        Type, Action, Length, PacketDump.Hex(Encode()));
                    break;
            }
        }

        /// <summary>
        ///     Defines actions that may be requested by the user, or given to by the server.
        ///     Allows for action handling as a packet subtype. Enums should be named by the
        ///     action they provide to a system in the context of the player actor.
        /// </summary>
        public enum ActionType
        {
            LoginSpawn = 74,
            LoginInventory,
            LoginRelationships,
            LoginProficiencies,
            LoginSpells,
            CharacterDirection,
            CharacterEmote = 81,
            MapPortal = 85,
            MapTeleport,
            CharacterLevelUp = 92,
            SpellAbortXp,
            CharacterRevive,
            CharacterDelete,
            CharacterPkMode,
            LoginGuild,
            MapMine = 99,
            MapTeamLeaderStar = 101,
            MapQuery,
            AbortMagic = 103,
            MapArgb = 104,
            MapTeamMemberStar = 106,
            Kickback = 108,
            SpellRemove,
            ProficiencyRemove,
            BoothSpawn,
            BoothSuspend,
            BoothResume,
            BoothLeave,
            ClientCommand = 116,
            CharacterObservation,
            SpellAbortTransform,
            SpellAbortFlight = 120,
            MapGold,
            RelationshipsEnemy = 123,
            ClientDialog = 126,
            LoginComplete = 132,
            MapEffect = 134,
            RemoveEntity = 135,
            MapJump = 137,
            CharacterDead = 145,
            RelationshipsFriend = 148,
            CharacterAvatar = 151,
            QueryTradeBuddy = 143,
            ItemDetained = 153,
            ItemDetainedEx = 155,
            NinjaStep = 156,
            Away = 161,
            //SetGhost = 145,
            FriendObservation = 310,
        }
    }
}