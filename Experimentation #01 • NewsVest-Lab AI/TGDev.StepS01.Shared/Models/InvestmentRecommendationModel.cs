namespace TGDev.StepS01.Shared.Models;

public class InvestmentRecommendationModel
{
    public string Stock { get; set; } = string.Empty;
    public ActionRecommandee Action { get; set; } = ActionRecommandee.Achat;
    public double Weight { get; set; } = 0.0;
    public double Confidence { get; set; } = 0.0;
    public string Justification { get; set; } = string.Empty;
}

public enum ActionRecommandee
{
    Achat,
    Vente,
    Conserver
}
