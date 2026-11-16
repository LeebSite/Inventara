---
applyTo: "docs/**/*.md"
---

# Workflow for docs / brain files

- Brain files are short and specific: one topic per file, max ~60 lines. If a file grows past that, split it.
- Every claim in a brain file must be checkable: a file path, a command, or a rule from the project context. No invented endpoints, fields, or libraries.
- When a rule changes, update the brain file in the same task — do not leave the brain out of date.
- Project-specific files live in `docs/brain/10-*-context.md`. General rules stay in `AGENTS.md` / `.github/`. Never put project secrets in any `.md` file.
