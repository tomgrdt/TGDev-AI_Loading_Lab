using System.Globalization;

namespace TGDev.Experimentation02.Creances;

public enum TypeAction { Surveiller, Rappel, RelanceCourtoise, RelanceFerme, MiseEnDemeure, Expert }
public enum Ton { Courtois, Ferme, Formel }

public record Facture(string Numero, DateOnly Echeance, decimal Reste, bool EnLitige = false, string? MotifLitige = null);
public record Evenement(DateOnly Date, string Type, string Detail);
public record Compte(string Id, string Nom, string Contact, string Email, decimal EncoursAutorise,
                     IReadOnlyList<Facture> Factures, IReadOnlyList<Evenement> Historique)
{
    public decimal Encours => Factures.Sum(f => f.Reste);
}
public record Source(string Titre, string Url);
public record Recommandation(TypeAction Action, string Libelle, int Priorite, int Confiance, decimal Echu, int RetardMax,
                             IReadOnlyList<string> Raisons, IReadOnlyList<Source> Sources, string? MotifExpert)
{
    public bool NecessiteExpert => MotifExpert is not null;
}
public record Decision(DateTime Date, string Utilisateur, string Compte, string Action, string Commentaire);

public static class Fmt
{
    static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    public static string Eur(decimal v) => v.ToString("C0", Fr);
    public static string Eur2(decimal v) => v.ToString("C2", Fr);
}

/// <summary>Règles transparentes et auditables. Chaque recommandation expose ses raisons et ses sources.</summary>
public static class Moteur
{
    // À charger depuis la configuration et à mettre à jour à chaque avis officiel (taux d'intérêt légal du semestre).
    // Valeur d'EXEMPLE : pénalités = 3 x taux d'intérêt légal en vigueur.
    public const decimal TauxPenalitesAnnuel = 0.12m;
    public const decimal IndemniteForfaitaire = 40m;

    // Liens à préciser (article exact, avis du semestre) et à contrôler régulièrement.
    public static readonly Source[] Sources =
    {
        new("Code de commerce, art. L441-10 : délais de paiement et pénalités (Légifrance)", "https://www.legifrance.gouv.fr"),
        new("Retard de paiement : indemnité forfaitaire de 40 € (Service-Public)", "https://www.service-public.fr"),
        new("Observatoire des délais de paiement (Banque de France)", "https://www.banque-france.fr")
    };

