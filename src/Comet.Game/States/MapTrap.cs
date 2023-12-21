// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MapTrap.cs
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

using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.World;
using Comet.Shared;

#endregion

namespace Comet.Game.States
{
    public sealed class MapTrap : Role
    {
        private readonly TimeOutMS m_tFight;
        private readonly TimeOut m_tLifePeriod;

        private DbTrap m_dbTrap;
        private Role m_owner;

        public MapTrap(DbTrap trap)
        {
            m_dbTrap = trap;

            Identity = trap.Id;
            MapIdentity = trap.MapId;
            MapX = trap.PosX;
            MapY = trap.PosY;

            m_tLifePeriod = new TimeOut();
            m_tLifePeriod.Clear();
            m_tFight = new TimeOutMS();
            m_tFight.Clear();
        }

        public uint Type => m_dbTrap.TypeId;
        public uint IdAction => m_dbTrap.Type.ActionId;
        public int ActiveTimes => m_dbTrap.Type.ActiveTimes;
        public int RemainingActiveTimes { get; set; }
        public override byte Level
        {
            get => m_dbTrap.Type.Level;
            set => m_dbTrap.Type.Level = value;
        }
        public override int MinAttack => m_dbTrap.Type.AttackMin;
        public override int MaxAttack => m_dbTrap.Type.AttackMax;
        public override int MagicAttack => m_dbTrap.Type.AttackMax;
        public int AttackMode => m_dbTrap.Type.AtkMode;
        public override int AttackSpeed => m_dbTrap.Type.AttackSpeed;
        public ushort MagicType => m_dbTrap.Type.MagicType;
        public int MagicHitRate => m_dbTrap.Type.MagicHitrate;
        public int Size => m_dbTrap.Type.Size;

        public bool IsTrapSort => m_dbTrap.Type.Sort < 10;

        public bool IsAutoSort => !IsTrapSort;

        public override bool IsAlive => RemainingActiveTimes > 0;

        public bool IsInRange(Role target)
        {
            return GetDistance(target) <= Size;
        }

        #region Initialization

        public async Task<bool> InitializeAsync(Role owner = null)
        {
            m_dbTrap.Type ??= await DbTrapType.GetAsync(m_dbTrap.TypeId);

            if (m_dbTrap.Type == null)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Trap has no type {m_dbTrap.Type} [TrapId: {m_dbTrap.Id}]");
                return false;
            }

            m_owner = owner;

            m_tFight.SetInterval(m_dbTrap.Type.AttackSpeed);

            Mesh = m_dbTrap.Look;

            m_idMap = m_dbTrap.MapId;
            m_posX = m_dbTrap.PosX;
            m_posY = m_dbTrap.PosY;

            await EnterMapAsync();
            return true;
        }

        #endregion

        #region Map

        public override Task EnterMapAsync()
        {
            Map = Kernel.MapManager.GetMap(MapIdentity);
            if (Map != null)
            {
                return Map.AddAsync(this);
            }
            return Task.CompletedTask;
        }

        public override async Task LeaveMapAsync()
        {
            await BroadcastRoomMsgAsync(new MsgMapItem
            {
                Identity = Identity,
                Itemtype = Mesh,
                MapX = MapX,
                MapY = MapY,
                Mode = DropType.DropTrap
            }, false);

            if (Map != null)
            {
                Kernel.Services.Processor.Queue(Map.Partition, async () =>
                {
                    await Map.RemoveAsync(Identity);
                });
            }

            if (Identity >= MAGICTRAPID_FIRST && Identity <= MAGICTRAPID_LAST)
                IdentityGenerator.Traps.ReturnIdentity(Identity);
        }

        #endregion

        #region Trap Attack

        public async Task TrapAttackAsync(Role target)
        {
            if (!IsTrapSort)
                return;

            if (!m_tFight.ToNextTime(AttackSpeed))
                return;

            if (RemainingActiveTimes > 0)
                RemainingActiveTimes--;

            if (!target.IsAttackable(this))
                return;

            if (m_owner?.IsImmunity(target) == true)
                return;

            if ((AttackMode & (int) TargetType.User) != 0 && !(target is Character))
                return;

            if ((AttackMode & (int)TargetType.Monster) != 0 && !(target is Monster))
                return;

            Character user = m_owner as Character;
            if (user?.IsEvil() == true)
                await user.SetCrimeStatusAsync(25);

            if ((AttackMode & (int) TargetType.Passive) == 0)
            {
                BattleSystem.CreateBattle(target.Identity);
                QueueAction(BattleSystem.ProcessAttackAsync);
            }

            if (IdAction > 0)
            {
                if ((AttackMode & (int) TargetType.User) != 0)
                    await GameAction.ExecuteActionAsync(IdAction, target as Character, this, null, "");
                else if ((AttackMode & (int)TargetType.Monster) != 0)
                    await GameAction.ExecuteActionAsync(IdAction, null, target, null, "");
                else
                    await GameAction.ExecuteActionAsync(IdAction, null, null, null, "");
            }

            if (ActiveTimes > 0 && RemainingActiveTimes <= 0)
                await LeaveMapAsync();
        }

        public override async Task KillAsync(Role target, uint dieWay)
        {
            if (target == null)
                return;

            if (m_owner != null)
            {
                await m_owner.KillAsync(target, dieWay);
                return;
            }

            await BroadcastRoomMsgAsync(new MsgInteract
            {
                Action = MsgInteractType.Kill,
                SenderIdentity = Identity,
                TargetIdentity = target.Identity,
                PosX = target.MapX,
                PosY = target.MapY,
                Data = (int)dieWay
            }, true);

            await target.BeKillAsync(this);
        }

        #endregion

        #region Timer

        public override async Task OnTimerAsync()
        {
            if (!IsAlive)
                return;

            if (m_tLifePeriod.IsActive())
            {
                if (m_tLifePeriod.ToNextTime())
                {
                    await LeaveMapAsync();
                    m_tLifePeriod.Clear();
                }
            }

            if (IsAutoSort)
            {
                if (m_tFight.ToNextTick(AttackSpeed))
                {
                    if (ActiveTimes > 0)
                        RemainingActiveTimes--;

                    // only on higher versions we have magic attacks on traps, wont implement this now

                    if (ActiveTimes > 0 && RemainingActiveTimes <= 0)
                        await LeaveMapAsync();
                }
            }
        }

        #endregion

        #region Socket

        public override Task SendSpawnToAsync(Character player)
        {
            return player.SendAsync(new MsgMapItem
            {
                Identity = Identity,
                Itemtype = Mesh,
                MapX = MapX,
                MapY = MapY,
                Mode = DropType.SynchroTrap
            });
        }

        #endregion

        public enum TargetType
        {
            None,
            User = 1,
            Monster = 2,
            Passive = 4
        }
    }
}