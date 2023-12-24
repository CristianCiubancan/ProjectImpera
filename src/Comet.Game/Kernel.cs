namespace Comet.Game
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;
    using Comet.Game.Database;
    using Comet.Game.Internal;
    using Comet.Game.States;
    using Comet.Game.States.Families;
    using Comet.Game.World;
    using Comet.Game.World.Managers;
    using Comet.Game.World.Threading;
    using Comet.Network.Services;
    using Comet.Shared;



    /// <summary>
    /// Kernel for the server, acting as a central core for pools of models and states
    /// initialized by the server. Used in database repositories to load data into memory
    /// from essential tables or tables which require heavy post-processing. Used in the
    /// server packet process methods for tracking client and world states.
    /// </summary>
    public static class Kernel
    {
        // State caches
        public const int SERVER_VERSION = 5180;
        public static readonly string Version;

        /// <summary>
        /// The account server client object.
        /// </summary>
        public static AccountServer AccountServer;
        /// <summary>
        /// The account server client socket.
        /// </summary>
        public static AccountClient AccountClient;

        // State caches
        public static MemoryCache Logins = MemoryCache.Default;
        public static List<uint> Registration = new List<uint>();

        public static MyApi Api;

        public static ServerConfiguration.GameNetworkConfiguration Configuration;

        public static MapManager MapManager = new MapManager();
        public static RoleManager RoleManager = new RoleManager();
        public static ItemManager ItemManager = new ItemManager();
        public static PeerageManager PeerageManager = new PeerageManager();
        public static MagicManager MagicManager = new MagicManager();
        public static EventManager EventManager = new EventManager();
        public static SyndicateManager SyndicateManager = new SyndicateManager();
        public static MineManager MineManager = new MineManager();
        public static PigeonManager PigeonManager = new PigeonManager();
        public static FlowerManager FlowerManager = new FlowerManager();
        public static FamilyManager FamilyManager = new FamilyManager();

        public static NetworkMonitor NetworkMonitor = new NetworkMonitor();

        public static SystemProcessor SystemThread = new SystemProcessor();
        public static UserProcessor UserThread = new UserProcessor();
        public static GeneratorManager GeneratorManager = new GeneratorManager();
        public static AiProcessor AiThread = new AiProcessor();
        public static AutomaticActionsProcessing AutomaticActions = new AutomaticActionsProcessing();
        public static EventsProcessing EventThread = new EventsProcessing();
        public static GeneratorProcessing GeneratorThread = new GeneratorProcessing();
        // Background services
        public static class Services
        {
            public static RandomnessService Randomness = new RandomnessService();
            public static ServerProcessor Processor = new ServerProcessor(Environment.ProcessorCount / 2);
        }
        public static Task<int> NextAsync(int maxValue)
        {
            // return NextAsync(0, maxValue);
            try
            {
                return NextAsync(0, maxValue);
            }
            catch (Exception ex)
            {
                _ = Log.WriteLogAsync(LogLevel.Error, $"NextAsync error!").ConfigureAwait(false);
                _ = Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return Task.FromResult(0);
            }
        }

        /// <summary>Returns the next random number from the generator.</summary>
        /// <param name="minValue">The least legal value for the Random number.</param>
        /// <param name="maxValue">One greater than the greatest legal return value.</param>
        public static Task<int> NextAsync(int minValue, int maxValue)
        {
            return Services.Randomness.NextAsync(minValue, maxValue);
        }
        public static async Task<bool> ChanceCalcAsync(int chance, int outOf)
        {
            return await NextAsync(outOf) < chance;
        }

        /// <summary>
        /// Calculates the chance of success based in a rate.
        /// </summary>
        /// <param name="chance">Rate in percent.</param>
        /// <returns>True if the rate is successful.</returns>
        public static async Task<bool> ChanceCalcAsync(double chance)
        {
            const int DIVISOR_I = 10000;
            const int MAX_VALUE = 100 * DIVISOR_I;
            try
            {
                return await NextAsync(0, MAX_VALUE) <= chance * DIVISOR_I;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Chance Calc error!");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }
        /// <summary>Writes random numbers from the generator to a buffer.</summary>
        /// <param name="buffer">Buffer to write bytes to.</param>
        public static Task NextBytesAsync(byte[] buffer) =>
            Services.Randomness.NextBytesAsync(buffer);


        public static async Task<bool> StartupAsync()
        {
            try
            {
                await Services.Processor.StartAsync(CancellationToken.None);
                await MapManager.LoadDataAsync().ConfigureAwait(true);
                await MapManager.LoadMapsAsync().ConfigureAwait(true);

                await ItemManager.InitializeAsync();
                await RoleManager.InitializeAsync();
                await MagicManager.InitializeAsync();
                await PeerageManager.InitializeAsync();
                _ = await SyndicateManager.InitializeAsync();
                _ = await FamilyManager.InitializeAsync();
                _ = await EventManager.InitializeAsync();
                _ = await MineManager.InitializeAsync();
                _ = await PigeonManager.InitializeAsync();
                _ = await FlowerManager.InitializeAsync();
                await QuestInfo.InitializeAsync();

                _ = await GeneratorManager.InitializeAsync();

                await SystemThread.StartAsync();
                await UserThread.StartAsync();
                await AiThread.StartAsync();
                await AutomaticActions.StartAsync();
                await AutomaticActions.DailyResetAsync();
                await EventThread.StartAsync();
                await GeneratorThread.StartAsync();

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception and handle any necessary cleanup
                await Log.WriteLogAsync(LogLevel.Error, $"Failed to start Kernel: {ex}");
                return false;
            }
        }

        public static bool IsValidName(string szName)
        {
            foreach (var c in szName)
            {
                if (c < ' ')
                    return false;
                switch (c)
                {
                    case ' ':
                    case ';':
                    case ',':
                    case '/':
                    case '\\':
                    case '=':
                    case '%':
                    case '@':
                    case '\'':
                    case '"':
                    case '[':
                    case ']':
                    case '?':
                    case '{':
                    case '}':
                        return false;
                }
            }

            string lower = szName.ToLower();
            return _invalidNameChar.All(part => !lower.Contains(part));
        }

        private static readonly string[] _invalidNameChar =
{
            "{", "}", "[", "]", "(", ")", "\"", "[gm]", "[pm]", "'", "�", "`", "admin", "helpdesk", " ",
            "bitch", "puta", "whore", "ass", "fuck", "cunt", "fdp", "porra", "poha", "caralho", "caraio"
        };
    }
}