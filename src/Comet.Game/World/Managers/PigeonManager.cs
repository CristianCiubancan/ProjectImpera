// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Pigeon Manager.cs
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
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States;

#endregion

namespace Comet.Game.World.Managers
{
    public sealed class PigeonManager
    {
        private const int PIGEON_PRICE = 5;
        private const int PIGEON_ADDITION = 5;
        private const int PIGEON_TOP_ADDITION = 15;
        private const int PIGEON_MAX_MSG_LENGTH = 80;
        private const int PIGEON_STAND_SECS = 60;

        private List<DbPigeon> m_past = new List<DbPigeon>();
        private List<DbPigeonQueue> m_queue = new List<DbPigeonQueue>();
        private DbPigeon m_current = null;

        private TimeOut m_next = new TimeOut(PIGEON_STAND_SECS);

        public async Task<bool> InitializeAsync()
        {
            m_queue = new List<DbPigeonQueue>((await DbPigeonQueue.GetAsync()).OrderBy(x => x.NextIdentity));
            await SyncAsync();
            return true;
        }

        public async Task<bool> PushAsync(Character sender, string message, bool showError = true, bool forceShow = false)
        {
            if (message.Length > PIGEON_MAX_MSG_LENGTH)
            {
                if (showError)
                    await sender.SendAsync(Language.StrPigeonSendErrStringTooLong);
                return false;
            }

            if (string.IsNullOrEmpty(message))
            {
                if (showError)
                    await sender.SendAsync(Language.StrPigeonSendErrEmptyString);
                return false;
            }

            if (OnQueueByUser(sender.Identity) >= 5 && !sender.IsGm())
            {
                if (showError)
                    await sender.SendAsync(Language.StrPigeonSendOver5Pieces);
                return false;
            }

            if (!await sender.SpendConquerPointsAsync(PIGEON_PRICE, true))
            {
                if (showError)
                    await sender.SendAsync(Language.StrPigeonUrgentErrNoEmoney);
                return false;
            }

            DbPigeonQueue pigeon = new DbPigeonQueue
            {
                UserIdentity = sender.Identity,
                UserName = sender.Name,
                Message = message.Substring(0, Math.Min(message.Length, PIGEON_MAX_MSG_LENGTH)),
                Addition = 0,
                NextIdentity = 0
            };

            await BaseRepository.SaveAsync(pigeon);

            m_queue.Add(pigeon);

            if (forceShow || m_next.IsTimeOut(PIGEON_STAND_SECS))
                await SyncAsync();

            await RebuildQueueAsync();
            await sender.SendAsync(Language.StrPigeonSendProducePrompt);
            return true;
        }

        public async Task AdditionAsync(Character sender, MsgPigeon request)
        {
            DbPigeonQueue pigeon = null;
            int position = 0;

            for (int i = 0; i < m_queue.Count; i++)
            {
                if (m_queue[i].Identity == request.Param &&
                    m_queue[i].UserIdentity == sender.Identity)
                {
                    position = i;
                    pigeon = m_queue[i];
                    break;
                }
            }

            if (pigeon == null)
                return;

            int newPos = 0;
            switch (request.Mode)
            {
                case MsgPigeon.PigeonMode.Urgent:
                    if (!await sender.SpendConquerPointsAsync(PIGEON_ADDITION, false))
                    {
                        await sender.SendAsync(Language.StrPigeonUrgentErrNoEmoney);
                        return;
                    }

                    pigeon.Addition += PIGEON_ADDITION;
                    newPos = Math.Max(0, position - 5);
                    break;
                case MsgPigeon.PigeonMode.SuperUrgent:
                    if (!await sender.SpendConquerPointsAsync(PIGEON_TOP_ADDITION, false))
                    {
                        await sender.SendAsync(Language.StrPigeonUrgentErrNoEmoney);
                        return;
                    }

                    pigeon.Addition += PIGEON_TOP_ADDITION;
                    newPos = 0;
                    break;
            }

            m_queue.RemoveAt(position);
            m_queue.Insert(newPos, pigeon);

            await RebuildQueueAsync();
            await sender.SendAsync(Language.StrPigeonSendProducePrompt);
        }

        public Task RebuildQueueAsync()
        {
            uint idx = 0;
            foreach (var queued in m_queue)
            {
                queued.NextIdentity = idx++;
            }
            return BaseRepository.SaveAsync(m_queue);
        }

        public async Task SyncAsync()
        {
            if (m_queue.Count == 0)
                return;

            m_next.Startup(PIGEON_STAND_SECS);

            await BaseRepository.SaveAsync(m_current = new DbPigeon
            {
                UserIdentity = m_queue[0].UserIdentity,
                UserName = m_queue[0].UserName,
                Addition = m_queue[0].Addition,
                Message = m_queue[0].Message,
                Time = DateTime.Now
            });
            await BaseRepository.DeleteAsync(m_queue[0]);

            m_queue.RemoveAt(0);
            m_past.Add(m_current);

            await RebuildQueueAsync();
            await Kernel.RoleManager.BroadcastMsgAsync(new MsgTalk(m_current.UserIdentity,  MsgTalk.TalkChannel.Broadcast, Color.White, MsgTalk.ALLUSERS, m_current.UserName, m_current.Message));
        }

        public async Task OnTimerAsync()
        {
            if (m_queue.Count == 0)
                return;

            if (!m_next.ToNextTime(PIGEON_STAND_SECS))
                return;

            await SyncAsync();
        }

        public async Task SendListAsync(Character user, MsgPigeon.PigeonMode request)
        {
            List<DbPigeonQueue> temp;
            if (request == MsgPigeon.PigeonMode.Query)
            {
                temp = new List<DbPigeonQueue>(m_queue);
            }
            else
            {
                temp = new List<DbPigeonQueue>(m_queue.FindAll(x => x.UserIdentity == user.Identity));
            }

            uint pos = 0;
            MsgPigeonQuery msg = new MsgPigeonQuery
            {
                Mode = 0
            };
            bool sent = false;
            foreach (var pigeon in temp)
            {
                if (msg.Messages.Count >= 8)
                {
                    sent = true;
                    await user.SendAsync(msg);
                    msg.Messages.Clear();
                }

                msg.Messages.Add(new MsgPigeonQuery.PigeonMessage
                {
                    Identity = pigeon.Identity,
                    UserIdentity = pigeon.UserIdentity,
                    UserName = pigeon.UserName,
                    Addition = pigeon.Addition,
                    Message = pigeon.Message,
                    Position = pos++
                });
            }

            if (msg.Messages.Count > 0 && !sent)
                await user.SendAsync(msg);
        }

        public async Task SendToUserAsync(Character user)
        {
            if (m_current != null)
                await user.SendAsync(new MsgTalk(m_current.UserIdentity, MsgTalk.TalkChannel.Broadcast, Color.White, MsgTalk.ALLUSERS, m_current.UserName, m_current.Message));
        }

        public int OnQueueByUser(uint idUser) => m_queue.Count(x => x.UserIdentity == idUser);
    }
}