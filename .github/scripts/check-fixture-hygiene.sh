#!/usr/bin/env bash
# Fails if tracked files under tests/ contain a real-looking IBKR account id, email address,
# or a non-empty primaryEmail / dateOfBirth attribute.
# Allowed synthetic values:
#   - account ids U0000000-U0000099 (U0000001 is the anonymized placeholder)
#   - emails at reserved domains that cannot be real (RFC 2606 / RFC 6761):
#     example.com/.net/.org, *.example, *.invalid, *.test, *.localhost
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

scope=(tests)
status=0

account_ids=$(git grep -InoP '(?<![A-Za-z0-9])U(?!00000\d\d(?!\d))\d{7,8}(?!\d)' -- "${scope[@]}" || true)
if [[ -n "$account_ids" ]]; then
  echo "::error::Real-looking IBKR account ids found. Use the anonymized placeholder U0000001 (or U00000xx for extra synthetic accounts)."
  echo "$account_ids"
  status=1
fi

emails=$(git grep -InoiP '[A-Z0-9._%+-]+@[A-Z0-9-]+(\.[A-Z0-9-]+)*\.[A-Z]{2,}' -- "${scope[@]}" \
  | grep -viP '@([a-z0-9-]+\.)*(example\.(com|net|org)|example|invalid|test|localhost)$' || true)
if [[ -n "$emails" ]]; then
  echo "::error::Email addresses found. Use a reserved domain such as user@example.com in synthetic fixtures."
  echo "$emails"
  status=1
fi

pii_attributes=$(git grep -InoP '\b(primaryEmail|dateOfBirth)="[^"]+"' -- "${scope[@]}" || true)
if [[ -n "$pii_attributes" ]]; then
  echo "::error::Non-empty primaryEmail / dateOfBirth found. Strip them with tools/ibkr-fixtures/anonymize_flex.py."
  echo "$pii_attributes"
  status=1
fi

if [[ $status -eq 0 ]]; then
  echo "Fixture hygiene OK."
fi
exit $status
