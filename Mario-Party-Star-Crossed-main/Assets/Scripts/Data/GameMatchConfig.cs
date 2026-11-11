using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configurações da partida para Multiplayer Local e Online.
/// Persiste as escolhas da tela de lobby entre a StartScene e a cena do tabuleiro (MVP).
/// </summary>
public static class GameMatchConfig {

    public enum ControllerType {
        LocalHuman = 0,
        BotEasy = 1,
        BotHard = 2,
        RemoteNetwork = 5
    }

    [System.Serializable]
    public class PlayerSlot {
        public string name = "Jogador";
        public ControllerType controllerType = ControllerType.LocalHuman;
        public int avatarId = 1; // 1: João, 2: Maria, 3: Pedro, 4: Ana, etc.
        public ulong networkClientId = 0; // Usado no Multiplayer Online
        public bool isReady = false;

        public bool IsHuman => controllerType == ControllerType.LocalHuman || controllerType == ControllerType.RemoteNetwork;
        public int ControllerInt => (int)controllerType;
    }

    public static PlayerSlot[] Slots = new PlayerSlot[4] {
        new PlayerSlot { name = "Jogador 1", controllerType = ControllerType.LocalHuman, avatarId = 1 },
        new PlayerSlot { name = "Jogador 2", controllerType = ControllerType.BotEasy, avatarId = 2 },
        new PlayerSlot { name = "Jogador 3", controllerType = ControllerType.BotEasy, avatarId = 3 },
        new PlayerSlot { name = "Jogador 4", controllerType = ControllerType.BotEasy, avatarId = 4 },
    };

    public static int MaxTurns = 20;
    public static string SelectedMapScene = "MVP";
    public static bool PassAndPlayEnabled = false;
    public static bool IsOnlineMatch = false;
    public static string RoomCode = "";
    public static int LocalPlayerIndex = 0; // 0: Host, 1..3: Clientes remotos conectados
    public static bool HasCustomConfig = false;
    // Seed do GameRng gerada pelo host no online (0 = sortear localmente)
    public static int SeedPartida = 0;

    // Mapas na ordem em que o botão "MAPA" do lobby alterna entre eles
    public static readonly string[] Mapas = { "MVP", "MapaRapido", "IlhaOportunidades" };

    public static string GetMapDisplayName(string sceneName) {
        if (sceneName == "MapaRapido") return "CIRCUITO RÁPIDO";
        if (sceneName == "IlhaOportunidades") return "ILHA DAS OPORTUNIDADES";
        return "CIDADE CLÁSSICA";
    }

    public static string ProximoMapa(string atual) {
        int i = System.Array.IndexOf(Mapas, atual);
        return Mapas[(i + 1) % Mapas.Length];
    }

    // Rodadas sugeridas ao trocar de mapa (o Circuito Rápido é curto; os outros pedem partidas longas)
    public static int TurnosSugeridos(string mapa) => mapa == "MapaRapido" ? 15 : 20;

    public static void ResetToDefault() {
        Slots[0] = new PlayerSlot { name = "Jogador 1", controllerType = ControllerType.LocalHuman, avatarId = 1 };
        Slots[1] = new PlayerSlot { name = "Jogador 2", controllerType = ControllerType.BotEasy, avatarId = 2 };
        Slots[2] = new PlayerSlot { name = "Jogador 3", controllerType = ControllerType.BotEasy, avatarId = 3 };
        Slots[3] = new PlayerSlot { name = "Jogador 4", controllerType = ControllerType.BotEasy, avatarId = 4 };
        MaxTurns = 20;
        SelectedMapScene = "MVP";
        PassAndPlayEnabled = false;
        IsOnlineMatch = false;
        RoomCode = "";
        LocalPlayerIndex = 0;
        HasCustomConfig = false;
        SeedPartida = 0;
    }

    public static void SaveToPrefs() {
        HasCustomConfig = true;
        PlayerPrefs.SetInt("Match_HasCustomConfig", 1);
        PlayerPrefs.SetInt("Match_MaxTurns", MaxTurns);
        PlayerPrefs.SetString("Match_MapScene", string.IsNullOrEmpty(SelectedMapScene) ? "MVP" : SelectedMapScene);
        PlayerPrefs.SetInt("Match_PassAndPlay", PassAndPlayEnabled ? 1 : 0);
        PlayerPrefs.SetInt("Match_IsOnline", IsOnlineMatch ? 1 : 0);
        PlayerPrefs.SetString("Match_RoomCode", RoomCode);
        PlayerPrefs.SetInt("Match_LocalPlayerIndex", LocalPlayerIndex);
        PlayerPrefs.SetInt("Match_Seed", SeedPartida);

        for (int i = 0; i < 4; i++) {
            PlayerPrefs.SetString($"Match_P{i}_Name", Slots[i].name);
            PlayerPrefs.SetInt($"Match_P{i}_Controller", (int)Slots[i].controllerType);
            PlayerPrefs.SetInt($"Match_P{i}_Avatar", Slots[i].avatarId);
        }
        PlayerPrefs.Save();
    }

    public static void LoadFromPrefs() {
        if (PlayerPrefs.GetInt("Match_HasCustomConfig", 0) != 1) {
            ResetToDefault();
            return;
        }

        HasCustomConfig = true;
        MaxTurns = PlayerPrefs.GetInt("Match_MaxTurns", 20);
        SelectedMapScene = PlayerPrefs.GetString("Match_MapScene", "MVP");
        if (string.IsNullOrEmpty(SelectedMapScene)) SelectedMapScene = "MVP";
        PassAndPlayEnabled = PlayerPrefs.GetInt("Match_PassAndPlay", 0) == 1;
        IsOnlineMatch = PlayerPrefs.GetInt("Match_IsOnline", 0) == 1;
        RoomCode = PlayerPrefs.GetString("Match_RoomCode", "");
        LocalPlayerIndex = PlayerPrefs.GetInt("Match_LocalPlayerIndex", 0);
        SeedPartida = PlayerPrefs.GetInt("Match_Seed", 0);

        for (int i = 0; i < 4; i++) {
            Slots[i].name = PlayerPrefs.GetString($"Match_P{i}_Name", $"Jogador {i + 1}");
            Slots[i].controllerType = (ControllerType)PlayerPrefs.GetInt($"Match_P{i}_Controller", i == 0 ? 0 : 1);
            Slots[i].avatarId = PlayerPrefs.GetInt($"Match_P{i}_Avatar", i + 1);
        }
    }

    public static Color GetAvatarColor(int avatarId) {
        switch (avatarId) {
            case 1: return new Color(0.2f, 0.75f, 0.35f, 1f); // Verde
            case 2: return new Color(0.95f, 0.35f, 0.65f, 1f); // Rosa
            case 3: return new Color(0.25f, 0.6f, 0.95f, 1f); // Azul
            case 4: return new Color(0.95f, 0.65f, 0.15f, 1f); // Laranja / Dourado
            case 5: return new Color(0.9f, 0.25f, 0.25f, 1f); // Vermelho
            case 6: return new Color(0.6f, 0.35f, 0.85f, 1f); // Roxo
            default: return new Color(0.3f, 0.8f, 0.5f, 1f);
        }
    }

    public static string GetAvatarDefaultName(int avatarId) {
        switch (avatarId) {
            case 1: return "João";
            case 2: return "Maria";
            case 3: return "Pedro";
            case 4: return "Ana";
            case 5: return "Carlos";
            case 6: return "Laura";
            default: return $"Jogador {avatarId}";
        }
    }
}
