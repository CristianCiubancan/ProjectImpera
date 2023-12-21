// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Message Board.cs
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
using System.Linq;
using Comet.Game.Packets;

#endregion

namespace Comet.Game.States
{
    public static class MessageBoard
    {
        private static Dictionary<uint, MessageInfo> m_dicTrade = new Dictionary<uint, MessageInfo>();
        private static Dictionary<uint, MessageInfo> m_dicTTeam = new Dictionary<uint, MessageInfo>();
        private static Dictionary<uint, MessageInfo> m_dicFriend = new Dictionary<uint, MessageInfo>();
        private static Dictionary<uint, MessageInfo> m_dicSyndicate = new Dictionary<uint, MessageInfo>();
        private static Dictionary<uint, MessageInfo> m_dicOther = new Dictionary<uint, MessageInfo>();
        private static Dictionary<uint, MessageInfo> m_dicSystem = new Dictionary<uint, MessageInfo>();

        public static bool AddMessage(Character user, string message, MsgTalk.TalkChannel channel)
        {
            Dictionary<uint, MessageInfo> board;
            switch (channel)
            {
                case MsgTalk.TalkChannel.TradeBoard:
                    board = m_dicTrade;
                    break;
                case MsgTalk.TalkChannel.TeamBoard:
                    board = m_dicTTeam;
                    break;
                case MsgTalk.TalkChannel.FriendBoard:
                    board = m_dicFriend;
                    break;
                case MsgTalk.TalkChannel.GuildBoard:
                    board = m_dicSyndicate;
                    break;
                case MsgTalk.TalkChannel.OthersBoard:
                    board = m_dicOther;
                    break;
                case MsgTalk.TalkChannel.Bbs:
                    board = m_dicSystem;
                    break;
                default:
                    return false;
            }

            if (board.ContainsKey(user.Identity))
                board.Remove(user.Identity);

            // todo verify silence
            // todo filter words

            board.Add(user.Identity, new MessageInfo
            {
                SenderIdentity = user.Identity,
                Sender = user.Name,
                Message = message.Substring(0, Math.Min(message.Length, 255)),
                Time = DateTime.Now
            });

            return true;
        }

        public static List<MessageInfo> GetMessages(MsgTalk.TalkChannel channel, int page)
        {
            List<MessageInfo> msgs;
            switch (channel)
            {
                case MsgTalk.TalkChannel.TradeBoard:
                    msgs = m_dicTrade.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.TeamBoard:
                    msgs = m_dicTTeam.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.FriendBoard:
                    msgs = m_dicFriend.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.GuildBoard:
                    msgs = m_dicSyndicate.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.OthersBoard:
                    msgs = m_dicOther.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.Bbs:
                    msgs = m_dicSystem.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                default:
                    return new List<MessageInfo>();
            }

            if (page * 8 > msgs.Count)
                return new List<MessageInfo>();

            return msgs.Skip(page * 8).Take(8).ToList();
        }

        public static string GetMessage(string name, MsgTalk.TalkChannel channel)
        {
            List<MessageInfo> msgs;
            switch (channel)
            {
                case MsgTalk.TalkChannel.TradeBoard:
                    msgs = m_dicTrade.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.TeamBoard:
                    msgs = m_dicTTeam.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.FriendBoard:
                    msgs = m_dicFriend.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.GuildBoard:
                    msgs = m_dicSyndicate.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.OthersBoard:
                    msgs = m_dicOther.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case MsgTalk.TalkChannel.Bbs:
                    msgs = m_dicSystem.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                default:
                    return string.Empty;
            }

            return msgs.FirstOrDefault(x => x.Sender.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Message ?? string.Empty;
        }
    }

    public struct MessageInfo
    {
        public uint SenderIdentity;
        public string Sender;
        public string Message;
        public DateTime Time;
    }
}