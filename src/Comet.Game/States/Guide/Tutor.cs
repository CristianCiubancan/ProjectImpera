// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Mentor.cs
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database;
using Comet.Game.Database.Models;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.Syndicates;
using Comet.Shared;

namespace Comet.Game.States.Guide
{
    public sealed class Tutor
    {
        public const int BETRAYAL_FLAG_TIMEOUT = 60 * 60 * 24 * 3;
        public const int STUDENT_BETRAYAL_VALUE = 50000;

        private TimeOut m_betrayCheck = new TimeOut();

        private DbTutor m_tutor;
        private DbTutorContributions m_access;

        private Tutor() { }
        
        public static async Task<Tutor> CreateAsync(DbTutor tutor)
        {
            var guide = new Tutor
            {
                m_tutor = tutor,
                m_access = await DbTutorContributions.GetGuideAsync(tutor.StudentId)
            };
            guide.m_access ??= new DbTutorContributions
            {
                TutorIdentity = tutor.GuideId,
                StudentIdentity = tutor.StudentId
            };

            DbCharacter dbMentor = await CharactersRepository.FindByIdentityAsync(tutor.GuideId);
            if (dbMentor == null)
                return null;
            guide.GuideName = dbMentor.Name;

            dbMentor = await CharactersRepository.FindByIdentityAsync(tutor.StudentId);
            if (dbMentor == null)
                return null;
            guide.StudentName = dbMentor.Name;

            if (guide.Betrayed)
                guide.m_betrayCheck.Startup(1);

            return guide;
        }

        public uint GuideIdentity => m_tutor.GuideId;
        public string GuideName { get; private set; }

        public uint StudentIdentity => m_tutor.StudentId;
        public string StudentName { get; private set; }

        public bool Betrayed => m_tutor.BetrayalFlag != 0;
        public bool BetrayalCheck => Betrayed && m_betrayCheck.IsActive() && m_betrayCheck.ToNextTime();

        public Character Guide => Kernel.RoleManager.GetUser(m_tutor.GuideId);
        public Character Student => Kernel.RoleManager.GetUser(m_tutor.StudentId);

