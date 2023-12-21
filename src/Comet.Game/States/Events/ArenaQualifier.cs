// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - ArenaQualifier.cs
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States.BaseEntities;
using Comet.Game.States.Magics;
using Comet.Game.World;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Shared;
using Microsoft.Extensions.Configuration.Ini;

namespace Comet.Game.States.Events
{
    public sealed class ArenaQualifier : GameEvent
    {
        public const int MIN_LEVEL = 40;
        public const int PRICE_PER_1500_POINTS = 9000000;
        public const uint BASE_MAP_ID_U = 900000;
        public const uint TRIUMPH_HONOR_REWARD = 5000;

        public static IdentityGenerator MapIdentityGenerator = new IdentityGenerator(900001, 999999);
        public static DbMap BaseMap { get; private set; }

        private static readonly uint[] m_dwStartPoints =
        {
            1500, // over 70
            2200, // over 90
            2700, // over 100
            3200, // over 110
            4000 // over 120
        };

        private ConcurrentDictionary<uint, DbArenic> m_arenics = new ConcurrentDictionary<uint, DbArenic>();
        private ConcurrentDictionary<uint, QualifierUser> m_awaitingQueue = new ConcurrentDictionary<uint, QualifierUser>();
        private ConcurrentDictionary<uint, QualifierMatch> m_matches = new ConcurrentDictionary<uint, QualifierMatch>();
        private ConcurrentDictionary<int, HonorRewards> m_rewards = new ConcurrentDictionary<int, HonorRewards>();

        public ArenaQualifier()
            : base("ArenaQualifier", 500)
        {
        }

        #region Event Override

        public override EventType Identity => EventType.ArenaQualifier;

