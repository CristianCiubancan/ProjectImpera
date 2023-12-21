// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Transformation.cs
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

using Comet.Core.Mathematics;
using Comet.Game.Database.Models;
using Comet.Game.States.BaseEntities;

#endregion

namespace Comet.Game.States
{
    public sealed class Transformation
    {
        private DbMonstertype m_dbMonster;
        private readonly Role m_pOwner;

        public Transformation(Role pOwner)
        {
            m_pOwner = pOwner;
        }

        public ushort Life { get; set; }

        public int MaxLife => m_dbMonster.Life;

        public int MinAttack => m_dbMonster.AttackMin;

        public int MaxAttack => m_dbMonster.AttackMax;

        public int Attack => (MinAttack + MaxAttack) / 2;

        public int Defense => m_dbMonster.Defence;

        public uint Defense2 => m_dbMonster.Defence2;

        public uint Dexterity => m_dbMonster.Dexterity;

        public uint Dodge => m_dbMonster.Dodge;

        public int MagicDefense => m_dbMonster.MagicDef;

        public int InterAtkRate
        {
            get
            {
                int nRate = IntervalAtkRate;
                IStatus pStatus = m_pOwner.QueryStatus(StatusSet.CYCLONE);
                if (pStatus != null)
                    nRate = Calculations.CutTrail(0, Calculations.AdjustDataEx(nRate, pStatus.Power, 0));
                return nRate;
            }
        }

        public int IntervalAtkRate => m_dbMonster.AttackSpeed;

        public int MagicHitRate => (int) m_dbMonster.MagicHitrate;

        public int Lookface => m_dbMonster.Lookface;

        public bool IsJumpEnable => (m_dbMonster.AttackUser & Monster.ATKUSER_JUMP) != 0;

        public bool IsMoveEnable => (m_dbMonster.AttackUser & Monster.ATKUSER_FIXED) == 0;

        public bool Create(DbMonstertype pTrans)
        {
            if (m_pOwner == null || pTrans == null || pTrans.Life <= 0)
                return false;

            m_dbMonster = pTrans;
            Life = (ushort) Calculations.CutTrail(1, Calculations.MulDiv(m_pOwner.Life, MaxLife, m_pOwner.MaxLife));

            return true;
        }

        public int GetAttackRange(int nTargetSizeAdd)
        {
            return (int) ((m_dbMonster.AttackRange + GetSizeAdd() + nTargetSizeAdd + 1) / 2);
        }

        public uint GetSizeAdd()
        {
            return m_dbMonster.SizeAdd;
        }
    }
}