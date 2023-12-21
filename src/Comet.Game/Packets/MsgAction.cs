namespace Comet.Game.Packets
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Comet.Game.States;
    using Comet.Game.World.Maps;
    using Comet.Network.Packets;

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
        /// Decodes a byte packet into the packet structure defined by this message class. 
        /// Should be invoked to structure data from the client for processing. Decoding
        /// follows TQ Digital's byte ordering rules for an all-binary protocol.
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
        /// Encodes the packet structure defined by this message class into a byte packet
        /// that can be sent to the client. Invoked automatically by the client's send 
        /// method. Encodes using byte ordering rules interoperable with the game client.
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
        /// Process can be invoked by a packet after decode has been called to structure
        /// packet fields and properties. For the server implementations, this is called
        /// in the packet handler after the message has been dequeued from the server's
        /// <see cref="PacketProcessor"/>.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            switch (this.Action)
            {
                case ActionType.LoginSpawn: // 74
                    Identity = client.Character.Identity;

                    // if (user.IsOfflineTraining)
                    // {
                    //     client.Character.MapIdentity = 601;
                    //     client.Character.MapX = 61;
                    //     client.Character.MapY = 54;
                    // }

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

                    // await GameAction.ExecuteActionAsync(1000000, user, null, null, "");

                    // if (client.Character.VipLevel > 0)
                    //     await client.Character.SendAsync(
                    //         string.Format(Language.StrVipNotify, client.Character.VipLevel,
                    //             client.Character.VipExpiration.ToString("U")), MsgTalk.TalkChannel.Talk);

                    if (user.Life == 0)
                        // await user.SetAttributesAsync(ClientUpdateType.Hitpoints, 10);

                    user.Connection = Character.ConnectionStage.Ready; // set user ready to be processed.
                    break;

                case ActionType.LoginComplete:
                    await client.SendAsync(this);
                    break;

                case ActionType.MapJump:
                    ushort newX = (ushort)Command;
                    ushort newY = (ushort)(Command >> 16);

                    await client.Character.JumpPosAsync(newX, newY);
                    // GameMap userMap = Kernel.MapManager.GetMap(client.Character.DbCharacter.MapID);

                    // if (userMap == null)
                    // {
                    //     await client.Character.SavePositionAsync(1002, 430, 378);
                    //     client.Disconnect();
                    // }

                    // IEnumerable<Character> characters = userMap.GetUsersInMap(client.Character.DbCharacter.MapID);
                    // foreach (Character character in characters)
                    // {
                    //     if (character.DbCharacter.CharacterID != client.Character.DbCharacter.CharacterID)
                    //     {
                    //         await character.SendSpawnToAsync(client.Character);
                    //     }
                    // }
                    X = client.Character.MapX;
                    Y = client.Character.MapY;

                    await client.SendAsync(this);
                    break;

                default:
                    await client.SendAsync(this);
                    await client.SendAsync(new MsgTalk(client.ID, MsgTalk.TalkChannel.Service,
                        String.Format("Missing packet {0}, Action {1}, Length {2}",
                        this.Type, this.Action, this.Length)));
                    Console.WriteLine(
                        "Missing packet {0}, Action {1}, Length {2}\n{3}",
                        this.Type, this.Action, this.Length, PacketDump.Hex(this.Encode()));
                    break;
            }
        }

        /// <summary>
        /// Defines actions that may be requested by the user, or given to by the server.
        /// Allows for action handling as a packet subtype. Enums should be named by the 
        /// action they provide to a system in the context of the player actor.
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