        public override async Task<bool> CreateAsync()
        {
            Map = Kernel.MapManager.GetMap(BASE_MAP_ID_U);
            
            if (Map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Base Map {BASE_MAP_ID_U} not found Arena Qualifier");
                return false;
            }

            m_arenics = new ConcurrentDictionary<uint, DbArenic>((await DbArenic.GetAsync(DateTime.Now)).ToDictionary(x => x.UserId));

            try
            {
                var rows = File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "ini", "HonorRewards.ini"));
                int count = Math.Min(1, rows.Length);
                for (int i = 0; i < count + 1; i++)
                {
                    string[] row = rows[i].Split('=');
                    if (i == 0)
                    {
                        int.TryParse(row[1], out count);
                        continue;
                    }

                    if (!ushort.TryParse(row[0], out var rank))
                    {
                        await Log.WriteLogAsync(LogLevel.Warning, $"Error parsing rank on line {count + 1} of HonorRewards.ini");
                        continue;
                    }

                    string[] values = row[1].Split('/');

                    int.TryParse(values[0], out var honor);
                    int.TryParse(values[1], out var monthlyHonor);

                    m_rewards.TryAdd(rank, new HonorRewards
                    {
                        MonthlyHonor = monthlyHonor,
                        Honor = honor,
                        Rank = rank                        
                    });
                }
            }
            catch (Exception e)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Could not load HonorRewards.ini file. Error wont stop server execution but no Honor Rewards will be given." +
                    $" Exception: {e.Message}");
            }

            return true;
        }

        public override bool IsAllowedToJoin(Role sender)
        {
            if (!(sender is Character user))
                return false;

            if (user.Level < 40)
                return false;

            if (user.Map.IsPrisionMap())
                return false;

            if (user.QualifierPoints == 0)
                return false;

            return true;
        }

        public override bool IsAttackEnable(Role sender)
        {
            return FindMatchByMap(sender.MapIdentity)?.IsAttackEnable ?? true; // return true, because if not in event already it must be able to attack
        }

        public override Task OnEnterAsync(Character sender)
        {
            return SendArenaInformationAsync(sender);
        }

        public override Task OnExitAsync(Character sender)
        {
            return SendArenaInformationAsync(sender);
        }

        public override Task OnBeAttackAsync(Role attacker, Role target, int damage = 0, Magic magic = null)
        {
            if (!(attacker is Character atkUser) || !(target is Character tgtUser))
                return Task.CompletedTask;

            if (attacker.MapIdentity - attacker.MapIdentity % BASE_MAP_ID_U != BASE_MAP_ID_U || attacker.MapIdentity != target.MapIdentity)
                return Task.CompletedTask; // ??? should remove?

            QualifierMatch match = FindMatchByMap(attacker.MapIdentity);
            if (match == null)
                return Task.CompletedTask; // ??? Should remove???

            if (attacker.Identity == match.Player1.Identity)
            {
                match.Points1 += damage;
            }
            else if (attacker.Identity == match.Player2.Identity)
            {
                match.Points2 += damage;
            }
            else
            {
                return Task.CompletedTask;
            }

            return match.SendBoardAsync();
        }

        public override async Task OnTimerAsync()
        {
            foreach (var queueUser in m_awaitingQueue.Values)
            {
                Character player = Kernel.RoleManager.GetUser(queueUser.Identity);
                if (player == null)
                {
                    await UnsubscribeAsync(queueUser.Identity);
                    continue;
                }

                QualifierUser target = await FindTargetAsync(queueUser);
                if (target == null)
                    continue;

                Character targetPlayer = Kernel.RoleManager.GetUser(target.Identity);
                if (targetPlayer == null)
                {
                    await UnsubscribeAsync(target.Identity);
                    continue;
                }

                if (!IsAllowedToJoin(player))
                {
                    await UnsubscribeAsync(player.Identity);
                    continue;
                }

                if (!IsAllowedToJoin(targetPlayer))
                {
                    await UnsubscribeAsync(player.Identity);
                    continue;
                }

                QualifierMatch match = new QualifierMatch();
                if (!await match.CreateAsync(player, queueUser, targetPlayer, target) || !m_matches.TryAdd(match.MapIdentity, match))
                {
                    await UnsubscribeAsync(queueUser.Identity);
                    await UnsubscribeAsync(target.Identity);
                    continue;
                }

                m_awaitingQueue.TryRemove(player.Identity, out _);
                m_awaitingQueue.TryRemove(target.Identity, out _);

                player.QualifierStatus = MsgQualifyingDetailInfo.ArenaStatus.WaitingInactive;
                targetPlayer.QualifierStatus = MsgQualifyingDetailInfo.ArenaStatus.WaitingInactive;

                await SendArenaInformationAsync(player);
                await SendArenaInformationAsync(targetPlayer);
            }

            foreach (var match in m_matches.Values)
            {
                if (match.Status == QualifierMatch.MatchStatus.ReadyToDispose)
                {
                    m_matches.TryRemove(match.MapIdentity, out _);
                    continue;
                }

                await match.OnTimerAsync();
            }
        }

        #endregion

        #region Inscribe

        public async Task<bool> InscribeAsync(Character user)
        {
            if (HasUser(user.Identity))
                return false; // already joined ????

            if (!IsAllowedToJoin(user))
                return false;

            if (EnterQueue(user) != null)
                await user.SignInEventAsync(this);
            return true;
        }
        
        public async Task<bool> UnsubscribeAsync(uint idUser)
        {
            Character user = Kernel.RoleManager.GetUser(idUser);

            LeaveQueue(idUser);
            if (user != null)
            {
                user.QualifierStatus = MsgQualifyingDetailInfo.ArenaStatus.NotSignedUp;
                await user.SignOutEventAsync();
            }
            return true;
        }

        #endregion

        #region Match Management

        public QualifierMatch FindMatch(uint idUser)
        {
            return m_matches.Values.FirstOrDefault(x => x.Player1.Identity == idUser || x.Player2.Identity == idUser);
        }

        public QualifierMatch FindMatchByMap(uint idMap)
        {
            return m_matches.TryGetValue(idMap, out var match) ? match : null;
        }

        public List<QualifierMatch> QueryMatches(int from, int limit)
        {
            return m_matches.Values
                .Skip(from)
                .Take(limit)
                .ToList();
        }

        public async Task FinishMatchAsync(QualifierMatch match)
        {
            if (!m_arenics.TryGetValue(match.Player1.Identity, out var arenic) 
                || arenic.Date.Date != DateTime.Now.Date)
            {
                m_arenics.TryRemove(match.Player1.Identity, out _);
                m_arenics.TryAdd(match.Player1.Identity, arenic = new DbArenic
                {
                    UserId = match.Player1.Identity,
                    Date = DateTime.Now.Date
                });
            }

            arenic.AthletePoint = match.Player1.QualifierPoints;
            arenic.DayWins = match.Player1.QualifierDayWins;
            arenic.DayLoses = match.Player1.QualifierDayLoses;
            arenic.CurrentHonor = match.Player1.HonorPoints;
            arenic.HistoryHonor = match.Player1.HistoryHonorPoints;

            await BaseRepository.SaveAsync(arenic);

            if (!m_arenics.TryGetValue(match.Player2.Identity, out arenic)
                || arenic.Date.Date != DateTime.Now.Date)
            {
                m_arenics.TryRemove(match.Player2.Identity, out _);
                m_arenics.TryAdd(match.Player2.Identity, arenic = new DbArenic
                {
                    UserId = match.Player2.Identity,
                    Date = DateTime.Now.Date
                });
            }

            arenic.AthletePoint = match.Player2.QualifierPoints;
            arenic.DayWins = match.Player2.QualifierDayWins;
            arenic.DayLoses = match.Player2.QualifierDayLoses;
            arenic.CurrentHonor = match.Player2.HonorPoints;
            arenic.HistoryHonor = match.Player2.HistoryHonorPoints;

            await BaseRepository.SaveAsync(arenic);
        }

        public async Task<QualifierUser> FindTargetAsync(QualifierUser request)
        {
            List<QualifierUser> possibleTargets = new List<QualifierUser>();
            foreach (var target in m_awaitingQueue.Values
                .Where(x => x.Identity != request.Identity
                    && IsMatchEnable(request.Grade, x.Grade)))
                possibleTargets.Add(target);

            if (possibleTargets.Count == 0)
                return null;

            return possibleTargets[await Kernel.NextAsync(possibleTargets.Count) % possibleTargets.Count];
        }

        #endregion

        #region User Management

        public int PlayersOnQueue => m_awaitingQueue.Count;

        public QualifierUser FindInQueue(uint idUser) => m_awaitingQueue.TryGetValue(idUser, out var value) ? value : null;

        public QualifierUser EnterQueue(Character user)
        {
            if (FindInQueue(user.Identity) != null)
                return null;

            QualifierUser queueUser = new QualifierUser
            {
                Identity = user.Identity,
                Name = user.Name,
                Level = user.Level,
                Points = (int) user.QualifierPoints,
                PreviousPkMode = user.PkMode,
                Profession = user.Profession
            };
            if (m_awaitingQueue.TryAdd(user.Identity, queueUser))
            {
                user.QualifierStatus = MsgQualifyingDetailInfo.ArenaStatus.WaitingForOpponent;
                return queueUser;
            }
            return null;
        }

        public QualifierUser LeaveQueue(uint idUser)
        {
            if (m_awaitingQueue.TryRemove(idUser, out var result))
            {
                Character user = Kernel.RoleManager.GetUser(idUser);
                if (user != null)
                    user.QualifierStatus = MsgQualifyingDetailInfo.ArenaStatus.NotSignedUp;
                return result;
            }
            return null;
        }

        public bool HasUser(uint idUser)
        {
            return FindInQueue(idUser) != null || FindMatch(idUser) != null;
        }

        public bool IsInsideMatch(uint idUser) => FindMatch(idUser) != null;

        #endregion

        #region Default Data

        public static bool IsMatchEnable(int userGrade, int targetGrade)
        {
            int nDelta = userGrade - targetGrade;
            if (nDelta < 0)
                nDelta *= -1;
            return nDelta < 2;
        }

        public static uint GetInitialPoints(byte level)
        {
            if (level < MIN_LEVEL)
                return 0;
            if (level < 90)
                return m_dwStartPoints[0];
            if (level < 100)
                return m_dwStartPoints[1];
            if (level < 110)
                return m_dwStartPoints[2];
            if (level < 120)
                return m_dwStartPoints[3];
            return m_dwStartPoints[4];
        }

        #endregion

        #region Rankings

        public int GetPlayerRanking(uint idUser)
        {
            int rank = 0;
            foreach (var arenic in m_arenics.Values
                .Where(x => x.Date == DateTime.Now.Date)
                .OrderByDescending(x => x.AthletePoint)
                .ThenByDescending(x => x.DayWins)
                .ThenBy(x => x.DayLoses))
            {
                if (idUser == arenic.UserId)
                    return rank + 1;
                rank++;
            }
            return 0;
        }

        public bool GetReward(uint idUser, out HonorRewards value)
        {
            if (m_rewards.TryGetValue(GetPlayerRanking(idUser), out var result))
            {
                value = result;
                return true;
            }
            value = default;
            return false;
        }

        #endregion

        #region Socket

        public static Task SendArenaInformationAsync(Character target)
        {
            return target.SendAsync(new MsgQualifyingDetailInfo
            {
                Ranking = target.QualifierRank,
                CurrentHonor = target.HonorPoints,
                HistoryHonor = target.HistoryHonorPoints,
                Points = target.QualifierPoints,
                TodayWins = target.QualifierDayWins,
                TodayLoses = target.QualifierDayLoses,
                TotalWins = target.QualifierHistoryWins,
                TotalLoses = target.QualifierHistoryLoses,
                Status = target.QualifierStatus,
                TriumphToday9 = (byte) Math.Min(9, target.QualifierDayWins),
                TriumphToday20 = (byte) Math.Min(20, target.QualifierDayGames)
            });
        }

        #endregion

        public sealed class QualifierMatch
        {
            public enum MatchStatus
            {
                Awaiting,
                Running,
                Finished,
                ReadyToDispose
            }

            private TimeOut m_startMatch = new TimeOut();
            private TimeOut m_leaveMap = new TimeOut();
            private TimeOut m_confirmation = new TimeOut();
            private TimeOut m_matchTime = new TimeOut();

            public GameMap Map { get; private set; }
            public uint Winner { get; private set; }

            public bool Accepted1 { get; set; }
            public Character Player1 { get; private set; }
            public int Points1 { get; set; }
            public int Cheers1 { get; set; }
            public QualifierUser Object1 { get; set; }

            public bool Accepted2 { get; set; }
            public Character Player2 { get; private set; }
            public int Points2 { get; set; }
            public int Cheers2 { get; set; }
            public QualifierUser Object2 { get; set; }

            public uint MapIdentity { get; private set; }

            public MatchStatus Status { get; private set; } = MatchStatus.Awaiting;

            public bool InvitationExpired => m_confirmation.IsActive() && m_confirmation.IsTimeOut();
            public bool IsRunning => Status == MatchStatus.Running;
            public bool IsAttackEnable => m_startMatch.IsActive() && m_startMatch.IsTimeOut() && Status == MatchStatus.Running;
            public bool TimeOut => m_matchTime.IsActive() && m_matchTime.IsTimeOut();

            ~QualifierMatch()
            {
                if (MapIdentity > 0)
                    MapIdentityGenerator.ReturnIdentity(MapIdentity);
            }

            public async Task<bool> CreateAsync(Character user1, QualifierUser obj1, Character user2, QualifierUser obj2)
            {
                ArenaQualifier qualifier = Kernel.EventThread.GetEvent<ArenaQualifier>();
                if (qualifier == null)
                    return false;

                if (!qualifier.IsAllowedToJoin(user1) || !qualifier.IsAllowedToJoin(user2))
                    return false;

                MapIdentity = (uint) MapIdentityGenerator.GetNextIdentity;

                Player1 = user1;
                Object1 = obj1;
                Player2 = user2;
                Object2 = obj2;

                MsgQualifyingInteractive msg = new MsgQualifyingInteractive
                {
                    Interaction = MsgQualifyingInteractive.InteractionType.Countdown,
                    Identity = (uint) MsgQualifyingDetailInfo.ArenaStatus.WaitingInactive
                };

                await Player1.SendAsync(msg);
                await Player2.SendAsync(msg);

                m_confirmation.Startup(60);
                return true;
            }

            public async Task<bool> StartAsync()
            {
                m_confirmation.Clear();

                ArenaQualifier qualifier = Kernel.EventThread.GetEvent<ArenaQualifier>();
                if (qualifier == null)
                    return false;
                
                Map = new GameMap(new DbDynamap
                {
                    Identity = MapIdentity,
                    Name = "ArenaQualifier",
                    Description = $"{Player1.Name} x {Player2.Name}`s map",
                    Type = (uint) qualifier.Map.Type,
                    OwnerIdentity = Player1.Identity,
                    LinkMap = 1002,
                    LinkX = 430,
                    LinkY = 378,
                    MapDoc = qualifier.Map.MapDoc,
                    OwnerType = 1
                });

                if (!await Map.InitializeAsync())
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Could not initialize map for arena qualifier!!");
                    return false;
                }

                Kernel.MapManager.AddMap(Map);
                
                if (!await PrepareAsync(Player1, Object1, Player2, Object2))
                {
                    await FinishAsync(Player1, Player2);
                    return false;
                }

                if (!await PrepareAsync(Player2, Object2, Player1, Object1))
                {
                    await FinishAsync(Player2, Player1);
                    return false;
                }

                await MoveToMapAsync(Player1, Player2);
                await MoveToMapAsync(Player2, Player1);

                Status = MatchStatus.Running;
                m_startMatch.Startup(10);
                return true;
            }

            private async Task<bool> PrepareAsync(Character sender, QualifierUser senderObj, Character target, QualifierUser targetObj)
            {
                ArenaQualifier qualifier = Kernel.EventThread.GetEvent<ArenaQualifier>();
                if (qualifier == null)
                    return false;

                if (!qualifier.IsAllowedToJoin(sender))
                    return false;

                await sender.DetachAllStatusAsync();

                if (!sender.IsAlive)
                    await sender.RebornAsync(false, true);

                if (!sender.Map.IsRecordDisable())
                    await sender.SavePositionAsync(sender.MapIdentity, sender.MapX, sender.MapY);

                senderObj.PreviousPkMode = sender.PkMode;
                await sender.SetPkModeAsync(PkModeType.FreePk);
                
                return true;
            }

            public async Task MoveToMapAsync(Character sender, Character target)
            {
                int x = 32 + await Kernel.NextAsync(37);
                int y = 32 + await Kernel.NextAsync(37);

                await sender.FlyMapAsync(MapIdentity, x, y);

                await sender.SendAsync(new MsgQualifyingInteractive
                {
                    Interaction = MsgQualifyingInteractive.InteractionType.StartTheFight,
                    Identity = target.Identity,
                    Rank = target.QualifierRank,
                    Name = target.Name,
                    Level = target.Level,
                    Profession = target.Profession,
                    Points = (int) target.QualifierPoints
                });

                await sender.SendAsync(new MsgArenicScore
                {
                    Identity1 = Player1.Identity,
                    Name1 = Player1.Name,
                    Damage1 = Points1,
                    
                    Identity2 = Player2.Identity,
                    Name2 = Player2.Name,
                    Damage2 = Points2
                });

                await sender.SendAsync(new MsgQualifyingInteractive
                {
                    Interaction = MsgQualifyingInteractive.InteractionType.Match,
                    Option = 5
                });

                await sender.SetAttributesAsync(ClientUpdateType.Hitpoints, sender.MaxLife);
                await sender.SetAttributesAsync(ClientUpdateType.Mana, sender.MaxMana);
                await sender.SetAttributesAsync(ClientUpdateType.Stamina, sender.MaxEnergy);
                await sender.ClsXpValAsync();
            }

            public async Task DestroyAsync(uint idDisconnect = 0, bool notStarted = false)
            {
                Character temp = Kernel.RoleManager.GetUser(Player1.Identity);
                if (Player1.Identity != idDisconnect && temp != null)
                {
                    if (!notStarted)
                    {
                        if (Player1 != null && Player1.MapIdentity == Map.Identity)
                            await Player1.FlyMapAsync(Player1.RecordMapIdentity, Player1.RecordMapX,
                                Player1.RecordMapY);

                        if (!Player1.IsAlive)
                        {
                            await Player1.RebornAsync(false, true);
                        }
                        else
                        {
                            await Player1.SetAttributesAsync(ClientUpdateType.Hitpoints, Player1.MaxLife);
                            await Player1.SetAttributesAsync(ClientUpdateType.Mana, Player1.MaxMana);
                        }

                        await Player1.SetPkModeAsync();
                    }
                    
                    await Player1.SendAsync(new MsgQualifyingInteractive
                    {
                        Interaction = MsgQualifyingInteractive.InteractionType.Dialog,
                        Option = Winner == Player1.Identity ? 1 : 0
                    });
                }

                temp = Kernel.RoleManager.GetUser(Player2.Identity);
                if (Player2.Identity != idDisconnect && temp != null)
                {
                    if (!notStarted)
                    {
                        if (Player2 != null && Player2.MapIdentity == Map.Identity)
                            await Player2.FlyMapAsync(Player2.RecordMapIdentity, Player2.RecordMapX,
                                Player2.RecordMapY);

                        if (!Player2.IsAlive)
                        {
                            await Player2.RebornAsync(false, true);
                        }
                        else
                        {
                            await Player2.SetAttributesAsync(ClientUpdateType.Hitpoints, Player2.MaxLife);
                            await Player2.SetAttributesAsync(ClientUpdateType.Mana, Player2.MaxMana);
                        }

                        await Player2.SetPkModeAsync();
                    }

                    await Player2.SendAsync(new MsgQualifyingInteractive
                    {
                        Interaction = MsgQualifyingInteractive.InteractionType.Dialog,
                        Option = Winner == Player2.Identity ? 1 : 0
                    });
                }

                if (Map != null)
                {
                    Kernel.MapManager.RemoveMap(Map.Identity);
                }

                MapIdentityGenerator.ReturnIdentity(MapIdentity);

                Status = MatchStatus.ReadyToDispose;
            }

            public Task FinishAsync()
            {
                if (Points1 == 0 && Points1 == Points2)
                    return DrawAsync();
                return Points1 > Points2 ? FinishAsync(Player1, Player2) : FinishAsync(Player2, Player1);
            }

            public async Task FinishAsync(Character winner, Character loser, uint disconnect = 0)
            {
                bool force = Status != MatchStatus.Running;

                winner ??= Player1.Identity == loser.Identity ? Player2 : Player1;

                Status = MatchStatus.Finished;
                m_leaveMap.Startup(3);

                Winner = winner.Identity;

                winner.QualifierPoints = (uint) (winner.QualifierPoints * 1.03d);
                winner.QualifierDayWins++;
                winner.QualifierHistoryWins++;

                if (winner.QualifierDayWins == 9)
                {
                    winner.HonorPoints += TRIUMPH_HONOR_REWARD;
                    winner.HistoryHonorPoints += TRIUMPH_HONOR_REWARD;
                    await winner.UserPackage.AwardItemAsync(723912);
                }

                if (winner.QualifierDayGames == 20)
                {
                    winner.HonorPoints += TRIUMPH_HONOR_REWARD;
                    winner.HistoryHonorPoints += TRIUMPH_HONOR_REWARD;
                    await winner.UserPackage.AwardItemAsync(723912);
                }

                //await winner.SendAsync(new MsgQualifyingInteractive
                //{
                //    Interaction = MsgQualifyingInteractive.InteractionType.Dialog,
                //    Option = 1,
                //    Identity = winner.Identity,
                //    Name = winner.Name,
                //    Rank = winner.QualifierRank,
                //    Points = (int) winner.QualifierPoints,
                //    Level = winner.Level,
                //    Profession = winner.Profession
                //});

                loser.QualifierPoints = (uint)(loser.QualifierPoints * 0.975d);
                loser.QualifierDayLoses++;
                loser.QualifierHistoryLoses++;

                if (loser.QualifierDayGames == 20)
                {
                    loser.HonorPoints += TRIUMPH_HONOR_REWARD;
                    loser.HistoryHonorPoints += TRIUMPH_HONOR_REWARD;

                    await loser.UserPackage.AwardItemAsync(723912);
                }

                //await loser.SendAsync(new MsgQualifyingInteractive
                //{
                //    Interaction = MsgQualifyingInteractive.InteractionType.Dialog,
                //    Option = 3,
                //    Identity = loser.Identity,
                //    Name = loser.Name,
                //    Rank = loser.QualifierRank,
                //    Points = (int)loser.QualifierPoints,
                //    Level = loser.Level,
                //    Profession = loser.Profession
                //});

                ArenaQualifier qualifier = Kernel.EventThread.GetEvent<ArenaQualifier>();
                await qualifier.FinishMatchAsync(this);

                string strWin = "";
                string strLose = "";

                if (winner.SyndicateIdentity != 0)
                    strWin += $"({winner.SyndicateName}) ";
                strWin += winner.Name;

                if (loser.SyndicateIdentity != 0)
                    strLose += $"({loser.SyndicateName}) ";
                strLose += loser.Name;

                await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrArenicMatchEnd, strWin, strLose, winner.QualifierRank), MsgTalk.TalkChannel.Qualifier);

                if (force)
                    await DestroyAsync(disconnect, true);
            }

            /// <remarks>Both lose!</remarks>
            public async Task DrawAsync()
            {
                bool force = Status != MatchStatus.Running;

                Status = MatchStatus.Finished;
                m_leaveMap.Startup(3);

                Player1.QualifierPoints = (uint)(Player1.QualifierPoints * 0.975d);
                Player1.QualifierDayLoses++;
                Player1.QualifierHistoryLoses++;

                if (Player1.QualifierDayGames == 20)
                {
                    Player1.HonorPoints += TRIUMPH_HONOR_REWARD;
                    Player1.HistoryHonorPoints += TRIUMPH_HONOR_REWARD;
                    await Player1.UserPackage.AwardItemAsync(723912);
                }

                await Player1.SendAsync(new MsgQualifyingInteractive
                {
                    Interaction = MsgQualifyingInteractive.InteractionType.Dialog,
                    Option = 3
                });

                Player2.QualifierPoints = (uint)(Player2.QualifierPoints * 0.975d);
                Player2.QualifierDayLoses++;
                Player2.QualifierHistoryLoses++;

                if (Player2.QualifierDayGames == 20)
                {
                    Player2.HonorPoints += TRIUMPH_HONOR_REWARD;
                    Player2.HistoryHonorPoints += TRIUMPH_HONOR_REWARD;
                    await Player2.UserPackage.AwardItemAsync(723912);
                }

                await Player2.SendAsync(new MsgQualifyingInteractive
                {
                    Interaction = MsgQualifyingInteractive.InteractionType.Dialog,
                    Option = 3
                });

                ArenaQualifier qualifier = Kernel.EventThread.GetEvent<ArenaQualifier>();
                await qualifier.FinishMatchAsync(this);

                string strLose1 = "";
                string strLose2 = "";

                if (Player1.SyndicateIdentity != 0)
                    strLose1 += $"({Player1.SyndicateName}) ";
                strLose1 += Player1.Name;

                if (Player2.SyndicateIdentity != 0)
                    strLose2 += $"({Player2.SyndicateName}) ";
                strLose2 += Player2.Name;

                await Kernel.RoleManager.BroadcastMsgAsync(string.Format(Language.StrArenicMatchDraw, strLose1, strLose2), MsgTalk.TalkChannel.Qualifier);

                if (force)
                    await DestroyAsync(0, true);
            }

            public async Task OnTimerAsync()
            {
                if (m_confirmation.IsActive() && m_confirmation.IsTimeOut())
                {
                    if (!Accepted1 && !Accepted2)
                    {
                        await DrawAsync();
                    }
                    else if (!Accepted1)
                    {
                        await FinishAsync(Player2, Player1);
                    }
                    else if (!Accepted2)
                    {
                        await FinishAsync(Player1, Player2);
                    }
                    return;
                }

                if (m_startMatch.IsActive() && m_startMatch.IsActive() && !m_matchTime.IsActive())
                    m_matchTime.Startup(180);

                if (Status == MatchStatus.ReadyToDispose)
                    return; // do nothing :]

                if (Status == MatchStatus.Running && TimeOut)
                {
                    await FinishAsync();
                    return; // finish match now!
                }

                if (m_leaveMap.IsActive() && m_leaveMap.IsTimeOut())
                    await DestroyAsync();
            }

            public Task SendBoardAsync() => SendAsync(new MsgArenicScore
            {
                Identity1 = Player1.Identity,
                Name1 = Player1.Name,
                Damage1 = Points1,

                Identity2 = Player2.Identity,
                Name2 = Player2.Name,
                Damage2 = Points2
            });

            public Task SendAsync(IPacket msg) => Map.BroadcastMsgAsync(msg);
        }

        public class QualifierUser
        {
            public QualifierUser()
            {
                JoinTime = DateTime.Now;
            }

            public uint Identity { get; set; }
            public string Name { get; set; }
            public int Level { get; set; }
            public int Profession { get; set; }
            public PkModeType PreviousPkMode { get; set; }
            public int Points { get; set; }
            public int Grade
            {
                get
                {
                    if (Points >= 4000)
                        return 5;
                    if (Points >= 3300 && Points < 4000)
                        return 4;
                    if (Points >= 2800 && Points < 3300)
                        return 3;
                    if (Points >= 2200 && Points < 2800)
                        return 2;
                    if (Points >= 1500 && Points < 2200)
                        return 1;
                    return 0;
                }
            }
            public DateTime JoinTime { get; }
        }

        public struct HonorRewards
        {
            public ushort Rank;
            public int Honor;
            public int MonthlyHonor;
        }
    }
}