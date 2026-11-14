# Local development

- Use `bash scripts/dev.sh` (Aspire AppHost) for all local debugging and visual previews. Follow the prerequisites in README.md.
- Let AppHost manage migrations, API, frontend and smtp4dev together. Do not replace this stack with manually launched standalone preview processes.
- Local mail must use AppHost-managed smtp4dev capture, never a real SMTP provider.
- Standalone published hosts/containers are only for isolated automated release/parity tests, not the development preview.
