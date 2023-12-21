// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Monster.cs
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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.Mathematics;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Items;
using Comet.Game.States.Magics;
using Comet.Game.World;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;

#endregion

namespace Comet.Game.States
{
    public class Monster : Role, IRoleAi
    {
        private readonly Generator m_generator;
        private readonly DbMonstertype m_dbMonster;

        private TimeOutMS m_tStatusCheck = new TimeOutMS(500);
        private TimeOutMS m_tAttackMs = new TimeOutMS();
        private TimeOutMS m_tAction = new TimeOutMS();
        private TimeOutMS m_tMoveMs = new TimeOutMS();
        private TimeOut m_tHealPeriod = new TimeOut(2);
        private TimeOut m_disappear = new TimeOut(5);
        private TimeOut m_leaveMap = new TimeOut(10);

        private AiStage m_stage;
        private FacingDirection m_nextDir = FacingDirection.Invalid;

        private Role m_actTarget;
        private Role m_moveTarget;
        private Role m_attackMe;

        private bool m_bAheadPath;
        private bool m_atkFirst;

        private uint m_idRole = 0;

        private uint m_idUseMagic = 0;
        private List<DbMonsterMagic> m_monsterMagics = new List<DbMonsterMagic>();

        public Monster(DbMonstertype type, uint identity, Generator generator)
        {
            m_dbMonster = type;
            m_generator = generator;
            m_idRole = identity;

            m_idMap = generator.MapIdentity;

            m_tStatusCheck.Update();

            m_tMoveMs.Startup(m_dbMonster.MoveSpeed);
            m_tAttackMs.Startup(m_dbMonster.AttackSpeed);
            m_tAction.Startup(500);
        }
        
        #region Identity

        public override uint Identity
        {
            get => m_idRole;
            protected set { }
        }

        public override string Name
        {
            get => m_dbMonster.Name;
            set => m_dbMonster.Name = value;
        }

        #endregion

        #region Initialization

        public async Task<bool> InitializeAsync(uint idMap, ushort x, ushort y)
        {
            m_idMap = idMap;

            if ((Map = Kernel.MapManager.GetMap(idMap)) == null)
                return false;

            m_posX = x;
            m_posY = y;

            Life = MaxLife;

            m_idUseMagic = m_dbMonster.MagicType;
            m_monsterMagics = Kernel.RoleManager.GetMonsterMagics(m_dbMonster.Type);

            if (m_dbMonster.MagicType > 0)
            {
                Magic defaultMagic = new Magic(this);
                if (await defaultMagic.CreateAsync(m_dbMonster.MagicType))
                    MagicData.Magics.TryAdd(defaultMagic.Type, defaultMagic);
            }

            foreach (var dbMagic in m_monsterMagics)
            {
                Magic magic = new Magic(this);
                if (await magic.CreateAsync(dbMagic.MagicIdentity, dbMagic.MagicLevel))
                    MagicData.Magics.TryAdd(magic.Type, magic);
            }

            return true;
        }

        #endregion

        #region Appearence

        public uint Type => m_dbMonster.Id;

        public override uint Mesh
        {
            get => m_dbMonster.Lookface;
            set => m_dbMonster.Lookface = (ushort) value;
        }

        #endregion

        #region Battle Attributes

        public override byte Level
        {
            get => (byte)(m_dbMonster?.Level ?? 0);
            set => m_dbMonster.Level = value;
        }

        public override uint Life
        {
            get;
            set;
        }

        public override uint MaxLife => (uint)(m_dbMonster?.Life ?? 1);

        public override int BattlePower => m_dbMonster?.ExtraBattlelev ?? 0;

        public override int MinAttack => m_dbMonster?.AttackMin ?? 0;

        public override int MaxAttack => m_dbMonster?.AttackMax ?? 0;

        public override int ExtraDamage => m_dbMonster?.ExtraDamage ?? 0;

        public override int MagicAttack => m_dbMonster?.AttackMax ?? 0;

        public override int Defense => m_dbMonster?.Defence ?? 0;

        public override int MagicDefense => m_dbMonster?.MagicDef ?? 0;

        public override int Dodge => (int) (m_dbMonster?.Dodge ?? 0);

        public override int AttackSpeed => m_dbMonster?.AttackSpeed ?? 1000;

        public override int Accuracy => (int) (m_dbMonster?.Dexterity ?? 0);

        public uint AttackUser => m_dbMonster?.AttackUser ?? 0;

        public int ViewRange => m_dbMonster?.ViewRange ?? 1;
        
        #endregion

        #region Battle

        public override int GetAttackRange(int sizeAdd)
        {
            return m_dbMonster.AttackRange + sizeAdd;
        }

        private async Task<bool> CheckMagicAttackAsync()
        {
            if (m_actTarget == null)
                return false;

            foreach (var magic in m_monsterMagics.OrderBy(x => x.Chance))
            {
                if (MagicData.CheckType(magic.MagicIdentity) && await Kernel.ChanceCalcAsync((int) magic.Chance, 10000))
                {
                    m_idUseMagic = magic.MagicIdentity;
                    return true;
                }
            }

            if (m_idUseMagic == 0 && m_dbMonster.MagicType != 0 &&
                await Kernel.ChanceCalcAsync(m_dbMonster.MagicHitrate))
                m_idUseMagic = m_dbMonster.MagicType;

            return false;
        }

