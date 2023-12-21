// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgQuiz.cs
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgQuiz : MsgBase<Client>
    {
        public QuizAction Action { get; set; }

        /// <remarks>Countdown | Score | Question Number</remarks>
        public ushort Param1 { get; set; }
        /// <remarks>Last Correct Answer | Time Taken | Reward</remarks>
        public ushort Param2 { get; set; }
        /// <remarks>Time Per Question | Exp. Awarded |  Rank</remarks>
        public ushort Param3 { get; set; }
        /// <remarks>First Prize | Time Taken</remarks>
        public ushort Param4 { get; set; }
        /// <remarks>Second Prize | Current Score</remarks>
        public ushort Param5 { get; set; }
        /// <remarks>Third Prize</remarks>
        public ushort Param6 { get; set; }
        public ushort Param7 { get; set; }
        public ushort Param8 { get; set; }
        public ushort Param9 { get; set; }

        public List<string> Strings = new List<string>();
        public List<QuizRank> Scores = new List<QuizRank>();

        public override void Decode(byte[] bytes)
        {
            PacketReader reader = new PacketReader(bytes);
            Length = reader.ReadUInt16(); // 0
            Type = (PacketType)reader.ReadUInt16(); // 2
            Action = (QuizAction) reader.ReadUInt16();
            Param1 = reader.ReadUInt16();
            Param2 = reader.ReadUInt16();
            Param3 = reader.ReadUInt16();
            Param4 = reader.ReadUInt16();
            Param5 = reader.ReadUInt16();
            Param6 = reader.ReadUInt16();
            Param7 = reader.ReadUInt16();
            Param8 = reader.ReadUInt16();
            Param9 = reader.ReadUInt16();
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgQuiz);
            writer.Write((ushort) Action);
            writer.Write(Param1);
            writer.Write(Param2);
            writer.Write(Param3);
            writer.Write(Param4);
            writer.Write(Param5);
            writer.Write(Param6);
            writer.Write(Param7);
            if (Scores.Count > 0)
            {
                writer.Write(Scores.Count);
                foreach (var score in Scores)
                {
                    writer.Write(score.Name, 16);
                    writer.Write(score.Score);
                    writer.Write(score.Time);
                }
            }
            else
            {
                writer.Write(Param8);
                writer.Write(Param9);
                writer.Write(Strings);
            }

            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            QuizShow quiz = Kernel.EventThread.GetEvent<QuizShow>();
            if (quiz == null)
                return;

            switch (Action)
            {
                case QuizAction.Reply:
                {
                    if (quiz.IsCanceled(user.Identity))
                        return;

                    await quiz.OnReplyAsync(user, Param1, Param2);
                    return;
                }

                case QuizAction.Quit:
                {
                    if (quiz.IsCanceled(user.Identity))
                        return;

                    quiz.Cancel(user.Identity);
                    return;
                }

                default:
                {
                    await client.SendAsync(this);
                    if (client.Character.IsPm())
                    {
                        await client.SendAsync(new MsgTalk(client.Identity, MsgTalk.TalkChannel.Service,
                            $"Missing packet {Type}, Action {Action}, Length {Length}"));
                    }

                    await Log.WriteLogAsync(LogLevel.Warning,
                        "Missing packet {0}, Action {1}, Length {2}\n{3}",
                        Type, Action, Length, PacketDump.Hex(Encode()));
                        return;
                }
            }
        }

        public struct QuizRank
        {
            public string Name { get; set; }
            public ushort Score { get; set; }
            public ushort Time { get; set; }
        }

        public enum QuizAction : ushort
        {
            None,
            Start,
            Question,
            Reply,
            AfterReply,
            Finish,
            Quit = 8
        }
    }
}