# Notes for coding agents

**This repository is public.** Everything committed here is visible to anyone.

- Never commit real network data: no scan output, hostnames, device names, IP or MAC addresses, or service
  lists captured from a real network. Run the `mdns` CLI freely, but keep its output out of the repo.
- Examples, tests and docs use made-up devices ("Printer", "Mac") and documentation or private example
  addresses (e.g. `192.168.1.x`, `192.0.2.x`), never ones observed on a real LAN.
- Don't mention employers, products or internal projects that aren't part of this repository.
- Review the diff for the above before every commit, and don't push without the owner's go-ahead.
