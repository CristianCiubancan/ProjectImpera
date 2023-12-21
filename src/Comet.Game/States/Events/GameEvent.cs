// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - GameEvent.cs
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
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;
using Comet.Game.States.Magics;
using Comet.Game.World.Maps;

#endregion

namespace Comet.Game.States.Events
{
    public abstract class GameEvent
    {
        protected enum EventStage
        {
            Idle,
            Running,
            Ending
        }

        public enum EventType
        {
            None,
            TimedGuildWar,
            GuildPk,
            GuildContest,
            LineSkillPk,
            ArenaQualifier,
            QuizShow,
            Limit
        }

        public const int RANK_REFRESH_RATE_MS = 10000;

        private TimeOutMS m_eventCheck;

        protected GameEvent(string name, int timeCheck = 1000)
        {
            Name = name;
            m_eventCheck = new TimeOutMS(timeCheck);
        }

        public virtual EventType Identity { get; } = EventType.None;

        public string Name { get; }

        protected EventStage Stage { get; set; } = EventStage.Idle;

        public virtual GameMap Map { get; protected set; }

        public virtual bool IsInTime { get; } = false;
        public virtual bool IsActive { get; } = false;
        public virtual bool IsEnded { get; } = false;

        public virtual bool IsAttackEnable(Role sender) => true;

        public bool ToNextTime() => m_eventCheck.ToNextTime();

        public virtual bool IsAllowedToJoin(Role sender)
        {
            return true;
        }

        public virtual Task<bool> CreateAsync()
        {
            return Task.FromResult(true);
        }

        public virtual Task OnEnterAsync(Character sender)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnExitAsync(Character sender)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnMoveAsync(Character sender)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAttackAsync(Character sender)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnBeAttackAsync(Role attacker, Role target, int damage = 0, Magic magic = null)
        {
            return Task.CompletedTask;
        }

        public virtual Task<int> GetDamageLimitAsync(Role attacker, Role target, int power)
        {
            return Task.FromResult(power);
        }

        public virtual Task OnHitAsync(Role attacker, Role target, Magic magic = null) // magic null is auto attack
        {
            return Task.CompletedTask;
        }

        public virtual Task OnKillAsync(Role attacker, Role target, Magic magic = null)
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> OnReviveAsync(Character sender, bool selfRevive)
        {
            return Task.FromResult(false);
        }

        public virtual Task OnTimerAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> OnActionCommandAsync(string param, Character user, Role role, Item item, string input)
        {
            return Task.FromResult(true);
        }

        public virtual Task<(uint id, ushort x, ushort y)> GetRevivePositionAsync(Character sender)
        {
            return Task.FromResult((sender.RecordMapIdentity, sender.RecordMapX, sender.RecordMapY));
        }
    }
}