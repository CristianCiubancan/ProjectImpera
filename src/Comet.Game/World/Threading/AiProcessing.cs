// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Ai Processing.cs
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
using System.Threading.Tasks;
using Comet.Shared;
using Comet.Shared.Comet.Shared;

#endregion

namespace Comet.Game.World.Threading
{
    public sealed class AiProcessor : TimerBase
    {
        public AiProcessor() 
            : base(500, "Ai Thread")
        {
        }

        public int ProcessedMonsters { get; private set; }

        protected override async Task<bool> OnElapseAsync()
        {
            try
            {
                ProcessedMonsters = 0;
                foreach (var map in Kernel.MapManager.GameMaps.Values)
                    ProcessedMonsters += await map.OnTimerAsync();
                await Kernel.RoleManager.OnRoleTimerAsync();
            }
            catch (Exception ex)
            {
Console.WriteLine(ex.ToString());
            }

            return true;
        }
    }
}