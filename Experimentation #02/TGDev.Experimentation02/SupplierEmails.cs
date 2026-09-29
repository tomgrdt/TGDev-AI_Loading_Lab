namespace TGDev.Experimentation02.SupplierEmails;

public enum RequestType { InvoiceStatus, PaymentConfirmation, RemittanceAdvice, Dispute, Acknowledgement, Other }
public enum FieldStatus { Present, Missing, Inconsistent }

public record Attachment(string FileName, string Kind);

public record ExtractedField(string Name, string? Value, FieldStatus Status, string? Note = null);

public record ErpCheckResult(string InvoiceNumber, string Status, DateOnly? DueDate, DateOnly? PaymentDate, string? RemittanceReference, bool Found = true);

public record SupplierEmailMsg(string Id, string SupplierName, string From, string Subject, DateTime Received,
                               string Body, IReadOnlyList<Attachment> Attachments);

public record AnalysisResult(RequestType Type, string TypeLabel, int Confidence, IReadOnlyList<ExtractedField> Fields,
                             IReadOnlyList<string> Issues, IReadOnlyList<ErpCheckResult> ErpChecks,
                             bool NeedsHumanReview, string? ReviewReason)
{
    public int AnomalyCount => Fields.Count(f => f.Status != FieldStatus.Present) + ErpChecks.Count(e => !e.Found);
}

public record Decision(DateTime Date, string User, string Email, string Action, string Comment);

// Point d'extension : en production, remplacez DemoEmailAnalyzer par un appel à Copilot Studio / Azure OpenAI
// + Document Intelligence pour les pièces jointes. Ici, les champs extraits sont des données de démonstration
// pré-calculées : seule la vérification ERP (IErpService) simule un appel réellement dynamique.
public interface IEmailAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(SupplierEmailMsg email);
}

public interface IErpService
{
    Task<IReadOnlyList<ErpCheckResult>> CheckAsync(IEnumerable<string> invoiceNumbers);
}

public interface IReplyWriter
{
    Task<(string Subject, string Body)> DraftAsync(SupplierEmailMsg email, AnalysisResult analysis);
}

public interface ISupplierEmailService
{
    Task<IReadOnlyList<SupplierEmailMsg>> LoadAsync();
}

public class DemoErpService : IErpService
{
    // Reprend les factures utilisées dans l'outil de suivi des comptes clients, pour rester cohérent.
    static readonly Dictionary<string, ErpCheckResult> Ledger = new()
    {
        ["F-2411"] = new("F-2411", "Payée", new DateOnly(2026, 9, 20), new DateOnly(2026, 9, 18), "REM-8841"),
        ["F-2450"] = new("F-2450", "En attente d'échéance", new DateOnly(2026, 10, 11), null, null),
        ["F-2388"] = new("F-2388", "En retard", new DateOnly(2026, 9, 7), null, null),
        ["F-2401"] = new("F-2401", "Payée", new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 19), "REM-9002", Found: true),
        ["F-2290"] = new("F-2290", "En retard", new DateOnly(2026, 8, 19), null, null),
        ["F-2334"] = new("F-2334", "En retard", new DateOnly(2026, 9, 14), null, null),
        ["F-2361"] = new("F-2361", "Litige ouvert", new DateOnly(2026, 9, 10), null, null),
        ["F-2480"] = new("F-2480", "En attente d'échéance", new DateOnly(2026, 10, 14), null, null),
    };

    public Task<IReadOnlyList<ErpCheckResult>> CheckAsync(IEnumerable<string> invoiceNumbers)
    {
        IReadOnlyList<ErpCheckResult> results = invoiceNumbers
            .Select(n => Ledger.TryGetValue(n, out var r) ? r : new ErpCheckResult(n, "Introuvable dans l'ERP", null, null, null, Found: false))
            .ToList();
        return Task.FromResult(results);
    }
}

public class DemoEmailAnalyzer : IEmailAnalyzer
{
    readonly IErpService _erp;
    public DemoEmailAnalyzer(IErpService erp) => _erp = erp;

