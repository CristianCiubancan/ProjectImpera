// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Event Manager.cs
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

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.States.NPCs;
using Comet.Shared;

namespace Comet.Game.World.Managers
{
    public sealed class EventManager
    {
        private ConcurrentDictionary<uint, DbAction> m_dicActions = new ConcurrentDictionary<uint, DbAction>();
        private ConcurrentDictionary<uint, DbTask> m_dicTasks = new ConcurrentDictionary<uint, DbTask>();

        public async Task<bool> InitializeAsync()
        {
            foreach (var task in await TaskRepository.GetAsync())
            {
                m_dicTasks.TryAdd(task.Id, task);
            }

            foreach (var action in await ActionRepository.GetAsync())
            {
                if (action.Type == 102)
                {
                    string[] response = action.Param.Split(' ');
                    if (response.Length < 2)
                    {
                        await Log.WriteLogAsync(LogLevel.Warning, $"Action [{action.Identity}] Type 102 doesn't set a task [param: {action.Param}]");
                    }
                    else if (response[1] != "0")
                    {
                        if (!uint.TryParse(response[1], out uint taskId) || !m_dicTasks.ContainsKey(taskId))
                        {
                            await Log.WriteLogAsync(LogLevel.Warning, $"Task not found for action {action.Identity}");
                        }
                    }
                }

                m_dicActions.TryAdd(action.Identity, action);
            }

            foreach (var dbNpc in await NpcRepository.GetAsync())
            {
                Npc npc = new Npc(dbNpc);

                if (!await npc.InitializeAsync())
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Could not load NPC {dbNpc.Id} {dbNpc.Name}");
                }

                if (npc.Task0 != 0 && !m_dicTasks.ContainsKey(npc.Task0))
                    await Log.WriteLogAsync(LogLevel.Warning, $"Npc {npc.Identity} {npc.Name} no task found [taskid: {npc.Task0}]");
            }

            foreach (var dbDynaNpc in await DynaNpcRespository.GetAsync())
            {
                DynamicNpc npc = new DynamicNpc(dbDynaNpc);
                if (!await npc.InitializeAsync())
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Could not load NPC {dbDynaNpc.Id} {dbDynaNpc.Name}");
                }

                if (npc.Task0 != 0 && !m_dicTasks.ContainsKey(npc.Task0))
                    await Log.WriteLogAsync(LogLevel.Warning, $"Npc {npc.Identity} {npc.Name} no task found [taskid: {npc.Task0}]");
            }

            return true;
        }

        public async Task ReloadActionTaskAllAsync()
        {
            m_dicTasks.Clear();
            foreach (var task in await TaskRepository.GetAsync())
            {
                m_dicTasks.TryAdd(task.Id, task);
            }
            await Log.WriteLogAsync(LogLevel.Debug, $"All Tasks has been reloaded. {m_dicTasks.Count} in the server.");

            m_dicActions.Clear();
            foreach (var action in await ActionRepository.GetAsync())
            {
                if (action.Type == 102)
                {
                    string[] response = action.Param.Split(' ');
                    if (response.Length < 2)
                    {
                        await Log.WriteLogAsync(LogLevel.Warning, $"Action [{action.Identity}] Type 102 doesn't set a task [param: {action.Param}]");
                    }
                    else if (response[1] != "0")
                    {
                        if (!uint.TryParse(response[1], out uint taskId) || !m_dicTasks.ContainsKey(taskId))
                        {
                            await Log.WriteLogAsync(LogLevel.Warning, $"Task not found for action {action.Identity}");
                        }
                    }
                }

                m_dicActions.TryAdd(action.Identity, action);
            }

            await Log.WriteLogAsync(LogLevel.Debug, $"All Actions has been reloaded. {m_dicActions.Count} in the server.");
        }

        public DbAction GetAction(uint idAction)
        {
            return m_dicActions.TryGetValue(idAction, out var result) ? result : null;
        }

        public DbTask GetTask(uint idTask)
        {
            return m_dicTasks.TryGetValue(idTask, out var result) ? result : null;
        }
    }
} 