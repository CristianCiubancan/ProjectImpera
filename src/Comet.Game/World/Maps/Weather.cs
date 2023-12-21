// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Game - Weather.cs
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
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Packets;
using Comet.Game.States;

#endregion

namespace Comet.Game.World.Maps
{
    public sealed class Weather
    {
        public const int MAX_WEATHER_INTENSITY = 1000;
        public const int MAX_WEATHER_DIR = 360;
        public const int WEATHER_NORMAL_SPEED = 10;
        public const int WEATHER_FAST_SPEED = 5;

        public const int MAX_KEEP_SECS = 1200;
        public const int MIN_KEEP_SECS = 500;
        public const int WERTHER_FINE_PERCENT = 60;
        public const int WERTHER_CHANGE_DIR_PERCENT = 5;

        public const int WEATHER_RAINY_INTENSITY = MAX_WEATHER_INTENSITY / 5;
        public const int WEATHER_SNOWY_INTENSITY = MAX_WEATHER_INTENSITY / 10;
        public const int WEATHER_DEFAULT_INTENSITY = MAX_WEATHER_INTENSITY / 5;
        public const int WEATHER_DEFAULT_DIR = 10;
        public const int WEATHER_DEFAULT_COLOR = 0x00FFFFFF;
        public const int WEATHER_RAINY_SPEED = WEATHER_NORMAL_SPEED;
        private readonly GameMap m_pOwner;

        private readonly TimeOut m_tLoop;
        private int m_nCurrColor;
        private int m_nCurrDir;
        private int m_nCurrIntensity;
        private int m_nCurrParticle;

        private WeatherType m_nCurrType = WeatherType.WeatherFine;
        private int m_nDefaultColor;
        private int m_nDefaultDir;
        private int m_nDefaultIntensity;
        private int m_nDefaultSpeedSecs;

        private WeatherType m_nDefaultType = WeatherType.WeatherFine;

        private int m_nIncrIntensity;
        private int m_nKeepSecs;
        private int m_nSpeedSecs;
        private int m_nTargColor;
        private int m_nTargDir;
        private int m_nTargIntensity;

        private WeatherType m_nTargType = WeatherType.WeatherFine;

        public Weather(GameMap pOwner)
        {
            m_pOwner = pOwner;
            m_tLoop = new TimeOut(1);
            m_tLoop.Startup(1);
        }

        public async Task<bool> CreateAsync(WeatherType nType, int nIntensity, int nDir, int nColor, int nSpeedSecs)
        {
            if (nType <= WeatherType.WeatherNone || nType >= WeatherType.WeatherAll)
                return false;
            if (nIntensity < 0 || nIntensity >= MAX_WEATHER_INTENSITY)
                return false;
            if (nDir < 0 || nDir >= MAX_WEATHER_DIR)
                return false;

            if (nType == WeatherType.WeatherFine || nIntensity == 0)
            {
                nType = WeatherType.WeatherFine;
                nIntensity = 0;
            }

            m_nDefaultType = nType;
            m_nDefaultIntensity = nIntensity;
            m_nDefaultDir = nDir;
            m_nDefaultColor = nColor;
            m_nDefaultSpeedSecs = nSpeedSecs;

            m_nCurrType = nType;
            m_nCurrIntensity = nIntensity;
            m_nCurrDir = nDir;
            m_nCurrColor = nColor;
            m_nCurrParticle = GetParticle(m_nCurrIntensity);

            m_nTargType = nType;
            m_nTargIntensity = nIntensity;
            m_nTargDir = nDir;
            m_nTargColor = nColor;

            m_nIncrIntensity = 0;
            m_nKeepSecs = await Kernel.NextAsync(MAX_KEEP_SECS - MIN_KEEP_SECS + 1) + MIN_KEEP_SECS;
            if (nType == WeatherType.WeatherCloudy)
                m_nKeepSecs *= 5;
            m_nSpeedSecs = WEATHER_NORMAL_SPEED;

            return true;
        }

