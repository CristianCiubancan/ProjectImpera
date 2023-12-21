// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Shared - Unix Timestamp.cs
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

#endregion

namespace Comet.Shared
{
    public static class UnixTimestamp
    {
        public const int TIME_SECONDS_MINUTE = 60;
        public const int TIME_SECONDS_HOUR = 60 * TIME_SECONDS_MINUTE;
        public const int TIME_SECONDS_DAY = 24 * TIME_SECONDS_HOUR;
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

        #region Date Time Related Functions

        public static DateTime ToDateTime(uint timestamp)
        {
            return UnixEpoch.AddSeconds(timestamp);
        }

        public static DateTime ToDateTime(int timestamp)
        {
            return UnixEpoch.AddSeconds(timestamp);
        }

        public static int Now()
        {
            return Convert.ToInt32((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime())
                .TotalSeconds);
        }

        public static long NowMs()
        {
            return Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime())
                .TotalMilliseconds);
        }

        public static int Timestamp(DateTime time)
        {
            return Convert.ToInt32((time - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()).TotalSeconds);
        }

        public static long LongTimestamp(DateTime time)
        {
            return Convert.ToInt64((time - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()).TotalMilliseconds);
        }

        public static int MonthDayStamp()
        {
            return Convert.ToInt32((DateTime.Now - new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, 0).ToLocalTime())
                .TotalSeconds);
        }

        public static int MonthDayStamp(DateTime time)
        {
            return Convert.ToInt32((time - new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, 0).ToLocalTime())
                .TotalSeconds);
        }

        public static int DayOfTheMonthStamp()
        {
            return Convert.ToInt32((DateTime.Now -
                                    new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, 0).ToLocalTime()
                ).TotalSeconds);
        }

        public static int DayOfTheMonthStamp(DateTime time)
        {
            return Convert.ToInt32(
                (time - new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, 0).ToLocalTime())
                .TotalSeconds);
        }

        #endregion
    }
}