// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - TotemPole.cs
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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;

#endregion

namespace Comet.Game.States.Syndicates
{
    public sealed class TotemPole
    {
        private DbTotemAdd m_Enhancement;

        public ConcurrentDictionary<uint, Totem> Totems = new ConcurrentDictionary<uint, Totem>();

        public TotemPole(Syndicate.TotemPoleType flag, DbTotemAdd enhance = null)
        {
            Type = flag;
            m_Enhancement = enhance;
        }

        public Syndicate.TotemPoleType Type { get; }
        public long Donation => Totems.Values.Sum(x => x.Points);
        public bool Locked { get; set; } = true;

        public int Enhancement
        {
            get => EnhancementExpiration.HasValue && EnhancementExpiration.Value > DateTime.Now
                ? m_Enhancement.BattleAddition
                : 0;
            set => m_Enhancement.BattleAddition = (byte) Math.Min(2, Math.Max(0, value));
        }

        public DateTime? EnhancementExpiration => m_Enhancement?.TimeLimit;

        public int BattlePower
        {
            get
            {
                int result = 0;
                long donation = Donation;
                if (donation >= 2000000)
                    result++;
                if (donation >= 4000000)
                    result++;
                if (donation >= 10000000)
                    result++;
                return Math.Min(3, result);
            }
        }

        public int SharedBattlePower => Enhancement + BattlePower;

        public int GetUserContribution(uint idUser)
        {
            return Totems.Values.Where(x => x.PlayerIdentity == idUser).Sum(x => x.Points);
        }

        public Task<bool> SetEnhancementAsync(DbTotemAdd totem)
        {
            m_Enhancement = totem;
            if (totem != null)
                return BaseRepository.SaveAsync(m_Enhancement);
            return Task.FromResult(true);
        }

        public async Task<bool> RemoveEnhancementAsync()
        {
            if (m_Enhancement != null)
            {
                await BaseRepository.DeleteAsync(m_Enhancement);
                m_Enhancement = null;
                return true;
            }
            return true;
        }
    }
}