        public Task DoAttackAsync()
        {
            if (m_actTarget == null)
                return Task.CompletedTask;

            if (!m_tAttackMs.ToNextTime(AttackSpeed))
                return Task.CompletedTask;

            if (m_idUseMagic != 0)
            {
                QueueAction(async () => await ProcessMagicAttackAsync((ushort)m_idUseMagic, m_actTarget.Identity, m_actTarget.MapX, m_actTarget.MapY));
            }
            else
            {
                BattleSystem.CreateBattle(m_actTarget.Identity);
                QueueAction(async () => await BattleSystem.ProcessAttackAsync());
            }

            return Task.CompletedTask;
        }

        public override bool IsAttackable(Role attacker)
        {
            if (!IsAlive)
                return false;

            return base.IsAttackable(attacker);
        }

        public override async Task<bool> BeAttackAsync(BattleSystem.MagicType magic, Role attacker, int nPower, bool bReflectEnable)
        {
            if (!IsAlive)
                return false;

            await AddAttributesAsync(ClientUpdateType.Hitpoints, nPower * -1);

            if (!IsAlive)
            {
                await BeKillAsync(attacker);
                return true;
            }

            if (m_disappear.IsActive())
                return false;

            return true;
        }

        public override async Task BeKillAsync(Role attacker)
        {
            if (m_disappear.IsActive())
                return;

            if (attacker?.BattleSystem.IsActive() == true)
                attacker.BattleSystem.ResetBattle();

            if (attacker?.MagicData.QueryMagic != null)
                await attacker.MagicData.AbortMagicAsync(false);

            await DetachAllStatusAsync();
            await AttachStatusAsync(attacker, StatusSet.FADE, 0, int.MaxValue, 0, 0);
            await AttachStatusAsync(attacker, StatusSet.DEAD, 0, int.MaxValue, 0, 0);
            await AttachStatusAsync(attacker, StatusSet.GHOST, 0, int.MaxValue, 0, 0);

            Character user = attacker as Character;
            int dieType = user?.KoCount * 65541 ?? 1;
            
            await BroadcastRoomMsgAsync(new MsgInteract
            {
                SenderIdentity = attacker?.Identity ?? 0,
                TargetIdentity = Identity,
                PosX = MapX,
                PosY = MapY,
                Action = MsgInteractType.Kill,
                Data = dieType
            }, false);

            m_disappear.Startup(5);
            m_leaveMap.Startup(m_generator.RestSeconds);

            if (m_dbMonster.Action > 0)
            {
                await GameAction.ExecuteActionAsync(m_dbMonster.Action, user, this, null, "");
            }

            if (IsPkKiller() || IsGuard() || IsEvilKiller() || IsDynaNpc() || attacker == null)
                return;

            uint idDropOwner = user?.Identity ?? 0;

            if (user?.Team != null)
            {
                foreach (var member in user.Team.Members)
                {
                    if (member.MapIdentity == user.MapIdentity
                        && member.GetDistance(user) <= Screen.VIEW_SIZE * 2)
                        await member.AddJarKillsAsync(m_dbMonster.StcType);
                }
            }
            else if (user != null)
            {
                await user.AddJarKillsAsync(m_dbMonster.StcType);
            }

            int chanceAdjust = 35;
            if (user != null && BattleSystem.GetNameType(user.Level, Level) == BattleSystem.NAME_GREEN)
                chanceAdjust = 9;

            if (await Kernel.ChanceCalcAsync(chanceAdjust))
            {
                int moneyMin = (int)(m_dbMonster.DropMoney * 0.85f);
                int moneyMax = (int)(m_dbMonster.DropMoney * 1.15f);
                uint money = (uint)(moneyMin + await Kernel.NextAsync(moneyMin, moneyMax) + 1);

                int heapNum = 1 + await Kernel.NextAsync(1, 3);
                uint moneyAve = (uint)(money / heapNum);

                for (int i = 0; i < heapNum; i++)
                {
                    uint moneyTmp = (uint)Calculations.MulDiv((int)moneyAve, 90 + await Kernel.NextAsync(3, 21), 100);

                    await DropMoneyAsync(moneyTmp, 0);
                }
            }

            if (await Kernel.ChanceCalcAsync(50, 16500))
            {
                uint cpsBagType = (uint)await Kernel.NextAsync(729910, 729912);
                await DropItemAsync(cpsBagType, null);
                await Log.GmLogAsync("emoney_bag", $"{idDropOwner},{cpsBagType},{attacker?.MapIdentity},{attacker?.MapX},{attacker?.MapY},{MapX},{MapY},{Identity}");
            } 
            else if (await Kernel.ChanceCalcAsync(625, 54_000_000))
            {
                await DropItemAsync(Item.TYPE_DRAGONBALL, user);
                await Kernel.RoleManager.BroadcastMsgAsync(
                    string.Format(Language.StrDragonBallDropped, attacker?.Name ?? Language.StrNone,
                        attacker?.Map.Name ?? Language.StrNone), MsgTalk.TalkChannel.TopLeft);

                if (user != null)
                    await user.AddActivityPointsAsync(1);
            }
            else if (await Kernel.ChanceCalcAsync(50, 42500))
            {
                await DropItemAsync(Item.TYPE_METEOR, user);
            }
            else if (await Kernel.ChanceCalcAsync(100, 45500))
            {
                uint[] normalGem = {700001, 700011, 700021, 700031, 700041, 700051, 700061, 700071, 700101, 700121};
                uint dGem = normalGem[await Kernel.NextAsync(normalGem.Length) % normalGem.Length];
                if (dGem > 0)
                    await DropItemAsync(dGem, user); // normal gems
            }
            else if (m_dbMonster.StcType == 17 && await Kernel.ChanceCalcAsync(2f))
            {
                await DropItemAsync(1080001, user); // emerald
            }
            else if (await Kernel.ChanceCalcAsync(43, 7500000)) // Fire Of Hell
            {
                await DropItemAsync(1060101, user);
            }
            else if (await Kernel.ChanceCalcAsync(43, 3500000)) // Bomb Scroll
            {
                await DropItemAsync(1060100, user);
            }

            int dropNum = 0;
            int rate = await Kernel.NextAsync(0, 10000);
            int chance = BattleSystem.AdjustDrop(700, attacker.Level, Level);
            if (rate < Math.Min(10000, chance))
            {
                dropNum = 1 + await Kernel.NextAsync(4, 7); // drop 6-10 items
            }
            else
            {
                chance += BattleSystem.AdjustDrop(750, attacker.Level, Level);
                if (rate < Math.Min(10000, chance))
                {
                    dropNum = 1 + await Kernel.NextAsync(2, 4); // drop 4-7 items
                }
                else
                {
                    chance += BattleSystem.AdjustDrop(1000, attacker.Level, Level);
                    if (rate < Math.Min(10000, chance))
                    {
                        dropNum = 1 + await Kernel.NextAsync(1, 3); // drop 4-7 items
                    }
                    else
                    {
                        chance += BattleSystem.AdjustDrop(2000, attacker.Level, Level);
                        if (rate < Math.Min(10000, chance))
                        {
                            dropNum = 1; // drop 1 item
                        }
                    }
                }
            }

            for (int i = 0; i < dropNum; i++)
            {
                uint idItemtype = await GetDropItemAsync();

                DbItemtype itemtype = Kernel.ItemManager.GetItemtype(idItemtype);
                if (itemtype == null)
                    continue;

                await DropItemAsync(itemtype, user);
            }
        }