    public static Recommandation Analyser(Compte c, DateOnly auj)
    {
        var ech = c.Factures.Where(f => f.Echeance < auj && f.Reste > 0).ToList();
        var echu = ech.Sum(f => f.Reste);
        var retard = ech.Count == 0 ? 0 : ech.Max(f => auj.DayNumber - f.Echeance.DayNumber);
        var relances = c.Historique.Count(e => e.Type == "Relance");
        var litige = c.Factures.FirstOrDefault(f => f.EnLitige);
        var raisons = new List<string>();

        if (ech.Count > 0) raisons.Add($"{ech.Count} facture(s) échue(s) pour {Fmt.Eur(echu)}, retard maximal de {retard} jours.");
        if (relances > 0) raisons.Add($"{relances} relance(s) déjà envoyée(s), dernier échange le {c.Historique.Max(e => e.Date):dd/MM/yyyy}.");
        if (c.Encours > c.EncoursAutorise) raisons.Add($"Encours de {Fmt.Eur(c.Encours)} au-dessus de l'autorisation ({Fmt.Eur(c.EncoursAutorise)}).");
        var paiement = c.Historique.FirstOrDefault(e => e.Type == "Paiement");
        if (paiement is not null) raisons.Add($"Historique de paiement : {paiement.Detail}.");

        TypeAction a; string libelle; string? expert = null;
        if (litige is not null)
        {
            a = TypeAction.Expert; libelle = "Faire arbitrer le litige par un expert";
            expert = $"Facture {litige.Numero} contestée ({litige.MotifLitige}). Arbitrage commercial et juridique requis.";
            raisons.Insert(0, "Une facture est contestée : ne pas relancer avant arbitrage.");
        }
        else if (retard == 0) { a = TypeAction.Surveiller; libelle = "Aucune action, surveiller les échéances"; raisons.Add("Aucune facture échue."); }
        else if (retard >= 60 || (relances >= 3 && retard > 30))
        {
            a = TypeAction.MiseEnDemeure; libelle = "Préparer une mise en demeure";
            expert = "Mise en demeure : validation juridique requise avant tout envoi.";
        }
        else if (retard > 30 || (relances >= 1 && retard > 14)) { a = TypeAction.RelanceFerme; libelle = "Envoyer une relance ferme"; }
        else if (retard > 7) { a = TypeAction.RelanceCourtoise; libelle = "Envoyer une relance courtoise"; }
        else { a = TypeAction.Rappel; libelle = "Envoyer un rappel amical"; }

        if (expert is null && echu >= 25000)
            expert = $"Montant échu élevé ({Fmt.Eur(echu)}) : revue par le responsable financier recommandée.";

        var priorite = a == TypeAction.Surveiller ? 0
            : Math.Min(100, retard + (int)(echu / 500) + (relances >= 2 ? 10 : 0) + (c.Encours > c.EncoursAutorise ? 10 : 0) + (litige is not null ? 15 : 0));
        var confiance = c.Historique.Count == 0 ? 65 : a is TypeAction.Expert or TypeAction.MiseEnDemeure ? 70 : 88;

        return new(a, libelle, priorite, confiance, echu, retard, raisons,
                   a == TypeAction.Surveiller ? Array.Empty<Source>() : Sources, expert);
    }
}

// Point d'extension : remplacez RedacteurModele par une implémentation qui appelle Ollama
// (http://localhost:11434/api/generate) ou un webhook n8n, en lui passant le contexte du compte.
public interface IRedacteurRelance
{
    Task<(string Objet, string Corps)> GenererAsync(Compte c, Recommandation r, Ton ton, DateOnly auj);
}

public class RedacteurModele : IRedacteurRelance
{
    public Task<(string Objet, string Corps)> GenererAsync(Compte c, Recommandation r, Ton ton, DateOnly auj)
    {
        var ech = c.Factures.Where(f => f.Echeance < auj && f.Reste > 0).OrderBy(f => f.Echeance).ToList();
        var lignes = string.Join("\n", ech.Select(f => $"  - {f.Numero} : {Fmt.Eur(f.Reste)}, échue le {f.Echeance:dd/MM/yyyy}"));
        var total = Fmt.Eur(r.Echu);
        var penalites = ech.Sum(f => f.Reste * Moteur.TauxPenalitesAnnuel * (auj.DayNumber - f.Echeance.DayNumber) / 365m);
        var indemnites = ech.Count * Moteur.IndemniteForfaitaire;
        var relances = c.Historique.Count(e => e.Type == "Relance");
        const string sign = "\n\nCordialement,\n[Votre nom]\n[Service recouvrement]";

        return Task.FromResult(ton switch
        {
            Ton.Courtois => ("Rappel : factures en attente de règlement",
                $"Bonjour {c.Contact},\n\nSauf erreur de notre part, les factures suivantes ne sont pas encore réglées :\n{lignes}\n\nTotal : {total}.\n\nSi le paiement est en cours, merci de ne pas tenir compte de ce message. Sinon, pourriez-vous nous indiquer la date de règlement prévue ?{sign}"),
            Ton.Ferme => ("Relance : factures échues à régler",
                $"Bonjour {c.Contact},\n\nMalgré {(relances > 0 ? $"nos {relances} précédente(s) relance(s)" : "notre rappel")}, les factures suivantes restent impayées :\n{lignes}\n\nTotal dû : {total}.\n\nNous vous demandons de procéder au règlement sous 8 jours. Des pénalités de retard (estimées à {Fmt.Eur2(penalites)}) et l'indemnité forfaitaire de recouvrement de {Fmt.Eur(Moteur.IndemniteForfaitaire)} par facture ({Fmt.Eur(indemnites)}) sont exigibles. Si un point bloque le paiement, contactez-nous rapidement.{sign}"),
            _ => ("Mise en demeure de payer",
                $"Madame, Monsieur,\n\nNous constatons que les factures suivantes demeurent impayées :\n{lignes}\n\nTotal principal : {total}, hors pénalités de retard (estimées à {Fmt.Eur2(penalites)}) et indemnités forfaitaires de recouvrement ({Fmt.Eur(indemnites)}).\n\nNous vous mettons en demeure de régler ces sommes dans un délai de 8 jours à compter de la réception de ce courrier. À défaut, nous engagerons les démarches de recouvrement adaptées.{sign}")
        });
    }
}

