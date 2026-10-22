# Adding and editing travelers

The source of truth is **Assets/Checkpoint/Resources/Checkpoint/cases.json**, loaded with Unity JsonUtility. Edit the JSON directly. The top-level cases array determines processing order. Normal cases share the same UI and require no scene edits.

## Workflow

1. Duplicate a case object and give it a unique id.
2. Set actualIdentity and a separate perceivedIdentity object. For ordinary cases their values should match.
3. Add documents with id, type, issuer, number, footnote, and fields.
4. Give each field a case-unique id, label, value, and kind. Use **expiry** for expiration checks, **date** for other dates, and **text** otherwise. Dates use yyyy-MM-dd; validity includes the expiry date.
5. Author discrepancies with rule references. A single-field defect uses fieldA and an empty fieldB; inspecting the field discovers it. A paired discrepancy requires comparing both fields.
6. Add source records and evidence. Evidence is discovered only when pinned.
7. Add dialogue. Optional requiresEvidence gates a question; unlockEvidence adds a clue when asked. Do not create circular or unreachable prerequisites.
8. Define all three outcomes: **Approve**, **Deny**, **Secondary**.
9. Add optional injection metadata, then run tests and play the case.

The repository validates duplicate IDs, dates, rules, record indexes, evidence references, dialogue dependencies, comparison fields, and outcome coverage. Malformed content produces a startup error with details in the Console.

## Identity

Fields: name, birthdate, sex, nationality, passport, portrait.

Portrait keys: mira, tomas, leila, idris, grandmother, sena. Unknown keys use the base portrait. New visual styles can be added in CheckpointIllustration.

A sex marker or appearance mismatch alone should not establish fraud. Author independent corroboration and a relevant rule.

## Records and evidence

Records contain id, app, title, source, keywords, snippet, body, and optional evidenceId.

The app must be exactly **Public Search**, **Government**, **News Archive**, **Social**, or **Travel**. Public Search searches all indexes; other tabs filter first. All normalized query tokens must appear somewhere in the title, keywords, snippet or body. Punctuation and case are ignored. Empty or unknown queries yield zero results. Add alternate spellings and identifiers to keywords without changing search code.

Evidence contains id, title, detail, source. State what the source does and does not establish. A clickable page can be unreliable; provenance is not a truth flag.

## Outcomes

Each of the three outcomes contains:

- decision: exact enum name
- procedurallyCorrect: conformity to the rulebook, not ethical goodness
- citation: rule ID shown after disposition
- consequence: narrative outcome
- observation: institutional interpretation
- moralReflection: optional non-scored consideration of costs and uncertainty
- requiredEvidence: optional clue needed for the procedural result
- missingEvidenceConsequence: explanatory fallback if that clue is absent

A player can always commit an explicit decision. Missing evidence affects evaluation rather than button availability. C03 demonstrates conditional approval after a signed correction.

Citations must refer to rules discoverable before the choice. No secret mandatory clicks or automatic dispositions.

## Manipulation

Optional injection fields: kind, affectedLabel, rawInstruction, requiredEvidence.

- **kinship** activates the grandmother perception until the independent identity and packet evidence are pinned and INTEGRITY CHECK is run.
- Other nonempty kinds use the generic false-authority quarantine presentation.
- rawInstruction is story data, never evaluated or sent to a model. Its visible transcript is authored in a source record.
- Restoration changes display selection and flags, never canonical identity, documents, or decisions.
- A missed kinship attack is isolated at handoff to guarantee the AI revelation. It earns no operator detection credit.
- Repeating a check never adds detection credit.

New categories should use explicit presentation logic rather than interpreting arbitrary story text as code.

## Rules, saves and tests

Top-level rules supply the rulebook. applicableRules highlights relevant rules; the whole rulebook remains visible. shiftDate drives validation. The current briefing/date display is authored for 06 NOV 2038; changing the date also requires updating that presentation copy.

For structural edits, start a new shift. A released version would need save migrations when IDs or schemas change.

Tests assume the reference five-case shift. Update fixture expectations if extending it, and add tests for new validation or consequence behavior.