        public async Task<bool> AwardTutorExperienceAsync(uint addExpTime)
        {
            m_access.Experience += addExpTime;

            Character user = Kernel.RoleManager.GetUser(m_access.TutorIdentity);
            if (user != null)
            {
                user.MentorExpTime += addExpTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await DbTutorAccess.GetAsync(m_access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Experience += addExpTime;
                await BaseRepository.SaveAsync(tutorAccess);
            }
            return await SaveAsync();
        }

        public async Task<bool> AwardTutorGodTimeAsync(ushort addGodTime)
        {
            m_access.GodTime += addGodTime;

            Character user = Kernel.RoleManager.GetUser(m_access.TutorIdentity);
            if (user != null)
            {
                user.MentorGodTime += addGodTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await DbTutorAccess.GetAsync(m_access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Blessing += addGodTime;
                await BaseRepository.SaveAsync(tutorAccess);
            }
            return await SaveAsync();
        }

        public async Task<bool> AwardOpportunityAsync(ushort addTime)
        {
            m_access.PlusStone += addTime;

            Character user = Kernel.RoleManager.GetUser(m_access.TutorIdentity);
            if (user != null)
            {
                user.MentorAddLevexp += addTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await DbTutorAccess.GetAsync(m_access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Composition += addTime;
                await BaseRepository.SaveAsync(tutorAccess);
            }
            return await SaveAsync();
        }

        public int SharedBattlePower
        {
            get
            {
                Character mentor = Guide;
                Character student = Student;
                if (mentor == null || student == null)
                    return 0;
                if (mentor.PureBattlePower < student.PureBattlePower)
                    return 0;

                var limit = Kernel.RoleManager.GetTutorBattleLimitType(student.PureBattlePower);
                if (limit == null)
                    return 0;

                var type = Kernel.RoleManager.GetTutorType(mentor.Level);
                if (type == null)
                    return 0;
                
                return (int) Math.Min(limit.BattleLevelLimit, (mentor.PureBattlePower - student.PureBattlePower) * (type.BattleLevelShare / 100f));
            }
        }

        public async Task BetrayAsync()
        {
            m_tutor.BetrayalFlag = UnixTimestamp.Now();
            await SaveAsync();
        }

        public async Task SendTutorAsync()
        {
            if (Student == null)
                return;

            await Student.SendAsync(new MsgGuideInfo
            {
                Identity = StudentIdentity,
                Level = Guide?.Level ?? 0,
                Blessing = m_access.GodTime,
                Composition = (ushort) m_access.PlusStone,
                Experience = m_access.Experience,
                IsOnline = Guide != null,
                Mesh = Guide?.Mesh ?? 0,
                Mode = MsgGuideInfo.RequestMode.Mentor,
                Syndicate = Guide?.SyndicateIdentity ?? 0,
                SyndicatePosition = Guide?.SyndicateRank ?? SyndicateMember.SyndicateRank.None,
                Names = new List<string>
                {
                    GuideName,
                    StudentName,
                    Guide?.MateName ?? Language.StrNone
                },
                EnroleDate = uint.Parse(m_tutor.Date?.ToString("yyyyMMdd") ?? "0"),
                PkPoints = Guide?.PkPoints ?? 0,
                Profession = Guide?.Profession ?? 0,
                SharedBattlePower = (uint) (SharedBattlePower),
                SenderIdentity = GuideIdentity,
                Unknown24 = 999999
            });
        }

        public async Task SendStudentAsync()
        {
            if (Guide == null)
                return;

            await Guide.SendAsync(new MsgGuideInfo
            {
                Identity = StudentIdentity,
                Level = Student?.Level ?? 0,
                Blessing = m_access.GodTime,
                Composition = (ushort)m_access.PlusStone,
                Experience = m_access.Experience,
                IsOnline = Student != null,
                Mesh = Student?.Mesh ?? 0,
                Mode = MsgGuideInfo.RequestMode.Apprentice,
                Syndicate = Student?.SyndicateIdentity ?? 0,
                SyndicatePosition = Student?.SyndicateRank ?? SyndicateMember.SyndicateRank.None,
                Names = new List<string>
                {
                    GuideName,
                    StudentName,
                    Student?.MateName ?? Language.StrNone
                },
                EnroleDate = uint.Parse(m_tutor.Date?.ToString("yyyyMMdd") ?? "0"),
                PkPoints = Student?.PkPoints ?? 0,
                Profession = Student?.Profession ?? 0,
                SharedBattlePower = 0,
                SenderIdentity = GuideIdentity,
                Unknown24 = 999999
            });
        }

        public async Task BetrayalTimerAsync()
        {
            /*
             * Since this will be called in a queue, it might be called twice per run, so we will trigger the TimeOut
             * to see it can be checked.
             */
            if (m_tutor.BetrayalFlag != 0)
            {
                if (m_tutor.BetrayalFlag + BETRAYAL_FLAG_TIMEOUT < UnixTimestamp.Now()) // expired, leave mentor
                {
                    if (Guide != null)
                    {
                        await Guide.SendAsync(string.Format(Language.StrGuideExpelTutor, StudentName));
                        Guide.RemoveApprentice(StudentIdentity);
                    }
                    if (Student != null)
                    {
                        await Student.SendAsync(string.Format(Language.StrGuideExpelStudent, GuideName));
                        await Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, 0, 0);
                        Student.Guide = null;
                    }

                    await DeleteAsync();
                }
            }
        }

        public async Task<bool> SaveAsync()
        {
            return await BaseRepository.SaveAsync(m_tutor) && await BaseRepository.SaveAsync(m_access);
        }

        public async Task<bool> DeleteAsync()
        {
            await BaseRepository.DeleteAsync(m_tutor);
            await BaseRepository.DeleteAsync(m_access);
            return true;
        }
    }
}