    // Extraction pré-calculée par e-mail (à remplacer par le résultat réel du modèle IA).
    static readonly Dictionary<string, (RequestType Type, string Label, int Confidence, ExtractedField[] Fields, string? ClaimedAmount)> Extracted = new()
    {
        ["E1"] = (RequestType.InvoiceStatus, "Demande de statut de facture", 92, new[]
        {
            new ExtractedField("N° de facture", "F-2450", FieldStatus.Present),
            new ExtractedField("N° de commande", null, FieldStatus.Missing, "non mentionné dans l'e-mail"),
        }, null),
        ["E2"] = (RequestType.PaymentConfirmation, "Confirmation de paiement", 85, new[]
        {
            new ExtractedField("N° de facture", "F-2401", FieldStatus.Present),
            new ExtractedField("Montant réclamé", "6 200,00 €", FieldStatus.Inconsistent, "l'ERP indique 6 000,00 € réglés"),
        }, "6200"),
        ["E3"] = (RequestType.RemittanceAdvice, "Avis de règlement", 78, new[]
        {
            new ExtractedField("Factures citées", "F-2290, F-2299", FieldStatus.Inconsistent, "F-2299 introuvable dans l'ERP, possible erreur de saisie"),
        }, null),
        ["E4"] = (RequestType.Dispute, "Litige sur facture", 90, new[]
        {
            new ExtractedField("N° de facture", "F-2361", FieldStatus.Present),
            new ExtractedField("Motif du litige", "livrable jugé non conforme", FieldStatus.Present),
        }, null),
        ["E5"] = (RequestType.Other, "Relance de paiement, sans référence", 55, new[]
        {
            new ExtractedField("N° de facture", null, FieldStatus.Missing, "aucun numéro cité, impossible de vérifier automatiquement"),
        }, null),
        ["E6"] = (RequestType.Acknowledgement, "Accusé de réception de facture", 96, new[]
        {
            new ExtractedField("N° de facture", "F-2480", FieldStatus.Present),
        }, null),
    };

    public async Task<AnalysisResult> AnalyzeAsync(SupplierEmailMsg email)
    {
        var (type, label, confidence, fields, _) = Extracted[email.Id];
        var invoiceNumbers = fields
            .Where(f => f.Name.Contains("facture") && f.Value is not null)
            .SelectMany(f => f.Value!.Split(',').Select(v => v.Trim()))
            .Distinct()
            .ToList();

        var erpChecks = invoiceNumbers.Count > 0
            ? await _erp.CheckAsync(invoiceNumbers)
            : Array.Empty<ErpCheckResult>();

        var issues = new List<string>();
        foreach (var f in fields.Where(f => f.Status != FieldStatus.Present))
            issues.Add($"{f.Name} {(f.Status == FieldStatus.Missing ? "manquant" : "incohérent")} : {f.Note}");
        foreach (var e in erpChecks.Where(e => !e.Found))
            issues.Add($"Facture {e.InvoiceNumber} introuvable dans l'ERP — vérification manuelle nécessaire.");

        string? reviewReason = type switch
        {
            RequestType.Dispute => "Litige : un arbitrage humain est requis avant toute réponse.",
            _ when issues.Count > 0 => "Données manquantes ou incohérentes détectées : une vérification humaine est recommandée avant l'envoi.",
            _ => null
        };

        return new AnalysisResult(type, label, confidence, fields, issues, erpChecks,
            NeedsHumanReview: reviewReason is not null, reviewReason);
    }
}

