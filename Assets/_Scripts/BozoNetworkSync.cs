using Unity.Netcode;
using UnityEngine;
using Bozo.ModularCharacters;
using System.Threading.Tasks;

public struct NetworkString : INetworkSerializable
{
    public string Value;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Value);
    }
}

public class BozoNetworkSync : NetworkBehaviour
{
    private OutfitSystem m_outfitSystem;
    private string m_lastLoadedJson = "";
    private bool m_isLoading = false;

    private NetworkVariable<NetworkString> m_netLookJson = new NetworkVariable<NetworkString>(
        new NetworkString { Value = "" }, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (!enabled) return;

        // 1. Check for the Outfit System!
        m_outfitSystem = GetComponentInChildren<OutfitSystem>();
        if (m_outfitSystem == null)
        {
            Debug.LogError("<color=red>CRITICAL ERROR: OutfitSystem component is missing from your Player Prefab!</color>");
            return;
        }

        m_outfitSystem.loadMode = OutfitSystem.LoadMode.Manual;

        m_netLookJson.OnValueChanged += (oldVal, newVal) => TriggerLookUpdate(newVal.Value);

        if (IsOwner)
        {
            if (!string.IsNullOrEmpty(PlayerSessionLook.CharacterJSON))
            {
                Debug.Log("<color=yellow>1. Player Spawned! Sending Outfit to Network...</color>");
                m_netLookJson.Value = new NetworkString { Value = PlayerSessionLook.CharacterJSON };
                TriggerLookUpdate(PlayerSessionLook.CharacterJSON);
            }
            else
            {
                Debug.LogError("<color=red>Player Spawned, but the Session Data was blank!</color>");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(m_netLookJson.Value.Value)) TriggerLookUpdate(m_netLookJson.Value.Value);
        }
    }

    private async void TriggerLookUpdate(string json)
    {
        if (string.IsNullOrEmpty(json) || m_outfitSystem == null) return;
        
        // Prevent double-loading spam
        if (m_isLoading || json == m_lastLoadedJson) return; 

        m_isLoading = true;
        m_lastLoadedJson = json;

        Debug.Log("<color=yellow>2. Waiting 150ms for Skeleton to initialize...</color>");
        
        await Task.Delay(150);

        if (m_outfitSystem == null) return; 

        Debug.Log("<color=yellow>3. Applying Clothes to Skeleton!</color>");

        if (m_outfitSystem.animator != null) m_outfitSystem.animator.enabled = false;

        CharacterData data = JsonUtility.FromJson<CharacterData>(json);
        await BMAC_SaveSystem.LoadCharacter(m_outfitSystem, data);

        if (m_outfitSystem.animator != null) m_outfitSystem.animator.enabled = true;

        Debug.Log("<color=green>4. Outfit Successfully Loaded!</color>");
        m_isLoading = false;
    }
}