        #endregion

        #region Drop Function

        public async Task DropItemAsync(uint type, Character owner)
        {
            DbItemtype itemType = Kernel.ItemManager.GetItemtype(type);
            if (itemType != null) await DropItemAsync(itemType, owner);
        }

        private async Task DropItemAsync(DbItemtype itemtype, Character owner)
        {
            Point targetPos = new Point(MapX, MapY);
            if (Map.FindDropItemCell(4, ref targetPos))
            {
                MapItem drop = new MapItem((uint)IdentityGenerator.MapItem.GetNextIdentity);
                if (drop.Create(Map, targetPos, itemtype, owner?.Identity ?? 0, 0, 0, 0))
                {
                    await drop.GenerateRandomInfoAsync();
                    await drop.EnterMapAsync();

                    if (drop.Info.Addition > 0 && owner?.Guide != null)
                        await owner.Guide.AwardOpportunityAsync(1);
                }
                else
                {
                    IdentityGenerator.MapItem.ReturnIdentity(drop.Identity);
                }
            }
        }

        public async Task DropMoneyAsync(uint amount, uint idOwner)
        {
            Point targetPos = new Point(MapX, MapY);
            if (Map?.FindDropItemCell(4, ref targetPos) == true)
            {
                MapItem drop = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
                if (drop.CreateMoney(Map, targetPos, amount, idOwner))
                    await drop.EnterMapAsync();
                else
                {
                    IdentityGenerator.MapItem.ReturnIdentity(drop.Identity);
                }
            }
        }

