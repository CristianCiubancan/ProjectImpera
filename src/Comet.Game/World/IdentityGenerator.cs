// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Identity Generator.cs
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

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Comet.Game.States.BaseEntities;

#endregion

namespace Comet.Game.World
{
    public sealed class IdentityGenerator
    {
        public static IdentityGenerator MapItem = new IdentityGenerator(Role.MAPITEM_FIRST, Role.MAPITEM_LAST);
        public static IdentityGenerator Monster = new IdentityGenerator(Role.MONSTERID_FIRST, Role.MONSTERID_LAST);
        public static IdentityGenerator Furniture = new IdentityGenerator(Role.SCENE_NPC_MIN, Role.SCENE_NPC_MAX);
        public static IdentityGenerator Traps = new IdentityGenerator(Role.MAGICTRAPID_FIRST, Role.MAGICTRAPID_LAST);

        private readonly ConcurrentQueue<long> m_cqidQueue = new ConcurrentQueue<long>();
        private readonly long m_idMax = uint.MaxValue;
        private readonly long m_idMin;
        private long m_idNext;

        public IdentityGenerator(long min, long max)
        {
            m_idNext = m_idMin = min;
            m_idMax = max;

            for (long i = m_idMin; i <= m_idMax; i++)
            {
                m_cqidQueue.Enqueue(i);
            }

            m_idNext = m_idMax + 1;
        }

        public long GetNextIdentity
        {
            get
            {
                if (m_cqidQueue.TryDequeue(out long result))
                    return result;
                return 0;
            }
        }

        public void ReturnIdentity(long id)
        {
            if (!m_cqidQueue.Contains(id))
                m_cqidQueue.Enqueue(id);
        }

        public int IdentitiesCount() => m_cqidQueue.Count;
    }
}