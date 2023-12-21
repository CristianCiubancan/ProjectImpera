// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Character.cs
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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.Mathematics;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Internal;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Events;
using Comet.Game.States.Families;
using Comet.Game.States.Guide;
using Comet.Game.States.Items;
using Comet.Game.States.Magics;
using Comet.Game.States.NPCs;
using Comet.Game.States.Relationship;
using Comet.Game.States.Syndicates;
using Comet.Game.World;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Network.Packets.Internal;
using Comet.Shared;

#endregion

namespace Comet.Game.States
{
    /// <summary>
    ///     Character class defines a database record for a player's character. This allows
    ///     for easy saving of character information, as well as means for wrapping character
    ///     data for spawn packet maintenance, interface update pushes, etc.
    /// </summary>
    public class Character : Role
    {
        private Client m_socket;
        private readonly DbCharacter m_dbObject;

        private TimeOut m_flowerRankRefresh = new TimeOut(10);
        private TimeOutMS m_energyTm = new TimeOutMS(ADD_ENERGY_STAND_MS);
        private TimeOut m_autoHeal = new TimeOut(AUTOHEALLIFE_TIME);
        private TimeOut m_pkDecrease = new TimeOut(PK_DEC_TIME);
        private TimeOut m_xpPoints = new TimeOut(3);
        private TimeOut m_ghost = new TimeOut(3);
        private TimeOut m_transformation = new TimeOut();
        private TimeOut m_tRevive = new TimeOut();
        private TimeOut m_respawn = new TimeOut();
        private TimeOut m_mine = new TimeOut(2);
        private TimeOut m_teamLeaderPos = new TimeOut(3);
        private TimeOut m_heavenBlessing = new TimeOut(60);
        private TimeOut m_luckyAbsorbStart = new TimeOut(2);
        private TimeOut m_luckyStep = new TimeOut(1);
        private TimeOut m_tWorldChat = new TimeOut();
        private TimeOutMS m_tVigor = new TimeOutMS(1500);
        private TimeOut m_activityPointsAdd = new TimeOut(120);

        private ConcurrentDictionary<RequestType, uint> m_dicRequests = new ConcurrentDictionary<RequestType, uint>();

        private int m_blessPoints = 0;
        private uint m_idLuckyTarget = 0;
        private int m_luckyTimeCount = 0;

        private int m_KillsToCaptcha = 0;

        /// <summary>
        ///     Instantiates a new instance of <see cref="Character" /> using a database fetched
        ///     <see cref="DbCharacter" />. Copies attributes over to the base class of this
        ///     class, which will then be used to save the character from the game world.
        /// </summary>
        /// <param name="character">Database character information</param>
        /// <param name="socket"></param>
        public Character(DbCharacter character, Client socket)
        {
            /*
             * Removed the base class because we'll be inheriting role stuff.
             */
            m_dbObject = character;

            if (socket == null)
                return; // ?

            m_socket = socket;
            
            m_mesh = m_dbObject.Mesh;

            m_posX = character.X;
            m_posY = character.Y;
            m_idMap = character.MapID;

            Screen = new Screen(this);
            WeaponSkill = new WeaponSkill(this);
            UserPackage = new UserPackage(this);
            Statistic = new UserStatistic(this);
            TaskDetail = new TaskDetail(this);

            if (m_dbObject.LuckyTime != null)
                m_luckyTimeCount = (int) Math.Max(0, (m_dbObject.LuckyTime.Value - DateTime.Now).TotalSeconds);

            m_energyTm.Update();
            m_autoHeal.Update();
            m_pkDecrease.Update();
            m_xpPoints.Update();
            m_ghost.Update();
        }

        public Client Client => m_socket;

        public MessageBox MessageBox = null;

        public ConnectionStage Connection { get; set; } = ConnectionStage.Connected;

        #region Identity

        public override uint Identity
        {
            get => m_dbObject.Identity;
            protected set
            {
                // cannot change the identity
            }
        }

        public override string Name
        {
            get => m_dbObject.Name;
            set => m_dbObject.Name = value;
        }

        public string MateName { get; set; }

        public uint MateIdentity
        {
            get => m_dbObject.Mate;
            set => m_dbObject.Mate = value;
        }

        public TimeSpan OnlineTime =>
            TimeSpan.Zero
                .Add(new TimeSpan(0, 0, 0, m_dbObject.OnlineSeconds))
                .Add(new TimeSpan(0, 0, 0, (int) (DateTime.Now - m_dbObject.LoginTime).TotalSeconds));

        public TimeSpan SessionOnlineTime => TimeSpan.Zero
            .Add(new TimeSpan(0, 0, 0, (int)(DateTime.Now - m_dbObject.LoginTime).TotalSeconds));

        #endregion

        #region Appearence

        private uint m_mesh = 0;
        private ushort m_transformMesh = 0;

        public int Gender => Body == BodyType.AgileMale || Body == BodyType.MuscularMale ? 1 : 2;

        public ushort TransformationMesh
        {
            get => m_transformMesh;
            set
            {
                m_transformMesh = value;
                Mesh = (uint)((uint)value * 10000000 + Avatar * 10000 + (uint) Body);
            }
        }

        public override uint Mesh
        {
            get => m_mesh;
            set
            {
                m_mesh = value;
                m_dbObject.Mesh = value % 10000000;
            }
        }

        public BodyType Body
        {
            get => (BodyType)(Mesh % 10000);
            set => Mesh = ((uint) value + (Avatar * 10000u));
        }

        public ushort Avatar
        {
            get => (ushort)(Mesh % 1000000 / 10000);
            set => Mesh = (uint)(value * 10000 + (int)Body);
        }

        public ushort Hairstyle
        {
            get => m_dbObject.Hairstyle;
            set => m_dbObject.Hairstyle = value;
        }

        #endregion

        #region Transformation

        public Transformation Transformation { get; protected set; }

        public async Task<bool> TransformAsync(uint dwLook, int nKeepSecs, bool bSynchro)
        {
            bool bBack = false;

            if (Transformation != null)
            {
                await ClearTransformationAsync();
                bBack = true;
            }

            DbMonstertype pType = Kernel.RoleManager.GetMonstertype(dwLook);
            if (pType == null)
            {
                return false;
            }

            Transformation pTransform = new Transformation(this);
            if (pTransform.Create(pType))
            {
                Transformation = pTransform;
                TransformationMesh = (ushort)pTransform.Lookface;
                await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);
                Life = MaxLife;
                m_transformation = new TimeOut(nKeepSecs);
                m_transformation.Startup(nKeepSecs);
                if (bSynchro)
                    await SynchroTransformAsync();
            }
            else
            {
                pTransform = null;
            }

            if (bBack)
                await SynchroTransformAsync();

            return false;
        }

        public async Task ClearTransformationAsync()
        {
            TransformationMesh = 0;
            Transformation = null;
            m_transformation.Clear();
            
            await SynchroTransformAsync();
            await MagicData.AbortMagicAsync(true);
            BattleSystem.ResetBattle();
        }

        public async Task<bool> SynchroTransformAsync()
        {
            MsgUserAttrib msg = new MsgUserAttrib(Identity, ClientUpdateType.Mesh, Mesh);
            if (TransformationMesh != 98 && TransformationMesh != 99)
            {
                Life = MaxLife;
                msg.Append(ClientUpdateType.MaxHitpoints, MaxLife);
                msg.Append(ClientUpdateType.Hitpoints, Life);
            }
            await BroadcastRoomMsgAsync(msg, true);
            return true;
        }

        public async Task SetGhostAsync()
        {
            if (IsAlive) return;

            ushort trans = 98;
            if (Gender == 2)
                trans = 99;
            TransformationMesh = trans;
            await SynchroTransformAsync();
        }

        #endregion

        #region Profession

        public byte ProfessionSort => (byte)(Profession / 10);

        public byte ProfessionLevel => (byte)(Profession % 10);

        public byte Profession
        {
            get => m_dbObject?.Profession ?? 0;
            set => m_dbObject.Profession = value;
        }

        public byte PreviousProfession
        {
            get => m_dbObject?.PreviousProfession ?? 0;
            set => m_dbObject.PreviousProfession = value;
        }

        public byte FirstProfession
        {
            get => m_dbObject?.FirstProfession ?? 0;
            set => m_dbObject.FirstProfession = value;
        }

        #endregion

        #region Attribute Points

        public ushort Strength
        {
            get => m_dbObject?.Strength ?? 0;
            set => m_dbObject.Strength = value;
        }

        public ushort Agility
        {
            get => m_dbObject?.Agility ?? 0;
            set => m_dbObject.Agility = value;
        }

        public ushort Vitality
        {
            get => m_dbObject?.Vitality ?? 0;
            set => m_dbObject.Vitality = value;
        }

        public ushort Spirit
        {
            get => m_dbObject?.Spirit ?? 0;
            set => m_dbObject.Spirit = value;
        }

        public ushort AttributePoints
        {
            get => m_dbObject?.AttributePoints ?? 0;
            set => m_dbObject.AttributePoints = value;
        }

        #endregion

        #region Life and Mana

        public override uint Life
        {
            get => m_dbObject.HealthPoints;
            set => m_dbObject.HealthPoints = (ushort)Math.Min(MaxLife, value);
        }

