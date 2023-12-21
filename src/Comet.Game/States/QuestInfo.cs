using Comet.Shared;
using Microsoft.Extensions.Configuration.Ini;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comet.Game.States
{
    public sealed class QuestInfo
    {
        private static Dictionary<int, string> m_questInfoType = new();
        private static Dictionary<int, QuestInfo> m_questInfo = new();

        public static async Task InitializeAsync()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "ini", "QuestInfo.ini");
            if (!File.Exists(path))
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"File '{path}' is missing");
                return;
            }

            IniConfigurationSource source = new();
            source.Path = path;
            source.ReloadOnChange = false;
            IniConfigurationProvider reader = new(source);
            try
            {
                reader.Load(new FileStream(path, FileMode.Open, FileAccess.Read));
            }
            catch
            {
                await Log.WriteLogAsync(LogLevel.Error, $"An error ocurred while reading '{path}'. Check for duplicated values or parsing errors! Startup will continue.");
                return;
            }

            if (reader.TryGet("TaskType:Num", out var strTaskTypeNum) 
                && int.TryParse(strTaskTypeNum, out var taskTypeNum))
            {
                for (int i = 1; i <= taskTypeNum; i++)
                {
                    if (!reader.TryGet($"TaskType:TypeId{i}", out var strId)
                        || !int.TryParse(strId, out var id)
                        || !reader.TryGet($"TaskType:TypeName{i}", out var name))
                        continue;

                    if (m_questInfoType.ContainsKey(id))
                        continue;

                    m_questInfoType.Add(id, name);
                }
            }

            if (!reader.TryGet("TotalMission:TotalMission", out var strTotalMission)
                || !int.TryParse(strTotalMission, out var totalMission))
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"No total mission found.");
                return;
            }

            for (int i = 1; i <= totalMission; i++)
            {
                QuestInfo questInfo = new();

                if (!reader.TryGet($"{i}:TypeId", out string strTypeId)
                    || !int.TryParse(strTypeId, out var typeId))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid TypeId for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:TaskNameColor", out string strTaskNameColor))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid TaskNameColor for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:CompleteFlag", out string strCompleteFlag)
                    || !int.TryParse(strCompleteFlag, out var completeFlag))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid CompleteFlag for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:ActivityType", out string strActivityType)
                    || !int.TryParse(strActivityType, out var activityType))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid ActivityType for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:MissionId", out string strMissionId)
                    || !int.TryParse(strMissionId, out var missionId))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid MissionId for QuestInfo [{i}]");
                    continue;
                }

                if (m_questInfo.ContainsKey(missionId))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Duplicate of missionId {missionId} on [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:Name", out string strName))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Name for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:Lv_min", out string strLevelMin)
                    || !int.TryParse(strLevelMin, out var levelMin))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Lv_min for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:Lv_max", out string strLevelMax)
                    || !int.TryParse(strLevelMax, out var levelMax))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Lv_max for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:Auto", out string strAuto)
                    || !int.TryParse(strAuto, out var auto))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Auto for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:First", out string strFirst)
                    || !int.TryParse(strFirst, out var first))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid First for QuestInfo [{i}]");
                    continue;
                }

                int[] preQuests = new[] { 0 };
                if (reader.TryGet($"{i}:Prequest", out string strPrequest))
                {
                    string[] splitPrequests = strPrequest.Split('|');
                    preQuests = splitPrequests.Select(x =>
                    {
                        return int.TryParse(x, out int value) ? value : 0;
                    }).ToArray();
                }

                if (!reader.TryGet($"{i}:Map", out string strMap)
                    || !int.TryParse(strMap, out var map))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Map for QuestInfo [{i}]");
                    continue;
                }

                int[] professions = new [] { 0 };
                if (reader.TryGet($"{i}:Profession", out string strProfessions))
                {
                    string[] splitProf = strProfessions.Split(',');
                    professions = splitProf.Select(x =>
                    {
                        return int.TryParse(x, out int value) ? value : 0;
                    }).ToArray();
                }

                if (!reader.TryGet($"{i}:FinishTime", out string stFinishTime)
                    || !int.TryParse(stFinishTime, out var finishTime))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid FinishTime for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:ActivityBeginTime", out string strActivityBeginTime)
                    || !int.TryParse(strActivityBeginTime, out var activityBeginTime))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid ActivityBeginTime for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:ActivityEndTime", out string strActivityEndTime)
                    || !int.TryParse(strActivityEndTime, out var activityEndTime))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid ActivityEndTime for QuestInfo [{i}]");
                    continue;
                }

                NpcInfo beginNpcInfo = default;
                if (reader.TryGet($"{i}:BeginNpcId", out string strBeginNpcInfo))
                {
                    string[] info = strBeginNpcInfo.Split(',');
                    if (info.Length >= 6)
                    {
                        
                    }
                }

                NpcInfo endNpcInfo = default;
                if (reader.TryGet($"{i}:FinishNpcId", out string strFinishNpcInfo))
                {

                }

                if (!reader.TryGet($"{i}:Prize", out string strPrize))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Prize for QuestInfo [{i}]");
                    continue;
                }

                if (!reader.TryGet($"{i}:IntentionDesp", out string strIntentionDesp))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid IntentionDesp for QuestInfo [{i}]");
                    continue;
                }

                string[] intents = { };
                if (reader.TryGet($"{i}:IntentAmount", out string strIntentAmount)
                    && int.TryParse(strIntentAmount, out var intentAmount))
                {

                    intents = new string[intentAmount];
                    for (int intent = 1; intent < intentAmount; intent++)
                    {
                        if (reader.TryGet($"{i}:Intention{intent}", out var strIntention))
                            intents[intent] = strIntention;
                    }

                }

                if (!reader.TryGet($"{i}:Content", out string strContent))
                {
                    await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Content for QuestInfo [{i}]");
                    continue;
                }
            }
        }

        public int TypeId { get; set; }
        public string TaskNameColor { get; set; }
        public int CompleteFlag { get; set; }
        public int ActivityType { get; set; }
        public int MissionId { get; set; }
        public string Name { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public bool Auto { get; set; }
        public bool First { get; set; }
        public int[] PreQuest { get; set; }
        public uint MapId { get; set; }
        public int[] Profession { get; set; }
        public int Sex { get; set; }
        public int FinishTime { get; set; }
        public int ActivityBeginTime { get; set; }
        public int ActivityEndTime { get; set; }
        public NpcInfo BeginNpc { get; set; }
        public NpcInfo EndNpc { get; set; }
        public string Prize { get; set; }
        public string IntentionDesp { get; set; }
        public string IntentAmount { get; set; }
        public string[] Intent { get; set; }
        public string Content { get; set; }

        public struct NpcInfo
        {
            public uint Id;
            public uint Map;
            public ushort X;
            public ushort Y;
            public string Name;
            public string MapName;
        }
    }    
}