        public async Task<uint> GetDropItemAsync()
        {
            /*
             * 0 = armet
             * 1 = necklace
             * 2 = armor
             * 3 = ring
             * 4 = weapon
             * 5 = shield
             * 6 = shoes
             * 7 = hp
             * 8 = mp
             */
            var drops = Kernel.ItemManager.GetByRange(Level, 15, 15).ToList();
            var possibleDrops = new List<byte>();
            if (m_dbMonster.DropArmet != 99)
                possibleDrops.Add(0);
            if (m_dbMonster.DropNecklace != 99)
                possibleDrops.Add(1);
            if (m_dbMonster.DropArmor != 99)
                possibleDrops.Add(2);
            if (m_dbMonster.DropRing != 99)
                possibleDrops.Add(3);
            if (m_dbMonster.DropWeapon != 99)
                possibleDrops.Add(4);
            if (m_dbMonster.DropShield != 99)
                possibleDrops.Add(5);
            if (m_dbMonster.DropShoes != 99)
                possibleDrops.Add(6);

            if (possibleDrops.Count > 0)
            {
                byte drop = possibleDrops[await Kernel.NextAsync(0, possibleDrops.Count) % possibleDrops.Count];
                bool dropTalisman = await Kernel.ChanceCalcAsync(10);
                switch (drop)
                {
                    case 0:
                        if (dropTalisman)
                            drops.RemoveAll(x => !Item.IsHelmet(x.Type) && !Item.IsTalisman(x.Type));
                        else 
                            drops.RemoveAll(x => !Item.IsHelmet(x.Type));
                        drops.RemoveAll(x => Item.GetItemSubType(x.Type) == 115);
                        break;
                    case 1:
                        if (dropTalisman)
                            drops.RemoveAll(x => !Item.IsNeck(x.Type) && !Item.IsTalisman(x.Type));
                        else
                            drops.RemoveAll(x => !Item.IsNeck(x.Type));
                        break;
                    case 2:
                        if (dropTalisman)
                            drops.RemoveAll(x => !Item.IsArmor(x.Type) && !Item.IsTalisman(x.Type));
                        else
                            drops.RemoveAll(x => !Item.IsArmor(x.Type));
                        drops.RemoveAll(x => Item.GetItemSubType(x.Type) == 137);
                        break;
                    case 3:
                        if (dropTalisman)
                            drops.RemoveAll(x => !Item.IsRing(x.Type) && !Item.IsBangle(x.Type) && !Item.IsTalisman(x.Type));
                        else
                            drops.RemoveAll(x => !Item.IsRing(x.Type) && !Item.IsBangle(x.Type));
                        break;
                    case 4:
                        if (dropTalisman)
                            drops.RemoveAll(x => !Item.IsWeapon(x.Type) && !Item.IsTalisman(x.Type));
                        else
                            drops.RemoveAll(x => !Item.IsWeapon(x.Type));
                        break;
                    case 5:
                        if (dropTalisman)
                            drops.RemoveAll(x => !Item.IsShield(x.Type) && !Item.IsTalisman(x.Type));
                        else
                            drops.RemoveAll(x => !Item.IsShield(x.Type));
                        break;
                    case 6:
                        if (dropTalisman)
                            drops.RemoveAll(x => !Item.IsShoes(x.Type) && !Item.IsTalisman(x.Type));
                        else
                            drops.RemoveAll(x => !Item.IsShoes(x.Type));
                        break;
                    default:
                        drops.Clear();
                        break;
                }
            }
            else
            {
                drops.Clear();
            }

            DbItemtype pot = Kernel.ItemManager.GetItemtype(m_dbMonster.DropHp);
            if (pot != null)
                drops.Add(pot);
            pot = Kernel.ItemManager.GetItemtype(m_dbMonster.DropMp);
            if (pot != null)
                drops.Add(pot);

            if (drops.Count < 1)
                return 0;

            if (await Kernel.ChanceCalcAsync(625, 20000000))
            {
                drops.RemoveAll(x => Item.GetQuality(x.Type) != 9);
            }
            else if (await Kernel.ChanceCalcAsync(625, 4000000))
            {
                drops.RemoveAll(x => Item.GetQuality(x.Type) != 8);
            }
            else if (await Kernel.ChanceCalcAsync(100, 40000))
            {
                drops.RemoveAll(x => Item.GetQuality(x.Type) != 7);
            }
            else if (await Kernel.ChanceCalcAsync(312, 25000))
            {
                drops.RemoveAll(x => Item.GetQuality(x.Type) != 6);
            }
            else if (await Kernel.ChanceCalcAsync(2, 5))
            {
                drops.RemoveAll(x => !Item.IsMedicine(x.Type));
            }
            else
            {
                drops.RemoveAll(x => Item.GetQuality(x.Type) > 5 || Item.GetQuality(x.Type) < 3);
            }

            if (drops.Count < 1)
                return 0;

            return drops[(await Kernel.NextAsync(drops.Count))%drops.Count]?.Type ?? 0;
        }

        #endregion

        #region Checks

        public bool CanLeaveMap()
        {
            return m_leaveMap.IsTimeOut();
        }

        public bool IsLockUser()
        {
            return (AttackUser & ATKUSER_LOCKUSER) != 0;
        }

        public bool IsRighteous()
        {
            return (AttackUser & ATKUSER_RIGHTEOUS) != 0;
        }

        public bool IsGuard()
        {
            return (AttackUser & ATKUSER_GUARD) != 0;
        }

        public bool IsPkKiller()
        {
            return (AttackUser & ATKUSER_PPKER) != 0;
        }

        public bool IsWalkEnable()
        {
            return (AttackUser & ATKUSER_FIXED) == 0;
        }

        public bool IsJumpEnable()
        {
            return (AttackUser & ATKUSER_JUMP) != 0;
        }

        public bool IsFastBack()
        {
            return (AttackUser & ATKUSER_FASTBACK) != 0;
        }

        public bool IsLockOne()
        {
            return (AttackUser & ATKUSER_LOCKONE) != 0;
        }

        public bool IsAddLife()
        {
            return (AttackUser & ATKUSER_ADDLIFE) != 0;
        }

        public bool IsEvilKiller()
        {
            return (AttackUser & ATKUSER_EVIL_KILLER) != 0;
        }

        public bool IsDormancyEnable()
        {
            return (AttackUser & ATKUSER_LOCKUSER) == 0;
        }

        public bool IsEscapeEnable()
        {
            return (AttackUser & ATKUSER_NOESCAPE) == 0;
        }

        public bool IsEquality()
        {
            return (AttackUser & ATKUSER_EQUALITY) != 0;
        }

        #endregion

        #region Map and Movement

        /// <summary>
        ///     The current map identity for the role.
        /// </summary>
        public override uint MapIdentity
        {
            get => m_idMap;
            set => m_idMap = value;
        }

        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public override ushort MapX
        {
            get => m_posX;
            set => m_posX = value;
        }

        /// <summary>
        ///     Current Y position of the user in the map.
        /// </summary>
        public override ushort MapY
        {
            get => m_posY;
            set => m_posY = value;
        }

        public override async Task EnterMapAsync()
        {
            Map = Kernel.MapManager.GetMap(MapIdentity);
            if (Map != null)
                await Map.AddAsync(this);

            await BroadcastRoomMsgAsync(new MsgAction
            {
                Action = MsgAction.ActionType.MapEffect,
                Identity = Identity,
                X = MapX,
                Y = MapY
            }, false);
        }