        public override uint MaxLife
        {
            get
            {
                if (Transformation != null)
                    return (uint) Transformation.MaxLife;

                uint result = (uint)(Vitality * 24);
                switch (Profession)
                {
                    case 11:
                        result = (uint)(result * 1.05d);
                        break;
                    case 12:
                        result = (uint)(result * 1.08d);
                        break;
                    case 13:
                        result = (uint)(result * 1.10d);
                        break;
                    case 14:
                        result = (uint)(result * 1.12d);
                        break;
                    case 15:
                        result = (uint)(result * 1.15d);
                        break;
                }

                result += (uint)((Strength + Agility + Spirit) * 3);

                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                    pos <= Item.ItemPosition.EquipmentEnd;
                    pos++)
                {
                    result += (uint)(UserPackage[pos]?.Life ?? 0);
                }

                return result;
            }
        }

        public override uint Mana
        {
            get => m_dbObject.ManaPoints;
            set => 
                m_dbObject.ManaPoints = (ushort)Math.Min(MaxMana, value);
        }

        public override uint MaxMana
        {
            get
            {
                uint result = (uint)(Spirit * 5);
                switch (Profession)
                {
                    case 132:
                    case 142:
                        result *= 3;
                        break;
                    case 133:
                    case 143:
                        result *= 4;
                        break;
                    case 134:
                    case 144:
                        result *= 5;
                        break;
                    case 135:
                    case 145:
                        result *= 6;
                        break;
                }

                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                    pos <= Item.ItemPosition.EquipmentEnd;
                    pos++)
                {
                    result += (uint)(UserPackage[pos]?.Mana ?? 0);
                }

                return result;
            }
        }

        #endregion

        #region Level and Experience

        public bool AutoAllot
        {
            get => m_dbObject.AutoAllot != 0;
            set => m_dbObject.AutoAllot = (byte) (value ? 1 : 0);
        }

        public override byte Level
        {
            get => m_dbObject?.Level ?? 0;
            set => m_dbObject.Level = Math.Min(MAX_UPLEV, Math.Max((byte)1, value));
        }

        public ulong Experience
        {
            get => m_dbObject?.Experience ?? 0;
            set
            {
                if (Level >= MAX_UPLEV)
                    return;

                m_dbObject.Experience = value;
            }
        }

        public byte Metempsychosis
        {
            get => m_dbObject?.Rebirths ?? 0;
            set => m_dbObject.Rebirths = value;
        }

        public bool IsNewbie()
        {
            return Level < 70;
        }

        public async Task<bool> AwardLevelAsync(ushort amount)
        {
            if (Level >= MAX_UPLEV)
                return false;

            if (Level + amount <= 0)
                return false;

            int addLev = amount;
            if (addLev + Level > MAX_UPLEV)
                addLev = MAX_UPLEV - Level;

            if (addLev <= 0)
                return false;

            await AddAttributesAsync(ClientUpdateType.Atributes, (ushort) (addLev * 3));
            await AddAttributesAsync(ClientUpdateType.Level, addLev);
            await BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = Identity,
                Action = MsgAction.ActionType.CharacterLevelUp,
                ArgumentX = MapX,
                ArgumentY = MapY
            }, true);

            await UplevelEventAsync();
            return true;
        }

        public async Task AwardBattleExpAsync(long nExp, bool bGemEffect)
        {
            if (nExp == 0 || QueryStatus(StatusSet.CURSED) != null)
                return;

            if (Level >= MAX_UPLEV)
                return;

            if (nExp < 0)
            {
                await AddAttributesAsync(ClientUpdateType.Experience, nExp);
                return;
            }

            const int BATTLE_EXP_TAX = 5;

            if (Level >= 120)
                nExp /= 2;

            if (Metempsychosis >= 2)
                nExp /= 3;

            if (Level < 130)
                nExp *= BATTLE_EXP_TAX;

            double multiplier = 1;
            if (HasMultipleExp)
                multiplier += ExperienceMultiplier - 1;

            if (!IsNewbie() && ProfessionSort == 13 && ProfessionLevel >= 3)
                multiplier += 1;

            DbLevelExperience levExp = Kernel.RoleManager.GetLevelExperience(Level);
            if (IsBlessed)
            {
                if (levExp != null)
                    OnlineTrainingExp += (uint) (levExp.UpLevTime * (nExp / (float) levExp.Exp)* 0.2);
            }

            if (Guide != null && levExp != null)
                await Guide.AwardTutorExperienceAsync((uint)(levExp.MentorUpLevTime * ((float)nExp / levExp.Exp)));
            
            if (bGemEffect)
                multiplier += (1 + (RainbowGemBonus / 100d));

            if (IsLucky && await Kernel.ChanceCalcAsync(10, 10000))
            {
                await SendEffectAsync("LuckyGuy", true);
                nExp *= 5;
                await SendAsync(Language.StrLuckyGuyQuintuple);
            }

            multiplier += 1 + BattlePower / 100d;

            nExp = (long)(nExp * Math.Max(1, multiplier));
            
            await AwardExperienceAsync(nExp);
        }

        public long AdjustExperience(Role pTarget, long nRawExp, bool bNewbieBonusMsg)
        {
            if (pTarget == null) return 0;
            long nExp = nRawExp;
            nExp = BattleSystem.AdjustExp(nExp, Level, pTarget.Level);
            return nExp;
        }

        public async Task<bool> AwardExperienceAsync(long amount, bool noContribute = false)
        {
            if (Level > Kernel.RoleManager.GetLevelLimit())
                return true;
            
            amount += (long)Experience;
            bool leveled = false;
            uint pointAmount = 0;
            byte newLevel = Level;
            ushort virtue = 0;
            long usedExp = amount;

            double mentorUpLevTime = 0;
            while (newLevel < MAX_UPLEV && amount >= (long)Kernel.RoleManager.GetLevelExperience(newLevel).Exp)
            {
                DbLevelExperience dbExp = Kernel.RoleManager.GetLevelExperience(newLevel);
                amount -= (long) dbExp.Exp;
                leveled = true;
                newLevel++;

                if (newLevel <= 70)
                {
                    virtue += (ushort)dbExp.UpLevTime;
                }

                if (!AutoAllot || Level > 120)
                {
                    pointAmount += 3;
                    continue;
                }

                mentorUpLevTime += dbExp.MentorUpLevTime;//(leveXp.MentorUpLevTime * ((float)usedExp / leveXp.Exp));

                if (newLevel < Kernel.RoleManager.GetLevelLimit()) continue;
                amount = 0;
                break;
            }

            uint metLev = 0;
            var leveXp = Kernel.RoleManager.GetLevelExperience(newLevel);
            if (leveXp != null)
            {
                float fExp = amount / (float)leveXp.Exp;
                metLev = (uint)(newLevel * 10000 + fExp * 1000);

                mentorUpLevTime += (leveXp.MentorUpLevTime * ((float)amount / leveXp.Exp));
            }

            byte checkLevel = 130; //(byte)(m_dbObject.Reincarnation > 0 ? 110 : 130);
            if (newLevel >= checkLevel && Metempsychosis > 0 && m_dbObject.MeteLevel > metLev)
            {
                byte extra = 0;
                if (newLevel >= checkLevel && m_dbObject.MeteLevel / 10000 > newLevel)
                {
                    var mete = m_dbObject.MeteLevel / 10000;
                    extra += (byte)(mete - newLevel);
                    pointAmount += (uint)(extra * 3);
                    leveled = true;
                    amount = 0;
                }

                newLevel += extra;

                if (newLevel >= Kernel.RoleManager.GetLevelLimit())
                {
                    newLevel = (byte)Kernel.RoleManager.GetLevelLimit();
                    amount = 0;
                }
                else if (m_dbObject.MeteLevel >= newLevel * 10000)
                {
                    amount = (long)(Kernel.RoleManager.GetLevelExperience(newLevel).Exp * ((m_dbObject.MeteLevel % 10000) / 1000d));
                }
            }

            if (leveled)
            {
                byte job;
                if (Profession > 100)
                    job = 10;
                else
                    job = (byte)((Profession - Profession % 10) / 10);

                var allot = Kernel.RoleManager.GetPointAllot(job, newLevel);
                Level = newLevel;
                if (AutoAllot && allot != null)
                {
                    await SetAttributesAsync(ClientUpdateType.Strength, allot.Strength);
                    await SetAttributesAsync(ClientUpdateType.Agility, allot.Agility);
                    await SetAttributesAsync(ClientUpdateType.Vitality, allot.Vitality);
                    await SetAttributesAsync(ClientUpdateType.Spirit, allot.Spirit);
                }
                else if (pointAmount > 0)
                    await AddAttributesAsync(ClientUpdateType.Atributes, (int)pointAmount);

                await SetAttributesAsync(ClientUpdateType.Level, Level);
                await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
                await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
                await Screen.BroadcastRoomMsgAsync(new MsgAction
                {
                    Action = MsgAction.ActionType.CharacterLevelUp,
                    Identity = Identity
                });

                await UplevelEventAsync();

                if (!noContribute && Guide != null && mentorUpLevTime > 0)
                    await Guide.AwardTutorExperienceAsync((uint) mentorUpLevTime).ConfigureAwait(false);
            }

            if (Team != null && !Team.IsLeader(Identity) && virtue > 0)
            {
                Team.Leader.VirtuePoints += virtue;
                await Team.SendAsync(new MsgTalk(Identity, MsgTalk.TalkChannel.Team, Color.White, 
                    string.Format(Language.StrAwardVirtue, Team.Leader.Name, virtue)));

                if (Team.Leader.SyndicateIdentity != 0)
                {
                    Team.Leader.SyndicateMember.GuideDonation += 1;
                    Team.Leader.SyndicateMember.GuideTotalDonation += 1;
                    await Team.Leader.SyndicateMember.SaveAsync();
                }
            }

            Experience = (ulong)amount;
            await SetAttributesAsync(ClientUpdateType.Experience, Experience);
            return true;
        }

        public async Task UplevelEventAsync()
        {
            if (Level > 3 && Metempsychosis == 0)
            {
                bool burstXp = false;
                switch (ProfessionSort)
                {
                    case 1:
                        if (!MagicData.CheckType(1110))
                        {
                            await MagicData.CreateAsync(1110, 0);
                            burstXp = true;
                        }
                        break;
                    case 2:
                        if (!MagicData.CheckType(1025))
                        {
                            await MagicData.CreateAsync(1025, 0);
                            burstXp = true;
                        }
                        break;
                    case 4:
                        if (!MagicData.CheckType(8002))
                        {
                            await MagicData.CreateAsync(8002, 0);
                            burstXp = true;
                        }
                        break;
                    case 10:
                        if (!MagicData.CheckType(1010))
                        {
                            await MagicData.CreateAsync(1010, 0);
                            burstXp = true;
                        }
                        break;
                }

                if (burstXp)
                {
                    await SetXpAsync(100);
                    await BurstXpAsync();
                }
            }

            if (Team != null)
                await Team.SyncFamilyBattlePowerAsync();

            if (ApprenticeCount > 0)
                await SynchroApprenticesSharedBattlePowerAsync();
        }

        public long CalculateExpBall(int amount = EXPBALL_AMOUNT)
        {
            long exp = 0;

            if (Level >= Kernel.RoleManager.GetLevelLimit())
                return 0;

            byte level = Level;
            if (Experience > 0)
            {
                double pct = 1.00 - Experience / (double)Kernel.RoleManager.GetLevelExperience(Level).Exp;
                if (amount > pct * Kernel.RoleManager.GetLevelExperience(Level).UpLevTime)
                {
                    amount -= (int)(pct * Kernel.RoleManager.GetLevelExperience(Level).UpLevTime);
                    exp += (long)(Kernel.RoleManager.GetLevelExperience(Level).Exp - Experience);
                    level++;
                }
            }

            while (amount > Kernel.RoleManager.GetLevelExperience(level).UpLevTime)
            {
                amount -= Kernel.RoleManager.GetLevelExperience(level).UpLevTime;
                exp += (long)Kernel.RoleManager.GetLevelExperience(level).Exp;

                if (level >= Kernel.RoleManager.GetLevelLimit())
                    return exp;
                level++;
            }

            exp += (long)(amount / (double)Kernel.RoleManager.GetLevelExperience(Level).UpLevTime *
                          Kernel.RoleManager.GetLevelExperience(Level).Exp);
            return exp;
        }

        public (int Level, ulong Experience) PreviewExpBallUsage(int amount = EXPBALL_AMOUNT)
        {
            long expBallExp = CalculateExpBall(amount);
            byte newLevel = Level;
            while (newLevel < MAX_UPLEV && amount >= (long)Kernel.RoleManager.GetLevelExperience(newLevel).Exp)
            {
                DbLevelExperience dbExp = Kernel.RoleManager.GetLevelExperience(newLevel);
                expBallExp -= (long)dbExp.Exp;
                newLevel++;
                if (newLevel < Kernel.RoleManager.GetLevelLimit()) continue;
                expBallExp = 0;
                break;
            }
            return (newLevel, (ulong) expBallExp);
        }

        public void IncrementExpBall()
        {
            m_dbObject.ExpBallUsage = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
            m_dbObject.ExpBallNum += 1;
        }

        public bool CanUseExpBall()
        {
            if (Level >= Kernel.RoleManager.GetLevelLimit())
                return false;

            if (m_dbObject.ExpBallUsage < uint.Parse(DateTime.Now.ToString("yyyyMMdd")))
            {
                m_dbObject.ExpBallNum = 0;
                return true;
            }

            return m_dbObject.ExpBallNum < 10;
        }

        #endregion

        #region Weapon Skill

        public WeaponSkill WeaponSkill { get; }
        
        public async Task AddWeaponSkillExpAsync(ushort usType, int nExp, bool byAction = false)
        {
            DbWeaponSkill skill = WeaponSkill[usType];
            if (skill == null)
            {
                await WeaponSkill.CreateAsync(usType, 0);
                if ((skill = WeaponSkill[usType]) == null)
                    return;
            }

            if (skill.Level >= MAX_WEAPONSKILLLEVEL)
                return;

            if (skill.Unlearn != 0)
                skill.Unlearn = 0;

            nExp = (int)(nExp * (1 + VioletGemBonus / 100d));

            uint nIncreaseLev = 0;
            if (skill.Level > MASTER_WEAPONSKILLLEVEL)
            {
                int nRatio = (int)(100 - (skill.Level - MASTER_WEAPONSKILLLEVEL) * 20);
                if (nRatio < 10)
                    nRatio = 10;
                nExp = Calculations.MulDiv(nExp, nRatio, 100) / 2;
            }

            int nNewExp = (int)Math.Max(nExp + skill.Experience, skill.Experience);

#if DEBUG
            if (IsPm())
                await SendAsync($"Add Weapon Skill exp: {nExp}, CurExp: {nNewExp}");
#endif

            int nLevel = skill.Level;
            uint oldPercent = (uint) (skill.Experience / (double) MsgWeaponSkill.RequiredExperience[nLevel] * 100);
            if (nLevel < MAX_WEAPONSKILLLEVEL)
            {
                if (nNewExp > MsgWeaponSkill.RequiredExperience[nLevel] ||
                    nLevel >= skill.OldLevel / 2 && nLevel < skill.OldLevel)
                {
                    nNewExp = 0;
                    nIncreaseLev = 1;
                }
            }

            if (byAction || skill.Level < Level / 10 + 1
                || skill.Level >= MASTER_WEAPONSKILLLEVEL)
            {
                skill.Experience = (uint)nNewExp;

                if (nIncreaseLev > 0)
                {
                    skill.Level += (byte) nIncreaseLev;
                    await SendAsync(new MsgWeaponSkill(skill));
                    await SendAsync(Language.StrWeaponSkillUp);
                    await WeaponSkill.SaveAsync(skill);
                }
                else
                {
                    await SendAsync(new MsgFlushExp
                    {
                        Action = MsgFlushExp.FlushMode.WeaponSkill,
                        Identity = (ushort) skill.Type,
                        Experience = skill.Experience
                    });

                    int newPercent = (int) (skill.Experience / (double) MsgWeaponSkill.RequiredExperience[nLevel] * 100);
                    if (oldPercent-oldPercent%10 != newPercent- newPercent%10)
                        await WeaponSkill.SaveAsync(skill);
                }
            }
        }

        #endregion

        #region Currency

        public uint Silvers
        {
            get => m_dbObject?.Silver ?? 0;
            set => m_dbObject.Silver = value;
        }

        public uint ConquerPoints
        {
            get => m_dbObject?.ConquerPoints ?? 0;
            set => m_dbObject.ConquerPoints = value;
        }

        public uint StorageMoney
        {
            get => m_dbObject?.StorageMoney ?? 0;
            set => m_dbObject.StorageMoney = value;
        }

        public async Task<bool> ChangeMoneyAsync(int amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardMoneyAsync(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendMoneyAsync(amount * -1, notify);
            }
            return false;
        }

        public async Task AwardMoneyAsync(int amount)
        {
            Silvers = (uint) (Silvers + amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.Money, Silvers);
        }

        public async Task<bool> SpendMoneyAsync(int amount, bool notify = false)
        {
            if (amount > Silvers)
            {
                if (notify)
                    await SendAsync(Language.StrNotEnoughMoney, MsgTalk.TalkChannel.TopLeft, Color.Red);
                return false;
            }

            Silvers = (uint)(Silvers - amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.Money, Silvers);
            return true;
        }

        public async Task<bool> ChangeConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardConquerPointsAsync(amount);
                return true;
            }
            if (amount < 0)
            {
                return await SpendConquerPointsAsync(amount * -1, notify);
            }
            return false;
        }

        public async Task AwardConquerPointsAsync(int amount)
        {
            ConquerPoints = (uint)(ConquerPoints + amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.ConquerPoints, ConquerPoints);
        }

        public async Task<bool> SpendConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > ConquerPoints)
            {
                if (notify)
                    await SendAsync(Language.StrNotEnoughEmoney, MsgTalk.TalkChannel.TopLeft, Color.Red);
                return false;
            }

            ConquerPoints = (uint)(ConquerPoints - amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.ConquerPoints, ConquerPoints);
            return true;
        }

        #endregion

        #region Pk

        public PkModeType PkMode { get; set; }

        public ushort PkPoints
        {
            get => m_dbObject?.KillPoints ?? 0;
            set => m_dbObject.KillPoints = value;
        }

        public Task SetPkModeAsync(PkModeType mode = PkModeType.Capture)
        {
            PkMode = mode;
            return SendAsync(new MsgAction
            {
                Identity = Identity,
                Action = MsgAction.ActionType.CharacterPkMode,
                Command = (uint) PkMode
            });
        }

        public async Task ProcessPkAsync(Character target)
        {
            if (!Map.IsPkField() && !Map.IsPkGameMap() && !Map.IsSynMap() && !Map.IsPrisionMap())
            {
                if (!Map.IsDeadIsland() && !target.IsEvil())
                {
                    int nAddPk = 10;
                    if (target.IsNewbie() && !IsNewbie())
                    {
                        nAddPk = 20;
                    }
                    else
                    {
                        if (Syndicate?.IsEnemy(target.SyndicateIdentity) == true)
                            nAddPk = 3;
                        else if (IsEnemy(target.Identity))
                            nAddPk = 5;
                        if (target.PkPoints > 29)
                            nAddPk /= 2;
                    }

                    int deltaLevel = Level - target.Level;
                    var synPkPoints = 0;
                    if (deltaLevel > 30)
                        synPkPoints = 1;
                    else if (deltaLevel > 20)
                        synPkPoints = 2;
                    else if (deltaLevel > 10)
                        synPkPoints = 3;
                    else if (deltaLevel > 0)
                        synPkPoints = 5;
                    else
                        synPkPoints = 10;

                    if (SyndicateIdentity != 0)
                    {
                        SyndicateMember.PkDonation += synPkPoints;
                        SyndicateMember.PkTotalDonation += synPkPoints;
                        await SyndicateMember.SaveAsync().ConfigureAwait(false);
                    }

                    if (target.SyndicateIdentity != 0)
                    {
                        target.SyndicateMember.PkDonation -= synPkPoints;
                        await target.SyndicateMember.SaveAsync().ConfigureAwait(false);
                    }

                    if (SyndicateIdentity != 0 && target.SyndicateIdentity != 0)
                    {
                        if (SyndicateIdentity == target.SyndicateIdentity)
                        {
                            await Syndicate.SendAsync(
                                string.Format(Language.StrSyndicateSameKill, SyndicateRankName, Name, target.SyndicateRankName, target.Name, Map.Name), 0, Color.White);
                        }
                        else
                        {
                            await Syndicate.SendAsync(string.Format(Language.StrSyndicateKill, SyndicateRankName, Name, target.Name, target.SyndicateRankName, target.SyndicateName, Map.Name));
                            await target.Syndicate.SendAsync(string.Format(Language.StrSyndicateBeKill, Name, SyndicateRankName, SyndicateName, target.SyndicateRankName, target.Name, Map.Name));
                        }
                    }

                    await AddAttributesAsync(ClientUpdateType.PkPoints, nAddPk);

                    await SetCrimeStatusAsync(90);

                    if (PkPoints > 29)
                        await SendAsync(Language.StrKillingTooMuch);
                }
            }
        }

        public override async Task<bool> CheckCrimeAsync(Role target)
        {
            if (target == null || !target.IsAlive) return false;
            if (!target.IsEvil() && !target.IsMonster() && !(target is DynamicNpc))
            {
                if (!Map.IsTrainingMap() && !Map.IsDeadIsland() 
                                         && !Map.IsPrisionMap() 
                                         && !Map.IsFamilyMap() 
                                         && !Map.IsPkGameMap() 
                                         && !Map.IsPkField()
                                         && !Map.IsSynMap()
                                         && !Map.IsFamilyMap())
                {
                    await SetCrimeStatusAsync(30);
                }
                return true;
            }

            if (target is Monster mob && (mob.IsGuard() || mob.IsPkKiller()))
            {
                await SetCrimeStatusAsync(15);
                return true;
            }

            return false;
        }

        #endregion

        #region Equipment

        public Item Headgear => UserPackage[Item.ItemPosition.Headwear];
        public Item Necklace => UserPackage[Item.ItemPosition.Necklace];
        public Item Ring => UserPackage[Item.ItemPosition.Ring];
        public Item RightHand => UserPackage[Item.ItemPosition.RightHand];
        public Item LeftHand => UserPackage[Item.ItemPosition.LeftHand];
        public Item Armor => UserPackage[Item.ItemPosition.Armor];
        public Item Boots => UserPackage[Item.ItemPosition.Boots];
        public Item Garment => UserPackage[Item.ItemPosition.Garment];

        #endregion

        #region User Package

        public uint LastAddItemIdentity { get; set; }

        public UserPackage UserPackage { get; }

        public async Task<bool> SpendEquipItemAsync(uint dwItem, uint dwAmount, bool bSynchro)
        {
            if (dwItem <= 0)
                return false;

            Item item = null;
            if (UserPackage[Item.ItemPosition.RightHand]?.GetItemSubType() == dwItem &&
                UserPackage[Item.ItemPosition.RightHand]?.Durability >= dwAmount)
                item = UserPackage[Item.ItemPosition.RightHand];
            else if (UserPackage[Item.ItemPosition.LeftHand]?.GetItemSubType() == dwItem)
                item = UserPackage[Item.ItemPosition.LeftHand];

            if (item == null)
                return false;

            if (!item.IsExpend() && item.Durability < dwAmount && !item.IsArrowSort())
                return false;

            if (item.IsExpend())
            {
                item.Durability = (ushort)Math.Max(0, item.Durability - (int) dwAmount);
                if (bSynchro)
                    await SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
            }
            else
            {
                if (item.IsNonsuchItem())
                {
                    await Log.GmLogAsync("SpendEquipItem",
                        $"{Name}({Identity}) Spend item:[id={item.Identity}, type={item.Type}], dur={item.Durability}, max_dur={item.MaximumDurability}");
                }
            }

            if (item.IsArrowSort() && item.Durability == 0)
            {
                Item.ItemPosition pos = item.Position;
                await UserPackage.UnEquipAsync(item.Position, UserPackage.RemovalType.Delete);
                Item other = UserPackage.GetItemByType(item.Type);
                if (other != null)
                    await UserPackage.EquipItemAsync(other, pos);
            }

            if (item.Durability > 0)
                await item.SaveAsync();
            return true;
        }

        public bool CheckWeaponSubType(uint idItem, uint dwNum = 0)
        {
            uint[] items = new uint[idItem.ToString().Length / 3];
            for (int i = 0; i < items.Length; i++)
            {
                if (idItem > 999 && idItem != 40000 && idItem != 50000)
                {
                    int idx = i * 3; // + (i > 0 ? -1 : 0);
                    items[i] = uint.Parse(idItem.ToString().Substring(idx, 3));
                }
                else
                {
                    items[i] = uint.Parse(idItem.ToString());
                }
            }

            if (items.Length <= 0) return false;

            foreach (var dwItem in items)
            {
                if (dwItem <= 0) continue;

                if (UserPackage[Item.ItemPosition.RightHand] != null &&
                    UserPackage[Item.ItemPosition.RightHand].GetItemSubType() == dwItem &&
                    UserPackage[Item.ItemPosition.RightHand].Durability >= dwNum)
                    return true;
                if (UserPackage[Item.ItemPosition.LeftHand] != null &&
                    UserPackage[Item.ItemPosition.LeftHand].GetItemSubType() == dwItem &&
                    UserPackage[Item.ItemPosition.LeftHand].Durability >= dwNum)
                    return true;

                ushort[] set1Hand = { 410, 420, 421, 430, 440, 450, 460, 480, 481, 490 };
                ushort[] set2Hand = { 510, 530, 540, 560, 561, 580 };
                ushort[] setSword = { 420, 421 };
                ushort[] setSpecial = { 601, 610, 611, 612, 613 };

                if (dwItem == 40000 || dwItem == 400)
                {
                    if (UserPackage[Item.ItemPosition.RightHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        for (int i = 0; i < set1Hand.Length; i++)
                        {
                            if (item.GetItemSubType() == set1Hand[i] && item.Durability >= dwNum)
                                return true;
                        }
                    }
                }

                if (dwItem == 50000)
                {
                    if (UserPackage[Item.ItemPosition.RightHand] != null)
                    {
                        if (dwItem == 50000) return true;

                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        for (int i = 0; i < set2Hand.Length; i++)
                        {
                            if (item.GetItemSubType() == set2Hand[i] && item.Durability >= dwNum)
                                return true;
                        }
                    }
                }

                if (dwItem == 50) // arrow
                {
                    if (UserPackage[Item.ItemPosition.RightHand] != null &&
                        UserPackage[Item.ItemPosition.LeftHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        Item arrow = UserPackage[Item.ItemPosition.LeftHand];
                        if (arrow.GetItemSubType() == 1050 && arrow.Durability >= dwNum)
                            return true;
                    }
                }

                if (dwItem == 500)
                {
                    if (UserPackage[Item.ItemPosition.RightHand] != null &&
                        UserPackage[Item.ItemPosition.LeftHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        if (item.GetItemSubType() == idItem && item.Durability >= dwNum)
                            return true;
                    }
                }

                if (dwItem == 420)
                {
                    if (UserPackage[Item.ItemPosition.RightHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        for (int i = 0; i < setSword.Length; i++)
                        {
                            if (item.GetItemSubType() == setSword[i] && item.Durability >= dwNum)
                                return true;
                        }
                    }
                }

                if (dwItem == 601 || dwItem == 610 || dwItem == 611 || dwItem == 612 || dwItem == 613)
                {
                    if (UserPackage[Item.ItemPosition.RightHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        if (item.GetItemSubType() == dwItem && item.Durability >= dwNum)
                            return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Booth

        public BoothNpc Booth { get; private set; }

        public async Task<bool> CreateBoothAsync()
        {
            if (Booth != null)
            {
                await Booth.LeaveMapAsync();
                Booth = null;
                return false;
            }

            if (Map?.IsBoothEnable() != true)
            {
                await SendAsync(Language.StrBoothRegionCantSetup);
                return false;
            }

            Booth = new BoothNpc(this);
            if (!await Booth.InitializeAsync())
                return false;
            return true;
        }

        public async Task<bool> DestroyBoothAsync()
        {
            if (Booth == null)
                return false;

            await Booth.LeaveMapAsync();
            Booth = null;
            return true;
        }

        public bool AddBoothItem(uint idItem, uint value, MsgItem.Moneytype type)
        {
            if (Booth == null)
                return false;

            if (!Booth.ValidateItem(idItem))
                return false;

            Item item = UserPackage[idItem];
            return Booth.AddItem(item, value, type);
        }

        public bool RemoveBoothItem(uint idItem)
        {
            if (Booth == null)
                return false;
            return Booth.RemoveItem(idItem);
        }

        public async Task<bool> SellBoothItemAsync(uint idItem, Character target)
        {
            if (Booth == null)
                return false;

            if (target.Identity == Identity)
                return false;

            if (!target.UserPackage.IsPackSpare(1))
                return false;

            if (GetDistance(target) > Screen.VIEW_SIZE)
                return false;

            if (!Booth.ValidateItem(idItem))
                return false;

            BoothItem item = Booth.QueryItem(idItem);
            int value = (int) item.Value;
            string moneyType = item.IsSilver ? Language.StrSilvers : Language.StrConquerPoints;
            if (item.IsSilver)
            {
                if (!await target.SpendMoneyAsync((int) item.Value, true))
                    return false;
                await AwardMoneyAsync(value);
            }
            else
            {
                if (!await target.SpendConquerPointsAsync((int) item.Value, true))
                    return false;
                await AwardConquerPointsAsync(value);
            }

            Booth.RemoveItem(idItem);

            await SendAsync(new MsgItem(item.Identity, MsgItem.ItemActionType.BoothRemove) {Command = Booth.Identity});
            await UserPackage.RemoveFromInventoryAsync(item.Item, UserPackage.RemovalType.RemoveAndDisappear);
            await item.Item.ChangeOwnerAsync(target.Identity, Item.ChangeOwnerType.BoothSale);
            await target.UserPackage.AddItemAsync(item.Item);

            await SendAsync(string.Format(Language.StrBoothSold, target.Name, item.Item.Name, value, moneyType), MsgTalk.TalkChannel.Talk, Color.White);
            await target.SendAsync(string.Format(Language.StrBoothBought, item.Item.Name, value, moneyType), MsgTalk.TalkChannel.Talk, Color.White);

            await Log.GmLogAsync("booth_sale", $"{item.Identity},{item.Item.PlayerIdentity},{Identity},{item.Item.Type},{item.IsSilver},{item.Value},{item.Item.ToJson()}");
            return true;
        }

        #endregion

        #region Map Item

        public async Task<bool> DropItemAsync(uint idItem, int x, int y, bool force = false)
        {
            Point pos = new Point(x, y);
            if (!Map.FindDropItemCell(9, ref pos))
                return false;

            Item item = UserPackage[idItem];
            if (item == null)
                return false;

            if (Booth?.QueryItem(idItem) != null)
                return false;

            if (Trade != null)
                return false;

            await Log.GmLogAsync("drop_item",
                $"{Name}({Identity}) drop item:[id={item.Identity}, type={item.Type}], dur={item.Durability}, max_dur={item.OriginalMaximumDurability}\r\n\t{item.ToJson()}");

            if (item.IsSuspicious())
                return false;

            if ((item.CanBeDropped() || force) && item.IsDisappearWhenDropped())
                return await UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.Delete);

            if (item.CanBeDropped() || force)
            {
                await UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.RemoveAndDisappear);
            }
            else
            {
                await SendAsync(string.Format(Language.StrItemCannotDiscard, item.Name));
                return false;
            }

            item.Position = Item.ItemPosition.Floor;
            await item.SaveAsync();

            MapItem mapItem = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
            if (await mapItem.CreateAsync(Map, pos, item, Identity))
            {
                await mapItem.EnterMapAsync();
                await item.SaveAsync();
            }
            else
            {
                IdentityGenerator.MapItem.ReturnIdentity(mapItem.Identity);
                if (IsGm())
                {
                    await SendAsync($"The MapItem object could not be created. Check Output log");
                }
                return false;
            }

            return true;
        }

        public async Task<bool> DropSilverAsync(uint amount)
        {
            if (amount > 10000000)
                return false;

            if (Trade != null)
                return false;

            Point pos = new Point(MapX, MapY);
            if (!Map.FindDropItemCell(1, ref pos))
                return false;

            if (!await SpendMoneyAsync((int) amount, true))
                return false;

            await Log.GmLogAsync("drop_money", $"drop money: {Identity} {Name} has dropped {amount} silvers");

            MapItem mapItem = new MapItem((uint)IdentityGenerator.MapItem.GetNextIdentity);
            if (mapItem.CreateMoney(Map, pos, amount, 0u))
                await mapItem.EnterMapAsync();
            else
            {
                IdentityGenerator.MapItem.ReturnIdentity(mapItem.Identity);
                if (IsGm())
                {
                    await SendAsync($"The DropSilver MapItem object could not be created. Check Output log");
                }
                return false;
            }

            return true;
        }

        public async Task<bool> PickMapItemAsync(uint idItem)
        {
            MapItem mapItem = Map.QueryAroundRole(this, idItem) as MapItem;
            if (mapItem == null)
                return false;

            if (GetDistance(mapItem) > 0)
            {
                await SendAsync(Language.StrTargetNotInRange);
                return false;
            }

            if (!mapItem.IsMoney() && !UserPackage.IsPackSpare(1))
            {
                await SendAsync(Language.StrYourBagIsFull);
                return false;
            }

            if (mapItem.OwnerIdentity != Identity && mapItem.IsPrivate())
            {
                Character owner = Kernel.RoleManager.GetUser(mapItem.OwnerIdentity);
                if (owner != null && !IsMate(owner))
                {
                    if (Team == null 
                        || (!Team.IsMember(mapItem.OwnerIdentity)
                        || mapItem.IsMoney() && !Team.MoneyEnable)
                        || mapItem.IsJewel() && !Team.JewelEnable
                        || mapItem.IsItem() && !Team.ItemEnable)
                    {
                        await SendAsync(Language.StrCannotPickupOtherItems);
                        return false;
                    }
                }
            }

            if (mapItem.IsMoney())
            {
                await AwardMoneyAsync((int) mapItem.Money);
                if (mapItem.Money > 1000)
                {
                    await SendAsync(new MsgAction
                    {
                        Identity = Identity,
                        Command = mapItem.Money,
                        ArgumentX = MapX,
                        ArgumentY = MapY,
                        Action = MsgAction.ActionType.MapGold
                    });
                }
                await SendAsync(string.Format(Language.StrPickupSilvers, mapItem.Money));

                await Log.GmLogAsync("pickup_money", $"User[{Identity},{Name}] picked up {mapItem.Money} at {MapIdentity}({Map.Name}) {MapX}, {MapY}");
            }
            else
            {
                Item item = await mapItem.GetInfoAsync(this);

                if (item != null)
                {
                    await UserPackage.AddItemAsync(item);
                    await SendAsync(string.Format(Language.StrPickupItem, item.Name));

                    await Log.GmLogAsync("pickup_item", $"User[{Identity},{Name}] picked up (id:{mapItem.ItemIdentity}) {mapItem.Itemtype} at {MapIdentity}({Map.Name}) {MapX}, {MapY}");

                    if (VipLevel > 0 && mapItem.IsConquerPointsPack())
                    {
                        await UserPackage.UseItemAsync(item.Identity, Item.ItemPosition.Inventory);
                    }

                    if (VipLevel > 1 && UserPackage.MultiCheckItem(Item.TYPE_METEOR, Item.TYPE_METEOR, 10, true))
                    {
                        await UserPackage.MultiSpendItemAsync(Item.TYPE_METEOR, Item.TYPE_METEOR, 10, true);
                        await UserPackage.AwardItemAsync(Item.TYPE_METEOR_SCROLL);
                    }

                    if (VipLevel > 3 && UserPackage.MultiCheckItem(Item.TYPE_DRAGONBALL, Item.TYPE_DRAGONBALL, 10, true))
                    {
                        await UserPackage.MultiSpendItemAsync(Item.TYPE_DRAGONBALL, Item.TYPE_DRAGONBALL, 10, true);
                        await UserPackage.AwardItemAsync(Item.TYPE_DRAGONBALL_SCROLL);
                    }
                }
            }
            
            await mapItem.LeaveMapAsync();
            return true;
        }

        #endregion

        #region Trade

        public Trade Trade { get; set; }

        #endregion

        #region Peerage

        public NobilityRank NobilityRank => Kernel.PeerageManager.GetRanking(Identity);

        public int NobilityPosition => Kernel.PeerageManager.GetPosition(Identity);

        public ulong NobilityDonation
        {
            get => m_dbObject.Donation;
            set => m_dbObject.Donation = value;
        }

        public async Task SendNobilityInfoAsync(bool broadcast = false)
        {
            MsgPeerage msg = new MsgPeerage
            {
                Action = NobilityAction.Info,
                DataLow = Identity
            };
            msg.Strings.Add($"{Identity} {NobilityDonation} {(int) NobilityRank:d} {NobilityPosition}");
            await SendAsync(msg);

            if (broadcast)
                await BroadcastRoomMsgAsync(msg, false);
        }

        #endregion

        #region Team

        public uint VirtuePoints
        {
            get => m_dbObject.Virtue;
            set => m_dbObject.Virtue = value;
        }

        public Team Team { get; set; }

        #endregion

        #region Battle Attributes

        public override int BattlePower
        {
            get
            {
#if BATTLE_POWER
                int result = Level + Metempsychosis * 5 + (int) NobilityRank;
                if (SyndicateIdentity > 0)
                    result += Syndicate.GetSharedBattlePower(SyndicateRank);
                result += Math.Max(FamilyBattlePower, Guide?.SharedBattlePower ?? 0);
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.BattlePower ?? 0;
                }
                return result;
#else
                return 1;
#endif
            }
        }

        public int PureBattlePower
        {
            get
            {
                int result = Level + Metempsychosis * 5 + (int) NobilityRank;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.BattlePower ?? 0;
                }
                return result;
            }
        }

        public override int MinAttack
        {
            get
            {
                if (Transformation != null)
                    return Transformation.MinAttack;

                int result = Strength;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    if (pos == Item.ItemPosition.LeftHand)
                    {
                        result += (UserPackage[pos]?.MinAttack ?? 0) / 2;
                    }
                    else
                    {
                        result += UserPackage[pos]?.MinAttack ?? 0;
                    }
                }

                result = (int) (result * (1 + (DragonGemBonus / 100d)));
                return result;
            }
        }

        public override int MaxAttack
        {
            get
            {
                if (Transformation != null)
                    return Transformation.MaxAttack;

                int result = Strength;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    if (pos == Item.ItemPosition.LeftHand)
                        result += (UserPackage[pos]?.MaxAttack ?? 0) / 2;
                    else
                        result += UserPackage[pos]?.MaxAttack ?? 0;
                }

                result = (int)(result * (1 + (DragonGemBonus / 100d)));
                return result;
            }
        }

        public override int MagicAttack
        {
            get
            {
                if (Transformation != null)
                    return Transformation.MaxAttack;

                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    result += UserPackage[pos]?.MagicAttack ?? 0;
                }

                result = (int)(result * (1 + (PhoenixGemBonus/ 100d)));
                return result;
            }
        }

        public override int Defense
        {
            get
            {
                if (Transformation != null)
                    return Transformation.Defense;
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    result += UserPackage[pos]?.Defense ?? 0;
                }

                return result;
            }
        }

        public override int Defense2
        {
            get
            {
                if (Transformation != null)
                    return (int) Transformation.Defense2;
                return QueryStatus(StatusSet.VORTEX) != null ? 1 : Metempsychosis >= 1 && ProfessionLevel >= 3 ? 7000 : Calculations.DEFAULT_DEFENCE2;
            }
        }

        public override int MagicDefense
        {
            get
            {
                if (Transformation != null)
                    return Transformation.MagicDefense;
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    result += UserPackage[pos]?.MagicDefense ?? 0;
                }

                return result;
            }
        }

        public override int MagicDefenseBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.MagicDefenseBonus ?? 0;
                }
                return result;
            }
        }

        public override int Dodge
        {
            get
            {
                if (Transformation != null)
                    return (int) Transformation.Dodge;
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Dodge ?? 0;
                }
                return result;
            }
        }

        public override int Blessing
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Blessing ?? 0;
                }
                return result;
            }
        }

        public override int AddFinalAttack
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                    pos <= Item.ItemPosition.EquipmentEnd;
                    pos++)
                {
                    result += UserPackage[pos]?.AddFinalDamage ?? 0;
                }

                return result;
            }
        }

        public override int AddFinalMAttack
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                    pos <= Item.ItemPosition.EquipmentEnd;
                    pos++)
                {
                    result += UserPackage[pos]?.AddFinalMagicDamage ?? 0;
                }

                return result;
            }
        }

        public override int AddFinalDefense
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin;
                    pos <= Item.ItemPosition.EquipmentEnd;
                    pos++)
                {
                    result += UserPackage[pos]?.AddFinalDefense ?? 0;
                }

                return result;
            }
        }

        public override int AddFinalMDefense
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.AddFinalMagicDefense ?? 0;
                }
                return result;
            }
        }

        public override int AttackSpeed { get; } = 1000;

        public override int Accuracy
        {
            get
            {
                int result = Agility;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.Accuracy ?? 0;
                }
                return result;
            }
        }

        public int DragonGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    Item item = UserPackage[pos];
                    if (item != null)
                    {
                        result += item.DragonGemEffect;
                    }
                }
                return result;
            }
        }

        public int PhoenixGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.PhoenixGemEffect ?? 0;
                }
                return result;
            }
        }

        public int VioletGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.VioletGemEffect ?? 0;
                }
                return result;
            }
        }

        public int MoonGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.MoonGemEffect ?? 0;
                }
                return result;
            }
        }

        public int RainbowGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.RainbowGemEffect ?? 0;
                }
                return result;
            }
        }

        public int FuryGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.FuryGemEffect ?? 0;
                }
                return result;
            }
        }

        public int TortoiseGemBonus
        {
            get
            {
                int result = 0;
                for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    result += UserPackage[pos]?.TortoiseGemEffect ?? 0;
                }
                return result;
            }
        }

        public int KoCount { get; set; }

        #endregion

        #region Battle

        public override bool IsBowman => UserPackage[Item.ItemPosition.RightHand]?.IsBow() == true;

        public override bool IsShieldUser => UserPackage[Item.ItemPosition.LeftHand]?.IsShield() == true;

        public async Task<bool> AutoSkillAttackAsync(Role target)
        {
            foreach (var magic in MagicData.Magics.Values)
            {
                float percent = magic.Percent;
                if (magic.AutoActive > 0
                    && Transformation == null
                    && (magic.WeaponSubtype == 0
                        || CheckWeaponSubType(magic.WeaponSubtype, magic.UseItemNum))
                    && await Kernel.ChanceCalcAsync(percent))
                {
                    return await ProcessMagicAttackAsync(magic.Type, target.Identity, target.MapX, target.MapY, magic.AutoActive);
                }
            }

            return false;
        }

        public async Task SendWeaponMagic2Async(Role pTarget = null)
        {
            Item item = null;

            if (UserPackage[Item.ItemPosition.RightHand] != null &&
                UserPackage[Item.ItemPosition.RightHand].Effect != Item.ItemEffect.None)
                item = UserPackage[Item.ItemPosition.RightHand];
            if (UserPackage[Item.ItemPosition.LeftHand] != null &&
                UserPackage[Item.ItemPosition.LeftHand].Effect != Item.ItemEffect.None)
                if (item != null && await Kernel.ChanceCalcAsync(50f) || item == null)
                    item = UserPackage[Item.ItemPosition.LeftHand];

            if (item != null)
            {
                switch (item.Effect)
                {
                    case Item.ItemEffect.Life:
                        {
                            if (!await Kernel.ChanceCalcAsync(15f))
                                return;
                            await AddAttributesAsync(ClientUpdateType.Hitpoints, 310);
                            var msg = new MsgMagicEffect
                            {
                                AttackerIdentity = Identity,
                                MagicIdentity = 1005
                            };
                            msg.Append(Identity, 310, false);
                            await BroadcastRoomMsgAsync(msg, true);
                            break;
                        }

                    case Item.ItemEffect.Mana:
                        {
                            if (!await Kernel.ChanceCalcAsync(17.5f))
                                return;
                            await AddAttributesAsync(ClientUpdateType.Mana, 310);
                            var msg = new MsgMagicEffect
                            {
                                AttackerIdentity = Identity,
                                MagicIdentity = 1195
                            };
                            msg.Append(Identity, 310, false);
                            await BroadcastRoomMsgAsync(msg, true);
                            break;
                        }

                    case Item.ItemEffect.Poison:
                        {
                            if (pTarget == null)
                                return;

                            if (!await Kernel.ChanceCalcAsync(5f))
                                return;

                            var msg = new MsgMagicEffect
                            {
                                AttackerIdentity = Identity,
                                MagicIdentity = 1320
                            };
                            msg.Append(pTarget.Identity, 210, true);
                            await BroadcastRoomMsgAsync(msg, true);

                            await pTarget.AttachStatusAsync(this, StatusSet.POISONED, 310, POISONDAMAGE_INTERVAL, 20, 0);

                            var result = await AttackAsync(pTarget);
                            int nTargetLifeLost = result.Damage;

                            await SendDamageMsgAsync(pTarget.Identity, nTargetLifeLost);

                            if (!pTarget.IsAlive)
                            {
                                int dwDieWay = 1;
                                if (nTargetLifeLost > pTarget.MaxLife / 3)
                                    dwDieWay = 2;

                                await KillAsync(pTarget, IsBowman ? 5 : (uint)dwDieWay);
                            }
                            break;
                        }
                }
            }
        }

        public async Task<bool> DecEquipmentDurabilityAsync(bool bAttack, int hitByMagic, ushort useItemNum)
        {
            int nInc = -1 * useItemNum;

            for (Item.ItemPosition i = Item.ItemPosition.Headwear; i <= Item.ItemPosition.Crop; i++)
            {
                if (i == Item.ItemPosition.Garment || i == Item.ItemPosition.Gourd || i == Item.ItemPosition.Steed
                    || i == Item.ItemPosition.SteedArmor || i == Item.ItemPosition.LeftHandAccessory ||
                    i == Item.ItemPosition.RightHandAccessory)
                    continue;
                if (hitByMagic == 1)
                {
                    if (i == Item.ItemPosition.Ring
                        || i == Item.ItemPosition.RightHand
                        || i == Item.ItemPosition.LeftHand
                        || i == Item.ItemPosition.Boots)
                    {
                        if (!bAttack)
                            await AddEquipmentDurabilityAsync(i, nInc);
                    }
                    else
                    {
                        if (bAttack)
                            await AddEquipmentDurabilityAsync(i, nInc);
                    }
                }
                else
                {
                    if (i == Item.ItemPosition.Ring
                        || i == Item.ItemPosition.RightHand
                        || i == Item.ItemPosition.LeftHand
                        || i == Item.ItemPosition.Boots)
                    {
                        if (!bAttack)
                            await AddEquipmentDurabilityAsync(i, -1);
                    }
                    else
                    {
                        if (bAttack)
                            await AddEquipmentDurabilityAsync(i, nInc);
                    }
                }
            }

            return true;
        }

        public async Task AddEquipmentDurabilityAsync(Item.ItemPosition pos, int nInc)
        {
            if (nInc >= 0)
                return;

            Item item = UserPackage[pos];
            if (item == null
                || !item.IsEquipment()
                || item.GetItemSubType() == 2100)
                return;

            ushort nOldDur = item.Durability;
            ushort nDurability = (ushort)Math.Max(0, item.Durability + nInc);

            if (nDurability < 100)
            {
                if (nDurability % 10 == 0)
                    await SendAsync(string.Format(Language.StrDamagedRepair, item.Itemtype.Name));
            }
            else if (nDurability < 200)
            {
                if (nDurability % 10 == 0)
                    await SendAsync(string.Format(Language.StrDurabilityRepair, item.Itemtype.Name));
            }

            item.Durability = nDurability;
            await item.SaveAsync();

            int noldDur = (int) Math.Floor(nOldDur / 100f);
            int nnewDur = (int) Math.Floor(nDurability / 100f);

            if (nDurability <= 0)
            {
                await SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
            }
            else if (noldDur != nnewDur)
            {
                await SendAsync(new MsgItemInfo(item, MsgItemInfo.ItemMode.Update));
            }
        }

        public bool SetAttackTarget(Role target)
        {
            if (target == null)
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (!target.IsAttackable(this))
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (target.IsWing && !IsWing && !IsBowman)
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (QueryStatus(StatusSet.FATAL_STRIKE) != null)
            {
                if (GetDistance(target) > Screen.VIEW_SIZE)
                    return false;
            }
            else
            {
                if (GetDistance(target) > GetAttackRange(target.SizeAddition))
                {
                    BattleSystem.ResetBattle();
                    return false;
                }
            }

            if (CurrentEvent != null && !CurrentEvent.IsAttackEnable(this))
                return false;

            return true;
        }

        public Task AddSynWarScoreAsync(DynamicNpc npc, int score)
        {
            if (npc == null || score == 0)
                return Task.CompletedTask;

            if (Syndicate == null || npc.OwnerIdentity == SyndicateIdentity)
                return Task.CompletedTask;

            npc.AddSynWarScore(Syndicate, score);
            return Task.CompletedTask;
        }

        public async Task<int> GetInterAtkRateAsync()
        {
            int nRate = USER_ATTACK_SPEED;
            int nRateR = 0, nRateL = 0;

            if (UserPackage[Item.ItemPosition.RightHand] != null)
                nRateR = UserPackage[Item.ItemPosition.RightHand].Itemtype.AtkSpeed;
            if (UserPackage[Item.ItemPosition.LeftHand] != null && !UserPackage[Item.ItemPosition.LeftHand].IsArrowSort())
                nRateL = UserPackage[Item.ItemPosition.LeftHand].Itemtype.AtkSpeed;

            if (nRateR > 0 && nRateL > 0)
                nRate = (nRateR + nRateL) / 2;
            else if (nRateR > 0)
                nRate = nRateR;
            else if (nRateL > 0)
                nRate = nRateL;

#if DEBUG
            if (QueryStatus(StatusSet.CYCLONE) != null)
            {
                nRate = Calculations.CutTrail(0,
                    Calculations.AdjustData(nRate, QueryStatus(StatusSet.CYCLONE).Power));
                if (IsPm())
                    await SendAsync($"attack speed+: {nRate}");
            }
#endif

            return Math.Max(400, nRate);
        }
        
        public override int GetAttackRange(int sizeAdd)
        {
            int nRange = 1, nRangeL = 0, nRangeR = 0;

            if (UserPackage[Item.ItemPosition.RightHand] != null && UserPackage[Item.ItemPosition.RightHand].IsWeapon())
                nRangeR = UserPackage[Item.ItemPosition.RightHand].AttackRange;
            if (UserPackage[Item.ItemPosition.LeftHand] != null && UserPackage[Item.ItemPosition.LeftHand].IsWeapon())
                nRangeL = UserPackage[Item.ItemPosition.LeftHand].AttackRange;

            if (nRangeR > 0 && nRangeL > 0)
                nRange = (nRangeR + nRangeL) / 2;
            else if (nRangeR > 0)
                nRange = nRangeR;
            else if (nRangeL > 0)
                nRange = nRangeL;

            nRange += (SizeAddition + sizeAdd + 1) / 2;

            return nRange + 1;
        }

        public override bool IsImmunity(Role target)
        {
            if (base.IsImmunity(target))
                return true;

            if (target is Character user)
            {
                switch (PkMode)
                {
                    case PkModeType.Capture:
                        return !user.IsEvil();
                    case PkModeType.Peace:
                        return true;
                    case PkModeType.FreePk:
                        if (Level >= 26 && user.Level < 26)
                            return true;
                        return false;
                    case PkModeType.Team:
                        if (IsFriend(user.Identity))
                            return true;
                        if (IsMate(user.Identity))
                            return true;
                        if (Map?.IsFamilyMap() == true)
                        {
                            if (Family.GetMember(user.Identity) != null)
                                return true;
                        }
                        else
                        {
                            if (Syndicate?.QueryMember(user.Identity) != null)
                                return true;
                            if (Syndicate?.IsAlly(user.SyndicateIdentity) == true)
                                return true;
                            if (Team?.IsMember(user.Identity) == true)
                                return true;
                        }
                        return false;
                }
            }
            else if (target is Monster monster)
            {
                switch (PkMode)
                {
                    case PkModeType.Peace:
                        return false;
                    case PkModeType.Team:
                    case PkModeType.Capture:
                        if (monster.IsGuard() || monster.IsPkKiller())
                            return true;
                        return false;
                    case PkModeType.FreePk:
                        return false;
                }
            }
            else if (target is DynamicNpc dynaNpc)
            {
                return false;
            }
            
            return true;
        }

        public override bool IsAttackable(Role attacker)
        {
            if (attacker is Character && Map.IsPkDisable())
                return false;

            return (!m_respawn.IsActive() || m_respawn.IsTimeOut()) && IsAlive && !(attacker is Character && Map.QueryRegion(RegionTypes.PkProtected, MapX, MapY));
        }

        public override async Task<(int Damage, InteractionEffect Effect)> AttackAsync(Role target)
        {
            if (target == null)
                return (0, InteractionEffect.None);

            if (!target.IsEvil() && Map.IsDeadIsland() || (target is Monster mob && mob.IsGuard()))
                await SetCrimeStatusAsync(15);

            return await BattleSystem.CalcPowerAsync(BattleSystem.MagicType.None, this, target);
        }

        public override async Task KillAsync(Role target, uint dieWay)
        {
            if (target == null)
                return;

            if (target is Character targetUser)
            {
                await BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.Kill,
                    SenderIdentity = Identity,
                    TargetIdentity = target.Identity,
                    PosX = target.MapX,
                    PosY = target.MapY,
                    Data = (int) dieWay
                }, true);

                if (MagicData.QueryMagic != null && MagicData.QueryMagic.Sort != MagicData.MagicSort.Activateswitch)
                    await ProcessPkAsync(targetUser);

                if (targetUser.IsBlessed && !IsBlessed)
                {
                    if (QueryStatus(StatusSet.CURSED) == null)
                        await AttachStatusAsync(this, StatusSet.CURSED, 0, 300, 0, 0, true);
                    else
                    {
                        QueryStatus(StatusSet.CURSED).IncTime(300000, int.MaxValue);
                        await QueryStatus(StatusSet.CURSED).ChangeDataAsync(0, QueryStatus(StatusSet.CURSED).RemainingTime, 0, 0);
                    }
                }
            }
            else if (target is Monster monster)
            {
                await AddXpAsync(1);

                if (QueryStatus(StatusSet.CYCLONE) != null || QueryStatus(StatusSet.SUPERMAN) != null)
                {
                    KoCount += 1;
                    var status = QueryStatus(StatusSet.CYCLONE) ?? QueryStatus(StatusSet.SUPERMAN);
                    status?.IncTime(700, 30000);
                }

                if (!(MessageBox is CaptchaBox))
                    m_KillsToCaptcha++;

                if (!(MessageBox is CaptchaBox)
                    && m_KillsToCaptcha > 5000 + await Kernel.NextAsync(1500)
                    && await Kernel.ChanceCalcAsync(50, 10000))
                {
                    CaptchaBox captcha = (CaptchaBox) (MessageBox = new CaptchaBox(this));
                    await captcha.GenerateAsync();
                    m_KillsToCaptcha = 0;
                }

                await KillMonsterAsync(monster.Type);
            }

            await target.BeKillAsync(this);

            if (CurrentEvent != null)
                await CurrentEvent.OnKillAsync(this, target, MagicData.QueryMagic);
        }

        public override async Task<bool> BeAttackAsync(BattleSystem.MagicType magic, Role attacker, int power,
            bool bReflectEnable)
        {
            if (attacker == null)
                return false;

            if (IsLucky && await Kernel.ChanceCalcAsync(1, 100))
            {
                await SendEffectAsync("LuckyGuy", true);
                power /= 10;
            }

            if ((PreviousProfession == 25 || FirstProfession == 25) && bReflectEnable && await Kernel.ChanceCalcAsync(5, 100))
            {
                power = Math.Min(1700, power);
                
                await attacker.BeAttackAsync(magic, this, power, false);
                await BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.ReflectMagic,
                    Data = power,
                    PosX = MapX,
                    PosY = MapY,
                    SenderIdentity = Identity,
                    TargetIdentity = attacker.Identity
                }, true);

                if (!attacker.IsAlive)
                    await attacker.BeKillAsync(null);

                return true;
            }

            if (CurrentEvent != null)
                await CurrentEvent.OnBeAttackAsync(attacker, this, (int) Math.Min(Life, power));

            if (power > 0)
            {
                await AddAttributesAsync(ClientUpdateType.Hitpoints, power * -1);
                _ = BroadcastTeamLifeAsync().ConfigureAwait(false);
            }

            if (IsAlive && await Kernel.ChanceCalcAsync(5))
                await SendGemEffect2Async();

            if (!Map.IsTrainingMap())
                await DecEquipmentDurabilityAsync(true, (int) magic, (ushort) (power > MaxLife / 4 ? 10 : 1));

            if (MagicData.QueryMagic != null && MagicData.State == MagicData.MagicState.Intone)
                await MagicData.AbortMagicAsync(true);

            if (Action == EntityAction.Sit)
                await SetAttributesAsync(ClientUpdateType.Stamina, (ulong) (Energy / 2));
            return true;    
        }

        public override async Task BeKillAsync(Role attacker)
        {
            if (QueryStatus(StatusSet.GHOST) != null)
                return;

            BattleSystem.ResetBattle();

            TransformationMesh = 0;
            Transformation = null;
            m_transformation.Clear();

            if (QueryStatus(StatusSet.CYCLONE) != null || QueryStatus(StatusSet.SUPERMAN) != null)
            {
                await FinishXpAsync();
            }

            await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);

            await DetachStatusAsync(StatusSet.BLUE_NAME);
            await DetachAllStatusAsync();

            if (Scapegoat)
                await SetScapegoatAsync(false);

            await AttachStatusAsync(this, StatusSet.DEAD, 0, int.MaxValue, 0, 0);
            await AttachStatusAsync(this, StatusSet.GHOST, 0, int.MaxValue, 0, 0);

            m_ghost.Startup(4);

            if (CurrentEvent is ArenaQualifier qualifier)
            {
                ArenaQualifier.QualifierMatch match = qualifier.FindMatchByMap(MapIdentity);
                if (match != null)
                {
                    await match.FinishAsync(null, this);
                    return;
                }
            }

            uint idMap = 0;
            Point posTarget = new Point();
            if (Map.GetRebornMap(ref idMap, ref posTarget))
                await SavePositionAsync(idMap, (ushort) posTarget.X, (ushort) posTarget.Y);

            if (Map.IsPkField() || Map.IsSynMap())
            {
                if (Map.IsSynMap() && !Map.IsWarTime())
                    await SavePositionAsync(1002, 430, 378);
                return;
            }

            if (Map.IsPrisionMap())
            {
                if (!Map.IsDeadIsland())
                {
                    int nChance = Math.Min(90, 20 + PkPoints / 2);
                    await UserPackage.RandDropItemAsync(3, nChance);
                }
                return;
            }

            if (attacker == null)
                return;

            if (!Map.IsDeadIsland())
            {
                int nChance = 0;
                if (PkPoints < 30)
                    nChance = 10 + await Kernel.NextAsync(40);
                else if (PkPoints < 100)
                    nChance = 50 + await Kernel.NextAsync(50);
                else
                    nChance = 100;

                int nItems = UserPackage.InventoryCount;
                int nDropItem = Level < 15 ? 0 : nItems * nChance / 100;

                await UserPackage.RandDropItemAsync(nDropItem);

                if (attacker.Identity != Identity && attacker is Character atkrUser)
                {
                    await CreateEnemyAsync(atkrUser);

                    if (!IsBlessed)
                    {
                        float nLossPercent;
                        if (PkPoints < 30)
                            nLossPercent = 0.01f;
                        else if (PkPoints < 100)
                            nLossPercent = 0.02f;
                        else nLossPercent = 0.03f;

                        long nLevExp = (long) Experience;
                        long nLostExp = (long) (nLevExp * nLossPercent);

                        if (nLostExp > 0)
                        {
                            await AddAttributesAsync(ClientUpdateType.Experience, nLostExp * -1);
                            await attacker.AddAttributesAsync(ClientUpdateType.Experience, nLostExp / 3);
                        }
                    }

                    if (!atkrUser.IsBlessed && IsBlessed)
                    {
                        if (atkrUser.QueryStatus(StatusSet.CURSED) != null)
                        {
                            //var status = QueryStatus(StatusSet.CYCLONE) ?? QueryStatus(StatusSet.SUPERMAN);
                            //status?.IncTime(700, 30000);
                            var status = atkrUser.QueryStatus(StatusSet.CURSED);
                            status.IncTime(300000, 60 * 5 * 12 * 1000);
                            await atkrUser.SynchroAttributesAsync(ClientUpdateType.CursedTimer, (ulong) status.RemainingTime);
                        }
                        else
                        {
                            await atkrUser.AttachStatusAsync(this, StatusSet.CURSED, 0, 300, 0, 0);
                        }
                    }

                    if (PkPoints >= 300)
                    {
                        //await UserPackage.RandDropEquipmentAsync(atkrUser);
                        //await UserPackage.RandDropEquipmentAsync(atkrUser);
                        await Kernel.ItemManager.DetainItemAsync(this, atkrUser);
                        await Kernel.ItemManager.DetainItemAsync(this, atkrUser);
                    }
                    else if (PkPoints >= 100)
                    {
                        //await UserPackage.RandDropEquipmentAsync(atkrUser);
                        await Kernel.ItemManager.DetainItemAsync(this, atkrUser);
                    }
                    else if (PkPoints >= 30 && await Kernel.ChanceCalcAsync(40, 100))
                    {
                        //await UserPackage.RandDropEquipmentAsync(atkrUser);
                        await Kernel.ItemManager.DetainItemAsync(this, atkrUser);
                    }

                    if (PkPoints >= 100)
                    {
                        await SavePositionAsync(6000, 31, 72);
                        await FlyMapAsync(6000, 31, 72);
                        await Kernel.RoleManager.BroadcastMsgAsync(
                            string.Format(Language.StrGoToJail, attacker.Name, Name), MsgTalk.TalkChannel.Talk,
                            Color.White);
                    }
                }
            }
            else if (attacker is Character atkUser && Map.IsDeadIsland())
            {
                await CreateEnemyAsync(atkUser);
            }
            else if (attacker is Monster monster)
            {
                if (monster.IsGuard() && PkPoints > 99)
                {
                    await SavePositionAsync(6000, 31, 72);
                    await FlyMapAsync(6000, 31, 72);
                    await Kernel.RoleManager.BroadcastMsgAsync(
                        string.Format(Language.StrGoToJail, attacker.Name, Name), MsgTalk.TalkChannel.Talk,
                        Color.White);
                }
            }
        }

        public async Task SendGemEffectAsync()
        {
            var setGem = new List<Item.SocketGem>();

            for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos < Item.ItemPosition.EquipmentEnd; pos++)
            {
                Item item = UserPackage[pos];
                if (item == null)
                    continue;

                setGem.Add(item.SocketOne);
                if (item.SocketTwo != Item.SocketGem.NoSocket)
                    setGem.Add(item.SocketTwo);
            }

            int nGems = setGem.Count;
            if (nGems <= 0)
                return;

            string strEffect = "";
            switch (setGem[await Kernel.NextAsync(0, nGems)])
            {
                case Item.SocketGem.SuperPhoenixGem:
                    strEffect = "phoenix";
                    break;
                case Item.SocketGem.SuperDragonGem:
                    strEffect = "goldendragon";
                    break;
                case Item.SocketGem.SuperFuryGem:
                    strEffect = "fastflash";
                    break;
                case Item.SocketGem.SuperRainbowGem:
                    strEffect = "rainbow";
                    break;
                case Item.SocketGem.SuperKylinGem:
                    strEffect = "goldenkylin";
                    break;
                case Item.SocketGem.SuperVioletGem:
                    strEffect = "purpleray";
                    break;
                case Item.SocketGem.SuperMoonGem:
                    strEffect = "moon";
                    break;
            }

            await SendEffectAsync(strEffect, true);
        }

        public async Task SendGemEffect2Async()
        {
            var setGem = new List<int>();

            for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos < Item.ItemPosition.EquipmentEnd; pos++)
            {
                Item item = UserPackage[pos];
                if (item == null)
                    continue;

                if (item.Blessing > 0)
                    setGem.Add(item.Blessing);
            }

            int nGems = setGem.Count;
            if (nGems <= 0)
                return;

            string strEffect = "";
            switch (setGem[await Kernel.NextAsync(0, nGems)])
            {
                case 1:
                    strEffect = "Aegis1";
                    break;
                case 3:
                    strEffect = "Aegis2";
                    break;
                case 5:
                    strEffect = "Aegis3";
                    break;
                case 7:
                    strEffect = "Aegis4";
                    break;
            }

            await SendEffectAsync(strEffect, true);
        }

        #endregion

        #region Revive

        public bool CanRevive()
        {
            return !IsAlive && m_tRevive.IsTimeOut();
        }

        public async Task RebornAsync(bool chgMap, bool isSpell = false)
        {
            if (IsAlive || !CanRevive() && !isSpell)
            {
                if (QueryStatus(StatusSet.GHOST) != null)
                {
                    await DetachStatusAsync(StatusSet.GHOST);
                }

                if (QueryStatus(StatusSet.DEAD) != null)
                {
                    await DetachStatusAsync(StatusSet.DEAD);
                }

                if (TransformationMesh == 98 || TransformationMesh == 99)
                    await ClearTransformationAsync();
                return;
            }

            BattleSystem.ResetBattle();
            
            await DetachStatusAsync(StatusSet.GHOST);
            await DetachStatusAsync(StatusSet.DEAD);
            
            await ClearTransformationAsync();

            await SetAttributesAsync(ClientUpdateType.Stamina, DEFAULT_USER_ENERGY);
            await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
            await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
            await SetXpAsync(0);

            if (CurrentEvent != null)
            {
                await CurrentEvent.OnReviveAsync(this, !isSpell);

                if (isSpell)
                {
                    await FlyMapAsync(m_idMap, m_posX, m_posY);
                }
                else
                {
                    var revive = await CurrentEvent.GetRevivePositionAsync(this);
                    await FlyMapAsync(revive.id, revive.x, revive.y);
                }
            }
            else if (chgMap || !IsBlessed && !isSpell)
            {
                await FlyMapAsync(m_dbObject.MapID, m_dbObject.X, m_dbObject.Y);
            }
            else
            {
                if (!isSpell && (Map.IsPrisionMap()
                                 || Map.IsPkField()
                                 || Map.IsPkGameMap()
                                 || Map.IsSynMap()))
                {
                    await FlyMapAsync(m_dbObject.MapID, m_dbObject.X, m_dbObject.Y);
                }
                else
                {
                    await FlyMapAsync(m_idMap, m_posX, m_posY);
                }
            }

            m_respawn.Startup(CHGMAP_LOCK_SECS);
        }

        #endregion

        #region Rebirth

        public async Task<bool> RebirthAsync(ushort prof, ushort look)
        {
            DbRebirth data = Kernel.RoleManager.GetRebirth(Profession, prof, Metempsychosis + 1);

            if (data == null)
            {
                if (IsPm())
                    await SendAsync($"No rebirth set for {Profession} -> {prof}");
                return false;
            }

            if (Level < data.NeedLevel)
            {
                await SendAsync(Language.StrNotEnoughLevel);
                return false;
            }

            if (Level >= 130)
            {
                DbLevelExperience levExp = Kernel.RoleManager.GetLevelExperience(Level);
                if (levExp != null)
                {
                    float fExp = Experience / (float)levExp.Exp;
                    uint metLev = (uint)(Level * 10000 + fExp * 1000);
                    if (metLev > m_dbObject.MeteLevel)
                        m_dbObject.MeteLevel = metLev;
                }
                else if (Level >= MAX_UPLEV)
                    m_dbObject.MeteLevel = MAX_UPLEV * 10000;
            }

            int metempsychosis = Metempsychosis;
            int oldProf = Profession;
            await ResetUserAttributesAsync(Metempsychosis, prof, look, data.NewLevel);

            for (Item.ItemPosition pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
            {
                if (UserPackage[pos] != null)
                    await UserPackage[pos].DegradeItemAsync(false);
            }

            var removeSkills = Kernel.RoleManager.GetMagictypeOp(MagicTypeOp.MagictypeOperation.RemoveOnRebirth, oldProf / 10, prof / 10, metempsychosis)?.Magics;
            var resetSkills = Kernel.RoleManager.GetMagictypeOp(MagicTypeOp.MagictypeOperation.ResetOnRebirth, oldProf / 10, prof/10, metempsychosis)?.Magics;
            var learnSkills = Kernel.RoleManager.GetMagictypeOp(MagicTypeOp.MagictypeOperation.LearnAfterRebirth, oldProf / 10, prof/10, metempsychosis)?.Magics;

            if (removeSkills != null)
            {
                foreach (var skill in removeSkills)
                {
                    await MagicData.UnlearnMagicAsync(skill, true);
                }
            }

            if (resetSkills != null)
            {
                foreach (var skill in resetSkills)
                {
                    await MagicData.ResetMagicAsync(skill);
                }
            }

            if (learnSkills != null)
            {
                foreach (var skill in learnSkills)
                {
                    await MagicData.CreateAsync(skill, 0);
                }
            }

            if (UserPackage[Item.ItemPosition.LeftHand]?.IsArrowSort() == false)
                await UserPackage.UnEquipAsync(Item.ItemPosition.LeftHand);

            if (UserPackage[Item.ItemPosition.RightHand]?.IsBow() == true && ProfessionSort != 4)
                await UserPackage.UnEquipAsync(Item.ItemPosition.RightHand);

            return true;
        }

        public async Task ResetUserAttributesAsync(byte mete, ushort newProf, ushort newLook, int newLev)
        {
            if (newProf == 0) newProf = (ushort) (Profession / 10 * 10 + 1);
            byte prof = (byte) (newProf > 100 ? 10 : newProf / 10);

            int force = 0, speed = 0, health = 0, soul = 0;
            DbPointAllot pointAllot = Kernel.RoleManager.GetPointAllot(prof, 1);
            if (pointAllot != null)
            {
                force = pointAllot.Strength;
                speed = pointAllot.Agility;
                health = pointAllot.Vitality;
                soul = pointAllot.Spirit;
            }
            else if (prof == 1)
            {
                force = 5;
                speed = 2;
                health = 3;
                soul = 0;
            }
            else if (prof == 2)
            {
                force = 5;
                speed = 2;
                health = 3;
                soul = 0;
            }
            else if (prof == 4)
            {
                force = 2;
                speed = 7;
                health = 1;
                soul = 0;
            }
            else if (prof == 10)
            {
                force = 0;
                speed = 2;
                health = 3;
                soul = 5;
            }
            else
            {
                force = 5;
                speed = 2;
                health = 3;
                soul = 0;
            }

            AutoAllot = false;

            int newAttrib = (GetRebirthAddPoint(Profession, Level, mete) + (newLev * 3));
            await SetAttributesAsync(ClientUpdateType.Atributes, (ulong) newAttrib);
            await SetAttributesAsync(ClientUpdateType.Strength, (ulong)force);
            await SetAttributesAsync(ClientUpdateType.Agility, (ulong)speed);
            await SetAttributesAsync(ClientUpdateType.Vitality, (ulong)health);
            await SetAttributesAsync(ClientUpdateType.Spirit, (ulong)soul);
            await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
            await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
            await SetAttributesAsync(ClientUpdateType.Stamina, MaxEnergy);
            await SetAttributesAsync(ClientUpdateType.XpCircle, 0);

            if (newLook > 0 && newLook != Mesh % 10)
                await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);

            await SetAttributesAsync(ClientUpdateType.Level, (ulong)newLev);
            await SetAttributesAsync(ClientUpdateType.Experience, 0);

            if (mete == 0)
            {
                FirstProfession = Profession;
                mete++;
            }
            else if (mete == 1)
            {
                PreviousProfession = Profession;
                mete++;
            }
            else
            {
                FirstProfession = PreviousProfession;
                PreviousProfession = Profession;
            }


            await SetAttributesAsync(ClientUpdateType.Class, newProf);
            await SetAttributesAsync(ClientUpdateType.Reborn, mete);
            await SaveAsync();
        }

        public int GetRebirthAddPoint(int oldProf, int oldLev, int metempsychosis)
        {
            int points = 0;

            if (metempsychosis == 0)
            {
                if (oldProf == HIGHEST_WATER_WIZARD_PROF)
                {
                    points += Math.Min((1 + (oldLev - 110) / 2) * ((oldLev - 110) / 2) / 2, 55);
                }
                else
                {
                    points += Math.Min((1 + (oldLev - 120)) * (oldLev - 120) / 2, 55);
                }
            }
            else
            {
                if (oldProf == HIGHEST_WATER_WIZARD_PROF)
                    points += 52 + Math.Min((1 + (oldLev - 110) / 2) * ((oldLev - 110) / 2) / 2, 55);
                else
                    points += 52 + Math.Min((1 + (oldLev - 120)) * (oldLev - 120) / 2, 55);
            }

            return points;
        }

        public async Task<bool> UnlearnAllSkillAsync()
        {
            return await WeaponSkill.UnearnAllAsync();
        }

        #endregion

        #region Bonus

        public async Task<bool> DoBonusAsync()
        {
            if (!UserPackage.IsPackSpare(10))
            {
                await SendAsync(string.Format(Language.StrNotEnoughSpaceN, 10));
                return false;
            }

            DbBonus bonus = await BonusRepository.GetAsync(m_dbObject.AccountIdentity);
            if (bonus == null || bonus.Flag != 0 || bonus.Time != null)
            {
                await SendAsync(Language.StrNoBonus);
                return false;
            }

            bonus.Flag = 1;
            bonus.Time = DateTime.Now;
            await BaseRepository.SaveAsync(bonus);
            if (!await GameAction.ExecuteActionAsync(bonus.Action, this, null, null, ""))
            {
                await Log.GmLogAsync("bonus_error", $"{bonus.Identity},{bonus.AccountIdentity},{Identity},{bonus.Action}");
                return false;
            }

            await Log.GmLogAsync("bonus", $"{bonus.Identity},{bonus.AccountIdentity},{Identity},{bonus.Action}");
            return true;
        }

        public async Task<int> BonusCountAsync()
        {
            return await BonusRepository.CountAsync(m_dbObject.AccountIdentity);
        }

        public async Task<bool> DoCardsAsync()
        {
            var cards = await DbCard.GetAsync(m_dbObject.AccountIdentity);
            if (cards.Count == 0)
                return false;

            int inventorySpace = cards.Count(x => x.ItemType != 0);
            if (inventorySpace > 0 && !UserPackage.IsPackSpare(inventorySpace))
            {
                await SendAsync(string.Format(Language.StrNotEnoughSpaceN, inventorySpace));
                return false;
            }

            int money = 0;
            int emoney = 0;
            int emoneyMono = 0;
            foreach (var card in cards)
            {
                if (card.ItemType != 0)
                    await UserPackage.AwardItemAsync(card.ItemType);

                if (card.Money != 0)
                    money += (int) card.Money;

                if (card.ConquerPoints != 0)
                    emoney += (int) card.ConquerPoints;

                if (card.ConquerPointsMono != 0)
                    emoneyMono += (int) card.ConquerPointsMono;

                card.Flag |= 0x1;
                card.Timestamp = DateTime.Now;
            }

            await BaseRepository.SaveAsync(cards);

            if (money > 0)
                await AwardMoneyAsync(money);

            if (emoney > 0)
                await AwardConquerPointsAsync(emoney);
            return true;
        }

        public Task<int> CardsCountAsync()
        {
            return DbCard.CountAsync(m_dbObject.AccountIdentity);
        }

        #endregion

        #region Monster Kills

        private ConcurrentDictionary<uint, DbMonsterKill> m_monsterKills = new ConcurrentDictionary<uint, DbMonsterKill>();

        public async Task LoadMonsterKillsAsync()
        {
            m_monsterKills = new ConcurrentDictionary<uint, DbMonsterKill>((await DbMonsterKill.GetAsync(Identity)).ToDictionary(x => x.Monster));
        }

        public Task KillMonsterAsync(uint type)
        {
            if (!m_monsterKills.TryGetValue(type, out var value))
            {
                m_monsterKills.TryAdd(type, value = new DbMonsterKill
                {
                    CreatedAt = DateTime.Now,
                    UserIdentity = Identity,
                    Monster = type
                });
            }

            value.Amount += 1;
            return Task.CompletedTask;
        }

        #endregion

        #region Statistic

        public UserStatistic Statistic { get; }

        public long Iterator = -1;
        public long[] VarData = new long[MAX_VAR_AMOUNT];
        public string[] VarString = new string[MAX_VAR_AMOUNT];

        #endregion

        #region Task Detail

        public TaskDetail TaskDetail { get; }

        #endregion

        #region Game Action

        private List<uint> m_setTaskId = new List<uint>();

        public uint InteractingItem { get; set; }
        public uint InteractingNpc { get; set; }
        
        public bool CheckItem(DbTask task)
        {
            if (task.Itemname1.Length > 0)
            {
                if (UserPackage[task.Itemname1] == null)
                    return false;

                if (task.Itemname2.Length > 0)
                {
                    if (UserPackage[task.Itemname2] == null)
                        return false;
                }
            }

            return true;
        }

        public void CancelInteraction()
        {
            m_setTaskId.Clear();
            InteractingItem = 0;
            InteractingNpc = 0;
        }

        public byte PushTaskId(uint idTask)
        {
            if (idTask != 0 && m_setTaskId.Count < MAX_MENUTASKSIZE)
            {
                m_setTaskId.Add(idTask);
                return (byte)m_setTaskId.Count;
            }

            return 0;
        }

        public void ClearTaskId()
        {
            m_setTaskId.Clear();
        }
        public uint GetTaskId(int idx)
        {
            return idx > 0 && idx <= m_setTaskId.Count ? m_setTaskId[idx - 1] : 0u;
        }

        public async Task<bool> TestTaskAsync(DbTask task)
        {
            if (task == null) return false;

            try
            {
                if (!CheckItem(task))
                    return false;

                if (Silvers < task.Money)
                    return false;

                if (task.Profession != 0 && Profession != task.Profession)
                    return false;

                if (task.Sex != 0 && task.Sex != 999 && task.Sex != Gender)
                    return false;

                if (PkPoints < task.MinPk || PkPoints > task.MaxPk)
                    return false;

                if (task.Marriage >= 0)
                {
                    if (task.Marriage == 0 && MateIdentity != 0)
                        return false;
                    if (task.Marriage == 1 && MateIdentity == 0)
                        return false;
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Test task error");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
            return true;
        }

        public async Task AddTaskMaskAsync(int idx)
        {
            if (idx < 0 || idx >= 32)
                return;

            m_dbObject.TaskMask |= (1u << idx);
            await SaveAsync();
        }

        public async Task ClearTaskMaskAsync(int idx)
        {
            if (idx < 0 || idx >= 32)
                return;

            m_dbObject.TaskMask &= ~(1u << idx);
            await SaveAsync();
        }

        public bool CheckTaskMask(int idx)
        {
            if (idx < 0 || idx >= 32)
                return false;
            return (m_dbObject.TaskMask & (1u << idx)) != 0;
        }

        #endregion

        #region Home

        public uint HomeIdentity
        {
            get => m_dbObject?.HomeIdentity ?? 0u;
            set => m_dbObject.HomeIdentity = value;
        }

        #endregion

        #region Marriage

        public bool IsMate(Character user)
        {
            return user.Identity == MateIdentity;
        }

        public bool IsMate(uint idMate)
        {
            return idMate == MateIdentity;
        }

        #endregion

        #region Requests

        public void SetRequest(RequestType type, uint target)
        {
            m_dicRequests.TryRemove(type, out _);
            if (target == 0)
                return;

            m_dicRequests.TryAdd(type, target);
        }

        public uint QueryRequest(RequestType type)
        {
            return m_dicRequests.TryGetValue(type, out var value) ? value : 0;
        }

        public uint PopRequest(RequestType type)
        {
            return m_dicRequests.TryRemove(type, out var value) ? value : 0;
        }

        #endregion

        #region Friend

        private ConcurrentDictionary<uint, Friend> m_dicFriends = new ConcurrentDictionary<uint, Friend>();

        public int FriendAmount => m_dicFriends.Count;
        
        public int MaxFriendAmount => 50;

        public bool AddFriend(Friend friend)
        {
            return m_dicFriends.TryAdd(friend.Identity, friend);
        }

        public async Task<bool> CreateFriendAsync(Character target)
        {
            if (IsFriend(target.Identity))
                return false;

            Friend friend = new Friend(this);
            if (!await friend.CreateAsync(target))
                return false;

            Friend targetFriend = new Friend(target);
            if (!await targetFriend.CreateAsync(this))
                return false;

            await friend.SaveAsync();
            await targetFriend.SaveAsync();
            await friend.SendAsync();
            await targetFriend.SendAsync();

            AddFriend(friend);
            target.AddFriend(targetFriend);

            await BroadcastRoomMsgAsync(string.Format(Language.StrMakeFriend, Name, target.Name));
            return true;
        }

        public bool IsFriend(uint idTarget)
        {
            return m_dicFriends.ContainsKey(idTarget);
        }

        public Friend GetFriend(uint idTarget)
        {
            return m_dicFriends.TryGetValue(idTarget, out var friend) ? friend : null;
        }

        public async Task<bool> DeleteFriendAsync(uint idTarget, bool notify = false)
        {
            if (!IsFriend(idTarget) || !m_dicFriends.TryRemove(idTarget, out var target))
                return false;
            
            if (target.Online)
            {
                await target.User.DeleteFriendAsync(Identity);
            }
            else
            {
                DbFriend targetFriend = await FriendRepository.GetAsync(Identity, idTarget);
                await using ServerDbContext ctx = new ServerDbContext();
                ctx.Remove(targetFriend);
                await ctx.SaveChangesAsync();
            }

            await target.DeleteAsync();

            await SendAsync(new MsgFriend
            {
                Identity = target.Identity,
                Name = target.Name,
                Action = MsgFriend.MsgFriendAction.RemoveFriend,
                Online = target.Online
            });

            if (notify)
                await BroadcastRoomMsgAsync(string.Format(Language.StrBreakFriend, Name, target.Name));
            return true;
        }

        public async Task SendAllFriendAsync()
        {
            foreach (var friend in m_dicFriends.Values)
            {
                await friend.SendAsync();
                if (friend.Online)
                {
                    await friend.User.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend.MsgFriendAction.SetOnlineFriend,
                        Online = true
                    });
                }
            }
        }

        public async Task NotifyOfflineFriendAsync()
        {
            foreach (var friend in m_dicFriends.Values)
            {
                if (friend.Online)
                {
                    await friend.User.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend.MsgFriendAction.SetOfflineFriend,
                        Online = true
                    });
                }
            }
        }

        public async Task SendToFriendsAsync(IPacket msg)
        {
            foreach (var friend in m_dicFriends.Values.Where(x => x.Online))
                await friend.User.SendAsync(msg);
        }

        #endregion

        #region Enemy

        private ConcurrentDictionary<uint, Enemy> m_dicEnemies = new ConcurrentDictionary<uint, Enemy>();

        public bool AddEnemy(Enemy friend)
        {
            return m_dicEnemies.TryAdd(friend.Identity, friend);
        }

        public async Task<bool> CreateEnemyAsync(Character target)
        {
            if (IsEnemy(target.Identity))
                return false;

            Enemy enemy = new Enemy(this);
            if (!await enemy.CreateAsync(target))
                return false;

            await enemy.SaveAsync();
            await enemy.SendAsync();
            AddEnemy(enemy);
            return true;
        }

        public bool IsEnemy(uint idTarget)
        {
            return m_dicEnemies.ContainsKey(idTarget);
        }

        public Enemy GetEnemy(uint idTarget)
        {
            return m_dicEnemies.TryGetValue(idTarget, out var friend) ? friend : null;
        }

        public async Task<bool> DeleteEnemyAsync(uint idTarget)
        {
            if (!IsFriend(idTarget) || !m_dicEnemies.TryRemove(idTarget, out var target))
                return false;

            await target.DeleteAsync();

            await SendAsync(new MsgFriend
            {
                Identity = target.Identity,
                Name = target.Name,
                Action = MsgFriend.MsgFriendAction.RemoveEnemy,
                Online = true
            });
            return true;
        }

        public async Task SendAllEnemiesAsync()
        {
            foreach (var enemy in m_dicEnemies.Values)
            {
                await enemy.SendAsync();
            }

            foreach (var enemy in await EnemyRepository.GetOwnEnemyAsync(Identity))
            {
                Character user = Kernel.RoleManager.GetUser(enemy.UserIdentity);
                if (user != null)
                    await user.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend.MsgFriendAction.SetOnlineEnemy,
                        Online = true
                    });
            }
        }

        #endregion

        #region Trade Partner

        private ConcurrentDictionary<uint, TradePartner> m_tradePartners = new ConcurrentDictionary<uint, TradePartner>();

        public void AddTradePartner(TradePartner partner)
        {
            m_tradePartners.TryAdd(partner.Identity, partner);
        }

        public void RemoveTradePartner(uint idTarget)
        {
            if (m_tradePartners.ContainsKey(idTarget))
                m_tradePartners.TryRemove(idTarget, out _);
        }

        public async Task<bool> CreateTradePartnerAsync(Character target)
        {
            if (IsTradePartner(target.Identity) || target.IsTradePartner(Identity))
            {
                await SendAsync(Language.StrTradeBuddyAlreadyAdded);
                return false;
            }

            DbBusiness business = new DbBusiness
            {
                User = GetDatabaseObject(),
                Business = target.GetDatabaseObject(),
                Date = DateTime.Now.AddDays(3)
            };

            if (!await BaseRepository.SaveAsync(business))
            {
                await SendAsync(Language.StrTradeBuddySomethingWrong);
                return false;
            }

            TradePartner me;
            TradePartner targetTp;
            AddTradePartner(me = new TradePartner(this, business));
            target.AddTradePartner(targetTp = new TradePartner(target, business));

            await me.SendAsync();
            await targetTp.SendAsync();

            await BroadcastRoomMsgAsync(string.Format(Language.StrTradeBuddyAnnouncePartnership, Name, target.Name));
            return true;
        }

        public async Task<bool> DeleteTradePartnerAsync(uint idTarget)
        {
            if (!IsTradePartner(idTarget))
                return false;

            TradePartner partner = GetTradePartner(idTarget);
            if (partner == null)
                return false;

            await partner.SendRemoveAsync();
            RemoveTradePartner(idTarget);
            await SendAsync(string.Format(Language.StrTradeBuddyBrokePartnership1, partner.Name));

            var delete = partner.DeleteAsync();
            Character target = Kernel.RoleManager.GetUser(idTarget);
            if (target != null)
            {
                partner = target.GetTradePartner(Identity);
                if (partner != null)
                {
                    await partner.SendRemoveAsync();
                    target.RemoveTradePartner(Identity);
                }

                await target.SendAsync(string.Format(Language.StrTradeBuddyBrokePartnership0, Name));
            }

            await delete;
            return true;
        }

        public async Task LoadTradePartnerAsync()
        {
            var tps = await DbBusiness.GetAsync(Identity);
            foreach (var tp in tps)
            {
                var db = new TradePartner(this, tp);
                AddTradePartner(db);
                await db.SendAsync();
            }
        }

        public TradePartner GetTradePartner(uint target)
        {
            return m_tradePartners.TryGetValue(target, out var result) ? result : null;
        }

        public bool IsTradePartner(uint target)
        {
            return m_tradePartners.ContainsKey(target);
        }

        public bool IsValidTradePartner(uint target)
        {
            return m_tradePartners.ContainsKey(target) && m_tradePartners[target].IsValid();
        }

        #endregion

        #region Syndicate

        public Syndicate Syndicate { get; set; }
        public SyndicateMember SyndicateMember => Syndicate?.QueryMember(Identity);
        public ushort SyndicateIdentity => Syndicate?.Identity ?? 0;
        public string SyndicateName => Syndicate?.Name ?? Language.StrNone;
        public SyndicateMember.SyndicateRank SyndicateRank => SyndicateMember?.Rank ?? SyndicateMember.SyndicateRank.None;
        public string SyndicateRankName => SyndicateMember?.RankName ?? Language.StrNone;

        public async Task<bool> CreateSyndicateAsync(string name, int price = 1000000)
        {
            if (Syndicate != null)
            {
                await SendAsync(Language.StrSynAlreadyJoined);
                return false;
            }

            if (name.Length > 15)
            {
                return false;
            }

            if (!Kernel.IsValidName(name))
                return false;

            if (Kernel.SyndicateManager.GetSyndicate(name) != null)
            {
                await SendAsync(Language.StrSynNameInUse);
                return false;
            }

            if (!await SpendMoneyAsync(price))
            {
                await SendAsync(Language.StrNotEnoughMoney);
                return false;
            }

            Syndicate = new Syndicate();
            if (!await Syndicate.CreateAsync(name, price, this))
            {
                Syndicate = null;
                await AwardMoneyAsync(price);
                return false;
            }

            if (!Kernel.SyndicateManager.AddSyndicate(Syndicate))
            {
                await Syndicate.DeleteAsync();
                Syndicate = null;
                await AwardMoneyAsync(price);
                return false;
            }
            
            await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrSynCreate, Name, name), MsgTalk.TalkChannel.Talk, Color.White);
            await SendSyndicateAsync();
            await Screen.SynchroScreenAsync();
            await Syndicate.BroadcastNameAsync();
            return true;
        }

        public async Task<bool> DisbandSyndicateAsync()
        {
            if (SyndicateIdentity == 0)
                return false;

            if (Syndicate.Leader.UserIdentity != Identity)
                return false;

            if (Syndicate.MemberCount > 1)
            {
                await SendAsync(Language.StrSynNoDisband);
                return false;
            }
            
            return await Syndicate.DisbandAsync(this);
        }

        public async Task SendSyndicateAsync()
        {
            if (Syndicate != null)
            {
                await SendAsync(new MsgSyndicateAttributeInfo
                {
                    Identity = SyndicateIdentity,
                    Rank = SyndicateRank,
                    MemberAmount = Syndicate.MemberCount,
                    Funds = Syndicate.Money,
                    PlayerDonation = SyndicateMember.Silvers,
                    LeaderName = Syndicate.Leader.UserName,
                    ConditionLevel = Syndicate.LevelRequirement,
                    ConditionMetempsychosis = Syndicate.MetempsychosisRequirement,
                    ConditionProfession = (int) Syndicate.ProfessionRequirement,
                    ConquerPointsFunds = Syndicate.ConquerPoints,
                    PositionExpiration = uint.Parse(SyndicateMember.PositionExpiration?.ToString("yyyyMMdd") ?? "0"),
                    EnrollmentDate = uint.Parse(SyndicateMember.JoinDate.ToString("yyyyMMdd")),
                    Level = Syndicate.Level
                });
                await SendAsync(new MsgSyndicate
                {
                    Mode = MsgSyndicate.SyndicateRequest.Bulletin,
                    Strings = new List<string>{ Syndicate.Announce },
                    Identity = uint.Parse(Syndicate.AnnounceDate.ToString("yyyyMMdd"))
                });
                await Syndicate.SendAsync(this);
                await SendAsync(new MsgSynpOffer(SyndicateMember));
                await SynchroAttributesAsync(ClientUpdateType.TotemPoleBattlePower, (ulong) Syndicate.TotemSharedBattlePower, true);
            }
            else
            {
                await SendAsync(new MsgSyndicateAttributeInfo
                {
                    Rank = SyndicateMember.SyndicateRank.None
                });
            }
        }

        #endregion

        #region User Secondary Password

        public ulong SecondaryPassword
        {
            get => m_dbObject.LockKey;
            set => m_dbObject.LockKey = value;
        }

        public bool IsUnlocked()
        {
            return SecondaryPassword == 0 || VarData[0] != 0;
        }

        public void UnlockSecondaryPassword()
        {
            VarData[0] = 1;
        }

        public bool CanUnlock2ndPassword()
        {
            return VarData[1] <= 2;
        }

        public void Increment2ndPasswordAttempts()
        {
            VarData[1] += 1;
        }

        public async Task SendSecondaryPasswordInterfaceAsync()
        {
            await GameAction.ExecuteActionAsync(100, this, null, null, string.Empty);
        }

        #endregion

        #region Administration

        public bool IsPm()
        {
            return Name.Contains("[PM]");
        }

        public bool IsGm()
        {
            return IsPm() || Name.Contains("[GM]");
        }

        #endregion

        #region Events

        public GameEvent CurrentEvent { get; private set; }

        public async Task<bool> SignInEventAsync(GameEvent e)
        {
            if (!e.IsAllowedToJoin(this))
            {
                return false;
            }

            CurrentEvent = e;
            await e.OnEnterAsync(this);
            return true;
        }

        public async Task<bool> SignOutEventAsync()
        {
            if (CurrentEvent != null)
                await CurrentEvent.OnExitAsync(this);

            CurrentEvent = null;
            return true;
        }

        #endregion

        #region Screen

        public Screen Screen { get; }

        public async Task BroadcastRoomMsgAsync(string message, MsgTalk.TalkChannel channel = MsgTalk.TalkChannel.TopLeft, Color? color = null, bool self = true)
        {
            await BroadcastRoomMsgAsync(new MsgTalk(Identity, channel, color ?? Color.Red, message), self);
        }

        public override async Task BroadcastRoomMsgAsync(IPacket msg, bool self)
        {
            await Screen.BroadcastRoomMsgAsync(msg, self);
        }

        #endregion

        #region Map and Position

        public override GameMap Map { get; protected set; }

        /// <summary>
        /// The current map identity for the role.
        /// </summary>
        public override uint MapIdentity
        {
            get => m_idMap;
            set => m_idMap = value;
        }
        /// <summary>
        /// Current X position of the user in the map.
        /// </summary>
        public override ushort MapX
        {
            get => m_posX;
            set => m_posX = value;
        }
        /// <summary>
        /// Current Y position of the user in the map.
        /// </summary>
        public override ushort MapY
        {
            get => m_posY;
            set => m_posY = value;
        }

        public uint RecordMapIdentity
        {
            get => m_dbObject.MapID;
            set => m_dbObject.MapID = value;
        }

        public ushort RecordMapX
        {
            get => m_dbObject.X;
            set => m_dbObject.X = value;
        }

        public ushort RecordMapY
        {
            get => m_dbObject.Y;
            set => m_dbObject.Y = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public override async Task EnterMapAsync()
        {
            Map = Kernel.MapManager.GetMap(m_idMap);
            if (Map != null)
            {
                await Map.AddAsync(this);
                await Map.SendMapInfoAsync(this);
                await Screen.SynchroScreenAsync();

                m_respawn.Startup(10);

                if (Map.IsTeamDisable() && Team != null)
                {
                    if (Team.Leader.Identity == Identity)
                        await Team.DismissAsync(this);
                    else await Team.DismissMemberAsync(this);
                }

                if (CurrentEvent == null)
                {
                    GameEvent @event = Kernel.EventThread.GetEvent(m_idMap);
                    if (@event != null)
                        await SignInEventAsync(@event);
                }

                if (Team != null)
                    await Team.SyncFamilyBattlePowerAsync();
            }
            else
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Invalid map {m_idMap} for user {Identity} {Name}");
                m_socket?.Disconnect();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override async Task LeaveMapAsync()
        {
            BattleSystem.ResetBattle();
            await MagicData.AbortMagicAsync(false);
            StopMining();

            if (Map != null)
            {
                await Map.RemoveAsync(Identity);

                if (CurrentEvent != null && CurrentEvent.Map.Identity != 0 && CurrentEvent.Map.Identity == Map.Identity)
                {
                    await SignOutEventAsync();
                }
            }

            if (Team != null)
                await Team.SyncFamilyBattlePowerAsync();

            await Screen.ClearAsync();
        }

        public override async Task ProcessOnMoveAsync()
        {
            StopMining();

            if (CurrentEvent != null)
                await CurrentEvent.OnMoveAsync(this);

            if (QueryStatus(StatusSet.LUCKY_DIFFUSE) != null)
            {
                foreach (var user in Screen.Roles.Values.Where(x => x.IsPlayer() && x.QueryStatus(StatusSet.LUCKY_ABSORB)?.CasterId == Identity).Cast<Character>())
                {
                    await user.DetachStatusAsync(StatusSet.LUCKY_DIFFUSE);
                }
            }

            m_luckyAbsorbStart.Clear();
            m_idLuckyTarget = 0;

            m_respawn.Clear();

            await base.ProcessOnMoveAsync();
        }

        public override Task ProcessAfterMoveAsync()
        {
            return base.ProcessAfterMoveAsync();
        }

        public override async Task ProcessOnAttackAsync()
        {
            StopMining();

            if (CurrentEvent != null)
                await CurrentEvent.OnAttackAsync(this);

            m_respawn.Clear();

            await base.ProcessOnAttackAsync();
        }

        public async Task SavePositionAsync()
        {
            if (!Map.IsRecordDisable())
            {
                m_dbObject.X = m_posX;
                m_dbObject.Y = m_posY;
                m_dbObject.MapID = m_idMap;
                await SaveAsync();
            }
        }

        public async Task SavePositionAsync(uint idMap, ushort x, ushort y)
        {
            GameMap map = Kernel.MapManager.GetMap(idMap);
            if (map?.IsRecordDisable() == false)
            {
                m_dbObject.X = x;
                m_dbObject.Y = y;
                m_dbObject.MapID = idMap;
                await SaveAsync();
            }
        }

        public async Task<bool> FlyMapAsync(uint idMap, int x, int y)
        {
            if (Map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"FlyMap user not in map");
                return false;
            }

            if (idMap == 0)
                idMap = MapIdentity;

            GameMap newMap = Kernel.MapManager.GetMap(idMap);
            if (newMap == null || !newMap.IsValidPoint(x, y))
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"FlyMap user fly invalid position {idMap}[{x},{y}]");
                return false;
            }

            try
            {
                await LeaveMapAsync();

                m_idMap = newMap.Identity;
                MapX = (ushort) x;
                MapY = (ushort) y;

                await SendAsync(new MsgAction
                {
                    Identity = Identity,
                    Command = newMap.MapDoc,
                    X = MapX,
                    Y = MapY,
                    Action = MsgAction.ActionType.MapTeleport,
                    Direction = (ushort) Direction
                });

                await EnterMapAsync();
            }
            catch
            {
                await Log.WriteLogAsync(LogLevel.Error, "FlyMap error");
            }

            return true;
        }

        public Role QueryRole(uint idRole)
        {
            return Map.QueryAroundRole(this, idRole);
        }

        #endregion

        #region Movement

        public async Task<bool> SynPositionAsync(ushort x, ushort y, int nMaxDislocation)
        {
            if (nMaxDislocation <= 0 || x == 0 && y == 0) // ignore in this condition
                return true;

            int nDislocation = GetDistance(x, y);
            if (nDislocation >= nMaxDislocation)
                return false;

            if (nDislocation <= 0)
                return true;

            if (IsGm())
                await SendAsync($"syn move: ({MapX},{MapY})->({x},{y})", MsgTalk.TalkChannel.Talk, Color.Red);

            if (!Map.IsValidPoint(x, y))
                return false;

            await ProcessOnMoveAsync();
            await JumpPosAsync(x, y);
            await Screen.BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = Identity,
                Action = MsgAction.ActionType.Kickback,
                X = x,
                Y = y,
                Command = (uint)((y << 16) | x),
                Direction = (ushort)Direction,
            });
            
            return true;
        }

        public Task KickbackAsync()
        {
            return SendAsync(new MsgAction
            {
                Identity = Identity,
                X = MapX,
                Y = MapY,
                Command = (uint) ((MapY << 16) | MapX),
                Direction = (ushort)Direction,
                Action = MsgAction.ActionType.Kickback
            });
        }

        #endregion

        #region Jar

        public async Task AddJarKillsAsync(int stcType)
        {
            Item jar = UserPackage.GetItemByType(Item.TYPE_JAR);
            if (jar != null)
            {
                if (jar.MaximumDurability == stcType)
                {
                    jar.Data += 1;
                    await jar.SaveAsync();

                    if (jar.Data % 50 == 0)
                    {
                        await jar.SendJarAsync();
                    }
                }
            }
        }

        #endregion

        #region Offline TG

        public ushort MaxTrainingMinutes => (ushort)Math.Min(1440 + 60 * VipLevel, (m_dbObject.HeavenBlessing.Value - DateTime.Now).TotalMinutes);

        public ushort CurrentTrainingMinutes => //600;
            (ushort)Math.Min(((DateTime.Now - m_dbObject.LoginTime).TotalMinutes) * 10, MaxTrainingMinutes);

        //public ushort CurrentOfflineTrainingTime => (ushort)(m_dbObject.AutoExercise == 0 || m_dbObject.LogoutTime2 == null
        //    ? 0
        //    : Math.Min((MaxTrainingMinutes - (m_dbObject.LogoutTime2.Value.AddMinutes(m_dbObject.AutoExercise) - DateTime.Now).TotalMinutes), MaxTrainingMinutes));

        public ushort CurrentOfflineTrainingTime
        {
            get
            {
                if (m_dbObject.AutoExercise == 0 || m_dbObject.LogoutTime2 == null)
                    return 0;

                DateTime endTime = m_dbObject.LogoutTime2.Value.AddMinutes(m_dbObject.AutoExercise);
                if (endTime < DateTime.Now)
                    return CurrentTrainingTime;

                int remainingTime = (int)Math.Min((DateTime.Now - m_dbObject.LogoutTime2.Value).TotalMinutes, CurrentTrainingTime);
                return (ushort)(remainingTime);
            }
        }

        public ushort CurrentTrainingTime => m_dbObject.AutoExercise;

        public bool IsOfflineTraining => m_dbObject.AutoExercise != 0;

        public async Task EnterAutoExerciseAsync()
        {
            if (!IsBlessed)
                return;

            m_dbObject.AutoExercise = CurrentTrainingMinutes;
            m_dbObject.LogoutTime2 = DateTime.Now;
        }

        public async Task LeaveAutoExerciseAsync()
        {
            await AwardExperienceAsync(CalculateExpBall(GetAutoExerciseExpTimes()), true);

            int totalMinutes = Math.Min(CurrentTrainingTime, CurrentOfflineTrainingTime);

            const int moneyPerMinute = 100;
            const double conquerPointsChance = 0.0125;

            await AwardMoneyAsync(moneyPerMinute * totalMinutes);

            int emoneyAmount = 0;
            for (int i = 0; i < totalMinutes; i++)
            {
                if (await Kernel.ChanceCalcAsync(conquerPointsChance))
                    emoneyAmount += await Kernel.NextAsync(1, 3);
            }

            if (emoneyAmount > 0)
                await AwardConquerPointsAsync(emoneyAmount);

            await FlyMapAsync(RecordMapIdentity, RecordMapX, RecordMapY);

            m_dbObject.AutoExercise = 0;
            m_dbObject.LogoutTime2 = null;
            await SaveAsync();
        }

        public int GetAutoExerciseExpTimes()
        {
            const int MAX_REWARD = 3000; // 5 Exp Balls every 8 hours
            const double REWARD_EVERY_N_MINUTES = 480;
            return (int)(Math.Min(CurrentOfflineTrainingTime, CurrentTrainingTime) / REWARD_EVERY_N_MINUTES * MAX_REWARD);
        }

        public (int Level, ulong Experience) GetCurrentOnlineTGExp()
        {
            
            return PreviewExpBallUsage(GetAutoExerciseExpTimes());
        }

        #endregion

        #region Multiple Exp

        public bool HasMultipleExp => m_dbObject.ExperienceMultiplier > 1 && m_dbObject.ExperienceExpires >= DateTime.Now;

        public float ExperienceMultiplier => !HasMultipleExp || m_dbObject.ExperienceMultiplier <= 0 ? 1f : m_dbObject.ExperienceMultiplier;

        public async Task SendMultipleExpAsync()
        {
            if (RemainingExperienceSeconds > 0)
                await SynchroAttributesAsync(ClientUpdateType.DoubleExpTimer, RemainingExperienceSeconds, false);
        }

        public uint RemainingExperienceSeconds
        {
            get
            {
                DateTime now = DateTime.Now;
                if (m_dbObject.ExperienceExpires < now)
                {
                    m_dbObject.ExperienceMultiplier = 1;
                    m_dbObject.ExperienceExpires = null;
                    return 0;
                }

                return (uint)((m_dbObject.ExperienceExpires - now)?.TotalSeconds ?? 0);
            }
        }

        public async Task<bool> SetExperienceMultiplierAsync(uint nSeconds, float nMultiplier = 2f)
        {
            m_dbObject.ExperienceExpires = DateTime.Now.AddSeconds(nSeconds);
            m_dbObject.ExperienceMultiplier = nMultiplier;
            await SendMultipleExpAsync();
            return true;
        }

        #endregion

        #region Heaven Blessing

        public async Task SendBlessAsync()
        {
            if (IsBlessed)
            {
                DateTime now = DateTime.Now;
                await SynchroAttributesAsync(ClientUpdateType.HeavensBlessing, (uint)(HeavenBlessingExpires - now).TotalSeconds);

                if (Map != null && !Map.IsTrainingMap())
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 0);
                else
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 1);

                await AttachStatusAsync(this, StatusSet.HEAVEN_BLESS, 0, (int)(HeavenBlessingExpires - now).TotalSeconds, 0, 0);
            }
        }

        /// <summary>
        /// This method will update the user blessing time.
        /// </summary>
        /// <param name="amount">The amount of minutes to be added.</param>
        /// <returns>If the heaven blessing has been added successfully.</returns>
        public async Task<bool> AddBlessingAsync(uint amount)
        {
            DateTime now = DateTime.Now;
            if (m_dbObject.HeavenBlessing != null && m_dbObject.HeavenBlessing > now)
                m_dbObject.HeavenBlessing = m_dbObject.HeavenBlessing.Value.AddHours(amount);
            else
                m_dbObject.HeavenBlessing = now.AddHours(amount);

            if (Guide != null)
                await Guide.AwardTutorGodTimeAsync((ushort) (amount / 10));

            await SendBlessAsync();
            return true;
        }

        public DateTime HeavenBlessingExpires => m_dbObject.HeavenBlessing ?? DateTime.MinValue;

        public bool IsBlessed => m_dbObject.HeavenBlessing > DateTime.Now;

        #endregion

        #region Lucky

        public Task ChangeLuckyTimerAsync(int value)
        {
            ulong ms = 0;

            m_luckyTimeCount += value;
            if (m_luckyTimeCount > 0)
                m_dbObject.LuckyTime = DateTime.Now.AddSeconds(m_luckyTimeCount);

            if (IsLucky)
                ms = (ulong) (m_dbObject.LuckyTime.Value - DateTime.Now).TotalSeconds * 1000UL;

            return SynchroAttributesAsync(ClientUpdateType.LuckyTimeTimer, ms);
        }

        public bool IsLucky => m_dbObject.LuckyTime.HasValue && m_dbObject.LuckyTime.Value > DateTime.Now;

        public async Task SendLuckAsync()
        {
            if (IsLucky)
                await SynchroAttributesAsync(ClientUpdateType.LuckyTimeTimer, (ulong) (m_dbObject.LuckyTime.Value - DateTime.Now).TotalSeconds * 1000UL);
        }

        #endregion

        #region XP and Stamina

        public byte Energy { get; private set; } = DEFAULT_USER_ENERGY;

        public byte MaxEnergy => (byte) (IsBlessed ? 150 : 100);

        public byte XpPoints = 0;

        public async Task ProcXpValAsync()
        {
            if (!IsAlive)
            {
                await ClsXpValAsync();
                return;
            }

            IStatus pStatus = QueryStatus(StatusSet.START_XP);
            if (pStatus != null)
                return;

            if (XpPoints >= 100)
            {
                await BurstXpAsync();
                await SetXpAsync(0);
                m_xpPoints.Update();
            }
            else
            {
                if (Map != null && Map.IsBoothEnable())
                    return;
                await AddXpAsync(1);
            }
        }

        public async Task<bool> BurstXpAsync()
        {
            if (XpPoints < 100)
                return false;

            IStatus pStatus = QueryStatus(StatusSet.START_XP);
            if (pStatus != null)
                return true;

            await AttachStatusAsync(this, StatusSet.START_XP, 0, 20, 0, 0);
            return true;
        }

        public async Task SetXpAsync(byte nXp)
        {
            if (nXp > 100)
                return;
            await SetAttributesAsync(ClientUpdateType.XpCircle, nXp);
        }

        public async Task AddXpAsync(byte nXp)
        {
            if (nXp <= 0 || !IsAlive || QueryStatus(StatusSet.START_XP) != null)
                return;
            await AddAttributesAsync(ClientUpdateType.XpCircle, nXp);
        }

        public async Task ClsXpValAsync()
        {
            XpPoints = 0;
            await StatusSet.DelObjAsync(StatusSet.START_XP);
        }

        public async Task FinishXpAsync()
        {
            int currentPoints = Kernel.RoleManager.GetSupermanPoints(Identity);
            if (KoCount >= 25
                && currentPoints < KoCount)
            {
                await Kernel.RoleManager.AddOrUpdateSupermanAsync(Identity, KoCount);
                int rank = Kernel.RoleManager.GetSupermanRank(Identity);
                if (rank < 100)
                    await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrSupermanBroadcast, Name, KoCount, rank), MsgTalk.TalkChannel.Talk);
            }
            KoCount = 0;
        }

        #endregion

        #region Attributes Set and Add

        public override async Task<bool> AddAttributesAsync(ClientUpdateType type, long value)
        {
            bool screen = false;
            switch (type)
            {
                case ClientUpdateType.Level:
                    if (value < 0)
                        return false;

                    screen = true;
                    value = Level = (byte)Math.Max(1, Math.Min(MAX_UPLEV, Level + value));
                    break;

                case ClientUpdateType.Experience:
                    if (value < 0)
                    {
                        Experience = Math.Max(0, Experience - (ulong) (value * -1));
                    }
                    else
                    {
                        Experience += (ulong) value;
                    }

                    value = (long) Experience;
                    break;

                case ClientUpdateType.Strength:
                    if (value < 0)
                        return false;

                    value = Strength = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, Strength + value));
                    break;

                case ClientUpdateType.Agility:
                    if (value < 0)
                        return false;

                    value = Agility = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, Agility + value));
                    break;

                case ClientUpdateType.Vitality:
                    if (value < 0)
                        return false;

                    value = Vitality = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, Vitality + value));
                    break;

                case ClientUpdateType.Spirit:
                    if (value < 0)
                        return false;

                    value = Spirit = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, Spirit + value));
                    break;

                case ClientUpdateType.Atributes:
                    if (value < 0)
                        return false;

                    value = AttributePoints = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, AttributePoints + value));
                    break;

                case ClientUpdateType.XpCircle:
                    if (value < 0)
                    {
                        XpPoints = (byte)Math.Max(0, XpPoints - (value * -1));
                    }
                    else
                    {
                        XpPoints = (byte)Math.Max(0, XpPoints + value);
                    }

                    value = XpPoints;
                    break;

                case ClientUpdateType.Stamina:
                    if (value < 0)
                    {
                        Energy = (byte) Math.Max(0, Energy - (value * -1));
                    }
                    else
                    {
                        Energy = (byte)Math.Max(0, Math.Min(MaxEnergy, Energy + value));
                    }

                    value = Energy;
                    break;

                case ClientUpdateType.PkPoints:
                    value = PkPoints = (ushort) Math.Max(0, Math.Min(PkPoints + value, ushort.MaxValue));
                    await CheckPkStatusAsync();
                    break;

                case ClientUpdateType.Vigor:
                {
                    Vigor = Math.Max(0, Math.Min(MaxVigor, (int) value + Vigor));
                    await SendAsync(new MsgData
                    {
                        Action = MsgData.DataAction.SetMountMovePoint,
                        Year = Vigor
                    });
                    return true;
                }

                default:
                    bool result = await base.AddAttributesAsync(type, value);
                    return result && await SaveAsync();
            }

            await SaveAsync();
            await SynchroAttributesAsync(type, (ulong) value, screen);
            return true;
        }

        public override async Task<bool> SetAttributesAsync(ClientUpdateType type, ulong value)
        {
            bool screen = false;
            switch (type)
            {
                case ClientUpdateType.Level:
                    screen = true;
                    Level = (byte) Math.Max(1, Math.Min(MAX_UPLEV, value));
                    break;

                case ClientUpdateType.Experience:
                    Experience = Math.Max(0, value);
                    break;

                case ClientUpdateType.XpCircle:
                    XpPoints = (byte) Math.Max(0, Math.Min(value, 100));
                    break;

                case ClientUpdateType.Stamina:
                    Energy = (byte)Math.Max(0, Math.Min(value, MaxEnergy));
                    break;

                case ClientUpdateType.Atributes:
                    AttributePoints = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, value));
                    break;

                case ClientUpdateType.PkPoints:
                    PkPoints = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, value));
                    await CheckPkStatusAsync();
                    break;

                case ClientUpdateType.Mesh:
                    screen = true;
                    Mesh = (uint) value;
                    break;

                case ClientUpdateType.HairStyle:
                    screen = true;
                    Hairstyle = (ushort)value;
                    break;

                case ClientUpdateType.Strength:
                    value = Strength = (ushort) Math.Min(ushort.MaxValue, value);
                    break;

                case ClientUpdateType.Agility:
                    value = Agility = (ushort)Math.Min(ushort.MaxValue, value);
                    break;

                case ClientUpdateType.Vitality:
                    value = Vitality = (ushort)Math.Min(ushort.MaxValue, value);
                    break;

                case ClientUpdateType.Spirit:
                    value = Spirit = (ushort)Math.Min(ushort.MaxValue, value);
                    break;

                case ClientUpdateType.Class:
                    Profession = (byte) value;
                    break;

                case ClientUpdateType.Reborn:
                    Metempsychosis = (byte) value;
                    break;

                case ClientUpdateType.Vigor:
                {
                    Vigor = Math.Max(0, Math.Min(MaxVigor, (int)value));
                    await SendAsync(new MsgData
                    {
                        Action = MsgData.DataAction.SetMountMovePoint,
                        Year = Vigor
                    });
                    return true;
                }

                case ClientUpdateType.VipLevel:
                {
                    value = VipLevel = (uint) Math.Max(0, Math.Min(6, value));

                    if (VipLevel > 0)
                        await AttachStatusAsync(this, StatusSet.ORANGE_HALO_GLOW, 0, (int) (VipExpiration - DateTime.Now).TotalSeconds, 0, 0);
                    break;
                }

                default:
                    bool result = await base.SetAttributesAsync(type, value);
                    return result && await SaveAsync();
            }

            await SaveAsync();
            await SynchroAttributesAsync(type, value, screen);
            return true;
        }

        public async Task CheckPkStatusAsync()
        {
            //if (m_dbObject.KillPoints != value)
            {
                if (PkPoints > 99 && QueryStatus(StatusSet.BLACK_NAME) == null)
                {
                    await DetachStatusAsync(StatusSet.RED_NAME);
                    await AttachStatusAsync(this, StatusSet.BLACK_NAME, 0, int.MaxValue, 1, 0);
                }
                else if (PkPoints > 29 && PkPoints < 100 && QueryStatus(StatusSet.RED_NAME) == null)
                {
                    await DetachStatusAsync(StatusSet.BLACK_NAME);
                    await AttachStatusAsync(this, StatusSet.RED_NAME, 0, int.MaxValue, 1, 0);
                }
                else if (PkPoints < 30)
                {
                    await DetachStatusAsync(StatusSet.BLACK_NAME);
                    await DetachStatusAsync(StatusSet.RED_NAME);
                }
            }
        }

        #endregion

        #region Mining

        private int m_mineCount = 0;

        public void StartMining()
        {
            m_mine.Startup(3);
            m_mineCount = 0;
        }

        public void StopMining()
        {
            m_mine.Clear();
        }

        public async Task DoMineAsync()
        {
            if (!IsAlive)
            {
                await SendAsync(Language.StrDead);
                StopMining();
                return;
            }

            if (UserPackage[Item.ItemPosition.RightHand]?.GetItemSubType() != 562)
            {
                await SendAsync(Language.StrMineWithPecker);
                StopMining();
                return;
            }

            if (UserPackage.IsPackFull())
            {
                await SendAsync(Language.StrYourBagIsFull);
                return;
            }

            float nChance = 15f + ((float)(WeaponSkill[562]?.Level ?? 0) / 2);
            if (await Kernel.ChanceCalcAsync(nChance))
            {
                uint idItem = await Kernel.MineManager.GetDropAsync(this);

                DbItemtype itemtype = Kernel.ItemManager.GetItemtype(idItem);
                if (itemtype == null)
                    return;

                if (await UserPackage.AwardItemAsync(idItem))
                {
                    await SendAsync(string.Format(Language.StrMineItemFound, itemtype.Name));
                    await Log.GmLogAsync($"mine_drop", $"{Identity},{Name},{idItem},{MapIdentity},{Map?.Name},{MapX},{MapY}");
                }
                m_mineCount++;
            }

            await BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = Identity,
                Command = 0,
                ArgumentX = MapX,
                ArgumentY = MapY,
                Action = MsgAction.ActionType.MapMine
            }, true);
        }

        #endregion

        #region Status

        public bool IsAway { get; set; }

        public async Task LoadStatusAsync()
        {
            var statusList = await DbStatus.GetAsync(Identity);
            foreach (var status in statusList)
            {
                if (status.EndTime < DateTime.Now)
                {
                    _ = BaseRepository.DeleteAsync(status);
                    continue;
                }
                await AttachStatusAsync(status);
            }
        }

        #endregion

        #region Merchant

        public int Merchant => m_dbObject.Business == null ? 0 : (IsMerchant() ? 255 : 1);

        public int BusinessManDays => (int) (m_dbObject.Business == null ? 0 : Math.Ceiling((m_dbObject.Business.Value - DateTime.Now).TotalDays));


        public bool IsMerchant()
        {
            return m_dbObject.Business.HasValue && m_dbObject.Business.Value < DateTime.Now;
        }

        public bool IsAwaitingMerchantStatus()
        {
            return m_dbObject.Business.HasValue && m_dbObject.Business.Value > DateTime.Now;
        }

        public async Task<bool> SetMerchantAsync()
        {
            if (IsMerchant())
                return false;

            if (Level <= 30 && Metempsychosis == 0)
            {
                m_dbObject.Business = DateTime.Now;
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 255);
            }
            else
                m_dbObject.Business = DateTime.Now.AddDays(5);
            return await SaveAsync();
        }

        public async Task RemoveMerchantAsync()
        {
            m_dbObject.Business = null;
            await SynchroAttributesAsync(ClientUpdateType.Merchant, 0);
            await SaveAsync();
        }

        public async Task SendMerchantAsync()
        {
            if (IsMerchant())
            {
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 255);
                return;
            }

            if (IsAwaitingMerchantStatus())
            {
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 1);
                await SendAsync(new MsgInteract
                {
                    Action = MsgInteractType.MerchantProgress,
                    Command = BusinessManDays
                });
                return;
            }

            if (Level <= 30 && Metempsychosis == 0)
            {
                await SendAsync(new MsgInteract
                {
                    Action = MsgInteractType.InitialMerchant
                });
                return;
            }

            await SynchroAttributesAsync(ClientUpdateType.Merchant, 0);
        }

        #endregion

        #region VIP

        private TimeOut m_vipCmdTp = new TimeOut(120);

        public bool IsVipTeleportEnable()
        {
            return m_vipCmdTp.ToNextTime();
        }

        public uint BaseVipLevel => Math.Min(6, Math.Max(0, VipLevel));

        public uint VipLevel
        {
            get =>
                m_dbObject.VipExpiration.HasValue && m_dbObject.VipExpiration > DateTime.Now
                    ? m_dbObject.VipLevel
                    : 0;
            set => m_dbObject.VipLevel = value;
        }

        public bool HasVip => m_dbObject.VipExpiration.HasValue && m_dbObject.VipExpiration > DateTime.Now;

        public DateTime VipExpiration
        {
            get => m_dbObject.VipExpiration ?? DateTime.MinValue;
            set => m_dbObject.VipExpiration = value;
        }

        #endregion

        #region User Title

        public enum UserTitles
        {
            None,
            Vip,
            ElitePkChampionHigh = 10
        }

        private ConcurrentDictionary<uint, DbUserTitle> m_userTitles = new ConcurrentDictionary<uint, DbUserTitle>();

        public async Task LoadTitlesAsync()
        {
            var titles = await DbUserTitle.GetAsync(Identity);
            foreach (var title in titles)
            {
                m_userTitles.TryAdd(title.TitleId, title);
            }
            await SendTitlesAsync();
        }

        public bool HasTitle(UserTitles idTitle) => m_userTitles.ContainsKey((uint) idTitle);

        public List<DbUserTitle> GetUserTitles() => m_userTitles.Values.Where(x => x.DelTime > DateTime.Now).ToList();

        public byte UserTitle
        {
            get => m_dbObject.TitleSelect;
            set => m_dbObject.TitleSelect = value;
        }

        public async Task<bool> AddTitleAsync(UserTitles idTitle, DateTime expiration)
        {
            if (expiration < DateTime.Now)
                return false;

            if (HasTitle(idTitle))
            {
                m_userTitles.TryRemove((uint) idTitle, out var old);
                await BaseRepository.DeleteAsync(old);
            }

            DbUserTitle title = new DbUserTitle
            {
                PlayerId = Identity,
                TitleId = (uint) idTitle,
                DelTime = expiration,
                Status = 0,
                Type = 0
            };
            await BaseRepository.SaveAsync(title);
            return m_userTitles.TryAdd((uint) idTitle, title);
        }

        public async Task SendTitlesAsync()
        {
            foreach (var title in GetUserTitles().Select(x => (byte) x.TitleId))
                await SendAsync(new MsgTitle
                {
                    Action = MsgTitle.TitleAction.Add,
                    Title = title,
                    Identity = Identity
                });
        }

        #endregion

        #region Quiz

        public uint QuizPoints
        {
            get => m_dbObject.QuizPoints;
            set => m_dbObject.QuizPoints = value;
        }

        #endregion

        #region Flower

        public bool CanRefreshFlowerRank => m_flowerRankRefresh.ToNextTime();

        public uint FlowerCharm { get; set; }

        public DateTime? SendFlowerTime
        {
            get => m_dbObject.SendFlowerDate;
            set => m_dbObject.SendFlowerDate = value;
        }

        public uint FlowerRed
        {
            get => m_dbObject.FlowerRed;
            set
            {
                m_dbObject.FlowerRed = value;
                if (SyndicateIdentity != 0)
                    SyndicateMember.RedRoseDonation = value;
            }
        }

        public uint FlowerWhite
        {
            get => m_dbObject.FlowerWhite;
            set
            {
                m_dbObject.FlowerWhite = value;
                if (SyndicateIdentity != 0)
                    SyndicateMember.WhiteRoseDonation = value;
            }
        }

        public uint FlowerOrchid
        {
            get => m_dbObject.FlowerOrchid;
            set
            {
                m_dbObject.FlowerOrchid = value;
                if (SyndicateIdentity != 0)
                    SyndicateMember.OrchidDonation = value;
            }
        }

        public uint FlowerTulip
        {
            get => m_dbObject.FlowerTulip;
            set
            {
                m_dbObject.FlowerTulip = value;
                if (SyndicateIdentity != 0)
                    SyndicateMember.TulipDonation = value;
            }
        }
        
        #endregion

        #region Vigor

        public int Vigor { get; set; } = 0;

        public int MaxVigor => QueryStatus(StatusSet.RIDING) != null ? UserPackage[Item.ItemPosition.Steed]?.Vigor ?? 0 : 0;

        public void UpdateVigorTimer()
        {
            m_tVigor.Update();
        }

        #endregion

        #region Chat

        public bool CanUseWorldChat()
        {
            if (Level < 50)
                return false;
            if (Level < 70 && m_tWorldChat.ToNextTime(60))
                return false;
            // todo get correct times
            return m_tWorldChat.ToNextTime(15);
        }

        #endregion

        #region Arena Qualifier

        public int QualifierRank => Kernel.EventThread.GetEvent<ArenaQualifier>()?.GetPlayerRanking(Identity) ?? 0;

        public MsgQualifyingDetailInfo.ArenaStatus QualifierStatus { get; set; } = MsgQualifyingDetailInfo.ArenaStatus.NotSignedUp;

        public uint QualifierPoints
        {
            get => m_dbObject.AthletePoint;
            set => m_dbObject.AthletePoint = value;
        }

        public uint QualifierDayWins
        {
            get => m_dbObject.AthleteDayWins;
            set => m_dbObject.AthleteDayWins = value;
        }

        public uint QualifierDayLoses
        {
            get => m_dbObject.AthleteDayLoses;
            set => m_dbObject.AthleteDayLoses = value;
        }

        public uint QualifierDayGames => QualifierDayWins + QualifierDayLoses;

        public uint QualifierHistoryWins
        {
            get => m_dbObject.AthleteHistoryWins;
            set => m_dbObject.AthleteHistoryWins = value;
        }

        public uint QualifierHistoryLoses
        {
            get => m_dbObject.AthleteHistoryLoses;
            set => m_dbObject.AthleteHistoryLoses = value;
        }

        public uint HonorPoints
        {
            get => m_dbObject.AthleteCurrentHonorPoints;
            set => m_dbObject.AthleteCurrentHonorPoints = value;
        }

        public uint HistoryHonorPoints
        {
            get => m_dbObject.AthleteHistoryHonorPoints;
            set => m_dbObject.AthleteHistoryHonorPoints = value;
        }

        public DbArenic DailyArenic { get; set; }

        #endregion

        #region Activity Points

        public async Task<bool> AddActivityPointsAsync(int amount)
        {
            await Statistic.AddOrUpdateAsync(1200, 0, Statistic.GetValue(1200) + 1, true);
            await Log.GmLogAsync($"activity_{Identity}", $"{Identity},{Name},{amount}");
            return true;
        }

        #endregion

        #region Family

        public Family Family { get; set; }
        public FamilyMember FamilyMember => Family?.GetMember(Identity);

        public uint FamilyIdentity => Family?.Identity ?? 0;
        public string FamilyName => Family?.Name ?? Language.StrNone;

        public Family.FamilyRank FamilyPosition => FamilyMember?.Rank ?? Family.FamilyRank.None;

        public async Task LoadFamilyAsync()
        {
            Family = Kernel.FamilyManager.FindByUser(Identity);
            if (Family == null)
            {
                if (MateIdentity != 0)
                {
                    Family family = Kernel.FamilyManager.FindByUser(MateIdentity);
                    FamilyMember mateFamily = family?.GetMember(MateIdentity);
                    if (mateFamily == null || mateFamily.Rank == Family.FamilyRank.Spouse)
                        return;

                    if (!await family.AppendMemberAsync(null, this, Family.FamilyRank.Spouse))
                        return;
                }
            }
            else
            {
                await SendFamilyAsync();
                await Family.SendRelationsAsync(this);
            }

            if (Family == null)
                return;

            FamilyWar war = Kernel.EventThread.GetEvent<FamilyWar>();
            if (war == null)
                return;

            if (Family.ChallengeMap == 0)
                return;

            GameMap map = Kernel.MapManager.GetMap(Family.ChallengeMap);
            if (map == null)
                return;

            await SendAsync(string.Format(Language.StrPrepareToChallengeFamilyLogin, map.Name), MsgTalk.TalkChannel.Talk, Color.White);

            map = Kernel.MapManager.GetMap(Family.FamilyMap);
            if (map == null)
                return;

            if (war.GetChallengers(map.Identity).Count == 0)
                return;

            await SendAsync(string.Format(Language.StrPrepareToDefendFamilyLogin, map.Name), MsgTalk.TalkChannel.Talk, Color.White);
        }

        private string FamilyOccupyString
        {
            get
            {
                FamilyWar war = Kernel.EventThread.GetEvent<FamilyWar>();
                if (war == null || Family == null)
                    return "0 0 0 0 0 0 0 0";
                uint idNpc = war.GetDominatingNpc(Family)?.Identity ?? 0;
                return "0 " +
                       $"{Family.OccupyDays} " +
                       $"{war.GetNextReward(this, idNpc)} " +
                       $"{war.GetNextWeekReward(this, idNpc)} " +
                       $"{(war.IsChallenged(Family.FamilyMap) ? 1 : 0)} " +
                       $"{(war.HasRewardToClaim(this) ? 1 : 0)} " +
                       $"{(war.HasExpToClaim(this) ? 1 : 0)}";
            }
        }

        public string FamilyDominatedMap => Family != null ? Kernel.EventThread.GetEvent<FamilyWar>()?.GetMap(Family.FamilyMap)?.Name ?? "" : "";
        public string FamilyChallengedMap => Family != null ? Kernel.EventThread.GetEvent<FamilyWar>()?.GetMap(Family.ChallengeMap)?.Name ?? "" : "";

        public Task SendFamilyAsync()
        {
            if (Family == null)
                return Task.CompletedTask;

            MsgFamily msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily.FamilyAction.Query
            };
            msg.Strings.Add($"{Family.Identity} {Family.MembersCount} {Family.MembersCount} {Family.Money} {Family.Rank} {(int) FamilyPosition} 0 {Family.BattlePowerTower} 0 0 1 {FamilyMember.Proffer}");
            msg.Strings.Add(FamilyName);
            msg.Strings.Add(Name);
            msg.Strings.Add(FamilyOccupyString);
            msg.Strings.Add(FamilyDominatedMap);
            msg.Strings.Add(FamilyChallengedMap);
            return SendAsync(msg);
        }

        public Task SendFamilyOccupyAsync()
        {
            if (Family == null)
                return Task.CompletedTask;

            MsgFamily msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily.FamilyAction.QueryOccupy
            };
            // uid occupydays reward nextreward challenged rewardtoclaim exptoclaim
            msg.Strings.Add(FamilyOccupyString);
            return SendAsync(msg);
        }

        public async Task SendNoFamilyAsync()
        {
            MsgFamily msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily.FamilyAction.Query
            };
            msg.Strings.Add(FamilyOccupyString);
            msg.Strings.Add("");
            msg.Strings.Add(Name);
            await SendAsync(msg);

            msg.Action = MsgFamily.FamilyAction.Quit;
            await SendAsync(msg);
        }

        public async Task<bool> CreateFamilyAsync(string name, uint proffer)
        {
            if (Family != null)
                return false;

            if (!Kernel.IsValidName(name))
                return false;

            if (name.Length > 15)
                return false;

            if (Kernel.FamilyManager.GetFamily(name) != null)
                return false;

            if (!await SpendMoneyAsync((int) proffer, true))
                return false;

            Family = await Family.CreateAsync(this, name, proffer / 2);
            if (Family == null)
                return false;

            await SendFamilyAsync();
            await Family.SendRelationsAsync(this);
            return true;
        }

        public async Task<bool> DisbandFamilyAsync()
        {
            if (Family == null)
                return false;

            if (FamilyPosition != Family.FamilyRank.ClanLeader)
                return false;

            if (Family.MembersCount > 1)
                return false;

            await FamilyMember.DeleteAsync();
            await Family.SoftDeleteAsync();

            Family = null;

            await SendNoFamilyAsync();
            return true;
        }

        public Task SynchroFamilyBattlePowerAsync()
        {
            if (Team == null || Family == null)
                return Task.CompletedTask;

            int bp = Team.FamilyBattlePower(this, out var provider);
            MsgUserAttrib msg = new MsgUserAttrib(Identity, ClientUpdateType.FamilySharedBattlePower, provider);
            msg.Append(ClientUpdateType.FamilySharedBattlePower, (ulong) bp);
            return SendAsync(msg);
        }

        public int FamilyBattlePower => Team?.FamilyBattlePower(this, out _) ?? 0;

        #endregion

        #region Tutor

        private DbTutorAccess m_tutorAccess;

        public ulong MentorExpTime
        {
            get => m_tutorAccess?.Experience ?? 0;
            set
            {
                m_tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                m_tutorAccess.Experience = value;
            }
        }

        public ushort MentorAddLevexp
        {
            get => m_tutorAccess?.Composition ?? 0;
            set
            {
                m_tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                m_tutorAccess.Composition = value;
            }
        }

        public ushort MentorGodTime
        {
            get => m_tutorAccess?.Blessing ?? 0;
            set
            {
                m_tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                m_tutorAccess.Blessing = value;
            }
        }

        public Tutor Guide;

        private ConcurrentDictionary<uint, Tutor> m_apprentices = new ConcurrentDictionary<uint, Tutor>();

        public Tutor GetStudent(uint idStudent)
        {
            return m_apprentices.TryGetValue(idStudent, out var value) ? value : null;
        }

        public int ApprenticeCount => m_apprentices.Count;

        public async Task LoadGuideAsync()
        {
            DbTutor tutor = await DbTutor.GetAsync(Identity);
            if (tutor != null)
            {
                Guide = await Tutor.CreateAsync(tutor);
                if (Guide != null)
                {
                    //await Guide.SendAsync(MsgGuideInfo.RequestMode.Mentor);
                    await Guide.SendTutorAsync();
                    await Guide.SendStudentAsync();
                    
                    Character guide = Guide.Guide;
                    if (guide != null)
                    {
                        // await Guide.SendAsync(MsgGuideInfo.RequestMode.Apprentice);
                        await SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, (uint) Guide.SharedBattlePower, (uint) guide.BattlePower);
                        await guide.SendAsync(string.Format(Language.StrGuideStudentLogin, Name));
                    }
                }
            }

            var apprentices = await DbTutor.GetStudentsAsync(Identity);
            foreach (var dbApprentice in apprentices)
            {
                Tutor apprentice = await Tutor.CreateAsync(dbApprentice);
                if (apprentice != null)
                {
                    m_apprentices.TryAdd(dbApprentice.StudentId, apprentice);
                    // await apprentice.SendAsync(MsgGuideInfo.RequestMode.Apprentice);
                    await apprentice.SendTutorAsync();
                    await apprentice.SendStudentAsync();

                    Character student = apprentice.Student;
                    if (student != null)
                    {
                        // await apprentice.SendAsync(MsgGuideInfo.RequestMode.Mentor);
                        await student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, (uint) apprentice.SharedBattlePower, (uint) BattlePower);
                        await student.SendAsync(string.Format(Language.StrGuideTutorLogin, Name));
                    }
                }
            }

            m_tutorAccess = await DbTutorAccess.GetAsync(Identity);
        }

        public static async Task<bool> CreateTutorRelationAsync(Character guide, Character apprentice)
        {
            if (guide.Level < apprentice.Level || guide.Metempsychosis < apprentice.Metempsychosis)
                return false;

            int deltaLevel = guide.Level - apprentice.Level;
            if (apprentice.Metempsychosis == 0)
            {
                if (deltaLevel < 30)
                    return false;
            }
            else if (apprentice.Metempsychosis == 1)
            {
                if (deltaLevel > 20)
                    return false;
            }
            else
            {
                if (deltaLevel > 10)
                    return false;
            }

            DbTutorType type = Kernel.RoleManager.GetTutorType(guide.Level);
            if (type == null || guide.ApprenticeCount >= type.StudentNum)
                return false;

            if (apprentice.Guide != null)
                return false;

            if (guide.m_apprentices.ContainsKey(apprentice.Identity))
                return false;

            DbTutor dbTutor = new DbTutor
            {
                GuideId = guide.Identity,
                StudentId = apprentice.Identity,
                Date = DateTime.Now
            };
            if (!await BaseRepository.SaveAsync(dbTutor))
                return false;

            var tutor = await Tutor.CreateAsync(dbTutor);
            
            apprentice.Guide = tutor;
            //await tutor.SendAsync(MsgGuideInfo.RequestMode.Mentor);
            await tutor.SendTutorAsync();
            guide.m_apprentices.TryAdd(apprentice.Identity, tutor);
            await tutor.SendStudentAsync();
            //await tutor.SendAsync(MsgGuideInfo.RequestMode.Apprentice);

            await apprentice.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, (uint) tutor.SharedBattlePower, (uint) guide.BattlePower);
            return true;
        }

        public async Task SynchroApprenticesSharedBattlePowerAsync()
        {
            foreach (var apprentice in m_apprentices.Values.Where(x => x.Student != null))
            {
                await apprentice.Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower,
                    (uint) apprentice.SharedBattlePower, (uint) (apprentice.Guide?.BattlePower ?? 0));
            }
        }

        /// <summary>
        /// Returns true if the current user is the tutor of the target ID.
        /// </summary>
        public bool IsTutor(uint idApprentice)
        {
            return m_apprentices.ContainsKey(idApprentice);
        }

        public bool IsApprentice(uint idGuide)
        {
            return Guide?.GuideIdentity == idGuide;
        }

        public void RemoveApprentice(uint idApprentice)
        {
            m_apprentices.TryRemove(idApprentice, out _);
        }

        public Task<bool> SaveTutorAccessAsync()
        {
            if (m_tutorAccess != null)
                return BaseRepository.SaveAsync(m_tutorAccess);
            return Task.FromResult(true);
        }

        #endregion

        #region Online Training

        public uint GodTimeExp
        {
            get => m_dbObject.OnlineGodExpTime;
            set => m_dbObject.OnlineGodExpTime = value;
        }

        public uint OnlineTrainingExp
        {
            get => m_dbObject.BattleGodExpTime;
            set => m_dbObject.BattleGodExpTime = value;
        }

        #endregion

        #region Relation Packet

        public Task SendRelationAsync(Character target)
        {
            return SendAsync(new MsgRelation
            {
                SenderIdentity = target.Identity,
                Level = target.Level,
                BattlePower = target.BattlePower,
                IsSpouse = target.Identity == MateIdentity,
                IsTradePartner = IsTradePartner(target.Identity),
                IsTutor = IsTutor(target.Identity),
                TargetIdentity = Identity
            });
        }

        #endregion

        #region Equipment Detain

        public async Task SendDetainedEquipmentAsync()
        {
            var items = await DbDetainedItem.GetFromDischargerAsync(Identity);
            foreach (var dbDischarged in items)
            {
                if (dbDischarged.ItemIdentity == 0)
                    continue; // item already claimed back

                var dbItem = await ItemRepository.GetByIdAsync(dbDischarged.ItemIdentity);
                if (dbItem == null)
                {
                    await BaseRepository.DeleteAsync(dbDischarged);
                    continue;
                }

                Item item = new ();
                if (!await item.CreateAsync(dbItem))
                    continue;

                await SendAsync(new MsgDetainItemInfo(dbDischarged, item, MsgDetainItemInfo.Mode.DetainPage));
            }

            if (items.Count > 0)
                await SendAsync(Language.StrHasDetainEquip, MsgTalk.TalkChannel.Talk);
        }

        public async Task SendDetainRewardAsync()
        {
            var items = await DbDetainedItem.GetFromHunterAsync(Identity);
            foreach (var dbDetained in items)
            {
                DbItem dbItem = null;
                Item item = null;

                if (dbDetained.ItemIdentity != 0)
                {
                    dbItem = await ItemRepository.GetByIdAsync(dbDetained.ItemIdentity);
                    if (dbItem == null)
                    {
                        await BaseRepository.DeleteAsync(dbDetained);
                        continue;
                    }

                    item = new();
                    if (!await item.CreateAsync(dbItem))
                        continue;
                }

                bool expired = dbDetained.HuntTime + 60 * 60 * 24 * 7 < UnixTimestamp.Now();
                bool notClaimed = dbDetained.ItemIdentity != 0;

                await SendAsync(new MsgDetainItemInfo(dbDetained, item, MsgDetainItemInfo.Mode.ClaimPage));
                if (!expired && notClaimed)
                {
                    // ? send message? do nothing
                }
                else if (expired && notClaimed)
                {
                    // ? send message, item ready to be claimed
                    if (ItemManager.Confiscator != null) 
                    {
                        await SendAsync(string.Format(Language.StrHasEquipBonus, dbDetained.TargetName, ItemManager.Confiscator.Name, ItemManager.Confiscator.MapX, ItemManager.Confiscator.MapY), MsgTalk.TalkChannel.Talk);
                    }
                }
                else if (!notClaimed)
                {
                    if (ItemManager.Confiscator != null)
                    {
                        await SendAsync(string.Format(Language.StrHasEmoneyBonus, dbDetained.TargetName, ItemManager.Confiscator.Name, ItemManager.Confiscator.MapX, ItemManager.Confiscator.MapY), MsgTalk.TalkChannel.Talk);
                    }

                    // claimed, show CPs reward
                    await SendAsync(new MsgItem
                    {
                        Action = MsgItem.ItemActionType.RedeemEquipment,
                        Identity = dbDetained.Identity,
                        Command = dbDetained.TargetIdentity,
                        Argument2 = dbDetained.RedeemPrice
                    });
                }
            }

            if (items.Count > 0 && ItemManager.Confiscator != null)
            {
                await SendAsync(string.Format(Language.StrPkBonus, ItemManager.Confiscator.Name, ItemManager.Confiscator.MapX, ItemManager.Confiscator.MapY), MsgTalk.TalkChannel.Talk);
            }
        }

        #endregion

        #region Timer

        public uint DayResetDate
        {
            get => m_dbObject.DayResetDate;
            set => m_dbObject.DayResetDate = value;
        }

        public async Task OnBattleTimerAsync()
        {
            if (BattleSystem != null
                && BattleSystem.IsActive()
                && BattleSystem.NextAttack(await GetInterAtkRateAsync()))
            {
                QueueAction(BattleSystem.ProcessAttackAsync);
            }

            if (MagicData.State != MagicData.MagicState.None)
                QueueAction(MagicData.OnTimerAsync);
        }

        public override async Task OnTimerAsync()
        {
            if (Connection != ConnectionStage.Ready)
                return;

            try
            {
                if (MessageBox != null)
                    await MessageBox.OnTimerAsync();

                if (MessageBox != null && MessageBox.HasExpired)
                    MessageBox = null;

                if (!m_activityPointsAdd.IsActive())
                {
                    m_activityPointsAdd.Update();
                }
                else if (m_activityPointsAdd.ToNextTime())
                {
                    await AddActivityPointsAsync(1);
                }

                if (m_pkDecrease.ToNextTime(PK_DEC_TIME) && PkPoints > 0)
                {
                    if (MapIdentity == 6001)
                    {
                        QueueAction(() => AddAttributesAsync(ClientUpdateType.PkPoints, PKVALUE_DEC_ONCE_IN_PRISON));
                    }
                    else
                    {
                        QueueAction(() => AddAttributesAsync(ClientUpdateType.PkPoints, PKVALUE_DEC_ONCE));
                    }
                }

                foreach (var status in StatusSet.Status.Values)
                {
                    QueueAction(async () =>
                    {
                        await status.OnTimerAsync();

                        if (!status.IsValid && status.Identity != StatusSet.GHOST && status.Identity != StatusSet.DEAD)
                        {
                            await StatusSet.DelObjAsync(status.Identity);

                            if ((status.Identity == StatusSet.SUPERMAN || status.Identity == StatusSet.CYCLONE)
                                && (QueryStatus(StatusSet.SUPERMAN) == null && QueryRole(StatusSet.CYCLONE) == null))
                            {
                                await FinishXpAsync();
                            }
                        }
                    });
                }

                if (IsBlessed && m_heavenBlessing.ToNextTime() && !Map.IsTrainingMap())
                {
                    m_blessPoints++;
                    if (m_blessPoints >= 10)
                    {
                        GodTimeExp += 60;

                        await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 5);
                        await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 0);
                        m_blessPoints = 0;
                    }
                    else
                    {
                        await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 4);
                        await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 3);
                    }
                }
                
                if (m_idLuckyTarget == 0 && Metempsychosis < 2 && QueryStatus(StatusSet.LUCKY_DIFFUSE) == null)
                {
                    if (QueryStatus(StatusSet.LUCKY_ABSORB) == null)
                    {
                        foreach (var user in Screen.Roles.Values.Where(x => x.IsPlayer()).Cast<Character>())
                        {
                            if (user.QueryStatus(StatusSet.LUCKY_DIFFUSE) != null && GetDistance(user) <= 3)
                            {
                                m_idLuckyTarget = user.Identity;
                                m_luckyAbsorbStart.Startup(3);
                                break;
                            }
                        }
                    }
                }
                else if (QueryStatus(StatusSet.LUCKY_DIFFUSE) == null)
                {
                    Character role = QueryRole(m_idLuckyTarget) as Character;
                    if (m_luckyAbsorbStart.IsTimeOut() && role != null)
                    {
                        await AttachStatusAsync(role, StatusSet.LUCKY_ABSORB, 0, 1000000, 0, 0);
                        m_idLuckyTarget = 0;
                        m_luckyAbsorbStart.Clear();
                    }
                }

                if (m_luckyStep.ToNextTime() && IsLucky)
                {
                    if (QueryStatus(StatusSet.LUCKY_DIFFUSE) == null && QueryStatus(StatusSet.LUCKY_ABSORB) == null)
                        await ChangeLuckyTimerAsync(-1);
                }

                if (!IsAlive && !IsGhost() && m_ghost.IsActive() && m_ghost.IsTimeOut(4))
                {
                    await SetGhostAsync();
                    m_ghost.Clear();
                }

                if (Team != null && !Team.IsLeader(Identity) && Team.Leader.MapIdentity == MapIdentity &&
                    m_teamLeaderPos.ToNextTime())
                {
                    await SendAsync(new MsgAction
                    {
                        Action = MsgAction.ActionType.MapTeamLeaderStar,
                        Command = Team.Leader.Identity,
                        ArgumentX = Team.Leader.MapX,
                        ArgumentY = Team.Leader.MapY
                    });
                }

                if (Guide != null && Guide.BetrayalCheck)
                {
                    QueueAction(() => Guide.BetrayalTimerAsync());
                }

                foreach (var apprentice in m_apprentices.Values.Where(x => x.BetrayalCheck))
                {
                    QueueAction(() => apprentice.BetrayalTimerAsync());
                }

                if (m_tVigor.ToNextTime() && QueryStatus(StatusSet.RIDING) != null && Vigor < MaxVigor)
                {
                    await AddAttributesAsync(ClientUpdateType.Vigor, (long) Math.Max(10, Math.Min(200, MaxVigor * 0.005)));
                }

                if (!IsAlive)
                    return;

                if (Transformation != null && m_transformation.IsTimeOut())
                    await ClearTransformationAsync();

                if (m_energyTm.ToNextTime(ADD_ENERGY_STAND_MS))
                {
                    byte energyAmount = ADD_ENERGY_STAND;
                    if (IsWing)
                    {
                        energyAmount = ADD_ENERGY_STAND / 2;
                    }
                    else
                    {
                        if (Action == EntityAction.Sit)
                        {
                            energyAmount = ADD_ENERGY_SIT;
                        }
                        else if (Action == EntityAction.Lie)
                        {
                            energyAmount = ADD_ENERGY_LIE;
                        }
                    }

                    QueueAction(() => AddAttributesAsync(ClientUpdateType.Stamina, energyAmount));
                }

                if (m_xpPoints.ToNextTime())
                {
                    await ProcXpValAsync();
                }

                if (m_autoHeal.ToNextTime() && IsAlive)
                {
                    QueueAction(() => AddAttributesAsync(ClientUpdateType.Hitpoints, AUTOHEALLIFE_EACHPERIOD));
                }

                if (m_mine.IsActive() && m_mine.ToNextTime())
                    await DoMineAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Timer error for user {Identity}:{Name}");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                m_socket.Disconnect();
            }
        }

        #endregion

        #region Socket

        public DateTime LastLogin => m_dbObject.LoginTime;
        public DateTime LastLogout => m_dbObject.LogoutTime;
        public int TotalOnlineTime => m_dbObject.OnlineSeconds;

        public async Task SetLoginAsync()
        {
            m_dbObject.LoginTime = m_dbObject.LogoutTime = DateTime.Now;
            await SaveAsync();
        }

        public async Task OnDisconnectAsync()
        {
            if (Map?.IsRecordDisable() == false && IsAlive)
            {
                m_dbObject.MapID = m_idMap;
                m_dbObject.X = m_posX;
                m_dbObject.Y = m_posY;
            }

            m_dbObject.LogoutTime = DateTime.Now;
            m_dbObject.OnlineSeconds += (int)(m_dbObject.LogoutTime - m_dbObject.LoginTime).TotalSeconds;

            if (!IsAlive)
                m_dbObject.HealthPoints = 1;

            { // scope to don't create variable externally
                var msg = new MsgAccServerPlayerExchange
                {
                    ServerName = Kernel.Configuration.ServerName
                };
                msg.Data.Add(MsgAccServerPlayerExchange.CreatePlayerData(this));
                await Kernel.AccountServer.SendAsync(msg);

                await Kernel.AccountServer.SendAsync(new MsgAccServerPlayerStatus
                {
                    ServerName = Kernel.Configuration.ServerName,
                    Status = new List<MsgAccServerPlayerStatus<AccountServer>.PlayerStatus>
                    {
                        new MsgAccServerPlayerStatus<AccountServer>.PlayerStatus
                        {
                            Identity = Client.AccountIdentity,
                            Online = false
                        }
                    }
                });
            }

            try
            {
                if (CurrentEvent is ArenaQualifier qualifier)
                {
                    if (qualifier.IsInsideMatch(Identity))
                    {
                        var match = qualifier.FindMatchByMap(MapIdentity);
                        if (match != null && match.IsRunning) // if not running probably opponent quit first?
                            await match.FinishAsync(null, this, Identity);
                    }
                    else if (qualifier.FindInQueue(Identity) != null)
                    {
                        await qualifier.UnsubscribeAsync(Identity);
                    }
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on leave qualifier disconnection");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                if (Booth != null)
                    await Booth.LeaveMapAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on booth disconnection");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {               
                await NotifyOfflineFriendAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on notifying friends disconnection");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                foreach (var apprentice in m_apprentices.Values.Where(x => x.Student != null))
                {
                    // await apprentice.SendAsync(MsgGuideInfo.RequestMode.Mentor);
                    await apprentice.SendTutorAsync();
                    await apprentice.Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, 0, 0);
                }

                if (m_tutorAccess != null)
                    await BaseRepository.SaveAsync(m_tutorAccess);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on guide dismiss");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                if (Team != null && Team.IsLeader(Identity))
                    await Team.DismissAsync(this, true);
                else if (Team != null)
                    await Team.DismissMemberAsync(this);                
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on team dismiss");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                if (Trade != null)
                    await Trade.SendCloseAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on close trade");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                await LeaveMapAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on leave map");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                foreach (var status in StatusSet.Status.Values.Where(x => x.Model != null))
                {
                    if (status is StatusMore && status.RemainingTimes == 0)
                        continue;

                    status.Model.LeaveTimes = (uint) status.RemainingTimes;
                    status.Model.RemainTime = (uint) status.RemainingTime;

                    await BaseRepository.SaveAsync(status.Model);
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on save status");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                await BaseRepository.SaveAsync(m_monsterKills.Values.ToList());
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on save monster kills");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                await WeaponSkill.SaveAllAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on save weaponskills ");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                if (Syndicate != null && SyndicateMember != null)
                {
                    SyndicateMember.LastLogout = DateTime.Now;
                    await SyndicateMember.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on save syndicate");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            if (!m_IsDeleted)
                await SaveAsync();

            try
            {
                await using ServerDbContext context = new ServerDbContext();
                await context.LoginRcd.AddAsync(new DbGameLoginRecord
                {
                    AccountIdentity = Client.AccountIdentity,
                    UserIdentity = Identity,
                    LoginTime = m_dbObject.LoginTime,
                    LogoutTime = m_dbObject.LogoutTime,
                    ServerVersion = $"[{Kernel.SERVER_VERSION}]{Kernel.Version}",
                    IpAddress = Client.IPAddress,
                    MacAddress = Client.MacAddress,
                    OnlineTime = (uint) (m_dbObject.LogoutTime - m_dbObject.LoginTime).TotalSeconds
                });
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on saving login rcd");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }
        }

        public override Task SendAsync(IPacket msg)
        {
            try
            {
                if (Connection != ConnectionStage.Disconnected)
                    return m_socket.SendAsync(msg);
            }
            catch (Exception ex)
            {
                return Log.WriteLogAsync(LogLevel.Error, ex.Message);
            }
            return Task.CompletedTask;
        }

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgPlayer(this));
            
            if (Syndicate != null)
                await Syndicate.SendAsync(player);
        }

        public async Task SendWindowToAsync(Character player)
        {
            await player.SendAsync(new MsgPlayer(this)
            {
                WindowSpawn = true
            });
        }

        public async Task BroadcastTeamLifeAsync(bool maxLife = false)
        {
            if (Team != null)
                await Team.BroadcastMemberLifeAsync(this, maxLife);
        }

        #endregion

        #region Database

        public DbCharacter GetDatabaseObject() => m_dbObject;

        public async Task<bool> SaveAsync()
        {
            try
            {
                await using var db = new ServerDbContext();
                db.Update(m_dbObject);
                return await Task.FromResult(await db.SaveChangesAsync() != 0);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        #endregion

        #region Deletion

        private bool m_IsDeleted = false;

        public async Task<bool> DeleteCharacterAsync()
        {
            if (Syndicate != null)
            {
                if (SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                {
                    if (!await Syndicate.QuitSyndicateAsync(this))
                        return false;
                }
                else
                {
                    if (!await Syndicate.DisbandAsync(this))
                        return false;
                }
            }

            await BaseRepository.ScalarAsync($"INSERT INTO `cq_deluser` SELECT * FROM `cq_user` WHERE `id`={Identity};");
            await BaseRepository.DeleteAsync(m_dbObject);
            await Log.GmLogAsync("delete_user", $"{Identity},{Name},{MapIdentity},{MapX},{MapY},{Silvers},{ConquerPoints},{Level},{Profession},{FirstProfession},{PreviousProfession}");

            foreach (var friend in m_dicFriends.Values)
                await friend.DeleteAsync();

            foreach (var enemy in m_dicEnemies.Values)
                await enemy.DeleteAsync();

            foreach (var tradePartner in m_tradePartners.Values)
                await tradePartner.DeleteAsync();

            if (Guide != null)
            {
                await Guide.DeleteAsync();
            }

            DbPeerage peerage = Kernel.PeerageManager.GetUser(Identity);
            if (peerage != null)
                await BaseRepository.DeleteAsync(peerage);

            return m_IsDeleted = true;
        }

        #endregion

        public enum ConnectionStage
        {
            Connected,
            Ready,
            Disconnected
        }
    }

    /// <summary>Enumeration type for body types for player characters.</summary>
    public enum BodyType : ushort
    {
        AgileMale = 1003,
        MuscularMale = 1004,
        AgileFemale = 2001,
        MuscularFemale = 2002
    }

    /// <summary>Enumeration type for base classes for player characters.</summary>
    public enum BaseClassType : ushort
    {
        Trojan = 10,
        Warrior = 20,
        Archer = 40,
        Ninja = 50,
        Taoist = 100
    }

    public enum PkModeType
    {
        FreePk,
        Peace,
        Team,
        Capture
    }

    public enum RequestType
    {
        Friend,
        Syndicate,
        TeamApply,
        TeamInvite,
        Trade,
        Marriage,
        TradePartner,
        Guide,
        Family
    }
}