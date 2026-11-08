#!/usr/bin/env bash
# Root-owned forced SSH command. No shell, forwarding or caller-controlled deployment options.
set -euo pipefail
command="${SSH_ORIGINAL_COMMAND:-}"
if [[ ! $command =~ ^deploy\ ([0-9a-f]{40})$ ]]; then
  printf 'Only deploy <40-character-commit-sha> is permitted.\n' >&2
  exit 2
fi
exec sudo -n /usr/local/sbin/monatrox-portfolio-deploy "${BASH_REMATCH[1]}"