        public override async Task LeaveMapAsync()
        {
            m_generator.Remove(Identity);
            IdentityGenerator.Monster.ReturnIdentity(Identity);
            if (Map != null)
            {
                await Map.RemoveAsync(Identity);
                Kernel.RoleManager.RemoveRole(Identity);
            }
            Map = null;
        }

        #endregion

        #region AI Movement

        private bool DetectPath(FacingDirection noDir)
        {
            ClearPath();

            Point posTarget = new Point();
            if (m_actTarget != null)
            {
                posTarget.X = m_actTarget.MapX;
                posTarget.Y = m_actTarget.MapY;
            }
            else if (m_moveTarget != null)
            {
                posTarget.X = m_moveTarget.MapX;
                posTarget.Y = m_moveTarget.MapY;
            }
            else
            {
                posTarget = m_generator.GetCenter();
            }

            int oldDist = GetDistance(posTarget.X, posTarget.Y);
            int bestDist = oldDist;
            FacingDirection bestDir = FacingDirection.Invalid;
            FacingDirection firstDir = FacingDirection.Begin;

            for (FacingDirection i = FacingDirection.Begin; i < FacingDirection.Invalid; i++)
            {
                FacingDirection dir = firstDir;
                if (dir != noDir)
                {
                    int x = MapX + GameMap.WalkXCoords[(int)dir];
                    int y = MapY + GameMap.WalkYCoords[(int)dir];
                    if (Map.IsMoveEnable(x, y) && !Map.IsSuperPosition(x, y))
                    {
                        int dist = GetDistance(x, y);
                        if (bestDist - dist * (m_bAheadPath ? 1 : -1) > 0)
                        {
                            bestDist = dist;
                            bestDir = dir;
                        }
                    }
                }
            }

            if (bestDir != FacingDirection.Invalid)
            {
                m_nextDir = bestDir;
                return true;
            }

            return true;
        }

        private bool FindPath(int x, int y)
        {
            if (x == MapX && y == MapY)
                return false;

            FacingDirection dir = (FacingDirection) ScreenCalculations.GetDirection(MapX, MapY, x, y);
            for (int i = 0; i < 8; i++)
            {
                dir = (FacingDirection) (((int) dir + i) % 8);
                if (TestPath(dir))
                {
                    m_nextDir = dir;
                    return true;
                }
            }

            return m_nextDir != FacingDirection.Invalid;
        }

        private bool FindPath(int scapeSteps = 0)
        {
            if (m_moveTarget == null)
                return false;

            m_bAheadPath = scapeSteps == 0;
            ClearPath();

            Role role = Map.QueryAroundRole(this, m_moveTarget.Identity);
            if (role == null || !role.IsAlive || GetDistance(role) > ViewRange)
            {
                m_moveTarget = null;
                m_actTarget = null;
                return false;
            }

            if (!FindPath(role.MapX, role.MapY))
            {
                m_moveTarget = null;
                m_actTarget = null;
                return false;
            }

            if (m_nextDir != FacingDirection.Invalid)
            {
                if (!Map.IsMoveEnable(MapX, MapY, m_nextDir))
                {
                    DetectPath(m_nextDir);
                    return m_nextDir != FacingDirection.Invalid;
                }
            }

            return m_nextDir != FacingDirection.Invalid;
        }

        private bool TestPath(FacingDirection dir)
        {
            if (dir == FacingDirection.Invalid)
                return false;

            int x = MapX + GameMap.WalkXCoords[(int) dir];
            int y = MapY + GameMap.WalkYCoords[(int) dir];

            if (Map.IsMoveEnable(x, y))
            {
                m_nextDir = dir;
                return true;
            }

            return false;
        }

        private async Task<bool> JumpBlockAsync(int x, int y, FacingDirection dir)
        {
            int steps = GetDistance(x, y);
            
            if (steps <= 0)
                return false;

            for (int i = 0; i < steps; i++)
            {
                Point pos = new Point(MapX + (x - MapX) * i / steps, MapY + (y - MapY) * i / steps);
                if (Map.IsStandEnable(pos.X, pos.Y))
                {
                    QueueAction(async () => await JumpPosAsync(pos.X, pos.Y, true));
                    return true;
                }
            }

            if (Map.IsStandEnable(x, y))
            {
                QueueAction(async () => await JumpPosAsync(x, y, true));
                return true;
            }

            return false;
        }

        private async Task<bool> FarJumpAsync(int x, int y, FacingDirection dir)
        {
            int steps = GetDistance(x, y);

            if (steps <= 0)
                return false;

            if (Map.IsStandEnable(x, y))
            {
                QueueAction(async () => await JumpPosAsync(x, y, true));
                return true;
            }

            for (int i = 0; i < steps; i++)
            {
                Point pos = new Point(MapX + (x - MapX) * i / steps, MapY + (y - MapY) * i / steps);
                if (Map.IsStandEnable(pos.X, pos.Y))
                {
                    QueueAction(async () => await JumpPosAsync(pos.X, pos.Y, true));
                    return true;
                }
            }
            return false;
        }

        public void ClearPath()
        {
            m_nextDir = FacingDirection.Invalid;
        }

