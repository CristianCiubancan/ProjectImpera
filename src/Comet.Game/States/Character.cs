namespace Comet.Game.States
{
    using System;
    using System.Threading.Tasks;
    using Comet.Game.Database.Models;
    using Comet.Game.Database.Repositories;
    using Comet.Game.Packets;
    using Comet.Network.Packets;

    /// <summary>
    /// Character class defines a database record for a player's character. This allows
    /// for easy saving of character information, as well as means for wrapping character
    /// data for spawn packet maintenance, interface update pushes, etc.
    /// </summary>
    public sealed class Character : Role
    {
        // Fields and properties
        private DateTime LastSaveTimestamp { get; set; }
        public DbCharacter DbCharacter { get; set; }
        public uint UID
        {
            get => DbCharacter.CharacterID;
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
        public override ushort X
        {
            get => CurrentX;
            set => CurrentX = value;
        }

        /// <summary>
        ///     Current Y position of the user in the map.
        /// </summary>
        public override ushort Y
        {
            get => CurrentY;
            set => CurrentY = value;
        }
        public uint Map
        {
            get => DbCharacter.MapID;
            set => DbCharacter.MapID = value;
        }
        // public uint DynamicID { get; }
        public bool Alive
        {
            get => DbCharacter.HealthPoints > 0;
        }

        public string Name
        {
            get => DbCharacter.Name;
        }

        public Client Client { get; init; }
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
            this.Map = character.MapID;

            Client = socket;
        }

        /// <summary>
        /// Saves the character to persistent storage.
        /// </summary>
        /// <param name="force">True if the change is important to save immediately.</param>
        public async Task SaveAsync(bool force = false)
        {
            DateTime now = DateTime.UtcNow;
            if (force || this.LastSaveTimestamp.AddMilliseconds(CharactersRepository.ThrottleMilliseconds) < now)
            {
                this.LastSaveTimestamp = now;
                await CharactersRepository.SaveAsync(this.DbCharacter);
            }
        }

        public override Task SendAsync(IPacket msg)
        {
            return SendAsync(msg.Encode());
        }

        public override Task SendAsync(byte[] msg)
        {
            try
            {
                if (Client != null)
                {
                    return Client.SendAsync(msg);
                }
                return Task.CompletedTask;
            }
            // catch (Exception ex)
            catch
            {
                return Task.CompletedTask;
            }
        }

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgPlayer(this));
        }

        public async Task<bool> JumpPosAsync(int x, int y, bool sync = false)
        {
            this.X = (ushort)x;
            this.Y = (ushort)y;
            return true;
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