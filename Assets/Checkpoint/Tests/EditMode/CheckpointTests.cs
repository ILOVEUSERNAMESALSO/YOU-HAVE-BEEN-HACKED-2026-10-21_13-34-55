using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Checkpoint.Tests
{
    public class CheckpointTests
    {
        CaseBook book;
        ShiftManager shift;
        [SetUp] public void Setup() { book = CaseRepository.Load(); shift = new ShiftManager(book); }
        void Case(int index)
        {
            shift.StartShift();
            for (int i = 0; i < index; i++) { shift.Decide(Decision.Secondary, "test"); shift.Next(); }
        }
        [Test] public void LoadsFiveCasesAndSixRules() { Assert.AreEqual(5, book.cases.Length); Assert.AreEqual(6, book.rules.Length); }
        [Test] public void EveryCaseHasAllOutcomesAndIndependentIdentities()
        {
            foreach (var c in book.cases) { Assert.AreEqual(3, c.outcomes.Length); Assert.AreNotSame(c.actualIdentity, c.perceivedIdentity); }
        }
        [Test] public void DuplicateCaseIdsFailValidation() { book.cases[1].id = book.cases[0].id; Assert.Throws<InvalidOperationException>(() => CaseRepository.Validate(book)); }
        [Test] public void InvalidEvidenceReferenceFailsValidation() { book.cases[0].records[0].evidenceId = "absent"; Assert.Throws<InvalidOperationException>(() => CaseRepository.Validate(book)); }
        [Test] public void InvalidDateFailsValidation() { book.cases[0].documents[0].fields.Last().value = "not-a-date"; Assert.Throws<InvalidOperationException>(() => CaseRepository.Validate(book)); }
        [Test] public void PassportExpirationIsInclusive()
        {
            var f = new DocumentField {kind="expiry",value=book.shiftDate};
            Assert.IsFalse(DocumentValidator.IsExpired(f,book.shiftDate));
            f.value="2038-11-05"; Assert.IsTrue(DocumentValidator.IsExpired(f,book.shiftDate));
        }
        [Test] public void CaseTwoPassportIsExpiredButPermitIsValid()
        {
            var fields=book.cases[1].documents.SelectMany(d=>d.fields);
            Assert.IsTrue(DocumentValidator.IsExpired(fields.First(f=>f.id=="passport-expiry"),book.shiftDate));
            Assert.IsFalse(DocumentValidator.IsExpired(fields.First(f=>f.id=="permit-expiry"),book.shiftDate));
        }
        [Test] public void InspectingExpiryRecordsOneIssueNotDuplicates()
        {
            Case(1); shift.InspectField("passport-expiry"); shift.InspectField("passport-expiry");
            Assert.AreEqual(1,shift.State.progress.discrepancies.Count);
        }
        [Test] public void MaterialPairRequiresBothFields()
        {
            Case(2); shift.InspectField("watch-passport"); Assert.IsEmpty(shift.State.progress.discrepancies);
            shift.CompareFields("watch-passport","permit-passport"); Assert.Contains("watch-passport",shift.State.progress.discrepancies);
        }
        [TestCase("")] [TestCase("   ")] [TestCase("!!!")] [TestCase("no-such-person")]
        public void EmptyOrUnknownSearchHasNoResults(string query) { Assert.IsEmpty(FakeBrowser.Search(book.cases[0],"Public Search",query)); }
        [Test] public void SearchIgnoresCaseAndPunctuation() { Assert.IsNotEmpty(FakeBrowser.Search(book.cases[3],"Public Search","as-90117")); }
        [Test] public void SearchFiltersByApp() { Assert.IsTrue(FakeBrowser.Search(book.cases[2],"News Archive","Orr").All(r=>r.app=="News Archive")); }
        [Test] public void NewRecordNeedsNoSearchCodeChanges()
        {
            var c=book.cases[0];
            c.records=c.records.Concat(new[]{new WebRecord{app="Social",title="Added record",keywords="uniquequartz",body="",snippet=""}}).ToArray();
            Assert.AreEqual(1,FakeBrowser.Search(c,"Public Search","uniquequartz").Count);
        }
        [Test] public void DecisionsOnlyDuringProcessing()
        {
            Assert.Throws<InvalidOperationException>(()=>shift.Decide(Decision.Approve,""));
            shift.StartShift(); shift.Decide(Decision.Approve,"");
            Assert.Throws<InvalidOperationException>(()=>shift.Decide(Decision.Deny,""));
            Assert.AreEqual(1,shift.State.decisions.Count);
        }
        [Test] public void AllFiveCasesReachEvaluation()
        {
            shift.StartShift();
            foreach(var c in book.cases) { shift.Decide(Decision.Secondary,"test"); shift.Next(); }
            Assert.AreEqual(ShiftPhase.Evaluation,shift.State.phase);
            Assert.AreEqual(5,shift.State.decisions.Count);
            shift.Next(); Assert.AreEqual(5,shift.State.decisions.Count);
        }
        [Test] public void RestartClearsEveryMutableState()
        {
            Case(3); shift.CollectEvidence("identity"); shift.CollectEvidence("packet"); shift.RestoreIntegrity();
            shift.Decide(Decision.Secondary,"saved reason"); shift.Restart();
            Assert.AreEqual(ShiftPhase.Briefing,shift.State.phase); Assert.AreEqual(0,shift.State.caseIndex);
            Assert.IsEmpty(shift.State.decisions); Assert.IsEmpty(shift.State.progress.evidence);
            Assert.IsFalse(shift.State.progress.restored); Assert.IsFalse(shift.State.identityRevealed);
        }
        [Test] public void GrandmaPerceptionNeverMutatesCanonicalFacts()
        {
            Case(3); string original=JsonUtility.ToJson(shift.Current);
            Assert.AreEqual("Nana",PerceptionManager.DisplayIdentity(shift.Current,shift.State.progress).name);
            shift.CollectEvidence("identity"); shift.CollectEvidence("packet"); Assert.IsTrue(shift.RestoreIntegrity());
            Assert.AreEqual("Idris Vale",PerceptionManager.DisplayIdentity(shift.Current,shift.State.progress).name);
            Assert.AreEqual(original,JsonUtility.ToJson(shift.Current));
            Assert.AreEqual(3,shift.State.decisions.Count);
        }
        [Test] public void RestorationNeedsIndependentIdentityAndPayload()
        {
            Case(3); Assert.IsFalse(shift.RestoreIntegrity());
            shift.CollectEvidence("packet"); Assert.IsFalse(shift.RestoreIntegrity());
            shift.CollectEvidence("identity"); Assert.IsTrue(shift.RestoreIntegrity());
            Assert.IsFalse(shift.RestoreIntegrity());
        }
        [Test] public void ReadingAndPinningAreDifferentActions()
        {
            Case(3); FakeBrowser.Search(shift.Current,"Government","AS-90117");
            Assert.IsEmpty(shift.State.progress.evidence); Assert.IsFalse(shift.RestoreIntegrity());
        }
        [Test] public void DialogueUnlocksOnlyAfterEvidence()
        {
            Case(2); Assert.AreEqual("Find the supporting evidence first.",shift.Ask("correction"));
            shift.CollectEvidence("correction"); Assert.That(shift.Ask("correction"),Does.Contain("Two digits"));
        }
        [Test] public void EvidenceIsIdempotentAndRejectsUnknownIds()
        {
            shift.StartShift(); Assert.IsFalse(shift.CollectEvidence("invalid"));
            Assert.IsTrue(shift.CollectEvidence("clearance")); Assert.IsFalse(shift.CollectEvidence("clearance"));
        }
        [Test] public void CorrectingTheRecordChangesApprovalEvaluation()
        {
            Case(2); var without=shift.Decide(Decision.Approve,"test"); Assert.IsFalse(without.procedurallyCorrect);
            shift.Restart(); Case(2); shift.CollectEvidence("correction");
            Assert.IsTrue(shift.Decide(Decision.Approve,"test").procedurallyCorrect);
        }
        [TestCase(Decision.Approve,false)] [TestCase(Decision.Deny,true)] [TestCase(Decision.Secondary,false)]
        public void MoralCaseRecordsDistinctConsequences(Decision decision,bool correct)
        {
            Case(4); shift.CollectEvidence("risk"); var r=shift.Decide(decision,"Protection concern");
            Assert.AreEqual(correct,r.procedurallyCorrect); Assert.AreEqual("Protection concern",r.reason);
            Assert.IsNotEmpty(r.moralReflection); Assert.Contains("risk",r.evidence);
        }
        [Test] public void EvaluationCountsActualDiscoveries()
        {
            Case(1); shift.InspectField("passport-expiry"); shift.CollectEvidence("expired-record");
            shift.Decide(Decision.Deny,"R1");
            var s=EvaluationManager.Evaluate(shift.State);
            Assert.AreEqual(2,s.processed); Assert.AreEqual(1,s.correct); Assert.AreEqual(1,s.inconsistencies); Assert.AreEqual(1,s.evidence);
        }
        [Test] public void SaveRoundTripPreservesInvestigationAndPhase()
        {
            Case(3); shift.CollectEvidence("identity"); shift.Ask("document");
            var restored=JsonUtility.FromJson<ShiftState>(JsonUtility.ToJson(shift.State));
            var other=new ShiftManager(book); Assert.IsTrue(other.TryResume(restored));
            Assert.AreEqual(3,other.State.caseIndex); Assert.Contains("identity",other.State.progress.evidence);
        }
        [Test] public void InvalidSaveIsRejectedWithoutMutation()
        {
            var saved=new ShiftState{caseIndex=99}; Assert.IsFalse(shift.TryResume(saved));
            saved=new ShiftState{phase=ShiftPhase.Evaluation}; Assert.IsFalse(shift.TryResume(saved));
            saved=new ShiftState(); saved.progress.evidence.Add("unknown"); Assert.IsFalse(shift.TryResume(saved));
            Assert.AreEqual(ShiftPhase.Briefing,shift.State.phase);
        }
        [Test] public void SaveRejectsRemovedContentReferences()
        {
            var s=new ShiftState(); s.progress.openedDocuments.Add("missing"); Assert.IsFalse(shift.ValidSave(s));
            s=new ShiftState(); s.progress.questions.Add("missing"); Assert.IsFalse(shift.ValidSave(s));
            s=new ShiftState(); s.progress.restored=true; s.progress.manipulationDetected=true; Assert.IsFalse(shift.ValidSave(s));
            shift.StartShift(); shift.Decide(Decision.Approve,"test"); shift.State.decisions[0].evidence.Add("missing");
            Assert.IsFalse(shift.ValidSave(shift.State));
        }
        [Test] public void DecisionSnapshotsDoNotChangeWithNextCase()
        {
            shift.StartShift(); shift.CollectEvidence("clearance"); var record=shift.Decide(Decision.Approve,"Valid");
            shift.Next(); shift.CollectEvidence("expired-record");
            CollectionAssert.AreEqual(new[]{"clearance"},record.evidence);
        }
    }
}
