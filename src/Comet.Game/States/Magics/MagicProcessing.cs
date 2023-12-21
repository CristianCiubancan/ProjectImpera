// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Magic Processing.cs
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.Mathematics;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;
using Comet.Game.States.NPCs;
using Comet.Game.World.Maps;
using Comet.Shared;

#endregion

namespace Comet.Game.States.Magics
{
    public partial class MagicData
    {
        private const int MAX_TARGET_NUM = 25;
        private const int TC_PK_ARENA_ID = 1005;

        /// <summary>
        ///     This delay is a general one, to avoid people from spamming skills in low ping scenarios.
        /// </summary>
        private readonly TimeOutMS m_tDelay = new TimeOutMS(MAGIC_DELAY);
        private TimeOutMS m_tIntone = new TimeOutMS();
        private readonly TimeOutMS m_MagicDelay = new TimeOutMS(MAGIC_DELAY);

        //private Magic m_pMagic = null;
        private ushort m_typeMagic = 0;

        private Point m_targetPos = default;
        private uint m_idTarget = 0;

        private bool m_autoAttack = false;
        private MagicState m_state = MagicState.None;

        private int m_autoAttackNum = 0;

        public MagicState State => m_state;

        public async Task<(bool Success, ushort X, ushort Y)> CheckConditionAsync(Magic magic, uint idTarget, ushort x,
            ushort y)
        {
            int delay = m_pOwner.Map.IsTrainingMap()
                ? MAGIC_DELAY
                : MAGIC_DELAY - magic.Level * MAGIC_DECDELAY_PER_LEVEL;
            if (!m_MagicDelay.IsTimeOut(delay) &&
                magic.Sort != MagicSort.Collide)
                return (false, x, y);

            if (!magic.IsReady())
                return (false, x, y);

            if (m_pOwner.Map.IsLineSkillMap() && magic.Sort != MagicSort.Line)
                return (false, x, y);

            if (!((magic.AutoActive & 1) == 1
                  || (magic.AutoActive & 4) == 4) && magic.Type != 6001)
            {
                if (!await Kernel.ChanceCalcAsync(magic.Percent))
                    return (false, x, y);
            }

            GameMap map = m_pOwner.Map;
            Character user = m_pOwner as Character;
            Role role = null;
            if (user != null && user.Map.QueryRegion(RegionTypes.PkProtected, user.MapX, user.MapY))
            {
                if (magic.Ground > 0)
                {
                    if (magic.Crime > 0)
                        return (false, x, y);
                }
                else
                {
                    role = map.QueryAroundRole(user, idTarget);
                    if (role is Character && magic.Crime > 0)
                        return (false, x, y);
                }
            }

            if (map.IsLineSkillMap() && magic.Sort != MagicSort.Line)
                return (false, x, y);

            if (user?.CurrentEvent != null && !user.CurrentEvent.IsAttackEnable(user) && magic.Sort != MagicSort.Attachstatus)
                return (false, x, y);

            if (!map.IsTrainingMap() && user != null)
            {
                if (user.Mana < magic.UseMana)
                    return (false, x, y);
                if (user.Energy < magic.UseStamina)
                    return (false, x, y);

                if (magic.UseItem > 0 && user.CheckWeaponSubType(magic.UseItem, magic.UseItemNum))
                    return (false, x, y);
            }

            if (magic.UseXp == BattleSystem.MagicType.Normal)
            {
                IStatus pStatus = m_pOwner.QueryStatus(StatusSet.START_XP);
                if (pStatus == null && magic.Status != StatusSet.VORTEX)
                    return (false, x, y);
            }

            if (magic.WeaponSubtype > 0 && user != null)
            {
                if (!user.CheckWeaponSubType(magic.WeaponSubtype))
                    return (false, x, y);

                if (magic.Type == TWOFOLDBLADES_ID 
                    && user.UserPackage[Item.ItemPosition.RightHand].GetItemSubType() != user.UserPackage[Item.ItemPosition.LeftHand]?.GetItemSubType())
                    return (false, x, y);

            }

            if (user != null && user.TransformationMesh != 0)
                return (false, x, y);

            if (m_pOwner.IsWing && magic.Sort == MagicSort.Transform)
                return (false, x, y);

            if (map.IsWingDisable() && magic.Sort == MagicSort.Attachstatus && magic.Status == StatusSet.FLY)
                return (false, x, y);
            
            if (magic.Ground == 0 && magic.Sort != MagicSort.Groundsting
                                  && magic.Sort != MagicSort.Vortex
                                  && magic.Sort != MagicSort.Dashwhirl
                                  && magic.Sort != MagicSort.Dashdeadmark
                                  && magic.Sort != MagicSort.Mountwhirl)
            {
                role = map.QueryAroundRole(m_pOwner, idTarget);
                if (role == null)
                    return (false, x, y);

                if (!role.IsAlive
                    && magic.Sort != MagicSort.Attachstatus
                    && magic.Sort != MagicSort.Detachstatus)
                    return (false, x, y);

                if (magic.Sort == MagicSort.Declife)
                {
                    if (role.Life * 100 / role.MaxLife >= 15)
                        return (false, x, y);
                }

                x = role.MapX;
                y = role.MapY;
            }

            if (HitByMagic(magic) != 0 && !HitByWeapon() && m_pOwner.GetDistance(x, y) > magic.Distance + 1)
                return (false, x, y);

            if (m_pOwner.GetDistance(x, y) > m_pOwner.GetAttackRange(0) + magic.Distance + 1)
                return (false, x, y);

            if (role is DynamicNpc dyna)
            {
                if (dyna.IsGoal() && m_pOwner.Level < dyna.Level)
                    return (false, x, y);
            }

            return (true, x, y);
        }

