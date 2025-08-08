using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;

public class MainSubmenu : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private TMP_InputField nicknameField, roomNameField;
    [SerializeField] private GameObject inRoomPanel;

    public void Start() {
        QuantumCallback.Subscribe<CallbackGameStarted>(this, OnGameStarted);
    }

    private void OnGameStarted(CallbackGameStarted e) {
        gameObject.SetActive(false);
        inRoomPanel.SetActive(true);
    }

    [Preserve]
    public async void JoinRoom_Click() {
        string room = roomNameField.text;

        if (string.IsNullOrWhiteSpace(room)) {
            return;
        }

        short code = await NetworkHandler.Instance.JoinOrCreateRoom(room, new RuntimePlayer {
            PlayerNickname = nicknameField.text
        });
        Debug.Log(code);
    }
}