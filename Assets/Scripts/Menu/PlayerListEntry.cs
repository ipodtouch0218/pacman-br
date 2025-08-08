using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListEntry : MonoBehaviour {

    //---Properties
    public PlayerRef Player { get; set; }

    //---Serialized Variables
    [SerializeField] private Graphic pacmanFillGraphic, readyGraphic, hostGraphic;
    [SerializeField] private TMP_Text nicknameText;

    public unsafe void Initialize(Frame f, PlayerRef player) {
        var runtimePlayer = f.GetPlayerData(player);
        if (runtimePlayer == null) {
            return;
        }

        nicknameText.text = runtimePlayer.PlayerNickname;
        pacmanFillGraphic.color = Utils.GetPlayerColor(player);

        if (f.TryResolveDictionary(f.Global->PlayerDatas, out var dict)
            && dict.TryGetValue(player, out var entity)
            && f.Unsafe.TryGetPointer(entity, out PlayerData* playerData)) {

            hostGraphic.gameObject.SetActive(playerData->IsRoomHost);
        } else {
            hostGraphic.gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }
}
