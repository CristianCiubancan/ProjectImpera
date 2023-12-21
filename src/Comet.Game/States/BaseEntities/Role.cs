// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Role.cs
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core.Mathematics;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.Magics;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;

namespace Comet.Game.States.BaseEntities
{
    public abstract class Role
    {
        protected uint m_idMap;
        protected ushort m_posX,
            m_posY;

        protected uint m_maxLife = 0,
            m_maxMana = 0;

        protected Role()
        {
            StatusSet = new StatusSet(this);
            BattleSystem = new BattleSystem(this);
            MagicData = new MagicData(this);
        }

        #region Identity

        /// <summary>
        ///     The identity of the role in the world. This will be unique for ANY role in the world.
        /// </summary>
        public virtual uint Identity { get; protected set; }

        /// <summary>
        ///     The name of the role. May be empty or null for NPCs and Dynamic NPCs.
        /// </summary>
        public virtual string Name { get; set; }

        #endregion

        #region Appearence

        public virtual uint Mesh { get; set; }

        #endregion

        #region Level

        public virtual byte Level { get; set; }

        #endregion

        #region Life and Mana

        public virtual bool IsAlive => Life > 0;
        public virtual uint Life { get; set; }
        public virtual uint MaxLife => m_maxLife;
        public virtual uint Mana { get; set; }
        public virtual uint MaxMana => m_maxMana;

        #endregion

        #region Map and Position

        public virtual GameMap Map { get; protected set; }

        /// <summary>
        ///     The current map identity for the role.
        /// </summary>
        public virtual uint MapIdentity
        {
            get => m_idMap;
            set => m_idMap = value;
        }

        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public virtual ushort MapX
        {
            get => m_posX;
            set => m_posX = value;
        }

        /// <summary>
        ///     Current Y position of the user in the map.
        /// </summary>
        public virtual ushort MapY
        {
            get => m_posY;
            set => m_posY = value;
        }

        public virtual int GetDistance(Role role)
        {
            return GetDistance(role.MapX, role.MapY);
        }

        public virtual int GetDistance(int x, int y)
        {
            return ScreenCalculations.GetDistance(MapX, MapY, x, y);
        }

        /// <summary>
        /// </summary>
        public virtual Task EnterMapAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// </summary>
        public virtual Task LeaveMapAsync()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Movement

        public async Task<bool> JumpPosAsync(int x, int y, bool sync = false)
        {
            if (x == MapX && y == MapY)
                return false;

            if (Map == null || !Map.IsValidPoint(x, y))
                return false;

            Character user = null;
            if (IsPlayer())
            {
                user = (Character)this;
                // we're trusting this
                if (GetDistance(x, y) > Screen.VIEW_SIZE || !Map.IsStandEnable(x, y))
                {
                    await user.SendAsync(Language.StrInvalidCoordinate, MsgTalk.TalkChannel.System, Color.Red);
                    await user.KickbackAsync();
                    return false;
                }

                if (user.QueryStatus(StatusSet.RIDING) != null)
                {
                    int distance = user.GetDistance(x, y);
                    int vigorConsume = (-1 * distance);
                    if (vigorConsume > 0 && vigorConsume > user.Vigor)
                    {
                        await user.KickbackAsync();
                        return false;
                    }
                    await AddAttributesAsync(ClientUpdateType.Vigor, vigorConsume);
                    user.UpdateVigorTimer();
                }
            }

            Map.EnterBlock(this, x, y, MapX, MapY);

            Direction = (FacingDirection)ScreenCalculations.GetDirectionSector(MapX, MapY, x, y);

            if (sync)
            {
                await BroadcastRoomMsgAsync(new MsgAction
                {
                    X = (ushort) x,
                    Y = (ushort) y,
                    ArgumentX = m_posX,
                    ArgumentY = m_posY,
                    Identity = Identity,
                    Action = MsgAction.ActionType.MapJump,
                    Direction = (ushort) Direction
                }, true);
            }

            m_posX = (ushort)x;
            m_posY = (ushort)y;

            await ProcessAfterMoveAsync();
            return true;
        }

