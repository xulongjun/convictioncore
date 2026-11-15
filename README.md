# ConvictionCore

Clean rewrite of ConvictionOS with a new architecture.

This repository is **code-only**. Architecture, product, and design documents live in the Cursor Project Context (`docs/`), not in this git tree.

## Status

Scaffold only (TypeScript / Node). PoC implementation waits on open decisions in the architecture doc.

## Layout

```
src/
  domain/       # core concepts & invariants
  portfolio/    # owners, portfolios, broker accounts
  trading/      # import, normalize, reconstruct (PoC path)
  api/          # thin HTTP entry (placeholder)
  platform/     # config, logging ports
```

## Local

```bash
npm install
npm run typecheck
```

Do not commit real broker exports or account identifiers.
