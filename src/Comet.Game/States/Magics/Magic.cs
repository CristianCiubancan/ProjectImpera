// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Magic.cs
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

using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Shared;

namespace Comet.Game.States.Magics
{
    public sealed class Magic
    {
        private Role m_pOwner;

        private DbMagic m_dbMagic;
        private DbMagictype m_dbMagictype;

        private TimeOutMS m_tDelay = new TimeOutMS();

        private byte m_pMaxLevel;

        public Magic(Role owner)
        {
            m_pOwner = owner;
        }

        public async Task<bool> CreateAsync(uint idMgc)
        {
            return await CreateAsync(idMgc, 0);
        }

        public async Task<bool> CreateAsync(uint idMgc, ushort level)
        {
            m_dbMagictype = Kernel.MagicManager.GetMagictype(idMgc, level);
            if (m_dbMagictype == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Skill not existent for creation (type:{idMgc}, level:{0}, player: {m_pOwner.Identity})");
                return false;
            }
            
            if (m_pOwner.MagicData.CheckType((ushort)idMgc))
                return false;
            
            m_dbMagic = new DbMagic
            {
                OwnerId = m_pOwner.Identity,
                Type = (ushort)idMgc,
                Level = level
            };

            m_pMaxLevel = Kernel.MagicManager.GetMaxLevel(idMgc);

            if (m_pOwner is Character)
            {
                await SaveAsync();
                await SendAsync();
            }
            return true;
        }

        public async Task<bool> CreateAsync(DbMagic pMgc)
        {
            m_dbMagictype = Kernel.MagicManager.GetMagictype(pMgc.Type, pMgc.Level);
            if (m_dbMagictype == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Skill not existent (type:{pMgc.Type}, level:{pMgc.Level}, player: {m_pOwner.Identity})");
                return false;
            }

            m_dbMagic = pMgc;

            m_pMaxLevel = Kernel.MagicManager.GetMaxLevel(pMgc.Type);
            return true;
        }

        #region Attributes

        public uint Identity => m_dbMagic.Id;

        public string Name => m_dbMagictype.Name;

        public ushort Type => m_dbMagic.Type;

        public ushort Level
        {
            get => m_dbMagic.Level;
            set
            {
                m_dbMagic.Level = Math.Min(m_pMaxLevel, value);
                m_dbMagictype = Kernel.MagicManager.GetMagictype(Type, Level);
            }
        }

        public uint Experience
        {
            get => m_dbMagic.Experience;
            set => m_dbMagic.Experience = value;
        }

        public byte OldLevel
        {
            get => (byte)m_dbMagic.OldLevel;
            set => m_dbMagic.OldLevel = value;
        }

        public bool Unlearn
        {
            get => m_dbMagic.Unlearn != 0;
            set => m_dbMagic.Unlearn = (byte) (value ? 1 : 0);
        }

        public ushort MaxLevel => m_pMaxLevel;
        public MagicData.MagicSort Sort => (MagicData.MagicSort) m_dbMagictype.Sort;
        public uint AutoActive => m_dbMagictype.AutoActive;
        public byte Crime => m_dbMagictype.Crime;
        public byte Ground => m_dbMagictype.Ground;
        public byte Multi => m_dbMagictype.Multi;
        public byte Target => (byte)m_dbMagictype.Target;
        public uint UseMana => m_dbMagictype.UseMp;
        public int Power => m_dbMagictype.Power;
        public uint IntoneSpeed => m_dbMagictype.IntoneSpeed;
        public uint Percent => m_dbMagictype.Percent;
        public uint StepSeconds => m_dbMagictype.StepSecs;
        public uint Range => m_dbMagictype.Range;
        public uint Distance => m_dbMagictype.Distance;
        public long Status => m_dbMagictype.Status;
        public uint NeedProf => m_dbMagictype.NeedProf;
        public int NeedExp => m_dbMagictype.NeedExp;
        public uint NeedLevel => m_dbMagictype.NeedLevel;
        public BattleSystem.MagicType UseXp => (BattleSystem.MagicType) m_dbMagictype.UseXp;
        public uint WeaponSubtype => m_dbMagictype.WeaponSubtype;
        public uint ActiveTimes => m_dbMagictype.ActiveTimes;
        public uint FloorAttr => m_dbMagictype.FloorAttr;
        public byte AutoLearn => m_dbMagictype.AutoLearn;
        public byte DropWeapon => m_dbMagictype.DropWeapon;
        public uint UseStamina => m_dbMagictype.UseEp;
        public byte WeaponHit => m_dbMagictype.WeaponHit;
        public uint UseItem => m_dbMagictype.UseItem;
        public uint NextMagic => m_dbMagictype.NextMagic;
        public int DelayMs => (int)(m_dbMagictype.DelayMs > 0 ? m_dbMagictype.DelayMs : m_dbMagictype.Timeout);
        public uint UseItemNum => m_dbMagictype.UseItemNum;
        public byte ElementType => m_dbMagictype.ElementType;
        public uint ElementPower => m_dbMagictype.ElementPower;
        public uint CpsCost => (uint)(m_dbMagictype.ReqUplevTime / 22.22d);

        #endregion

        #region Checks

        public void SetDelay()
        {
            m_tDelay.Startup(DelayMs);
        }

        public bool Use()
        {
            if (!IsReady())
                return false;

            m_tDelay.Startup(DelayMs);
            return true;
        }

        public bool IsReady()
        {
            if (!m_tDelay.IsActive())
            {
                m_tDelay.Startup(DelayMs);
                return true;
            }
            return m_tDelay.IsTimeOut(DelayMs);
        }

        #endregion

        #region Socket

        public async Task SendAsync()
        {
            if (m_pOwner is Character user)
                await user.SendAsync(new MsgMagicInfo
                {
                    Experience = Experience,
                    Level = Level,
                    Magictype = Type
                });
        }

        public async Task FlushAsync()
        {
            if (m_pOwner is Character user)
                await user.SendAsync(new MsgFlushExp
                {
                    Action = MsgFlushExp.FlushMode.Magic,
                    Identity = Type,
                    Experience = Experience
                });
        }

        #endregion

        #region Database

        public async Task<bool> SaveAsync()
        {
            try
            {
                await using ServerDbContext context = new ServerDbContext();
                if (m_dbMagic.Id == 0)
                    context.Magic.Add(m_dbMagic);
                else context.Magic.Update(m_dbMagic);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public async Task<bool> DeleteAsync()
        {
            try
            {
                m_dbMagic.Unlearn = 1;
                return await SaveAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        #endregion
    }
}