        private async Task<bool> PathMoveAsync(RoleMoveMode mode)
        {
            if (mode == RoleMoveMode.Walk)
            {
                if (!m_tMoveMs.ToNextTime(m_dbMonster.MoveSpeed))
                    return true;
            }
            else
            {
                if (!m_tMoveMs.ToNextTime((int) m_dbMonster.RunSpeed))
                    return true;
            }

            int newX = MapX + GameMap.WalkXCoords[(int) m_nextDir];
            int newY = MapY + GameMap.WalkYCoords[(int) m_nextDir];

            if (!Map.IsSuperPosition(newX, newY))
            {
                if (TestPath(m_nextDir))
                {
                    QueueAction(async () => await MoveTowardAsync((int)m_nextDir, (int)mode, true));
                    return true;
                }
            }

            if (DetectPath(m_nextDir) && !Map.IsSuperPosition(newX, newY))
            {
                QueueAction(async () => await MoveTowardAsync((int)m_nextDir, (int)mode, true));
                return true;
            }

            if (IsJumpEnable())
            {
                var pos = m_generator.GetCenter();
                QueueAction(async () => await JumpBlockAsync(pos.X, pos.Y, Direction));
                return true;
            }

            return false;
        }

        #endregion

        #region AI

        private int m_playersOnSight;

        public int PlayerOnSight
        {
            get => m_playersOnSight;
            set => Interlocked.Exchange(ref m_playersOnSight, value);
        }

        public bool IsActive => PlayerOnSight > 0 || !StatusSet.Status.IsEmpty || IsGuard();

        public bool IsCloseAttack()
        {
            return !IsBowman;
        }

        public bool IsMoveEnable()
        {
            return IsWalkEnable() || IsJumpEnable();
        }

        public override bool IsEvil() { return (AttackUser & (4 | 8192)) == 0; }

        public bool CheckTarget()
        {
            if (m_actTarget == null || !m_actTarget.IsAlive)
            {
                return false;
            }

            if (m_actTarget.IsWing && !IsWing && !IsCloseAttack())
                return false;

            int nDistance = ViewRange;
            int nAtkDistance = Calculations.CutOverflow(nDistance, GetAttackRange(m_actTarget.SizeAddition));
            int nDist = GetDistance(m_actTarget.MapX, m_actTarget.MapY);

            if (!(nDist <= nDistance) || (nDist <= nAtkDistance && GetAttackRange(m_actTarget.SizeAddition) > 1))
            {
                m_actTarget = null;
                return false;
            }

            return true;
        }

        private async Task ChangeModeAsync(AiStage mode)
        {
            switch (mode)
            {
                case AiStage.Dormancy:
                    //Life = MaxLife;
                    QueueAction(async () => await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife));
                    break;
                case AiStage.Attack:
                    await DoAttackAsync();
                    break;
            }

            if (mode != AiStage.Move)
            {
                ClearPath();
            }

            m_stage = mode;
        }

        public async Task<bool> FindNewTargetAsync()
        {
            // lock target
            if (IsLockUser() || IsLockOne())
            {
                if (CheckTarget())
                {
                    if (IsLockOne())
                        return true;
                }
                else
                {
                    if (IsLockUser())
                        return false;
                }
            }

            uint idOldTarget = m_actTarget?.Identity ?? 0;
            int distance = ViewRange;
            var roles = Map.Query9BlocksByPos(MapX, MapY);
            foreach (var role in roles)
            {
                if (role.Identity == Identity)
                    continue;

                if (!role.IsAlive)
                    continue;

                if (role.QueryStatus(StatusSet.INVISIBLE) != null)
                    continue;

                if (role is Character targetUser)
                {
                    bool pkKill = IsPkKiller() && targetUser.IsPker();
                    bool evilKill = IsEvilKiller() && !targetUser.IsVirtuous();
                    if ((IsGuard() && targetUser.IsCrime())
                        || (pkKill)
                        || (evilKill)
                        || (IsEvil() && !(IsPkKiller() || IsEvilKiller())))
                    {
                        if (targetUser.IsWing && !IsWing && !IsBowman && m_monsterMagics.Count == 0)
                            continue;

                        if (!targetUser.IsAttackable(this))
                            continue;

                        int nDist = GetDistance(targetUser.MapX, targetUser.MapY);
                        if (nDist <= distance)
                        {
                            distance = nDist;
                            m_moveTarget = m_actTarget = targetUser;

                            if (pkKill || evilKill)
                                break;
                        }
                    }
                }
                else if (role is Monster monster)
                {
                    if ((IsEvil() && monster.IsRighteous()
                                         || IsRighteous() && monster.IsEvil()))
                    {
                        if (monster.IsWing && !IsWing) continue;

                        int nDist = GetDistance(monster.MapX, monster.MapY);
                        if (nDist < distance)
                        {
                            distance = nDist;
                            m_moveTarget = m_actTarget = monster;
                        }
                    }
                }
            }

            if (m_actTarget != null)
            {
                if (m_actTarget is Character targetUser && targetUser.Identity != idOldTarget)
                {
                    if (IsGuard() && targetUser.IsCrime())
                    {
                        await  targetUser.BroadcastRoomMsgAsync(
                            new MsgTalk(Identity, MsgTalk.TalkChannel.Talk, Color.White, targetUser.Name, Name,
                                Language.StrGuardYouPay), true);
                    }
                    else if (IsPkKiller() && targetUser.IsPker() && m_stage == AiStage.Idle)
                    {
                        await targetUser.BroadcastRoomMsgAsync(
                            new MsgTalk(Identity, MsgTalk.TalkChannel.Talk, Color.White, targetUser.Name, Name,
                                Language.StrGuardYouPay), true);
                    }
                }

                FindPath();
                return m_actTarget != null;
            }
            return true;
        }