public class TemplateReplyWriter : IReplyWriter
{
    public Task<(string Subject, string Body)> DraftAsync(SupplierEmailMsg email, AnalysisResult analysis)
    {
        const string sign = "\n\nCordialement,\nService Comptes Fournisseurs";

        if (analysis.NeedsHumanReview)
        {
            var body = $"Bonjour,\n\nNous accusons réception de votre message concernant {SubjectRef(email)}. " +
                       "Votre demande nécessite une vérification complémentaire de notre part avant de pouvoir vous répondre précisément. " +
                       "Nous revenons vers vous rapidement.{sign}".Replace("{sign}", sign);
            return Task.FromResult(($"RE: {email.Subject}", body));
        }

        var lines = string.Join("\n", analysis.ErpChecks.Select(e =>
            $"  - {e.InvoiceNumber} : {e.Status}" +
            (e.PaymentDate is not null ? $", réglée le {e.PaymentDate:dd/MM/yyyy} (réf. {e.RemittanceReference})" :
             e.DueDate is not null ? $", échéance le {e.DueDate:dd/MM/yyyy}" : "")));

        var text = analysis.Type switch
        {
            RequestType.InvoiceStatus or RequestType.PaymentConfirmation or RequestType.RemittanceAdvice =>
                $"Bonjour,\n\nSuite à votre message, voici le statut de votre/vos facture(s) dans notre système :\n{lines}\n\n" +
                "N'hésitez pas à revenir vers nous pour toute question.{sign}".Replace("{sign}", sign),
            RequestType.Acknowledgement =>
                $"Bonjour,\n\nNous confirmons la bonne réception de votre facture. Statut actuel :\n{lines}{sign}",
            _ => $"Bonjour,\n\nNous avons bien reçu votre message. {(lines.Length > 0 ? $"Statut des factures identifiées :\n{lines}\n\n" : "")}" +
                 "Pourriez-vous nous préciser le ou les numéros de facture concernés afin que nous puissions vérifier leur statut ?{sign}".Replace("{sign}", sign)
        };

        return Task.FromResult(($"RE: {email.Subject}", text));
    }

    static string SubjectRef(SupplierEmailMsg email) => $"« {email.Subject} »";
}

public class DemoSupplierEmails : ISupplierEmailService
{
    public Task<IReadOnlyList<SupplierEmailMsg>> LoadAsync()
    {
        var t = DateTime.Now;
        DateTime H(int hoursAgo) => t.AddHours(-hoursAgo);

        IReadOnlyList<SupplierEmailMsg> emails = new List<SupplierEmailMsg>
        {
            new("E1", "Menuiseries Lefèvre", "compta@menuiseries-lefevre.example", "Statut de la facture F-2450", H(3),
                "Bonjour, pourriez-vous nous confirmer le statut de la facture F-2450 ? Merci.",
                new[] { new Attachment("F-2450.pdf", "Copie de facture") }),

            new("E2", "Groupe Océane Logistique", "comptabilite@oceane-log.example", "Confirmation de paiement facture F-2401", H(6),
                "Bonjour, nous constatons un paiement de 6 200,00 € reçu pour la facture F-2401. Pouvez-vous confirmer ?",
                new[] { new Attachment("Releve_bancaire_extrait.pdf", "Justificatif bancaire") }),

            new("E3", "Atelier Bréhat", "contact@atelier-brehat.example", "Avis de règlement - factures F-2290 et F-2299", H(20),
                "Bonjour, veuillez trouver ci-joint notre avis de règlement pour les factures F-2290 et F-2299.",
                new[] { new Attachment("Avis_reglement_Brehat.pdf", "Avis de règlement") }),

            new("E4", "Cabinet Morvan & Associés", "admin@morvan-associes.example", "Litige facture F-2361", H(30),
                "Bonjour, nous contestons la facture F-2361, le livrable associé n'étant pas conforme à notre commande.",
                new[] { new Attachment("Reclamation_F-2361.pdf", "Courrier de contestation") }),

            new("E5", "Transports Kerjean", "direction@kerjean-transports.example", "Où en est le paiement de nos factures ?", H(48),
                "Bonjour, pouvez-vous nous indiquer où en est le paiement de nos factures en cours ? Merci de votre retour.",
                Array.Empty<Attachment>()),

            new("E6", "Pharmacie du Port", "compta@pharmacie-du-port.example", "Accusé de réception facture F-2480", H(72),
                "Bonjour, nous confirmons la bonne réception de votre facture F-2480.",
                Array.Empty<Attachment>()),
        };
        return Task.FromResult(emails);
    }
}
