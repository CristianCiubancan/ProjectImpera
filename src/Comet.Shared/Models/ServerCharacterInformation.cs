// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Shared - ServerCharacterInformation.cs
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

#endregion

namespace Comet.Shared.Models
{
    public class ServerCharacterInformation
    {
        public uint ServerIdentity { get; set; }
        public string ServerName { get; set; }
        public List<CharacterInformation> Characters { get; set; }

        public class CharacterInformation
        {
            public uint ServerIdentity { get; set; }
            public uint UserIdentity { get; set; }
            public uint AccountIdentity { get; set; }
            public string Name { get; set; }
            public uint MateId { get; set; }
            public byte Level { get; set; }
            public ulong Experience { get; set; }
            public byte Profession { get; set; }
            public byte OldProfession { get; set; }
            public byte NewProfession { get; set; }
            public byte Metempsychosis { get; set; }
            public ushort Strength { get; set; }
            public ushort Agility { get; set; }
            public ushort Vitality { get; set; }
            public ushort Spirit { get; set; }
            public ushort AdditionalPoints { get; set; }
            public uint SyndicateIdentity { get; set; }
            public ushort SyndicatePosition { get; set; }
            public ulong NobilityDonation { get; set; }
            public byte NobilityRank { get; set; }
            public uint SupermanCount { get; set; }
            public DateTime? DeletedAt { get; set; }
            public ulong Money { get; set; }
            public uint WarehouseMoney { get; set; }
            public uint ConquerPoints { get; set; }
            public uint FamilyIdentity { get; set; }
            public ushort FamilyRank { get; set; }
        }
    }
}