        public async Task<bool> ProcessMagicAttackAsync(ushort usMagicType, uint idTarget, ushort x, ushort y,
            uint ucAutoActive = 0)
        {
            switch (m_state)
            {
                case MagicState.Intone:
                    await AbortMagicAsync(true);
                    break;
            }

            m_state = MagicState.None;
            m_typeMagic = usMagicType;

            if (!Magics.TryGetValue(usMagicType, out Magic magic)
                && (ucAutoActive == 0 || (magic?.AutoActive ?? 0 & ucAutoActive) != 0))
            {
                await Log.GmLogAsync("cheat", $"invalid magic type: {usMagicType}, user[{m_pOwner.Name}][{m_pOwner.Identity}]");
                return false;
            }

            if (magic == null)
                return false;

            var result = await CheckConditionAsync(magic, idTarget, x, y);
            if (!result.Success)
            {
                if (magic.Sort == MagicSort.Collide)
                    await ProcessCollideFailAsync(x, y, (int) idTarget);

                await AbortMagicAsync(true);
                return false;
            }

            /*if (magic.Ground > 0 && magic.Sort != MagicSort.Atkstatus)
                m_idTarget = 0;
            else*/
                m_idTarget = idTarget;

            m_targetPos = new Point(x, y);

            Character user = m_pOwner as Character;
            GameMap map = m_pOwner.Map;
            if (user != null  && !map.IsTrainingMap() && map.Identity != TC_PK_ARENA_ID)
            {
                if (magic.UseMana > 0)
                    await user.AddAttributesAsync(ClientUpdateType.Mana, magic.UseMana * -1);
                if (magic.UseStamina > 0)
                    await user.AddAttributesAsync(ClientUpdateType.Stamina, magic.UseStamina * -1);
                if (magic.UseItem > 0)
                    await user.SpendEquipItemAsync(magic.UseItem, Math.Max(magic.UseItemNum, 1), true);
            }

            if (magic.UseXp == BattleSystem.MagicType.Normal && user != null)
            {
                if (magic.Status == StatusSet.VORTEX && m_pOwner.QueryStatus(StatusSet.VORTEX) != null)
                {

                }
                else
                {
                    IStatus pStatus = m_pOwner.QueryStatus(StatusSet.START_XP);
                    if (pStatus == null)
                    {
                        await AbortMagicAsync(true);
                        return false;
                    }

                    await user.DetachStatusAsync(StatusSet.START_XP);
                    await user.ClsXpValAsync();
                }
            }

            if (!IsWeaponMagic(magic.Type))
            {
                await m_pOwner.BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.MagicAttack,
                    TargetIdentity = idTarget,
                    SenderIdentity = m_pOwner.Identity,
                    PosX = m_pOwner.MapX,
                    PosY = m_pOwner.MapY
                }, true);
            }

            m_typeMagic = magic.Type; // for auto attack!

            if (magic.UseMana != 0)
            {
                if (!map.IsTrainingMap() && user != null)
                    await user.DecEquipmentDurabilityAsync(false, (int) HitByMagic(magic), (ushort)magic.UseItemNum);

                if (await Kernel.ChanceCalcAsync(7) && user != null)
                    await user.SendGemEffectAsync();
            }

            m_MagicDelay.Update();
            await m_pOwner.ProcessOnAttackAsync();

            if (magic.IntoneSpeed <= 0)
            {
                if (!await LaunchAsync(magic)) // pode ocorrer caso o monstro desapareça, morra antes da hora
                {
                    ResetDelay();
                }
                else
                {
                    if (m_pOwner.Map.IsTrainingMap() || IsAutoAttack())
                    {
                        SetAutoAttack(magic.Type);
                        m_tDelay.Startup(Math.Max(MAGIC_DELAY, magic.DelayMs));
                        m_state = MagicState.Delay;
                        return true;
                    }

                    m_state = MagicState.None;
                }
            }
            else
            {
                m_state = MagicState.Intone;
                m_tIntone.Startup((int) magic.IntoneSpeed);
            }

