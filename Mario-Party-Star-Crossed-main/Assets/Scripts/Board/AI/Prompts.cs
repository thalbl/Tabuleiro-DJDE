/// <summary>
/// Textos de perguntas compartilhados entre o jogo (ui.Dialogue) e a IA (AIBrain.Prompt).
/// A IA decide fazendo switch pelo texto da pergunta: SEMPRE use estas constantes nos dois lados,
/// nunca uma string literal, para que mudar um texto não quebre o bot silenciosamente.
/// </summary>
public static class Prompts {
    public const string MenuTurno = "O que você fará?";
    public const string EscolhaItem = "Qual item você deseja usar?";
    public const string EscolhaCaminho = "Escolha o Caminho";
    public const string DadoMagico = "Escolha o Dado Mágico";
    public const string AlvoBuracoNoBolso = "Em quem você quer jogar o Buraco no Bolso?";
    public const string AlvoVentania = "Em quem você quer usar a Ventania?";
    public const string AlvoRouboItem = "De quem você quer roubar um item?";
    public const string AlvoDuelo = "Quem você quer desafiar para um duelo?";

    public const string Cancelar = "Cancelar";

    // Banco: o texto mostrado é "<linha fixa>\n<saldos>"; a IA reconhece pela linha fixa (StartsWith)
    public const string MenuBanco = "Banco: o que você quer fazer?";
    public const string BancoPoupanca = "Poupança: rende 3% por turno.";
    public const string BancoReserva = "Reserva de emergência: paga os imprevistos primeiro.";
    public const string BancoAcoes = "Ações: sobem ou caem com a Bolsa a cada rodada.";
    public const string BancoEmprestimo = "Empréstimo: +50 agora, juros de 8% por turno.";
    public const string OpcaoBanco = "Banco";
    public const string Voltar = "Voltar";
}
