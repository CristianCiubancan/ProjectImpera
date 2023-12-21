// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - FamilyMember.cs
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
using Comet.Shared;

#endregion

namespace Comet.Game.States.Families
{
    public sealed class FamilyMember
    {
        private DbFamilyAttr m_attr;

        private FamilyMember()
        {

        }

        #region Static Creation

        public static async Task<FamilyMember> CreateAsync(Character player, Family family, Family.FamilyRank rank = Family.FamilyRank.Member, uint proffer = 0)
        {
            if (player == null || family == null || rank == Family.FamilyRank.None)
                return null;

            DbFamilyAttr attr = new DbFamilyAttr
            {
                FamilyIdentity = family.Identity,
                UserIdentity = player.Identity,
                Proffer = proffer,
                AutoExercise = 0,
                ExpDate = 0,
                JoinDate = DateTime.Now,
                Rank = (byte) rank
            };
            if (!await BaseRepository.SaveAsync(attr))
                return null;

            FamilyMember member = new FamilyMember
            {
                m_attr = attr,
                Name = player.Name,
                MateIdentity = player.MateIdentity,
                Level = player.Level,
                LookFace = player.Mesh,
                Profession = player.Profession,

                FamilyIdentity = family.Identity,
                FamilyName = family.Name
            };
            if (!await member.SaveAsync())
                return null;

            await Log.GmLogAsync("family", $"[{player.Identity}],[{player.Name}],[{family.Identity}],[{family.Name}],[Join]");
            return member;
        }

        public static async Task<FamilyMember> CreateAsync(DbFamilyAttr player, Family family)
        {
            DbCharacter dbUser = await CharactersRepository.FindByIdentityAsync(player.UserIdentity);
            if (dbUser == null)
                return null;

            FamilyMember member = new FamilyMember
            {
                m_attr = player, 
                Name = dbUser.Name, 
                MateIdentity = dbUser.Mate, 
                Level = dbUser.Level,
                LookFace = dbUser.Mesh,
                Profession = dbUser.Profession,

                FamilyIdentity = family.Identity,
                FamilyName = family.Name
            };

            return member;
        }

        #endregion

        #region Properties

        public uint Identity => m_attr.UserIdentity;
        public string Name { get; private set; }
        public byte Level { get; private set; }
        public uint MateIdentity { get; private set; }
        public uint LookFace { get; private set; }
        public ushort Profession { get; private set; }

        public Family.FamilyRank Rank
        {
            get => (Family.FamilyRank) m_attr.Rank;
            set => m_attr.Rank = (byte) value;
        }

        public DateTime JoinDate => m_attr.JoinDate;

        public uint Proffer
        {
            get => m_attr.Proffer;
            set => m_attr.Proffer = value;
        }

        public Character User => Kernel.RoleManager.GetUser(Identity);

        public bool IsOnline => User != null;

        #endregion

        #region Family Properties

        public uint FamilyIdentity { get; private set; }
        public string FamilyName { get; private set; }

        #endregion

        #region Database

        public Task<bool> SaveAsync()
        {
            return BaseRepository.SaveAsync(m_attr);
        }


        public Task<bool> DeleteAsync()
        {
            return BaseRepository.DeleteAsync(m_attr);
        }

        #endregion
    }
}