            return true;
        }

        #region Processing

        private async Task<bool> LaunchAsync(Magic magic)
        {
            bool result = false;
            try
            {
                if (magic == null)
                    return false;

                if (!m_pOwner.IsAlive) 
                    return false;

                magic.Use();

                switch (magic.Sort)
                {
                    case MagicSort.Attack:
                        result = await ProcessAttackAsync(magic);
                        break;
                    case MagicSort.Recruit:
                        result = await ProcessRecruitAsync(magic);
                        break;
                    case MagicSort.Fan:
                        result = await ProcessFanAsync(magic);
                        break;
                    case MagicSort.Bomb:
                        result = await ProcessBombAsync(magic);
                        break;
                    case MagicSort.Attachstatus:
                        result = await ProcessAttachAsync(magic);
                        break;
                    case MagicSort.Detachstatus:
                        result = await ProcessDetachAsync(magic);
                        break;
                    case MagicSort.Dispatchxp:
                        result = await ProcessDispatchXpAsync(magic);
                        break;
                    case MagicSort.Line:
                        result = await ProcessLineAsync(magic);
                        break;
                    case MagicSort.Atkstatus:
                        result = await ProcessAttackStatusAsync(magic);
                        break;
                    case MagicSort.Transform:
                        result = await ProcessTransformAsync(magic);
                        break;
                    case MagicSort.Addmana:
                        result = await ProcessAddManaAsync(magic);
                        break;
                    case MagicSort.Groundsting:
                        result = await ProcessGroundstingAsync(magic);
                        break;
                    case MagicSort.Vortex:
                        result = await ProcessVortexAsync(magic);
                        break;
                    case MagicSort.Activateswitch:
                        result = await ProcessActivateSwitchAsync(magic);
                        break;
                    case MagicSort.Riding:
                        result = await ProcessRidingAsync(magic);
                        break;

                    default:
                        await Log.WriteLogAsync(LogLevel.Warning, $"MagicProcessing::LaunchAsync {magic.Sort} not handled!!!");
                        result = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Error ocurred on MagicProcessing::LaunchAsync");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            m_autoAttackNum++;
            return result;
        }
        
        private async Task<bool> ProcessAttackAsync(Magic magic)
        {
            if (magic == null || m_pOwner == null || m_idTarget == 0)
                return false;

            Role targetRole = m_pOwner.Map.QueryAroundRole(m_pOwner, m_idTarget);
            if (targetRole == null
                || m_pOwner.GetDistance(targetRole) > magic.Distance
                || !targetRole.IsAlive
                || !targetRole.IsAttackable(m_pOwner))
                return false;

            if (m_pOwner.IsImmunity(targetRole))
                return false;

            if (magic.FloorAttr > 0)
            {
                int nAttr = targetRole.Map[targetRole.MapX, targetRole.MapY].Elevation;
                if (nAttr != magic.FloorAttr)
                    return false;
            }

            var byMagic = HitByMagic(magic);
            var result = await m_pOwner.BattleSystem.CalcPowerAsync(byMagic, m_pOwner, targetRole, magic.Power);
            int power = result.Damage;

            Character user = m_pOwner as Character;
            if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
            {
                await user.SendEffectAsync("LuckyGuy", true);
                power *= 2;
            }

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = m_pOwner.MapX,
                MapY = m_pOwner.MapY
            };
            msg.Append(targetRole.Identity, power, true);
            await m_pOwner.BroadcastRoomMsgAsync(msg, true);

            await CheckCrimeAsync(targetRole, magic);

            int totalExp = 0;
            if (power > 0 && !await targetRole.CheckScapegoatAsync(m_pOwner))
            {
                int lifeLost = (int) Math.Min(targetRole.MaxLife, power);
                await targetRole.BeAttackAsync(byMagic, m_pOwner, power, true);
                totalExp = lifeLost;

                if (user != null && targetRole is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                {
                    dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);
                }

                if (user?.CurrentEvent != null)
                    await user.CurrentEvent.OnHitAsync(user, targetRole, magic);
            }

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            if (totalExp > 0)
            {
                await AwardExpOfLifeAsync(targetRole, totalExp, magic);
            }

            if (!targetRole.IsAlive)
            {
                int nBonusExp = (int)(targetRole.MaxLife * 20 / 100);
                await m_pOwner.BattleSystem.OtherMemberAwardExpAsync(targetRole, nBonusExp);
                await m_pOwner.KillAsync(targetRole, GetDieMode());
            }
            else
            {
                if (((targetRole is Monster monster && !monster.IsGuard())
                    || targetRole is DynamicNpc) 
                    && byMagic == BattleSystem.MagicType.Normal)
                {
                    SetAutoAttack(magic.Type);
                }
            }
            
            if (user != null)
                await user.SendWeaponMagic2Async(targetRole);

            return true;
        }

        private async Task<bool> ProcessRecruitAsync(Magic magic)
        {
            if (magic == null || m_pOwner == null || m_idTarget == 0)
                return false;

            List<Role> setTarget = new List<Role>();
            // todo team
            Role targetRole = m_pOwner.Map.QueryAroundRole(m_pOwner, m_idTarget);
            if (targetRole == null
                || m_pOwner.GetDistance(targetRole) > magic.Distance
                || !targetRole.IsAlive)
                return false;

            setTarget.Add(targetRole);

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = m_pOwner.MapX,
                MapY = m_pOwner.MapY
            };

            int exp = 0;
            foreach (var target in setTarget)
            {
                if (!target.IsAlive)
                    continue;

                int power = (int) Math.Min(magic.Power, target.MaxLife - target.Life);
                if (power == Calculations.ADJUST_FULL)
                    power = (int) (target.MaxLife - target.Life);

                exp += power;

                msg.Append(target.Identity, power, false);

                if (power > 0)
                {
                    await target.AddAttributesAsync(ClientUpdateType.Hitpoints, power);

                    if (target is Character user)
                        await user.BroadcastTeamLifeAsync();
                }
            }

            if (targetRole.Map.IsTrainingMap())
                exp = Math.Max(exp, magic.Power);

            await m_pOwner.BroadcastRoomMsgAsync(msg, true);
            await AwardExpAsync(0, exp, true);
            return true;
        }

        private async Task<bool> ProcessFanAsync(Magic magic)
        {
            int nRange = (int)magic.Distance + 2;
            const int WIDTH = DEFAULT_MAGIC_FAN;
            long nExp = 0, battleExp = 0;

            List<Role> setTarget = new List<Role>();
            Point center = new Point(m_pOwner.MapX, m_pOwner.MapY);
            
            Role tgt = m_pOwner.Map.QueryAroundRole(m_pOwner, m_idTarget);
            if (tgt != null && tgt.IsAlive)
                setTarget.Add(tgt);

            var targets = m_pOwner.Map.Query9BlocksByPos(m_pOwner.MapX, m_pOwner.MapY);
            foreach (var target in targets)
            {
                if (target.Identity == m_pOwner.Identity)
                    continue;

                Point posThis = new Point(target.MapX, target.MapY);
                if (!ScreenCalculations.IsInFan(center, m_targetPos, posThis, WIDTH, nRange))
                    continue;

                if (target.IsAttackable(m_pOwner)
                    && !m_pOwner.IsImmunity(target)
                    && target.Identity != m_idTarget)
                    setTarget.Add(target);
            }

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = (ushort) m_targetPos.X,
                MapY = (ushort) m_targetPos.Y,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };

            Character user = m_pOwner as Character;
            var byMagic = HitByMagic(magic);
            bool bMagic2Dealt = false;
            foreach (var target in setTarget)
            {
                if (!target.IsAttackable(m_pOwner)
                    || m_pOwner.IsImmunity(target))
                    continue;

                var (damage, effect) = await m_pOwner.BattleSystem.CalcPowerAsync(byMagic, m_pOwner, target, magic.Power);
                if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 250))
                {
                    await user.SendEffectAsync("LuckyGuy", true);
                    damage *= 2;
                }

                if (msg.Count >= MAX_TARGET_NUM)
                {
                    await m_pOwner.BroadcastRoomMsgAsync(msg, true);
                    msg.ClearTargets();
                }

                msg.Append(target.Identity, damage, true);

                if (!await target.CheckScapegoatAsync(m_pOwner))
                {
                    int lifeLost = (int) Math.Min(target.Life, damage);
                    await target.BeAttackAsync(byMagic, m_pOwner, lifeLost, true);

                    if (user != null && target is Monster monster)
                    {
                        nExp += lifeLost;
                        battleExp += user.AdjustExperience(monster, lifeLost, false);
                        if (!monster.IsAlive)
                        {
                            int nBonusExp = (int) (monster.MaxLife * 20 / 100d);

                            if (user.Team != null)
                                await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);

                            nExp += user.AdjustExperience(monster, nBonusExp, false);
                        }
                    }
                    
                    if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                    {
                        dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);
                    }

                    if (user?.CurrentEvent != null)
                        await user.CurrentEvent.OnHitAsync(user, target, magic);

                    if (!target.IsAlive)
                        await m_pOwner.KillAsync(target, GetDieMode());
                }

                if (!bMagic2Dealt && await Kernel.ChanceCalcAsync(5d) && user != null)
                {
                    await user.SendWeaponMagic2Async(target);
                    bMagic2Dealt = true;
                }
            }

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            await m_pOwner.BroadcastRoomMsgAsync(msg, true);
            await CheckCrimeAsync(setTarget.ToDictionary(x => x.Identity), magic);
            await AwardExpAsync(battleExp, nExp, false, magic);
            return true;
        }

        private async Task<bool> ProcessBombAsync(Magic magic)
        {
            if (magic == null || m_pOwner == null)
                return false;

            List<Role> setTarget = new List<Role>();

            var result = CollectTargetBomb(0, (int) magic.Range);

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = (ushort) result.Center.X,
                MapY = (ushort) result.Center.Y,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };

            long battleExp = 0;
            long exp = 0;
            Character user = m_pOwner as Character;
            foreach (var target in result.Roles)
            {
                if (magic.Ground != 0 && target.IsWing)
                    continue;

                var atkResult = await m_pOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), m_pOwner, target, magic.Power);

                if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
                {
                    await user.SendEffectAsync("LuckyGuy", true);
                    atkResult.Damage *= 2;
                }

                if (!await target.CheckScapegoatAsync(m_pOwner))
                {
                    int lifeLost = (int) Math.Min(atkResult.Damage, target.Life);

                    await target.BeAttackAsync(HitByMagic(magic), m_pOwner, atkResult.Damage, true);

                    if (user != null && target is Monster monster)
                    {
                        exp += lifeLost;
                        battleExp += user.AdjustExperience(target, lifeLost, false);
                        if (!monster.IsAlive)
                        {
                            int nBonusExp = (int) (monster.MaxLife * 20 / 100d);
                            if (user.Team != null)
                                await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);
                            battleExp += user.AdjustExperience(monster, nBonusExp, false);
                        }
                    }

                    if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                    {
                        dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);
                    }

                    if (user?.CurrentEvent != null)
                        await user.CurrentEvent.OnHitAsync(user, target, magic);

                    if (!target.IsAlive)
                        await m_pOwner.KillAsync(target, GetDieMode());
                }

                if (msg.Count < MAX_TARGET_NUM)
                    msg.Append(target.Identity, atkResult.Damage, true);
            }
            
            await m_pOwner.Map.BroadcastRoomMsgAsync(result.Center.X, result.Center.Y, msg);

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            await CheckCrimeAsync(result.Roles.ToDictionary(x => x.Identity, x => x), magic);
            await AwardExpAsync(0, battleExp, exp, magic);
            return true;
        }

        private async Task<bool> ProcessAttachAsync(Magic magic)
        {
            if (magic == null)
                return false;

            var target = m_pOwner.Map.QueryRole(m_idTarget);
            if (target == null)
                return false;

            /*
             * 64 can only be used on dead players
             */
            if (!target.IsAlive && magic.Target != 64 && !(target is Character))
                return false;

            int power = magic.Power;
            int secs = (int) magic.StepSeconds;
            int times = (int) magic.ActiveTimes;
            int status = (int) magic.Status;
            byte level = (byte) magic.Level;

            if (power < 0)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Error magic type invalid power {magic.Type} {magic.Power}");
                return false;
            }

            if (secs <= 0)
                secs = int.MaxValue;

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = m_pOwner.MapX,
                MapY = m_pOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };

            int damage = 1;
            switch (status)
            {
                case StatusSet.FLY:
                {
                    if (target.Identity != m_pOwner.Identity)
                        return false;
                    if (!target.IsBowman || !target.IsAlive)
                        return false;
                    if (target.Map.IsWingDisable())
                        return false;
                    if (target.QueryStatus(StatusSet.SHIELD) != null)
                        return false;
                    if (target.QueryStatus(StatusSet.RIDING) != null)
                        return false;
                    break;
                }

                case StatusSet.LUCKY_DIFFUSE:
                {
                    damage = 0;
                    break;
                }

                case StatusSet.POISON_STAR:
                {
                    int chance = 100 - Math.Min(20, Math.Max(0, target.BattlePower - m_pOwner.BattlePower)) * 5;
                    if (!await Kernel.ChanceCalcAsync(chance))
                    {
                        msg.Append(target.Identity, 0, false);
                        await m_pOwner.BroadcastRoomMsgAsync(msg, true);
                        return true;
                    }
                    break;
                }
            }
            
            msg.Append(target.Identity, damage, damage != 0);
            await m_pOwner.BroadcastRoomMsgAsync(msg, true);

            await CheckCrimeAsync(target, magic);

            await target.AttachStatusAsync(m_pOwner, status, power, secs, times, level);

            if (power >= Calculations.ADJUST_PERCENT)
            {
                var powerTimes = (power - 30000) - 100;
                switch (status)
                {
                    case StatusSet.STAR_OF_ACCURACY:
                        await target.SendAsync(string.Format(Language.StrAccuracyActiveP, secs, powerTimes));
                        break;
                    case StatusSet.DODGE:
                        await target.SendAsync(string.Format(Language.StrDodgeActiveP, secs, powerTimes));
                        break;
                    case StatusSet.STIG:
                        await target.SendAsync(string.Format(Language.StrStigmaActiveP, secs, powerTimes));
                        break;
                    case StatusSet.SHIELD:
                        await target.SendAsync(string.Format(Language.StrShieldActiveP, secs, powerTimes));
                        break;
                }
            }
            else
            {
                switch (status)
                {
                    case StatusSet.STAR_OF_ACCURACY:
                        await target.SendAsync(string.Format(Language.StrAccuracyActiveT, secs, power));
                        break;
                    case StatusSet.DODGE:
                        await target.SendAsync(string.Format(Language.StrDodgeActiveT, secs, power));
                        break;
                    case StatusSet.STIG:
                        await target.SendAsync(string.Format(Language.StrStigmaActiveT, secs, power));
                        break;
                    case StatusSet.SHIELD:
                        await target.SendAsync(string.Format(Language.StrShieldActiveT, secs, power));
                        break;
                }
            }

            if (m_pOwner is Character)
                await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessDetachAsync(Magic magic)
        {
            if (magic == null) return false;

            Role target = m_pOwner.Map.QueryRole(m_idTarget);
            if (target == null)
                return false;

            int power = magic.Power;
            int secs = (int)magic.StepSeconds;
            int times = (int)magic.ActiveTimes;
            int status = (int)magic.Status;
            byte level = (byte)magic.Level;

            if (!target.IsAlive && target.IsPlayer())
            {
                if (status != -1)
                    return false;

                if (target.Map.IsPkField())
                    return false;
            }

            if (status == -1 && target is Character user)
            {
                await user.RebornAsync(false, true);
                await user.Map.SendMapInfoAsync(user);
            }
            else
            {
                // todo handle any other skill
            }

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = m_pOwner.MapX,
                MapY = m_pOwner.MapY,
                MagicIdentity = (ushort)magic.Type,
                MagicLevel = magic.Level
            };

            switch (magic.Status)
            {
                case StatusSet.FLY:
                {
                    if (!target.IsWing)
                        return true;

                    int chance = 100 - Math.Min(20, Math.Max(0, target.BattlePower - m_pOwner.BattlePower)) * 5;
                    if (!await Kernel.ChanceCalcAsync(chance))
                    {
                        msg.Append(target.Identity, 0, false);
                        await m_pOwner.BroadcastRoomMsgAsync(msg, true);
                        return true;
                    }
                    break;
                }
            }

            msg.Append(target.Identity, power, true);
            await target.BroadcastRoomMsgAsync(msg, true);

            if (power > 0)
            {
                int lifeLost = (int) Math.Min(target.Life, Math.Max(0, Calculations.AdjustData(target.Life, power)));
                await target.BeAttackAsync(HitByMagic(magic), m_pOwner, lifeLost, true);
                await target.AddAttributesAsync(ClientUpdateType.Hitpoints, lifeLost * -1);
            }

            await target.DetachStatusAsync((int) magic.Status);
            return true;
        }

        private async Task<bool> ProcessDispatchXpAsync(Magic magic)
        {
            if (magic == null)
                return false;

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = m_pOwner.MapX,
                MapY = m_pOwner.MapY,
                MagicIdentity = (ushort)magic.Type,
                MagicLevel = magic.Level
            };
            if (m_pOwner is Character user && user.Team != null)
            {
                foreach (Character member in user.Team.Members.Where(x => x.Identity != user.Identity && x.IsAlive))
                {
                    if (m_pOwner.GetDistance(member) > Screen.VIEW_SIZE * 2)
                        continue;

                    msg.Append(member.Identity, DISPATCHXP_NUMBER, true);
                    await member.SetXpAsync(DISPATCHXP_NUMBER);
                    await member.BurstXpAsync();
                    await member.SendAsync(string.Format(Language.StrDispatchXp, user.Name));
                }
            }

            await m_pOwner.BroadcastRoomMsgAsync(msg, true);
            await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessLineAsync(Magic magic)
        {
            if (magic == null || m_pOwner == null)
                return false;

            var allTargets = m_pOwner.Map.Query9BlocksByPos(m_pOwner.MapX, m_pOwner.MapY);
            List<Role> targets = new List<Role>();
            List<Point> setPoint = new List<Point>();
            var pos = new Point(m_pOwner.MapX, m_pOwner.MapY);
            ScreenCalculations.DDALine(pos.X, pos.Y, m_targetPos.X, m_targetPos.Y, (int)magic.Range, ref setPoint);

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = (ushort)m_targetPos.X,
                MapY = (ushort)m_targetPos.Y,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            long exp = 0;
            long battleExp = 0;
            Character user = m_pOwner as Character;

            Tile userTile = m_pOwner.Map[m_pOwner.MapX, m_pOwner.MapY];
            foreach (var point in setPoint)
            {
                if (msg.Count >= MAX_TARGET_NUM)
                {
                    await m_pOwner.BroadcastRoomMsgAsync(msg, true);
                    msg.ClearTargets();
                }

                Tile targetTile = m_pOwner.Map[point.X, point.Y];
                if (userTile.Elevation - targetTile.Elevation > 26)
                    continue;
                
                Role target = allTargets.FirstOrDefault(x => x.MapX == point.X && x.MapY == point.Y);
                if (target == null || target.Identity == m_pOwner.Identity)
                    continue;

                if (m_pOwner.IsImmunity(target)
                    || !target.IsAttackable(m_pOwner))
                    continue;

                if (magic.Ground != 0 && target.IsWing)
                    continue;

                var result = await m_pOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), m_pOwner, target, magic.Power);

                if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
                {
                    await user.SendEffectAsync("LuckyGuy", true);
                    result.Damage *= 2;
                }

                if (!await target.CheckScapegoatAsync(m_pOwner))
                {
                    int lifeLost = (int) Math.Min(result.Damage, target.Life);

                    await target.BeAttackAsync(HitByMagic(magic), m_pOwner, result.Damage, true);

                    if (user != null && (target is Monster monster || (target is DynamicNpc npc && npc.IsGoal())))
                    {
                        exp += lifeLost;
                        battleExp += user.AdjustExperience(target, lifeLost, false);
                        if (!target.IsAlive)
                        {
                            int nBonusExp = (int) (target.MaxLife * 20 / 100d);
                            if (user.Team != null)
                                await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);
                            battleExp += user.AdjustExperience(target, nBonusExp, false);
                        }
                    }

                    if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                    {
                        dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);
                    }

                    if (user?.CurrentEvent != null)
                        await user.CurrentEvent.OnHitAsync(user, target, magic);

                    if (!target.IsAlive)
                        await m_pOwner.KillAsync(target, GetDieMode());
                }

                msg.Append(target.Identity, result.Damage, true);
                targets.Add(target);
            }

            await m_pOwner.BroadcastRoomMsgAsync(msg, true);

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            await CheckCrimeAsync(targets.ToDictionary(x => x.Identity, x => x), magic);
            await AwardExpAsync(0, battleExp, exp, magic);
            return true;
        }

        private async Task<bool> ProcessAttackStatusAsync(Magic magic)
        {
            if (magic == null)
                return false;

            Role target = m_pOwner.Map.QueryRole(m_idTarget);
            if (target == null)
                return false;

            if (target.MapIdentity != m_pOwner.MapIdentity
                || m_pOwner.GetDistance(target) > magic.Distance + target.SizeAddition)
                return false;

            if (!target.IsAttackable(m_pOwner) || m_pOwner.IsImmunity(target))
                return false;

            if (magic.Ground != 0 && target.IsWing)
                return false;

            int power = 0;
            InteractionEffect effect = InteractionEffect.None;

            if (HitByWeapon())
            {
                switch (magic.Status)
                {
                    case 0:
                        break;
                    default:
                        var result = await m_pOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), m_pOwner, target, magic.Power);
                        power = result.Damage;
                        effect = result.effect;
                        break;
                }
            }
            
            Character user = m_pOwner as Character;
            if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
            {
                await user.SendEffectAsync("LuckyGuy", true);
                power *= 2;
            }

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = m_pOwner.MapX,
                MapY = m_pOwner.MapY,
                MagicIdentity = (ushort) magic.Type,
                MagicLevel = magic.Level
            };
            msg.Append(target.Identity, power, true);
            await m_pOwner.BroadcastRoomMsgAsync(msg, true);

            long battleExp = 0;
            if (power > 0 && !await target.CheckScapegoatAsync(m_pOwner))
            {
                int lifeLost = (int) Math.Max(0, Math.Min(target.Life, power));

                await target.BeAttackAsync(HitByMagic(magic), m_pOwner, power, true);

                if (user != null && target is Monster monster)
                {
                    battleExp += user.AdjustExperience(target, lifeLost, false);
                    if (!monster.IsAlive)
                    {
                        int nBonusExp = (int)(monster.MaxLife * 20 / 100d);

                        if (user.Team != null)
                            await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);

                        battleExp += user.AdjustExperience(monster, nBonusExp, false);
                    }
                }

                if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                {
                    dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);
                }

                if (user?.CurrentEvent != null)
                    await user.CurrentEvent.OnHitAsync(user, target, magic);
            }

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            await AwardExpAsync(0, battleExp, AWARDEXP_BY_TIMES, magic);

            if (!target.IsAlive)
                await m_pOwner.KillAsync(target, GetDieMode());

            return true;
        }

        private async Task<bool> ProcessTransformAsync(Magic magic)
        {
            if (magic == null || m_pOwner == null || !(m_pOwner is Character user))
                return false;

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = m_pOwner.MapX,
                MapY = m_pOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            await m_pOwner.BroadcastRoomMsgAsync(msg, true);
            await user.TransformAsync((uint) magic.Power, (int) magic.StepSeconds, true);
            await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessAddManaAsync(Magic magic)
        {
            if (magic == null)
                return false;

            Role target = null;
            if (magic.Target == 2) // self
            {
                target = m_pOwner;
            }
            else if (magic.Target == 1) // target
            {
                target = m_pOwner.Map.QueryRole(m_idTarget);
            }
            else // unhandled
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Add mana unhandled target {magic.Target}");
                return false;
            }

            if (target.Identity != m_pOwner.Identity
                && (target.MapIdentity != m_pOwner.MapIdentity || m_pOwner.GetDistance(target) > magic.Distance))
                return false;

            int addMana = (int) Math.Max(0, Math.Min(target.MaxMana - target.Mana, magic.Power));

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MapX = m_pOwner.MapX,
                MapY = m_pOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            msg.Append(target.Identity, addMana, true);
            await target.BroadcastRoomMsgAsync(msg, true);

            await target.AddAttributesAsync(ClientUpdateType.Mana, addMana);

            await AwardExpAsync(0, 0, Math.Max(addMana, AWARDEXP_BY_TIMES), magic);
            return true;
        }

        private async Task<bool> ProcessGroundstingAsync(Magic magic)
        {
            if (magic == null || magic.Status == 0)
                return false;

            var targetLocked = CollectTargetBomb(0, (int) magic.Range);
            var center = targetLocked.Center;
            var msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = (ushort) center.X,
                MapY = (ushort) center.Y
            };

            bool msgSent = false;
            List<Role> setTarget = new List<Role>();
            foreach (var target in targetLocked.Roles)
            {
                if (magic.Ground != 0 && target.IsWing)
                    continue;

                if (m_pOwner.GetDistance(target) > magic.Distance)
                    continue;

                if (!target.IsPlayer() && !target.IsMonster())
                    continue;

                if (target is Monster targetMonster)
                {
                    if (targetMonster.IsGuard())
                        continue;
                }

                int chance = 100 - Math.Min(20, Math.Max(0, target.BattlePower - m_pOwner.BattlePower)) * 5;
                int damage = 1;
                if (!await Kernel.ChanceCalcAsync(chance))
                    damage = 0;

                if (msg.Count >= 25)
                {
                    await m_pOwner.Map.BroadcastRoomMsgAsync(center.X, center.Y, msg);
                    msg = new MsgMagicEffect
                    {
                        AttackerIdentity = m_pOwner.Identity,
                        MagicIdentity = magic.Type,
                        MagicLevel = magic.Level,
                        MapX = (ushort)center.X,
                        MapY = (ushort)center.Y
                    };
                    msgSent = true;
                }

                msg.Append(target.Identity, damage, true);

                if (damage > 0)
                    setTarget.Add(target);
            }

            if (msg.Count > 0 || !msgSent)
                await m_pOwner.Map.BroadcastRoomMsgAsync(center.X, center.Y, msg);

            foreach (var target in setTarget)
                await target.AttachStatusAsync(m_pOwner, (int) magic.Status, magic.Power, (int) magic.StepSeconds, (int) magic.ActiveTimes, (byte) magic.Level);

            await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessVortexAsync(Magic magic)
        {
            if (!m_pOwner.IsAlive)
                return false;

            if (m_pOwner.IsWing)
                return false;

            if (m_pOwner.QueryStatus(StatusSet.VORTEX) == null)
            {
                await m_pOwner.AttachStatusAsync(m_pOwner, (int) magic.Status, magic.Power, (int) magic.StepSeconds, (int) magic.ActiveTimes, (byte) magic.Level);
            }
            else
            {
                var result = CollectTargetBomb(0, (int) magic.Range);

                MsgMagicEffect msg = new MsgMagicEffect
                {
                    AttackerIdentity = m_pOwner.Identity,
                    MapX = (ushort)result.Center.X,
                    MapY = (ushort)result.Center.Y,
                    MagicIdentity = magic.Type,
                    MagicLevel = magic.Level
                };

                long battleExp = 0;
                long exp = 0;
                Character user = m_pOwner as Character;
                foreach (var target in result.Roles)
                {
                    if (magic.Ground != 0 && target.IsWing)
                        continue;

                    var atkResult = await m_pOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), m_pOwner, target, magic.Power);

                    if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
                    {
                        await user.SendEffectAsync("LuckyGuy", true);
                        atkResult.Damage *= 2;
                    }

                    if (!await target.CheckScapegoatAsync(m_pOwner))
                    {
                        int lifeLost = (int)Math.Min(atkResult.Damage, target.Life);

                        await target.BeAttackAsync(HitByMagic(magic), m_pOwner, atkResult.Damage, true);

                        if (user != null && target is Monster monster)
                        {
                            exp += lifeLost;
                            battleExp += user.AdjustExperience(target, lifeLost, false);
                            if (!monster.IsAlive)
                            {
                                int nBonusExp = (int)(monster.MaxLife * 20 / 100d);
                                if (user.Team != null)
                                    await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);
                                battleExp += user.AdjustExperience(monster, nBonusExp, false);
                            }
                        }

                        if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                        {
                            dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);
                        }

                        if (user?.CurrentEvent != null)
                            await user.CurrentEvent.OnHitAsync(user, target, magic);

                        if (!target.IsAlive)
                            await m_pOwner.KillAsync(target, GetDieMode());
                    }

                    if (msg.Count < MAX_TARGET_NUM)
                        msg.Append(target.Identity, atkResult.Damage, true);
                }

                await m_pOwner.BroadcastRoomMsgAsync(msg, true);

                if (user?.CurrentEvent != null)
                    await user.CurrentEvent.OnAttackAsync(user);

                await CheckCrimeAsync(result.Roles.ToDictionary(x => x.Identity, x => x), magic);
                await AwardExpAsync(0, battleExp, exp, magic);
            }
            return true;
        }

        private async Task<bool> ProcessActivateSwitchAsync(Magic magic)
        {
            if (m_idTarget == 0)
                return false;

            Role target = m_pOwner.Map.QueryAroundRole(m_pOwner, m_idTarget);
            if (target == null)
                return false;

            var (damage, effect) = await m_pOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), m_pOwner, target, magic.Power);
            long battleExp = 0;

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = m_pOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = target.MapX,
                MapY = target.MapY
            };
            msg.Append(target.Identity, damage, true);
            await m_pOwner.BroadcastRoomMsgAsync(msg, true);

            if (damage > 0)
            {
                int lifeLost = (int)Math.Max(0, Math.Min(target.Life, damage));

                await target.BeAttackAsync(HitByMagic(magic), m_pOwner, damage, true);

                Character user = m_pOwner as Character;
                if (user != null && target is Monster monster)
                {
                    battleExp += user.AdjustExperience(target, lifeLost, false);
                    if (!monster.IsAlive)
                    {
                        int nBonusExp = (int)(monster.MaxLife * 20 / 100d);

                        if (user.Team != null)
                            await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);

                        battleExp += user.AdjustExperience(monster, nBonusExp, false);
                    }
                }

                if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                {
                    dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);
                }

                if (user?.CurrentEvent != null)
                    await user.CurrentEvent.OnHitAsync(user, target, magic);
            }

            if (!target.IsAlive)
                await m_pOwner.KillAsync(target, GetDieMode());

            if (battleExp > 0)
                await AwardExpAsync(0, battleExp, AWARDEXP_BY_TIMES, magic);

            return true;
        }

        private async Task<bool> ProcessRidingAsync(Magic magic)
        {
            if (!(m_pOwner is Character user))
                return false;

            Item mount = user.UserPackage[Item.ItemPosition.Steed];
            if (mount == null)
                return false;

            if (user.QueryStatus(StatusSet.RIDING) != null)
            {
                await user.DetachStatusAsync(StatusSet.RIDING);
                return true;
            }
            
            if (user.QueryStatus(StatusSet.FLY) != null)
                return false;

            if (user.Map.IsTrainingMap())
                return false;

            if (user.MapIdentity == 1036 && mount.Plus < 6)
                return false;

            await user.AttachStatusAsync(user, StatusSet.RIDING, 0, (int) magic.StepSeconds, 0, 0);
            await user.SetAttributesAsync(ClientUpdateType.Vigor, (ulong) user.MaxVigor);
            user.UpdateVigorTimer();
            return true;
        }

        private async Task<bool> ProcessCollideFailAsync(ushort x, ushort y, int nDir)
        {
            ushort nTargetX = (ushort)(x + GameMap.WalkXCoords[nDir]);
            ushort nTargetY = (ushort)(y + GameMap.WalkYCoords[nDir]);

            if (!m_pOwner.Map.IsStandEnable(nTargetX, nTargetY))
            {
                if (m_pOwner is Character owner)
                {
                    await owner.SendAsync(Language.StrInvalidMsg);
                    await Kernel.RoleManager.KickOutAsync(owner.Identity, "INVALID COORDINATES ProcessCollideFail");
                }
                return false;
            }

            MsgInteract pMsg = new MsgInteract
            {
                SenderIdentity = m_pOwner.Identity,
                TargetIdentity = 0,
                PosX = nTargetX,
                PosY = nTargetY,
                Action = MsgInteractType.Dash,
                Data = (nDir * 0x01000000)
            };

            await m_pOwner.BroadcastRoomMsgAsync(pMsg, true);
            if (m_pOwner is Character character)
            {
                await character.ProcessOnMoveAsync();
                await character.MoveTowardAsync(nDir, (int) RoleMoveMode.Collide);
            }
            return true;
        }

        #endregion

        #region Collect Targets

        private (List<Role> Roles, Point Center) CollectTargetBomb(int nLockType, int nRange)
        {
            List<Role> targets = new List<Role>();

            Point center = new Point(m_targetPos.X, m_targetPos.Y);
            if (QueryMagic?.Ground == 1)
            {
                center.X = m_pOwner.MapX;
                center.Y = m_pOwner.MapY;
            }
            else if (m_idTarget != 0)
            {
                Role target = m_pOwner.Map.QueryAroundRole(m_pOwner, m_idTarget);
                if (target != null)
                {
                    center.X = target.MapX;
                    center.Y = target.MapY;
                }
            }

            var setRoles = m_pOwner.Map.Query9BlocksByPos(center.X, center.Y);

            foreach (var target in setRoles)
            {
                if (target.Identity == m_pOwner.Identity)
                    continue;

                if (target.GetDistance(center.X, center.Y) > nRange)
                    continue;

                if (m_pOwner.IsImmunity(target) || !target.IsAttackable(m_pOwner))
                    continue;

                if (target.IsWing)
                    continue;

                targets.Add(target);
            }

            return (targets, center);
        }

        #endregion

        #region Experience

        public async Task<bool> AwardExpOfLifeAsync(Role pTarget, int nLifeLost, Magic magic = null, bool bMagicRecruit = false)
        {
            Character pOwner = m_pOwner as Character;
            if ((pTarget.IsMonster()) && pOwner != null || pTarget is DynamicNpc dynamicNpc && dynamicNpc.IsGoal()) // todo check if dynamic npc
            {
                int exp = nLifeLost;
                long battleExp = pOwner.AdjustExperience(pTarget, nLifeLost, false);

                if (!pTarget.IsAlive && !bMagicRecruit)
                {
                    int nBonusExp = (int)(pTarget.MaxLife * (5 / 100));
                    battleExp += nBonusExp;
                    if (!pOwner.Map.IsTrainingMap() && nBonusExp > 0)
                        await pOwner.SendAsync(string.Format(Language.StrKillingExperience, nBonusExp));
                }

                await AwardExpAsync(0, (int)battleExp, exp, magic);
            }
            return true;
        }

        public async Task<bool> AwardExpAsync(int nType, long nBattleExp, long nExp, Magic pMagic = null)
        {
            if (pMagic == null)
                return await AwardExpAsync(nBattleExp, nExp, true, QueryMagic);
            return await AwardExpAsync(nBattleExp, nExp, true, pMagic);
        }

        public async Task<bool> AwardExpAsync(long nBattleExp, long nExp, bool bIgnoreFlag, Magic pMagic = null)
        {
            if (nBattleExp <= 0 && nExp == 0) return false;

            pMagic ??= QueryMagic;

            if (m_pOwner.Map.IsTrainingMap())
            {
                if (nBattleExp > 0)
                {
                    if (m_pOwner.IsBowman)
                        nBattleExp /= 2;
                    nBattleExp = Calculations.CutTrail(1, Calculations.MulDiv(nBattleExp, 10, 100));
                }
            }

            if (nBattleExp > 0 && m_pOwner is Character user)
                await user.AwardBattleExpAsync(nBattleExp, true);

            if (pMagic == null)
                return false;

            if (!CheckAwardExpEnable(pMagic))
                return false;

            if (m_pOwner.Map.Identity == TC_PK_ARENA_ID)
                return true;

            if (m_pOwner.Map.IsTrainingMap() && pMagic.AutoActive == 0 && m_autoAttackNum > 0 && m_autoAttackNum % 10 != 0)
                return true;

            /*if (pMagic.AutoActive == 0 && m_pOwner.Map.IsTrainingMap())
                nExp = Math.Max(AWARDEXP_BY_TIMES, nExp / 3);*/

            if (pMagic.NeedExp > 0
                && (pMagic.AutoActive & 16) == 0
                || bIgnoreFlag)
            {
                if (m_pOwner is Character owner)
                    nExp = (int)(nExp * ((1 + (owner.MoonGemBonus / 100d))));

                pMagic.Experience += (uint)nExp;

                //if ((pMagic.AutoActive & 8) == 0)
                    await pMagic.FlushAsync();

                await UpLevelMagic(true, pMagic);
                await pMagic.SaveAsync();
                return true;
            }

            if (pMagic.NeedExp == 0
                && pMagic.Target == 4)
            {
                if (m_pOwner is Character owner)
                    nExp = (int)(nExp * ((1 + (owner.MoonGemBonus / 100d))));

                pMagic.Experience += (uint)nExp;

                //if ((pMagic.AutoActive & 8) == 0)
                    await pMagic.FlushAsync();
                await UpLevelMagic(true, pMagic);

                await pMagic.SaveAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> UpLevelMagic(bool synchro, Magic pMagic)
        {
            if (pMagic == null)
                return false;
            
            int nNeedExp = pMagic.NeedExp;

            if (!(nNeedExp > 0
                  && (pMagic.Experience >= nNeedExp
                      || pMagic.OldLevel > 0
                      && pMagic.Level >= pMagic.OldLevel / 2
                      && pMagic.Level < pMagic.OldLevel)))
                return false;

            ushort nNewLevel = (ushort)(pMagic.Level + 1);
            pMagic.Experience = 0;
            pMagic.Level = nNewLevel;
            if (synchro)
                await pMagic.SendAsync();
            return true;
        }

        public bool CheckAwardExpEnable(Magic magic)
        {
            if (magic == null)
                return false;
            return m_pOwner.Level >= magic.NeedLevel
                   && magic.NeedExp > 0
                   && m_pOwner.MapIdentity != 1005;
        }

        #endregion

        #region OnTimer

        public async Task OnTimerAsync()
        {
            if (!Magics.TryGetValue(m_typeMagic, out var magic))
            {
                m_state = MagicState.None;
                return;
            }

            switch (m_state)
            {
                case MagicState.Intone: // intone
                    {
                        if (m_tIntone != null && !m_tIntone.IsTimeOut())
                            return;

                        if (m_tIntone != null && m_tIntone.IsTimeOut() && !await LaunchAsync(magic))
                        {
                            ResetDelay();
                        }

                        m_state = MagicState.None;

                        if (IsAutoAttack())
                        {
                            m_state = MagicState.Delay;
                            m_tDelay.Startup(Math.Max(MAGIC_DELAY, magic.DelayMs));
                        }
                        break;
                    }

                case MagicState.Delay: // delay
                    {
                        if ((m_pOwner.Map.IsTrainingMap() || IsAutoAttack())
                            && m_tDelay.IsActive()
                            && magic.Sort != MagicSort.Atkstatus)
                        {
                            if (m_tDelay.IsTimeOut())
                            {
                                m_state = MagicState.None;
                                if (!await m_pOwner.ProcessMagicAttackAsync(magic.Type, m_idTarget, (ushort)m_targetPos.X,
                                        (ushort)m_targetPos.Y,
                                        0))
                                    m_state = MagicState.Delay;
                            }
                            return;
                        }

                        if (!m_tDelay.IsActive())
                        {
                            m_state = MagicState.None;
                            await AbortMagicAsync(true);
                            return;
                        }

                        if (m_autoAttack && m_tDelay.IsTimeOut())
                        {
                            if ((m_tDelay.IsActive() && !m_tDelay.TimeOver()))
                                return;

                            m_state = MagicState.None;
                            await m_pOwner.ProcessMagicAttackAsync(magic.Type, m_idTarget, (ushort)m_targetPos.X, (ushort)m_targetPos.Y,
                                0);

                            if (m_idTarget != 0 && m_pOwner.Map.QueryAroundRole(m_pOwner, m_idTarget)?.IsPlayer() == true)
                                await AbortMagicAsync(false);
                        }

                        if (m_tDelay.IsActive() && m_tDelay.TimeOver())
                        {
                            m_state = MagicState.None;
                            await AbortMagicAsync(false);
                        }
                        break;
                    }
            }
        }

        #endregion

        private void ResetDelay()
        {
            if (!Magics.TryGetValue(m_typeMagic, out var magic))
                return;
            m_state = MagicState.Delay;
            m_tDelay.Update();
            magic.SetDelay();
        }

        private void SetAutoAttack(ushort type)
        {
            m_typeMagic = type;
            m_autoAttack = true;
        }

        private void BreakAutoAttack()
        {
            m_typeMagic = 0;
            m_autoAttack = false;
        }

        public bool IsAutoAttack()
        {
            return m_autoAttack && m_typeMagic != 0;
        }

        public async Task<bool> AbortMagicAsync(bool bSynchro)
        {
            BreakAutoAttack();

            if (m_state == MagicState.Intone)
            {
                m_tIntone.Clear();
            }

            m_state = MagicState.None;

            if (bSynchro && m_pOwner is Character)
            {
                await m_pOwner.SendAsync(new MsgAction
                {
                    Identity = m_pOwner.Identity,
                    Action = MsgAction.ActionType.AbortMagic
                });
            }

            return true;
        }

        public enum MagicState
        {
            None = 0,
            Intone = 1,
            //Launch = 2,
            Delay = 3
        }
    }
}