        public async Task<bool> MoveTowardAsync(int direction, int mode, bool sync = false)
        {
            Character user = this as Character;
            ushort newX = 0, newY = 0;
            
            if (mode == (int) RoleMoveMode.Track)
            {
                direction %= 24;
                newX = (ushort)(MapX + GameMap.RideXCoords[direction]);
                newY = (ushort)(MapY + GameMap.RideYCoords[direction]);
            }
            else
            {
                direction %= 8;
                newX = (ushort)(MapX + GameMap.WalkXCoords[direction]);
                newY = (ushort)(MapY + GameMap.WalkYCoords[direction]);

                bool isRunning = mode >= (int)RoleMoveMode.RunDir0 &&
                                 mode <= (int)RoleMoveMode.RunDir7;
                if (isRunning && IsAlive)
                {
                    newX += (ushort)GameMap.WalkXCoords[direction];
                    newY += (ushort)GameMap.WalkYCoords[direction];
                }
            }

            int vigor = 0;
            if (user != null && QueryStatus(StatusSet.RIDING) != null)
            {
                vigor = GetDistance(newX, newY);
                if (user.Vigor < vigor)
                {
                    await user.KickbackAsync();
                    return false;
                }
            }

            if (!IsAlive && user != null && !user.IsGhost())
            {
                await user.SendAsync(Language.StrDead, MsgTalk.TalkChannel.System, Color.Red);
                return false;
            }

            if (!Map.IsMoveEnable(newX, newY) && user != null)
            {
                await user.KickbackAsync();
                await user.SendAsync(Language.StrInvalidCoordinate, MsgTalk.TalkChannel.System, Color.Red); 
                return false;
            }

            if (vigor > 0)
            {
                await AddAttributesAsync(ClientUpdateType.Vigor, vigor);
                user.UpdateVigorTimer();
            }

            Map.EnterBlock(this, newX, newY, MapX, MapY);

            Direction = (FacingDirection)direction;

            if (sync)
            {
                await BroadcastRoomMsgAsync(new MsgWalk
                {
                    Direction = (byte) direction,
                    Identity = Identity,
                    Mode = (byte) mode
                }, true);
            }

            m_posX = newX;
            m_posY = newY;

            await ProcessAfterMoveAsync();
            return true;
        }

        public virtual async Task ProcessOnMoveAsync()
        {
            BattleSystem.ResetBattle();
            
            if (MagicData.State == MagicData.MagicState.Intone || MagicData.IsAutoAttack())
                await MagicData.AbortMagicAsync(true);

            await DetachStatusAsync(StatusSet.INTENSIFY);

            await DetachStatusAsync(StatusSet.LUCKY_DIFFUSE);
            await DetachStatusAsync(StatusSet.LUCKY_ABSORB);
        }

        public virtual async Task ProcessAfterMoveAsync()
        {
            Action = EntityAction.Stand;

            foreach (var trap in Map.Query9BlocksByPos(MapX, MapY).Where(x => x is MapTrap).Cast<MapTrap>())
            {
                if (trap.IsTrapSort && trap.IsInRange(this))
                    await trap.TrapAttackAsync(this);
            }
        }

        public virtual async Task ProcessOnAttackAsync()
        {
            Action = EntityAction.Stand;
            await DetachStatusAsync(StatusSet.INTENSIFY);

            await DetachStatusAsync(StatusSet.LUCKY_DIFFUSE);
            await DetachStatusAsync(StatusSet.LUCKY_ABSORB);
        }

        #endregion

        #region Action and Direction

        public virtual FacingDirection Direction { get; protected set; }

        public virtual EntityAction Action { get; protected set; }

        public async Task SetDirectionAsync(FacingDirection direction, bool sync = true)
        {
            Direction = direction;
            if (sync)
            {
                await BroadcastRoomMsgAsync(new MsgAction
                {
                    Identity = Identity,
                    Action = MsgAction.ActionType.CharacterDirection,
                    Direction = (ushort)direction,
                    ArgumentX = MapX,
                    ArgumentY = MapY
                }, true);
            }
        }

        public async Task SetActionAsync(EntityAction action, bool sync = true)
        {
            if (action != EntityAction.Cool && IsWing)
                return;

            Action = action;
            if (sync)
            {
                await BroadcastRoomMsgAsync(new MsgAction
                {
                    Identity = Identity,
                    Action = MsgAction.ActionType.CharacterEmote,
                    Command = (ushort)action,
                    ArgumentX = MapX,
                    ArgumentY = MapY,
                    Direction = (ushort)Direction
                }, true);
            }
        }

