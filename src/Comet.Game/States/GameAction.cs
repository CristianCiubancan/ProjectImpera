// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Game Action.cs
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

//#define DEBUG_FELIPE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Events;
using Comet.Game.States.Families;
using Comet.Game.States.Items;
using Comet.Game.States.NPCs;
using Comet.Game.States.Syndicates;
using Comet.Game.World;
using Comet.Game.World.Maps;
using Comet.Shared;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Comet.Game.States
{
    public static class GameAction
    {
        public static async Task<bool> ExecuteActionAsync(uint idAction, Character user, Role role, Item item,
            string input)
        {
            const int _MAX_ACTION_I = 128;
            const int _DEADLOCK_CHECK_I = 5;

            if (idAction == 0)
                return false;

            int actionCount = 0;
            int deadLookCount = 0;
            uint idNext = idAction, idOld = idAction;
            while (idNext > 0)
            {
                if (actionCount++ > _MAX_ACTION_I)
                {
                    await Log.WriteLogAsync(LogLevel.Error,
                        $"Error: too many game action, from: {idAction}, last action: {idNext}");
                    return false;
                }

                if (idAction == idOld && deadLookCount++ >= _DEADLOCK_CHECK_I)
                {
                    await Log.WriteLogAsync(LogLevel.Deadloop,
                        $"Error: dead loop detected, from: {idAction}, last action: {idNext}");
                    return false;
                }

                if (idNext != idOld)
                {
                    deadLookCount = 0;
                }

                DbAction action = Kernel.EventManager.GetAction(idNext);
                if (action == null)
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Error: invalid game action: {idNext}");
                    return false;
                }

                string param = FormatParam(action, user, role, item, input);

#if !DEBUG_FELIPE
                if (user?.IsPm() == true)
                {
#else 
                if (user != null)
                {
#endif
                    await user.SendAsync(
                        $"{action.Identity}: [{action.IdNext},{action.IdNextfail}]. type[{action.Type}], data[{action.Data}], param:[{param}].",
                        MsgTalk.TalkChannel.System,
                        Color.White);
#if !DEBUG_FELIPE
                }
#else
                }
#endif

                bool result = false;
                switch ((TaskActionType)action.Type)
                {
                    case TaskActionType.ActionMenutext:
                        result = await ExecuteActionMenuTextAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenulink:
                        result = await ExecuteActionMenuLinkAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenuedit:
                        result = await ExecuteActionMenuEditAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenupic:
                        result = await ExecuteActionMenuPicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenuMessage:
                        result = await ExecuteActionMenuMessageAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenucreate:
                        result = await ExecuteActionMenuCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionRand:
                        result = await ExecuteActionMenuRandAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionRandaction:
                        result = await ExecuteActionMenuRandActionAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionChktime:
                        result = await ExecuteActionMenuChkTimeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionPostcmd:
                        result = await ExecuteActionPostcmdAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionBrocastmsg:
                        result = await ExecuteActionBrocastmsgAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMessagebox:
                        result = await ExecuteActionMessageboxAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionExecutequery:
                        result = await ExecuteActionExecutequeryAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionInviteTrans:
                        result = await ExecuteActionInviteTransAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionNpcAttr:
                        result = await ExecuteActionNpcAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcErase:
                        result = await ExecuteActionNpcEraseAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcResetsynowner:
                        result = await ExecuteActionNpcResetsynownerAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFindNextTable:
                        result = await ExecuteActionNpcFindNextTableAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFamilyCreate:
                        result = await ExecuteActionNpcFamilyCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFamilyDestroy:
                        result = await ExecuteActionNpcFamilyDestroyAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionMapMovenpc:
                        result = await ExecuteActionMapMovenpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapMapuser:
                        result = await ExecuteActionMapMapuserAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapBrocastmsg:
                        result = await ExecuteActionMapBrocastmsgAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapDropitem:
                        result = await ExecuteActionMapDropitemAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapSetstatus:
                        result = await ExecuteActionMapSetstatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapAttrib:
                        result = await ExecuteActionMapAttribAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapRegionMonster:
                        result = await ExecuteActionMapRegionMonsterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapRandDropItem:
                        result = await ExecuteActionMapRandDropItemAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapChangeweather:
                        result = await ExecuteActionMapChangeweatherAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapChangelight:
                        result = await ExecuteActionMapChangelightAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapMapeffect:
                        result = await ExecuteActionMapMapeffectAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapFireworks:
                        result = await ExecuteActionMapFireworksAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionItemRequestlaynpc:
                        result = await ExecuteActionItemRequestlaynpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemLaynpc:
                        result = await ExecuteActionItemLaynpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemDelthis:
                        result = await ExecuteActionItemDelthisAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionItemAdd:
                        result = await ExecuteActionItemAdd(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemDel:
                        result = await ExecuteActionItemDel(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemCheck:
                        result = await ExecuteActionItemCheck(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemHole:
                        result = await ExecuteActionItemHole(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemMultidel:
                        result = await ExecuteActionItemMultidel(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemMultichk:
                        result = await ExecuteActionItemMultichk(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemLeavespace:
                        result = await ExecuteActionItemLeavespace(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemUpequipment:
                        result = await ExecuteActionItemUpequipment(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquiptest:
                        result = await ExecuteActionItemEquiptest(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquipexist:
                        result = await ExecuteActionItemEquipexist(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquipcolor:
                        result = await ExecuteActionItemEquipcolor(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemCheckrand:
                        result = await ExecuteActionItemCheckrand(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemModify:
                        result = await ExecuteActionItemModify(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemJarCreate:
                        result = await ExecuteActionItemJarCreate(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemJarVerify:
                        result = await ExecuteActionItemJarVerify(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionSynCreate:
                        result = await ExecuteActionSynCreate(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynDestroy:
                        result = await ExecuteActionSynDestroy(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynSetAssistant:
                        result = await ExecuteActionSynSetAssistant(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearRank:
                        result = await ExecuteActionSynClearRank(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynChangeLeader:
                        result = await ExecuteActionSynChangeLeader(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAntagonize:
                        result = await ExecuteActionSynAntagonize(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearAntagonize:
                        result = await ExecuteActionSynClearAntagonize(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAlly:
                        result = await ExecuteActionSynAlly(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearAlly:
                        result = await ExecuteActionSynClearAlly(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAttr:
                        result = await ExecuteActionSynAttr(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionMstDropitem:
                        result = await ExecuteActionMstDropitem(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionUserAttr:
                        result = await ExecuteUserAttr(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserFull:
                        result = await ExecuteUserFull(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChgmap:
                        result = await ExecuteUserChgMap(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserRecordpoint:
                        result = await ExecuteUserRecordpoint(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserHair:
                        result = await ExecuteUserHair(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChgmaprecord:
                        result = await ExecuteUserChgmaprecord(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChglinkmap:
                        result = await ExecuteActionUserChglinkmap(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTransform:
                        result = await ExecuteUserTransform(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserIspure:
                        result = await ExecuteActionUserIspure(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTalk:
                        result = await ExecuteActionUserTalk(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMagic:
                        result = await ExecuteActionUserMagic(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWeaponskill:
                        result = await ExecuteActionUserWeaponSkill(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserLog:
                        result = await ExecuteActionUserLog(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserBonus:
                        result = await ExecuteActionUserBonus(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDivorce:
                        result = await ExecuteActionUserDivorce(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMarriage:
                        result = await ExecuteActionUserMarriage(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSex:
                        result = await ExecuteActionUserSex(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEffect:
                        result = await ExecuteActionUserEffect(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskmask:
                        result = await ExecuteActionUserTaskmask(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMediaplay:
                        result = await ExecuteActionUserMediaplay(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserCreatemap:
                        result = await ExecuteActionUserCreatemap(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEnterHome:
                        result = await ExecuteActionUserEnterHome(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEnterMateHome:
                        result = await ExecuteActionUserEnterMateHome(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserUnlearnMagic:
                        result = await ExecuteActionUserUnlearnMagic(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserRebirth:
                        result = await ExecuteActionUserRebirth(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWebpage:
                        result = await ExecuteActionUserWebpage(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserBbs:
                        result = await ExecuteActionUserBbs(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserUnlearnSkill:
                        result = await ExecuteActionUserUnlearnSkill(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDropMagic:
                        result = await ExecuteActionUserDropMagic(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserOpenDialog:
                        result = await ExecuteActionUserOpenDialog(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserPointAllot:
                        result = await ExecuteActionUserFixAttr(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserExpMultiply:
                        result = await ExecuteActionUserExpMultiply(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWhPassword:
                        result = await ExecuteActionUserWhPassword(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSetWhPassword:
                        result = await ExecuteActionUserSetWhPassword(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserOpeninterface:
                        result = await ExecuteActionUserOpeninterface(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskManager:
                        result = await ExecuteActionUserTaskManager(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskOpe:
                        result = await ExecuteActionUserTaskOpe(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskLocaltime:
                        result = await ExecuteActionUserTaskLocaltime(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskFind:
                        result = await ExecuteActionUserTaskFind(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarCompare:
                        result = await ExecuteActionUserVarCompare(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarDefine:
                        result = await ExecuteActionUserVarDefine(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarCalc:
                        result = await ExecuteActionUserVarCalc(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTestEquipment:
                        result = await ExecuteActionUserTestEquipment(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserExecAction:
                        result = await ExecuteActionUserExecAction(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTestPos:
                        result = true;
                        break; // gotta investigate
                    case TaskActionType.ActionUserStcCompare:
                        result = await ExecuteActionUserStcCompare(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcOpe:
                        result = await ExecuteActionUserStcOpe(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDataSync:
                        result = await ExecuteActionUserDataSync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSelectToData:
                        result = await ExecuteActionUserSelectToData(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcTimeCheck:
                        result = await ExecuteActionUserStcTimeCheck(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcTimeOperation:
                        result = await ExecuteActionUserStcTimeOpe(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserAttachStatus:
                        result = await ExecuteActionUserAttachStatus(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserGodTime:
                        result = await ExecuteActionUserGodTime(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserLogEvent:
                        result = await ExecuteActionUserLog(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserExpballExp:
                        result = await ExecuteActionUserExpballExp(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSomethingRelatedToRebirth:
                        result = true;
                        break;
                    case TaskActionType.ActionUserStatusCreate:
                        result = await ExecuteActionUserStatusCreate(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStatusCheck:
                        result = await ExecuteActionUserStatusCheck(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionTeamBroadcast:
                        result = await ExecuteActionTeamBroadcast(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamAttr:
                        result = await ExecuteActionTeamAttr(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamLeavespace:
                        result = await ExecuteActionTeamLeavespace(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemAdd:
                        result = await ExecuteActionTeamItemAdd(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemDel:
                        result = await ExecuteActionTeamItemDel(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemCheck:
                        result = await ExecuteActionTeamItemCheck(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamChgmap:
                        result = await ExecuteActionTeamChgmap(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamChkIsleader:
                        result = await ExecuteActionTeamChkIsleader(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionGeneralLottery:
                        result = await ExecuteGeneralLottery(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionChgMapSquare:
                        result = await ExecuteActionChgMapSquare(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionAchievements:
                        result = true;
                        break;

                    case TaskActionType.ActionEventSetstatus:
                        result = await ExecuteActionEventSetstatus(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventDelnpcGenid:
                        result = await ExecuteActionEventDelnpcGenid(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCompare:
                        result = await ExecuteActionEventCompare(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCompareUnsigned:
                        result = await ExecuteActionEventCompareUnsigned(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventChangeweather:
                        result = await ExecuteActionEventChangeweather(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCreatepet:
                        result = await ExecuteActionEventCreatepet(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCreatenewNpc:
                        result = await ExecuteActionEventCreatenewNpc(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCountmonster:
                        result = await ExecuteActionEventCountmonster(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventDeletemonster:
                        result = await ExecuteActionEventDeletemonster(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventBbs:
                        result = await ExecuteActionEventBbs(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventErase:
                        result = await ExecuteActionEventErase(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventTeleport:
                        result = await ExecuteActionEventTeleport(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventMassaction:
                        result = await ExecuteActionEventMassaction(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionEventRegister:
                        result = await ExecuteActionEventRegisterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventExit:
                        result = await ExecuteActionEventExit(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCmd:
                        result = await ExecuteActionEventCmdAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionFamilyAttr:
                        result = await ExecuteActionFamilyAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyMemberAttr:
                        result = await ExecuteActionFamilyMemberAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarActivityCheck:
                        result = await ExecuteActionFamilyWarActivityCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarAuthorityCheck:
                        result = await ExecuteActionFamilyWarAuthorityCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarRegisterCheck:
                        result = await ActionFamilyWarRegisterCheckAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionTrapCreate:
                        result = await ExecuteActionTrapCreate(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTrapErase:
                        result = await ExecuteActionTrapErase(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTrapCount:
                        result = await ExecuteActionTrapCount(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionDetainDialog:
                        result = await ExecuteActionDetainDialogAsync(action, param, user, role, item, input);
                        break;

                    default:
                        await Log.WriteLogAsync(LogLevel.Warning,
                            $"GameAction::ExecuteActionAsync unhandled action type {action.Type} for action: {action.Identity}");
                        break;
                }

                idOld = idAction;
                idNext = result ? action.IdNext : action.IdNextfail;
            }

            return true;
        }

        #region Action

        private static async Task<bool> ExecuteActionMenuTextAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Action[{action.Identity}] type 101 non character");
                return false;
            }

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.Dialog,
                Text = param,
                Data = (ushort)action.Data
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuLinkAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Action[{action.Identity}] type 101 non character");
                return false;
            }

            uint task = 0;
            int align = 0;
            string[] parsed = param.Split(' ');
            if (parsed.Length > 1)
                _ = uint.TryParse(parsed[1], out task);
            if (parsed.Length > 2)
                _ = int.TryParse(parsed[2], out align);

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.Option,
                Text = parsed[0],
                OptionIndex = user.PushTaskId(task),
                Data = (ushort)align
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuEditAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] paramStrings = SplitParam(param, 3);
            if (paramStrings.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"Invalid input param length for {action.Identity}, param: {param}");
                return false;
            }

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.Input,
                OptionIndex = user.PushTaskId(uint.Parse(paramStrings[1])),
                Data = ushort.Parse(paramStrings[0]),
                Text = paramStrings[2]
            });

            return true;
        }
    

        private static async Task<bool> ExecuteActionMenuPicAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);

            ushort x = ushort.Parse(splitParam[0]);
            ushort y = ushort.Parse(splitParam[1]);

            await user.SendAsync(new MsgTaskDialog
            {
                TaskIdentity = (uint)((x << 16) | y),
                InteractionType = MsgTaskDialog.TaskInteraction.Avatar,
                Data = ushort.Parse(splitParam[2])
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuMessageAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.MessageBox,
                Text = param,
                OptionIndex = user.PushTaskId(action.Data)
            });

            return true;
        }

        private static async Task<bool> ExecuteActionMenuCreateAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog.TaskInteraction.Finish
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuRandAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            string[] paramSplit = SplitParam(param, 2);

            int x = int.Parse(paramSplit[0]);
            int y = int.Parse(paramSplit[1]);
            double chance = 0.01;
            if (x > y)
                chance = 99;
            else
            {
                chance = x / (double)y;
                chance *= 100;
            }

            return await Kernel.ChanceCalcAsync(chance);
        }

        private static async Task<bool> ExecuteActionMenuRandActionAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] paramSplit = SplitParam(param);
            if (paramSplit.Length == 0)
                return false;
            uint taskId = uint.Parse(paramSplit[await Kernel.NextAsync(0, paramSplit.Length) % paramSplit.Length]);
            return await ExecuteActionAsync(taskId, user, role, item, input);
        }

        private static async Task<bool> ExecuteActionMenuChkTimeAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] paramSplit = SplitParam(param);

            DateTime actual = DateTime.Now;
            var nCurWeekDay = (int)actual.DayOfWeek;
            int nCurHour = actual.Hour;
            int nCurMinute = actual.Minute;

            switch (action.Data)
            {
                #region Complete date (yyyy-mm-dd hh:mm yyyy-mm-dd hh:mm)

                case 0:
                    {
                        if (paramSplit.Length < 4)
                            return false;

                        string[] time0 = paramSplit[1].Split(':');
                        string[] date0 = paramSplit[0].Split('-');
                        string[] time1 = paramSplit[3].Split(':');
                        string[] date1 = paramSplit[2].Split('-');

                        var dTime0 = new DateTime(int.Parse(date0[0]), int.Parse(date0[1]), int.Parse(date0[2]),
                            int.Parse(time0[0]), int.Parse(time0[1]), 0);
                        var dTime1 = new DateTime(int.Parse(date1[0]), int.Parse(date1[1]), int.Parse(date1[2]),
                            int.Parse(time1[0]), int.Parse(time1[1]), 59);

                        return dTime0 <= actual && dTime1 >= actual;
                    }

                #endregion

                #region On Year date (mm-dd hh:mm mm-dd hh:mm)

                case 1:
                    {
                        if (paramSplit.Length < 4)
                            return false;

                        string[] time0 = paramSplit[1].Split(':');
                        string[] date0 = paramSplit[0].Split('-');
                        string[] time1 = paramSplit[3].Split(':');
                        string[] date1 = paramSplit[2].Split('-');

                        var dTime0 = new DateTime(DateTime.Now.Year, int.Parse(date0[1]), int.Parse(date0[2]),
                            int.Parse(time0[0]), int.Parse(time0[1]), 0);
                        var dTime1 = new DateTime(DateTime.Now.Year, int.Parse(date1[1]), int.Parse(date1[2]),
                            int.Parse(time1[0]), int.Parse(time1[1]), 59);

                        return dTime0 <= actual && dTime1 >= actual;
                    }

                #endregion

                #region Day of the month (dd hh:mm dd hh:mm)

                case 2:
                    {
                        if (paramSplit.Length < 4)
                            return false;

                        string[] time0 = paramSplit[1].Split(':');
                        string date0 = paramSplit[0];
                        string[] time1 = paramSplit[3].Split(':');
                        string date1 = paramSplit[2];

                        var dTime0 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(date0),
                            int.Parse(time0[0]), int.Parse(time0[1]), 0);
                        var dTime1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(date1),
                            int.Parse(time1[0]), int.Parse(time1[1]), 59);

                        return dTime0 <= actual && dTime1 >= actual;
                    }

                #endregion

                #region Day of the week (dw hh:mm dw hh:mm)

                case 3:
                    {
                        if (paramSplit.Length < 4)
                            return false;

                        string[] time0 = paramSplit[1].Split(':');
                        string[] time1 = paramSplit[3].Split(':');

                        int nDay0 = int.Parse(paramSplit[0]);
                        int nDay1 = int.Parse(paramSplit[2]);
                        int nHour0 = int.Parse(time0[0]);
                        int nHour1 = int.Parse(time1[0]);
                        int nMinute0 = int.Parse(time0[1]);
                        int nMinute1 = int.Parse(time1[1]);

                        int timeNow = nCurWeekDay * 24 * 60 + nCurHour * 60 + nCurMinute;
                        int from = nDay0 * 24 * 60 + nHour0 * 60 + nMinute0;
                        int to = nDay1 * 24 * 60 + nHour1 * 60 + nMinute1;

                        return timeNow >= from && timeNow <= to;
                    }

                #endregion

                #region Hour check (hh:mm hh:mm)

                case 4:
                    {
                        if (paramSplit.Length < 2)
                            return false;

                        string[] time0 = paramSplit[0].Split(':');
                        string[] time1 = paramSplit[1].Split(':');

                        int nHour0 = int.Parse(time0[0]);
                        int nHour1 = int.Parse(time1[0]);
                        int nMinute0 = int.Parse(time0[1]);
                        int nMinute1 = int.Parse(time1[1]);

                        int timeNow = nCurHour * 60 + nCurMinute;
                        int from = nHour0 * 60 + nMinute0;
                        int to = nHour1 * 60 + nMinute1;

                        return timeNow >= from && timeNow <= to;
                    }

                #endregion

                #region Minute check (mm mm)

                case 5:
                    {
                        if (paramSplit.Length < 2)
                            return false;

                        return nCurMinute >= int.Parse(paramSplit[0]) && nCurMinute <= int.Parse(paramSplit[1]);
                    }

                    #endregion
            }

            return false;
        }

        private static async Task<bool> ExecuteActionPostcmdAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(new MsgAction
            {
                Identity = user.Identity,
                Command = action.Data,
                Action = MsgAction.ActionType.ClientCommand,
                ArgumentX = user.MapX,
                ArgumentY = user.MapY
            });

            return true;
        }

        private static async Task<bool> ExecuteActionBrocastmsgAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            await Kernel.RoleManager.BroadcastMsgAsync(param, (MsgTalk.TalkChannel)action.Data, Color.White);
            return true;
        }

        private static async Task<bool> ExecuteActionMessageboxAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user != null)
            {
                MsgTalk.TalkChannel channel = MsgTalk.TalkChannel.MessageBox;
                if (action.Data != 0 && Enum.IsDefined(typeof(MsgTalk.TalkChannel), (ushort)action.Data))
                    channel = (MsgTalk.TalkChannel)action.Data;

                await user.SendAsync(new MsgTalk(0, channel, param));
            }

            return true;
        }

        private static async Task<bool> ExecuteActionExecutequeryAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            try
            {
                if (param.Trim().StartsWith("SELECT", StringComparison.InvariantCultureIgnoreCase) ||
                    param.Trim().StartsWith("UPDATE", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!param.Contains("WHERE") || !param.Contains("LIMIT"))
                    {
                        await Log.WriteLogAsync("database", LogLevel.Warning,
                            $"ExecuteActionExecutequery {action.Identity} doesn't have WHERE or LIMIT clause [{param}]");
                        return false;
                    }
                }

                _ = await new ServerDbContext().Database.ExecuteSqlRawAsync(param);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionInviteTransAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            if (user.MessageBox is CaptchaBox)
                return false;

            string[] split = param.Trim().Split(' ');
            if (split.Length != 21)
            {
                await Log.WriteLogAsync(LogLevel.Action, "ActionInviteTrans must have 21 parameters. mapid 8pos msg acceptmsg type seconds");
                return false;
            }

            uint idMap = uint.Parse(split[0]);
            ushort[] px = new ushort[8];
            ushort[] py = new ushort[8];
            for (int i = 0; i < 8; i++)
            {
                px[i] = ushort.Parse(split[i * 2 + 1]);
                py[i] = ushort.Parse(split[i * 2 + 2]);
            }
            int sendMsg = int.Parse(split[17]);
            int acceptMsg = int.Parse(split[18]);
            int priority = int.Parse(split[19]);
            int seconds = int.Parse(split[20]);

            if (user.MessageBox is TimedMessageBox box)
            {
                if (box.Priority > priority && !box.HasExpired)
                    return true;
            }

            TimedMessageBox timedMessageBox = new TimedMessageBox(user, seconds);
            timedMessageBox.MessageId = sendMsg;
            timedMessageBox.AcceptMsgId = acceptMsg;
            timedMessageBox.Priority = priority;
            timedMessageBox.TargetMapIdentity = idMap;
            timedMessageBox.TargetMapX = px;
            timedMessageBox.TargetMapY = py;

            user.MessageBox = timedMessageBox;

            await timedMessageBox.SendAsync();

            return true;
        }

        #endregion

        #region Npc

        private static async Task<bool> ExecuteActionNpcAttrAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            string[] splitParams = SplitParam(param);
            if (splitParams.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionNpcAttr invalid param num {param}, {action.Identity}");
                return false;
            }

            string ope = splitParams[0].ToLower();
            string opt = splitParams[1].ToLower();
            bool isInt = int.TryParse(splitParams[2], out int data);
            string strData = splitParams[2];

            if (splitParams.Length < 4 || !uint.TryParse(splitParams[3], out var idNpc))
            {
                idNpc = role?.Identity ?? user?.InteractingNpc ?? 0;
            }

            BaseNpc npc = Kernel.RoleManager.GetRole<BaseNpc>(idNpc);
            if (npc == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionNpcAttr invalid NPC id {idNpc} for action {action.Identity}");
                return false;
            }

            int cmp = 0;
            string strCmp = "";
            if (ope.Equals("life", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    return await npc.SetAttributesAsync(ClientUpdateType.Hitpoints, (ulong)data);
                }

                if (opt == "+=")
                {
                    return await npc.AddAttributesAsync(ClientUpdateType.Hitpoints, data);
                }

                cmp = (int)npc.Life;
            }
            else if (ope.Equals("lookface", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    return await npc.SetAttributesAsync(ClientUpdateType.Mesh, (ulong)data);
                }

                cmp = (int)npc.Mesh;
            }
            else if (ope.Equals("ownerid", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    if (!(npc is DynamicNpc dyna))
                        return false;
                    return await dyna.SetOwnerAsync((uint)data);
                }

                cmp = (int)npc.OwnerIdentity;
            }
            else if (ope.Equals("ownertype", StringComparison.InvariantCultureIgnoreCase))
            {
                cmp = (int)npc.OwnerType;
            }
            else if (ope.Equals("maxlife", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    return await npc.SetAttributesAsync(ClientUpdateType.MaxHitpoints, (ulong)data);
                }

                cmp = (int)npc.MaxLife;
            }
            else if (ope.StartsWith("data", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    _ = npc.SetData(ope, data);
                    return await npc.SaveAsync();
                }

                if (opt == "+=")
                {
                    _ = npc.SetData(ope, npc.GetData(ope) + data);
                    return await npc.SaveAsync();
                }

                cmp = npc.GetData(ope);
                isInt = true;
            }
            else if (ope.Equals("datastr", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    npc.DataStr = strData;
                    return await npc.SaveAsync();
                }

                if (opt == "+=")
                {
                    npc.DataStr += strData;
                    return await npc.SaveAsync();
                }

                strCmp = npc.DataStr;
            }

            switch (opt)
            {
                case "==": return isInt && cmp == data || strCmp == strData;
                case ">=": return isInt && cmp >= data;
                case "<=": return isInt && cmp <= data;
                case ">": return isInt && cmp > data;
                case "<": return isInt && cmp < data;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionNpcEraseAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            BaseNpc npc = Kernel.RoleManager.GetRole<BaseNpc>(user.InteractingNpc) ?? role as BaseNpc;
            if (npc == null)
                return false;

            if (action.Data == 0)
            {
                await npc.DelNpcAsync();
                user.InteractingNpc = 0;
                return true;
            }

            foreach (var del in Kernel.RoleManager.QueryRoleByType<DynamicNpc>().Where(x => x.Type == action.Data))
            {
                await del.DelNpcAsync();
            }

            return true;
        }

        private static async Task<bool> ExecuteActionNpcResetsynownerAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (!(role is DynamicNpc npc))
                return false;

            if (npc.IsSynNpc() && !npc.Map.IsSynMap()
                || npc.IsCtfFlag() && !npc.Map.IsSynMap())
                return false;

            var score = npc.GetTopScore();
            if (score != null)
            {
                Syndicate syn = Kernel.SyndicateManager.GetSyndicate((int)score.Identity);
                if (npc.IsSynFlag() && syn != null)
                {
                    await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrWarWon, syn.Name),
                        MsgTalk.TalkChannel.Center);
                    npc.Map.OwnerIdentity = syn.Identity;
                }
                else if (npc.IsCtfFlag())
                {
                    if (user?.IsPm() == true)
                        await user.SendAsync($"CTF Flag is not handled");
                    return true;
                }

                if (syn != null)
                {
                    _ = await npc.SetOwnerAsync(syn.Identity, true);
                }

                npc.ClearScores();

                if (npc.Map.IsDynamicMap())
                    _ = await npc.Map.SaveAsync();

                _ = await npc.SaveAsync();
            }

            foreach (var player in Kernel.RoleManager.QueryRoleByMap<Character>(npc.MapIdentity))
            {
                _ = player.SetAttackTarget(null);
            }

            if (npc.IsSynFlag())
            {
                foreach (var resetNpc in Kernel.RoleManager.QueryRoleByMap<BaseNpc>(npc.MapIdentity))
                {
                    if (resetNpc.IsSynFlag())
                        continue;

                    resetNpc.OwnerIdentity = npc.OwnerIdentity;
                    /*if (resetNpc.IsLinkNpc()) TODO
                    {
                        
                    }*/
                    _ = await resetNpc.SaveAsync();
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionNpcFindNextTableAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 4)
                return false;

            uint idNpc = uint.Parse(splitParam[0]);
            uint idMap = uint.Parse(splitParam[1]);
            ushort usMapX = ushort.Parse(splitParam[2]);
            ushort usMapY = ushort.Parse(splitParam[3]);

            BaseNpc npc = Kernel.RoleManager.GetRole<BaseNpc>(idNpc);
            if (npc == null)
                return false;

            _ = npc.SetData("data0", (int)idMap);
            _ = npc.SetData("data1", usMapX);
            _ = npc.SetData("data2", usMapY);
            _ = await npc.SaveAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionNpcFamilyCreateAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null || user.Family != null)
                return false;

            if (user.Level < 50 || user.Silvers < 500000)
                return false;

            return await user.CreateFamilyAsync(input, 500000);
        }

        private static async Task<bool> ExecuteActionNpcFamilyDestroyAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
                return false;
            return await user.DisbandFamilyAsync();
        }

        #endregion

        #region Map

        private static async Task<bool> ExecuteActionMapMovenpcAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
                return false;

            uint idMap = uint.Parse(splitParam[0]);
            ushort nPosX = ushort.Parse(splitParam[1]), nPosY = ushort.Parse(splitParam[2]);

            if (idMap <= 0 || nPosX <= 0 || nPosY <= 0)
                return false;

            BaseNpc npc = Kernel.RoleManager.GetRole<BaseNpc>(action.Data);
            if (npc == null)
                return false;

            return await npc.ChangePosAsync(idMap, nPosX, nPosY);
        }

        private static async Task<bool> ExecuteActionMapMapuserAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3) return false;

            int amount = 0;

            if (splitParam[0].Equals("map_user", StringComparison.InvariantCultureIgnoreCase))
            {
                amount += Kernel.MapManager.GetMap(action.Data)?.PlayerCount ?? 0;
            }
            else if (splitParam[0].Equals("alive_user", StringComparison.InvariantCultureIgnoreCase))
            {
                amount += Kernel.RoleManager.QueryRoleByMap<Character>(action.Data).Count(x => x.IsAlive);
            }
            else
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionMapMapuser invalid cmd {splitParam[0]} for action {action.Identity}, {param}");
                return false;
            }

            switch (splitParam[1])
            {
                case "==":
                    return amount == int.Parse(splitParam[2]);
                case "<=":
                    return amount <= int.Parse(splitParam[2]);
                case ">=":
                    return amount >= int.Parse(splitParam[2]);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapBrocastmsgAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            GameMap map = Kernel.MapManager.GetMap(action.Data);
            if (map == null)
                return false;

            await map.BroadcastMsgAsync(param);
            return true;
        }

        private static async Task<bool> ExecuteActionMapDropitemAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 4)
                return false;

            uint idMap = uint.Parse(splitParam[1]);
            uint idItemtype = uint.Parse(splitParam[0]);
            ushort x = ushort.Parse(splitParam[2]);
            ushort y = ushort.Parse(splitParam[3]);

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
                return false;

            MapItem mapItem = new MapItem((uint)IdentityGenerator.MapItem.GetNextIdentity);
            if (mapItem.Create(map, new Point(x, y), idItemtype, 0, 0, 0, 0))
            {
                await mapItem.EnterMapAsync();
            }
            else
            {
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionMapSetstatusAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
                return false;

            var idMap = uint.Parse(splitParam[0]);
            var dwStatus = byte.Parse(splitParam[1]);
            bool flag = splitParam[2] != "0";

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
                return false;

            await map.SetStatusAsync(dwStatus, flag);
            return true;
        }

        private static async Task<bool> ExecuteActionMapAttribAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3) return false;

            string szField = splitParam[0];
            string szOpt = splitParam[1];
            int x = 0;
            int data = int.Parse(splitParam[2]);
            uint idMap = 0;

            if (splitParam.Length >= 4)
                idMap = uint.Parse(splitParam[3]);

            GameMap map;
            if (idMap == 0)
            {
                if (user == null)
                    return false;
                map = Kernel.MapManager.GetMap(user.MapIdentity);
            }
            else
            {
                map = Kernel.MapManager.GetMap(idMap);
            }

            if (map == null)
                return false;

            if (szField.Equals("status", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (szOpt.ToLowerInvariant())
                {
                    case "test":
                        return map.IsWarTime();
                    case "set":
                        await map.SetStatusAsync((ulong)data, true);
                        return true;
                    case "reset":
                        await map.SetStatusAsync((ulong)data, false);
                        return true;
                }
            }
            else if (szField.Equals("type", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (szOpt.ToLowerInvariant())
                {
                    case "test":
                        return (map.Type & (ulong)data) != 0;
                }
            }
            else if (szField.Equals("mapdoc", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.MapDoc = (uint)data;
                    _ = await map.SaveAsync();
                    return true;
                }

                x = (int)map.MapDoc;
            }
            else if (szField.Equals("portal0_x", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.PortalX = (ushort)data;
                    _ = await map.SaveAsync();
                    return true;
                }

                x = (int)map.PortalX;
            }
            else if (szField.Equals("portal0_y", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.PortalY = (ushort)data;
                    _ = await map.SaveAsync();
                    return true;
                }

                x = (int)map.PortalY;
            }
            else if (szField.Equals("res_lev", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.ResLev = (byte)data;
                    _ = await map.SaveAsync();
                    return true;
                }

                x = map.ResLev;
            }
            else
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionMapAttrib invalid field {szField} for action {action.Identity}, {param}");
                return false;
            }

            switch (szOpt)
            {
                case "==": return x == data;
                case ">=": return x >= data;
                case "<=": return x <= data;
                case "<": return x < data;
                case ">": return x > data;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapRegionMonsterAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 8)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ERROR: Invalid param amount on actionid: [{action.Identity}]");
                return false;
            }

            string szOpt = splitParam[6];
            uint idMap = uint.Parse(splitParam[0]);
            uint idType = uint.Parse(splitParam[5]);
            ushort nRegionX = ushort.Parse(splitParam[1]),
                nRegionY = ushort.Parse(splitParam[2]),
                nRegionCX = ushort.Parse(splitParam[3]),
                nRegionCY = ushort.Parse(splitParam[4]);
            int nData = int.Parse(splitParam[7]);

            GameMap map;
            if (idMap == 0)
            {
                if (user == null)
                    return false;

                map = user.Map;
            }
            else
            {
                map = Kernel.MapManager.GetMap(idMap);
            }

            if (map == null)
                return false;

            int count = Kernel.RoleManager.QueryRoleByMap<Monster>(idMap).Count(x =>
                ((idType != 0 && x.Type == idType) || idType == 0) && x.MapX >= nRegionX &&
                x.MapX < nRegionX - nRegionCX
                && x.MapY >= nRegionY && x.MapY < nRegionY - nRegionCY);

            switch (szOpt)
            {
                case "==": return count == nData;
                case "<=": return count <= nData;
                case ">=": return count >= nData;
                case "<": return count < nData;
                case ">": return count > nData;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapRandDropItemAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            // Example: 728006 3030 186 187 304 307 250 3600
            //          ItemID MAP  X   Y   CX  CY  AMOUNT DURATION
            string[] splitParam = SplitParam(param, 8);
            if (splitParam.Length != 8)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionMapRandDropItem: ItemID MAP  X   Y   CX  CY  AMOUNT DURATION :: {param} ({action.Identity})");
                return false;
            }

            var idItemtype = uint.Parse(splitParam[0]); // the item to be dropped
            var idMap = uint.Parse(splitParam[1]); // the map
            var initX = ushort.Parse(splitParam[2]); // start coordinates
            var initY = ushort.Parse(splitParam[3]); // start coordinates 
            var endX = ushort.Parse(splitParam[4]); // end coordinates
            var endY = ushort.Parse(splitParam[5]); // end coordinates
            var amount = int.Parse(splitParam[6]); // amount of items to be dropped
            var duration = int.Parse(splitParam[7]); // duration of the item in the floor

            DbItemtype itemtype = Kernel.ItemManager.GetItemtype(idItemtype);
            if (itemtype == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid itemtype {idItemtype}, {param}, {action.Identity}");
                return false;
            }

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid map {idMap}, {param}, {action.Identity}");
                return false;
            }

            for (int i = 0; i < amount; i++)
            {
                MapItem mapItem = new MapItem((uint)IdentityGenerator.MapItem.GetNextIdentity);
                int positionRetry = 0;
                bool posSuccess = true;

                int deltaX = endX - initX;
                int deltaY = endY - initY;

                int targetX = initX + await Kernel.NextAsync(deltaX);
                int targetY = initY + await Kernel.NextAsync(deltaY);

                Point pos = new Point(targetX, targetY);
                while (!map.FindDropItemCell(9, ref pos))
                {
                    if (positionRetry++ >= 5)
                    {
                        posSuccess = false;
                        break;
                    }

                    targetX = initX + await Kernel.NextAsync(deltaX);
                    targetY = initY + await Kernel.NextAsync(deltaY);

                    pos = new Point(targetX, targetY);
                }

                if (!posSuccess)
                    continue;

                if (!mapItem.Create(map, pos, idItemtype, 0, 0, 0, 0))
                    continue;

                mapItem.SetAliveTimeout(duration);
                await mapItem.EnterMapAsync();
            }

            return true;
        }

        private static async Task<bool> ExecuteActionMapChangeweatherAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5) return false;

            int nType = int.Parse(pszParam[0]), nIntensity = int.Parse(pszParam[1]), nDir = int.Parse(pszParam[2]);
            uint dwColor = uint.Parse(pszParam[3]), dwKeepSecs = uint.Parse(pszParam[4]);

            GameMap map;
            if (action.Data == 0)
            {
                if (user == null)
                    return false;
                map = user.Map;
            }
            else
            {
                map = Kernel.MapManager.GetMap(action.Data);
            }

            if (map == null)
                return false;

            _ = await map.Weather.SetNewWeatherAsync((Weather.WeatherType)nType, nIntensity, nDir, (int)dwColor,
                (int)dwKeepSecs, 0);
            _ = await map.Weather.SendWeatherAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionMapChangelightAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2) return false;

            uint idMap = uint.Parse(splitParam[0]), dwRgb = uint.Parse(splitParam[1]);

            GameMap map;
            if (action.Data == 0)
            {
                if (user == null)
                    return false;
                map = user.Map;
            }
            else
            {
                map = Kernel.MapManager.GetMap(idMap);
            }

            if (map == null)
                return false;

            map.Light = dwRgb;
            await map.BroadcastMsgAsync(new MsgAction
            {
                Identity = 1,
                Command = dwRgb,
                Argument = 0,
                Action = MsgAction.ActionType.MapArgb
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMapMapeffectAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 4) return false;

            uint idMap = uint.Parse(splitParam[0]);
            ushort posX = ushort.Parse(splitParam[1]), posY = ushort.Parse(splitParam[2]);
            string szEffect = splitParam[3];

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
                return false;

            await map.BroadcastRoomMsgAsync(posX, posY, new MsgName
            {
                Identity = 0,
                Action = StringAction.MapEffect,
                PositionX = posX,
                PositionY = posY,
                Strings = new List<string>
                {
                    szEffect
                }
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMapFireworksAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user != null)
            {
                await user.BroadcastRoomMsgAsync(new MsgName
                {
                    Identity = user.Identity,
                    Action = StringAction.Fireworks
                }, true);
            }

            return true;
        }

        #endregion

        #region Lay Item

        private static async Task<bool> ExecuteActionItemRequestlaynpcAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param, 5);

            uint idNextTask = uint.Parse(splitParams[0]);
            uint dwType = uint.Parse(splitParams[1]);
            uint dwSort = uint.Parse(splitParams[2]);
            uint dwLookface = uint.Parse(splitParams[3]);
            uint dwRegion = 0;

            if (splitParams.Length > 4)
                _ = uint.TryParse(splitParams[4], out dwRegion);

            if (idNextTask != 0)
                user.InteractingItem = idNextTask;

            await user.SendAsync(new MsgNpc
            {
                Identity = dwRegion,
                Data = dwLookface,
                Event = (ushort)dwType,
                RequestType = MsgNpc.NpcActionType.LayNpc
            });
            return true;
        }

        private static async Task<bool> ExecuteActionItemLaynpcAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            string[] splitParam = SplitParam(input, 5);
            if (splitParam.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Invalid input count for action [{action.Identity}]: {input}");
                return false;
            }

            if (!ushort.TryParse(splitParam[0], out var mapX)
                || !ushort.TryParse(splitParam[1], out var mapY)
                || !uint.TryParse(splitParam[2], out var lookface))
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"Invalid input params for action [{action.Identity}]1: {input}");
                return false;
            }

            uint frame = 0;
            uint pose = 0;
            if (splitParam.Length >= 4)
            {
                _ = uint.TryParse(splitParam[3], out frame);
                _ = uint.TryParse(splitParam[4], out pose);
            }

            if (user.Map.IsSuperPosition(mapX, mapY))
            {
                await user.SendAsync(Language.StrLayNpcSuperPosition);
                return false;
            }

            splitParam = SplitParam(param, 21);

            if (param.Length < 5) return false;

            uint nRegionType = 0;
            string szName = splitParam[0];
            ushort usType = ushort.Parse(splitParam[1]);
            ushort usSort = ushort.Parse(splitParam[2]);
            uint dwOwnerType = uint.Parse(splitParam[4]);
            uint dwLife = 0;
            uint idBase = 0;
            uint idLink = 0;
            uint idTask0 = 0;
            uint idTask1 = 0;
            uint idTask2 = 0;
            uint idTask3 = 0;
            uint idTask4 = 0;
            uint idTask5 = 0;
            uint idTask6 = 0;
            uint idTask7 = 0;
            int idData0 = 0;
            int idData1 = 0;
            int idData2 = 0;
            int idData3 = 0;

            if (splitParam.Length >= 6)
                dwLife = uint.Parse(splitParam[5]);
            if (splitParam.Length >= 7)
                nRegionType = uint.Parse(splitParam[6]);
            if (splitParam.Length >= 8)
                idBase = uint.Parse(splitParam[7]);
            if (splitParam.Length >= 9)
                idLink = uint.Parse(splitParam[8]);
            if (splitParam.Length >= 10)
                idTask0 = uint.Parse(splitParam[9]);
            if (splitParam.Length >= 11)
                idTask1 = uint.Parse(splitParam[10]);
            if (splitParam.Length >= 12)
                idTask2 = uint.Parse(splitParam[11]);
            if (splitParam.Length >= 13)
                idTask3 = uint.Parse(splitParam[12]);
            if (splitParam.Length >= 14)
                idTask4 = uint.Parse(splitParam[13]);
            if (splitParam.Length >= 15)
                idTask5 = uint.Parse(splitParam[14]);
            if (splitParam.Length >= 16)
                idTask6 = uint.Parse(splitParam[15]);
            if (splitParam.Length >= 17)
                idTask7 = uint.Parse(splitParam[16]);
            if (splitParam.Length >= 18)
                idData0 = int.Parse(splitParam[17]);
            if (splitParam.Length >= 19)
                idData1 = int.Parse(splitParam[18]);
            if (splitParam.Length >= 20)
                idData2 = int.Parse(splitParam[19]);
            if (splitParam.Length >= 21)
                idData3 = int.Parse(splitParam[20]);

            if (usType == BaseNpc.SYNTRANS_NPC && user.Map.IsTeleportDisable())
            {
                _ = user.SendAsync(Language.StrLayNpcSynTransInvalidMap);
                return false;
            }

            if (usType == BaseNpc.STATUARY_NPC)
            {
                szName = user.Name;
                lookface = user.Mesh % 10;
                idTask0 = user.Headgear?.Type ?? 0;
                idTask1 = user.Armor?.Type ?? 0;
                idTask2 = user.RightHand?.Type ?? 0;
                idTask3 = user.LeftHand?.Type ?? 0;
                idTask4 = frame;
                idTask5 = pose;
                idTask6 = user.Mesh;
                idTask7 = ((uint)user.SyndicateRank << 16) + user.Hairstyle;
            }

            if (nRegionType > 0 && !user.Map.QueryRegion((RegionTypes)nRegionType, mapX, mapY))
                return false;

            uint idOwner = 0;
            switch (dwOwnerType)
            {
                case 1:
                    if (user.Identity == 0)
                        return false;

                    idOwner = user.Identity;
                    break;
                case 2:
                    if (user.SyndicateIdentity == 0)
                        return false;

                    idOwner = user.SyndicateIdentity;
                    break;
            }

            DynamicNpc npc = user.Map.QueryStatuary(user, lookface, idTask0) ?? Kernel.RoleManager
                .QueryRoleByType<DynamicNpc>().FirstOrDefault(x => x.LinkId == idLink);
            if (npc == null)
            {
                npc = new DynamicNpc(new DbDynanpc
                {
                    Name = szName,
                    Ownerid = idOwner,
                    OwnerType = dwOwnerType,
                    Type = usType,
                    Sort = usSort,
                    Life = dwLife,
                    Maxlife = dwLife,
                    Base = idBase,
                    Linkid = idLink,
                    Task0 = idTask0,
                    Task1 = idTask1,
                    Task2 = idTask2,
                    Task3 = idTask3,
                    Task4 = idTask4,
                    Task5 = idTask5,
                    Task6 = idTask6,
                    Task7 = idTask7,
                    Data0 = idData0,
                    Data1 = idData1,
                    Data2 = idData2,
                    Data3 = idData3,
                    Datastr = "",
                    Defence = 0,
                    Cellx = mapX,
                    Celly = mapY,
                    Idxserver = 0,
                    Itemid = 0,
                    Lookface = (ushort)lookface,
                    MagicDef = 0,
                    Mapid = user.MapIdentity
                });

                if (!await npc.InitializeAsync())
                    return false;
            }
            else
            {
                npc.SetType(usType);
                // npc.OwnerIdentity = idOwner;
                npc.OwnerType = (byte)dwOwnerType;
                _ = await npc.SetOwnerAsync(idOwner);
                npc.Name = szName;
                _ = await npc.SetAttributesAsync(ClientUpdateType.Mesh, lookface);
                npc.SetSort(usSort);
                npc.SetTask(0, idTask0);
                npc.SetTask(1, idTask1);
                npc.SetTask(2, idTask2);
                npc.SetTask(3, idTask3);
                npc.SetTask(4, idTask4);
                npc.SetTask(5, idTask5);
                npc.SetTask(6, idTask6);
                npc.SetTask(7, idTask7);
                npc.Data0 = idData0;
                npc.Data1 = idData1;
                npc.Data2 = idData2;
                npc.Data3 = idData3;
                _ = await npc.SetAttributesAsync(ClientUpdateType.MaxHitpoints, dwLife);
                npc.MapX = mapX;
                npc.MapY = mapY;
            }

            _ = await npc.ChangePosAsync(user.MapIdentity, mapX, mapY);
            _ = await npc.SaveAsync();

            role = npc;
            user.InteractingNpc = npc.Identity;
            return true;
        }

        private static async Task<bool> ExecuteActionItemDelthisAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            user.InteractingItem = 0;
            if (item != null)
            {
                _ = user.UserPackage.SpendItemAsync(item);
            }

            _ = user.SendAsync(Language.StrUseItem);
            return true;
        }

        #endregion

        #region Item

        private static async Task<bool> ExecuteActionItemAdd(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (!user.UserPackage.IsPackSpare(1))
                return false;

            DbItemtype itemtype = Kernel.ItemManager.GetItemtype(action.Data);
            if (itemtype == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Invalid itemtype: {action.Identity}, {action.Type}, {action.Data}");
                return false;
            }

            string[] splitParam = SplitParam(param);
            DbItem newItem = Item.CreateEntity(action.Data);
            newItem.PlayerId = user.Identity;

            for (int i = 0; i < splitParam.Length; i++)
            {
                if (!int.TryParse(splitParam[i], out var value))
                    continue;

                switch (i)
                {
                    case 0: // amount
                        if (value > 0)
                            newItem.Amount = (ushort)Math.Min(value, ushort.MaxValue);
                        break;
                    case 1: // amount limit
                        if (value > 0)
                            newItem.AmountLimit = (ushort)Math.Min(value, ushort.MaxValue);
                        break;
                    case 2: // socket progress
                        newItem.Data = (uint)Math.Min(value, ushort.MaxValue);
                        break;
                    case 3: // gem 1
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte)value))
                            newItem.Gem1 = (byte)value;
                        break;
                    case 4: // gem 2
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte)value))
                            newItem.Gem2 = (byte)value;
                        break;
                    case 5: // effect magic 1
                        if (Enum.IsDefined(typeof(Item.ItemEffect), (ushort)value))
                            newItem.Magic1 = (byte)value;
                        break;
                    case 6: // magic 2
                        newItem.Magic2 = (byte)value;
                        break;
                    case 7: // magic 3
                        newItem.Magic3 = (byte)value;
                        break;
                    case 8: // reduce dmg
                        newItem.ReduceDmg = (byte)Math.Min(byte.MaxValue, value);
                        break;
                    case 9: // add life
                        newItem.AddLife = (byte)Math.Min(byte.MaxValue, value);
                        break;
                    case 10: // plunder
                        newItem.Plunder = null;
                        break;
                    case 11: // color
                        if (Enum.IsDefined(typeof(Item.ItemColor), (byte)value))
                            newItem.Color = (byte)value;
                        break;
                    case 12: // monopoly
                        newItem.Monopoly = (byte)Math.Min(byte.MaxValue, Math.Max(0, value));
                        break;
                    case 13: // mount color
                        newItem.Data = (uint)value;
                        break;
                    case 17: // Accumulate Num
                        newItem.AccumulateNum = (uint)value;
                        break;
                }
            }

            item = new Item(user);
            if (!await item.CreateAsync(newItem))
                return false;

            return await user.UserPackage.AddItemAsync(item);
        }

        private static async Task<bool> ExecuteActionItemDel(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (action.Data != 0)
                return await user.UserPackage.MultiSpendItemAsync(action.Data, action.Data, 1);

            if (!string.IsNullOrEmpty(param))
            {
                item = user.UserPackage[param];
                if (item == null)
                    return false;
                return await user.UserPackage.SpendItemAsync(item);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemCheck(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (action.Data != 0)
                return user.UserPackage.MultiCheckItem(action.Data, action.Data, 1);

            if (!string.IsNullOrEmpty(param))
            {
                return user.UserPackage[param] != null;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemHole(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] splitParam = SplitParam(param, 2);
            if (param.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"ExecuteActionItemHole invalid [{param}] param split length for action {action.Identity}");
                return false;
            }

            string opt = splitParam[0];
            if (!int.TryParse(splitParam[1], out var value))
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"ExecuteActionItemHole invalid value number [{param}] for action {action.Identity}");
                return false;
            }

            Item target = user.UserPackage[Item.ItemPosition.RightHand];
            if (target == null)
            {
                await user.SendAsync(Language.StrItemErrRepairItem);
                return false;
            }

            if (opt.Equals("chkhole", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == 1)
                    return target.SocketOne > Item.SocketGem.NoSocket;
                if (value == 2)
                    return target.SocketTwo > Item.SocketGem.NoSocket;
                return false;
            }

            if (opt.Equals("makehole", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == 1 && target.SocketOne == Item.SocketGem.NoSocket)
                {
                    target.SocketOne = Item.SocketGem.EmptySocket;
                }
                else if (value == 2 && target.SocketTwo == Item.SocketGem.NoSocket)
                {
                    target.SocketTwo = Item.SocketGem.EmptySocket;
                }
                else
                {
                    return false;
                }

                await user.SendAsync(new MsgItemInfo(target, MsgItemInfo.ItemMode.Update));
                _ = await target.SaveAsync();
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemMultidel(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] splitParams = SplitParam(param);
            int first = 0;
            int last = 0;
            byte amount = 0;

            if (action.Data == Item.TYPE_METEOR)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                    amount = 1;
                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                    first = 0; // bound check
                // todo set meteor bind check
                return await user.UserPackage.SpendMeteorsAsync(amount);
            }

            if (action.Data == Item.TYPE_DRAGONBALL)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                    amount = 1;
                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                    first = 0;
                return await user.UserPackage.SpendDragonBallsAsync(amount, first != 0);
            }

            if (action.Data != 0)
                return false; // only Mets and DBs are supported

            if (splitParams.Length < 3)
                return false; // invalid format

            first = int.Parse(splitParams[0]);
            last = int.Parse(splitParams[1]);
            amount = byte.Parse(splitParams[2]);

            if (splitParams.Length < 4)
                return await user.UserPackage.MultiSpendItemAsync((uint)first, (uint)last, amount, true);
            return await user.UserPackage.MultiSpendItemAsync((uint)first, (uint)last, amount,
                int.Parse(splitParams[3]) != 0);
        }

        private static async Task<bool> ExecuteActionItemMultichk(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] splitParams = SplitParam(param);
            int first = 0;
            int last = 0;
            byte amount = 0;

            if (action.Data == Item.TYPE_METEOR)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                    amount = 1;
                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                    first = 0; // bound check
                // todo set meteor bind check
                return user.UserPackage.MeteorAmount() >= amount;
            }

            if (action.Data == Item.TYPE_DRAGONBALL)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                    amount = 1;
                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                    first = 0;
                return user.UserPackage.DragonBallAmount(first != 0) >= amount;
            }

            if (action.Data != 0)
                return false; // only Mets and DBs are supported

            if (splitParams.Length < 3)
                return false; // invalid format

            first = int.Parse(splitParams[0]);
            last = int.Parse(splitParams[1]);
            amount = byte.Parse(splitParams[2]);

            if (splitParams.Length < 4)
                return user.UserPackage.MultiCheckItem((uint)first, (uint)last, amount, true);
            return user.UserPackage.MultiCheckItem((uint)first, (uint)last, amount, int.Parse(splitParams[3]) != 0);
        }

        private static async Task<bool> ExecuteActionItemLeavespace(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            return user?.UserPackage != null && user.UserPackage.IsPackSpare((int)action.Data);
        }

        private static async Task<bool> ExecuteActionItemUpequipment(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
                return false;

            string szCmd = pszParam[0];
            byte nPos = byte.Parse(pszParam[1]);

            Item pItem = user.UserPackage[(Item.ItemPosition)nPos];
            if (pItem == null)
                return false;

            switch (szCmd)
            {
                case "up_lev":
                    {
                        return await pItem.UpEquipmentLevelAsync();
                    }

                case "recover_dur":
                    {
                        var szPrice = (uint)pItem.GetRecoverDurCost();
                        return await user.SpendMoneyAsync((int)szPrice) && await pItem.RecoverDurabilityAsync();
                    }

                case "up_levultra":
                case "up_levultra2":
                    {
                        return await pItem.UpUltraEquipmentLevelAsync();
                    }

                case "up_quality":
                    {
                        return await pItem.UpItemQualityAsync();
                    }

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"ERROR: [509] [0] [{param}] not properly handled.");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionItemEquiptest(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            /* param: position type opt value (4 quality == 9) */
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 4)
                return false;

            byte nPosition = byte.Parse(pszParam[0]);
            string szCmd = pszParam[1];
            string szOpt = pszParam[2];
            int nData = int.Parse(pszParam[3]);

            Item pItem = user.UserPackage[(Item.ItemPosition)nPosition];
            if (pItem == null)
                return false;

            int nTestData = 0;
            switch (szCmd)
            {
                case "level":
                    nTestData = pItem.GetLevel();
                    break;
                case "quality":
                    nTestData = pItem.GetQuality();
                    break;
                case "durability":
                    if (nData == -1)
                        nData = pItem.MaximumDurability / 100;
                    nTestData = pItem.MaximumDurability / 100;
                    break;
                case "max_dur":
                    {
                        if (nData == -1)
                            nData = pItem.Itemtype.AmountLimit / 100;
                        // TODO Kylin Gem Support
                        nTestData = pItem.MaximumDurability / 100;
                        break;
                    }

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"ACTION: EQUIPTEST error {szCmd}");
                    return false;
            }

            if (szOpt == "==")
                return nTestData == nData;
            if (szOpt == "<=")
                return nTestData <= nData;
            if (szOpt == ">=")
                return nTestData >= nData;
            return false;
        }

        private static async Task<bool> ExecuteActionItemEquipexist(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] Params = SplitParam(param);
            if (param.Length >= 1 && user.UserPackage[(Item.ItemPosition)action.Data] != null)
                return user.UserPackage[(Item.ItemPosition)action.Data].GetItemSubType() == ushort.Parse(Params[0]);
            return user.UserPackage[(Item.ItemPosition)action.Data] != null;
        }

        private static async Task<bool> ExecuteActionItemEquipcolor(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
                return false;
            if (!Enum.IsDefined(typeof(Item.ItemColor), byte.Parse(pszParam[1])))
                return false;

            Item pItem = user.UserPackage[(Item.ItemPosition)byte.Parse(pszParam[0])];
            if (pItem == null)
                return false;

            Item.ItemPosition pos = pItem.GetPosition();
            if (pos != Item.ItemPosition.Armor
                && pos != Item.ItemPosition.Headwear
                && (pos != Item.ItemPosition.LeftHand || pItem.GetItemSort() != Item.ItemSort.ItemsortWeaponShield))
                return false;

            pItem.Color = (Item.ItemColor)byte.Parse(pszParam[1]);
            _ = await pItem.SaveAsync();
            await user.SendAsync(new MsgItemInfo(pItem, MsgItemInfo.ItemMode.Update));
            return true;
        }

        private static async Task<bool> ExecuteActionItemCheckrand(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 6)
                return false;

            byte initValue = byte.Parse(pszParam[3]), endValue = byte.Parse(pszParam[5]);

            List<Item.ItemPosition> lPos = new List<Item.ItemPosition>(15);

            byte pIdx = byte.Parse(pszParam[1]);

            if (initValue == 0 && pIdx == 14)
                initValue = 1;

            for (Item.ItemPosition i = Item.ItemPosition.EquipmentBegin; i <= Item.ItemPosition.EquipmentEnd; i++)
            {
                if (user.UserPackage[i] != null)
                {
                    if (pIdx == 14 && user.UserPackage[i].Position == Item.ItemPosition.Steed)
                        continue;

                    if (user.UserPackage[i].IsArrowSort())
                        continue;

                    switch (pIdx)
                    {
                        case 14:
                            if (user.UserPackage[i].ReduceDamage >= initValue
                                && user.UserPackage[i].ReduceDamage <= endValue)
                                continue;
                            break;
                    }

                    lPos.Add(i);
                }
            }

            byte pos = 0;

            if (lPos.Count > 0)
                pos = (byte)lPos[await Kernel.NextAsync(lPos.Count) % lPos.Count];

            if (pos == 0)
                return false;

            Item pItem = user.UserPackage[(Item.ItemPosition)pos];
            if (pItem == null)
                return false;

            byte pPos = byte.Parse(pszParam[0]);
            string opt = pszParam[2];

            switch (pIdx)
            {
                case 14: // bless
                    user.VarData[7] = pos;
                    return true;
                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"ACTION: 516: {pIdx} not handled id: {action.Identity}");
                    break;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemModify(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            // structure param:
            // pos  type    action  value   update
            // 1    7       ==      1       1
            // pos = Item Position
            // type = 7 Reduce Damage
            // action = Operator == or set
            // value = value lol
            // update = if the client will update live

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"ACTION: incorrect param, pos type action value update, for action (id:{action.Identity})");
                return false;
            }

            int pos = int.Parse(pszParam[0]);
            int type = int.Parse(pszParam[1]);
            string opt = pszParam[2];
            long value = int.Parse(pszParam[3]);
            bool update = int.Parse(pszParam[4]) > 0;

            Item pItem = user.UserPackage[(Item.ItemPosition)pos];
            if (pItem == null)
            {
                await user.SendAsync(Language.StrUnableToUseItem);
                return false;
            }

            switch (type)
            {
                case 1: // itemtype
                    {
                        if (opt == "set")
                        {
                            DbItemtype itemt = Kernel.ItemManager.GetItemtype((uint)value);
                            if (itemt == null)
                            {
                                // new item doesnt exist
                                await Log.WriteLogAsync(LogLevel.Error,
                                    $"ACTION: itemtype not found (type:{value}, action:{action.Identity})");
                                return false;
                            }

                            if (pItem.Type / 1000 != itemt.Type / 1000)
                            {
                                await Log.WriteLogAsync(LogLevel.Error,
                                    $"ACTION: cant change to different type (type:{pItem.Type}, new:{value}, action:{action.Identity})");
                                return false;
                            }

                            if (!await pItem.ChangeTypeAsync(itemt.Type))
                                return false;
                        }
                        else if (opt == "==")
                        {
                            return pItem.Type == value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.Type < value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                case 2: // owner id
                case 3: // player id
                    return false;
                case 4: // dura
                    {
                        if (opt == "set")
                        {
                            if (value > ushort.MaxValue)
                                value = ushort.MaxValue;
                            else if (value < 0)
                                value = 0;

                            pItem.Durability = (ushort)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.Durability == value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.Durability < value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                case 5: // max dura
                    {
                        if (opt == "set")
                        {
                            if (value > ushort.MaxValue)
                                value = ushort.MaxValue;
                            else if (value < 0)
                                value = 0;

                            if (value < pItem.Durability)
                                pItem.Durability = (ushort)value;

                            pItem.MaximumDurability = (ushort)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.MaximumDurability == value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.MaximumDurability < value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                case 6:
                case 7: // position
                    {
                        return false;
                    }

                case 8: // gem1
                    {
                        if (opt == "set")
                        {
                            pItem.SocketOne = (Item.SocketGem)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.SocketOne == (Item.SocketGem)value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.SocketOne < (Item.SocketGem)value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                case 9: // gem2
                    {
                        if (opt == "set")
                        {
                            pItem.SocketTwo = (Item.SocketGem)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.SocketTwo == (Item.SocketGem)value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.SocketTwo < (Item.SocketGem)value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                case 10: // magic1
                    {
                        if (opt == "set")
                        {
                            if (value < 200 || value > 203)
                                return false;
                            pItem.Effect = (Item.ItemEffect)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.Effect == (Item.ItemEffect)value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                case 11: // magic2
                    return false;
                case 12: // magic3
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                                value = 0;
                            else if (value > 12)
                                value = 12;

                            _ = pItem.ChangeAddition((byte)value);
                        }
                        else if (opt == "==")
                        {
                            return pItem.Plus == value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.Plus < value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                //case 13: // data
                //    {
                //        if (opt == "set")
                //        {
                //            if (value < 0)
                //                value = 0;
                //            else if (value > 20000)
                //                value = 20000;

                //            pItem.SocketProgress = (ushort)value;
                //        }
                //        else if (opt == "==")
                //        {
                //            return pItem.SocketProgress == value;
                //        }
                //        else if (opt == "<")
                //        {
                //            return pItem.SocketProgress < value;
                //        }
                //        else
                //        {
                //            return false;
                //        }

                //        break;
                //    }

                case 14: // reduce damage
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                                value = 0;
                            else if (value > 7)
                                value = 7;

                            pItem.ReduceDamage = (byte)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.ReduceDamage == value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.ReduceDamage < value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                case 15: // add life
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                                value = 0;
                            else if (value > 255)
                                value = 255;

                            pItem.Enchantment = (byte)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.Enchantment == value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.Enchantment < value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                case 16: // anti monster
                case 17: // chk sum
                case 18: // plunder
                case 19: // special flag
                    return false;
                case 20: // color
                    {
                        if (opt == "set")
                        {
                            if (!Enum.IsDefined(typeof(Item.ItemColor), value))
                                return false;

                            pItem.Color = (Item.ItemColor)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.Color == (Item.ItemColor)value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.Color < (Item.ItemColor)value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }

                //case 21: // add lev exp
                //    {
                //        if (opt == "set")
                //        {
                //            if (value < 0)
                //                value = 0;
                //            if (value > ushort.MaxValue)
                //                value = ushort.MaxValue;

                //            pItem.AdditionProgress = (ushort)value;
                //        }
                //        else if (opt == "==")
                //        {
                //            return pItem.AdditionProgress == value;
                //        }
                //        else if (opt == "<")
                //        {
                //            return pItem.AdditionProgress < value;
                //        }
                //        else
                //        {
                //            return false;
                //        }

                //        break;
                //    }
                default:
                    return false;
            }

            _ = await pItem.SaveAsync();
            if (update)
                await user.SendAsync(new MsgItemInfo(pItem, MsgItemInfo.ItemMode.Update));
            return true;
        }

        private static async Task<bool> ExecuteActionItemJarCreate(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (!user.UserPackage.IsPackSpare(1))
                return false;

            if (user.UserPackage.GetItemByType(Item.TYPE_JAR) != null)
                _ = await user.UserPackage.SpendItemAsync(user.UserPackage.GetItemByType(Item.TYPE_JAR));

            var itemtype = Kernel.ItemManager.GetItemtype(action.Data);
            if (itemtype == null)
                return false;

            string[] pszParam = SplitParam(param);

            var newItem = new DbItem
            {
                AddLife = 0,
                AddlevelExp = 0,
                AntiMonster = 0,
                ChkSum = 0,
                Color = 3,
                Data = 0,
                Gem1 = 0,
                Gem2 = 0,
                Ident = 0,
                Magic1 = 0,
                Magic2 = 0,
                ReduceDmg = 0,
                Plunder = null,
                Specialflag = 0,
                Type = itemtype.Type,
                Position = 0,
                PlayerId = user.Identity,
                Monopoly = 0,
                Magic3 = itemtype.Magic3,
                Amount = 0,
                AmountLimit = 0
            };
            for (int i = 0; i < pszParam.Length; i++)
            {
                uint value = uint.Parse(pszParam[i]);
                if (value <= 0) continue;

                switch (i)
                {
                    case 0:
                        newItem.Amount = (ushort)value;
                        break;
                    case 1:
                        newItem.AmountLimit = (ushort)value; //(ushort) (1 << ((ushort) value));
                        break;
                    case 2:
                        // Socket Progress
                        newItem.Data = value;
                        break;
                    case 3:
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte)value))
                            newItem.Gem1 = (byte)value;
                        break;
                    case 4:
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte)value))
                            newItem.Gem2 = (byte)value;
                        break;
                    case 5:
                        if (Enum.IsDefined(typeof(Item.ItemEffect), (ushort)value))
                            newItem.Magic1 = (byte)value;
                        break;
                    case 6:
                        // magic2.. w/e
                        break;
                    case 7:
                        if (value > 0 && value < 256)
                            newItem.Magic3 = (byte)value;
                        break;
                    case 8:
                        if (value > 0
                            && value < 8)
                            newItem.ReduceDmg = (byte)value;
                        break;
                    case 9:
                        if (value > 0
                            && value < 256)
                            newItem.AddLife = (byte)value;
                        break;
                    case 10:
                        newItem.Plunder = null;
                        break;
                    case 11:
                        if (value == 0)
                            value = 3;
                        if (Enum.IsDefined(typeof(Item.ItemColor), value))
                            newItem.Color = (byte)value;
                        break;
                    case 12:
                        if (value > 0 && value < 256)
                            newItem.Monopoly = (byte)value;
                        break;
                    case 13:
                    case 14:
                    case 15:
                        // R -> For Steeds only
                        // G -> For Steeds only
                        // B -> For Steeds only
                        // G == 8 R == 16
                        newItem.Data = value | (uint.Parse(pszParam[14]) << 8) | (uint.Parse(pszParam[13]) << 16);
                        break;
                }
            }

            Item pItem = new Item(user);
            if (!await pItem.CreateAsync(newItem))
                return false;

            _ = await user.UserPackage.AddItemAsync(pItem);
            return true;
        }

        private static async Task<bool> ExecuteActionItemJarVerify(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (!user.UserPackage.IsPackSpare(1))
                return false;

            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2) return false;

            uint amount = uint.Parse(pszParam[1]);
            uint monster = uint.Parse(pszParam[0]);

            Item jar = user.UserPackage.GetItemByType(action.Data);
            return jar != null && jar.MaximumDurability == monster && amount <= jar.Data;
        }

        #endregion

        #region Syndicate

        private static async Task<bool> ExecuteActionSynCreate(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null || user.Syndicate != null)
                return false;

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Invalid param count for guild creation: {param}, {action.Identity}");
                return false;
            }

            if (!int.TryParse(splitParam[0], out var level))
            {
                return false;
            }

            if (user.Level < level)
            {
                await user.SendAsync(Language.StrNotEnoughLevel);
                return false;
            }

            if (!int.TryParse(splitParam[1], out var price))
            {
                return false;
            }

            if (user.Silvers < price)
            {
                await user.SendAsync(Language.StrNotEnoughMoney);
                return false;
            }

            return await user.CreateSyndicateAsync(input, price);
        }


        private static async Task<bool> ExecuteActionSynDestroy(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                await user.SendAsync(Language.StrSynNotLeader);
                return false;
            }

            return await user.DisbandSyndicateAsync();
        }

        private static async Task<bool> ExecuteActionSynSetAssistant(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;
            return await user.Syndicate.PromoteAsync(user, input, SyndicateMember.SyndicateRank.DeputyLeader);
        }

        private static async Task<bool> ExecuteActionSynClearRank(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;
            return await user.Syndicate.DemoteAsync(user, input);
        }

        private static async Task<bool> ExecuteActionSynChangeLeader(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;
            return await user.Syndicate.PromoteAsync(user, input, SyndicateMember.SyndicateRank.GuildLeader);
        }

        private static async Task<bool> ExecuteActionSynAntagonize(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            Syndicate target = Kernel.SyndicateManager.GetSyndicate(input);
            if (target == null)
                return false;

            if (target.Identity == user.SyndicateIdentity)
                return false;

            return await user.Syndicate.AntagonizeAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynClearAntagonize(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            Syndicate target = Kernel.SyndicateManager.GetSyndicate(input);
            if (target == null)
                return false;

            if (target.Identity == user.SyndicateIdentity)
                return false;

            return await user.Syndicate.PeaceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynAlly(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            Syndicate target = user.Team?.Members.FirstOrDefault(x => x.Identity != user.Identity)?.Syndicate;
            if (target == null)
                return false;

            if (target.Identity == user.SyndicateIdentity)
                return false;

            return await user.Syndicate.CreateAllianceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynClearAlly(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            Syndicate target = Kernel.SyndicateManager.GetSyndicate(input);
            if (target == null)
                return false;

            if (target.Identity == user.SyndicateIdentity)
                return false;

            return await user.Syndicate.DisbandAllianceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynAttr(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            string[] splitParam = SplitParam(param, 4);
            if (splitParam.Length < 3)
                return false;

            string field = splitParam[0],
                opt = splitParam[1];
            long value = long.Parse(splitParam[2]);

            Syndicate target = null;
            if (splitParam.Length < 4)
                target = user.Syndicate;
            else
                target = Kernel.SyndicateManager.GetSyndicate(int.Parse(splitParam[3]));

            if (target == null)
                return true;

            int data = 0;
            if (field.Equals("money", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt.Equals("+="))
                {
                    if (target.Money + value < 0)
                        return false;

                    target.Money = (int)Math.Max(0, target.Money + value);
                    return await target.SaveAsync();
                }

                data = target.Money;
            }
            else if (field.Equals("membernum", StringComparison.InvariantCultureIgnoreCase))
            {
                data = target.MemberCount;
            }

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">": return data > value;
                case "<": return data < value;
            }

            return false;
        }

        #endregion

        #region Monster

        private static async Task<bool> ExecuteActionMstDropitem(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (role == null || !(role is Monster monster))
                return false;

            string[] splitParam = SplitParam(param, 2);
            if (splitParam.Length < 2)
                return false;

            string ope = splitParam[0];
            uint data = uint.Parse(splitParam[1]);

            int percent = 100;
            if (splitParam.Length >= 3)
                percent = int.Parse(splitParam[2]);

            if (ope.Equals("dropitem"))
            {
                await monster.DropItemAsync(data, user);
                return true;
            }

            if (ope.Equals("dropmoney"))
            {
                percent %= 100;
                uint dwMoneyDrop = (uint)(data * (percent + await Kernel.NextAsync(100 - percent)) / 100);
                if (dwMoneyDrop <= 0)
                    return false;
                uint idUser = user?.Identity ?? 0u;
                await monster.DropMoneyAsync(dwMoneyDrop, idUser);
                return true;
            }

            return false;
        }

        #endregion

        #region User

        private static async Task<bool> ExecuteUserAttr(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] parsedParam = SplitParam(param);
            if (parsedParam.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"GameAction::ExecuteUserAttr[{action.Identity}] invalid param num {param}");
                return false;
            }

            string type = "", opt = "", value = "", last = "";
            type = parsedParam[0];
            opt = parsedParam[1];
            value = parsedParam[2];
            if (parsedParam.Length > 3)
                last = parsedParam[3];

            switch (type.ToLower())
            {
                #region Force (>, >=, <, <=, =, +=, set)

                case "force":
                case "strength":
                    int forceValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Strength > forceValue;
                    if (opt.Equals(">="))
                        return user.Strength >= forceValue;
                    if (opt.Equals("<"))
                        return user.Strength < forceValue;
                    if (opt.Equals("<="))
                        return user.Strength <= forceValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Strength == forceValue;
                    if (opt.Equals("+="))
                    {
                        _ = await user.AddAttributesAsync(ClientUpdateType.Strength, forceValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Strength, (ulong)forceValue);
                        return true;
                    }

                    break;

                #endregion

                #region Speed (>, >=, <, <=, =, +=, set)

                case "agility":
                case "speed":
                    int speedValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Agility > speedValue;
                    if (opt.Equals(">="))
                        return user.Agility >= speedValue;
                    if (opt.Equals("<"))
                        return user.Agility < speedValue;
                    if (opt.Equals("<="))
                        return user.Agility <= speedValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Agility == speedValue;
                    if (opt.Equals("+="))
                    {
                        _ = await user.AddAttributesAsync(ClientUpdateType.Agility, speedValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Agility, (ulong)speedValue);
                        return true;
                    }

                    break;

                #endregion

                #region Health (>, >=, <, <=, =, +=, set)

                case "vitality":
                case "health":
                    int healthValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Vitality > healthValue;
                    if (opt.Equals(">="))
                        return user.Vitality >= healthValue;
                    if (opt.Equals("<"))
                        return user.Vitality < healthValue;
                    if (opt.Equals("<="))
                        return user.Vitality <= healthValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Vitality == healthValue;
                    if (opt.Equals("+="))
                    {
                        _ = await user.AddAttributesAsync(ClientUpdateType.Vitality, healthValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Vitality, (ulong)healthValue);
                        return true;
                    }

                    break;

                #endregion

                #region Soul (>, >=, <, <=, =, +=, set)

                case "spirit":
                case "soul":
                    int soulValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Spirit > soulValue;
                    if (opt.Equals(">="))
                        return user.Spirit >= soulValue;
                    if (opt.Equals("<"))
                        return user.Spirit < soulValue;
                    if (opt.Equals("<="))
                        return user.Spirit <= soulValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Spirit == soulValue;
                    if (opt.Equals("+="))
                    {
                        _ = await user.AddAttributesAsync(ClientUpdateType.Spirit, soulValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Spirit, (ulong)soulValue);
                        return true;
                    }

                    break;

                #endregion

                #region Attribute Points (>, >=, <, <=, =, +=, set)

                case "attr_points":
                case "attr":
                    int attrValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.AttributePoints > attrValue;
                    if (opt.Equals(">="))
                        return user.AttributePoints >= attrValue;
                    if (opt.Equals("<"))
                        return user.AttributePoints < attrValue;
                    if (opt.Equals("<="))
                        return user.AttributePoints <= attrValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.AttributePoints == attrValue;
                    if (opt.Equals("+="))
                    {
                        _ = await user.AddAttributesAsync(ClientUpdateType.Atributes, attrValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Atributes, (ulong)attrValue);
                        return true;
                    }

                    break;

                #endregion

                #region Level (>, >=, <, <=, =, +=, set)

                case "level":
                    int levelValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Level > levelValue;
                    if (opt.Equals(">="))
                        return user.Level >= levelValue;
                    if (opt.Equals("<"))
                        return user.Level < levelValue;
                    if (opt.Equals("<="))
                        return user.Level <= levelValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Level == levelValue;
                    if (opt.Equals("+="))
                    {
                        _ = await user.AddAttributesAsync(ClientUpdateType.Level, levelValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Level, (ulong)levelValue);
                        return true;
                    }

                    break;

                #endregion

                #region Metempsychosis (>, >=, <, <=, =, +=, set)

                case "metempsychosis":
                    int metempsychosisValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Metempsychosis > metempsychosisValue;
                    if (opt.Equals(">="))
                        return user.Metempsychosis >= metempsychosisValue;
                    if (opt.Equals("<"))
                        return user.Metempsychosis < metempsychosisValue;
                    if (opt.Equals("<="))
                        return user.Metempsychosis <= metempsychosisValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Metempsychosis == metempsychosisValue;
                    if (opt.Equals("+="))
                    {
                        _ = await user.AddAttributesAsync(ClientUpdateType.Reborn, metempsychosisValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Reborn, (ulong)metempsychosisValue);
                        return true;
                    }

                    break;

                #endregion

                #region Money (>, >=, <, <=, =, +=, set)

                case "money":
                case "silver":
                    int moneyValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Silvers > moneyValue;
                    if (opt.Equals(">="))
                        return user.Silvers >= moneyValue;
                    if (opt.Equals("<"))
                        return user.Silvers < moneyValue;
                    if (opt.Equals("<="))
                        return user.Silvers <= moneyValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Silvers == moneyValue;
                    if (opt.Equals("+="))
                    {
                        return await user.ChangeMoneyAsync(moneyValue);
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Money, (ulong)moneyValue);
                        return true;
                    }

                    break;

                #endregion

                #region Emoney (>, >=, <, <=, =, +=, set)

                case "emoney":
                case "e_money":
                case "cps":
                    int emoneyValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.ConquerPoints > emoneyValue;
                    if (opt.Equals(">="))
                        return user.ConquerPoints >= emoneyValue;
                    if (opt.Equals("<"))
                        return user.ConquerPoints < emoneyValue;
                    if (opt.Equals("<="))
                        return user.ConquerPoints <= emoneyValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.ConquerPoints == emoneyValue;
                    if (opt.Equals("+="))
                    {
                        return await user.ChangeConquerPointsAsync(emoneyValue);
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.ConquerPoints, (ulong)emoneyValue);
                        return true;
                    }

                    break;

                #endregion

                #region Rankshow (>, >=, <, <=, =)

                case "rank":
                case "rankshow":
                case "rank_show":
                    int rankShowValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.SyndicateRank > (SyndicateMember.SyndicateRank)rankShowValue;
                    if (opt.Equals(">="))
                        return user.SyndicateRank >= (SyndicateMember.SyndicateRank)rankShowValue;
                    if (opt.Equals("<"))
                        return user.SyndicateRank < (SyndicateMember.SyndicateRank)rankShowValue;
                    if (opt.Equals("<="))
                        return user.SyndicateRank <= (SyndicateMember.SyndicateRank)rankShowValue;
                    if (opt.Equals("==") || opt.Equals("="))
                        return user.SyndicateRank == (SyndicateMember.SyndicateRank)rankShowValue;
                    break;

                #endregion

                #region Syn Time (>, >=, <, <=, =)

                case "syn_time":
                    {
                        int synTime = int.Parse(value);
                        if (user.Syndicate == null)
                            return false;

                        int synDays = (int)(DateTime.Now - user.Syndicate.CreationDate).TotalDays;
                        if (opt.Equals("==") || opt.Equals("="))
                            return synDays == synTime;
                        if (opt.Equals(">="))
                            return synDays >= synTime;
                        if (opt.Equals(">"))
                            return synDays > synTime;
                        if (opt.Equals("<="))
                            return synDays <= synTime;
                        if (opt.Equals("<"))
                            return synDays < synTime;
                        break;
                    }

                #endregion

                #region Syn User Time (>, >=, <, <=, =)

                case "syn_user_time":
                    {
                        int synTime = int.Parse(value);
                        if (user.Syndicate == null)
                            return false;

                        int synDays = (int)(DateTime.Now - user.SyndicateMember.JoinDate).TotalDays;
                        if (opt.Equals("==") || opt.Equals("="))
                            return synDays == synTime;
                        if (opt.Equals(">="))
                            return synDays >= synTime;
                        if (opt.Equals(">"))
                            return synDays > synTime;
                        if (opt.Equals("<="))
                            return synDays <= synTime;
                        if (opt.Equals("<"))
                            return synDays < synTime;
                        break;
                    }

                #endregion

                #region Experience (>, >=, <, <=, =, +=, set)

                case "exp":
                case "experience":
                    ulong expValue = ulong.Parse(value);
                    if (opt.Equals(">"))
                        return user.Experience > expValue;
                    if (opt.Equals(">="))
                        return user.Experience >= expValue;
                    if (opt.Equals("<"))
                        return user.Experience < expValue;
                    if (opt.Equals("<="))
                        return user.Experience <= expValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Experience == expValue;
                    if (opt.Equals("+="))
                    {
                        return await user.AwardExperienceAsync((long)expValue, last.Equals("nocontribute"));
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Experience, (ulong)expValue);
                        return true;
                    }

                    break;

                #endregion

                #region Stamina (>, >=, <, <=, =, +=, set)

                case "ep":
                case "energy":
                case "stamina":
                    int energyValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Energy > energyValue;
                    if (opt.Equals(">="))
                        return user.Energy >= energyValue;
                    if (opt.Equals("<"))
                        return user.Energy < energyValue;
                    if (opt.Equals("<="))
                        return user.Energy <= energyValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Energy == energyValue;
                    if (opt.Equals("+="))
                    {
                        return await user.AddAttributesAsync(ClientUpdateType.Stamina, energyValue);
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Stamina, (ulong)energyValue);
                        return true;
                    }

                    break;

                #endregion

                #region Life (>, >=, <, <=, =, +=, set)

                case "life":
                case "hp":
                    int lifeValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Life > lifeValue;
                    if (opt.Equals(">="))
                        return user.Life >= lifeValue;
                    if (opt.Equals("<"))
                        return user.Life < lifeValue;
                    if (opt.Equals("<="))
                        return user.Life <= lifeValue;
                    if (opt.Equals("=="))
                        return user.Life == lifeValue;
                    if (opt.Equals("+="))
                    {
                        user.QueueAction(async () =>
                        {
                            _ = await user.AddAttributesAsync(ClientUpdateType.Hitpoints, lifeValue);
                            if (!user.IsAlive)
                                await user.BeKillAsync(null);
                        });
                        return true;
                    }

                    if (opt.Equals("set") || opt.Equals("="))
                    {
                        user.QueueAction(async () =>
                        {
                            _ = await user.SetAttributesAsync(ClientUpdateType.Hitpoints, (ulong)lifeValue);
                            if (!user.IsAlive)
                                await user.BeKillAsync(null);
                        });
                        return true;
                    }

                    break;

                #endregion

                #region Mana (>, >=, <, <=, =, +=, set)

                case "mana":
                case "mp":
                    int manaValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Mana > manaValue;
                    if (opt.Equals(">="))
                        return user.Mana >= manaValue;
                    if (opt.Equals("<"))
                        return user.Mana < manaValue;
                    if (opt.Equals("<="))
                        return user.Mana <= manaValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Mana == manaValue;
                    if (opt.Equals("+="))
                    {
                        return await user.AddAttributesAsync(ClientUpdateType.Mana, manaValue);
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Mana, (ulong)manaValue);
                        return true;
                    }

                    break;

                #endregion

                #region Pk (>, >=, <, <=, =, +=, set)

                case "pk":
                case "pkp":
                    int pkValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.PkPoints > pkValue;
                    if (opt.Equals(">="))
                        return user.PkPoints >= pkValue;
                    if (opt.Equals("<"))
                        return user.PkPoints < pkValue;
                    if (opt.Equals("<="))
                        return user.PkPoints <= pkValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.PkPoints == pkValue;
                    if (opt.Equals("+="))
                    {
                        return await user.AddAttributesAsync(ClientUpdateType.PkPoints, pkValue);
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.PkPoints, (ulong)pkValue);
                        return true;
                    }

                    break;

                #endregion

                #region Profession (>, >=, <, <=, =, +=, set)

                case "profession":
                case "pro":
                    int proValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Profession > proValue
                            ;
                    if (opt.Equals(">="))
                        return user.Profession >= proValue
                            ;
                    if (opt.Equals("<"))
                        return user.Profession < proValue
                            ;
                    if (opt.Equals("<="))
                        return user.Profession <= proValue
                            ;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Profession == proValue
                            ;
                    if (opt.Equals("+="))
                    {
                        return await user.AddAttributesAsync(ClientUpdateType.Class, proValue);
                    }

                    if (opt.Equals("set"))
                    {
                        _ = await user.SetAttributesAsync(ClientUpdateType.Class, (ulong)proValue);
                        return true;
                    }

                    break;

                #endregion

                #region Transform (>, >=, <, <=, =, ==)

                case "transform":
                    int transformValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.TransformationMesh > transformValue;
                    if (opt.Equals(">="))
                        return user.TransformationMesh >= transformValue;
                    if (opt.Equals("<"))
                        return user.TransformationMesh < transformValue;
                    if (opt.Equals("<="))
                        return user.TransformationMesh <= transformValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.TransformationMesh == transformValue;
                    break;

                #endregion

                #region Virtue (>, >=, <, <=, =, +=, set)

                case "virtue":
                case "vp":
                    int virtueValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.VirtuePoints > virtueValue;
                    if (opt.Equals(">="))
                        return user.VirtuePoints >= virtueValue;
                    if (opt.Equals("<"))
                        return user.VirtuePoints < virtueValue;
                    if (opt.Equals("<="))
                        return user.VirtuePoints <= virtueValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.VirtuePoints == virtueValue;
                    if (opt.Equals("+="))
                    {
                        if (virtueValue > 0)
                            user.VirtuePoints += (uint)virtueValue;
                        else
                            user.VirtuePoints -= (uint)(virtueValue * -1);
                        return await user.SaveAsync();
                    }

                    if (opt.Equals("set"))
                    {
                        user.VirtuePoints = (uint)virtueValue;
                        return await user.SaveAsync();
                    }

                    break;

                #endregion

                #region Vip (>, >=, <, <=, =, ==)

                case "vip":
                    {
                        int vipValue = int.Parse(value);
                        if (opt.Equals(">"))
                            return user.VipLevel > vipValue;
                        if (opt.Equals(">="))
                            return user.VipLevel >= vipValue;
                        if (opt.Equals("<"))
                            return user.VipLevel < vipValue;
                        if (opt.Equals("<="))
                            return user.VipLevel <= vipValue;
                        if (opt.Equals("=") || opt.Equals("=="))
                            return user.VipLevel == vipValue;
                        break;
                    }

                #endregion

                #region SVip (>, >=, <, <=, =, ==)

                case "svip":
                    {
                        int svipValue = int.Parse(value);
                        if (opt.Equals(">"))
                            return user.VipLevel > svipValue;
                        if (opt.Equals(">="))
                            return user.VipLevel >= svipValue;
                        if (opt.Equals("<"))
                            return user.VipLevel < svipValue;
                        if (opt.Equals("<="))
                            return user.VipLevel <= svipValue;
                        if (opt.Equals("=="))
                            return user.VipLevel == svipValue;
                        if (opt.Equals("set") || opt.Equals("="))
                        {
                            if (string.IsNullOrEmpty(last) || int.TryParse(last, out var minutes))
                                minutes = 1440; // 24 hours
                            uint vipValue = uint.Parse(value);
                            if (vipValue == user.VipLevel)
                            {
                                user.VipExpiration = user.VipExpiration < DateTime.Now
                                    ? DateTime.Now.AddMinutes(minutes)
                                    : user.VipExpiration.AddMinutes(minutes);
                            }
                            else
                            {
                                user.VipExpiration = DateTime.Now.AddMinutes(minutes);
                            }

                            _ = await user.SetAttributesAsync(ClientUpdateType.VipLevel, vipValue);
                            return true;
                        }

                        break;
                    }

                #endregion

                #region Xp (>, >=, <, <=, =, +=, set)

                case "xp":
                    int xpValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.XpPoints > xpValue;
                    if (opt.Equals(">="))
                        return user.XpPoints >= xpValue;
                    if (opt.Equals("<"))
                        return user.XpPoints < xpValue;
                    if (opt.Equals("<="))
                        return user.XpPoints <= xpValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.XpPoints == xpValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddXpAsync((byte)xpValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetXpAsync((byte)xpValue);
                        return true;
                    }

                    break;

                #endregion

                #region Iterator (>, >=, <, <=, =, +=, set)

                case "iterator":
                    int iteratorValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Iterator > iteratorValue;
                    if (opt.Equals(">="))
                        return user.Iterator >= iteratorValue;
                    if (opt.Equals("<"))
                        return user.Iterator < iteratorValue;
                    if (opt.Equals("<="))
                        return user.Iterator <= iteratorValue;
                    if (opt.Equals("=="))
                        return user.Iterator == iteratorValue;
                    if (opt.Equals("+="))
                    {
                        user.Iterator += iteratorValue;
                        return true;
                    }

                    if (opt.Equals("set") || opt == "=")
                    {
                        user.Iterator = iteratorValue;
                        return true;
                    }

                    break;

                #endregion

                #region Merchant (==, set)

                case "business":
                    int merchantValue = int.Parse(value);
                    if (opt.Equals("=="))
                        return user.Merchant == merchantValue;
                    if (opt.Equals("set") || opt == "=")
                    {
                        if (merchantValue == 0)
                        {
                            await user.RemoveMerchantAsync();
                        }
                        else
                        {
                            _ = await user.SetMerchantAsync();
                        }

                        return true;
                    }

                    break;

                #endregion

                #region Look (==, set)

                case "look":
                    {
                        switch (opt)
                        {
                            case "==": return (user.Mesh % 10) == ushort.Parse(value);
                            case "set":
                                {
                                    ushort usVal = ushort.Parse(value);
                                    if (user.Gender == 1 && (usVal == 3 || usVal == 4))
                                    {
                                        user.Body = (BodyType)(1000 + usVal);
                                        await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                        _ = await user.SaveAsync();
                                        return true;
                                    }

                                    if (user.Gender == 0 && (usVal == 1 || usVal == 2))
                                    {
                                        user.Body = (BodyType)(2000 + usVal);
                                        await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                        _ = await user.SaveAsync();
                                        return true;
                                    }

                                    return false;
                                }
                        }

                        return false;
                    }

                #endregion

                #region Body (set)

                case "body":
                    {
                        switch (opt)
                        {
                            case "set":
                                {
                                    ushort usNewBody = ushort.Parse(value);
                                    if (usNewBody == 1003 || usNewBody == 1004)
                                    {
                                        if (user.Body != BodyType.AgileFemale && user.Body != BodyType.MuscularFemale)
                                            return false; // to change body use the fucking item , asshole
                                    }

                                    if (usNewBody == 2001 || usNewBody == 2002)
                                    {
                                        if (user.Body != BodyType.AgileMale && user.Body != BodyType.MuscularMale)
                                            return false; // to change body use the fucking item , asshole
                                    }

                                    if (user.UserPackage[Item.ItemPosition.Garment] != null)
                                        _ = await user.UserPackage.UnEquipAsync(Item.ItemPosition.Garment);

                                    user.Body = (BodyType)usNewBody;
                                    await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                    _ = await user.SaveAsync();
                                    return true;
                                    ;
                                }
                        }

                        return false;
                    }

                    #endregion
            }

            return false;
        }

        private static async Task<bool> ExecuteUserFull(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            if (param.Equals("life", StringComparison.InvariantCultureIgnoreCase))
            {
                user.QueueAction(() => user.SetAttributesAsync(ClientUpdateType.Hitpoints, user.MaxLife));
                return true;
            }

            if (param.Equals("mana", StringComparison.InvariantCultureIgnoreCase))
            {
                user.QueueAction(() => user.SetAttributesAsync(ClientUpdateType.Mana, user.MaxMana));
                return true;
            }

            if (param.Equals("xp", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetXpAsync(100);
                _ = await user.BurstXpAsync();
                return true;
            }

            if (param.Equals("sp", StringComparison.InvariantCultureIgnoreCase))
            {
                _ = await user.SetAttributesAsync(ClientUpdateType.Stamina, user.MaxEnergy);
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteUserChgMap(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] paramStrings = SplitParam(param);
            if (paramStrings.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Action {action.Identity}:{action.Type} invalid param length: {param}");
                return false;
            }

            if (!uint.TryParse(paramStrings[0], out var idMap)
                || !ushort.TryParse(paramStrings[1], out var x)
                || !ushort.TryParse(paramStrings[2], out var y))
                return false;

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid map identity {idMap} for action {action.Identity}");
                return false;
            }

            if (!user.Map.IsTeleportDisable() ||
                (paramStrings.Length >= 4 && byte.TryParse(paramStrings[3], out var forceTeleport) && forceTeleport != 0))
            {
                return await user.FlyMapAsync(idMap, x, y);
            }

            return false;
        }

        private static async Task<bool> ExecuteUserRecordpoint(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] paramStrings = SplitParam(param);
            if (paramStrings.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Action {action.Identity}:{action.Type} invalid param length: {param}");
                return false;
            }

            if (!uint.TryParse(paramStrings[0], out var idMap)
                || !ushort.TryParse(paramStrings[1], out var x)
                || !ushort.TryParse(paramStrings[2], out var y))
                return false;

            if (idMap == 0)
            {
                await user.SavePositionAsync(user.MapIdentity, user.MapX, user.MapY);
                return true;
            }

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid map identity {idMap} for action {action.Identity}");
                return false;
            }

            await user.SavePositionAsync(idMap, x, y);
            return true;
        }

        private static async Task<bool> ExecuteUserHair(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param);

            if (splitParams.Length < 1)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Action {action.Identity}:{action.Type} has not enough argments: {param}");
                return false;
            }

            string cmd = "style";
            int value = 0;
            if (splitParams.Length > 1)
            {
                cmd = splitParams[0];
                value = int.Parse(splitParams[1]);
            }
            else
            {
                value = int.Parse(splitParams[0]);
            }

            if (cmd.Equals("style", StringComparison.InvariantCultureIgnoreCase))
            {
                _ = await user.SetAttributesAsync(ClientUpdateType.HairStyle,
                    (ushort)(value + (user.Hairstyle - (user.Hairstyle % 100))));
                return true;
            }

            if (cmd.Equals("color", StringComparison.InvariantCultureIgnoreCase))
            {
                _ = await user.SetAttributesAsync(ClientUpdateType.HairStyle,
                    (ushort)(user.Hairstyle % 100 + value * 100));
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteUserChgmaprecord(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            _ = await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
            return true;
        }

        private static async Task<bool> ExecuteActionUserChglinkmap(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Map == null)
                return false;

            if (user.IsPm())
                await user.SendAsync($"ExecuteActionUserChglinkmap");

            return true;
        }

        private static async Task<bool> ExecuteUserTransform(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 4)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Invalid param count for {action.Identity}:{action.Type}, {param}");
                return false;
            }

            uint transformation = uint.Parse(splitParam[2]);
            int time = int.Parse(splitParam[3]);
            return await user.TransformAsync(transformation, time, true);
        }

        private static async Task<bool> ExecuteActionUserIspure(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;
            return user.ProfessionSort == user.PreviousProfession / 10 &&
                   user.FirstProfession / 10 == user.ProfessionSort;
        }

        private static async Task<bool> ExecuteActionUserTalk(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Action type 1010 {action.Identity} no user");
                return false;
            }

            await user.SendAsync(param, (MsgTalk.TalkChannel)action.Data, Color.White);
            return true;
        }

        private static async Task<bool> ExecuteActionUserMagic(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Invalid ActionUserMagic param length: {action.Identity}, {param}");
                return false;
            }

            switch (splitParam[0].ToLowerInvariant())
            {
                case "check":
                    if (splitParam.Length >= 3)
                        return user.MagicData.CheckLevel(ushort.Parse(splitParam[1]), ushort.Parse(splitParam[2]));
                    return user.MagicData.CheckType(ushort.Parse(splitParam[1]));

                case "learn":
                    if (splitParam.Length >= 3)
                        return await user.MagicData.CreateAsync(ushort.Parse(splitParam[1]), byte.Parse(splitParam[2]));
                    return await user.MagicData.CreateAsync(ushort.Parse(splitParam[1]), 0);

                case "uplev":
                case "up_lev":
                case "uplevel":
                case "up_level":
                    return await user.MagicData.UpLevelByTaskAsync(ushort.Parse(splitParam[1]));

                case "addexp":
                    return await user.MagicData.AwardExpAsync(ushort.Parse(splitParam[1]), 0, int.Parse(splitParam[2]));

                default:
                    await Log.WriteLogAsync(LogLevel.Warning,
                        $"[ActionType: {action.Type}] Unknown {splitParam[0]} param {action.Identity}");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserWeaponSkill(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid param amount: {param} [{action.Identity}]");
                return false;
            }

            if (!ushort.TryParse(splitParam[1], out var type)
                || !int.TryParse(splitParam[2], out var value))
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Invalid weapon skill type {param} for action {action.Identity}");
                return false;
            }

            switch (splitParam[0].ToLowerInvariant())
            {
                case "check":
                    return user.WeaponSkill[type]?.Level >= value;

                case "learn":
                    return await user.WeaponSkill.CreateAsync(type, (byte)value);

                case "addexp":
                    await user.AddWeaponSkillExpAsync(type, value, true);
                    return true;

                default:
                    await Log.WriteLogAsync(LogLevel.Warning,
                        $"ExecuteActionUserWeaponSkill {splitParam[0]} invalid {action.Identity}");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserLog(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param, 2);

            if (splitParam.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionUserLog length [id:{action.Identity}], {param}");
                return true;
            }

            string file = splitParam[0];
            string message = splitParam[1];

            if (file.StartsWith("gmlog/"))
                file = file.Remove(0, "gmlog/".Length);

            await Log.GmLogAsync(file, message);
            return true;
        }

        private static async Task<bool> ExecuteActionUserBonus(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;
            return await user.DoBonusAsync();
        }

        private static async Task<bool> ExecuteActionUserDivorce(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            if (user.MateIdentity == 0)
                return false;

            Character mate = Kernel.RoleManager.GetUser(user.MateIdentity);
            if (mate == null)
            {
                DbCharacter dbMate = await CharactersRepository.FindByIdentityAsync(user.MateIdentity);
                if (dbMate == null)
                    return false;
                dbMate.Mate = 0;
                _ = await BaseRepository.SaveAsync(dbMate);
            }

            if (mate == null)
            {
                DbItem dbItem = Item.CreateEntity(Item.TYPE_METEORTEAR);
                dbItem.PlayerId = user.Identity;
                _ = await BaseRepository.SaveAsync(dbItem);
            }
            else
            {
                _ = await mate.UserPackage.AwardItemAsync(Item.TYPE_METEORTEAR);
            }

            user.MateIdentity = 0;
            user.MateName = Language.StrNone;
            _ = await user.SaveAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionUserMarriage(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            return user?.MateIdentity != 0;
        }

        private static async Task<bool> ExecuteActionUserSex(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            return user?.Gender != 0;
        }

        private static async Task<bool> ExecuteActionUserEffect(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] parsedString = SplitParam(param);
            if (parsedString.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"Invalid parsed param[{param}] ExecuteActionUserEffect[{action.Identity}]");
                return false;
            }

            MsgName msg = new MsgName
            {
                Identity = user.Identity,
                Action = StringAction.RoleEffect
            };
            msg.Strings.Add(parsedString[1]);
            switch (parsedString[0].ToLower())
            {
                case "self":
                    await user.BroadcastRoomMsgAsync(msg, true);
                    return true;

                case "couple":
                    await user.BroadcastRoomMsgAsync(msg, true);

                    Character couple = Kernel.RoleManager.GetUser(user.MateIdentity);
                    if (couple == null)
                        return true;

                    msg.Identity = couple.Identity;
                    await couple.BroadcastRoomMsgAsync(msg, true);
                    return true;

                case "team":
                    if (user.Team == null)
                        return false;

                    foreach (var member in user.Team.Members)
                    {
                        msg.Identity = member.Identity;
                        await member.BroadcastRoomMsgAsync(msg, true);
                    }

                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskmask(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] parsedParam = SplitParam(param);
            if (parsedParam.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionUserTaskmask invalid param num [{param}] for action {action.Identity}");
                return false;
            }

            if (!int.TryParse(parsedParam[1], out var flag) || flag < 0 || flag >= 32)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"ExecuteActionUserTaskmask invalid mask num {param}");
                return false;
            }

            switch (parsedParam[0].ToLower())
            {
                case "check":
                case "chk":
                    return user.CheckTaskMask(flag);
                case "add":
                    await user.AddTaskMaskAsync(flag);
                    return true;
                case "cls":
                case "clr":
                case "clear":
                    await user.ClearTaskMaskAsync(flag);
                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserMediaplay(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
                return false;

            var msg = new MsgName { Action = StringAction.PlayerWave };
            msg.Strings.Add(pszParam[1]);

            switch (pszParam[0].ToLower())
            {
                case "play":
                    await user.SendAsync(msg);
                    return true;
                case "broadcast":
                    await user.BroadcastRoomMsgAsync(msg, true);
                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserCreatemap(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] safeParam = SplitParam(param);

            if (safeParam.Length < 10)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"ExecuteActionUserCreatemap ({action.Identity}) with invalid param length [{param}]");
                return false;
            }

            string szName = safeParam[0];
            uint idOwner = uint.Parse(safeParam[2]),
                idRebornMap = uint.Parse(safeParam[7]);
            byte nOwnerType = byte.Parse(safeParam[1]);
            uint nMapDoc = uint.Parse(safeParam[3]);
            uint nType = uint.Parse(safeParam[4]);
            uint nRebornPortal = uint.Parse(safeParam[8]);
            byte nResLev = byte.Parse(safeParam[9]);
            ushort usPortalX = ushort.Parse(safeParam[5]),
                usPortalY = ushort.Parse(safeParam[6]);

            DbDynamap pMapInfo = new DbDynamap
            {
                Name = szName,
                OwnerIdentity = idOwner,
                OwnerType = nOwnerType,
                Description = $"{user.Name}`{szName}",
                RebornMap = idRebornMap,
                PortalX = usPortalX,
                PortalY = usPortalY,
                LinkMap = user.MapIdentity,
                LinkX = user.MapX,
                LinkY = user.MapY,
                MapDoc = nMapDoc,
                Type = nType,
                RebornPortal = nRebornPortal,
                ResourceLevel = nResLev,
                ServerIndex = (int)Kernel.Configuration.ServerIdentity
            };

            if (!await BaseRepository.SaveAsync(pMapInfo) || pMapInfo.Identity < 1000000)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"ExecuteActionUserCreatemap error when saving map\n\t{JsonConvert.SerializeObject(pMapInfo)}");
                return false;
            }

            GameMap map = new GameMap(pMapInfo);
            if (!await map.InitializeAsync())
                return false;

            user.HomeIdentity = pMapInfo.Identity;
            _ = await user.SaveAsync();
            return Kernel.MapManager.AddMap(map);
        }

        private static async Task<bool> ExecuteActionUserEnterHome(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null || user.HomeIdentity == 0)
                return false;

            GameMap target = Kernel.MapManager.GetMap(user.HomeIdentity);

            _ = await user.FlyMapAsync(target.Identity, target.PortalX, target.PortalY);

            if (user.Team != null)
            {
                foreach (var member in user.Team.Members)
                {
                    if (member.Identity == user.Identity || member.GetDistance(user) > 5)
                        continue;
                    _ = await member.FlyMapAsync(target.Identity, target.PortalX, target.PortalY);
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserEnterMateHome(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            uint idMap = 0;
            Character mate = Kernel.RoleManager.GetUser(user.MateIdentity);
            if (mate == null)
            {
                DbCharacter dbMate = await CharactersRepository.FindByIdentityAsync(user.MateIdentity);
                idMap = dbMate.HomeIdentity;
            }
            else
            {
                idMap = mate.HomeIdentity;
            }

            if (idMap == 0)
                return false;

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
                return false;

            _ = await user.FlyMapAsync(map.Identity, map.PortalX, map.PortalY);
            return true;
        }

        private static async Task<bool> ExecuteActionUserUnlearnMagic(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] magicsIds = SplitParam(param);

            foreach (var id in magicsIds)
            {
                ushort idMagic = ushort.Parse(id);
                if (user.MagicData.CheckType(idMagic))
                {
                    _ = await user.MagicData.UnlearnMagicAsync(idMagic, false);
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserRebirth(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);

            if (!ushort.TryParse(splitParam[0], out var prof)
                || !ushort.TryParse(splitParam[1], out var look))
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid parameter to rebirth {param}, {action.Identity}");
                return false;
            }

            return await user.RebirthAsync(prof, look);
        }

        private static async Task<bool> ExecuteActionUserWebpage(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(param, MsgTalk.TalkChannel.Website);
            return true;
        }

        private static async Task<bool> ExecuteActionUserBbs(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(param, MsgTalk.TalkChannel.Bbs);
            return true;
        }

        private static async Task<bool> ExecuteActionUserUnlearnSkill(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            return await user.UnlearnAllSkillAsync();
        }

        private static async Task<bool> ExecuteActionUserDropMagic(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] magicsIds = SplitParam(param);

            foreach (var id in magicsIds)
            {
                ushort idMagic = ushort.Parse(id);
                if (user.MagicData.CheckType(idMagic))
                {
                    _ = await user.MagicData.UnlearnMagicAsync(idMagic, true);
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserOpenDialog(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            switch ((OpenWindow)action.Data)
            {
                case OpenWindow.VipWarehouse:
                    if (user.BaseVipLevel == 0)
                        return false;
                    break;
            }

            await user.SendAsync(new MsgAction
            {
                Identity = role?.Identity ?? user.Identity,
                Command = action.Data,
                Action = MsgAction.ActionType.ClientDialog,
                Map = role?.MapIdentity ?? user.MapIdentity,
                ArgumentX = role?.MapX ?? user.MapX,
                ArgumentY = role?.MapY ?? user.MapY
            });
            return true;
        }

        private static async Task<bool> ExecuteActionUserFixAttr(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            ushort attr = (ushort)(user.Agility + user.Vitality + user.Strength + user.Spirit + user.AttributePoints -
                                    10);
            ushort profSort = user.ProfessionSort;
            if (profSort == 13 || profSort == 14)
                profSort = 10;

            DbPointAllot pointAllot = Kernel.RoleManager.GetPointAllot(profSort, 1);
            if (pointAllot != null)
            {
                _ = await user.SetAttributesAsync(ClientUpdateType.Strength, pointAllot.Strength);
                _ = await user.SetAttributesAsync(ClientUpdateType.Agility, pointAllot.Agility);
                _ = await user.SetAttributesAsync(ClientUpdateType.Vitality, pointAllot.Vitality);
                _ = await user.SetAttributesAsync(ClientUpdateType.Spirit, pointAllot.Spirit);
            }
            else
            {
                _ = await user.SetAttributesAsync(ClientUpdateType.Strength, 5);
                _ = await user.SetAttributesAsync(ClientUpdateType.Agility, 2);
                _ = await user.SetAttributesAsync(ClientUpdateType.Vitality, 3);
                _ = await user.SetAttributesAsync(ClientUpdateType.Spirit, 0);
            }

            _ = await user.SetAttributesAsync(ClientUpdateType.Atributes, attr);
            return true;
        }

        private static async Task<bool> ExecuteActionUserExpMultiply(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
                return false;

            uint time = uint.Parse(pszParam[1]);
            float multiply = int.Parse(pszParam[0]) / 100f;
            _ = await user.SetExperienceMultiplierAsync(time, multiply);
            return true;
        }

        private static async Task<bool> ExecuteActionUserWhPassword(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            if (user.SecondaryPassword == 0)
                return true;

            if (string.IsNullOrEmpty(input))
                return false;

            if (input.Length < 1 || input.Length > ulong.MaxValue.ToString().Length)
                return false;

            if (!ulong.TryParse(input, out var password))
                return false;

            return user.SecondaryPassword == password;
        }

        private static async Task<bool> ExecuteActionUserSetWhPassword(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            bool parsed = ulong.TryParse(input, out var password);
            if (string.IsNullOrEmpty(input) || (parsed && password == 0))
            {
                user.SecondaryPassword = 0;
                _ = await user.SaveAsync();
                return true;
            }

            if (input.Length < 1 || !parsed)
                return false;

            user.SecondaryPassword = password;
            _ = await user.SaveAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionUserOpeninterface(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null) return false;

            await user.SendAsync(new MsgAction
            {
                Identity = user.Identity,
                Command = action.Data,
                Action = MsgAction.ActionType.ClientCommand,
                ArgumentX = user.MapX,
                ArgumentY = user.MapY
            });
            return true;
        }

        private static async Task<bool> ExecuteActionUserTaskManager(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
                return false;

            if (action.Data == 0)
                return false;

            switch (param.ToLowerInvariant())
            {
                case "new":
                    if (user.TaskDetail.QueryTaskData(action.Data) != null)
                        return false;
                    return await user.TaskDetail.CreateNewAsync(action.Data);
                case "isexit":
                    return user.TaskDetail.QueryTaskData(action.Data) != null;
                case "delete":
                    return await user.TaskDetail.DeleteTaskAsync(action.Data);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskOpe(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
                return false;

            if (action.Data == 0)
                return false;

            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length != 3)
                return false;

            string ope = splitParam[0].ToLowerInvariant(),
                opt = splitParam[1].ToLowerInvariant();
            int data = int.Parse(splitParam[2]);

            if (ope.Equals("complete"))
            {
                if (opt.Equals("=="))
                {
                    return user.TaskDetail.QueryTaskData(action.Data)?.CompleteFlag == data;
                }

                if (opt.Equals("set"))
                {
                    return await user.TaskDetail.SetCompleteAsync(action.Data, data);
                }

                return false;
            }

            if (ope.StartsWith("data"))
            {
                switch (opt)
                {
                    case ">":
                        return user.TaskDetail.GetData(action.Data, ope) > data;
                    case "<":
                        return user.TaskDetail.GetData(action.Data, ope) < data;
                    case ">=":
                        return user.TaskDetail.GetData(action.Data, ope) >= data;
                    case "<=":
                        return user.TaskDetail.GetData(action.Data, ope) <= data;
                    case "==":
                        return user.TaskDetail.GetData(action.Data, ope) == data;
                    case "+=":
                        return await user.TaskDetail.AddDataAsync(action.Data, ope, data);
                    case "set":
                        return await user.TaskDetail.SetDataAsync(action.Data, ope, data);
                }

                return false;
            }

            if (ope.Equals("notify"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                    return false;

                detail.NotifyFlag = (byte)data;
                return await user.TaskDetail.SaveAsync(detail);
            }

            if (ope.Equals("overtime"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                    return false;

                detail.TaskOvertime = (uint)data;
                return await user.TaskDetail.SaveAsync(detail);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserTaskLocaltime(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
                return false;

            if (action.Data == 0)
                return false;

            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length != 3)
                return false;

            string ope = splitParam[0].ToLowerInvariant(),
                opt = splitParam[1].ToLowerInvariant();
            int data = int.Parse(splitParam[2]);

            if (ope.StartsWith("interval", StringComparison.InvariantCultureIgnoreCase))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                    return true;

                int mode = int.Parse(GetParenthesys(ope));
                switch (mode)
                {
                    case 0: // seconds
                        {
                            DateTime timeStamp = DateTime.Now;
                            int nDiff = (int)((timeStamp - UnixTimestamp.ToDateTime(detail.TaskOvertime)).TotalSeconds);
                            switch (opt)
                            {
                                case "==": return nDiff == data;
                                case "<": return nDiff < data;
                                case ">": return nDiff > data;
                                case "<=": return nDiff <= data;
                                case ">=": return nDiff >= data;
                                case "<>":
                                case "!=": return nDiff != data;
                            }

                            return false;
                        }

                    case 1: // days
                        int interval = int.Parse(DateTime.Now.ToString("yyyyMMdd")) -
                                       int.Parse(UnixTimestamp.ToDateTime(detail.TaskOvertime).ToString("yyyyMMdd"));
                        switch (opt)
                        {
                            case "==": return interval == data;
                            case "<": return interval < data;
                            case ">": return interval > data;
                            case "<=": return interval <= data;
                            case ">=": return interval >= data;
                            case "!=":
                            case "<>": return interval != data;
                        }

                        return false;
                    default:
                        await Log.WriteLogAsync(LogLevel.Warning,
                            $"Unhandled Time mode ({mode}) on action (id:{action.Identity})");
                        return false;
                }
            }

            if (opt.Equals("set"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                    return false;

                detail.TaskOvertime = (uint)data;
                return await user.TaskDetail.SaveAsync(detail);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskFind(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            await Log.WriteLogAsync(LogLevel.Warning, $"ExecuteActionUserTaskFind unhandled");
            return false;
        }

        private static async Task<bool> ExecuteActionUserVarCompare(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            byte varId = VarId(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            if (varId >= Role.MAX_VAR_AMOUNT)
                return false;

            switch (opt)
            {
                case "==":
                    return user.VarData[varId] == value;
                case ">=":
                    return user.VarData[varId] >= value;
                case "<=":
                    return user.VarData[varId] <= value;
                case ">":
                    return user.VarData[varId] > value;
                case "<":
                    return user.VarData[varId] < value;
                case "!=":
                    return user.VarData[varId] != value;
                default:
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserVarDefine(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] safeParam = SplitParam(param);
            if (safeParam.Length < 3)
                return false;

            byte varId = VarId(safeParam[0]);
            string opt = safeParam[1];
            long value = long.Parse(safeParam[2]);

            if (varId >= Role.MAX_VAR_AMOUNT)
                return false;

            try
            {
                switch (opt)
                {
                    case "set":
                        user.VarData[varId] = value;
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserVarCalc(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] safeParam = SplitParam(param);
            if (safeParam.Length < 3)
                return false;

            byte varId = VarId(safeParam[0]);
            string opt = safeParam[1];
            long value = long.Parse(safeParam[2]);

            if (opt == "/=" && value == 0)
                return false; // division by zero

            if (varId >= Role.MAX_VAR_AMOUNT)
                return false;

            switch (opt)
            {
                case "+=":
                    user.VarData[varId] += value;
                    return true;
                case "-=":
                    user.VarData[varId] -= value;
                    return true;
                case "*=":
                    user.VarData[varId] *= value;
                    return true;
                case "/=":
                    user.VarData[varId] /= value;
                    return true;
                case "mod=":
                    user.VarData[varId] %= value;
                    return true;
                default:
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserTestEquipment(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param, 2);
            if (!ushort.TryParse(splitParams[0], out var pos)
                || !int.TryParse(splitParams[1], out var type))
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"Invalid parsed param ExecuteActionUserTestEquipment, id[{action.Identity}]");
                return false;
            }

            if (!Enum.IsDefined(typeof(Item.ItemPosition), pos))
                return false;

            Item temp = user.UserPackage[(Item.ItemPosition)pos];
            if (temp == null)
                return false;
            return temp.GetItemSubType() == type;
        }

        private static async Task<bool> ExecuteActionUserExecAction(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3)
            {
                return false;
            }

            if (!int.TryParse(splitParams[0], out var secSpan)
                || !uint.TryParse(splitParams[1], out var idAction))
                return false;

            _ = Kernel.EventThread.QueueAction(new QueuedAction(secSpan, idAction, user.Identity));
            return true;
        }

        private static async Task<bool> ExecuteActionUserStcCompare(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length < 2)
                return false;

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            DbStatistic dbStc = user.Statistic.GetStc(idEvent, idType);

            long data = dbStc?.Data ?? 0L;
            switch (opt)
            {
                case ">=":
                    return data >= value;
                case "<=":
                    return data <= value;
                case ">":
                    return data > value;
                case "<":
                    return data < value;
                case "!=":
                case "<>":
                    return data != value;
                case "=":
                case "==":
                    return data == value;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserStcOpe(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);
            bool bUpdate = pszParam[3] != "0";

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length < 2)
                return false;

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            if (!user.Statistic.HasEvent(idEvent, idType))
            {
                return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint)value, bUpdate);
            }

            switch (opt)
            {
                case "+=":
                    if (value == 0) return false;

                    long tempValue = user.Statistic.GetValue(idEvent, idType) + value;
                    return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint)Math.Max(0, tempValue),
                        bUpdate);
                case "=":
                case "set":
                    if (value < 0) return false;
                    return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint)Math.Max(0, value), bUpdate);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserDataSync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
                return false;

            string act = splitParam[0];
            uint type = uint.Parse(splitParam[1]);
            uint data = uint.Parse(splitParam[2]);

            if (act.Equals("send"))
            {
                await user.SendAsync(new MsgAction
                {
                    Identity = user.Identity,
                    Action = (MsgAction.ActionType)type,
                    Command = data,
                    ArgumentX = user.MapX,
                    ArgumentY = user.MapY,
                    X = user.MapX,
                    Y = user.MapY
                });
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserSelectToData(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            try
            {
                user.VarData[action.Data] = long.Parse(await BaseRepository.ScalarAsync(param));
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserStcTimeCheck(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length <= 2)
                return false;

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);
            byte mode = byte.Parse(pStc[2]);

            if (value < 0)
                return false;

            DbStatistic dbStc = user.Statistic.GetStc(idEvent, idType);
            if (dbStc?.Timestamp == null)
                return true;

            switch (mode)
            {
                case 0: // seconds
                    {
                        DateTime timeStamp = DateTime.Now;
                        int nDiff = (int)((timeStamp - dbStc.Timestamp.Value).TotalSeconds);
                        switch (opt)
                        {
                            case "==": return nDiff == value;
                            case "<": return nDiff < value;
                            case ">": return nDiff > value;
                            case "<=": return nDiff <= value;
                            case ">=": return nDiff >= value;
                            case "<>":
                            case "!=": return nDiff != value;
                        }

                        return false;
                    }

                case 1: // days
                    int interval = int.Parse(DateTime.Now.ToString("yyyyMMdd")) -
                                   int.Parse(dbStc.Timestamp.Value.ToString("yyyyMMdd"));
                    switch (opt)
                    {
                        case "==": return interval == value;
                        case "<": return interval < value;
                        case ">": return interval > value;
                        case "<=": return interval <= value;
                        case ">=": return interval >= value;
                        case "!=":
                        case "<>": return interval != value;
                    }

                    return false;
                default:
                    await Log.WriteLogAsync(LogLevel.Warning,
                        $"Unhandled Time mode ({mode}) on action (id:{action.Identity})");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserStcTimeOpe(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            switch (opt)
            {
                case "set":
                    {
                        if (value > 0)
                            return await user.Statistic.SetTimestampAsync(idEvent, idType, DateTime.Now);
                        return await user.Statistic.SetTimestampAsync(idEvent, idType, DateTime.Now);
                    }
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserAttachStatus(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            // self add 64 200 900 0
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 6)
                return false;

            string target = pszParam[0].ToLower();
            string opt = pszParam[1].ToLower();
            int status = int.Parse(pszParam[2]) + 3;
            int multiply = int.Parse(pszParam[3]);
            uint seconds = uint.Parse(pszParam[4]);
            int times = int.Parse(pszParam[5]);
            // last param unknown

            if (target == "team" && user.Team == null)
                return false;

            if (target == "self")
            {
                if (opt == "add")
                    _ = await user.AttachStatusAsync(user, status, multiply, (int)seconds, times, 0);
                else if (opt == "del")
                    _ = await user.DetachStatusAsync(status);
                return true;
            }

            if (target == "team")
            {
                foreach (var member in user.Team.Members)
                {
                    if (opt == "add")
                        _ = await member.AttachStatusAsync(member, status, multiply, (int)seconds, times, 0);
                    else if (opt == "del")
                        _ = await member.DetachStatusAsync(status);
                }

                return true;
            }

            if (target == "couple")
            {
                Character mate = Kernel.RoleManager.GetUser(user.MateIdentity);
                if (mate == null)
                    return false;

                if (opt == "add")
                {
                    _ = await user.AttachStatusAsync(user, status, multiply, (int)seconds, times, 0);
                    _ = await mate.AttachStatusAsync(user, status, multiply, (int)seconds, times, 0);
                }
                else if (opt == "del")
                {
                    _ = await user.DetachStatusAsync(status);
                    _ = await mate.DetachStatusAsync(status);
                }

                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserGodTime(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParma = SplitParam(param);

            if (pszParma.Length < 2)
                return false;

            string opt = pszParma[0];
            uint minutes = uint.Parse(pszParma[1]);

            switch (opt)
            {
                case "+=":
                    {
                        return await user.AddBlessingAsync(minutes);
                    }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserExpballExp(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
                return false;

            int dwExpTimes = int.Parse(pszParam[0]);
            byte idData = byte.Parse(pszParam[1]);

            if (idData >= user.VarData.Length) return false;

            long exp = user.CalculateExpBall(dwExpTimes);
            user.VarData[idData] = exp;
            return true;
        }

        private static async Task<bool> ExecuteActionUserStatusCreate(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            // sort leave_times remain_time end_time interval_time
            // 200 0 604800 0 604800 1
            if (action.Data == 0)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"ERROR: invalid data num {action.Identity}");
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"ERROR: invalid param num {action.Identity}");
                return false;
            }

            uint sort = uint.Parse(pszParam[0]);
            uint leaveTimes = uint.Parse(pszParam[1]);
            uint remainTime = uint.Parse(pszParam[2]);
            uint intervalTime = uint.Parse(pszParam[4]);
            bool save = pszParam[5] != "0"; // ??

            _ = await user.AttachStatusAsync(user, (int)action.Data, 0, (int)remainTime, (int)leaveTimes, 0, save);
            return true;
        }

        private static async Task<bool> ExecuteActionUserStatusCheck(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.StatusSet == null) return false;

            string[] status = SplitParam(param);

            switch (action.Data)
            {
                case 0: // check
                    foreach (var st in status)
                        if (user.QueryStatus(int.Parse(st)) == null)
                            return false;
                    return true;

                case 1:
                    foreach (var st in status)
                        if (user.QueryStatus(int.Parse(st)) != null)
                        {
                            _ = await user.DetachStatusAsync(int.Parse(st));
                            DbStatus db =
                                (await StatusRepository.GetAsync(user.Identity)).FirstOrDefault(x =>
                                    x.Status == uint.Parse(st));
                            if (db != null)
                                _ = await BaseRepository.DeleteAsync(db);
                        }

                    return true;
            }

            return false;
        }

        #endregion

        #region Team

        private static async Task<bool> ExecuteActionTeamBroadcast(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Team == null || user.Team.MemberCount < 1)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                    $"ExecuteActionTeamBroadcast user or team is null {action.Identity}");
                return false;
            }

            if (!user.Team.IsLeader(user.Identity))
                return false;

            await user.Team.SendAsync(new MsgTalk(user.Identity, MsgTalk.TalkChannel.Team, Color.White, param));
            return true;
        }

        private static async Task<bool> ExecuteActionTeamAttr(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                    $"ExecuteActionTeamAttr user or team is null {action.Identity}");
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3) // invalid param num
                return false;

            string cmd = splitParams[0].ToLower();
            string opt = splitParams[1];
            _ = int.TryParse(splitParams[2], out var value);

            if (cmd.Equals("count"))
            {
                if (opt.Equals("<"))
                    return user.Team.MemberCount < value;
                if (opt.Equals("=="))
                    return user.Team.MemberCount == value;
            }

            foreach (var member in user.Team.Members)
            {
                if (cmd.Equals("money"))
                {
                    if (opt.Equals("+="))
                    {
                        _ = await member.ChangeMoneyAsync(value);
                    }
                    else if (opt.Equals("<"))
                    {
                        return member.Silvers < value;
                    }
                    else if (opt.Equals("=="))
                    {
                        return member.Silvers == value;
                    }
                    else if (opt.Equals(">"))
                    {
                        return member.Silvers > value;
                    }
                }
                else if (cmd.Equals("emoney"))
                {
                    if (opt.Equals("+="))
                    {
                        _ = await member.ChangeConquerPointsAsync(value);
                    }
                    else if (opt.Equals("<"))
                    {
                        return member.ConquerPoints < value;
                    }
                    else if (opt.Equals("=="))
                    {
                        return member.ConquerPoints == value;
                    }
                    else if (opt.Equals(">"))
                    {
                        return member.ConquerPoints > value;
                    }
                }
                else if (cmd.Equals("level"))
                {
                    if (opt.Equals("<"))
                    {
                        return member.Level < value;
                    }
                    else if (opt.Equals("=="))
                    {
                        return member.Level == value;
                    }
                    else if (opt.Equals(">"))
                    {
                        return member.Level > value;
                    }
                }
                else if (cmd.Equals("vip"))
                {
                    if (opt.Equals("<"))
                    {
                        return member.BaseVipLevel < value;
                    }
                    else if (opt.Equals("=="))
                    {
                        return member.BaseVipLevel == value;
                    }
                    else if (opt.Equals(">"))
                    {
                        return member.BaseVipLevel > value;
                    }
                }
                else if (cmd.Equals("mate"))
                {
                    if (member.Identity == user.Identity)
                        continue;

                    if (member.MateIdentity != user.Identity)
                        return false;
                }
                else if (cmd.Equals("friend"))
                {
                    if (member.Identity == user.Identity)
                        continue;

                    if (!user.IsFriend(member.Identity))
                        return false;
                }
                else if (cmd.Equals("count_near"))
                {
                    if (member.Identity == user.Identity)
                        continue;

                    if (!(member.MapIdentity == user.MapIdentity && member.IsAlive))
                        return false;
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamLeavespace(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                    $"ExecuteActionTeamLeavespace user or team is null {action.Identity}");
                return false;
            }

            if (!int.TryParse(param, out var space))
                return false;

            foreach (var member in user.Team.Members)
            {
                if (!member.UserPackage.IsPackSpare(space))
                    return false;
            }
            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemAdd(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                    $"ExecuteActionTeamItemAdd user or team is null {action.Identity}");
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemDel(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                    $"ExecuteActionTeamItemDel user or team is null {action.Identity}");
                return false;
            }

            foreach (var member in user.Team.Members)
                _ = await member.UserPackage.AwardItemAsync(action.Data);
            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemCheck(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                    $"ExecuteActionTeamItemCheck user or team is null {action.Identity}");
                return false;
            }

            foreach (var member in user.Team.Members)
                _ = await member.UserPackage.SpendItemAsync(action.Data);

            return true;
        }

        private static async Task<bool> ExecuteActionTeamChgmap(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                    $"ExecuteActionTeamChgmap user or team is null {action.Identity}");
                return false;
            }

            foreach (var member in user.Team.Members)
                if (member.UserPackage.GetItemByType(action.Data) == null)
                    return false;

            return true;
        }

        private static async Task<bool> ExecuteActionTeamChkIsleader(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                    $"ExecuteActionTeamChkIsleader user or team is null {action.Identity}");
                return false;
            }

            return user.Team.IsLeader(user.Identity);
        }

        #endregion

        #region General

        private static async Task<bool> ExecuteGeneralLottery(DbAction action, string param, Character user, Role role,
            Item item,
            string input)
        {
            if (user == null)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"No user for ExecuteGeneralLottery, {action.Identity}");
                return false;
            }

            List<DbLottery> allItems = await DbLottery.GetAsync();
            int lottoChance = await Kernel.NextAsync(10000);
            if (lottoChance > 20)
            {
                _ = allItems.RemoveAll(x => x.Rank == 1);
            }

            if (lottoChance > 75)
            {
                _ = allItems.RemoveAll(x => x.Rank == 2);
            }

            if (lottoChance > 200)
            {
                _ = allItems.RemoveAll(x => x.Rank == 3);
            }

            if (lottoChance > 1000)
            {
                _ = allItems.RemoveAll(x => x.Rank == 4);
            }

            if (lottoChance > 2000)
            {
                _ = allItems.RemoveAll(x => x.Rank == 5);
            }

            if (lottoChance > 4000)
            {
                _ = allItems.RemoveAll(x => x.Rank == 6);
            }

            _ = allItems.RemoveAll(x => Kernel.ItemManager.GetItemtype(x.ItemIdentity) == null);

            DbLottery lottery = allItems[await Kernel.NextAsync(allItems.Count) % allItems.Count];
            DbItemtype itemtype = Kernel.ItemManager.GetItemtype(lottery.ItemIdentity);

            DbItem lottoItem = new DbItem
            {
                Type = lottery.ItemIdentity,
                Amount = itemtype.Amount,
                AmountLimit = itemtype.AmountLimit,
                Magic3 = lottery.Plus > 0 ? lottery.Plus : itemtype.Magic3,
                Gem1 = (byte)(lottery.SocketNum > 0 ? 255 : 0),
                Gem2 = (byte)(lottery.SocketNum > 1 ? 255 : 0),
                Color = 3,
                PlayerId = user.Identity
            };

            Item newItem = new Item(user);
            if (!await newItem.CreateAsync(lottoItem))
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Error to create lottery item {newItem.ToJson()}");
                return false;
            }

            _ = await user.UserPackage.AddItemAsync(newItem);

            await Log.GmLogAsync("lottery",
                $"{user.Identity},{user.Name},{lottery.Rank},{lottery.Color},{newItem.Type},{newItem.Plus},{newItem.SocketOne},{newItem.SocketTwo}");

            if (lottery.Rank <= 5)
            {
                await Kernel.RoleManager.BroadcastMsgAsync(
                    string.Format(Language.StrLotteryHigh, user.Name, lottery.Itemname), MsgTalk.TalkChannel.Talk);
            }
            else
            {
                await user.SendAsync(string.Format(Language.StrLotteryLow, lottery.Itemname));
            }

            return true;
        }

        private static async Task<bool> ExecuteActionChgMapSquare(DbAction action, string param, Character user,
            Role role, Item item,
            string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param, 5);

            if (!uint.TryParse(splitParams[0], out var idMap)
                || !ushort.TryParse(splitParams[1], out var x)
                || !ushort.TryParse(splitParams[2], out var y)
                || !ushort.TryParse(splitParams[3], out var cx)
                || !ushort.TryParse(splitParams[4], out var cy))
                return false;

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"GameAction::ExecuteActionChgMapSquare invalid map {idMap}");
                return false;
            }

            for (int i = 0; i < 10; i++)
            {
                x = (ushort)(x + await Kernel.NextAsync(cx));
                y = (ushort)(y + await Kernel.NextAsync(cy));

                if (map.IsStandEnable(x, y))
                    break;
            }

            _ = await user.FlyMapAsync(idMap, x, y);
            return true;
        }

        #endregion

        #region Event

        private static async Task<bool> ExecuteActionEventSetstatus(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3) return false;

            uint idMap = uint.Parse(pszParam[0]);
            ulong nStatus = ulong.Parse(pszParam[1]);
            int nFlag = int.Parse(pszParam[2]);

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
                return false;

            if (nFlag == 0)
                map.Flag &= ~nStatus;
            else
                map.Flag |= nStatus;
            return true;
        }

        private static async Task<bool> ExecuteActionEventDelnpcGenid(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            //Generator generator = Kernel.WorldThread.GetGenerator(action.Data);
            Generator generator = Kernel.GeneratorManager.GetGenerator(action.Data);
            if (generator == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"Invalid generator {action.Data} for action {action.Identity}");
                return false;
            }

            foreach (var monster in generator.GetRoles())
            {
                if (monster.Map != null)
                    await monster.LeaveMapAsync();
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventCompare(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3)
                return false;

            long nData1 = long.Parse(pszParam[0]), nData2 = long.Parse(pszParam[2]);
            string szOpt = pszParam[1];

            switch (szOpt)
            {
                case "==":
                    return nData1 == nData2;
                case "<":
                    return nData1 < nData2;
                case ">":
                    return nData1 > nData2;
                case "<=":
                    return nData1 <= nData2;
                case ">=":
                    return nData1 >= nData2;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventCompareUnsigned(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3)
                return false;

            ulong nData1 = ulong.Parse(pszParam[0]), nData2 = ulong.Parse(pszParam[2]);
            string szOpt = pszParam[1];

            switch (szOpt)
            {
                case "==":
                    return nData1 == nData2;
                case "<":
                    return nData1 < nData2;
                case ">":
                    return nData1 > nData2;
                case "<=":
                    return nData1 <= nData2;
                case ">=":
                    return nData1 >= nData2;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventChangeweather(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {


            return true;
        }

        private static async Task<bool> ExecuteActionEventCreatepet(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 7) return false;

            uint dwOwnerType = uint.Parse(pszParam[0]);
            uint idOwner = uint.Parse(pszParam[1]);
            uint idMap = uint.Parse(pszParam[2]);
            ushort usPosX = ushort.Parse(pszParam[3]);
            ushort usPosY = ushort.Parse(pszParam[4]);
            uint idGen = uint.Parse(pszParam[5]);
            uint idType = uint.Parse(pszParam[6]);
            uint dwData = 0;
            string szName = "";

            if (pszParam.Length >= 8)
                dwData = uint.Parse(pszParam[7]);
            if (pszParam.Length >= 9)
                szName = pszParam[8];

            DbMonstertype monstertype = Kernel.RoleManager.GetMonstertype(idType);
            GameMap map = Kernel.MapManager.GetMap(idMap);

            if (monstertype == null || map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionEventCreatepet [{action.Identity}] invalid monstertype or map: {param}");
                return false;
            }

            //Generator generator = Kernel.WorldThread.GetGenerator(idGen);
            Generator generator = Kernel.GeneratorManager.GetGenerator(idGen);
            if (generator == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionEventCreatepet [{action.Identity}] unexistent generator: {param}");
                return false;
            }

            Monster monster = new Monster(monstertype, (uint)IdentityGenerator.Monster.GetNextIdentity, generator);
            if (!await monster.InitializeAsync(idMap, usPosX, usPosY))
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionEventCreatepet [{action.Identity}] could not initialize monster: {param}");
                return false;
            }

            _ = generator.Add(monster);
            await monster.EnterMapAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionEventCreatenewNpc(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 9)
                return false;

            string szName = pszParam[0];
            ushort nType = ushort.Parse(pszParam[1]);
            ushort nSort = ushort.Parse(pszParam[2]);
            ushort nLookface = ushort.Parse(pszParam[3]);
            uint nOwnerType = uint.Parse(pszParam[4]);
            uint nOwner = uint.Parse(pszParam[5]);
            uint idMap = uint.Parse(pszParam[6]);
            ushort nPosX = ushort.Parse(pszParam[7]);
            ushort nPosY = ushort.Parse(pszParam[8]);
            uint nLife = 0;
            uint idBase = 0;
            uint idLink = 0;
            uint setTask0 = 0;
            uint setTask1 = 0;
            uint setTask2 = 0;
            uint setTask3 = 0;
            uint setTask4 = 0;
            uint setTask5 = 0;
            uint setTask6 = 0;
            uint setTask7 = 0;
            int setData0 = 0;
            int setData1 = 0;
            int setData2 = 0;
            int setData3 = 0;
            string szData = "";
            if (pszParam.Length > 9)
            {
                nLife = uint.Parse(pszParam[9]);
                if (pszParam.Length > 10)
                    idBase = uint.Parse(pszParam[10]);
                if (pszParam.Length > 11)
                    idLink = uint.Parse(pszParam[11]);
                if (pszParam.Length > 12)
                    setTask0 = uint.Parse(pszParam[12]);
                if (pszParam.Length > 13)
                    setTask1 = uint.Parse(pszParam[13]);
                if (pszParam.Length > 14)
                    setTask2 = uint.Parse(pszParam[14]);
                if (pszParam.Length > 15)
                    setTask3 = uint.Parse(pszParam[15]);
                if (pszParam.Length > 16)
                    setTask4 = uint.Parse(pszParam[16]);
                if (pszParam.Length > 17)
                    setTask5 = uint.Parse(pszParam[17]);
                if (pszParam.Length > 18)
                    setTask6 = uint.Parse(pszParam[18]);
                if (pszParam.Length > 19)
                    setTask7 = uint.Parse(pszParam[19]);
                if (pszParam.Length > 20)
                    setData0 = int.Parse(pszParam[20]);
                if (pszParam.Length > 21)
                    setData1 = int.Parse(pszParam[21]);
                if (pszParam.Length > 22)
                    setData2 = int.Parse(pszParam[22]);
                if (pszParam.Length > 23)
                    setData3 = int.Parse(pszParam[23]);
                if (pszParam.Length > 24)
                    szData = pszParam[24];
            }

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                    $"ExecuteActionEventCreatenewNpc invalid {idMap} map identity for action {action.Identity}");
                return false;
            }

            var npc = new DbDynanpc
            {
                Name = szName,
                Base = idBase,
                Cellx = nPosX,
                Celly = nPosY,
                Data0 = setData0,
                Data1 = setData1,
                Data2 = setData2,
                Data3 = setData3,
                Datastr = szData,
                Defence = 0,
                Life = nLife,
                Maxlife = nLife,
                Linkid = idLink,
                Task0 = setTask0,
                Task1 = setTask1,
                Task2 = setTask2,
                Task3 = setTask3,
                Task4 = setTask4,
                Task5 = setTask5,
                Task6 = setTask6,
                Task7 = setTask7,
                Ownerid = nOwner,
                OwnerType = nOwnerType,
                Lookface = nLookface,
                Type = nType,
                Mapid = idMap,
                Sort = nSort
            };

            if (!await BaseRepository.SaveAsync(npc))
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"ExecuteActionEventCreatenewNpc could not save dynamic npc");
                return false;
            }

            DynamicNpc dynaNpc = new DynamicNpc(npc);
            if (!await dynaNpc.InitializeAsync())
                return false;

            await dynaNpc.EnterMapAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionEventCountmonster(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 5)
                return false;

            uint idMap = uint.Parse(pszParam[0]);
            string szField = pszParam[1];
            string szData = pszParam[2];
            string szOpt = pszParam[3];
            int nNum = int.Parse(pszParam[4]);
            int nCount = 0;

            switch (szField.ToLowerInvariant())
            {
                case "name":
                    nCount += Kernel.GeneratorManager.GetGenerators(idMap, szData)
                        .Sum(x => x.GetRoles().Count(y => y.IsAlive));
                    break;
                case "gen_id":
                    Generator gen = Kernel.GeneratorManager.GetGenerator(uint.Parse(szData));
                    if (gen == null)
                        return false;
                    nCount += gen.GetRoles().Count(x => x.IsAlive);
                    break;
            }

            switch (szOpt)
            {
                case "==":
                    return nCount == nNum;
                case "<":
                    return nCount < nNum;
                case ">":
                    return nCount > nNum;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventDeletemonster(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
                return false;

            uint idMap = uint.Parse(pszParam[0]);
            uint idType = uint.Parse(pszParam[1]);
            int nData = 0;
            string szName = "";

            if (pszParam.Length >= 3)
                nData = int.Parse(pszParam[2]);
            if (pszParam.Length >= 4)
                szName = pszParam[3];

            bool ret = false;

            if (!string.IsNullOrEmpty(szName))
            {
                foreach (var gen in Kernel.GeneratorManager.GetGenerators(idMap, szName))
                {
                    await gen.ClearGeneratorAsync();
                    ret = true;
                }
            }

            if (idType != 0)
            {
                foreach (var gen in Kernel.GeneratorManager.GetByMonsterType(idType))
                {
                    if (gen.MapIdentity == idMap)
                    {
                        await gen.ClearGeneratorAsync();
                        ret = true;
                    }
                }
            }

            return ret;
        }

        private static async Task<bool> ExecuteActionEventBbs(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            await Kernel.RoleManager.BroadcastMsgAsync(param, MsgTalk.TalkChannel.System);
            return true;
        }

        private static async Task<bool> ExecuteActionEventErase(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            uint npcType = uint.Parse(pszParam[2]);
            foreach (var dynaNpc in Kernel.RoleManager.QueryRoleByMap<DynamicNpc>(uint.Parse(pszParam[0])))
            {
                if (dynaNpc.Type == npcType)
                    await dynaNpc.DelNpcAsync();
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventTeleport(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 4)
                return false;

            if (!uint.TryParse(pszParam[0], out var idSource) || !uint.TryParse(pszParam[1], out var idTarget) ||
                !ushort.TryParse(pszParam[2], out var usMapX) || !ushort.TryParse(pszParam[3], out var usMapY))
                return false;

            GameMap sourceMap = Kernel.MapManager.GetMap(idSource);
            GameMap targetMap = Kernel.MapManager.GetMap(idTarget);

            if (sourceMap == null || targetMap == null)
                return false;

            if (sourceMap.IsTeleportDisable())
                return false;

            if (!sourceMap[usMapX, usMapY].IsAccessible())
                return false;

            foreach (var player in Kernel.RoleManager.QueryRoleByType<Character>()
                .Where(x => x.MapIdentity == sourceMap.Identity))
            {
                _ = await player.FlyMapAsync(idTarget, usMapX, usMapY);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventMassaction(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            if (!uint.TryParse(pszParam[0], out var idMap) || !uint.TryParse(pszParam[1], out var idAction)
                                                           || !int.TryParse(pszParam[2], out var nAmount))
                return false;

            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map == null)
                return false;

            if (nAmount <= 0)
                nAmount = int.MaxValue;

            foreach (var player in Kernel.RoleManager.QueryRoleByMap<Character>(idMap))
            {
                if (nAmount-- <= 0)
                    break;

                _ = await ExecuteActionAsync(idAction, player, role, null, input);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventRegisterAsync(DbAction action, string param, Character user,
            Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            GameEvent baseEvent;
            switch (param.ToLower())
            {
                case "timedguildwar":
                    {
                        baseEvent = Kernel.EventThread.GetEvent<TimedGuildWar>();
                        break;
                    }
                case "lineskillpk":
                    {
                        baseEvent = Kernel.EventThread.GetEvent(GameEvent.EventType.LineSkillPk);
                        break;
                    }
                case "syndicatecontest":
                    {
                        baseEvent = Kernel.EventThread.GetEvent(GameEvent.EventType.GuildContest);
                        break;
                    }

                default:
                    return false;
            }

            if (baseEvent == null)
                return false;

            return await user.SignInEventAsync(baseEvent);
        }

        private static async Task<bool> ExecuteActionEventExit(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            if (user == null)
                return false;

            _ = await user.SignOutEventAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionEventCmdAsync(DbAction action, string param, Character user, Role role,
            Item item, string input)
        {
            GameEvent.EventType eventType = (GameEvent.EventType)action.Data;
            if (eventType == GameEvent.EventType.None || eventType >= GameEvent.EventType.Limit)
                return false;

            var @event = Kernel.EventThread.GetEvent(eventType);
            if (@event == null)
                return false;

            return await @event.OnActionCommandAsync(param, user, role, item, input);
        }

        #endregion

        #region Family

        private static async Task<bool> ExecuteActionFamilyAttrAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length < 3)
                return false;

            string field = splitParam[0],
                opt = splitParam[1];
            long value = long.Parse(splitParam[2]);

            long data = -1;
            if (user?.Family != null)
            {
                if (field.Equals("money"))
                {
                    if (opt.Equals("+="))
                    {
                        if (value < 0)
                        {
                            ulong temp = (ulong)(value * -1);
                            if (user.Family.Money < temp)
                                return false;
                            user.Family.Money -= temp;
                        }
                        else
                        {
                            ulong temp = (ulong)value;
                            user.Family.Money += temp;
                        }
                        return await user.Family.SaveAsync();
                    }

                    data = (long)user.Family.Money;
                }
                else if (field.Equals("rank"))
                {
                    if (opt.Equals("="))
                    {
                        user.Family.Rank = (byte)Math.Min(4, value);
                        return await user.Family.SaveAsync();
                    }

                    data = user.Family.Rank;
                }
                else if (field.Equals("star_tower"))
                {
                    if (opt.Equals("="))
                    {
                        user.Family.BattlePowerTower = (byte)Math.Min(4, value);
                        return await user.Family.SaveAsync();
                    }

                    data = user.Family.BattlePowerTower;
                }
            }

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">": return data > value;
                case "<": return data < value;
            }
            return false;
        }

        private static async Task<bool> ExecuteActionFamilyMemberAttrAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length < 3)
                return false;

            string field = splitParam[0],
                opt = splitParam[1];
            long value = long.Parse(splitParam[2]);
            long data = 0;

            if (user?.Family != null)
            {
                if (field.Equals("rank"))
                {
                    data = (long)user.FamilyPosition;
                }
            }

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">": return data > value;
                case "<": return data < value;
            }
            return true;
        }

        private static async Task<bool> ExecuteActionFamilyWarActivityCheckAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
                return false;

            DynamicNpc npc = Kernel.RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
                return false;

            if (npc.Data1 != user.Family.ChallengeMap && npc.Data1 != user.Family.FamilyMap)
                return false;

            FamilyWar war = Kernel.EventThread.GetEvent<FamilyWar>();
            if (war == null)
                return false;
            if (!war.IsInTime)
                return false;

            GameMap map = Kernel.MapManager.GetMap((uint)npc.Data1);
            if (map == null)
                return false;

            Dictionary<uint, Family> families = new Dictionary<uint, Family>();
            foreach (var player in map.QueryPlayers(x =>
                x.FamilyIdentity != 0 && (x.Family.FamilyMap == map.Identity || x.Family.ChallengeMap == map.Identity) && x.IsAlive))
            {
                if (!families.ContainsKey(player.FamilyIdentity))
                    families.Add(player.FamilyIdentity, player.Family);
            }

            if (families.Count == 1)
            {
                Family winner = families.Values.ToList()[0];
                if (winner.ChallengeMap != npc.Data1 || winner.FamilyMap != npc.Data1)
                {
                    // this map is now transfered
                    // the clan will lose the old one
                    Family old = Kernel.FamilyManager.GetOccupyOwner((uint)npc.Data1);
                    if (old != null)
                    {
                        old.FamilyMap = 0;
                        old.OccupyDate = 0;
                        _ = await old.SaveAsync();
                    }

                    winner.FamilyMap = map.Identity;
                    if (winner.OccupyDate == 0)
                        winner.OccupyDate = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
                    _ = await winner.SaveAsync();
                }
            }
            else if (families.Count > 1)
            {
                uint currentTime = uint.Parse(DateTime.Now.ToString("HHmmss"));
                if (currentTime < 204500 || currentTime > 205459)
                    return false;

                Family current = Kernel.FamilyManager.GetOccupyOwner((uint)npc.Data1);
                if (families.All(x => x.Key != current?.Identity))
                {
                    Character higherBpPlayer = map.QueryPlayers(x =>
                            x.FamilyIdentity != 0 &&
                            (x.Family.FamilyMap == map.Identity || x.Family.ChallengeMap == map.Identity) && x.IsAlive)
                        .OrderByDescending(x => x.BattlePower)
                        .FirstOrDefault();

                    if (higherBpPlayer == null)
                        return true; // wont be null, but doesnt hurt to check

                    if (current != null)
                    {
                        current.FamilyMap = 0;
                        current.OccupyDate = 0;
                        _ = await current.SaveAsync();
                    }

                    higherBpPlayer.Family.FamilyMap = map.Identity;
                    higherBpPlayer.Family.OccupyDate = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
                    _ = await higherBpPlayer.Family.SaveAsync();
                } // return true even if false because the winner is the clan whose is already dominating. wont change

            }
            else return false;

            return true;
        }

        private static async Task<bool> ExecuteActionFamilyWarAuthorityCheckAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
                return false;

            DynamicNpc npc = Kernel.RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
                return false;

            if (npc.Data1 != user.Family.ChallengeMap)
                return false;
            return true;
        }

        private static async Task<bool> ActionFamilyWarRegisterCheckAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
                return false;

            DynamicNpc npc = Kernel.RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
                return false;

            if (npc.Data1 != user.Family.FamilyMap)
                return false;
            return true;
        }

        #endregion

        #region Trap

        private static async Task<bool> ExecuteActionTrapCreate(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParams = SplitParam(param, 7);

            if (splitParams.Length < 7)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"Invalid param length ExecuteActionTrapCreate {action.Identity}");
                return false;
            }

            uint type = uint.Parse(splitParams[0]),
                look = uint.Parse(splitParams[1]),
                owner = uint.Parse(splitParams[2]),
                idMap = uint.Parse(splitParams[3]);
            ushort posX = ushort.Parse(splitParams[4]),
                posY = ushort.Parse(splitParams[5]),
                data = ushort.Parse(splitParams[6]);

            if (Kernel.MapManager.GetMap(idMap) == null)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"Invalid map for ExecuteActionTrapCreate {idMap}:{action.Identity}");
                return false;
            }

            MapTrap trap = new MapTrap(new DbTrap
            {
                TypeId = type,
                Look = look,
                OwnerId = owner,
                Data = data,
                MapId = idMap,
                PosX = posX,
                PosY = posY,
                Id = (uint)IdentityGenerator.Traps.GetNextIdentity
            });

            if (!await trap.InitializeAsync())
            {
                await Log.WriteLogAsync(LogLevel.Error,
                    $"could not start trap for ExecuteActionTrapCreate {action.Identity}");
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTrapErase(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            MapTrap trap = role as MapTrap;
            if (trap == null)
                return false;

            await trap.LeaveMapAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionTrapCount(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            throw new NotImplementedException("No action for this yet.");
            // return true;
        }

        #endregion

        #region Detain

        private static async Task<bool> ExecuteActionDetainDialogAsync(DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (param.Equals("target"))
            {
                await user.SendAsync(new MsgAction
                {
                    Action = MsgAction.ActionType.ClientDialog,
                    X = user.MapX,
                    Y = user.MapY,
                    Identity = user.Identity,
                    Data = 336
                });
                return true;
            }
            else if (param.Equals("hunter"))
            {
                await user.SendAsync(new MsgAction
                {
                    Action = MsgAction.ActionType.ClientDialog,
                    X = user.MapX,
                    Y = user.MapY,
                    Identity = user.Identity,
                    Data = 337
                });
                return true;
            }
            return false;
        }

        #endregion

        private static string FormatParam(DbAction action, Character user, Role role, Item item, string input)
        {
            string result = action.Param;

            result = result.Replace("%user_name", user?.Name ?? Language.StrNone)
                .Replace("%user_id", user?.Identity.ToString() ?? "0")
                .Replace("%user_lev", user?.Level.ToString() ?? "0")
                .Replace("%user_mate", user?.MateName ?? Language.StrNone)
                .Replace("%user_pro", user?.Profession.ToString() ?? "0")
                .Replace("%user_map_id", user?.Map.Identity.ToString() ?? "0")
                .Replace("%user_map_name", user?.Map.Name ?? Language.StrNone)
                .Replace("%user_map_x", user?.MapX.ToString() ?? "0")
                .Replace("%user_map_y", user?.MapY.ToString() ?? "0")
                .Replace("%map_owner_id", user?.Map.OwnerIdentity.ToString() ?? "0")
                .Replace("%user_nobility_rank", ((int)(user?.NobilityRank ?? 0)).ToString())
                .Replace("%user_nobility_position", user?.NobilityPosition.ToString() ?? "0")
                .Replace("%user_home_id", user?.HomeIdentity.ToString() ?? "0")
                .Replace("%syn_id", user?.SyndicateIdentity.ToString() ?? "0")
                .Replace("%syn_name", user?.SyndicateName ?? Language.StrNone)
                .Replace("%account_id", user?.Client.AccountIdentity.ToString() ?? "0")
                .Replace("%user_virtue", user?.VirtuePoints.ToString() ?? "0")
                .Replace("%map_owner_id", user?.Map.OwnerIdentity.ToString() ?? "0")
                .Replace("%last_add_item_id", user?.LastAddItemIdentity.ToString() ?? "0")
                .Replace("%online_time",
                    $"{user?.OnlineTime.TotalDays:0} days, {user?.OnlineTime.Hours:00} hours, {user?.OnlineTime.Minutes} minutes and {user?.OnlineTime.Seconds} seconds")
                .Replace("%session_time",
                    $"{user?.SessionOnlineTime.TotalDays:0} days, {user?.SessionOnlineTime.Hours:00} hours, {user?.SessionOnlineTime.Minutes} minutes and {user?.SessionOnlineTime.Seconds} seconds")
                .Replace("%businessman_days", $"{user?.BusinessManDays}")
                .Replace("%user_vip", user?.BaseVipLevel.ToString())
                .Replace("%user_svip", user?.VipLevel.ToString());

            if (result.Contains("%levelup_exp"))
            {
                DbLevelExperience db = Kernel.RoleManager.GetLevelExperience(user?.Level ?? 0);
                result = result.Replace("%levelup_exp", db != null ? db.Exp.ToString() : "0");
            }

            if (user != null)
            {
                if (result.Contains("%vip_expire"))
                {
                    string s = Language.StrExpired;
                    if (user.VipExpiration > DateTime.Now)
                        s = user.VipExpiration.ToString("f");
                    result = result.Replace("%vip_expire", s);
                }

                while (result.Contains("%stc("))
                {
                    int start = result.IndexOf("%stc(", StringComparison.InvariantCultureIgnoreCase);
                    string strEvent = "", strStage = "";
                    bool comma = false;
                    for (int i = start + 5; i < result.Length; i++)
                    {
                        if (!comma)
                        {
                            if (result[i] == ',')
                            {
                                comma = true;
                                continue;
                            }

                            strEvent += result[i];
                        }
                        else
                        {
                            if (result[i] == ')')
                                break;

                            strStage += result[i];
                        }
                    }

                    _ = uint.TryParse(strEvent, out var stcEvent);
                    _ = uint.TryParse(strStage, out var stcStage);

                    DbStatistic stc = user.Statistic.GetStc(stcEvent, stcStage);
                    result = result.Replace($"%stc({stcEvent},{stcStage})", stc?.Data.ToString() ?? "0");
                }

                for (int i = 0; i < Role.MAX_VAR_AMOUNT; i++)
                {
                    result = result.Replace($"%iter_var_data{i}", user.VarData[i].ToString());
                    result = result.Replace($"%iter_var_str{i}", user.VarString[i]);
                }
            }

            if (role != null)
            {
                if (role is BaseNpc npc)
                {
                    result = result.Replace("%data0", npc.GetData("data0").ToString())
                        .Replace("%data1", npc.GetData("data1").ToString())
                        .Replace("%data2", npc.GetData("data2").ToString())
                        .Replace("%data3", npc.GetData("data3").ToString())
                        .Replace("%npc_ownerid", npc.OwnerIdentity.ToString())
                        .Replace("%map_owner_id", role.Map.OwnerIdentity.ToString() ?? "0");
                }
                else if (role is Monster monster)
                {

                }

                result = result.Replace("%map_owner_id", role.Map.OwnerIdentity.ToString());
            }

            if (item != null)
            {
                result = result.Replace("%item_data", item.Identity.ToString())
                    .Replace("%item_name", item.Name)
                    .Replace("%item_type", item.Type.ToString())
                    .Replace("%item_id", item.Identity.ToString());
            }

            if (result.Contains("%iter_upquality_gem"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition)user.Iterator];
                if (pItem != null)
                    result = result.Replace("%iter_upquality_gem", pItem.GetUpQualityGemAmount().ToString());
                else
                    result = result.Replace("%iter_upquality_gem", "0");
            }

            if (result.Contains("%iter_itembound"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition)user.Iterator];
                if (pItem != null)
                    result = result.Replace("%iter_itembound", pItem.IsBound ? "1" : "0");
                else
                    result = result.Replace("%iter_itembound", "0");
            }

            if (result.Contains("%iter_uplevel_gem"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition)user.Iterator];
                if (pItem != null)
                    result = result.Replace("%iter_uplevel_gem", pItem.GetUpgradeGemAmount().ToString());
                else
                    result = result.Replace("%iter_uplevel_gem", "0");
            }

            result = result.Replace("%map_name", user?.Map?.Name ?? role?.Map?.Name ?? Language.StrNone)
                .Replace("%iter_time", UnixTimestamp.Now().ToString());
            return result;
        }

        private static string[] SplitParam(string param, int count = 0)
        {
            return count > 0
                ? param.Split(new[] { ' ' }, count, StringSplitOptions.RemoveEmptyEntries)
                : param.Split(' ');
        }

        private static string GetParenthesys(string szParam)
        {
            int varIdx = szParam.IndexOf("(", StringComparison.CurrentCulture) + 1;
            int endIdx = szParam.IndexOf(")", StringComparison.CurrentCulture);
            return szParam.Substring(varIdx, endIdx - varIdx);
        }

        private static byte VarId(string szParam)
        {
            int varIdx = szParam.IndexOf("(", StringComparison.CurrentCulture) + 1;
            return byte.Parse(szParam.Substring(varIdx, 1));
        }
    }

    public class QueuedAction
    {
        private TimeOut m_timeOut = new TimeOut();

        public QueuedAction(int secs, uint action, uint idUser)
        {
            m_timeOut.Startup(secs);
            Action = action;
            UserIdentity = idUser;
        }

        public uint UserIdentity { get; }
        public uint Action { get; }
        public bool CanBeExecuted => m_timeOut.IsActive() && m_timeOut.IsTimeOut();
        public bool IsValid => Kernel.RoleManager.GetUser(UserIdentity) != null;

        public void Clear()
        {
            _ = m_timeOut.Clear();
        }
    }

    public enum TaskActionType
    {
        // System
        ActionSysFirst = 100,
        ActionMenutext = 101,
        ActionMenulink = 102,
        ActionMenuedit = 103,
        ActionMenupic = 104,
        ActionMenuMessage = 105,
        ActionMenubutton = 110,
        ActionMenulistpart = 111,
        ActionMenucreate = 120,
        ActionRand = 121,
        ActionRandaction = 122,
        ActionChktime = 123,
        ActionPostcmd = 124,
        ActionBrocastmsg = 125,
        ActionMessagebox = 126,
        ActionExecutequery = 127,
        ActionInviteTrans = 130,
        ActionVipFunctionCheck = 144, // data is flag data << 1UL
        ActionSysLimit = 199,

        //NPC
        ActionNpcFirst = 200,
        ActionNpcAttr = 201,
        ActionNpcErase = 205,
        ActionNpcModify = 206,
        ActionNpcResetsynowner = 207,
        ActionNpcFindNextTable = 208,
        ActionNpcFamilyCreate = 218,
        ActionNpcFamilyDestroy = 219,
        ActionNpcLimit = 299,

        // Map
        ActionMapFirst = 300,
        ActionMapMovenpc = 301,
        ActionMapMapuser = 302,
        ActionMapBrocastmsg = 303,
        ActionMapDropitem = 304,
        ActionMapSetstatus = 305,
        ActionMapAttrib = 306,
        ActionMapRegionMonster = 307,
        ActionMapRandDropItem = 308,
        ActionMapChangeweather = 310,
        ActionMapChangelight = 311,
        ActionMapMapeffect = 312,
        ActionMapFireworks = 314,
        ActionMapLimit = 399,

        // Lay item
        ActionItemonlyFirst = 400,
        ActionItemRequestlaynpc = 401,
        ActionItemCountnpc = 402,
        ActionItemLaynpc = 403,
        ActionItemDelthis = 498,
        ActionItemonlyLimit = 499,

        // Item
        ActionItemFirst = 500,
        ActionItemAdd = 501,
        ActionItemDel = 502,
        ActionItemCheck = 503,
        ActionItemHole = 504,
        ActionItemRepair = 505,
        ActionItemMultidel = 506,
        ActionItemMultichk = 507,
        ActionItemLeavespace = 508,
        ActionItemUpequipment = 509,
        ActionItemEquiptest = 510,
        ActionItemEquipexist = 511,
        ActionItemEquipcolor = 512,
        ActionItemRemoveAny = 513,
        ActionItemCheckrand = 516,
        ActionItemModify = 517,
        ActionItemJarCreate = 528,
        ActionItemJarVerify = 529,
        ActionItemLimit = 599,

        // Dyn NPCs
        ActionNpconlyFirst = 600,
        ActionNpconlyCreatenewPet = 601,
        ActionNpconlyDeletePet = 602,
        ActionNpconlyMagiceffect = 603,
        ActionNpconlyMagiceffect2 = 604,
        ActionNpconlyLimit = 699,

        // Syndicate
        ActionSynFirst = 700,
        ActionSynCreate = 701,
        ActionSynDestroy = 702,
        ActionSynSetAssistant = 705,
        ActionSynClearRank = 706,
        ActionSynChangeLeader = 709,
        ActionSynAntagonize = 711,
        ActionSynClearAntagonize = 712,
        ActionSynAlly = 713,
        ActionSynClearAlly = 714,
        ActionSynAttr = 717,
        ActionSynLimit = 799,

        //Monsters
        ActionMstFirst = 800,
        ActionMstDropitem = 801,
        ActionMstMagic = 802,
        ActionMstRefinery = 803,
        ActionMstLimit = 899,

        //User
        ActionUserFirst = 1000,
        ActionUserAttr = 1001,
        ActionUserFull = 1002, // Fill the user attributes. param is the attribute name. life/mana/xp/sp
        ActionUserChgmap = 1003, // Mapid Mapx Mapy savelocation
        ActionUserRecordpoint = 1004, // Records the user location, so he can be teleported back there later.
        ActionUserHair = 1005,
        ActionUserChgmaprecord = 1006,
        ActionUserChglinkmap = 1007,
        ActionUserTransform = 1008,
        ActionUserIspure = 1009,
        ActionUserTalk = 1010,
        ActionUserMagic = 1020,
        ActionUserWeaponskill = 1021,
        ActionUserLog = 1022,
        ActionUserBonus = 1023,
        ActionUserDivorce = 1024,
        ActionUserMarriage = 1025,
        ActionUserSex = 1026,
        ActionUserEffect = 1027,
        ActionUserTaskmask = 1028,
        ActionUserMediaplay = 1029,
        ActionUserSupermanlist = 1030,
        ActionUserAddTitle = 1031,
        ActionUserRemoveTitle = 1032,
        ActionUserCreatemap = 1033,
        ActionUserEnterHome = 1034,
        ActionUserEnterMateHome = 1035,
        ActionUserChkinCard2 = 1036,
        ActionUserChkoutCard2 = 1037,
        ActionUserFlyNeighbor = 1038,
        ActionUserUnlearnMagic = 1039,
        ActionUserRebirth = 1040,
        ActionUserWebpage = 1041,
        ActionUserBbs = 1042,
        ActionUserUnlearnSkill = 1043,
        ActionUserDropMagic = 1044,
        ActionUserFixAttr = 1045,
        ActionUserOpenDialog = 1046,
        ActionUserPointAllot = 1047,
        ActionUserExpMultiply = 1048,
        ActionUserDelWpgBadge = 1049,
        ActionUserChkWpgBadge = 1050,
        ActionUserTakestudentexp = 1051,
        ActionUserWhPassword = 1052,
        ActionUserSetWhPassword = 1053,
        ActionUserOpeninterface = 1054,
        ActionUserTaskManager = 1056,
        ActionUserTaskOpe = 1057,
        ActionUserTaskLocaltime = 1058,
        ActionUserTaskFind = 1059,
        ActionUserVarCompare = 1060,
        ActionUserVarDefine = 1061,
        ActionUserVarCalc = 1064,
        ActionUserTestEquipment = 1065,
        ActionUserExecAction = 1071,
        ActionUserTestPos = 1072,
        ActionUserStcCompare = 1073,
        ActionUserStcOpe = 1074,
        ActionUserDataSync = 1075,
        ActionUserSelectToData = 1077,
        ActionUserStcTimeOperation = 1080,
        ActionUserStcTimeCheck = 1081,
        ActionUserAttachStatus = 1082,
        ActionUserGodTime = 1083,
        ActionUserLogEvent = 1085,
        ActionUserExpballExp = 1086,
        ActionSomethingRelatedToRebirth = 1095,
        ActionUserStatusCreate = 1096,
        ActionUserStatusCheck = 1098,

        //User -> Team
        ActionTeamBroadcast = 1101,
        ActionTeamAttr = 1102,
        ActionTeamLeavespace = 1103,
        ActionTeamItemAdd = 1104,
        ActionTeamItemDel = 1105,
        ActionTeamItemCheck = 1106,
        ActionTeamChgmap = 1107,
        ActionTeamChkIsleader = 1501,

        // User -> Events???
        ActionElitePKInscribe = 1301,
        ActionElitePKExit = 1302,
        ActionElitePKCheck = 1303,

        ActionTeamPKInscribe = 1311,
        ActionTeamPKExit = 1312,
        ActionTeamPKCheck = 1313,
        ActionTeamPKUnknown1314 = 1314,
        ActionTeamPKUnknown1315 = 1315,

        ActionSkillTeamPKInscribe = 1321,
        ActionSkillTeamPKExit = 1322,
        ActionSkillTeamPKCheck = 1323,
        ActionSkillTeamPKUnknown1324 = 1324,
        ActionSkillTeamPKUnknown1325 = 1325,

        // User -> General
        ActionGeneralLottery = 1508,
        ActionChgMapSquare = 1509,
        ActionAchievements = 1554,

        ActionUserLimit = 1999,

        //Events
        ActionEventFirst = 2000,
        ActionEventSetstatus = 2001,
        ActionEventDelnpcGenid = 2002,
        ActionEventCompare = 2003,
        ActionEventCompareUnsigned = 2004,
        ActionEventChangeweather = 2005,
        ActionEventCreatepet = 2006,
        ActionEventCreatenewNpc = 2007,
        ActionEventCountmonster = 2008,
        ActionEventDeletemonster = 2009,
        ActionEventBbs = 2010,
        ActionEventErase = 2011,
        ActionEventTeleport = 2012,
        ActionEventMassaction = 2013,

        ActionEventRegister = 2050,
        ActionEventExit = 2051,
        ActionEventCmd = 2052,
        ActionEventLimit = 2099,

        //Traps
        ActionTrapFirst = 2100,
        ActionTrapCreate = 2101,
        ActionTrapErase = 2102,
        ActionTrapCount = 2103,
        ActionTrapAttr = 2104,
        ActionTrapLimit = 2199,

        // Detained Item
        ActionDetainFirst = 2200,
        ActionDetainDialog = 2205,
        ActionDetainLimit = 2299,

        //Wanted
        ActionWantedFirst = 3000,
        ActionWantedNext = 3001,
        ActionWantedName = 3002,
        ActionWantedBonuty = 3003,
        ActionWantedNew = 3004,
        ActionWantedOrder = 3005,
        ActionWantedCancel = 3006,
        ActionWantedModifyid = 3007,
        ActionWantedSuperadd = 3008,
        ActionPolicewantedNext = 3010,
        ActionPolicewantedOrder = 3011,
        ActionPolicewantedCheck = 3012,
        ActionWantedLimit = 3099,

        // Family
        ActionFamilyFirst = 3500,
        ActionFamilyAttr = 3501,
        ActionFamilyMemberAttr = 3510,
        ActionFamilyWarActivityCheck = 3521,
        ActionFamilyWarAuthorityCheck = 3523,
        ActionFamilyWarRegisterCheck = 3524,
        ActionFamilyLast = 3599,

        //Magic
        ActionMagicFirst = 4000,
        ActionMagicAttachstatus = 4001,
        ActionMagicAttack = 4002,
        ActionMagicLimit = 4099
    }

    public enum OpenWindow
    {
        Compose = 1,
        Craft = 2,
        Warehouse = 4,
        ClanWindow = 64,
        DetainRedeem = 336,
        DetainClaim = 337,
        VipWarehouse = 341,
        Breeding = 368,
        PurificationWindow = 455,
        StabilizationWindow = 459,
        TalismanUpgrade = 347,
        GemComposing = 422,
        OpenSockets = 425,
        Blessing = 426,
        TortoiseGemComposing = 438,
        HorseRacingStore = 464,
        EditCharacterName = 489,
        GarmentTrade = 502,
        DegradeEquipment = 506
    }
}