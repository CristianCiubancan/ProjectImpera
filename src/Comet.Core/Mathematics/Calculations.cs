// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Core - Calculations.cs
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

namespace Comet.Core.Mathematics
{
    public static class Calculations
    {
        public const int ADJUST_PERCENT = 30000; // ADJUSTÊ±£¬>=30000 ±íÊ¾°Ù·ÖÊý
        public const int ADJUST_SET = -30000; // ADJUSTÊ±£¬<=-30000 ±íÊ¾µÈÓÚ(-1*num - 30000)
        public const int ADJUST_FULL = short.MinValue; // ADJUSTÊ±£¬== -32768 ±íÊ¾ÌîÂú
        public const int DEFAULT_DEFENCE2 = 10000; // Êý¾Ý¿âÈ±Ê¡Öµ

        public static int ChangeAdjustRate(int nData, int divideBy)
        {
            if (divideBy == 0)
                return nData;

            if (nData >= ADJUST_PERCENT)
                return ADJUST_PERCENT + ((nData % ADJUST_PERCENT) / divideBy);

            return nData / divideBy;
        }

        public static int AdjustData(int nData, int nAdjust, int nMaxData = 0)
        {
            return AdjustDataEx(nData, nAdjust, nMaxData);
        }

        public static int AdjustDataEx(int nData, int nAdjust, int nMaxData = 0)
        {
            if (nAdjust >= ADJUST_PERCENT)
                return MulDiv(nData, nAdjust - ADJUST_PERCENT, 100);

            if (nAdjust <= ADJUST_SET)
                return -1 * nAdjust + ADJUST_SET;

            if (nAdjust == ADJUST_FULL)
                return nMaxData;

            return nData + nAdjust;
        }

        public static long AdjustData(long nData, long nAdjust, long nMaxData = 0)
        {
            return AdjustDataEx(nData, nAdjust, nMaxData);
        }

        public static long AdjustDataEx(long nData, long nAdjust, long nMaxData)
        {
            if (nAdjust >= ADJUST_PERCENT)
                return MulDiv(nData, nAdjust - ADJUST_PERCENT, 100);

            if (nAdjust <= ADJUST_SET)
                return -1 * nAdjust + ADJUST_SET;

            if (nAdjust == ADJUST_FULL)
                return nMaxData;

            return nData + nAdjust;
        }

        public static int MulDiv(byte number, byte numerator, byte denominator)
        {
            return number * numerator / denominator;
        }

        public static int MulDiv(short number, short numerator, short denominator)
        {
            return number * numerator / denominator;
        }

        public static int MulDiv(ushort number, ushort numerator, ushort denominator)
        {
            return number * numerator / denominator;
        }

        public static int MulDiv(int number, int numerator, int denominator)
        {
            return (int) ((long) number * numerator / denominator);
        }

        public static uint MulDiv(uint number, uint numerator, uint denominator)
        {
            return number * numerator / denominator;
        }

        public static long MulDiv(long number, long numerator, long denominator)
        {
            return number * numerator / denominator;
        }

        public static ulong MulDiv(ulong number, ulong numerator, ulong denominator)
        {
            return number * numerator / denominator;
        }

        public static long CutTrail(long x, long y)
        {
            return x >= y ? x : y;
        }

        public static long CutOverflow(long x, long y)
        {
            return x <= y ? x : y;
        }

        public static long CutRange(long n, long min, long max)
        {
            return n < min ? min : n > max ? max : n;
        }

        public static int CutTrail(int x, int y)
        {
            return x >= y ? x : y;
        }

        public static int CutOverflow(int x, int y)
        {
            return x <= y ? x : y;
        }

        public static int CutRange(int n, int min, int max)
        {
            return n < min ? min : n > max ? max : n;
        }

        public static short CutTrail(short x, short y)
        {
            return x >= y ? x : y;
        }

        public static short CutOverflow(short x, short y)
        {
            return x <= y ? x : y;
        }

        public static short CutRange(short n, short min, short max)
        {
            return n < min ? min : n > max ? max : n;
        }

        public static ulong CutTrail(ulong x, ulong y)
        {
            return x >= y ? x : y;
        }

        public static ulong CutOverflow(ulong x, ulong y)
        {
            return x <= y ? x : y;
        }

        public static ulong CutRange(ulong n, ulong min, ulong max)
        {
            return n < min ? min : n > max ? max : n;
        }

        public static uint CutTrail(uint x, uint y)
        {
            return x >= y ? x : y;
        }

        public static uint CutOverflow(uint x, uint y)
        {
            return x <= y ? x : y;
        }

        public static uint CutRange(uint n, uint min, uint max)
        {
            return n < min ? min : n > max ? max : n;
        }

        public static ushort CutTrail(ushort x, ushort y)
        {
            return x >= y ? x : y;
        }

        public static ushort CutOverflow(ushort x, ushort y)
        {
            return x <= y ? x : y;
        }

        public static ushort CutRange(ushort n, ushort min, ushort max)
        {
            return n < min ? min : n > max ? max : n;
        }

        public static byte CutTrail(byte x, byte y)
        {
            return x >= y ? x : y;
        }

        public static byte CutOverflow(byte x, byte y)
        {
            return x <= y ? x : y;
        }

        public static byte CutRange(byte n, byte min, byte max)
        {
            return n < min ? min : n > max ? max : n;
        }
    }
}