        private async Task EscapeAsync()
        {
            if (!IsEscapeEnable())
            {
                await ChangeModeAsync(AiStage.Idle);
                return;
            }

            if ((IsGuard() || IsPkKiller()) && m_actTarget != null)
            {
                QueueAction(async () => await JumpPosAsync(m_actTarget.MapX, m_actTarget.MapY, true));
                await ChangeModeAsync(AiStage.Move);
                return;
            }

            if (m_nextDir == FacingDirection.Invalid)
                FindPath(ViewRange * 2);

            if (m_actTarget == null)
            {
                await ChangeModeAsync(AiStage.Idle);
                return;
            }

            if (m_actTarget != null && m_nextDir == FacingDirection.Invalid)
            {
                await ChangeModeAsync(AiStage.Move);
                return;
            }

            await PathMoveAsync(RoleMoveMode.Run);
        }

        private async Task AttackAsync()
        {
            if (m_actTarget != null)
            {
                if (m_actTarget.IsAlive
                    && (await CheckMagicAttackAsync() || GetDistance(m_actTarget) <= GetAttackRange(m_actTarget.SizeAddition)))
                {
                    if (m_tAction.ToNextTime())
                    {
                        await ChangeModeAsync(AiStage.Move);
                        return;
                    }

                    return;
                }
            }

            await ChangeModeAsync(AiStage.Idle);
        }

        private async Task FowardAsync()
        {
            if (m_actTarget != null)
            {
                if (m_actTarget.IsAlive && (await CheckMagicAttackAsync() || GetDistance(m_actTarget) <= GetAttackRange(m_actTarget.SizeAddition)))
                {
                    if (!IsGuard() && !IsMoveEnable() && !m_bAheadPath && m_nextDir != FacingDirection.Invalid)
                        if (await PathMoveAsync(RoleMoveMode.Run))
                            return;

                    await ChangeModeAsync(AiStage.Attack);
                    return;
                }
            }

            // process forward
            if ((IsGuard() || IsPkKiller() || IsFastBack()) && m_generator.IsTooFar(MapX, MapY, 48))
            {
                Point pos = m_generator.GetCenter();

                m_actTarget = null;
                m_moveTarget = null;

                await FarJumpAsync(pos.X, pos.Y, Direction);
                ClearPath();
                await ChangeModeAsync(AiStage.Idle);
                return;
            }

            if ((IsGuard() || IsPkKiller() || IsEvilKiller()) && m_actTarget != null && GetDistance(m_actTarget.MapX, m_actTarget.MapY) >= GetAttackRange(m_actTarget.SizeAddition))
            {
                await JumpPosAsync(m_actTarget.MapX, m_actTarget.MapY, true);
                return;
            }

            if (m_nextDir == FacingDirection.Invalid)
            {
                if (await FindNewTargetAsync())
                {
                    if (m_nextDir == FacingDirection.Invalid)
                    {
                        if (IsJumpEnable())
                        {
                            await JumpBlockAsync(m_actTarget.MapX, m_actTarget.MapY, Direction);
                            return;
                        }

                        await ChangeModeAsync(AiStage.Idle);
                        return;
                    }

                    return;
                }

                await ChangeModeAsync(AiStage.Idle);
                return;
            }

            if (m_actTarget != null)
            {
                if (GetDistance(m_actTarget) <= ViewRange)
                {
                    FindPath();
                    m_tAttackMs.Update();
                    await PathMoveAsync(RoleMoveMode.Run);
                }
                else
                {
                    m_actTarget = null;
                }
            }
            else
            {
                await ChangeModeAsync(AiStage.Idle);
            }
        }

        private async Task IdleAsync()
        {
            m_attackMe = null;
            m_atkFirst = false;

            if (await FindNewTargetAsync())
            {
                if (m_actTarget == null)
                    return;

                int distance = GetDistance(m_actTarget);
                if (distance <= GetAttackRange(m_actTarget.SizeAddition))
                {
                    await ChangeModeAsync(AiStage.Attack);
                    return;
                }

                if (IsMoveEnable())
                {
                    if (m_nextDir == FacingDirection.Invalid)
                    {
                        if (!IsEscapeEnable() || await Kernel.ChanceCalcAsync(80))
                            return;

                        await ChangeModeAsync(AiStage.Escape);
                        return;
                    }

                    await ChangeModeAsync(AiStage.Move);
                    return;
                }
            }

            if (IsGuard() || IsPkKiller() || IsEvilKiller())
            {
                if (m_nextDir == FacingDirection.Invalid && m_generator.GetWidth() > 1 && m_generator.GetHeight() > 1)
                {
                    await PathMoveAsync(RoleMoveMode.Walk);
                    return;
                }

                if (m_tAction.ToNextTime())
                    return;
            }

            if (!IsMoveEnable())
                return;

            if (m_generator.IsInRegion(MapX, MapY))
            {
                if (IsGuard() || IsPkKiller() || IsEvilKiller())
                {
                    if (m_generator.GetWidth() > 1 || m_generator.GetHeight() > 1)
                    {
                        int x = MapX + await Kernel.NextAsync(m_generator.GetWidth());
                        int y = MapY + await Kernel.NextAsync(m_generator.GetHeight());

                        if (FindPath(x, y))
                            await PathMoveAsync(RoleMoveMode.Walk);
                    }
                }
                else
                {
                    FacingDirection dir = (FacingDirection) (await Kernel.NextAsync(int.MaxValue)%8);
                    if (TestPath(dir))
                        await PathMoveAsync(RoleMoveMode.Walk);
                }
            }
            else
            {
                if (IsGuard() || IsPkKiller() || IsEvilKiller() || IsFastBack())
                {
                    if (m_generator.IsInRegion(MapX, MapY))
                    {
                        Point pos = m_generator.GetCenter();
                        if (FindPath(pos.X, pos.Y))
                        {
                            await PathMoveAsync(RoleMoveMode.Walk);
                        }
                        else
                        {
                            await JumpBlockAsync(pos.X, pos.Y, Direction);
                        }
                    }
                }
            }
        }

