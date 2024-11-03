using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.NetworkManager;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Authentication;

public class optionsScript : NetworkBehaviour
{
    public static optionsScript Instance { get; private set; }


    //[SerializeField] private Button mainMenu;

    //public GameObject optionsMenu;
    //public bool paused;
    //[SerializeField] private Transform playerPrefab;
    [SerializeField] private List<GameObject> playerSkinList;

    public const int MAX_PLAYER_AMOUNT = 4;

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;

    public characterSelectPlayer characterSP;

    private NetworkList<playerData> playerDataNetworkList;

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);
        
        playerDataNetworkList = new NetworkList<playerData>();
        playerDataNetworkList.OnListChanged += playerDataNetworkList_onListChanged;
    }

    private void Update()
    {
        //Debug.Log(characterSP.sameCharacter.enabled);
    }
    private void playerDataNetworkList_onListChanged(NetworkListEvent<playerData> changeEvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(GetPlayerSkin(GetPlayerDataFromClientId(clientId).skinId).transform, new Vector3( 2.0f, 10f, 0), Quaternion.identity);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }
    
    public void startHost()
    {
        NetworkManager.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_onClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_Server_onClientDisconnectCallback(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            playerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId)
            {
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        playerDataNetworkList.Add(new playerData
        {
            clientId = clientId,
            skinId = GetFirstUnusedSkinId(),
        });
        setPlayerIdServerRPC(AuthenticationService.Instance.PlayerId);
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        if (SceneManager.GetActiveScene().name != Loader.Scene.characterSelect.ToString())
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game has already started";
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is full";
            return;
        }

        connectionApprovalResponse.Approved = true;
        
    }

    public void startClient()
    {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_onClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.NetworkConfig.ConnectionApproval = true;
        //Debug.Log(NetworkManager.NetworkConfig.ConnectionApproval);
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong obj)
    {
        setPlayerIdServerRPC(AuthenticationService.Instance.PlayerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void setPlayerIdServerRPC(string playerId, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        playerData playerdata = playerDataNetworkList[playerDataIndex];

        playerdata.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerdata;
    }

    private void NetworkManager_Client_onClientDisconnectCallback(ulong clientId)
    {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
        //Debug.Log("Ello");
    }

    public bool isPlayerIndexConnected(int playerIndex)
    {
        return playerIndex < playerDataNetworkList.Count;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }

    public playerData GetPlayerDataFromClientId(ulong clientId)
    {
        foreach (playerData playerData in playerDataNetworkList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData;
            }
        }
        return default;
    }

    public playerData GetPlayerData()
    {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public playerData GetPlayerDataFromPlayerIndex(int playerIndex)
    {
        return playerDataNetworkList[playerIndex];
    }

    public GameObject GetPlayerSkin(int skinId)
    {
        return playerSkinList[skinId];
    }

    public void ChangePlayerSkin(int skinId)
    {
        ChangePlayerSkinServerRpc(skinId);
    }


    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerSkinServerRpc(int skinId, ServerRpcParams serverRpcParams = default)
    {
        
        //characterSP.EnableImage();
        if (!IsSkinAvailable(skinId))
        {
            Debug.Log("Change player skin called");
            characterSP.EnableImage();
            //    // Color not available
            //        return;
        }
        else
        {
           // characterSP.disableImage();
        }

        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        playerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.skinId = skinId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private bool IsSkinAvailable(int skinId)
    {
        foreach (playerData playerData in playerDataNetworkList)
        {
            if (playerData.skinId == skinId)
            {
                // Already in use
                return false;
            }
        }
        return true;
    }

    private int GetFirstUnusedSkinId()
    {
        for (int i = 0; i < playerSkinList.Count; i++)
        {
            if (IsSkinAvailable(i))
            {
                return i;
            }
        }
        return -1;
    }

    public void kickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_onClientDisconnectCallback(clientId);
    }
}

