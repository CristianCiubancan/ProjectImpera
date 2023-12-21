// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) FTW! Masters
// Keep the headers and the patterns adopted by the project. If you changed anything in the file just insert
// your name below, but don't remove the names of who worked here before.
// 
// This project is a fork from Comet, a Conquer Online Server Emulator created by Spirited, which can be
// found here: https://gitlab.com/spirited/comet
// 
// Comet - Comet.Shared - MyApi.cs
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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Comet.Shared.Models;
using Newtonsoft.Json;

#endregion

namespace Comet.Shared
{
    public class MyApi
    {
        public const string SYNC_INFORMATION_URL = "/api/GameServerStatus";

        private const string BASE_URL = "https://api.worldconquer.online";

        private DateTime m_ExpireTime;
        private bool m_isAuthenticated;
        private string m_token = "";

        private string m_server;
        private string m_user;
        private string m_pass;

        public MyApi(string server, string user, string pass)
        {
            m_server = server;
            m_user = user;
            m_pass = pass;
        }

        private async Task<bool> AuthenticateAsync()
        {
            using HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(BASE_URL)
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            AuthenticationRequest model = new AuthenticationRequest
            {
                Username = m_user,
                Password = m_pass,
                ServerName = m_server
            };

            var contentData = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/api/GameServerStatus/Authenticate", contentData);

            string strResponse = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(strResponse))
                return false;

            AuthenticationResponse auth = JsonConvert.DeserializeObject<AuthenticationResponse>(strResponse);

            if (auth == null)
                return false;

            try
            {
                m_token = auth.Token;
                m_ExpireTime = DateTime.Parse(auth.Expiration);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Error on reply from API [{BASE_URL}] Auth, [{m_token}],[{auth.Expiration}] {ex.Message}");
                return false;
            }

            return true;
        }

        public async Task<bool> PostAsync<T>(T e, string url) where T : class
        {
            try
            {
                if (!m_isAuthenticated || DateTime.Now > m_ExpireTime) m_isAuthenticated = await AuthenticateAsync();

                if (!m_isAuthenticated)
                {
                    await Log.WriteLogAsync(LogLevel.Error, "Could not authenticate to the API.");
                    return false;
                }

                using HttpClient client = new HttpClient
                {
                    BaseAddress = new Uri(BASE_URL)
                };
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", m_token);

                var contentData = new StringContent(JsonConvert.SerializeObject(e), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(url, contentData);
                return JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
            }
            catch
            {
                return false;
            }
        }

        public async Task<T> GetAsync<T>(string url) where T : class
        {
            if (!m_isAuthenticated || DateTime.Now > m_ExpireTime) m_isAuthenticated = await AuthenticateAsync();

            if (!m_isAuthenticated)
                throw new AuthenticationException("Could not authenticate to the API.");

            using HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(BASE_URL)
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", m_token);

            HttpResponseMessage response = await client.GetAsync(url);

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }
    }
}