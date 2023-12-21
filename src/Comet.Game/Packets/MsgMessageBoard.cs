// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgMessageBoard.cs
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
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgMessageBoard : MsgBase<Client>
    {
        public enum BoardAction : byte
        {
            None = 0,
            Del = 1,			        // to server					// no return
            GetList = 2,		    	// to server: index(first index)
            List = 3,		        	// to client: index(first index), name, words, time...
            GetWords = 4,	    		// to server: index(for get)	// return by MsgTalk
        };

        public enum BoardChannel : ushort
        {
            None = 0,
            MsgTrade = 2201,
            MsgFriend = 2202,
            MsgTeam = 2203,
            MsgSyn = 2204,
            MsgOther = 2205,
            MsgSystem = 2206,
        };

        public MsgMessageBoard()
        {
            Type = PacketType.MsgMessageBoard;
        }

        public ushort Index { get; set; }
        public BoardChannel Channel { get; set; }
        public BoardAction Action { get; set; }
        public List<string> Messages = new List<string>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Index = reader.ReadUInt16();
            Channel = (BoardChannel) reader.ReadUInt16();
            Action = (BoardAction) reader.ReadByte();
            Messages = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write(Index);
            writer.Write((ushort) Channel);
            writer.Write((byte) Action);
            writer.Write(Messages);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Action)
            {
                case BoardAction.GetList:
                    var list = MessageBoard.GetMessages((MsgTalk.TalkChannel) Channel, Index);
                    if (list.Count == 0)
                        return;

                    foreach (var msg in list)
                    {
                        if (Messages.Count >= 8)
                            break;

                        Messages.Add(msg.Sender);
                        Messages.Add(msg.Message.Substring(0, Math.Min(44, msg.Message.Length)));
                        Messages.Add(msg.Time.ToString("yyyyMMddHHmmss"));
                    }

                    Action = BoardAction.List;
                    await user.SendAsync(this);
                    break;

                case BoardAction.GetWords:
                    string message = MessageBoard.GetMessage(Messages[0], (MsgTalk.TalkChannel) Channel);
                    await user.SendAsync(new MsgTalk
                    {
                        Channel = (MsgTalk.TalkChannel) Channel,
                        Color = Color.White,
                        Message = message,
                        SenderName = Messages[0],
                        RecipientName = user.Name,
                        Style = MsgTalk.TalkStyle.Normal,
                        Suffix = ""
                    });
                    break;
            }
        }
    }
}