// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Magic Data.cs
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;

namespace Comet.Game.States.Magics
{
    public partial class MagicData
    {
        public enum MagicSort
        {
            Attack = 1,
            Recruit = 2, // support auto active.
            Cross = 3,
            Fan = 4, // support auto active(random).
            Bomb = 5,
            Attachstatus = 6,
            Detachstatus = 7,
            Square = 8,
            Jumpattack = 9, // move, a-lock
            Randomtrans = 10, // move, a-lock
            Dispatchxp = 11,
            Collide = 12, // move, a-lock & b-synchro
            Serialcut = 13, // auto active only.
            Line = 14, // support auto active(random).
            Atkrange = 15, // auto active only, forever active.
            Atkstatus = 16, // support auto active, random active.
            Callteammember = 17,
            Recordtransspell = 18,
            Transform = 19,
            Addmana = 20, // support self target only.
            Laytrap = 21,
            Dance = 22, // ÌøÎè(only use for client)
            Callpet = 23, // ÕÙ»½ÊÞ
            Vampire = 24, // ÎüÑª£¬power is percent award. use for call pet
            Instead = 25, // ÌæÉí. use for call pet
            Declife = 26, // ¿ÛÑª(µ±Ç°ÑªµÄ±ÈÀý)
            Groundsting = 27, // µØ´Ì,
            Vortex = 28,
            Activateswitch = 29,
            Spook = 30,
            Warcry = 31,
            Riding = 32,
            AttachstatusArea = 34,
            Remotebomb = 35, // fuck tq i dont know what name to use _|_
            Knockback = 38,
            Dashwhirl = 40,
            Perseverance = 41,
            Selfdetach = 46,
            Detachbadstatus = 47,
            CloseLine = 48,
            Compassion = 50,
            Teamflag = 51,
            Increaseblock = 52,
            Oblivion = 53,
            Stunbomb = 54,
            Tripleattack = 55,
            Dashdeadmark = 61,
            Mountwhirl = 64,
            Targetdrag = 65,
            Closescatter = 67,
            Assassinvortex = 68,
            Blisteringwave = 69
        }

        public const int PURE_TROJAN_ID = 10315;
        public const int PURE_WARRIOR_ID = 10311;
        public const int PURE_ARCHER_ID = 10313;
        public const int PURE_NINJA_ID = 6003;
        public const int PURE_MONK_ID = 10405;
        public const int PURE_PIRATE_ID = 11040;
        public const int PURE_WATER_ID = 30000;
        public const int PURE_FIRE_ID = 10310;

        public const int TWOFOLDBLADES_ID = 6000;

        public const int MAGICDAMAGE_ALT = 26;
        public const int AUTOLEVELUP_EXP = -1;
        public const int DISABLELEVELUP_EXP = 0;
        public const int AUTOMAGICLEVEL_PER_USERLEVEL = 10;
        public const int USERLEVELS_PER_MAGICLEVEL = 10;

        public const int KILLBONUS_PERCENT = 5;
        public const int HAVETUTORBONUS_PERCENT = 10;
        public const int WITHTUTORBONUS_PERCENT = 20;

        public const int MAGIC_DELAY = 1000; // DELAY
        public const int MAGIC_DECDELAY_PER_LEVEL = 100; 
        public const int RANDOMTRANS_TRY_TIMES = 10;
        public const int DISPATCHXP_NUMBER = 20;
        public const int COLLIDE_POWER_PERCENT = 80;
        public const int COLLIDE_SHIELD_DURABILITY = 3;
        public const int LINE_WEAPON_DURABILITY = 2;
        public const int MAX_SERIALCUTSIZE = 10;
        public const int AWARDEXP_BY_TIMES = 1;
        public const int AUTO_MAGIC_DELAY_PERCENT = 150;
        public const int BOW_SUBTYPE = 500;
        public const ushort POISON_MAGIC_TYPE = 10010;
        public const int DEFAULT_MAGIC_FAN = 120;
        public const int STUDENTBONUS_PERCENT = 5;

        public const int MAGIC_KO_LIFE_PERCENT = 15;
        public const int MAGIC_ESCAPE_LIFE_PERCENT = 15;

        private Role m_pOwner;

        public ConcurrentDictionary<uint, Magic> Magics = new ConcurrentDictionary<uint, Magic>();

        public MagicData(Role owner)
        {
            m_pOwner = owner;
        }

        public async Task<bool> InitializeAsync()
        {
            if (m_pOwner.IsPlayer())
            {
                Character user = (Character) m_pOwner;

                foreach (var dbMagic in await MagicRepository.GetAsync(m_pOwner.Identity))
                {
                    Magic magic = new Magic(m_pOwner);
                    if (!await magic.CreateAsync(dbMagic))
                        continue;

                    if (magic.Type == 1100 && user != null && user.ProfessionSort != 13)
                    {
                        await BaseRepository.DeleteAsync(dbMagic);
                        continue;
                    }
                    if (magic.Type == 1025 && user != null && user.ProfessionSort != 2 && user.ProfessionSort != 13)
                    {
                        await BaseRepository.DeleteAsync(dbMagic);
                        continue;
                    }
                    if (magic.Type == 1050 && user != null && user.ProfessionSort != 14 && user.ProfessionSort != 13)
                    {
                        await BaseRepository.DeleteAsync(dbMagic);
                        continue;
                    }

                    Magics.TryAdd(magic.Type, magic);
                }
            }

            return true;
        }

