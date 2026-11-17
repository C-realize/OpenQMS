# OpenQMS.net

OpenQMS.net is a lightweight, scalable and cloud-native Quality Management System (QMS) application based on 15+ years of experience in the life science industry.
For instructions on how to get started, have a look at the [documentation](https://www.openqms.net/docs).

This program is offered under a commercial and under the AGPL license.
For commercial licensing, contact us at https://www.c-realize.com/contact.  For AGPL licensing, see below.

AGPL license:
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

Note that freedom comes with responsibilities. If you are unsure how to interpret the terms in the AGPL, you can check out https://www.gnu.org/licenses/gpl-faq.html or contact us.

## Controlled-document and training first release

The first-release portal adds a controlled-document register, immutable revision records, review/approval/effectivity, role-specific document grants, training assignments, and an audit trail. It seeds the 18-item register supplied for this project, but no source document files. Existing operational modules for products, processes, equipment, materials, changes, deviations, CAPA, and the previous document/training screens are deliberately excluded from routing in this release; their existing database tables are not deleted.

### Local setup

The current application configuration uses SQLite (`OpenQMS.db`) and applies EF Core migrations at startup. Set an initial administrator address and SMTP settings through user secrets or environment variables; never commit credentials:

```text
BootstrapAdmin__Email=quality-owner@example.eu
Email__From=qms@example.eu
Email__Host=smtp.example.eu
Email__Port=587
Email__UseSsl=true
Email__UserName=<provider account, if required>
Email__Password=<secret, if required>
Email__PublicBaseUrl=https://qms.example.eu/
```

In development, configure these values with the .NET Secret Manager or local environment. Email credentials must be supplied together. The sender and HTTPS public base URL are required in production, and email delivery errors are surfaced rather than simulated. The bootstrap address creates a passwordless, unconfirmed System Administrator account; the owner must receive and confirm the email before sign-in. Once an administrator exists, `BootstrapAdmin__Email` can be removed. Production startup refuses to run without an existing System Administrator or a configured bootstrap address.

For deployments behind a reverse proxy, configure `ForwardedHeaders__KnownProxies__0` (and further indexed entries as needed) with the actual trusted proxy IPs. Forwarded client IPs are used for the email-link rate limit. Do not trust arbitrary forwarded headers.

### Configuration and go-live limits

- The only configured database provider is SQLite. The repository is not yet qualified for a managed multi-instance/cloud database; select and implement a provider with matching migrations and prove backup/restore before production. Keep the SQLite database on persistent, access-controlled storage even in test deployments.
- Uploaded revision files are stored in the configured database. No EU hosting region, storage region, email processor, backup service, monitoring, malware scanning, or retention policy is configured by this repository. Select and qualify those controls for the company's deployment before loading controlled content.
- The six starter roles are seeded. New role grants are empty except for System Administrator's application-level administration; explicitly review and configure each document's role grants. Auditor permissions are enforced as read-only. Role assignments and document grants require company approval.
- New effective revisions create training assignments when training is enabled for the register item. An optional per-document due window controls overdue status. Completion is a user-entered evidence statement and requires local approval as an acceptable training record.
- Audit events are persisted in the application database, but no backup, retention, export, or tamper-resistant archival process is configured here.
- This software is not certified, validated, or automatically compliant with ISO 9001 or EU MDR. Validate the selected deployment, procedures, access matrix, data migration, backup/restore, and operational responsibilities before controlled use.

### Validation handoff

Before go-live, the company should approve the role/document matrix; test denied as well as permitted reads, downloads, review, and approval; verify author/reviewer/approver separation; verify future effectivity and superseded revision retention; exercise training assignment, due/overdue, evidence, and acknowledgment; test email confirmation, one-time sign-in, expiration, rate limiting, and proxy behavior; and document database backup/restore, incident response, access removal, retention, and change control. Preserve the resulting protocols and objective evidence under the company's validation process.

Run the automated rule tests with `dotnet test OpenQMS.sln`.