        #endregion

        #region Role Type

        public bool IsPlayer()
        {
            return Identity >= PLAYER_ID_FIRST && Identity < PLAYER_ID_LAST;
        }

        public bool IsMonster()
        {
            return Identity >= MONSTERID_FIRST && Identity < MONSTERID_LAST;
        }

        public bool IsNpc()
        {
            return Identity >= SYSNPCID_FIRST && Identity < SYSNPCID_LAST;
        }

        public bool IsDynaNpc()
        {
            return Identity >= DYNANPCID_FIRST && Identity < DYNANPCID_LAST;
        }

        public bool IsPet()
        {
            return Identity >= PETID_FIRST && Identity < PETID_LAST;
        }

        public bool IsTrap()
        {
            return Identity >= TRAPID_FIRST && Identity < TRAPID_LAST;
        }

        public bool IsMapItem()
        {
            return Identity >= MAPITEM_FIRST && Identity < MAPITEM_LAST;
        }

        public bool IsFurniture()
        {
            return Identity >= SCENE_NPC_MIN && Identity < SCENE_NPC_MAX;
        }

        #endregion

        #region Battle Attributes

        public virtual int BattlePower => 1;

        public virtual int MinAttack { get; } = 1;
        public virtual int MaxAttack { get; } = 1;
        public virtual int MagicAttack { get; } = 1;
        public virtual int Defense { get; } = 0;
        public virtual int MagicDefense { get; } = 0;
        public virtual int MagicDefenseBonus { get; } = 0;
        public virtual int Dodge { get; } = 0;
        public virtual int AttackSpeed { get; } = 1000;
        public virtual int Accuracy { get; } = 1;
        public virtual int Defense2 => Calculations.DEFAULT_DEFENCE2;
        public virtual int Blessing { get; } = 0;

        public virtual int AddFinalAttack { get; } = 0;
        public virtual int AddFinalMAttack { get; } = 0;
        public virtual int AddFinalDefense { get; } = 0;
        public virtual int AddFinalMDefense { get; } = 0;

        public virtual int ExtraDamage { get; } = 0;

        #endregion

        #region Battle Processing

        public BattleSystem BattleSystem { get; }
        public MagicData MagicData { get; }

        public async Task<bool> ProcessMagicAttackAsync(ushort usMagicType, uint idTarget, ushort x, ushort y,
            uint ucAutoActive = 0)
        {
            return await MagicData.ProcessMagicAttackAsync(usMagicType, idTarget, x, y, ucAutoActive);
        }

        public int SizeAddition => 1;

        public virtual Task<bool> CheckCrimeAsync(Role target)
        {
            return Task.FromResult(false);
        }

        public virtual int AdjustWeaponDamage(int damage)
        {
            return Calculations.MulDiv(damage, Defense2, Calculations.DEFAULT_DEFENCE2);
        }

        public int AdjustMagicDamage(int damage)
        {
            return Calculations.MulDiv(damage, Defense2, Calculations.DEFAULT_DEFENCE2);
        }


        public virtual int GetAttackRange(int sizeAdd)
        {
            return sizeAdd + 1;
        }

        public virtual bool IsAttackable(Role attacker)
        {
            return true;
        }

        public virtual bool IsImmunity(Role target)
        {
            if (target == null)
                return true;
            if (target.Identity == Identity)
                return true;
            return false;
        }

        public virtual Task<(int Damage, InteractionEffect Effect)> AttackAsync(Role target)
        {
            return BattleSystem.CalcPowerAsync(BattleSystem.MagicType.None, this, target);
        }

        public virtual Task<bool> BeAttackAsync(BattleSystem.MagicType magic, Role attacker, int nPower, bool bReflectEnable)
        {
            return Task.FromResult(false);
        }

        public virtual async Task KillAsync(Role target, uint dieWay)
        {
            if (this is Monster guard && guard.IsGuard())
                await BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.Kill,
                    SenderIdentity = Identity,
                    TargetIdentity = target.Identity,
                    PosX = target.MapX,
                    PosY = target.MapY,
                    Data = (int) dieWay
                }, true);

