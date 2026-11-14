# Local development

- Use `bash scripts/dev.sh` (Aspire AppHost) for all local debugging and visual previews; use the SDK pinned in `global.json` and working rootless Podman.
- Let AppHost manage migrations, API, frontend and smtp4dev together. Do not replace this stack with manually launched standalone preview processes.
- Local mail must use AppHost-managed smtp4dev capture, never a real SMTP provider.
- Standalone published hosts/containers are only for isolated automated release/parity tests, not the development preview.
- Branch new work from `develop` and integrate there. Merging or pushing `main` requires explicit release approval because main pushes deploy production.
