namespace Comet.Game.States
{
    using System;
    using System.Threading.Tasks;
    using Comet.Game.Database;
    using Comet.Game.Database.Models;
    using Comet.Game.Database.Repositories;
    using Comet.Game.Packets;
    using Comet.Game.States.BaseEntities;
    using Comet.Game.World.Maps;
    using Comet.Network.Packets;

    /// <summary>
    /// Character class defines a database record for a player's character. This allows
    /// for easy saving of character information, as well as means for wrapping character
    /// data for spawn packet maintenance, interface update pushes, etc.
    /// </summary>
    public sealed class Character : Role
    {
        // Fields and properties
        public ConnectionStage Connection { get; set; } = ConnectionStage.Connected;
     
        private DateTime LastSaveTimestamp { get; set; }
        public DbCharacter DbCharacter { get; set; }
        public uint Identity
        {
            get => DbCharacter.Identity;
        }
        public ushort CurrentX
        {
            get => DbCharacter.X;
            set => DbCharacter.X = value;
        }

        public ushort CurrentY
        {
            get => DbCharacter.Y;
            set => DbCharacter.Y = value;
        }
        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public override ushort MapX
        {
            get => CurrentX;
            set => CurrentX = value;
        }

        /// <summary>
        ///     Current Y position of the user in the map.
        /// </summary>
        public override ushort MapY
        {
            get => CurrentY;
            set => CurrentY = value;
        }
        public uint MapIdentity
        {
            get => DbCharacter.MapID;
            set => DbCharacter.MapID = value;
        }
        public GameMap Map { get; set; }
        // public uint DynamicID { get; }
        public bool Alive
        {
            get => DbCharacter.HealthPoints > 0;
        }
        public ushort Life
        {
            get => DbCharacter.HealthPoints;
            set => DbCharacter.HealthPoints = value;
        }
        public string Name
        {
            get => DbCharacter.Name;
        }

        public Screen Screen { get; set; }

        public Client m_socket { get; init; }
        /// <summary>
        /// Instantiates a new instance of <see cref="Character"/> using a database fetched
        /// <see cref="DbCharacter"/>. Copies attributes over to the base class of this
        /// class, which will then be used to save the character from the game world. 
        /// </summary>
        /// <param name="character">Database character information</param>
        public Character(DbCharacter character, Client socket)
        {
            this.DbCharacter = character;
            this.LastSaveTimestamp = DateTime.UtcNow;
            this.CurrentX = character.X;
            this.CurrentY = character.Y;
            this.MapIdentity = character.MapID;
            Screen = new Screen(this);
            m_socket = socket;
        }

        // /// <summary>
        // /// Saves the character to persistent storage.
        // /// </summary>
        // /// <param name="force">True if the change is important to save immediately.</param>
        // public async Task SaveAsync(bool force = false)
        // {
        //     DateTime now = DateTime.UtcNow;
        //     if (force || this.LastSaveTimestamp.AddMilliseconds(CharactersRepository.ThrottleMilliseconds) < now)
        //     {
        //         this.LastSaveTimestamp = now;
        //         await CharactersRepository.SaveAsync(this.DbCharacter);
        //     }
        // }
        public async Task<bool> SaveAsync()
        {
            try
            {
                await using var db = new ServerDbContext();
                db.Update(DbCharacter);
                return await Task.FromResult(await db.SaveChangesAsync() != 0);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }
        public override Task SendAsync(IPacket msg)
        {
            try
            {
                if (Connection != ConnectionStage.Disconnected)
                    return m_socket.SendAsync(msg);
            }
            catch (Exception ex)
            {
                // return Log.WriteLogAsync(LogLevel.Error, ex.Message);
            }
            return Task.CompletedTask;
        }

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgPlayer(this));
        }

        public async Task<bool> JumpPosAsync(int x, int y, bool sync = false)
        {
            this.MapX = (ushort)x;
            this.MapY = (ushort)y;
            return true;
        }

        public async Task SavePositionAsync(uint idMap, ushort x, ushort y)
        {
            GameMap map = Kernel.MapManager.GetMap(idMap);
            // TODO: add check for type of map...
            MapX = x;
            MapY = y;
            MapIdentity = idMap;
            await SaveAsync();
        }

        public override async Task EnterMapAsync()
        {
            Map = Kernel.MapManager.GetMap(MapIdentity);
            if (Map != null)
            {
                await Map.AddAsync(this);
                await Map.SendMapInfoAsync(this);
                await Screen.SynchroScreenAsync();

                // m_respawn.Startup(10);

                // if (Map.IsTeamDisable() && Team != null)
                // {
                //     if (Team.Leader.Identity == Identity)
                //         await Team.DismissAsync(this);
                //     else await Team.DismissMemberAsync(this);
                // }

                // if (CurrentEvent == null)
                // {
                //     GameEvent @event = Kernel.EventThread.GetEvent(m_idMap);
                //     if (@event != null)
                //         await SignInEventAsync(@event);
                // }

                // if (Team != null)
                //     await Team.SyncFamilyBattlePowerAsync();
            }
            else
            {
                Console.WriteLine($"Map {MapIdentity} not found");
                m_socket?.Disconnect();
            }
        }

        public enum ConnectionStage
        {
            Connected,
            Ready,
            Disconnected
        }
    }
    /// <summary>Enumeration type for body types for player characters.</summary>
    public enum BodyType : ushort
    {
        AgileMale = 1003,
        MuscularMale = 1004,
        AgileFemale = 2001,
        MuscularFemale = 2002
    }

    /// <summary>Enumeration type for base classes for player characters.</summary>
    public enum BaseClassType : ushort
    {
        Trojan = 10,
        Warrior = 20,
        Archer = 40,
        Ninja = 50,
        Taoist = 100
    }
}