            await target.BeKillAsync(this);
        }

        public virtual Task BeKillAsync(Role attacker)
        {
            return Task.CompletedTask;
        }

        public async Task SendDamageMsgAsync(uint idTarget, int nDamage)
        {
            MsgInteract msg = new MsgInteract
            {
                SenderIdentity = Identity,
                TargetIdentity = idTarget,
                Data = nDamage,
                PosX = MapX,
                PosY = MapY
            };
            
            msg.Action = IsBowman ? MsgInteractType.Shoot : MsgInteractType.Attack;

            if (this is Character user)
                await user.Screen.BroadcastRoomMsgAsync(msg);
            else
                await Map.BroadcastRoomMsgAsync(MapX, MapY, msg);
        }

        #endregion

        #region Status

        public ulong StatusFlag { get; set; } = 0UL;

        public StatusSet StatusSet { get; }

        public virtual async Task<bool> DetachWellStatusAsync()
        {
            for (int i = 1; i < 128; i++)
            {
                if (StatusSet[i] != null)
                    if (IsWellStatus(i))
                        await DetachStatusAsync(i);
            }
            return true;
        }

        public virtual async Task<bool> DetachBadlyStatusAsync()
        {
            for (int i = 1; i < 128; i++)
            {
                if (StatusSet[i] != null)
                    if (IsBadlyStatus(i))
                        await DetachStatusAsync(i);
            }
            return true;
        }

        public virtual async Task<bool> DetachAllStatusAsync()
        {
            await DetachBadlyStatusAsync();
            await DetachWellStatusAsync();
            return true;
        }

        public virtual bool IsWellStatus(int stts)
        {
            switch (stts)
            {
                case StatusSet.RIDING:
                case StatusSet.FULL_INVIS:
                case StatusSet.LUCKY_DIFFUSE:
                case StatusSet.STIG:
                case StatusSet.SHIELD:
                case StatusSet.STAR_OF_ACCURACY:
                case StatusSet.START_XP:
                case StatusSet.INVISIBLE:
                case StatusSet.SUPERMAN:
                case StatusSet.CYCLONE:
                case StatusSet.PARTIALLY_INVISIBLE:
                case StatusSet.LUCKY_ABSORB:
                case StatusSet.VORTEX:
                case StatusSet.FLY:
                case StatusSet.FATAL_STRIKE:
                case StatusSet.AZURE_SHIELD:
                case StatusSet.SUPER_SHIELD_HALO:
                case StatusSet.CARYING_FLAG:
                case StatusSet.EARTH_AURA:
                case StatusSet.FEND_AURA:
                case StatusSet.FIRE_AURA:
                case StatusSet.METAL_AURA:
                case StatusSet.TYRANT_AURA:
                case StatusSet.WATER_AURA:
                case StatusSet.WOOD_AURA:
                case StatusSet.OBLIVION:
                case StatusSet.CTF_FLAG:
                    return true;
            }
            return false;
        }

        public virtual bool IsBadlyStatus(int stts)
        {
            switch (stts)
            {
                case StatusSet.POISONED:
                case StatusSet.CONFUSED:
                case StatusSet.ICE_BLOCK:
                case StatusSet.HUGE_DAZED:
                case StatusSet.DAZED:
                case StatusSet.SHACKLED:
                case StatusSet.POISON_STAR:
                case StatusSet.TOXIC_FOG:
                    return true;
            }
            return false;
        }

        public virtual async Task<bool> AttachStatusAsync(DbStatus status)
        {
            if (Map == null)
                return false;

            if (status.LeaveTimes > 1)
            {
                var pNewStatus = new StatusMore
                {
                    Model = status
                };
                if (await pNewStatus.CreateAsync(this, (int) status.Status, status.Power, (int) status.RemainTime, (int) status.LeaveTimes))
                {
                    await StatusSet.AddObjAsync(pNewStatus);
                    return true;
                }
            }
            else
            {
                var pNewStatus = new StatusOnce
                {
                    Model = status
                };
                if (await pNewStatus.CreateAsync(this, (int) status.Status, status.Power, (int) status.RemainTime, 0))
                {
                    await StatusSet.AddObjAsync(pNewStatus);
                    return true;
                }
            }

            return false;
        }
        
        public virtual async Task<bool> AttachStatusAsync(Role pSender, int nStatus, int nPower, int nSecs, int nTimes, byte pLevel, bool save = false)
        {
            if (Map == null)
                return false;
            
            IStatus pStatus = QueryStatus(nStatus);
            if (pStatus != null)
            {
                bool bChangeData = false;
                if (pStatus.Power == nPower)
                    bChangeData = true;
                else
                {
                    int nMinPower = Math.Min(nPower, pStatus.Power);
                    int nMaxPower = Math.Max(nPower, pStatus.Power);

                    if (nPower <= 30000)
                        bChangeData = true;
                    else
                    {
                        if (nMinPower >= 30100 || nMinPower > 0 && nMaxPower < 30000)
                        {
                            if (nPower > pStatus.Power)
                                bChangeData = true;
                        }
                        else if (nMaxPower < 0 || nMinPower > 30000 && nMaxPower < 30100)
                        {
                            if (nPower < pStatus.Power)
                                bChangeData = true;
                        }
                    }
                }

                if (bChangeData)
                {
                    await pStatus.ChangeDataAsync(nPower, nSecs, nTimes, pSender?.Identity ?? 0);
                }
                return true;
            }
            else
            {
                if (nTimes > 1)
                {
                    var pNewStatus = new StatusMore();
                    if (await pNewStatus.CreateAsync(this, nStatus, nPower, nSecs, nTimes, pSender?.Identity ?? 0, pLevel, save))
                    {
                        await StatusSet.AddObjAsync(pNewStatus);
                        return true;
                    }
                }
                else
                {
                    var pNewStatus = new StatusOnce();
                    if (await pNewStatus.CreateAsync(this, nStatus, nPower, nSecs, 0, pSender?.Identity ?? 0, pLevel, save))
                    {
                        await StatusSet.AddObjAsync(pNewStatus);
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual async Task<bool> DetachStatusAsync(int nType)
        {
            return await StatusSet.DelObjAsync(nType);
        }

        public virtual async Task<bool> DetachStatusAsync(ulong nType, bool b64)
        {
            return await StatusSet.DelObjAsync(StatusSet.InvertFlag(nType, b64));
        }

        public virtual bool IsOnXpSkill()
        {
            return false;
        }

        public virtual IStatus QueryStatus(int nType)
        {
            return StatusSet?.GetObjByIndex(nType);
        }

        public bool IsGhost()
        {
            return QueryStatus(StatusSet.GHOST) != null;
        }

        public async Task SetCrimeStatusAsync(int nSecs)
        {
            await AttachStatusAsync(this, StatusSet.BLUE_NAME, 0, nSecs, 0, 0);
        }

        public virtual bool IsWing => QueryStatus(StatusSet.FLY) != null;

        public virtual bool IsBowman => false;

        public virtual bool IsShieldUser => false;

        public virtual bool IsEvil()
        {
            return QueryStatus(StatusSet.BLUE_NAME) != null || QueryStatus(StatusSet.BLACK_NAME) != null;
        }

        public bool IsVirtuous()
        {
            return (StatusFlag & KEEP_EFFECT_NOT_VIRTUOUS) == 0;
        }

        public bool IsCrime()
        {
            return QueryStatus(StatusSet.BLUE_NAME) != null;
        }

        public bool IsPker()
        {
            return QueryStatus(StatusSet.BLACK_NAME) != null;
        }

        #endregion

        #region Scapegoat

        public bool Scapegoat { get; set; } = false;

        public virtual async Task<bool> CheckScapegoatAsync(Role target)
        {
            Magic scapegoat = MagicData[6003];
            if (scapegoat != null && Scapegoat && scapegoat.IsReady())
            {
                return await ProcessMagicAttackAsync(scapegoat.Type, target.Identity, target.MapX, target.MapY);
            }
            return false;
        }

        public Task SetScapegoatAsync(bool on)
        {
            Scapegoat = on;
            return SendAsync(new MsgInteract
            {
                Action = MsgInteractType.CounterKillSwitch,
                SenderIdentity = Identity,
                TargetIdentity = Identity,
                Data = on ? 1 : 0,
                PosX = MapX,
                PosY = MapY
            });
        }

        #endregion

        #region Synchronization

        public virtual async Task<bool> AddAttributesAsync(ClientUpdateType type, long value)
        {
            long currAttr = 0;
            switch (type)
            {
                case ClientUpdateType.Hitpoints:
                    currAttr = Life = (uint) Math.Min(MaxLife, Math.Max(Life + value, 0));
                    break;

                case ClientUpdateType.Mana:
                    currAttr = Mana = (uint) Math.Min(MaxMana, Math.Max(Mana + value, 0));
                    break;

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"Role::AddAttributes {type} not handled");
                    return false;
            }

            await SynchroAttributesAsync(type, (ulong) currAttr);
            return true;
        }

        public virtual async Task<bool> SetAttributesAsync(ClientUpdateType type, ulong value)
        {
            bool screen = false;
            switch (type)
            {
                case ClientUpdateType.Hitpoints:
                    value = Life = (uint)Math.Max(0, Math.Min(MaxLife, value));
                    break;

                case ClientUpdateType.Mana:
                    value = Mana = (uint)Math.Max(0, Math.Min(MaxMana, value));
                    break;

                case ClientUpdateType.StatusFlag:
                    StatusFlag = value;
                    screen = true;
                    break;

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"Role::SetAttributes {type} not handled");
                    return false;
            }

            await SynchroAttributesAsync(type, value, screen);
            return true;
        }

        public async Task SynchroAttributesAsync(ClientUpdateType type, ulong value, bool screen = false)
        {
            MsgUserAttrib msg = new MsgUserAttrib(Identity, type, value);
            if (this is Character && !screen)
                await SendAsync(msg);

            if (screen)
            {
                if (this is Character user)
                    await user.BroadcastRoomMsgAsync(msg, true);
                else if (Map != null)
                    await Map.BroadcastRoomMsgAsync(MapX, MapY, msg, Identity);
            }
        }

        public async Task SynchroAttributesAsync(ClientUpdateType type, uint value1, uint value2, bool screen = false)
        {
            MsgUserAttrib msg = new MsgUserAttrib(Identity, type, value1);
            msg.Append(type, value2);
            if (this is Character && !screen)
                await SendAsync(msg);

            if (screen)
            {
                if (this is Character user)
                    await user.BroadcastRoomMsgAsync(msg, true);
                else if (Map != null)
                    await Map.BroadcastRoomMsgAsync(MapX, MapY, msg, Identity);
            }
        }

        #endregion

        #region Timers

        public virtual Task OnTimerAsync()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Processor Queue

        public void QueueAction(Func<Task> task)
        {
            if (Map == null)
                return;

            Kernel.Services.Processor.Queue(Map.Partition, task);
        }

        #endregion

        #region Socket

        public async Task SendEffectAsync(string effect, bool self)
        {
            MsgName msg = new MsgName
            {
                Identity = Identity, Action = StringAction.RoleEffect, PositionX = MapX, PositionY = MapY
            };
            msg.Strings.Add(effect);
            await Map.BroadcastRoomMsgAsync(MapX, MapY, msg, self ? 0 : Identity);
        }

        public async Task SendAsync(string message, MsgTalk.TalkChannel channel = MsgTalk.TalkChannel.TopLeft, Color? color = null)
        {
            await SendAsync(new MsgTalk(Identity, channel, color ?? Color.White, message));
        }

        public virtual async Task SendAsync(IPacket msg)
        {
            await Log.WriteLogAsync(LogLevel.Warning, $"{GetType().Name} - {Identity} has no SendAsync handler");
        }

        public virtual async Task SendSpawnToAsync(Character player)
        {
            await Log.WriteLogAsync(LogLevel.Warning, $"{GetType().Name} - {Identity} has no SendSpawnToAsync handler");
        }

        public virtual async Task BroadcastRoomMsgAsync(IPacket msg, bool self)
        {
            if (Map != null)
                await Map.BroadcastRoomMsgAsync(MapX, MapY, msg, (self ? Identity : 0));
        }

        #endregion

        #region Comparison

        public override bool Equals(object obj)
        {
            return obj is Role role && role.Identity == Identity;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Constants

        public static ulong KEEP_EFFECT_NOT_VIRTUOUS => (ulong)(StatusSet.GetFlag(StatusSet.BLUE_NAME) | StatusSet.GetFlag(StatusSet.RED_NAME) | StatusSet.GetFlag(StatusSet.BLACK_NAME));

        public const int SCENEID_FIRST = 1;
        public const int SYSNPCID_FIRST = 1;
        public const int SYSNPCID_LAST = 99999;
        public const int DYNANPCID_FIRST = 100000;
        public const int DYNANPCID_LAST = 199999;
        public const int SCENE_NPC_MIN = 200000;
        public const int SCENE_NPC_MAX = 299999;
        public const int SCENEID_LAST = 299999;

        public const int NPCSERVERID_FIRST = 400001;
        public const int MONSTERID_FIRST = 400001;
        public const int MONSTERID_LAST = 499999;
        public const int PETID_FIRST = 500001;
        public const int PETID_LAST = 599999;
        public const int NPCSERVERID_LAST = 699999;

        public const int CALLPETID_FIRST = 700001;
        public const int CALLPETID_LAST = 799999;

        public const int MAPITEM_FIRST = 800001;
        public const int MAPITEM_LAST = 899999;

        public const int TRAPID_FIRST = 900001;
        public const int MAGICTRAPID_FIRST = 900001;
        public const int MAGICTRAPID_LAST = 989999;
        public const int SYSTRAPID_FIRST = 990001;
        public const int SYSTRAPID_LAST = 999999;
        public const int TRAPID_LAST = 999999;

        public const int DYNAMAP_FIRST = 1000000;

        public const int PLAYER_ID_FIRST = 1000000;
        public const int PLAYER_ID_LAST = 1999999999;

        public const byte MAX_UPLEV = 140;

        public const int EXPBALL_AMOUNT = 600;
        public const int CHGMAP_LOCK_SECS = 10;
        public const int ADD_ENERGY_STAND_MS = 1000;
        public const int ADD_ENERGY_STAND = 3;
        public const int ADD_ENERGY_SIT = 15;
        public const int ADD_ENERGY_LIE = ADD_ENERGY_SIT / 2;
        public const int DEFAULT_USER_ENERGY = 70;
        public const int KEEP_STAND_MS = 1500;
        public const int MIN_SUPERMAP_KILLS = 25;
        public const int VETERAN_DIFF_LEVEL = 20;
        public const int HIGHEST_WATER_WIZARD_PROF = 135;
        public const int SLOWHEALLIFE_MS = 1000;
        public const int AUTOHEALLIFE_TIME = 10;
        public const int AUTOHEALLIFE_EACHPERIOD = 6;
        public const int TICK_SECS = 10;
        public const int MAX_PKLIMIT = 10000;
        public const int PILEMONEY_CHANGE = 5000;
        public const int ADDITIONALPOINT_NUM = 3;
        public const int PK_DEC_TIME = 180;
        public const int PKVALUE_DEC_ONCE = -1;
        public const int PKVALUE_DEC_ONCE_IN_PRISON = -3;
        public const int USER_ATTACK_SPEED = 1000;
        public const int POISONDAMAGE_INTERVAL = 2;
        public const int MAX_STORAGE_MONEY = int.MaxValue;

        public const int MASTER_WEAPONSKILLLEVEL = 12;
        public const int MAX_WEAPONSKILLLEVEL = 20;

        public const int MAX_MENUTASKSIZE = 8;
        public const int MAX_VAR_AMOUNT = 16;

        public const int SYNWAR_PROFFER_PERCENT = 1;
        public const int SYNWAR_MONEY_PERCENT = 2;
        public const int SYNWAR_NOMONEY_DAMAGETIMES = 10;

        public const int NPCDIEDELAY_SECS = 10;

        #endregion
    }

    public interface IRoleAi
    {
        bool IsActive { get; }
    }

    public enum FacingDirection : byte
    {
        Begin = SouthEast,
        SouthWest = 0,
        West = 1,
        NorthWest = 2,
        North = 3,
        NorthEast = 4,
        East = 5,
        SouthEast = 6,
        South = 7,
        End = South,
        Invalid = End+1
    }

    public enum EntityAction : ushort
    {
        Dance1 = 1,
        Dance2 = 2,
        Dance3 = 3,
        Dance4 = 4,
        Dance5 = 5,
        Dance6 = 6,
        Dance7 = 7,
        Dance8 = 8,
        Stand = 100,
        Happy = 150,
        Angry = 160,
        Sad = 170,
        Wave = 190,
        Bow = 200,
        Kneel = 210,
        Cool = 230,
        Sit = 250,
        Lie = 270
    }
}