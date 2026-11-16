---
applyTo: "src/**/Inventara.*/**/*.cs"
---

# Inventara — Domain Rules

- Source of truth: `docs/brain/10-inventara-context.md`. Do not invent entities, fields, or endpoints outside it.
- Business rules that must hold (and be tested):
  1. An Asset with status `Borrowed` cannot be added to a new Loan.
  2. Approving a Loan changes Loan status AND Asset status in ONE transaction.
  3. Returning an Asset as damaged automatically creates a Maintenance ticket with status `Open`.
  4. Only Admin approves/rejects loans, edits master data, and reads AuditLog.
  5. `AssetCode` is unique. A `Pending` Loan cannot be deleted, only cancelled.
- Enums: `AssetStatus = Available | Borrowed | Maintenance | Retired`, `LoanStatus = Pending | Approved | Rejected | Returned`, `MaintenanceStatus = Open | InProgress | Done`.
- Never call this project SIMIT. Never add fields "just in case".
