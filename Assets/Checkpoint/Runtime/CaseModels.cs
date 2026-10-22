using System;
using System.Collections.Generic;

namespace Checkpoint
{
    public enum Decision { Approve, Deny, Secondary }
    public enum ShiftPhase { Briefing, Processing, Receipt, Evaluation }

    [Serializable] public sealed class CaseBook
    {
        public int schemaVersion = 1;
        public string shiftDate;
        public string airport;
        public Rule[] rules;
        public TravelerCase[] cases;
    }
    [Serializable] public sealed class Rule { public string id, title, text; }
    [Serializable] public sealed class Identity
    {
        public string name, birthdate, sex, nationality, passport, portrait;
    }
    [Serializable] public sealed class TravelerCase
    {
        public string id, title, arrival, greeting, tutorial, hiddenNarrative;
        public Identity actualIdentity, perceivedIdentity;
        public DocumentData[] documents;
        public DialogueLine[] dialogue;
        public WebRecord[] records;
        public EvidenceData[] evidence;
        public Discrepancy[] discrepancies;
        public string[] applicableRules;
        public InjectionData injection;
        public DecisionOutcome[] outcomes;
    }
    [Serializable] public sealed class DocumentData
    {
        public string id, type, issuer, number, footnote;
        public DocumentField[] fields;
    }
    [Serializable] public sealed class DocumentField
    {
        public string id, label, value, kind;
    }
    [Serializable] public sealed class DialogueLine
    {
        public string id, question, answer, requiresEvidence, unlockEvidence;
    }
    [Serializable] public sealed class WebRecord
    {
        public string id, app, title, source, keywords, snippet, body, evidenceId;
    }
    [Serializable] public sealed class EvidenceData
    {
        public string id, title, detail, source;
    }
    [Serializable] public sealed class Discrepancy
    {
        public string id, fieldA, fieldB, title, explanation, ruleId;
    }
    [Serializable] public sealed class InjectionData
    {
        public string kind, affectedLabel, rawInstruction;
        public string[] requiredEvidence;
    }
    [Serializable] public sealed class DecisionOutcome
    {
        public string decision, citation, consequence, observation, moralReflection;
        public bool procedurallyCorrect;
        public string requiredEvidence, missingEvidenceConsequence;
    }
    [Serializable] public sealed class CaseProgress
    {
        public List<string> openedDocuments = new List<string>();
        public List<string> inspectedFields = new List<string>();
        public List<string> evidence = new List<string>();
        public List<string> discrepancies = new List<string>();
        public List<string> questions = new List<string>();
        public bool restored, manipulationDetected;
    }
    [Serializable] public sealed class DecisionRecord
    {
        public string caseId, traveler, decision, reason, citation, consequence, observation, moralReflection;
        public bool procedurallyCorrect, manipulationDetected, integrityRestored;
        public List<string> evidence = new List<string>();
        public List<string> discrepancies = new List<string>();
    }
    [Serializable] public sealed class ShiftState
    {
        public int version = 1, caseIndex;
        public ShiftPhase phase = ShiftPhase.Briefing;
        public CaseProgress progress = new CaseProgress();
        public List<DecisionRecord> decisions = new List<DecisionRecord>();
        public bool identityRevealed;
    }
}
