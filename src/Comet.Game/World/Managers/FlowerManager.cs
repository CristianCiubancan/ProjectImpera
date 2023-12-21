// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - FlowerManager.cs
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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Packets;
using Comet.Game.States;

#endregion

namespace Comet.Game.World.Managers
{
    public class FlowerManager
    {
        private ConcurrentDictionary<uint, FlowerRankObject> m_dicFlowers = new ConcurrentDictionary<uint, FlowerRankObject>();

        public async Task<bool> InitializeAsync()
        {
            foreach (var flower in await DbFlower.GetAsync())
            {
                FlowerRankObject obj = new FlowerRankObject(flower);
                m_dicFlowers.TryAdd(flower.UserId, obj);
            }
            return true;
        }

        public static async Task<List<FlowerRankingStruct>> GetFlowerRankingAsync(MsgFlower.FlowerType type, int from = 0, int limit = 10)
        {
            DataTable query = await BaseRepository.SelectAsync($"CALL QueryFlowerRanking({(int)type},{limit},{from})");
            List<FlowerRankingStruct> result = new List<FlowerRankingStruct>();

            foreach (DataRow row in query.Rows)
            {
                var item = new FlowerRankingStruct
                {
                    Identity = uint.Parse(row["id"]?.ToString() ?? "0"),
                    Name = row["name"].ToString(),
                    Profession = ushort.Parse(row["profession"]?.ToString() ?? "0"),
                    Position = int.Parse(row["rank"]?.ToString() ?? "0")
                };

                switch (type)
                {
                    case MsgFlower.FlowerType.RedRose:
                        item.Value = uint.Parse(row["rose"]?.ToString() ?? "0");
                        break;
                    case MsgFlower.FlowerType.WhiteRose:
                        item.Value = uint.Parse(row["lily"]?.ToString() ?? "0");
                        break;
                    case MsgFlower.FlowerType.Orchid:
                        item.Value = uint.Parse(row["orchid"]?.ToString() ?? "0");
                        break;
                    case MsgFlower.FlowerType.Tulip:
                        item.Value = uint.Parse(row["tulip"]?.ToString() ?? "0");
                        break;
                }

                result.Add(item);
            }

            return result;
        }

        public List<FlowerRankingStruct> GetFlowerRankingToday(MsgFlower.FlowerType type, int from = 0, int limit = 10)
        {
            int position = 1;
            switch (type)
            {
                case MsgFlower.FlowerType.RedRose:
                    return m_dicFlowers.Values.Where(x => ((x.Mesh%10000) - (x.Mesh%10)) / 1000 == 2 && x.RedRose > 0)
                        .OrderByDescending(x => x.RedRose).Skip(from)
                        .Take(limit).Select(x => new FlowerRankingStruct
                        {
                            Identity = x.UserIdentity,
                            Name = x.Name,
                            Profession = (ushort) x.Profession,
                            Value = x.RedRose,
                            Position = position++
                        }).ToList();

                case MsgFlower.FlowerType.WhiteRose:
                    return m_dicFlowers.Values.Where(x => ((x.Mesh % 10000) - (x.Mesh % 10)) / 1000 == 2 && x.WhiteRose > 0)
                        .OrderByDescending(x => x.WhiteRose)
                        .Skip(from).Take(limit).Select(x => new FlowerRankingStruct
                        {
                            Identity = x.UserIdentity,
                            Name = x.Name,
                            Profession = (ushort) x.Profession,
                            Value = x.WhiteRose,
                            Position = position++
                        }).ToList();

                case MsgFlower.FlowerType.Orchid:
                    return m_dicFlowers.Values.Where(x => ((x.Mesh % 10000) - (x.Mesh % 10)) / 1000 == 2 && x.Orchids > 0)
                        .OrderByDescending(x => x.Orchids).Skip(from)
                        .Take(limit).Select(x => new FlowerRankingStruct
                        {
                            Identity = x.UserIdentity,
                            Name = x.Name,
                            Profession = (ushort) x.Profession,
                            Value = x.Orchids,
                            Position = position++
                        }).ToList();

                case MsgFlower.FlowerType.Tulip:
                    return m_dicFlowers.Values.Where(x => ((x.Mesh % 10000) - (x.Mesh % 10)) / 1000 == 2 && x.Tulips > 0)
                        .OrderByDescending(x => x.Tulips).Skip(from)
                        .Take(limit).Select(x => new FlowerRankingStruct
                        {
                            Identity = x.UserIdentity,
                            Name = x.Name,
                            Profession = (ushort) x.Profession,
                            Value = x.Tulips,
                            Position = position++
                        }).ToList();
            }

            return new List<FlowerRankingStruct>();
        }

        public async Task<FlowerRankObject> QueryFlowersAsync(Character user)
        {
            if (m_dicFlowers.TryGetValue(user.Identity, out var value))
                return value;
            if (m_dicFlowers.TryAdd(user.Identity, value = new FlowerRankObject(user)))
            {
                await BaseRepository.SaveAsync(value.GetDatabaseObject());
                return value;
            }
            return null;
        }

        public async Task DailyResetAsync()
        {
            await BaseRepository.DeleteAsync(m_dicFlowers.Values.Select(x => x.GetDatabaseObject()).ToList());
            m_dicFlowers.Clear();
        }

        public class FlowerRankObject
        {
            private DbFlower m_flower;

            public FlowerRankObject(DbFlower flower)
            {
                m_flower = flower;

                if (flower.User == null)
                    return;

                Mesh = flower.User.Mesh;
                Name = flower.User.Name;
                Level = flower.User.Level;
                Profession = flower.User.Profession;
                Metempsychosis = flower.User.Rebirths;
            }

            public FlowerRankObject(Character user)
            {
                m_flower = new DbFlower
                {
                    UserId = user.Identity
                };

                Mesh = user.Mesh;
                Name = user.Name;
                Level = user.Level;
                Metempsychosis = user.Metempsychosis;
                Profession = user.Profession;
            }

            public uint UserIdentity => m_flower.UserId;
            public uint Mesh { get; }
            public string Name { get; }
            public int Level { get; }
            public int Profession { get; }
            public int Metempsychosis { get; }

            public uint RedRose { get => m_flower.RedRose; set => m_flower.RedRose = value; }
            public uint WhiteRose { get => m_flower.WhiteRose; set => m_flower.WhiteRose = value; }
            public uint Orchids { get => m_flower.Orchids; set => m_flower.Orchids = value; }
            public uint Tulips { get => m_flower.Tulips; set => m_flower.Tulips = value; }

            public Task SaveAsync() => BaseRepository.SaveAsync(m_flower);

            public DbFlower GetDatabaseObject() => m_flower;
        }

        public struct FlowerRankingStruct
        {
            public int Position;
            public uint Identity;
            public string Name;
            public ushort Profession;
            public uint Value;
        }
    }
}