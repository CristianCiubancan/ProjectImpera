// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - MsgData.cs
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
using Comet.Game.States;
using Comet.Network.Packets;

#endregion

namespace Comet.Game.Packets
{
    public sealed class MsgData : MsgBase<Client>
    {
        public enum DataAction
        {
            SetServerTime = 0,
            SetMountMovePoint = 2,
            AntiCheatAnswerMsgTypeCount = 3,
            AntiCheatAskMsgTypeCount = 4
        }

        public MsgData()
        {
            Type = PacketType.MsgData;
        }

        public MsgData(DateTime time)
        {
            Type = PacketType.MsgData;
            Action = DataAction.SetServerTime;
            Year = time.Year - 1900;
            Month = time.Month - 1;
            DayOfYear = time.DayOfYear;
            Day = time.Day;
            Hours = time.Hour;
            Minutes = time.Minute;
            Seconds = time.Second;
        }

        public DataAction Action { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int DayOfYear { get; set; }
        public int Day { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }

        public override byte[] Encode()
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((ushort) Type);
            writer.Write((uint) Action);
            writer.Write(Year);
            writer.Write(Month);
            writer.Write(DayOfYear);
            writer.Write(Day);
            writer.Write(Hours);
            writer.Write(Minutes);
            writer.Write(Seconds);
            return writer.ToArray();
        }
    }
}