        public async Task<bool> SetNewWeatherAsync(WeatherType nType, int nIntensity, int nDir, int nColor, int nKeepSecs,
            int nSpeedSecs)
        {
            if (nType <= WeatherType.WeatherNone || nType >= WeatherType.WeatherAll)
                return false;
            if (nIntensity < 0 || nIntensity >= MAX_WEATHER_INTENSITY)
                return false;
            if (nDir < 0 || nDir >= MAX_WEATHER_DIR)
                return false;

            if (nType == WeatherType.WeatherFine || nIntensity == 0)
            {
                nType = WeatherType.WeatherFine;
                nIntensity = 0;
            }

            if (nKeepSecs == 0)
            {
                m_nDefaultType = nType;
                m_nDefaultIntensity = nIntensity;
                m_nDefaultDir = nDir;
                m_nDefaultColor = nColor;
                m_nDefaultSpeedSecs = nSpeedSecs;
            }

            m_nTargType = nType;
            m_nTargIntensity = nIntensity;
            m_nTargDir = nDir;
            m_nTargColor = nColor;

            if (m_nCurrType == WeatherType.WeatherFine || m_nTargType == WeatherType.WeatherFine
                                                       || m_nCurrType == m_nTargType && m_nCurrDir == m_nTargDir &&
                                                       m_nCurrColor == m_nTargColor)
                m_nIncrIntensity = m_nTargIntensity - m_nCurrIntensity;
            else
                m_nIncrIntensity = 0 - m_nCurrIntensity;

            if (nKeepSecs == 0)
            {
                m_nKeepSecs = await Kernel.NextAsync(MAX_KEEP_SECS - MIN_KEEP_SECS + 1) + MIN_KEEP_SECS;
                if (nType == WeatherType.WeatherCloudy)
                    m_nKeepSecs *= 5;
            }
            else
                m_nKeepSecs = nKeepSecs;

            m_nSpeedSecs = nSpeedSecs;

            return true;
        }

        public new WeatherType GetType()
        {
            return m_nCurrType;
        }

        public int GetParticle()
        {
            return m_nCurrParticle;
        }

        public int GetParticle(int nIntensity)
        {
            var nParticle = nIntensity + (int) Math.Sqrt(MAX_WEATHER_INTENSITY);
            if (nParticle >= MAX_WEATHER_INTENSITY)
                nParticle = MAX_WEATHER_INTENSITY - 1;
            nParticle = nParticle * nParticle / MAX_WEATHER_INTENSITY;

            switch (m_nCurrType)
            {
                case WeatherType.WeatherFine:
                    break;
                case WeatherType.WeatherRainy:
                    nParticle = (nParticle + 1) / 2;
                    break;
                case WeatherType.WeatherSnowy:
                    nParticle = (nParticle + 3) / 4;
                    break;
                case WeatherType.WeatherSands:
                    nParticle = (nParticle + 1) / 2;
                    break;
                case WeatherType.WeatherLeaf:
                case WeatherType.WeatherBamboo:
                case WeatherType.WeatherFlower:
                case WeatherType.WeatherFlying:
                case WeatherType.WeatherDandelion:
                    nParticle = (nParticle + 24) / 25;
                    break;
                case WeatherType.WeatherWorm:
                    nParticle = (nParticle + 29) / 30;
                    break;
                case WeatherType.WeatherCloudy:
                    nParticle = (nParticle + 33) / 34;
                    break;
                default:
                    nParticle = 0;
                    break;
            }

            return nParticle;
        }

        public int GetDir()
        {
            return m_nCurrDir;
        }

        public int GetColor()
        {
            return m_nCurrColor;
        }