        #region Magic Checking

        public bool CheckType(ushort type)
        {
            return Magics.ContainsKey(type) && !Magics[type].Unlearn;
        }

        public async Task<bool> CreateAsync(ushort type, byte level)
        {
            if (this[type] != null)
            {
                Magic old = this[type];
                old.Unlearn = false;

                if (m_pOwner is Character)
                {
                    await old.SaveAsync();
                    await old.SendAsync();
                }
                return true;
            }

            Magic pMagic = new Magic(m_pOwner);
            if (await pMagic.CreateAsync(type, level))
            {
                return Magics.TryAdd(type, pMagic);
            }
            return false;
        }

        public async Task<bool> UpLevelByTaskAsync(ushort type)
        {
            Magic pMagic;
            if (!Magics.TryGetValue(type, out pMagic))
                return false;

            byte nNewLevel = (byte)(pMagic.Level + 1);
            if (!FindMagicType(type, nNewLevel))
                return false;

            pMagic.Experience = 0;
            pMagic.Level = nNewLevel;
            await pMagic.SendAsync();
            await pMagic.SaveAsync();
            return true;
        }

        public bool FindMagicType(ushort type, byte pLevel)
        {
            return Kernel.MagicManager.GetMagictype(type, pLevel) != null;
        }

        public bool CheckLevel(ushort type, ushort level)
        {
            return Magics.Values.FirstOrDefault(x => x.Type == type && x.Level == level) != null;
        }

        public bool IsWeaponMagic(ushort type)
        {
            return type >= 10000 && type < 10256;
        }

        private BattleSystem.MagicType HitByMagic(Magic magic)
        {
            // 0 none, 1 normal, 2 xp
            if (magic == null) return 0;

            if (magic.WeaponHit == 0)
            {
                return magic.UseXp == BattleSystem.MagicType.XpSkill ? BattleSystem.MagicType.XpSkill : BattleSystem.MagicType.Normal;
            }

            if (m_pOwner is Character pRole)
            {
                if (pRole.UserPackage[Item.ItemPosition.RightHand] != null && magic.WeaponHit == 2 &&
                    pRole.UserPackage[Item.ItemPosition.RightHand].Itemtype.MagicAtk > 0)
                {
                    return magic.UseXp == BattleSystem.MagicType.XpSkill ? BattleSystem.MagicType.XpSkill : BattleSystem.MagicType.Normal;
                }
            }

            return BattleSystem.MagicType.None;
        }

        private uint GetDieMode()
        {
            return (uint)(HitByMagic(QueryMagic) > 0 ? 3 : m_pOwner.IsBowman ? 5 : 1);
        }

        public bool HitByWeapon()
        {
            Magic magic = QueryMagic;
            if (magic == null)
                return true;

            if (magic.WeaponHit == 1)
                return true;

            Item pItem;
            if (m_pOwner is Character character
                && (pItem = character.UserPackage[Item.ItemPosition.RightHand]) != null
                && pItem.Itemtype.MagicAtk <= 0)
                return true;

            return false;
        }

        public async Task<bool> UnlearnMagicAsync(ushort type, bool drop)
        {
            Magic magic = this[type];
            if (magic == null)
                return false;

            if (drop)
            {
                await magic.DeleteAsync();
            }
            else
            {
                magic.OldLevel = (byte) magic.Level;
                magic.Level = 0;
                magic.Experience = 0;
                magic.Unlearn = true;
                await magic.SaveAsync();
            }

            await m_pOwner.SendAsync(new MsgAction
            {
                Identity = m_pOwner.Identity,
                Command = type,
                Action = MsgAction.ActionType.SpellRemove
            });

            return Magics.TryRemove(type, out _);
        }

        public async Task<bool> ResetMagicAsync(ushort type)
        {
            Magic magic = this[type];
            if (magic == null)
                return false;

            magic.OldLevel = (byte)magic.Level;
            magic.Level = 0;
            magic.Experience = 0;
            await magic.SaveAsync();
            await magic.SendAsync();
            return true;
        }

        #endregion

        #region Crime

        public async Task<bool> CheckCrimeAsync(Role pRole, Magic magic)
        {
            if (pRole == null || magic == null) return false;

            if (magic.Crime <= 0)
                return false;

            return await m_pOwner.CheckCrimeAsync(pRole);
        }

        public async Task<bool> CheckCrimeAsync(Dictionary<uint, Role> pRoleSet, Magic magic)
        {
            if (pRoleSet == null || magic == null) return false;

            if (magic.Crime <= 0)
                return false;

            foreach (var pRole in pRoleSet.Values)
                if (m_pOwner.Identity != pRole.Identity && await m_pOwner.CheckCrimeAsync(pRole))
                    return true;
            return false;
        }

        #endregion

        #region Socket

        public async Task SendAllAsync()
        {
            foreach (var magic in Magics.Values.Where(x => !x.Unlearn))
            {
                await magic.SendAsync();
            }
        }

        #endregion

        public Magic QueryMagic => Magics.TryGetValue(m_typeMagic, out var magic) ? magic : null;

        public Magic this[ushort nType] => Magics.TryGetValue(nType, out var ret) ? ret : null;
    }
}