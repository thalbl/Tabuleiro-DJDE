using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Protocolo de mensagens serializáveis para o Multiplayer Online.
/// Estrutura leve em JSON compatível com TCP, WebSockets e transporte local.
/// </summary>
public static class NetworkProtocol {

    public enum MessageType {
        None = 0,
        // Mensagens de Lobby
        LobbyJoinRequest = 1,
        LobbyJoinResponse = 2,
        LobbySlotUpdate = 3,
        LobbyReadyToggle = 4,
        LobbyStartGame = 5,
        LobbyError = 6,

        // Mensagens de Tabuleiro
        GameTurnStart = 10,
        GameDiceRoll = 11,
        GameOptionChoice = 12,
        GameEmote = 13,
        GameTimeout = 14,
        GamePlayerDisconnect = 15,
        Heartbeat = 20
    }

    [System.Serializable]
    public class NetworkPacket {
        public MessageType type;
        public int senderSlot;
        public string stringPayload = "";
        public int intPayload = 0;
        public bool boolPayload = false;
        public long timestamp;

        public NetworkPacket() {
            timestamp = DateTime.UtcNow.Ticks;
        }

        public NetworkPacket(MessageType type, int senderSlot) : this() {
            this.type = type;
            this.senderSlot = senderSlot;
        }

        public string ToJson() {
            return JsonUtility.ToJson(this);
        }

        public static NetworkPacket FromJson(string json) {
            try {
                return JsonUtility.FromJson<NetworkPacket>(json);
            } catch (Exception e) {
                Debug.LogWarning($"[NetworkProtocol] Falha ao desserializar pacote: {e.Message}");
                return null;
            }
        }
    }

    [System.Serializable]
    public class LobbyStatePayload {
        public string roomCode;
        public int maxTurns;
        public string mapScene = "MVP";
        public SlotData[] slots = new SlotData[4];

        [System.Serializable]
        public class SlotData {
            public int slotIndex;
            public string name;
            public int controllerType;
            public int avatarId;
            public bool isReady;
        }

        public static LobbyStatePayload CriarDeConfig() {
            LobbyStatePayload payload = new LobbyStatePayload {
                roomCode = GameMatchConfig.RoomCode,
                maxTurns = GameMatchConfig.MaxTurns,
                mapScene = string.IsNullOrEmpty(GameMatchConfig.SelectedMapScene) ? "MVP" : GameMatchConfig.SelectedMapScene,
                slots = new SlotData[4]
            };

            for (int i = 0; i < 4; i++) {
                payload.slots[i] = new SlotData {
                    slotIndex = i,
                    name = GameMatchConfig.Slots[i].name,
                    controllerType = (int)GameMatchConfig.Slots[i].controllerType,
                    avatarId = GameMatchConfig.Slots[i].avatarId,
                    isReady = GameMatchConfig.Slots[i].isReady
                };
            }
            return payload;
        }

        public void AplicarParaConfig(int meuSlotIndex) {
            GameMatchConfig.RoomCode = roomCode;
            if (maxTurns > 0) GameMatchConfig.MaxTurns = maxTurns;
            if (!string.IsNullOrEmpty(mapScene)) GameMatchConfig.SelectedMapScene = mapScene;

            if (slots == null) return;
            for (int i = 0; i < slots.Length && i < 4; i++) {
                if (slots[i] == null) continue;
                GameMatchConfig.Slots[i].name = slots[i].name;
                GameMatchConfig.Slots[i].avatarId = slots[i].avatarId;
                GameMatchConfig.Slots[i].isReady = slots[i].isReady;

                // O slot correspondente a esta máquina local sempre roda como LocalHuman
                if (i == meuSlotIndex) {
                    GameMatchConfig.Slots[i].controllerType = GameMatchConfig.ControllerType.LocalHuman;
                } else {
                    GameMatchConfig.Slots[i].controllerType = (GameMatchConfig.ControllerType)slots[i].controllerType;
                }
            }
        }
    }
}
