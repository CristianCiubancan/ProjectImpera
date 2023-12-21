// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - DbWeaponSkill.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace Comet.Game.Database.Models
{
    [Table("cq_weapon_skill")]
    public class DbWeaponSkill
    {
        /// <summary>
        ///     The unique identification of the weapon skill.
        /// </summary>
        [Key]
        [Column("id")]
        public virtual uint Identity { get; set; }

        /// <summary>
        ///     The actual level of the weapon skill.
        /// </summary>
        [Column("level")]
        public virtual byte Level { get; set; }

        /// <summary>
        ///     The amount of experience of the actual level.
        /// </summary>
        [Column("exp")]
        public virtual uint Experience { get; set; }

        /// <summary>
        ///     The owner unique identity (character identity).
        /// </summary>
        [Column("owner_id")]
        public virtual uint OwnerIdentity { get; set; }

        /// <summary>
        ///     The old level of the weapon skill before reborn (if higher)
        /// </summary>
        [Column("old_level")]
        public virtual byte OldLevel { get; set; }

        /// <summary>
        ///     If the weapon skill is active. 1 Means that it is waiting the level hit the
        ///     old level to restore the old status.
        /// </summary>
        [Column("unlearn")]
        public virtual byte Unlearn { get; set; }

        /// <summary>
        ///     The 3 digit type of weapon. (410 - Blade)
        /// </summary>
        [Column("type")]
        public virtual uint Type { get; set; }
    }
}