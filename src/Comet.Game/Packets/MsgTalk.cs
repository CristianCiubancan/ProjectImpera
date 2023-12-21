// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgTalk.cs
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
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.States;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Events;
using Comet.Game.States.Magics;
using Comet.Game.States.NPCs;
using Comet.Game.States.Syndicates;
using Comet.Game.World;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1004</remarks>
    /// <summary>
    ///     Message defining a chat message from one player to the other, or from the system
    ///     to a player. Used for all chat systems in the game, including messages outside of
    ///     the game world state, such as during character creation or to tell the client to
    ///     continue logging in after connect.
    /// </summary>
    public sealed class MsgTalk : MsgBase<Client>
    {
        // Static messages
        public const string SYSTEM = "SYSTEM";
        public const string ALLUSERS = "ALLUSERS";
        public static readonly MsgTalk LoginOk = new MsgTalk(0, TalkChannel.Login, "ANSWER_OK");
        public static readonly MsgTalk LoginInvalid = new MsgTalk(0, TalkChannel.Login, "Invalid login");
        public static readonly MsgTalk LoginNewRole = new MsgTalk(0, TalkChannel.Login, "NEW_ROLE");
        public static readonly MsgTalk RegisterOk = new MsgTalk(0, TalkChannel.Register, "ANSWER_OK");
        public static readonly MsgTalk RegisterInvalid = new MsgTalk(0, TalkChannel.Register, "Invalid character");
        public static readonly MsgTalk RegisterNameTaken = new MsgTalk(0, TalkChannel.Register, "Character name taken");
        public static readonly MsgTalk RegisterTryAgain = new MsgTalk(0, TalkChannel.Register, "Error, please try later");

        /// <summary>
        /// Instantiates a new instance of <see cref="MsgTalk"/> with an empty buffer.
        /// </summary>
        public MsgTalk()
        {
            Type = PacketType.MsgTalk;
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgTalk" /> using the recipient's
        ///     character ID, a destination channel, and text to display. By default, sends
        ///     from "SYSTEM" to "ALLUSERS".
        /// </summary>
        /// <param name="characterID">Character's identifier</param>
        /// <param name="channel">Destination channel to send the text on</param>
        /// <param name="text">Text to be displayed in the client</param>
        public MsgTalk(uint characterID, TalkChannel channel, string text)
        {
            Type = PacketType.MsgTalk;
            Color = Color.White;
            Channel = channel;
            Style = TalkStyle.Normal;
            CharacterID = characterID;
            SenderName = SYSTEM;
            RecipientName = ALLUSERS;
            Suffix = string.Empty;
            Message = text;
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgTalk" /> using the recipient's
        ///     character ID, a destination channel, a text color, and text to display. By
        ///     default, sends from "SYSTEM" to "ALLUSERS".
        /// </summary>
        /// <param name="characterID">Character's identifier</param>
        /// <param name="channel">Destination channel to send the text on</param>
        /// <param name="color">Color text is to be displayed in</param>
        /// <param name="text">Text to be displayed in the client</param>
        public MsgTalk(uint characterID, TalkChannel channel, Color color, string text)
        {
            Type = PacketType.MsgTalk;
            Color = color;
            Channel = channel;
            Style = TalkStyle.Normal;
            CharacterID = characterID;
            SenderName = SYSTEM;
            RecipientName = ALLUSERS;
            Suffix = string.Empty;
            Message = text;
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgTalk" /> using the recipient's
        ///     character ID, a destination channel, a text color, sender and recipient's name,
        ///     and text to display.
        /// </summary>
        /// <param name="characterID">Character's identifier</param>
        /// <param name="channel">Destination channel to send the text on</param>
        /// <param name="color">Color text is to be displayed in</param>
        /// <param name="recipient">Name the message displays it is to</param>
        /// <param name="sender">Name the message displays it is from</param>
        /// <param name="text">Text to be displayed in the client</param>
        public MsgTalk(uint characterID, TalkChannel channel, Color color,
            string recipient, string sender, string text)
        {
            Type = PacketType.MsgTalk;
            Color = color;
            Channel = channel;
            Style = TalkStyle.Normal;
            CharacterID = characterID;
            SenderName = sender;
            RecipientName = recipient;
            Suffix = string.Empty;
            Message = text;
        }

        // Packet Properties
        public Color Color { get; set; }
        public TalkChannel Channel { get; set; }
        public TalkStyle Style { get; set; }
        public uint CharacterID { get; set; }
        public uint RecipientMesh { get; set; }
        public string RecipientName { get; set; }
        public uint SenderMesh { get; set; }
        public string SenderName { get; set; }
        public string Suffix { get; set; }
        public string Message { get; set; }

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
            Color = Color.FromArgb(reader.ReadInt32());
            Channel = (TalkChannel)reader.ReadUInt16();
            Style = (TalkStyle)reader.ReadUInt16();
            CharacterID = reader.ReadUInt32();
            RecipientMesh = reader.ReadUInt32();
            SenderMesh = reader.ReadUInt32();

            var strings = reader.ReadStrings();
            if (strings.Count > 3)
            {
                SenderName = strings[0];
                RecipientName = strings[1];
                Suffix = strings[2];
                Message = strings[3];
            }
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
            writer.Write(Color.FromArgb(0, Color).ToArgb());
            writer.Write((ushort)Channel);
            writer.Write((ushort)Style);
            writer.Write(CharacterID);
            writer.Write(RecipientMesh);
            writer.Write(SenderMesh);
            writer.Write(new List<string>
            {
                SenderName,
                RecipientName,
                Suffix,
                Message
            });
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character sender = client.Character;
            Character target = Kernel.RoleManager.GetUser(RecipientName);

            if (sender.Name != SenderName)
            {
#if DEBUG
                if (sender.IsGm())
                    await sender.SendAsync("Invalid sender name????");
#endif
                return;
            }

            if (sender.IsGm() || target?.IsGm() == true)
            {
                await Log.GmLogAsync("gm_talk", $"{sender.Name} says to {RecipientName}: {Message}");
            }

            // if (Channel != TalkChannel.Whisper) // for privacy
            {
                await BaseRepository.SaveAsync(new DbMessageLog
                {
                    SenderIdentity = sender.Identity,
                    SenderName = sender.Name,
                    TargetIdentity = target?.Identity ?? 0,
                    TargetName = target?.Name ?? RecipientName,
                    Channel = (ushort)Channel,
                    Message = Message,
                    Time = DateTime.Now
                });
            }

            if (await ProcessCommandAsync(Message, sender))
            {
                await Log.GmLogAsync("gm_cmd", $"{sender.Name}: {Message}");
                return;
            }

            switch (Channel)
            {
                case TalkChannel.Talk:
                    {
                        if (!sender.IsAlive)
                            return;

                        await sender.BroadcastRoomMsgAsync(this, false);
                        break;
                    }

                case TalkChannel.Whisper:
                    {
                        if (target == null)
                        {
                            await sender.SendAsync(Language.StrTargetNotOnline, TalkChannel.Talk, Color.White);
                            return;
                        }

                        SenderMesh = sender.Mesh;
                        RecipientMesh = target.Mesh;

                        await target.SendAsync(this);
                        break;
                    }

                case TalkChannel.Team:
                    {
                        if (sender.Team != null)
                            await sender.Team.SendAsync(this, sender.Identity);
                        break;
                    }

                case TalkChannel.Friend:
                    {
                        await sender.SendToFriendsAsync(this);
                        break;
                    }

                case TalkChannel.Guild:
                    {
                        if (sender.SyndicateIdentity == 0)
                            return;

                        await sender.Syndicate.SendAsync(this, sender.Identity);
                        break;
                    }

                case TalkChannel.Family:
                    {
                        if (sender.FamilyIdentity == 0)
                            return;

                        await sender.Family.SendAsync(this, sender.Identity);
                        break;
                    }

                case TalkChannel.Ghost:
                    {
                        if (sender.IsAlive)
                            return;

                        await sender.BroadcastRoomMsgAsync(this, false);
                        break;
                    }

                case TalkChannel.Announce:
                    {
                        if (sender.SyndicateIdentity == 0 ||
                            sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                            return;

                        sender.Syndicate.Announce = Message.Substring(0, Math.Min(127, Message.Length));
                        sender.Syndicate.AnnounceDate = DateTime.Now;
                        await sender.Syndicate.SaveAsync();
                        break;
                    }

                case TalkChannel.Bbs:
                case TalkChannel.GuildBoard:
                case TalkChannel.FriendBoard:
                case TalkChannel.OthersBoard:
                case TalkChannel.TeamBoard:
                case TalkChannel.TradeBoard:
                    {
                        MessageBoard.AddMessage(sender, Message, Channel);
                        break;
                    }

                case TalkChannel.World:
                    {
                        if (sender.CanUseWorldChat())
                            return;

                        await Kernel.RoleManager.BroadcastMsgAsync(this);
                        break;
                    }
            }
        }

        private async Task<bool> ProcessCommandAsync(string fullCmd, Character user)
        {
            if (fullCmd[0] != '/')
                return false;

            string[] splitCmd = fullCmd.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string cmd = splitCmd[0];
            string param = "";
            if (splitCmd.Length > 1)
                param = splitCmd[1];

            if (user.IsPm())
            {
                switch (cmd.ToLower())
                {
                    case "/pro":
                        if (byte.TryParse(param, out byte proProf))
                            await user.SetAttributesAsync(ClientUpdateType.Class, proProf);

                        return true;

                    case "/life":
                        await user.SetAttributesAsync(ClientUpdateType.Hitpoints, user.MaxLife);
                        return true;

                    case "/mana":
                        await user.SetAttributesAsync(ClientUpdateType.Mana, user.MaxMana);
                        return true;

                    case "/superman":
                        await user.SetAttributesAsync(ClientUpdateType.Strength, 176);
                        await user.SetAttributesAsync(ClientUpdateType.Agility, 256);
                        await user.SetAttributesAsync(ClientUpdateType.Vitality, 110);
                        await user.SetAttributesAsync(ClientUpdateType.Spirit, 125);

                        return true;

                    case "/uplev":
                        if (byte.TryParse(param, out byte uplevValue))
                            await user.AwardLevelAsync(uplevValue);

                        return true;

                    case "/awarditem":
                        if (!uint.TryParse(param, out uint idAwardItem))
                            return true;

                        DbItemtype itemtype = Kernel.ItemManager.GetItemtype(idAwardItem);
                        if (itemtype == null)
                        {
                            await user.SendAsync($"[AwardItem] Itemtype {idAwardItem} not found");
                            return true;
                        }

                        await user.UserPackage.AwardItemAsync(idAwardItem);
                        return true;
                    case "/awardmoney":
                        if (int.TryParse(param, out int moneyAmount))
                            await user.AwardMoneyAsync(moneyAmount);
                        return true;
                    case "/awardemoney":
                        if (int.TryParse(param, out int emoneyAmount))
                            await user.AwardConquerPointsAsync(emoneyAmount);
                        return true;
                    case "/awardmagic":
                    case "/awardskill":
                        byte skillLevel = 0;
                        string[] awardSkill = param.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (!ushort.TryParse(awardSkill[0], out var skillType))
                            return true;
                        if (awardSkill.Length > 1 && !byte.TryParse(awardSkill[1], out skillLevel))
                            return true;

                        Magic magic;
                        if (user.MagicData.CheckType(skillType))
                        {
                            magic = user.MagicData[skillType];
                            magic.Level = Math.Min(magic.MaxLevel, Math.Max((byte)0, skillLevel));
                            await magic.SaveAsync();
                            await magic.SendAsync();
                        }
                        else
                        {
                            if (!await user.MagicData.CreateAsync(skillType, skillLevel))
                                await user.SendAsync("[Award Skill] Could not create skill!");
                        }

                        return true;
                    case "/awardwskill":
                        byte level = 1;

                        string[] awardwskill = param.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (!ushort.TryParse(awardwskill[0], out var type))
                            return true;
                        if (awardwskill.Length > 1 && !byte.TryParse(awardwskill[1], out level))
                            return true;

                        if (user.WeaponSkill[type] == null)
                            await user.WeaponSkill.CreateAsync(type, level);
                        else
                        {
                            user.WeaponSkill[type].Level = level;
                            await user.WeaponSkill.SaveAsync(user.WeaponSkill[type]);
                            await user.WeaponSkill.SendAsync(user.WeaponSkill[type]);
                        }
                        return true;

                    case "/status":
                        if (int.TryParse(param, out int flag))
                        {
                            await user.AttachStatusAsync(user, flag, 0, 10, 0, 0);
                        }
                        return true;

                    case "/creategen":
                        {
                            await user.SendAsync(
                                "Attention, use this command only on localhost tests or the generator thread may crash.");
                            // mobid mapid mapx mapy boundcx boundcy maxnpc rest maxpergen
                            string[] szComs = param.Split(' ');
                            if (szComs.Length < 9)
                            {
                                await user.SendAsync(
                                    "/creategen mobid mapid mapx mapy boundcx boundcy maxnpc rest maxpergen");
                                return true;
                            }

                            ushort idMob = ushort.Parse(szComs[0]);
                            uint idMap = uint.Parse(szComs[1]);
                            ushort mapX = ushort.Parse(szComs[2]);
                            ushort mapY = ushort.Parse(szComs[3]);
                            ushort boundcx = ushort.Parse(szComs[4]);
                            ushort boundcy = ushort.Parse(szComs[5]);
                            ushort maxNpc = ushort.Parse(szComs[6]);
                            ushort restSecs = ushort.Parse(szComs[7]);
                            ushort maxPerGen = ushort.Parse(szComs[8]);

                            if (idMap == 0)
                            {
                                idMap = user.MapIdentity;
                            }

                            if (mapX == 0 || mapY == 0)
                            {
                                mapX = user.MapX;
                                mapY = user.MapY;
                            }

                            DbGenerator newGen = new DbGenerator
                            {
                                Mapid = idMap,
                                Npctype = idMob,
                                BoundX = mapX,
                                BoundY = mapY,
                                BoundCx = boundcx,
                                BoundCy = boundcy,
                                MaxNpc = maxNpc,
                                RestSecs = restSecs,
                                MaxPerGen = maxPerGen,
                                BornX = 0,
                                BornY = 0,
                                TimerBegin = 0,
                                TimerEnd = 0
                            };

                            if (!await BaseRepository.SaveAsync(newGen))
                            {
                                await user.SendAsync("Could not save generator.");
                                return true;
                            }

                            Generator pGen = new Generator(newGen);
                            await pGen.GenerateAsync();
                            //await Kernel.WorldThread.AddGeneratorAsync(pGen);
                            await Kernel.GeneratorManager.AddGeneratorAsync(pGen);
                            return true;
                        }
                    case "/action":
                        if (uint.TryParse(param, out var idExecuteAction))
                            await GameAction.ExecuteActionAsync(idExecuteAction, user, null, null, string.Empty);
                        return true;

                    case "/reloadactionall":
                        await Kernel.EventManager.ReloadActionTaskAllAsync();
                        return true;

                    case "/xp":
                        await user.AddXpAsync(100);
                        await user.BurstXpAsync();
                        return true;

                    case "/sp":
                        await user.SetAttributesAsync(ClientUpdateType.Stamina, user.MaxEnergy);
                        return true;

                    case "/querynpcs":
                        {
                            foreach (var npc in user.Map.Query9BlocksByPos(user.MapX, user.MapY).Where(x => x is BaseNpc)
                                .Cast<BaseNpc>())
                            {
                                await user.SendAsync($"NPC[{npc.Identity}]:{npc.Name}({npc.MapX},{npc.MapY})", TalkChannel.Talk);
                            }
                            return true;
                        }

                    case "/movenpc":
                        {
                            string[] moveNpcParams = param.Trim().Split(new[] { " " }, 4, StringSplitOptions.RemoveEmptyEntries);

                            if (moveNpcParams.Length < 4)
                            {
                                await user.SendAsync("Move NPC cmd must have: npcid mapid targetx targety");
                                return true;
                            }

                            if (!uint.TryParse(moveNpcParams[0], out var idNpc)
                                || !uint.TryParse(moveNpcParams[1], out var idMap)
                                || !ushort.TryParse(moveNpcParams[2], out var mapX)
                                || !ushort.TryParse(moveNpcParams[3], out var mapY))
                                return true;

                            BaseNpc npc = Kernel.RoleManager.GetRole<BaseNpc>(idNpc);
                            if (npc == null)
                            {
                                await user.SendAsync($"Object {idNpc} is not of type npc");
                                return true;
                            }

                            GameMap map = Kernel.MapManager.GetMap(idMap);
                            if (map == null)
                                return true;

                            if (!map.IsValidPoint(mapX, mapY))
                                return true;

                            await npc.ChangePosAsync(idMap, mapX, mapY);
                            return true;
                        }

                    case "/msgbox":
                        {
                            await user.SendAsync(new MsgTaskDialog
                            {
                                InteractionType = MsgTaskDialog.TaskInteraction.MessageBox,
                                OptionIndex = 255,
                                Text = param,
                                Data = 60
                            });
                            return true;
                        }

                    case "/msgqualifierinteractive":
                        {
                            int opt = int.Parse(param);
                            MsgQualifyingInteractive msg = new MsgQualifyingInteractive
                            {
                                Interaction = MsgQualifyingInteractive.InteractionType.Dialog,
                                Option = opt
                            };
                            await user.SendAsync(msg);
                            return true;
                        }

                    case "/msgflower":
                        {
                            int mode = int.Parse(param.Split(' ')[0]);
                            int data = int.Parse(param.Split(' ')[1]);
                            await user.SendAsync(new MsgFlower
                            {
                                Mode = (MsgFlower.RequestMode)mode,
                                Identity = (uint)data
                            });
                            return true;
                        }

                    case "/msgfamilyoccupy":
                        {
                            MsgFamilyOccupy.FamilyPromptType fType = MsgFamilyOccupy.FamilyPromptType.RequestNpc;
                            uint subType = 0;
                            bool war = false,
                                a = false,
                                b = false,
                                c = false;

                            string[] splitParam = param.Split(' ');
                            if (splitParam.Length < 2)
                                return true;

                            fType = (MsgFamilyOccupy.FamilyPromptType)int.Parse(splitParam[0]);
                            subType = uint.Parse(splitParam[1]);
                            if (splitParam.Length >= 3)
                                war = int.Parse(splitParam[2]) != 0;
                            if (splitParam.Length >= 4)
                                a = int.Parse(splitParam[3]) != 0;
                            if (splitParam.Length >= 5)
                                b = int.Parse(splitParam[4]) != 0;
                            if (splitParam.Length >= 6)
                                c = int.Parse(splitParam[5]) != 0;

                            await user.SendAsync(new MsgFamilyOccupy
                            {
                                Action = fType,
                                CityName = user.Map.Name,
                                DailyPrize = 722455,
                                WeeklyPrize = 722454,
                                GoldFee = 1000000,
                                OccupyDays = 10,
                                Identity = 1000,
                                OccupyName = user.SyndicateName,
                                RequestNpc = 10026,
                                WarRunning = war,
                                CanRemoveChallenge = b,
                                CanApplyChallenge = a,
                                UnknownBool3 = c,
                                SubAction = subType
                            });
                            return true;
                        }

                    case "/msgfamily":
                        {
                            MsgFamily msg = new MsgFamily
                            {
                                Identity = user.FamilyIdentity,
                                Action = MsgFamily.FamilyAction.QueryOccupy
                            };
                            // uid#reward#nextreward#occupydays#name#currentmap#dominationmap#
                            msg.Strings.Add(param);
                            await user.SendAsync(msg);
                            return true;
                        }

                    case "/msguserattrib":
                        {
                            string[] splitParam = param.Split(' ');
                            if (splitParam.Length == 2)
                            {
                                int actType = int.Parse(splitParam[0]);
                                ulong value0 = ulong.Parse(splitParam[1]);
                                var msg = new MsgUserAttrib(user.Identity, (ClientUpdateType)actType, value0);
                                await user.SendAsync(msg);
                            }
                            else if (splitParam.Length == 3)
                            {
                                int actType = int.Parse(splitParam[0]);
                                uint value0 = uint.Parse(splitParam[1]);
                                uint value1 = uint.Parse(splitParam[2]);
                                var msg = new MsgUserAttrib(user.Identity, (ClientUpdateType)actType, value0);
                                msg.Append((ClientUpdateType)actType, value1);
                                await user.SendAsync(msg);
                            }

                            return true;
                        }

                    case "/msgguideinfo":
                        {

                            string[] splitParam = param.Split(' ');

                            bool isOnline = false;

                            MsgGuideInfo.RequestMode mode = MsgGuideInfo.RequestMode.Mentor;

                            if (splitParam.Length >= 1)
                                mode = (MsgGuideInfo.RequestMode)int.Parse(splitParam[0]);

                            if (splitParam.Length > 1)
                                isOnline = splitParam[1].Equals("1");

                            await user.SendAsync(new MsgGuideInfo
                            {
                                Identity = 1999999u,
                                SenderIdentity = user.Identity,
                                Blessing = 70,
                                Composition = 150,
                                Experience = 6540,
                                EnroleDate = 20210101,
                                IsOnline = isOnline,
                                Level = 133,
                                Mesh = 1002,
                                Mode = mode,
                                SharedBattlePower = 55,
                                Profession = 15,
                                PkPoints = 32000,
                                Syndicate = 0,
                                SyndicatePosition = SyndicateMember.SyndicateRank.None,
                                Names = new List<string>
                            {
                                "Mentor",
                                "Apprentice",
                                "AppSpouse"
                            },
                                Unknown24 = 999999
                            });
                            return true;
                        }
                }
            }

            if (user.IsGm())
            {
                switch (cmd.ToLower())
                {
                    case "/bring":
                        {
                            Character bringTarget;
                            if (uint.TryParse(param, out uint idFindTarget))
                            {
                                bringTarget = Kernel.RoleManager.GetUser(idFindTarget);
                            }
                            else
                            {
                                bringTarget = Kernel.RoleManager.GetUser(param);
                            }

                            if (bringTarget == null)
                            {
                                await user.SendAsync("Target not found");
                                return true;
                            }

                            await bringTarget.FlyMapAsync(user.MapIdentity, user.MapX, user.MapY);
                            return true;
                        }
                    case "/cmd":
                        {
                            string[] cmdParams = param.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            string subCmd = cmdParams[0];

                            if (cmd.Length > 1)
                            {
                                string subParam = cmdParams[1];

                                switch (subCmd.ToLower())
                                {
                                    case "broadcast":
                                        await Kernel.RoleManager.BroadcastMsgAsync(subParam, TalkChannel.Center,
                                            Color.White);
                                        break;

                                    case "gmmsg":
                                        await Kernel.RoleManager.BroadcastMsgAsync($"{user.Name} says: {subParam}",
                                            TalkChannel.Center, Color.White);
                                        break;

                                    case "player":
                                        if (subParam.Equals("all", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            await user.SendAsync(
                                                $"Players Online: {Kernel.RoleManager.OnlinePlayers}, Distinct: {Kernel.RoleManager.OnlineUniquePlayers} (max: {Kernel.RoleManager.MaxOnlinePlayers})",
                                                TalkChannel.TopLeft, Color.White);
                                        }
                                        else if (subParam.Equals("map", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            await user.SendAsync(
                                                $"Map Online Players: {user.Map.PlayerCount} ({user.Map.Name})",
                                                TalkChannel.TopLeft, Color.White);
                                        }

                                        break;
                                }

                                return true;
                            }

                            return true;
                        }
                    case "/chgmap":
                        string[] chgMapParams = param.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                        if (chgMapParams.Length < 3)
                            return true;

                        if (uint.TryParse(chgMapParams[0], out uint chgMapId)
                            && ushort.TryParse(chgMapParams[1], out ushort chgMapX)
                            && ushort.TryParse(chgMapParams[2], out ushort chgMapY))
                            await user.FlyMapAsync(chgMapId, chgMapX, chgMapY);

                        return true;

                    case "/openui":

                        if (uint.TryParse(param, out uint ui))
                            await user.SendAsync(new MsgAction
                            {
                                Action = MsgAction.ActionType.ClientCommand,
                                Identity = user.Identity,
                                Command = ui,
                                ArgumentX = user.MapX,
                                ArgumentY = user.MapY
                            });
                        return true;

                    case "/openwindow":
                        if (uint.TryParse(param, out uint window))
                            await user.SendAsync(new MsgAction
                            {
                                Action = MsgAction.ActionType.ClientDialog,
                                Identity = user.Identity,
                                Command = window,
                                ArgumentX = user.MapX,
                                ArgumentY = user.MapY
                            });
                        return true;

                    case "/kickout":
                        {
                            Character findTarget;
                            if (uint.TryParse(param, out uint idFindTarget))
                            {
                                findTarget = Kernel.RoleManager.GetUser(idFindTarget);
                            }
                            else
                            {
                                findTarget = Kernel.RoleManager.GetUser(param);
                            }

                            if (findTarget == null)
                            {
                                await user.SendAsync("Target not found");
                                return true;
                            }

                            try
                            {
                                findTarget.Client.Disconnect();
                            }
                            catch (Exception ex)
                            {
                                await Log.WriteLogAsync("kickout_ex", LogLevel.Exception, ex.ToString());
                                Kernel.RoleManager.ForceLogoutUser(findTarget.Identity);
                            }

                            return true;
                        }

                    case "/find":
                        {
                            Character findTarget;
                            if (uint.TryParse(param, out uint idFindTarget))
                            {
                                findTarget = Kernel.RoleManager.GetUser(idFindTarget);
                            }
                            else
                            {
                                findTarget = Kernel.RoleManager.GetUser(param);
                            }

                            if (findTarget == null)
                            {
                                await user.SendAsync("Target not found");
                                return true;
                            }

                            await user.FlyMapAsync(findTarget.MapIdentity, findTarget.MapX, findTarget.MapY);
                            return true;
                        }

                    case "/bot":
                        {
                            string[] myParams = param.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

                            if (myParams.Length < 2)
                            {
                                await user.SendAsync("/bot [target_name] [reason]", TalkChannel.Talk);
                                return true;
                            }

                            Character target = Kernel.RoleManager.GetUser(myParams[0]);
                            if (target != null)
                            {
                                await Log.GmLogAsync("botjail", $"{user.Identity} {user.Name} botjailed {target.Identity} {target.Name} by: {myParams[1]}");
                                await target.SendAsync(Language.StrBotjail);
                                await target.FlyMapAsync(6002, 28, 74);
                                await target.SaveAsync();
                            }
                            return true;
                        }

                    case "/macro":
                        {
                            string[] myParams = param.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

                            if (myParams.Length < 2)
                            {
                                await user.SendAsync("/macro [target_name] [reason]", TalkChannel.Talk);
                                return true;
                            }

                            Character target = Kernel.RoleManager.GetUser(myParams[0]);
                            if (target != null)
                            {
                                await Log.GmLogAsync("macrojail", $"{user.Identity} {user.Name} macrojailed {target.Identity} {target.Name} by: {myParams[1]}");
                                await target.SendAsync(Language.StrMacrojail);
                                await target.FlyMapAsync(6010, 28, 74);
                                await target.SaveAsync();
                            }
                            return true;
                        }

                    case "/cancelevent":
                        {
                            GameEvent.EventType type = (GameEvent.EventType)int.Parse(param);
                            Kernel.EventThread.RemoveEvent(type);
                            return true;
                        }

                    case "/invitetranstest":
                        {
                            string[] myParams = param.Split(new[] { ' ' }, 4);
                            await user.SendAsync(new MsgInviteTrans
                            {
                                Message = int.Parse(myParams[0]),
                                Mode = (MsgInviteTrans.Action) int.Parse(myParams[1]),
                                Priority = int.Parse(myParams[2]),
                                Seconds = int.Parse(myParams[3])
                            }); 
                            return true;
                        }
                }
            }

            switch (cmd.ToLower())
            {
                case "/dc":
                case "/discnonect":
                    user.Client.Disconnect();
                    return true;

                case "/battleattr":
                    Role target;
                    if (!string.IsNullOrEmpty(param) && uint.TryParse(param, out uint idBpTarget))
                    {
                        target = Kernel.RoleManager.GetRole(idBpTarget) ?? user;
                    }
                    else
                    {
                        target = user;
                    }

                    if (target.Identity == user.Identity)
                        await user.SendAsync($"Battle Attributes for yourself: {user.Name} [Potency: {user.BattlePower}]", TalkChannel.Talk, Color.White);
                    else
                        await user.SendAsync($"Battle Attributes for target: {target.Name} [Potency: {target.BattlePower}]", TalkChannel.Talk, Color.White);

                    await user.SendAsync($"Life: {target.Life}-{target.MaxLife}, Mana: {target.Mana}-{target.MaxMana}", TalkChannel.Talk, Color.White);
                    await user.SendAsync($"Attack: {target.MinAttack}-{target.MaxAttack}, Magic Attack: {target.MagicAttack}", TalkChannel.Talk, Color.White);
                    await user.SendAsync($"Defense: {target.Defense}, Defense2: {target.Defense2}, MagicDefense: {target.MagicDefense}, MagicDefenseBonus: {target.MagicDefenseBonus}%", TalkChannel.Talk, Color.White);
                    await user.SendAsync($"Accuracy: {target.Accuracy}, Dodge: {target.Dodge}, Attack Speed: {target.AttackSpeed}", TalkChannel.Talk, Color.White);
                    if (target is Character tgtUsr)
                        await user.SendAsync($"DG: {tgtUsr.DragonGemBonus}%, PG: {tgtUsr.PhoenixGemBonus}%, Blessing: {tgtUsr.Blessing}%, TG: {tgtUsr.TortoiseGemBonus}%", TalkChannel.Talk, Color.White);
                    return true;

                case "/onlinetime":
                    await user.SendAsync($"You have been online on the current session for {user?.SessionOnlineTime.TotalDays:0} days, {user?.SessionOnlineTime.Hours:00} hours, {user?.SessionOnlineTime.Minutes} minutes and {user?.SessionOnlineTime.Seconds} seconds.", TalkChannel.Talk);
                    await user.SendAsync($"You have been online in the game for {user?.OnlineTime.TotalDays:0} days, {user?.OnlineTime.Hours:00} hours, {user?.OnlineTime.Minutes} minutes and {user?.OnlineTime.Seconds} seconds", TalkChannel.Talk);
                    return true;

                case "/tp":
                    {
                        if (user.Map.IsTeleportDisable())
                            return true;

                        switch (param)
                        {
                            case "tc":
                                if (user.VipLevel >= 2 && user.IsVipTeleportEnable())
                                    await user.FlyMapAsync(1002, 430, 378);
                                break;
                            case "pc":
                                if (user.VipLevel >= 2 && user.IsVipTeleportEnable())
                                    await user.FlyMapAsync(1011, 188, 264);
                                break;
                            case "ac":
                                if (user.VipLevel >= 2 && user.IsVipTeleportEnable())
                                    await user.FlyMapAsync(1020, 565, 562);
                                break;
                            case "dc":
                                if (user.VipLevel >= 2 && user.IsVipTeleportEnable())
                                    await user.FlyMapAsync(1000, 500, 650);
                                break;
                            case "bi":
                                if (user.VipLevel >= 2 && user.IsVipTeleportEnable())
                                    await user.FlyMapAsync(1015, 717, 571);
                                break;
                        }
                        return true;
                    }

                case "/clearinventory":
                    {
                        await user.UserPackage.ClearInventoryAsync();
                        await user.SendAsync("Your inventory has been cleaned! The usage of this command is of your own responsability.");
                        return true;
                    }
            }

            return false;
        }

        /// <summary>
        ///     Enumeration for defining the channel text is printed to. Can also print to
        ///     separate states of the client such as character registration, and can be
        ///     used to change the state of the client or deny a login.
        /// </summary>
        public enum TalkChannel : ushort
        {
            Talk = 2000,
            Whisper,
            Action,
            Team,
            Guild,
            Family = 2006,
            System,
            Yell,
            Friend,
            Center = 2011,
            TopLeft,
            Ghost,
            Service,
            Tip,
            World = 2021,
            Qualifier = 2022,
            Register = 2100,
            Login,
            Shop,
            Vendor = 2104,
            Website,
            GuildWarRight1 = 2108,
            GuildWarRight2,
            Offline,
            Announce,
            MessageBox,
            TradeBoard = 2201,
            FriendBoard,
            TeamBoard,
            GuildBoard,
            OthersBoard,
            Bbs,
            Broadcast = 2500,
            Monster = 2600
        }

        /// <summary>
        ///     Enumeration type for controlling how text is stylized in the client's chat
        ///     area. By default, text appears and fades overtime. This can be overridden
        ///     with multiple styles, hard-coded into the client.
        /// </summary>
        [Flags]
        public enum TalkStyle : ushort
        {
            Normal = 0,
            Scroll = 1 << 0,
            Flash = 1 << 1,
            Blast = 1 << 2
        }
    }
}