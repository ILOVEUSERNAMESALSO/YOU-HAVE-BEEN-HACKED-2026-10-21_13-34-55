# Character cards

Select a character asset in this folder to edit their portrait, occupation/purpose,
registration name and DOB, printed passport fields, introduction lines and two farewells.
The registration record is the reference the player compares against the passport.
Each card's Seat Number is printed on the boarding pass alongside their Full Name.

Dates use `dd MMM yyyy`, for example `12 MAR 1988`.
The inspection date is set on **Passport > Decision** in GameIntegration (16 NOV 2026 initially).
Expiry on that date is valid; any earlier expiry is invalid. Impossible/future DOBs and
DOB/name mismatches are also grounds for denial. Feedback lists all detected faults.

To add someone: right-click in Project > Create > Checkpoint > Character Card, fill the
fields and add the asset to **Passport > Decision > Characters**. Assign a sprite from
the portrait texture's expanded entry. The queue uses every non-null, distinct card
once per shuffled batch and avoids immediate repeats across batches.

The ten character cards include intentional document discrepancies. Read the current
asset values: they change as new cases are authored. Emily's passport expires on the
initial inspection date and is intentionally valid. Andre's DOB now matches, ready
for the inventory case.

Inventory items have a name, category, baggage location, intended use and optional
authorization reference. The current rule rejects tools in cabin baggage; personal
items and checked tools do not trigger this rule. Authorization references do not
grant exceptions yet. Empty inventories mean content has not been authored, so the
chatbot is told not to invent belongings. Author these fields before testing baggage
questions. Keep roleplay notes and opening dialogue consistent with the item list.

Arrival Sound is a per-character clip. It sets the mood and is not used in grading.
Returning a stamped passport immediately starts the next visit.

Run **Tools > Checkpoint > Validate Character Cards** for asset, shuffle and rule checks.
