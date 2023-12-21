// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Core - Swaps.cs
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
    /// <summary>
    /// This class contains extension methods for providing unsigned integers with swap
    /// functions for swapping values between integers.
    /// </summary>
    public static class Swaps
    {
        /// <summary>Swaps the values of two unsigned integers.</summary>
        /// <param name="n1">The first number</param>
        /// <param name="n2">The first number</param>
        public static void Swap(this ref uint n1, ref uint n2)
        {
            uint tmp = n1;
            n1 = n2;
            n2 = tmp;
        }
    }
}