        #endregion

        #region Revive Monster

        public async Task OnReviveAsync()
        {
            await Task.Delay(m_generator.RestSeconds * 1000);

            if (Map == null)
            {
                await LeaveMapAsync();
                return;
            }



        }

        #endregion

        #region On Timer

        public override async Task OnTimerAsync()
        {
            if (!IsAlive
                && m_disappear.IsActive()
                && m_disappear.IsTimeOut())
            {
                QueueAction(async () => await AttachStatusAsync(this, StatusSet.INVISIBLE, 0, int.MaxValue, 0, 0));
            }

            if (m_leaveMap.IsActive())
            {
                if (CanLeaveMap())
                    QueueAction(LeaveMapAsync);
                return;
            }

            if (m_tStatusCheck.ToNextTime())
            {
                foreach (var stts in StatusSet.Status.Values)
                {
                    QueueAction(async () =>
                    {
                        await stts.OnTimerAsync();

                        if (!stts.IsValid && stts.Identity != StatusSet.GHOST && stts.Identity != StatusSet.DEAD)
                        {
                            await StatusSet.DelObjAsync(stts.Identity);
                        }
                    });
                }
            }

            switch (m_stage)
            {
                case AiStage.Escape:
                    await EscapeAsync();
                    break;
                case AiStage.Attack:
                    await AttackAsync();
                    break;
                case AiStage.Move:
                    await FowardAsync();
                    break;
                case AiStage.Idle:
                    await IdleAsync();
                    break;
            }
        }

        #endregion

        #region Socket

        public override async Task BroadcastRoomMsgAsync(IPacket msg, bool self)
        {
            if (Map != null)
                await Map.BroadcastRoomMsgAsync(MapX, MapY, msg);
        }

        public override async Task SendSpawnToAsync(Character player)
        {
            if (IsAlive)
                await player.SendAsync(new MsgPlayer(this));
        }

        #endregion

        public const int ATKUSER_LEAVEONLY = 0, // Ö»»áÌÓÅÜ
            ATKUSER_PASSIVE = 0x01, // ±»¶¯¹¥»÷
            ATKUSER_ACTIVE = 0x02, // Ö÷¶¯¹¥»÷
            ATKUSER_RIGHTEOUS = 0x04, // ÕýÒåµÄ(ÎÀ±ø»òÍæ¼ÒÕÙ»½ºÍ¿ØÖÆµÄ¹ÖÎï)
            ATKUSER_GUARD = 0x08, // ÎÀ±ø(ÎÞÊÂ»ØÔ­Î»ÖÃ)
            ATKUSER_PPKER = 0x10, // ×·É±ºÚÃû 
            ATKUSER_JUMP = 0x20, // »áÌø
            ATKUSER_FIXED = 0x40, // ²»»á¶¯µÄ
            ATKUSER_FASTBACK = 0x0080, // ËÙ¹é
            ATKUSER_LOCKUSER = 0x0100, // Ëø¶¨¹¥»÷Ö¸¶¨Íæ¼Ò£¬Íæ¼ÒÀë¿ª×Ô¶¯ÏûÊ§ 
            ATKUSER_LOCKONE = 0x0200, // Ëø¶¨¹¥»÷Ê×ÏÈ¹¥»÷×Ô¼ºµÄÍæ¼Ò
            ATKUSER_ADDLIFE = 0x0400, // ×Ô¶¯¼ÓÑª
            ATKUSER_EVIL_KILLER = 0x0800, // °×ÃûÉ±ÊÖ
            ATKUSER_WING = 0x1000, // ·ÉÐÐ×´Ì¬
            ATKUSER_NEUTRAL = 0x2000, // ÖÐÁ¢
            ATKUSER_ROAR = 0x4000, // ³öÉúÊ±È«µØÍ¼Å­ºð
            ATKUSER_NOESCAPE = 0x8000, // ²»»áÌÓÅÜ
            ATKUSER_EQUALITY = 0x10000; // ²»ÃêÊÓ

        enum AiStage
        {
            /// <summary>
            /// Monster is doing absolutely nothing.
            /// </summary>
            Idle,
            /// <summary>
            /// Monster wont do nothing, just heal. Activated if on active block but haven't triggered any other action.
            /// </summary>
            Dormancy,
            /// <summary>
            /// Monster will do some random move?
            /// </summary>
            Move,
            /// <summary>
            /// Monster is ready for attack.
            /// </summary>
            Attack,
            /// <summary>
            /// When monster is low life and want to run from the attacker.
            /// </summary>
            Escape
        }
    }
}