public interface IComptesService { Task<IReadOnlyList<Compte>> ChargerAsync(); }

/// <summary>Données fictives. Remplacez par la lecture de l'ERP (API, vue SQL ou export) et de l'historique des échanges.</summary>
public class ComptesDemo : IComptesService
{
    public Task<IReadOnlyList<Compte>> ChargerAsync()
    {
        var t = DateOnly.FromDateTime(DateTime.Today);
        DateOnly J(int d) => t.AddDays(d);
        IReadOnlyList<Compte> l = new List<Compte>
        {
            new("C001", "Menuiseries Lefèvre", "Mme Lefèvre", "compta@menuiseries-lefevre.example", 30000,
                new[] { new Facture("F-2411", J(-5), 3200m), new Facture("F-2450", J(12), 5100m) },
                new[] { new Evenement(J(-90), "Paiement", "facture F-2302 réglée à 28 jours") }),
            new("C002", "Groupe Océane Logistique", "M. Prigent", "comptabilite@oceane-log.example", 60000,
                new[] { new Facture("F-2388", J(-22), 12400m), new Facture("F-2401", J(-8), 6000m), new Facture("F-2477", J(20), 9800m) },
                new[] { new Evenement(J(-120), "Paiement", "règlement habituel autour de 35 jours") }),
            new("C003", "Atelier Bréhat", "Mme Le Gall", "contact@atelier-brehat.example", 25000,
                new[] { new Facture("F-2290", J(-41), 7850m), new Facture("F-2334", J(-15), 4300m) },
                new[] { new Evenement(J(-27), "Relance", "relance courtoise par e-mail"), new Evenement(J(-12), "Relance", "relance par e-mail, sans réponse") }),
            new("C004", "Cabinet Morvan & Associés", "M. Morvan", "admin@morvan-associes.example", 15000,
                new[] { new Facture("F-2361", J(-19), 9600m, true, "livrable jugé non conforme"), new Facture("F-2402", J(-3), 2100m) },
                new[] { new Evenement(J(-14), "Litige", "courrier de contestation reçu") }),
            new("C005", "Transports Kerjean", "M. Kerjean", "direction@kerjean-transports.example", 30000,
                new[] { new Facture("F-2201", J(-74), 21500m), new Facture("F-2255", J(-52), 17000m) },
                new[] { new Evenement(J(-60), "Relance", "relance courtoise"), new Evenement(J(-40), "Relance", "relance ferme par e-mail"),
                        new Evenement(J(-20), "Relance", "courrier ferme, promesse de paiement non tenue") }),
            new("C006", "Pharmacie du Port", "Mme Guillou", "compta@pharmacie-du-port.example", 20000,
                new[] { new Facture("F-2480", J(15), 4200m) },
                new[] { new Evenement(J(-30), "Paiement", "facture F-2419 réglée à 24 jours") })
        };
        return Task.FromResult(l);
    }
}
