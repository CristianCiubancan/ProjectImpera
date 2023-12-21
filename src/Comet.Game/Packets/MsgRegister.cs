// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgRegister.cs
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
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    #region References

    using static MsgTalk;

    #endregion

    /// <remarks>Packet Type 1001</remarks>
    /// <summary>
    ///     Message containing character creation details, such as the new character's name,
    ///     body size, and profession. The character name should be verified, and may be
    ///     rejected by the server if a character by that name already exists.
    /// </summary>
    public sealed class MsgRegister : MsgBase<Client>
    {
        // Registration constants
        private static readonly byte[] Hairstyles =
        {
            10, 11, 13, 14, 15, 24, 30, 35, 37, 38, 39, 40
        };

        private static readonly ushort[] m_startX = {430, 423, 439, 428, 452, 464, 439};
        private static readonly ushort[] m_startY = {378, 394, 384, 365, 365, 378, 396};

        // Packet Properties
        public string Username { get; set; }
        public string CharacterName { get; set; }
        public string MaskedPassword { get; set; }
        public ushort Mesh { get; set; }
        public ushort Class { get; set; }
        public uint Token { get; set; }

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
            Username = reader.ReadString(16);
            CharacterName = reader.ReadString(16);
            MaskedPassword = reader.ReadString(16);
            Mesh = reader.ReadUInt16();
            Class = reader.ReadUInt16();
            Token = reader.ReadUInt32();
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
            // Validate that the player has access to character creation
            if (client.Creation == null || Token != client.Creation.Token ||
                !Kernel.Registration.Contains(Token))
            {
                await client.SendAsync(RegisterInvalid);
                client.Disconnect();
                return;
            }

            // Check character name availability
            if (await CharactersRepository.ExistsAsync(CharacterName))
            {
                await client.SendAsync(RegisterNameTaken);
                return;
            }

            if (!Kernel.IsValidName(CharacterName))
            {
                await client.SendAsync(RegisterInvalid);
                return;
            }

            // Validate character creation input
            if (!Enum.IsDefined(typeof(BodyType), Mesh) ||
                !Enum.IsDefined(typeof(BaseClassType), Class))
            {
                await client.SendAsync(RegisterInvalid);
                return;
            }

            DbPointAllot allot = Kernel.RoleManager.GetPointAllot((ushort) (Class / 10), 1) ?? new DbPointAllot
            {
                Strength = 4,
                Agility = 6,
                Vitality = 12,
                Spirit = 0
            };

            // Create the character
            var character = new DbCharacter
            {
                AccountIdentity = client.Creation.AccountID,
                Name = CharacterName,
                Mate = 0,
                Profession = (byte) Class,
                Mesh = Mesh,
                Silver = 1000,
                Level = 1,
                //MapID = 1002,
                //X = m_startX[await Kernel.NextAsync(m_startX.Length) % m_startX.Length],
                //Y = m_startY[await Kernel.NextAsync(m_startY.Length) % m_startY.Length],
                MapID = 1010,
                X = 61,
                Y = 109,
                Strength = allot.Strength,
                Agility = allot.Agility,
                Vitality = allot.Vitality,
                Spirit = allot.Spirit,
                HealthPoints =
                    (ushort) (allot.Strength * 3
                              + allot.Agility * 3
                              + allot.Spirit * 3
                              + allot.Vitality * 24),
                ManaPoints = (ushort) (allot.Spirit * 5),
                Registered = DateTime.Now,
                ExperienceMultiplier = 5,
                ExperienceExpires = DateTime.Now.AddHours(1),
                HeavenBlessing = DateTime.Now.AddDays(30),
                AutoAllot = 1
            };

            // Generate a random look for the character
            BodyType body = (BodyType) Mesh;
            switch (body)
            {
                case BodyType.AgileFemale:
                case BodyType.MuscularFemale:
                    character.Mesh += 2010000;
                    break;
                default:
                    character.Mesh += 10000;
                    break;
            }

            character.Hairstyle = (ushort) (
                await Kernel.NextAsync(3, 9) * 100 + Hairstyles[
                    await Kernel.NextAsync(0, Hairstyles.Length)]);

            try
            {
                // Save the character and continue with login
                await CharactersRepository.CreateAsync(character);
                Kernel.Registration.Remove(client.Creation.Token);
            }
            catch
            {
                await client.SendAsync(RegisterTryAgain);
                return;
            }

            try
            {
                await GenerateInitialEquipmentAsync(character);                
            }
            catch (Exception e)
            {
                await Log.WriteLogAsync(LogLevel.Exception, $"Exception thrown when generating initial status for user. Msg: {e.Message}");
            }
            await client.SendAsync(RegisterOk);
        }

        private async Task GenerateInitialEquipmentAsync(DbCharacter user)
        {
            DbNewbieInfo info = await DbNewbieInfo.GetAsync(user.Profession);
            if (info == null)
                return;

            if (info.LeftHand != 0)
                await CreateItemAsync(info.LeftHand, user.Identity, Item.ItemPosition.LeftHand);
            if (info.RightHand != 0)
                await CreateItemAsync(info.RightHand, user.Identity, Item.ItemPosition.RightHand);
            if (info.Shoes != 0)
                await CreateItemAsync(info.Shoes, user.Identity, Item.ItemPosition.Boots);
            if (info.Headgear != 0)
                await CreateItemAsync(info.Headgear, user.Identity, Item.ItemPosition.Headwear);
            if (info.Necklace != 0)
                await CreateItemAsync(info.Necklace, user.Identity, Item.ItemPosition.Necklace);
            if (info.Armor != 0)
                await CreateItemAsync(info.Armor, user.Identity, Item.ItemPosition.Armor);
            if (info.Ring != 0)
                await CreateItemAsync(info.Ring, user.Identity, Item.ItemPosition.Ring);

            if (info.Item0 != 0)
                for (int i = 0; i < info.Number0; i++)
                    await CreateItemAsync(info.Item0, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item1 != 0)
                for (int i = 0; i < info.Number1; i++)
                    await CreateItemAsync(info.Item1, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item2 != 0)
                for (int i = 0; i < info.Number2; i++)
                    await CreateItemAsync(info.Item2, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item3 != 0)
                for (int i = 0; i < info.Number3; i++)
                    await CreateItemAsync(info.Item3, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item4 != 0)
                for (int i = 0; i < info.Number4; i++)
                    await CreateItemAsync(info.Item4, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item5 != 0)
                for (int i = 0; i < info.Number5; i++)
                    await CreateItemAsync(info.Item5, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item6 != 0)
                for (int i = 0; i < info.Number6; i++)
                    await CreateItemAsync(info.Item6, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item7 != 0)
                for (int i = 0; i < info.Number7; i++)
                    await CreateItemAsync(info.Item7, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item8 != 0)
                for (int i = 0; i < info.Number8; i++)
                    await CreateItemAsync(info.Item8, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item9 != 0)
                for (int i = 0; i < info.Number9; i++)
                    await CreateItemAsync(info.Item9, user.Identity, Item.ItemPosition.Inventory);

            if (info.Magic0 != 0)
                await CreateMagicAsync(user.Identity, (ushort)info.Magic0);
            if (info.Magic1 != 0)
                await CreateMagicAsync(user.Identity, (ushort)info.Magic1);
            if (info.Magic2 != 0)
                await CreateMagicAsync(user.Identity, (ushort)info.Magic2);
            if (info.Magic3 != 0)
                await CreateMagicAsync(user.Identity, (ushort)info.Magic3);
        }

        private async Task CreateItemAsync(uint type, uint idOwner, Item.ItemPosition position, byte add = 0,
            Item.SocketGem gem1 = Item.SocketGem.NoSocket, Item.SocketGem gem2 = Item.SocketGem.NoSocket,
            byte enchant = 0, byte reduceDmg = 0)
        {
            DbItem item = Item.CreateEntity(type);
            if (item == null)
                return;
            item.Position = (byte) position;
            item.PlayerId = idOwner;
            item.AddLife = enchant;
            item.ReduceDmg = reduceDmg;
            item.Magic3 = add;
            item.Gem1 = (byte) gem1;
            item.Gem2 = (byte) gem2;
            await BaseRepository.SaveAsync(item);
        }

        private Task CreateMagicAsync(uint idOwner, ushort type, byte level = 0)
        {
            return BaseRepository.SaveAsync(new DbMagic
            {
                Type = type,
                Level = level,
                OwnerId = idOwner
            });
        }
    }
}