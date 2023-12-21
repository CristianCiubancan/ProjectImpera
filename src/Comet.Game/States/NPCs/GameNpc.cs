// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Game Npc.cs
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

using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;

namespace Comet.Game.States.NPCs
{
    public sealed class Npc : BaseNpc
    {
        private DbNpc m_dbNpc;

        public Npc(DbNpc npc) 
            : base(npc.Id)
        {
            m_dbNpc = npc;

            m_idMap = npc.Mapid;
            m_posX = npc.Cellx;
            m_posY = npc.Celly;

            Name = npc.Name;
        }

        #region Type

        public override ushort Type => m_dbNpc.Type;

        public override NpcSort Sort => (NpcSort) m_dbNpc.Sort;

        public override uint OwnerIdentity
        {
            get => m_dbNpc.Ownerid;
            set => m_dbNpc.Ownerid = value;
        }

        #endregion

        #region Map and Position

        public override async Task<bool> ChangePosAsync(uint idMap, ushort x, ushort y)
        {
            if (await base.ChangePosAsync(idMap, x, y))
            {
                m_dbNpc.Mapid = idMap;
                m_dbNpc.Celly = y;
                m_dbNpc.Cellx = x;
                await SaveAsync();
                return true;
            }
            return false;
        }

        #endregion

        #region Task and Data

        public override uint Task0 => m_dbNpc.Task0;
        public override uint Task1 => m_dbNpc.Task1;
        public override uint Task2 => m_dbNpc.Task2;
        public override uint Task3 => m_dbNpc.Task3;
        public override uint Task4 => m_dbNpc.Task4;
        public override uint Task5 => m_dbNpc.Task5;
        public override uint Task6 => m_dbNpc.Task6;
        public override uint Task7 => m_dbNpc.Task7;

        public override int Data0 { get => m_dbNpc.Data0; set => m_dbNpc.Data0 = value; }
        public override int Data1 { get => m_dbNpc.Data1; set => m_dbNpc.Data1 = value; }
        public override int Data2 { get => m_dbNpc.Data2; set => m_dbNpc.Data2 = value; }
        public override int Data3 { get => m_dbNpc.Data3; set => m_dbNpc.Data3 = value; }
        public override string DataStr { get => m_dbNpc.Datastr; set => m_dbNpc.Datastr = value; }

        #endregion

        #region Socket

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgNpcInfo
            {
                Identity = Identity,
                Lookface = m_dbNpc.Lookface,
                Sort = m_dbNpc.Sort,
                PosX = MapX,
                PosY = MapY,
                Name = Name,
                NpcType = m_dbNpc.Type
            });
        }

        #endregion

        #region Database

        public override async Task<bool> SaveAsync()
        {
            return await BaseRepository.SaveAsync(m_dbNpc);
        }

        public override async Task<bool> DeleteAsync()
        {
            return await BaseRepository.DeleteAsync(m_dbNpc);
        }

        #endregion
    }
}