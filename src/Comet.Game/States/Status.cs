// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Status.cs
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
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.Mathematics;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Magics;
using Comet.Shared;

#endregion

namespace Comet.Game.States
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi, Size = 16)]
    public struct StatusInfoStruct
    {
        public int Status;
        public int Power;
        public int Seconds;
        public int Times;

        public StatusInfoStruct(int nStatus, int nPower, int nSecs, int nTimes)
            : this()
        {
            Status = nStatus;
            Power = nPower;
            Seconds = nSecs;
            Times = nTimes;
        }
    }

    public sealed class StatusOnce : IStatus
    {
        private Role m_pOwner;
        private TimeOutMS m_tKeep;
        private long m_dAutoFlash;
        private TimeOutMS m_tInterval;

        public StatusOnce()
        {
            m_pOwner = null;
            Identity = 0;
        }

        public StatusOnce(Role pOwner)
        {
            m_pOwner = pOwner;
            Identity = 0;
        }

        public int Identity { get; private set; }

        public bool IsValid => m_tKeep.IsActive() && !m_tKeep.IsTimeOut();

        public int Power { get; set; }

        public DbStatus Model { get; set; }

        public byte Level { get; private set; }

        public int RemainingTimes => 0;

        public int Time => m_tKeep.GetInterval();

        public int RemainingTime => m_tKeep.GetRemain() / 1000;

        public bool GetInfo(ref StatusInfoStruct pInfo)
        {
            pInfo.Power = Power;
            pInfo.Seconds = m_tKeep.GetRemain() / 1000;
            pInfo.Status = Identity;
            pInfo.Times = 0;

            return IsValid;
        }

        public async Task<bool> ChangeDataAsync(int nPower, int nSecs, int nTimes = 0, uint wCaster = 0U)
        {
            try
            {
                Power = nPower;
                m_tKeep.SetInterval((int) Math.Min(int.MaxValue, (long) nSecs * 1000));
                m_tKeep.Update();

                if (Model != null)
                {
                    Model.Power = nPower;
                    Model.IntervalTime = (uint) nSecs;
                    Model.EndTime = DateTime.Now.AddSeconds(nSecs);
                    await BaseRepository.SaveAsync(Model);
                }

                if (wCaster != 0)
                    CasterId = wCaster;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IncTime(int nMilliSecs, int nLimit)
        {
            int nInterval = Math.Min(nMilliSecs + m_tKeep.GetRemain(), nLimit);
            m_tKeep.SetInterval(nInterval);
            return m_tKeep.Update();
        }

        public bool ToFlash()
        {
            if (!IsValid)
                return false;

            if (m_dAutoFlash == 0 && m_tKeep.GetRemain() <= 5000)
            {
                m_dAutoFlash = 1;
                return true;
            }

            return false;
        }

        public uint CasterId { get; private set; }

        public bool IsUserCast => CasterId == m_pOwner.Identity || CasterId == 0;

        public async Task OnTimerAsync()
        {
            if (!IsValid || !m_tKeep.ToNextTime())
                return;

            try
            {
                switch (Identity)
                {
                    case StatusSet.LUCKY_DIFFUSE:
                    {
                        if (!(m_pOwner is Character user))
                            return;

                        if (!m_tInterval.ToNextTime(1000))
                            return;

                        await user.ChangeLuckyTimerAsync(3);
                        break;
                    }
                    case StatusSet.LUCKY_ABSORB:
                    {
                        if (!(m_pOwner is Character user))
                            return;

                        if (!m_tInterval.ToNextTime(1000))
                            return;
                        
                        Role sender = user.QueryRole(CasterId);
                        if (sender == null || sender.GetDistance(user) > 3)
                        {
                            m_tKeep.Clear(); // cancel
                            return;
                        }

                        await user.ChangeLuckyTimerAsync(1);
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }
        }

        public async Task<bool> CreateAsync(Role pRole, int nStatus, int nPower, int nSecs, int nTimes, uint caster = 0, byte level = 0, bool save = false)
        {
            m_pOwner = pRole;
            CasterId = caster;
            Identity = nStatus;
            Power = nPower;
            m_tKeep = new TimeOutMS(Math.Min(int.MaxValue, nSecs * 1000));
            m_tKeep.Startup((int) Math.Min((long) nSecs * 1000, int.MaxValue));
            m_tKeep.Update();
            m_tInterval = new TimeOutMS(1000);
            m_tInterval.Update();
            Level = level;

            if (save && m_pOwner is Character)
            {
                Model = new DbStatus
                {
                    Status = (uint) nStatus,
                    Power = nPower,
                    IntervalTime = (uint) nSecs,
                    LeaveTimes = 0,
                    RemainTime = (uint) nSecs,
                    EndTime = DateTime.Now.AddSeconds(nSecs),
                    OwnerId = m_pOwner.Identity,
                    Sort = 0
                };
                await BaseRepository.SaveAsync(Model);
            }

            return true;
        }
    }

    public sealed class StatusMore : IStatus
    {
        private Role m_pOwner;
        private TimeOut m_tKeep;
        private long m_dAutoFlash;
        private int m_nTimes;

        public StatusMore()
        {
            m_pOwner = null;
            Identity = 0;
        }

        public StatusMore(Role pOwner)
        {
            m_pOwner = pOwner;
            Identity = 0;
        }

        public int Identity { get; private set; }

        public bool IsValid => m_nTimes > 0;

        public int Power { get; set; }

        public DbStatus Model { get; set; }

        public byte Level { get; private set; }

        public int RemainingTimes => m_nTimes;

        public int RemainingTime => m_tKeep.GetRemain();

        public int Time => m_tKeep.GetInterval();

        public bool GetInfo(ref StatusInfoStruct pInfo)
        {
            pInfo.Power = Power;
            pInfo.Seconds = m_tKeep.GetRemain();
            pInfo.Status = Identity;
            pInfo.Times = m_nTimes;

            return IsValid;
        }

        public async Task<bool> ChangeDataAsync(int nPower, int nSecs, int nTimes = 0, uint wCaster = 0U)
        {
            try
            {
                Power = nPower;
                m_tKeep.SetInterval(nSecs);
                m_tKeep.Update();
                CasterId = wCaster;

                if (nTimes > 0)
                    m_nTimes = nTimes;

                if (Model != null)
                {
                    Model.Power = nPower;
                    Model.LeaveTimes = (uint) nTimes;
                    Model.IntervalTime = (uint)nSecs;
                    Model.EndTime = DateTime.Now.AddSeconds(nSecs);
                    await BaseRepository.SaveAsync(Model);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IncTime(int nMilliSecs, int nLimit)
        {
            int nInterval = Math.Min(nMilliSecs + m_tKeep.GetRemain(), nLimit);
            m_tKeep.SetInterval(nInterval);
            return m_tKeep.Update();
        }

        public bool ToFlash()
        {
            if (!IsValid)
                return false;

            if (m_dAutoFlash == 0 && m_tKeep.GetRemain() <= 5000)
            {
                m_dAutoFlash = 1;
                return true;
            }

            return false;
        }

        public uint CasterId { get; private set; }

        public bool IsUserCast => CasterId == m_pOwner.Identity || CasterId == 0;

        public async Task OnTimerAsync()
        {
            try
            {
                if (!IsValid || !m_tKeep.ToNextTime())
                    return;

                if (m_pOwner != null)
                {
                    int loseLife = 0;
                    switch (Identity)
                    {
                        case StatusSet.POISONED: // poison
                        {
                            if (!m_pOwner.IsAlive)
                                return;

                            loseLife = (int) Calculations.CutOverflow(Power, m_pOwner.Life - 1);
                            await m_pOwner.AddAttributesAsync(ClientUpdateType.Hitpoints, -1 * loseLife);

                            var msg2 = new MsgMagicEffect
                            {
                                AttackerIdentity = m_pOwner.Identity,
                                MagicIdentity = MagicData.POISON_MAGIC_TYPE
                            };
                            msg2.Append(m_pOwner.Identity, loseLife, true);
                            await m_pOwner.BroadcastRoomMsgAsync(msg2, true);

                            if (!m_pOwner.IsAlive)
                                await m_pOwner.BeKillAsync(null);
                            break;
                        }

                        case StatusSet.TOXIC_FOG:
                        {
                            if (!m_pOwner.IsAlive)
                            {
                                m_nTimes = 1;
                                break;
                            }

                            loseLife = Calculations.AdjustData((int) m_pOwner.Life, Power);
                            if (m_pOwner.Life - loseLife <= 0)
                                loseLife = 0;


                            await m_pOwner.BeAttackAsync(BattleSystem.MagicType.Normal, m_pOwner, loseLife, false);

                            var msg = new MsgMagicEffect
                            {
                                AttackerIdentity = m_pOwner.Identity,
                                MagicIdentity = MagicData.POISON_MAGIC_TYPE
                            };
                            msg.Append(m_pOwner.Identity, loseLife, true);
                            await m_pOwner.Map.BroadcastRoomMsgAsync(m_pOwner.MapX, m_pOwner.MapY, msg);
                            break;
                        }

                        case StatusSet.VORTEX:
                        {
                            if (!m_pOwner.IsAlive)
                            {
                                m_nTimes = 1;
                                break;
                            }

                            await m_pOwner.ProcessMagicAttackAsync(6010, 0, m_pOwner.MapX, m_pOwner.MapY);
                            break;
                        }
                    }

                    m_nTimes--;
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "StatusOnce::OnTimer() error!");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }
        }

        ~StatusMore()
        {
            // todo destroy and detach status
        }

        public Task<bool> CreateAsync(Role pRole, int nStatus, int nPower, int nSecs, int nTimes, uint wCaster = 0, byte level = 0, bool save = false)
        {
            m_pOwner = pRole;
            Identity = nStatus;
            Power = nPower;
            m_tKeep = new TimeOut(nSecs);
            m_tKeep.Update(); // no instant start
            m_nTimes = nTimes;
            CasterId = wCaster;
            Level = level;

            if (save && m_pOwner is Character)
            {
                Model = new DbStatus
                {
                    Status = (uint)nStatus,
                    Power = nPower,
                    IntervalTime = (uint)nSecs,
                    LeaveTimes = (uint) nTimes,
                    RemainTime = (uint)nSecs,
                    EndTime = DateTime.Now.AddSeconds(nSecs),
                    OwnerId = m_pOwner.Identity,
                    Sort = 0
                };
            }

            return Task.FromResult(true);
        }
    }

    public sealed class StatusSet
    {
        public const int NONE = 0,
            BLUE_NAME = 1,
            POISONED = 2,
            FULL_INVIS = 3,
            FADE = 4,
            START_XP = 5,
            GHOST = 6,
            TEAM_LEADER = 7,
            STAR_OF_ACCURACY = 8,
            SHIELD = 9,
            STIG = 10,
            DEAD = 11,
            INVISIBLE = 12,
            UNKNOWN13 = 13,
            UNKNOWN14 = 14,
            RED_NAME = 15,
            BLACK_NAME = 16,
            UNKNOWN17 = 17,
            UNKNOWN18 = 18,
            SUPERMAN = 19,
            REFLECTTYPE_THING = 20,
            DIF_REFLECT_THING = 21,
            FREEZE = 22,
            PARTIALLY_INVISIBLE = 23,
            CYCLONE = 24,
            UNKNOWN25 = 25,
            UNKNOWN26 = 26,
            DODGE = 27,
            FLY = 28,
            INTENSIFY = 29,
            UNKNOWN30 = 30,
            LUCKY_DIFFUSE = 31,
            LUCKY_ABSORB = 32,
            CURSED = 33,
            HEAVEN_BLESS = 34,
            TOP_GUILD = 35,
            TOP_DEP = 36,
            MONTH_PK = 37,
            WEEK_PK = 38,
            TOP_WARRIOR = 39,
            TOP_TRO = 40,
            TOP_ARCHER = 41,
            TOP_WATER = 42,
            TOP_FIRE = 43,
            TOP_NINJA = 44,
            POISON_STAR = 45,
            TOXIC_FOG = 46,
            VORTEX = 47,
            FATAL_STRIKE = 48,
            ORANGE_HALO_GLOW = 49,
            UNKNOWN50 = 50,
            LOW_VIGOR_UNABLE_TO_JUMP = 51,
            RIDING = 51,
            TOP_SPOUSE = 52,
            SPARKLE_HALO = 53,
            NO_POTION = 54,
            DAZED = 55,
            BLUE_RESTORE_AURA = 56,
            MOVE_SPEED_RECOVERED = 57,
            SUPER_SHIELD_HALO = 58,
            HUGE_DAZED = 59,
            ICE_BLOCK = 60,
            CONFUSED = 61,
            UNKNOWN62 = 62,
            UNKNOWN63 = 63,
            UNKNOWN64 = 64,
            WEEKLY_TOP8_PK = 65,
            WEEKLY_TOP2_PK_GOLD = 66,
            WEEKLY_TOP2_PK_BLUE = 67,
            MONTHLY_TOP8_PK = 68,
            MONTLY_TOP2_PK = 69,
            MONTLY_TOP3_PK = 70,
            TOP8_FIRE = 71,
            TOP2_FIRE = 72,
            TOP3_FIRE = 73,
            TOP8_WATER = 74,
            TOP2_WATER = 75,
            TOP3_WATER = 76,
            TOP8_NINJA = 77,
            TOP2_NINJA = 78,
            TOP3_NINJA = 79,
            TOP8_WARRIOR = 80,
            TOP2_WARRIOR = 81,
            TOP3_WARRIOR = 82,
            TOP8_TROJAN = 83,
            TOP2_TROJAN = 84,
            TOP3_TROJAN = 85,
            TOP8_ARCHER = 86,
            TOP2_ARCHER = 87,
            TOP3_ARCHER = 88,
            TOP3_SPOUSE_BLUE = 89,
            TOP2_SPOUSE_BLUE = 90,
            TOP3_SPOUSE_YELLOW = 91,
            CONTESTANT = 92,
            CHAIN_BOLT_ACTIVE = 93,
            AZURE_SHIELD = 94,
            AZURE_SHIELD_FADE = 95,
            CARYING_FLAG = 96,
            UNKNOWN97 = 97,
            UNKNOWN98 = 98,
            TYRANT_AURA = 99,
            UNKNOWN100 = 100,
            FEND_AURA = 101,
            UNKNOWN102 = 102,
            METAL_AURA = 103,
            UNKNOWN104 = 104,
            WOOD_AURA = 105,
            UNKNOWN106 = 106,
            WATER_AURA = 107,
            UNKNOWN108 = 108,
            FIRE_AURA = 109,
            UNKNOWN110 = 110,
            EARTH_AURA = 111,
            SHACKLED = 112,
            OBLIVION = 113,
            UNKNOWN114 = 114,
            TOP_MONK = 115,
            TOP8_MONK = 116,
            TOP2_MONK = 117,
            TOP3_MONK = 118,
            CTF_FLAG = 119,
            SCURVY_BOMB = 120,
            CANNON_BARRAGE = 121,
            BLACKBEARDS_REVENGE = 122,
            TOP_PIRATE = 123,
            TOP_PIRATE8 = 124,
            TOP_PIRATE2 = 125,
            TOP_PIRATE3 = 126,
            DEFENSIVE_INSTANCE = 127,
            MAGIC_DEFENDER = 129,
            ASSASSIN = 146,
            BLADE_FLURRY = 147,
            KINETIC_SPARK = 148,
            AUTO_HUNTING = 149;

        private readonly Role m_pOwner;
        public ConcurrentDictionary<int, IStatus> Status;

        public StatusSet(Role pRole)
        {
            if (pRole == null)
                return;

            m_pOwner = pRole;

            Status = new ConcurrentDictionary<int, IStatus>(5, 128);
        }

        private ulong StatusFlag
        {
            get => m_pOwner.StatusFlag;
            set => m_pOwner.StatusFlag = value;
        }

        public IStatus this[int nKey]
        {
            get
            {
                try
                {
                    return Status.TryGetValue(nKey, out var ret) ? ret : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public int GetAmount()
        {
            return Status.Count;
        }

        public IStatus GetObjByIndex(int nKey)
        {
            IStatus ret;
            return Status.TryGetValue(nKey, out ret) ? ret : null;
        }

        public IStatus GetObj(ulong nKey, bool b64 = false)
        {
            IStatus ret;
            return Status.TryGetValue(InvertFlag(nKey, b64), out ret) ? ret : null;
        }

        public async Task<bool> AddObjAsync(IStatus pStatus)
        {
            var pInfo = new StatusInfoStruct();
            pStatus.GetInfo(ref pInfo);
            if (Status.ContainsKey(pInfo.Status))
                return false; // status already exists

            ulong flag = 1UL << (pInfo.Status - 1);
            Status.TryAdd(pInfo.Status, pStatus);
            StatusFlag |= flag;

            await m_pOwner.SetAttributesAsync(ClientUpdateType.StatusFlag, StatusFlag);
            return true;
        }

        public async Task<bool> DelObjAsync(int nFlag)
        {
            if (nFlag > 192)
                return false;

            if (!Status.TryRemove(nFlag, out var status))
                return false;

            ulong uFlag = 1UL << (nFlag - 1);
            StatusFlag &= ~uFlag;

            if (status?.Model != null)
            {
                await BaseRepository.DeleteAsync(status.Model);
            }

            await m_pOwner.SetAttributesAsync(ClientUpdateType.StatusFlag, StatusFlag);
            return true;
        }

        /// <summary>
        ///     Gotta check if there is a faster way to do this.
        /// </summary>
        /// <param name="flag">The flag that will be checked.</param>
        /// <param name="b64">If it's a effect 2 flag, you should set this true.</param>
        /// <returns></returns>
        public static int InvertFlag(ulong flag, bool b64 = false, bool b128 = false)
        {
            var inv = flag >> 0;
            int ret = -1;
            for (int i = 0; inv > 1; i++)
            {
                inv = flag >> i;
                ret++;
            }

            return !b64 ? ret : !b128 ? ret + 64 : ret + 128;
        }

        public async Task SendAllStatusAsync()
        {
            if (m_pOwner is Character pUsr)
                await pUsr.SynchroAttributesAsync(ClientUpdateType.StatusFlag, StatusFlag, true);
        }

        public static ulong GetFlag(int status)
        {
            return 1UL << (status - 1);
        }
    }

    public interface IStatus
    {
        /// <summary>
        ///     This method will get the status id.
        /// </summary>
        int Identity { get; }

        /// <summary>
        ///     This method will check if the status still valid and running.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        ///     This method will return the power of the status. This wont make percentage checks. The value is a short.
        /// </summary>
        int Power { get; set; }

        int Time { get; }

        int RemainingTimes { get; }

        int RemainingTime { get; }

        byte Level { get; }
        uint CasterId { get; }
        bool IsUserCast { get; }

        /// <summary>
        ///     This method will get the status information into another param.
        /// </summary>
        /// <param name="pInfo">The structure that will be filled with the information.</param>
        bool GetInfo(ref StatusInfoStruct pInfo);

        /// <summary>
        ///     This method will override the old values from the status.
        /// </summary>
        /// <param name="nPower">The new power of the status.</param>
        /// <param name="nSecs">The remaining time to the status.</param>
        /// <param name="nTimes">How many times the status will appear. If StatusMore.</param>
        /// <param name="wCaster">The identity of the caster.</param>
        Task<bool> ChangeDataAsync(int nPower, int nSecs, int nTimes = 0, uint wCaster = 0);

        bool IncTime(int nMilliSecs, int nLimit);
        bool ToFlash();
        Task OnTimerAsync();

        DbStatus Model { get; set; }
    }
}