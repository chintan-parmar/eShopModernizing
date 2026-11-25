---
name: mh-modernize
description: Modernize eShopLegacyMVC to a single .NET 10 service on Google Cloud Run, driven by the Modernization Hub assessment report in Docs/mh. Use for any planning, code, container or Terraform work on eShopLegacyMVCSolution.
---

# Modernize eShopLegacyMVC with the Modernization Hub report

## Input

- Report: `Docs/mh/eshop-legacy-mvc.md` (Modernization Hub run 2, codmod 1.1.0, gemini-3.8-flash).
- Code: `eShopLegacyMVCSolution/` only (`src/eShopLegacyMVC`, `eShopPorted`, `eShopLegacy.Utilities`). Leave every other folder in this repository alone.

The report is AI-generated. It is accurate on the main blockers but overstates or invents some details. The code is the source of truth.

## Target

- ONE Cloud Run service: ASP.NET Core on .NET 10, Linux container.
- Out of scope: microservices, Cloud Run Functions, GKE, Compute Engine, Vertex AI, BigQuery, Looker, Pub/Sub, Memorystore. Make the app stateless instead of adding a session store.

## Which parts of the report to use

Use:
- "Modernization Blockers & Priority Matrix": work in its P0 to P3 order.
- "Modernize to Cloud Run": "Detailed Migration Plan", phases 1 to 5.
- Security findings SEC-01 to SEC-07.

Ignore the "Shift to Compute Engine" and "Modernize to Kubernetes Engine" sections.

## Known report errors (do not act on these as written)

- BinaryFormatter: the server only serializes (`SerializeBinary`); `DeserializeBinary` is never called. Replace it with `System.Text.Json`, but do not describe it as a server-side RCE.
- "Connection strings exposed in plaintext": both use Windows auth (`Integrated Security` / `Trusted_Connection`) to LocalDB with no credentials. The real change is a Cloud SQL connection string from Secret Manager.
- "Windows registry bindings": there is no registry use in the code.
- Session: `InProc` session holds only the machine name and start time. Remove it; do not add Memorystore.
- Treat every code snippet in the report as a pointer. Read the actual file before changing it.

When the report and the code disagree, follow the code and record the difference in the walkthrough.

## Infrastructure

Terraform under `infra/` at the repository root:
- Cloud Run service and its service account
- Artifact Registry repository for the image
- Cloud SQL for SQL Server
- Cloud Storage bucket for catalog images (replacing local `Pics/` reads)
- Secret Manager secret for the database connection string

Never run `terraform apply`, `gcloud run deploy` or any command that creates or changes cloud resources.

## Definition of done

Each step is done only when all of these pass:
1. `dotnet build` for the ported solution
2. `docker build` for the service image (Linux base, for example `mcr.microsoft.com/dotnet/aspnet:10.0`)
3. `terraform fmt -check`, `terraform validate` and `terraform plan` in `infra/`

## Working rules

- Start with an implementation plan and wait for approval before changing files.
- Work on branch `poc/antigravity-cloudrun`. One commit per blocker, with a message naming the blocker.
- Do not push and do not open pull requests; the user does that.
- Keep a walkthrough listing what changed, what was verified, and every report finding that the code contradicted.