        public async Task OnTimerAsync()
        {
            if (!m_tLoop.ToNextTime(1))
                return;

            WeatherType nOldType = m_nCurrType;
            int nOldIntensity = m_nCurrIntensity;
            int nOldParticle = m_nCurrParticle;

            if (m_nCurrType == m_nTargType && m_nTargType == m_nDefaultType &&
                m_nDefaultType == WeatherType.WeatherFine)
                return;

            if (m_nCurrType == WeatherType.WeatherFine || m_nTargType == WeatherType.WeatherFine
                                                       || m_nCurrType == m_nTargType && m_nCurrDir == m_nTargDir &&
                                                       m_nCurrColor == m_nTargColor)
            {
                if (m_nCurrType == m_nTargType && (m_nKeepSecs == 0 || --m_nKeepSecs <= 0))
                {
                    WeatherType nType = await Kernel.NextAsync(100) < WERTHER_FINE_PERCENT
                        ? WeatherType.WeatherFine
                        : m_nDefaultType;

                    int nIntensity = m_nDefaultIntensity - MAX_WEATHER_INTENSITY / 4 +
                                     await Kernel.NextAsync(MAX_WEATHER_INTENSITY / 2);
                    if (nIntensity < 1)
                        nIntensity = 1;
                    else if (nIntensity >= MAX_WEATHER_INTENSITY)
                        nIntensity = MAX_WEATHER_INTENSITY - 1;

                    int nDir = nType != nOldType && await Kernel.NextAsync(100) < WERTHER_CHANGE_DIR_PERCENT
                        ? await Kernel.NextAsync(MAX_WEATHER_DIR)
                        : m_nTargDir;
                    int nKeepSecs = await Kernel.NextAsync(MAX_KEEP_SECS - MIN_KEEP_SECS + 1) + MIN_KEEP_SECS;
                    if (nType == WeatherType.WeatherCloudy)
                        nKeepSecs *= 5;

                    await SetNewWeatherAsync(nType, nIntensity, nDir, m_nDefaultColor, nKeepSecs, m_nDefaultSpeedSecs);

                    return;
                }

                if (m_nCurrType == WeatherType.WeatherFine && m_nTargType != WeatherType.WeatherFine)
                {
                    m_nCurrType = m_nTargType;
                    m_nCurrIntensity = 0;
                    m_nCurrDir = m_nTargDir;
                    m_nCurrColor = m_nTargColor;

                    m_nIncrIntensity = m_nTargIntensity - 0;
                }

                if (m_nCurrType != WeatherType.WeatherFine && m_nIncrIntensity > 0 &&
                    m_nCurrIntensity != m_nTargIntensity)
                {
                    m_nCurrIntensity = m_nTargIntensity;

                    if (m_nIncrIntensity > 0 && m_nCurrIntensity > m_nTargIntensity
                        || m_nIncrIntensity < 0 && m_nCurrIntensity < m_nTargIntensity)
                        m_nCurrIntensity = m_nTargIntensity;
                }
            }
            else // return to fine
            {
                if (m_nCurrIntensity != 0)
                {
                    const int times = 1;

                    m_nCurrIntensity = 0;

                    if (m_nCurrIntensity < 0)
                        m_nCurrIntensity = 0;
                }
            }

            if (m_nCurrIntensity != nOldIntensity)
                m_nCurrParticle = GetParticle(m_nCurrIntensity);

            if (m_nCurrType != nOldType || m_nCurrParticle != nOldParticle)
            {
                MsgWeather cMsg = new MsgWeather
                {
                    ColorArgb = (uint) GetColor(),
                    Direction = (uint) GetDir(),
                    Intensity = (uint) GetParticle(),
                    WeatherType = GetType()
                };
                await m_pOwner.BroadcastMsgAsync(cMsg);
            }

            if (m_nCurrType != WeatherType.WeatherFine && m_nCurrIntensity == 0 && m_nIncrIntensity <= 0)
            {
                m_nCurrType = WeatherType.WeatherFine;
                m_nCurrIntensity = 0;
                m_nCurrDir = 0;
                m_nCurrColor = 0;
                m_nCurrParticle = 0;

                m_nIncrIntensity = 0;
            }
        }

        public async Task<bool> SendWeatherAsync(Character pUser = null)
        {
            MsgWeather pMsg = new MsgWeather
            {
                ColorArgb = (uint) GetColor(),
                Direction = (uint) GetDir(),
                Intensity = (uint) GetParticle(),
                WeatherType = GetType()
            };
            if (pUser != null)
                await pUser.SendAsync(pMsg);
            else
                await m_pOwner.BroadcastMsgAsync(pMsg);
            return true;
        }

        public async Task<bool> SendNoWeatherAsync(Character pUser)
        {
            if (pUser == null)
                return false;

            MsgWeather pMsg = new MsgWeather
            {
                WeatherType = WeatherType.WeatherFine,
                ColorArgb = 0,
                Direction = 0,
                Intensity = 0
            };
            await pUser.SendAsync(pMsg);
            return true;
        }

        public enum WeatherType
        {
            WeatherNone = 0,
            WeatherFine,
            WeatherRainy,
            WeatherSnowy,
            WeatherSands,
            WeatherLeaf,
            WeatherBamboo,
            WeatherFlower,
            WeatherFlying,
            WeatherDandelion,
            WeatherWorm,
            WeatherCloudy,
            WeatherAll
        }
    }
}