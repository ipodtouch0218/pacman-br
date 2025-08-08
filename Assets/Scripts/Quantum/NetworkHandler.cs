using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using UnityEngine;

public class NetworkHandler : MonoBehaviour {

    //---Static Variables
    public static NetworkHandler Instance { get; private set; }
    
    //---Properties
    public QuantumRunner Runner => currentRunner;

    //---Serialized Variables
    [SerializeField] private AssetRef<SimulationConfig> simulationConfig;

    //---Private Variables
    private RealtimeClient currentClient;
    private QuantumRunner currentRunner;


    [RuntimeInitializeOnLoadMethod]
    public static void CreateRunner() {
        GameObject go = Instantiate((GameObject) Resources.Load("Prefabs/NetworkHandler"), null);
        Instance = go.GetComponent<NetworkHandler>();
        DontDestroyOnLoad(go);
    }

    public async Awaitable ResetClient() {
        currentClient ??= new();
        await currentClient.DisconnectAsync();
    }

    public async Awaitable<short> JoinOrCreateRoom(string room, RuntimePlayer player) {
        await ResetClient();

        // Connect to master server
        try {
            await currentClient.ConnectUsingSettingsAsync(new AppSettings {
                AppIdQuantum = "0081a8bf-b93e-4623-a5cf-db9bf93f3db1",
                AppVersion = Application.version,
                FixedRegion = "us",
            });
        } catch {
            return short.MaxValue;
        }

        // Join lobby
        short response = await currentClient.JoinLobbyAsync(TypedLobby.Default, throwOnError: false);
        if (response != 0) {
            return response;
        }

        // Then join room
        response = await currentClient.JoinOrCreateRoomAsync(new EnterRoomArgs {
            RoomName = room
        }, throwOnError: false);

        if (response != 0) {
            return response;
        }

        // Start sim
        currentRunner = await QuantumRunner.StartGameAsync(new SessionRunner.Arguments {
            ClientId = Random.Range(0, 100).ToString(),
            Communicator = new QuantumNetworkCommunicator(currentClient),
            GameMode = DeterministicGameMode.Multiplayer,
            SessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig,
            PlayerCount = 6,
            RuntimeConfig = new RuntimeConfig {
                IsRealGame = true,
                SimulationConfig = simulationConfig,
            }
        });

        // Add player
        currentRunner.Game.AddPlayer(player);

        return 0;
    }
}