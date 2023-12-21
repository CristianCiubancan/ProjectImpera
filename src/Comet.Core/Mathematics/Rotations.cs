// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Core - Rotations.cs
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
    ///     This class contains extension methods for providing unsigned integers with bitwise
    ///     rotation / circular shift functions. Rotations in this implementation follow the C
    ///     compiler standard for ANSI C language instructions for rotations (32-bit rotations).
    /// </summary>
    public static class Rotations
    {
        /// <summary>Performs a bitwise left rotation.</summary>
        /// <param name="value">Current integer to be rotated</param>
        /// <param name="count">Number of bytes to be shifted by</param>
        /// <returns>Returns the resulting rotation.</returns>
        public static uint RotateLeft(this uint value, int count)
        {
            return (value << (count % 32)) | (value >> (32 - count % 32));
        }

        /// <summary>Performs a bitwise right rotation.</summary>
        /// <param name="value">Current integer to be rotated</param>
        /// <param name="count">Number of bytes to be shifted by</param>
        /// <returns>Returns the resulting rotation.</returns>
        public static uint RotateRight(this uint value, int count)
        {
            return (value >> (count % 32)) | (value << (32 - count % 32));
        }
    }
}