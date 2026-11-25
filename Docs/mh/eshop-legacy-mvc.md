# Project Modernization Report

# Table of Contents

1. [Report Metadata](#report-metadata)
1. [Summary](#summary)
1. [About this Project](#about-this-project)
    1. [Assessment Overview](#assessment-overview)
    1. [Architecture](#architecture)
    1. [Technology Stack](#technology-stack)
    1. [APIs and Endpoints](#apis-and-endpoints)
1. [Assessment Findings](#assessment-findings)
    1. [Security and Compliance](#security-and-compliance)
    1. [Modernization Challenges](#modernization-challenges)
    1. [Modernization Opportunities](#modernization-opportunities)
1. [Cloud Migration](#cloud-migration)
    1. [Migration Overview](#migration-overview)
    1. [Shift to Compute Engine](#shift-to-compute-engine)
    1. [Modernize to Kubernetes Engine](#modernize-to-kubernetes-engine)
    1. [Modernize to Cloud Run](#modernize-to-cloud-run)


## Report Metadata

**Project:** eShopLegacyMVCSolution

**Report ID:** CM-20261123-225754

**Generated:** Nov 23, 2026, 10:57 PM

## Summary
#### Executive Summary

The eShop application is an enterprise catalog management system that enables catalog administrators, merchandisers, and warehouse specialists to manage product inventories, pricing, restock thresholds, and merchandise categorization, while providing programmatic endpoints for downstream e-commerce storefronts. Structurally, the application is a monolithic web service currently divided across an original legacy ASP.NET MVC 5 implementation targeting .NET Framework 4.7.2 and a transitional, incomplete ASP.NET Core 2.2 port compiled against .NET Framework 4.6.1 (`net461`). The data tier relies on Microsoft SQL Server managed through Entity Framework, paired with local disk storage for product imagery.

### Key Findings

An analysis of the codebase reveals several critical architectural, operational, and security risks:

*   **Operating System and Runtime Lock-in**: Deep dependencies on legacy `System.Web` pipelines, IIS execution modules, and the Windows-only .NET Framework runtime prevent cross-platform execution on Linux and incur unnecessary Windows Server licensing and compute expenses.
*   **Critical Security Vulnerabilities**: The application utilizes `BinaryFormatter` for stream serialization, introducing acute remote code execution (RCE) exposure on an API that modern .NET explicitly blocks. Additionally, all data-modifying endpoints lack authentication and role-based access controls, image retrieval routines are vulnerable to directory traversal, and database connection strings are exposed in plaintext configuration files.
*   **Barriers to Cloud Scalability**: Reliance on in-process session state (`InProc`), synchronous database queries that saturate thread pools, and local server disk reads for static media prevent dynamic horizontal autoscaling and conflict with stateless cloud execution.
*   **Technical Debt and Fragmented Codebase**: The coexistence of the legacy MVC solution alongside a partially ported ASP.NET Core project creates duplicate domain models, conflicting configuration schemes, and excessive maintenance overhead.

### Strategic Recommendations

To resolve these architectural limitations while preserving system boundaries, the application should be modernized as a unified, single-service container deployed to Google Cloud:

*   **Consolidate and Upgrade to .NET 10 on Linux**: Unify the codebase into a single ASP.NET Core application targeting .NET 10. Eliminate obsolete libraries by upgrading to Entity Framework Core 10, replacing `BinaryFormatter` with `System.Text.Json`, standardizing asynchronous execution patterns (`async`/`await`), and implementing OpenID Connect or IAM-based authentication.
*   **Adopt Google Cloud Run for Containerized Hosting**: Package the .NET 10 application into a secure Linux container and deploy it to Google Cloud Run as a managed monolith. This architecture avoids unnecessary microservice complexity while delivering automatic request-driven scaling, scale-to-zero cost optimization, and zero operating system maintenance.
*   **Leverage Google Cloud Managed Services**:
    *   **Data Tier**: Migrate database workloads to Google Cloud SQL for SQL Server, retaining T-SQL sequence compatibility while connecting securely over private Virtual Private Cloud (VPC) channels.
    *   **Media Storage**: Offload static catalog imagery from the local filesystem to Google Cloud Storage buckets, serving assets directly or via Cloud CDN to improve responsiveness.
    *   **Security and Configuration**: Secure database credentials and secrets within Google Cloud Secret Manager, and protect public entry points using Google Cloud Armor for automated DDoS and WAF protection.
    *   **Observability**: Transition application diagnostics from file-based `log4net` appenders to structured console output ingested directly by Google Cloud Logging and Google Cloud Monitoring.

#### Report Information

##### Estimated Application Metrics (AI-generated)

| Metric | Value |
| :--- | :--- |
| Scanned Lines of Code (LOC) | 6,197 |
| Scanned Files | 149 |
| API Endpoints | 15 |

##### AI-Identified Security Findings

| Metric | Value |
| :--- | :--- |
| Known Vulnerabilities | 7 |
| Critical/High Security Findings | 43% |

#### Generation Details

 - **Tool**: Google Cloud Code Modernization Assessment Tool v1.1.0
 - **Modelset**: gemini-3.8-flash
 - **Generated By**: chintan_parmar

#### Command Line Options

The report was generated using the following CLI command:

 ```plaintext
codmod create \
    -c /home/chintan_parmar/eShopModernizing/eShopLegacyMVCSolution \
    -o /home/chintan_parmar/mh-reports/eshop-legacy-mvc.md \
    --format markdown \
    -r global \
    --intent MICROSOFT_MODERNIZATION \
    --force-exclude \.(js|css|map|min\.js)$ \
    --context <user-provided-context>
 ```

#### User Provided Context

```plaintext
Target: Cloud Run, .NET 10 on Linux. Keep it a single service; no microservices split, no Vertex AI/BigQuery/PubSub additions.
```

#### Notice Regarding AI-Generated Assessment

This report leverages advanced AI for analysis, providing rapid, evidence-based insights.

Please note that results are AI-derived estimates and may vary between assessments, even for the same codebase. Human review and validation are crucial for all findings and recommendations.

## About this Project
### Assessment Overview
#### Purpose of the System

The eShopLegacy system is an enterprise catalog management application designed to maintain and curate product inventories for an e-commerce platform [^1]. It functions as an administrative backbone, enabling store administrators and merchandising teams to organize, categorize, update, and manage products, associated brand identities, and product types.

The system addresses several critical operational problems:
- **Product Information Centralization**: It maintains a structured catalog database containing essential item attributes, such as descriptions, unit pricing, inventory levels, and classification data [^2].
- **Inventory Threshold Management**: It provides visibility into warehouse inventory parameters, including real-time stock levels, restock alerts, maximum capacity constraints, and reorder flags [^3].
- **Catalog Asset Organization**: It connects catalog records to external media assets (such as product imagery) to ensure consistent presentation across client touchpoints [^4].
- **Downstream Consumer Feeds**: It provides both traditional web interfaces and auxiliary programmatic endpoints to feed product and brand data to external consumers and services [^5].

In the context of the modernization initiative, this application is positioned to transition from legacy .NET Framework and interim .NET Core runtimes to a unified **.NET 10** single-service application deployed in a Linux container on **Google Cloud Run**, preserving its monolithic boundary while modernizing the hosting model.

---

#### Primary Users

The application caters to distinct internal user groups and programmatic consumers.

| User Persona | Role Description | Primary Objectives | Typical Interaction Channel |
| :--- | :--- | :--- | :--- |
| **Catalog Administrator** | Back-office merchandiser or inventory controller | Creates new catalog listings, updates pricing, monitors inventory stock, and configures restock thresholds [^3]. | Responsive Web UI (ASP.NET MVC Razor Views) [^1] |
| **Warehouse / Inventory Specialist** | Fulfillment or logistics personnel | Monitors physical warehouse limits against catalog records (`MaxStockThreshold`, `AvailableStock`) and tracks replenishment status (`OnReorder`) [^3]. | Catalog Web Interface and Details Views [^2] |
| **Downstream E-Commerce Frontends** | Client storefront applications or external client services | Consumes brand listings, product records, and binary-serialized payloads for catalog rendering [^5]. | REST / HTTP Web APIs (`/api/brands`, `/api/files`) [^5] |
| **Site Operations / DevOps Engineer** | Platform reliability and cloud infrastructure engineers | Verifies application operational status, runtime configurations (e.g., database mode vs. mock dataset mode), and system telemetry [^6]. | Cloud logging, host metrics, and runtime health endpoints [^7] |

---

#### Core Functionality

The core functions of the system center on inventory catalog curation, media dissemination, and lightweight API access.

| Functional Area | Description | Supporting Artifacts |
| :--- | :--- | :--- |
| **Paginated Catalog Browsing** | Enables users to view product records in a structured tabular format with adjustable page sizes, thumbnail previews, dynamic URL placeholders, and pagination navigation [^8]. | `CatalogController.cs`, `Index.cshtml`, `CatalogTable.cshtml` |
| **Product Record Curation (CRUD)** | Comprehensive management allowing creation, viewing, updating, and deletion of individual product entries, including client-side and model validation on pricing, types, and stock limits [^9]. | `CatalogController.cs`, `CatalogItem.cs`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml` |
| **Brand & Category Taxonomy** | Classifies merchandise under distinct product types (e.g., Mug, T-Shirt, Sheet) and brands (e.g., Azure, .NET, Visual Studio) using explicit foreign key mappings [^10]. | `CatalogType.cs`, `CatalogBrand.cs`, `CatalogItemConfig.cs` |
| **Dynamic Media Delivery** | Resolves and streams raw binary image assets to the browser or external clients, dynamically determining MIME content types (PNG, JPEG, SVG, GIF) based on file metadata [^11]. | `PicController.cs` |
| **Programmatic Data Access** | Exposes HTTP endpoints returning JSON representations of brands and serialized binary streams of data transfer objects (`BrandDTO`) for legacy client compatibility [^5] [^12]. | `BrandsController.cs`, `FilesController.cs`, `Serializing.cs` |
| **Dual Operational Modes** | Allows the application to switch transparently between a live SQL Server relational database backing and an in-memory preconfigured mock dataset via configuration toggles [^13]. | `ApplicationModule.cs`, `CatalogServiceMock.cs`, `CatalogService.cs` |

---

#### Key User Journeys

##### 1. Catalog Browsing and Search Navigation
1. The administrator accesses the catalog root path (`/Catalog`) [^8].
2. The system loads items for the current page offset using pagination parameters (`pageSize` and `pageIndex`) [^8].
3. For each catalog item, image placeholders are dynamically generated to route to the binary asset handler (`/items/{id}/pic`) [^4].
4. The user navigates across pages using the pagination bar, displaying total counts and active page locations [^14].

##### 2. Product Creation and Onboarding
1. The merchandiser navigates to the item creation portal (`/Catalog/Create`) [^9].
2. The application populates dropdown selections for available brands and product types from the catalog domain service [^9].
3. The merchandiser inputs details, ensuring pricing conforms to positive two-decimal currency constraints and stock levels adhere to specified bounds (0 to 10,000,000 units) [^3].
4. Upon form validation and submission with anti-forgery protection, the item is persisted with an automatically generated identifier, and the user is redirected back to the updated catalog table [^9].

##### 3. Inventory and Price Maintenance
1. The inventory manager identifies an item requiring price adjustments or stock reconciliation and opens the edit interface (`/Catalog/Edit/{id}`) [^15].
2. The application retrieves the item's current values while locking the read-only image filename to prevent accidental path alterations [^16].
3. The manager modifies operational variables such as `AvailableStock`, `RestockThreshold`, `MaxStockThreshold`, or toggle flags like `OnReorder` [^3] [^16].
4. On save, the persistence layer updates the entity state in the database, ensuring changes immediately reflect across the platform [^17].

##### 4. Media Asset Retrieval by Downstream Clients
1. An external storefront or catalog listing page requests a product image via `GET /items/{catalogItemId}/pic` [^11].
2. The server queries the catalog service to verify the product exists and locates the associated file on storage media [^11].
3. The server computes the appropriate MIME header (`image/png`, `image/jpeg`, etc.) and streams the byte array directly back to the client [^11].

##### 5. Programmatic Brand Ingestion
1. An integrated micro-service or downstream client initiates an HTTP `GET` request to `/api/brands` [^18].
2. The controller queries the catalog abstraction layer to obtain all configured brands [^18].
3. The response is returned as serialized JSON, supplying identifiers and brand names for client rendering [^18].

```
+-----------------------------------------------------------------------------------+
|                              Key System Workflows                                 |
+-----------------------------------------------------------------------------------+
|                                                                                   |
|  [ Storefront / Web UI ]                                                          |
|           │                                                                       |
|           ├── (1) Browse Catalog ──────────► [ CatalogController: Index() ]       |
|           │                                          │                            |
|           ├── (2) Create / Edit Item ──────► [ CatalogController: Create/Edit() ] |
|           │                                          │                            |
|           └── (3) Fetch Picture ───────────► [ PicController: Index() ]           |
|                                                      │                            |
|  [ Downstream APIs / Integration ]                   │                            |
|           │                                          ▼                            |
|           ├── (4) Fetch Brands ────────────► [ ICatalogService ]                  |
|           │                                    │            │                     |
|           └── (5) Serialized BrandDTO ─────►   │            │                     |
|                                                ▼            ▼                     |
|                                         [ SQL Database ] [ Mock In-Memory ]       |
|                                                                                   |
+-----------------------------------------------------------------------------------+
```

---

#### Modernization Context

The application is structured as a single self-contained web service. Upgrading from legacy .NET Framework 4.7.2 and .NET Core 2.2 to **.NET 10** on **Linux** will eliminate legacy Windows IIS dependencies (e.g., `Server.MapPath`, `System.Web` pipelines, and legacy ASP.NET modules) [^11] [^19]. 

Retaining the application as a single deployable unit aligns directly with running containerized workloads on **Google Cloud Run**, establishing a portable, scalable, and cost-efficient foundation on Google Cloud.

---

[^1]: `src/eShopLegacyMVC/Views/Shared/_Layout.cshtml`: lines 17-19 - Catalog manager (MVC) header branding in layout view.
[^2]: `eShopPorted/Models/CatalogItem.cs`: lines 13-39 - Properties defining catalog entity data structures including names, descriptions, and pricing.
[^3]: `eShopPorted/Models/CatalogItem.cs`: lines 41-60 - Stock, restock threshold, max stock threshold, and reorder status validation attributes.
[^4]: `src/eShopLegacyMVC/Controllers/CatalogController.cs`: lines 162-165 - AddUriPlaceHolder() populates item picture routing URIs.
[^5]: `eShopPorted/Controllers/Api/BrandsController.cs`: lines 18-22 - Index() exposes catalog brands via HTTP GET.
[^6]: `eShopPorted/appsettings.json`: lines 8-11 - Operational settings toggling UseMockData and logging levels.
[^7]: `src/eShopLegacyMVC/ApplicationInsights.config`: lines 1-15 - Telemetry module definitions for application performance tracking.
[^8]: `eShopPorted/Controllers/CatalogController.cs`: lines 24-30 - Index() handles paginated catalog item retrieval and logging.
[^9]: `eShopPorted/Controllers/CatalogController.cs`: lines 46-70 - Create() GET and POST action methods for catalog item instantiation and validation.
[^10]: `eShopPorted/Models/Config/CatalogItemConfig.cs`: lines 27-33 - Entity relationships mapping CatalogBrand and CatalogType to CatalogItem.
[^11]: `eShopPorted/Controllers/PicController.cs`: lines 23-47 - Index() loads image file bytes from disk and determines MIME content type.
[^12]: `eShopPorted/Controllers/Api/FilesController.cs`: lines 19-32 - Index() binary-serializes BrandDTO collection using Serializing utility.
[^13]: `eShopPorted/Modules/ApplicationModule.cs`: lines 15-28 - Load() registers either CatalogServiceMock or CatalogService based on configuration flag.
[^14]: `eShopPorted/Views/Catalog/Index.cshtml`: lines 19-38 - Paging control navigation displaying items per page and page indicators.
[^15]: `eShopPorted/Controllers/CatalogController.cs`: lines 73-88 - Edit() GET action retrieves existing catalog item for modification.
[^16]: `eShopPorted/Views/Catalog/Edit.cshtml`: lines 46-51 - Read-only PictureFileName input and editable threshold fields.
[^17]: `eShopPorted/Services/CatalogService.cs`: lines 54-58 - UpdateCatalogItem() modifies entity state and commits changes via DbContext.
[^18]: `src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs`: lines 20-25 - Get() Web API endpoint returning brands collection.
[^19]: `src/eShopLegacyMVC/Web.config`: lines 28-36 - Legacy system.web configuration specifying targetFramework 4.7.2, InProc sessionState, and HTTP modules.

### Architecture
#### Purpose of the Architecture

The architecture of the eShop application provides a centralized catalog management system designed to manage merchandise items, catalog types, and associated brands. The primary architectural goals of the codebase are separation of concerns, flexibility during local development and testing, and maintainability.

The system decouples user interface workflows from direct database manipulation through an abstraction layer based on `ICatalogService` [^6]. Key architectural decisions and their rationales include:

- **Pluggable Data Access Strategy**: The application integrates Autofac to configure dependency injection, enabling a configuration-driven toggle between a mock in-memory data store (`CatalogServiceMock`) and a persistent relational database service (`CatalogService`) [^7] [^8]. This allows developers to run, verify, and test presentation flows locally without an active SQL Server instance.
- **HiLo Sequence Key Generation**: In the legacy MVC implementation, unique database identifiers are managed using custom HiLo database sequences (`CatalogItemHiLoGenerator`) rather than standard auto-incrementing identity columns [^9]. This minimizes database roundtrips when inserting batch records.
- **Transitional Porting Architecture**: The repository contains two coexisting implementations: the legacy ASP.NET MVC 5 solution (`eShopLegacyMVC`) and a transitional ASP.NET Core solution (`eShopPorted`). This structure was instituted to bridge legacy .NET Framework patterns toward modern cross-platform development while sharing foundational domain logic.

#### .NET Version and Deployment Details

The solution contains projects with differing target frameworks, reflecting an active migration phase from legacy .NET Framework towards ASP.NET Core:

| Project | Target Framework | Application Framework | Operating System Affinity |
| :--- | :--- | :--- | :--- |
| `eShopLegacyMVC` | .NET Framework 4.7.2 [^2] | ASP.NET MVC 5.2.7 / Web API 5.2.7 | Windows Only |
| `eShopPorted` | .NET Framework 4.6.1 [^3] | ASP.NET Core 2.2 / EF Core 2.2.6 | Windows Only (via `net461`) |
| `eShopLegacy.Utilities` | .NET Framework 4.6.1 [^5] | .NET Framework Class Library | Windows Only |

##### Current Deployment Model
The codebase exhibits architectural attributes tied directly to Windows and Internet Information Services (IIS):
- The `eShopLegacyMVC` project uses classic ASP.NET configuration schemas (`Web.config`), `Global.asax`, IIS HTTP handlers (`System.Web.Handlers.TransferRequestHandler`), and native Windows IIS execution modules [^10] [^11].
- In-process HTTP session state (`<sessionState mode="InProc" />`) and custom request hooks (`HostingEnvironment.ApplicationPhysicalPath`, `Server.MapPath`) tie runtime execution to local Windows directory paths [^10] [^12] [^13].
- Telemetry modules configure Windows-specific performance counters using native processes (`??APP_WIN32_PROC??`, `??APP_W3SVC_PROC??`) [^14].

Current deployments target traditional on-premises Windows Servers or Windows Server Virtual Machines running IIS.

##### Modernization Target Deployment
Per project requirements, the monolithic application is slated for consolidation and migration to **.NET 10 on Linux** as a single containerized web service deployed to **Google Cloud Run**. Transitioning from Windows/.NET Framework to Linux/.NET 10 eliminates Windows Server licensing costs, simplifies runtime execution through containerization, and provides automated horizontal scaling natively within Google Cloud.

#### Main Components and Roles

The overall architecture is comprised of three primary projects within the solution structure [^1]:

```
+-------------------------------------------------------------------------+
|                               eShop Solution                            |
|                                                                         |
|  +--------------------+   +--------------------+   +-----------------+  |
|  |  eShopLegacyMVC    |   |    eShopPorted     |   | eShopLegacy.    |  |
|  | (.NET Fwk 4.7.2)   |   | (Core on net461)   |   | Utilities       |  |
|  | - MVC Controllers  |   | - Core Controllers |   | - Serialization |  |
|  | - Web API Endpoints|   | - EF Core DbContext|   +--------+--------+  |
|  | - EF 6 DbContext   |   | - Core Views       |            ^           |
|  +---------+----------+   +---------+----------+            |           |
|            |                        |                       |           |
|            +------------------------+-----------------------+           |
|                                                                         |
+-------------------------------------------------------------------------+
```

| Component | Responsibility / Role |
| :--- | :--- |
| **`eShopLegacyMVC`** | The original monolithic web application. Provides full CRUD Razor views for managing products, hosts internal Web API endpoints for brands and binary payloads, and persists data via Entity Framework 6 [^2] [^15] [^16]. |
| **`eShopPorted`** | A partially migrated ASP.NET Core 2.2 implementation operating on top of `net461`. Implements Microsoft.AspNetCore.Mvc controllers, Razor templates, and Microsoft.EntityFrameworkCore 2.2 data models [^3] [^17] [^18]. |
| **`eShopLegacy.Utilities`** | A shared utility library hosting binary serialization and deserialization routines via `BinaryFormatter` [^4]. |
| **`CatalogService`** | Implements the core business workflows (`ICatalogService`) for catalog querying, paging, updates, and creation against the relational database [^19] [^20]. |
| **`CatalogServiceMock`** | Provides an in-memory catalog repository utilizing preconfigured static test data (`PreconfiguredData`), supporting standalone deployment and testing [^21] [^22]. |
| **`CatalogDBContext`** | ORM context mapping domain models (`CatalogItem`, `CatalogBrand`, `CatalogType`) to the underlying relational schema [^18] [^23]. |

These components collectively manage the catalog lifecycle, handling requests from both browser-based users and programmatic API clients.

#### Relationships and Interactions

Interactions within the architecture follow a synchronous, controller-mediated pattern:

1. **Request Intake**:
   - Web browser and HTTP clients dispatch requests for catalog administration views (e.g., `/Catalog/Index`, `/Catalog/Create`) or REST endpoints (e.g., `/api/Brands`, `/api/Files`) [^16] [^24] [^25].
2. **Dependency Resolution**:
   - The application bootstrap (`Global.asax.cs` in legacy MVC or `Startup.cs` in the ported service) initializes the Autofac container with `ApplicationModule` [^7] [^8] [^11].
   - If `UseMockData` is configured to `true`, Autofac resolves `ICatalogService` to `CatalogServiceMock` as a singleton; otherwise, it resolves `CatalogService` scoped per request lifetime [^7] [^8].
3. **Data Retrieval and Pagination**:
   - `CatalogController` queries `ICatalogService.GetCatalogItemsPaginated(pageSize, pageIndex)`, which constructs pagination metadata wrapped inside `PaginatedItemsViewModel<CatalogItem>` [^26] [^27].
   - Entity Framework issues SQL queries to fetch relational entities, applying `.Skip()` and `.Take()` for windowed pagination [^19] [^20].
4. **Media and Static Asset Resolution**:
   - During list or detail rendering, `CatalogController` formats dynamic image paths (`/Pics/{id}.png` or route URL via `PicController.GetPicRouteName`) [^28] [^29].
   - `PicController` resolves the physical disk file path using local disk storage access (`Server.MapPath` or relative paths), determines the MIME type, and streams the binary content back via HTTP [^30] [^31].
5. **Cross-Component Serialization**:
   - API endpoints in `FilesController` consume `eShopLegacy.Utilities.Serializing` to serialize catalog entities into binary memory streams returned directly in the HTTP response [^4] [^16] [^32].

#### Architecture Layers

```
+--------------------------------------------------------------------------+
|                        Presentation Layer                                |
|  - Razor Views (.cshtml)                                                 |
|  - MVC Controllers: CatalogController, PicController                    |
|  - Web API Controllers: BrandsController, FilesController                |
+--------------------------------------------------------------------------+
                                    |
                                    v
+--------------------------------------------------------------------------+
|                        Business Logic Layer                              |
|  - Service Contracts: ICatalogService                                    |
|  - Implementations: CatalogService, CatalogServiceMock                   |
|  - ViewModels: PaginatedItemsViewModel<T>                                |
+--------------------------------------------------------------------------+
                                    |
                                    v
+--------------------------------------------------------------------------+
|                        Data Access Layer                                 |
|  - ORM: Entity Framework 6 (MVC) / EF Core 2.2 (Ported)                  |
|  - DbContext: CatalogDBContext                                           |
|  - Database Initializers: CatalogDBInitializer                           |
|  - Key Generation: CatalogItemHiLoGenerator                              |
+--------------------------------------------------------------------------+
                                    |
                                    v
+--------------------------------------------------------------------------+
|                        Data Layer                                        |
|  - Microsoft SQL Server (CatalogDb)                                      |
|  - Local File System (Images / Setup CSVs / Pictures ZIP)                |
+--------------------------------------------------------------------------+
```

##### Presentation Layer
The presentation layer manages HTTP request routing, view composition, model validation, and response delivery:
- **Frameworks**: In the legacy application, ASP.NET MVC 5 and ASP.NET Web API 2 are hosted concurrently [^11] [^33]. In the ported project, unified ASP.NET Core 2.2 MVC middleware handles both web pages and API routing [^8] [^34].
- **UI Stack**: Server-rendered Razor views (`.cshtml`) styled with Bootstrap 4, utilizing jQuery and unobtrusive validation (`jquery.validate`) for client-side forms [^35] [^36].
- **Controllers**:
  - `CatalogController`: Provides view endpoints for item listing, details, creation, updating, and deletion [^24] [^29].
  - `PicController`: Serves catalog item imagery from local disk storage [^30] [^31].
  - `BrandsController` & `FilesController`: Deliver programmatic endpoints returning JSON or serialized streams [^16] [^25] [^32] [^37].

##### Business Logic Layer
The business logic layer encapsulates data manipulation rules, pagination calculations, and entity lifecycle operations:
- **Contracts and Abstractions**: `ICatalogService` defines standard operations including `GetCatalogItemsPaginated`, `FindCatalogItem`, `CreateCatalogItem`, and `RemoveCatalogItem` [^6].
- **Implementations**:
  - `CatalogService`: Enforces catalog modification operations and coordinates transaction persistence via the ORM [^19] [^20].
  - `CatalogServiceMock`: Encapsulates test catalog manipulation in memory, linking mocked types and brands via static collections [^21] [^22].
- **DTOs and View Models**: `PaginatedItemsViewModel<TEntity>` encapsulates page indices, total product counts, and pagination limits for user display [^26] [^27].

##### Data Access Layer
The data access layer handles relational schema modeling, database connections, and ORM mapping:
- **Entity Framework 6 / EF Core**: Relational schema mapping is configured via fluent mappings (`OnModelCreating`) for tables `Catalog`, `CatalogBrand`, and `CatalogType` [^18] [^23] [^38] [^39] [^40].
- **Database Initialization & Seeding**: `CatalogDBInitializer` executes SQL scripts to create sequence objects for HiLo numbering and reads local CSV files (`CatalogBrands.csv`, `CatalogTypes.csv`, `CatalogItems.csv`) to seed base tables on startup [^12].
- **HiLo Sequence Generation**: `CatalogItemHiLoGenerator` executes raw SQL queries (`SELECT NEXT VALUE FOR catalog_hilo;`) and manages local ID allocations using thread locking [^9].

##### Data Layer
- **Relational Storage**: Microsoft SQL Server hosts catalog items, stock quantities, pricing, threshold values, and relational foreign keys between brands and types [^10] [^41].
- **File System Storage**: Binary picture assets (`.png` files) are stored in the local file directory (`/Pics`) and unpacked on deployment from a compressed ZIP archive (`CatalogItems.zip`) [^12] [^30].

#### Technology and Design Patterns

| Pattern / Architectural Style | Implementation in Codebase | Rationale and Context |
| :--- | :--- | :--- |
| **Monolithic Architecture** | Single web application solution uniting UI, API, domain, and data access. | Suitable for low-complexity catalog operations; keeps deployment boundaries unified. |
| **Model-View-Controller (MVC)** | Separation into Razor views, `CatalogItem` models, and `CatalogController`. | Enforces separation between HTTP presentation concerns and business domain models. |
| **Dependency Injection** | Autofac integration via `ApplicationModule`. | Inverts control, allowing dynamic swapping of concrete services (`CatalogServiceMock` vs `CatalogService`) based on configuration settings [^7] [^8]. |
| **Service Layer Pattern** | `ICatalogService` wrapping entity queries and commands. | Shields controllers from direct database interactions and raw Entity Framework contexts [^6] [^24]. |
| **HiLo Key Generation Pattern** | `CatalogItemHiLoGenerator` utilizing SQL sequences and in-memory key pooling [^9]. | Preallocates blocks of unique identifiers, reducing database roundtrips during record generation. |

#### Scalability and Resilience

##### Scalability Characteristics
- **Presentation Scalability**: The presentation logic is largely stateless, allowing basic horizontal scaling behind an HTTP load balancer. However, `eShopLegacyMVC` relies on in-process session state (`Session_Start` storing `MachineName` and `SessionStartTime` in memory), which prevents stateless multi-instance scaling unless sticky sessions or external session providers are configured [^10] [^11].
- **Database Scalability**: The system executes synchronous queries directly against a single SQL Server instance. Pagination (`.Skip().Take()`) mitigates memory pressure on large catalog tables [^19] [^20], but lack of asynchronous database queries (`async/await`) restricts thread pool throughput under heavy request volume.
- **Local Asset Bottlenecks**: Serving images directly from local container/server disk storage creates storage coupling and limits rapid instance scaling [^30].

##### Resilience Features
- **Database Seeding and Recovery**: In development and initial database setup, `CatalogDBInitializer` automates schema generation and initial data hydration [^12].
- **Mock Fallback**: If a database connection is unavailable during offline testing or local prototyping, the application can switch entirely to `CatalogServiceMock` using application settings [^7] [^8].
- **Telemetry and Monitoring**: Integrated Application Insights configurations (`ApplicationInsights.config`) collect dependency tracking, performance counters, and unhandled exception traces [^14].

#### Key Challenges or Limitations

1. **Legacy .NET Framework and Windows Lock-in**:
   - `eShopLegacyMVC` relies on .NET Framework 4.7.2 and legacy `System.Web` components (`HttpContext.Current`, `HostingEnvironment`, `HttpApplication`), preventing execution on modern Linux operating systems [^2] [^11] [^12].
   - `eShopPorted` is pinned to .NET Framework 4.6.1 (`net461`), despite using ASP.NET Core 2.2, which restricts migration to lightweight Linux container runtimes until fully upgraded to modern .NET [^3].
2. **Insecure and Obsolete Binary Serialization**:
   - `eShopLegacy.Utilities.Serializing` uses `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter` [^4]. `BinaryFormatter` is obsolete, dangerous, and entirely disabled in modern .NET (.NET 8+) due to severe remote code execution vulnerabilities.
3. **Local File System Coupling**:
   - Image management in `PicController` and database initialization in `CatalogDBInitializer` rely on local disk access (`Server.MapPath`, `ApplicationPhysicalPath`, extracting `CatalogItems.zip` to disk) [^12] [^30]. In a serverless container environment like Google Cloud Run, local filesystems are ephemeral.
4. **Synchronous Execution Model**:
   - All controller and repository actions execute synchronously without `async`/`await` patterns, limiting thread pool scalability under high concurrent load [^19] [^24].
5. **Partial and Fragmented Porting State**:
   - The repository hosts two parallel implementations (`eShopLegacyMVC` and `eShopPorted`), leading to duplicated domain models, configurations, and inconsistent controller implementations that require unification into a single modern .NET 10 service.

---

[^1]: `eShopLegacyMVC.sln`: Project definitions - Declares eShopLegacyMVC, eShopPorted, and eShopLegacy.Utilities solution modules.
[^2]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: PropertyGroup - Specifies TargetFrameworkVersion v4.7.2 for legacy MVC.
[^3]: `eShopPorted/eShopPorted.csproj`: PropertyGroup - Specifies TargetFramework net461 and ASP.NET Core 2.2 dependencies.
[^4]: `eShopLegacy.Utilities/Serializing.cs`: Serializing - Handles binary serialization using BinaryFormatter.
[^5]: `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj`: PropertyGroup - Defines TargetFrameworkVersion v4.6.1 for utilities project.
[^6]: `src/eShopLegacyMVC/Services/ICatalogService.cs`: ICatalogService - Declares service interface contract for catalog operations.
[^7]: `src/eShopLegacyMVC/Modules/ApplicationModule.cs`: ApplicationModule.Load() - Configures Autofac DI registrations for MVC based on UseMockData setting.
[^8]: `eShopPorted/Startup.cs`: Startup.ConfigureServices() - Configures Autofac DI registrations and ASP.NET Core services.
[^9]: `src/eShopLegacyMVC/Models/CatalogItemHiLoGenerator.cs`: CatalogItemHiLoGenerator.GetNextSequenceValue() - Manages HiLo sequence identifier allocation.
[^10]: `src/eShopLegacyMVC/Web.config`: system.web and system.webServer - Defines InProc sessionState, runtime targets, and IIS HTTP modules.
[^11]: `src/eShopLegacyMVC/Global.asax.cs`: MvcApplication.Application_Start() - Bootstraps DI, Web API, routes, and InProc session tracking.
[^12]: `src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs`: CatalogDBInitializer.Seed() - Seeds database tables from CSV files and unpacks images using hosting path.
[^13]: `src/eShopLegacyMVC/Controllers/PicController.cs`: PicController.Index() - Resolves local image paths via Server.MapPath.
[^14]: `src/eShopLegacyMVC/ApplicationInsights.config`: ApplicationInsights - Configures Windows-specific performance counters and telemetry modules.
[^15]: `src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs`: BrandsController - Web API controller serving catalog brand endpoints.
[^16]: `src/eShopLegacyMVC/Controllers/WebApi/FilesController.cs`: FilesController.Get() - Streams binary-serialized BrandDTO payloads via Web API.
[^17]: `eShopPorted/Controllers/CatalogController.cs`: CatalogController - ASP.NET Core MVC controller for catalog item management.
[^18]: `eShopPorted/Models/CatalogDBContext.cs`: CatalogDBContext - Entity Framework Core DbContext mapping catalog models.
[^19]: `src/eShopLegacyMVC/Services/CatalogService.cs`: CatalogService - Implements relational catalog persistence using Entity Framework 6.
[^20]: `eShopPorted/Services/CatalogService.cs`: CatalogService - Implements relational catalog persistence using Entity Framework Core.
[^21]: `src/eShopLegacyMVC/Services/CatalogServiceMock.cs`: CatalogServiceMock - In-memory implementation of ICatalogService using static seed data.
[^22]: `eShopPorted/Services/CatalogServiceMock.cs`: CatalogServiceMock - In-memory implementation of ICatalogService for the ported project.
[^23]: `src/eShopLegacyMVC/Models/CatalogDBContext.cs`: CatalogDBContext - Entity Framework 6 DbContext for catalog domain.
[^24]: `src/eShopLegacyMVC/Controllers/CatalogController.cs`: CatalogController.Index() - Loads paginated catalog items into views.
[^25]: `eShopPorted/Controllers/Api/BrandsController.cs`: BrandsController - ASP.NET Core API controller exposing brand resources.
[^26]: `src/eShopLegacyMVC/ViewModel/PaginatedItemsViewModel.cs`: PaginatedItemsViewModel - View model encapsulating pagination metadata.
[^27]: `eShopPorted/ViewModel/PaginatedItemsViewModel.cs`: PaginatedItemsViewModel - View model encapsulating pagination metadata in ported project.
[^28]: `eShopPorted/Controllers/CatalogController.cs`: CatalogController.AddUriPlaceHolder() - Appends local image route URI to catalog items.
[^29]: `src/eShopLegacyMVC/Controllers/CatalogController.cs`: CatalogController.AddUriPlaceHolder() - Appends dynamic route URI to catalog items.
[^30]: `src/eShopLegacyMVC/Controllers/PicController.cs`: PicController - Reads image files from local disk and delivers binary image streams.
[^31]: `eShopPorted/Controllers/PicController.cs`: PicController - Reads image files from disk path and delivers binary content.
[^32]: `eShopPorted/Controllers/Api/FilesController.cs`: FilesController.Index() - Serializes BrandDTO via BinaryFormatter utility.
[^33]: `src/eShopLegacyMVC/App_Start/WebApiConfig.cs`: WebApiConfig.Register() - Registers default HTTP routes for Web API.
[^34]: `eShopPorted/Startup.cs`: Startup.Configure() - Configures ASP.NET Core HTTP request pipeline and MVC routes.
[^35]: `src/eShopLegacyMVC/Views/Catalog/Index.cshtml`: Index view - Renders catalog management table and pagination navigation.
[^36]: `eShopPorted/Views/Catalog/Index.cshtml`: Index view - Renders ASP.NET Core catalog table and pagination.
[^37]: `src/eShopLegacyMVC/Controllers/Api/CatalogController.cs`: CatalogController2 - Simple API route returning JSON response.
[^38]: `eShopPorted/Models/Config/CatalogItemConfig.cs`: CatalogItemConfig.Configure() - Configures EF Core entity metadata and relationships for CatalogItem.
[^39]: `eShopPorted/Models/Config/CatalogBrandConfig.cs`: CatalogBrandConfig.Configure() - Configures EF Core entity metadata for CatalogBrand.
[^40]: `eShopPorted/Models/Config/CatalogTypeConfig.cs`: CatalogTypeConfig.Configure() - Configures EF Core entity metadata for CatalogType.
[^41]: `eShopPorted/Migrations/20201130044339_Initial.cs`: Initial.Up() - Creates database schema and default tables for catalog entities.

### Technology Stack
#### Technology Stack Overview

The solution currently demonstrates an application in transition, consisting of three main projects:
1. `eShopLegacyMVC`: The original legacy ASP.NET MVC 5 application targeting `.NET Framework 4.7.2` hosted via IIS [^1].
2. `eShopPorted`: An ASP.NET Core 2.2 application configured in an SDK-style project, currently targeting `.NET Framework 4.6.1` (`net461`) [^2].
3. `eShopLegacy.Utilities`: A shared class library targeting `.NET Framework 4.6.1` providing serialization helpers [^3].

As part of the roadmap toward modernizing this solution to run as a single containerized service on Google Cloud Run using `.NET 10` on Linux, the technology stack across both projects is analyzed below.

---

#### Runtimes and Frameworks

| Technology / Tool | Version | Purpose / Role | Integration Details |
| :--- | :--- | :--- | :--- |
| **.NET Framework** | `4.7.2` (Legacy)<br>`4.6.1` (Ported/Util) | Base CLR and runtime execution environment [^1] [^2] [^3]. | Tied to Windows APIs, IIS HTTP module pipeline, and Windows Registry. Migration to **.NET 10** is required for Linux and Google Cloud Run. |
| **ASP.NET MVC** | `5.2.7` | Legacy server-side presentation framework based on `System.Web` [^4]. | Coupled to `System.Web.Mvc` pipeline, `Global.asax`, and IIS execution engine [^5]. |
| **ASP.NET Web API** | `5.2.7` | Legacy HTTP service framework running side-by-side with MVC [^6]. | Integrated using `WebApiConfig.Register` and Web API message handlers [^5]. |
| **ASP.NET Core** | `2.2.0` | Modern modular web framework implemented in `eShopPorted` [^2]. | Configured via `Startup.cs` and `Program.cs`, using `Microsoft.AspNetCore.Mvc` [^7] [^8]. |

---

#### Data Access and Storage

| Technology / Tool | Version | Purpose / Role | Integration Details |
| :--- | :--- | :--- | :--- |
| **Entity Framework** | `6.2.0` | Object-Relational Mapper (ORM) in `eShopLegacyMVC` [^9]. | Direct database communication using SQL Server LocalDB and custom Hi/Lo sequence execution scripts [^10] [^11]. |
| **Entity Framework Core** | `2.2.6` | Modern ORM implemented in the `eShopPorted` project [^2]. | Configured via `CatalogDBContext` using `Microsoft.EntityFrameworkCore.SqlServer` [^12]. |
| **Microsoft SQL Server** | SQL Server / LocalDB | Relational Database Management System [^13] [^14]. | Configured via connection strings (`DefaultConnection` and `CatalogDBContext`) [^13] [^14]. Compatible with Google Cloud SQL for SQL Server. |

---

#### Dependency Injection and Inversion of Control

| Technology / Tool | Version | Purpose / Role | Integration Details |
| :--- | :--- | :--- | :--- |
| **Autofac** | `6.1.0` (Legacy)<br>`4.9.1` (Ported) | Inversion of Control (IoC) and dependency injection container [^1] [^2]. | Registers controllers, services (`ICatalogService`), and data contexts. |
| **Autofac.Mvc5** | `4.0.2` | Adapter integrating Autofac with ASP.NET MVC 5 dependency resolvers [^1]. | Hooked into `DependencyResolver.SetResolver` in `Global.asax.cs` [^5]. |
| **Autofac.WebApi2** | `6.0.1` | Adapter integrating Autofac with ASP.NET Web API 2 [^1]. | Attached to `GlobalConfiguration.Configuration.DependencyResolver` [^5]. |
| **Autofac.Extensions.DependencyInjection** | `4.4.0` | Bridges Autofac with Microsoft's `IServiceCollection` in `eShopPorted` [^2]. | Integrated into `Startup.ConfigureServices` to support `AutofacServiceProvider` [^7]. |

---

#### Monitoring, Logging, and Serialization

| Technology / Tool | Version | Purpose / Role | Integration Details |
| :--- | :--- | :--- | :--- |
| **log4net** | `2.0.10` | Enterprise diagnostic and application logging [^1] [^2]. | Configured via `log4Net.xml` with rolling file appenders (`logFiles\myapp.log`) and thread context properties (`activityid`, `requestinfo`) [^15] [^16]. |
| **Microsoft Application Insights** | `2.9.1` | Application performance monitoring (APM) and telemetry [^1]. | Initialized via `ApplicationInsights.config` and IIS HTTP modules (`TelemetryCorrelationHttpModule`, `ApplicationInsightsHttpModule`) [^17] [^18]. |
| **Newtonsoft.Json** | `12.0.1` (Legacy)<br>`13.0.2` (Ported) | High-performance JSON serialization [^1] [^2]. | Used for Web API responses and configuration modeling [^19]. |
| **BinaryFormatter** | Built-in .NET | Binary serialization utility in `eShopLegacy.Utilities` [^20]. | Executed by `FilesController` to export binary data streams [^21] [^22]. Note: Obsolete and unsupported in modern .NET due to security vulnerabilities. |

---

#### Client-Side and UI Libraries

| Technology / Tool | Version | Purpose / Role | Integration Details |
| :--- | :--- | :--- | :--- |
| **Bootstrap** | `4.3.1` | Front-end CSS grid framework for responsive layout styling [^1] [^23]. | Referenced globally in Razor layouts `_Layout.cshtml` and CSS assets [^23] [^24]. |
| **jQuery** | `3.5.0` (Legacy)<br>`3.3.1` (Ported) | Client-side DOM manipulation and AJAX calls [^1] [^23]. | Bundled in legacy MVC via `BundleConfig.cs` and linked in `_Layout.cshtml` [^23] [^25]. |
| **jQuery Validation / Unobtrusive** | `1.19.4` / `3.2.11` | Client-side form input validation against ASP.NET data annotations [^1] [^26]. | Rendered in `Create.cshtml` and `Edit.cshtml` views [^26] [^27]. |
| **Popper.js** | `1.14.3` | Element positioning engine for drop-downs and tooltips [^1]. | Bundled with Bootstrap components in the presentation scripts [^23]. |
| **Microsoft.AspNet.Web.Optimization** | `1.1.3` | Server-side JavaScript and CSS bundling/minification in legacy MVC [^1]. | Invoked at startup via `BundleConfig.RegisterBundles` [^5] [^25]. |

---

#### Modernization Assessment for Google Cloud Run (.NET 10 on Linux)

To meet the target platform of running as a single service on **Google Cloud Run** running **.NET 10** on **Linux**, key shifts across the technology stack are identified:

* **Target Framework Monolith Unification**: Consolidate `eShopLegacyMVC` and `eShopPorted` into a unified ASP.NET Core project targeting `net10.0`.
* **Removal of System.Web & Windows Dependencies**: Eliminate `Global.asax`, `Web.config`, `System.Web.Optimization`, and IIS-specific HTTP modules [^5] [^18].
* **ORM Migration**: Standardize entirely on Entity Framework Core, upgrading to version 10 to support native SQL Server connectivity (such as connecting to Google Cloud SQL for SQL Server) [^7].
* **Insecure Serialization Replacement**: Deprecate `BinaryFormatter` in `eShopLegacy.Utilities.Serializing` and transition to `System.Text.Json` to satisfy modern .NET security standards and cross-platform compatibility [^20].
* **Logging and Observability**: Replace rolling file appenders and IIS Application Insights modules with console-based structured logging (compatible with Google Cloud Logging) [^16] [^18].

---

[^1]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` and package references - Legacy ASP.NET MVC project configuration and packages.
[^2]: `eShopPorted/eShopPorted.csproj`: `<TargetFramework>net461</TargetFramework>` and package references - Ported ASP.NET Core 2.2 project configuration.
[^3]: `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj`: `<TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>` - Class library project settings and references.
[^4]: `src/eShopLegacyMVC/packages.config`: `<package id="Microsoft.AspNet.Mvc" version="5.2.7"` - Defines the legacy MVC framework version.
[^5]: `src/eShopLegacyMVC/Global.asax.cs`: `Application_Start()` - Configures legacy routes, bundles, and Autofac container resolvers.
[^6]: `src/eShopLegacyMVC/App_Start/WebApiConfig.cs`: `Register(HttpConfiguration config)` - Defines routing for ASP.NET Web API endpoints.
[^7]: `eShopPorted/Startup.cs`: `ConfigureServices(...)` - Registers ASP.NET Core services, EF Core DbContext, and Autofac container.
[^8]: `eShopPorted/Program.cs`: `CreateWebHostBuilder(string[] args)` - Builds and runs the ASP.NET Core web host.
[^9]: `src/eShopLegacyMVC/packages.config`: `<package id="EntityFramework" version="6.2.0"` - Declares Entity Framework 6 dependency.
[^10]: `src/eShopLegacyMVC/Models/CatalogDBContext.cs`: `CatalogDBContext` - Entity Framework 6 database context definition.
[^11]: `src/eShopLegacyMVC/Models/CatalogItemHiLoGenerator.cs`: `GetNextSequenceValue(CatalogDBContext db)` - Executes raw SQL queries against SQL Server sequence objects.
[^12]: `eShopPorted/Models/CatalogDBContext.cs`: `CatalogDBContext` - Entity Framework Core DbContext implementation.
[^13]: `src/eShopLegacyMVC/Web.config`: `<connectionStrings>` - Database connection configuration for localdb MSSQL.
[^14]: `eShopPorted/appsettings.json`: `"DefaultConnection"` - Connection string setting for SQL Server.
[^15]: `src/eShopLegacyMVC/log4Net.xml`: `<appender name="file"` - Rolling file configuration for log4net.
[^16]: `src/eShopLegacyMVC/Global.asax.cs`: `Application_BeginRequest(...)` - Assigns activity and request info context to log4net threads.
[^17]: `src/eShopLegacyMVC/ApplicationInsights.config`: `<ApplicationInsights>` - Configuration settings for Application Insights telemetry.
[^18]: `src/eShopLegacyMVC/Web.config`: `<system.webServer><modules>` - Registers IIS-specific telemetry and session state modules.
[^19]: `src/eShopLegacyMVC/Controllers/Api/CatalogController.cs`: `Index()` - Returns JSON-formatted data via controller action.
[^20]: `eShopLegacy.Utilities/Serializing.cs`: `SerializeBinary(object input)` - Uses BinaryFormatter to serialize objects into a MemoryStream.
[^21]: `src/eShopLegacyMVC/Controllers/WebApi/FilesController.cs`: `Get()` - Exports brand catalog data through BinaryFormatter stream.
[^22]: `eShopPorted/Controllers/Api/FilesController.cs`: `Index()` - Uses legacy Serializing binary stream in ASP.NET Core controller.
[^23]: `eShopPorted/Views/Shared/_Layout.cshtml`: Bootstrap, Popper, and jQuery script and CSS imports in the HTML layout.
[^24]: `src/eShopLegacyMVC/Views/Shared/_Layout.cshtml`: References bundled Bootstrap and modernizr components.
[^25]: `src/eShopLegacyMVC/App_Start/BundleConfig.cs`: `RegisterBundles(BundleCollection bundles)` - Configures script and style bundles using WebGrease.
[^26]: `eShopPorted/Views/Catalog/Create.cshtml`: `@section Scripts` - Loads jQuery validation scripts via CDN.
[^27]: `src/eShopLegacyMVC/Views/Catalog/Edit.cshtml`: `@section Scripts` - Renders bundled client-side validation scripts.

### APIs and Endpoints
#### Surface Analysis of APIs and Endpoints

The solution contains two web entry points representing the evolution of the catalog management service: `eShopLegacyMVC` (legacy ASP.NET MVC 5 and Web API 2 running on .NET Framework 4.7.2) [^1] and `eShopPorted` (ASP.NET Core 2.2 running on .NET Framework 4.6.1) [^2]. Both implementations share the same underlying domain model, business services, and functional endpoint contracts. 

In preparation for modernizing the system into a unified, single-service deployment running on .NET 10 and Linux on Google Cloud Run, the endpoints are classified into three architectural categories:
1. **HTML MVC Controllers**: Render Razor views for catalog browsing, item creation, editing, and deletion.
2. **RESTful Web API Controllers**: Provide programmatic endpoints for brand administration and system messaging.
3. **Binary and Media Streaming Endpoints**: Stream catalog product imagery and legacy serialized binary payloads.

##### Endpoint Inventory and Behavioral Summary

| Endpoint Path | HTTP Method | Controller Class | Purpose & Parameters | Request Format | Response Content-Type & Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/` or `/Catalog` or `/Catalog/Index` | `GET` | `CatalogController` | Paginated catalog view. Query params: `pageSize` (int, default: 10), `pageIndex` (int, default: 0) [^3] [^4]. | None | `text/html` (200 OK) |
| `/Catalog/Details` or `/Catalog/Details/{id}` | `GET` | `CatalogController` | Displays catalog item details. Query or route param: `id` (int, required) [^5] [^6]. | None | `text/html` (200 OK, 400 Bad Request, 404 Not Found) |
| `/Catalog/Create` | `GET` | `CatalogController` | Renders catalog item creation form [^7] [^8]. | None | `text/html` (200 OK) |
| `/Catalog/Create` | `POST` | `CatalogController` | Validates and creates a catalog item [^9] [^10]. | `application/x-www-form-urlencoded` (`CatalogItem` fields + AntiForgeryToken) | `text/html` (302 Redirect to `/Catalog/Index` on success; 200 OK on validation error) |
| `/Catalog/Edit` or `/Catalog/Edit/{id}` | `GET` | `CatalogController` | Renders catalog item edit form. Query or route param: `id` (int, required) [^11] [^12]. | None | `text/html` (200 OK, 400 Bad Request, 404 Not Found) |
| `/Catalog/Edit` or `/Catalog/Edit/{id}` | `POST` | `CatalogController` | Updates catalog item details. Query or route param: `id` (int, required) [^13] [^14]. | `application/x-www-form-urlencoded` (`CatalogItem` fields + AntiForgeryToken) | `text/html` (302 Redirect to `/Catalog/Index` on success; 200 OK on validation error) |
| `/Catalog/Delete` or `/Catalog/Delete/{id}` | `GET` | `CatalogController` | Renders deletion confirmation view. Query or route param: `id` (int, required) [^15] [^16]. | None | `text/html` (200 OK, 400 Bad Request, 404 Not Found) |
| `/Catalog/Delete` or `/Catalog/Delete/{id}` | `POST` | `CatalogController` | Deletes specified catalog item [^17] [^18]. | `application/x-www-form-urlencoded` (`id` + AntiForgeryToken) | `text/html` (302 Redirect to `/Catalog/Index` on success) |
| `/items/{catalogItemId}/pic` | `GET` | `PicController` | Retrieves item image by catalog item identifier [^19] [^20]. | None | `image/png`, `image/jpeg`, `image/gif`, etc. (200 OK, 400 Bad Request, 404 Not Found) |
| `/api` | `GET` | `CatalogController2` | Smoke-test endpoint returning greeting message [^21]. | None | `application/json` (200 OK) |
| `/api/Brands` | `GET` | `BrandsController` | Retrieves complete list of catalog brands [^22] [^23]. | None | `application/json` (200 OK) |
| `/api/Brands/{id}` | `GET` | `BrandsController` | Retrieves single catalog brand by identifier [^24]. Route param: `id` (int, required). | None | `application/json` (200 OK, 404 Not Found) |
| `/api/Brands/{id}` | `DELETE` | `BrandsController` | Mock deletion endpoint for catalog brand [^25] [^26]. Route param: `id` (int, required). | None | 200 OK, 404 Not Found |
| `/api/Files` | `GET` | `FilesController` | Exports brands as serialized binary stream [^27] [^28]. | None | `application/octet-stream` (200 OK) |

##### Modernization Considerations for Cloud Run

- **Binary Serialization Deprecation**: The `GET /api/Files` endpoint relies on .NET `BinaryFormatter` via `Serializing.cs` [^29]. `BinaryFormatter` is obsolete, dangerous, and completely disabled by default in .NET 10. During the migration to .NET 10 on Linux, this endpoint must be refactored to stream standard JSON via `System.Text.Json` or protocol buffers.
- **Local File System Access**: `PicController` resolves picture files using relative paths and `Server.MapPath("~/Pics")` [^19] [^20]. In a containerized Cloud Run environment, local disk storage is ephemeral and read-only outside `/tmp`. Media should be served directly from Google Cloud Storage or mounted volumes.
- **Anti-Forgery Cookie Encryption on Linux**: The MVC POST endpoints (`/Catalog/Create`, `/Catalog/Edit`, `/Catalog/Delete`) enforce `[ValidateAntiForgeryToken]` [^9] [^13] [^17]. In modern ASP.NET Core on Linux containers, ASP.NET Core Data Protection keys must be configured to persist outside the container (e.g., using Google Cloud Storage and Google Cloud Key Management Service) so that multiple Cloud Run instances can verify anti-forgery tokens consistently.

---

#### OpenAPI 3.0 Specification

```yaml
openapi: 3.0.3
info:
  title: eShop Catalog API and Management Interface
  description: Unified OpenAPI specification covering the catalog MVC UI and RESTful Web API endpoints in eShopLegacy.
  version: 1.0.0
servers:
  - url: /
    description: Current Host Server
paths:
  /Catalog:
    get:
      summary: Get paginated catalog items
      operationId: getCatalogIndex
      tags:
        - Catalog UI
      parameters:
        - name: pageSize
          in: query
          required: false
          description: Number of items per page.
          schema:
            type: integer
            default: 10
        - name: pageIndex
          in: query
          required: false
          description: Zero-based page index.
          schema:
            type: integer
            default: 0
      responses:
        '200':
          description: HTML page containing paginated catalog items table.
          content:
            text/html:
              schema:
                type: string

  /Catalog/Details:
    get:
      summary: View catalog item details
      operationId: getCatalogDetails
      tags:
        - Catalog UI
      parameters:
        - name: id
          in: query
          required: true
          description: Unique identifier of the catalog item.
          schema:
            type: integer
      responses:
        '200':
          description: HTML page displaying catalog item details.
          content:
            text/html:
              schema:
                type: string
        '400':
          description: Bad request if identifier is missing.
        '404':
          description: Catalog item not found.

  /Catalog/Create:
    get:
      summary: View catalog item creation form
      operationId: getCatalogCreateForm
      tags:
        - Catalog UI
      responses:
        '200':
          description: HTML form for creating a new catalog item.
          content:
            text/html:
              schema:
                type: string
    post:
      summary: Submit new catalog item
      operationId: postCatalogCreate
      tags:
        - Catalog UI
      requestBody:
        required: true
        content:
          application/x-www-form-urlencoded:
            schema:
              $ref: '#/components/schemas/CatalogItemFormData'
      responses:
        '302':
          description: Redirects to /Catalog on successful item creation.
          headers:
            Location:
              schema:
                type: string
                example: /Catalog
        '200':
          description: Re-renders creation form with validation errors if inputs are invalid.
          content:
            text/html:
              schema:
                type: string

  /Catalog/Edit:
    get:
      summary: View catalog item edit form
      operationId: getCatalogEditForm
      tags:
        - Catalog UI
      parameters:
        - name: id
          in: query
          required: true
          description: Unique identifier of the catalog item to edit.
          schema:
            type: integer
      responses:
        '200':
          description: HTML form for editing an existing catalog item.
          content:
            text/html:
              schema:
                type: string
        '400':
          description: Bad request if identifier is missing.
        '404':
          description: Catalog item not found.
    post:
      summary: Update existing catalog item
      operationId: postCatalogEdit
      tags:
        - Catalog UI
      parameters:
        - name: id
          in: query
          required: false
          description: Optional identifier in query when posting edits.
          schema:
            type: integer
      requestBody:
        required: true
        content:
          application/x-www-form-urlencoded:
            schema:
              $ref: '#/components/schemas/CatalogItemFormData'
      responses:
        '302':
          description: Redirects to /Catalog on successful update.
          headers:
            Location:
              schema:
                type: string
                example: /Catalog
        '200':
          description: Re-renders edit form with validation errors if inputs are invalid.
          content:
            text/html:
              schema:
                type: string

  /Catalog/Delete:
    get:
      summary: View catalog item delete confirmation form
      operationId: getCatalogDeleteConfirmation
      tags:
        - Catalog UI
      parameters:
        - name: id
          in: query
          required: true
          description: Unique identifier of the catalog item to delete.
          schema:
            type: integer
      responses:
        '200':
          description: HTML page prompting for deletion confirmation.
          content:
            text/html:
              schema:
                type: string
        '400':
          description: Bad request if identifier is missing.
        '404':
          description: Catalog item not found.
    post:
      summary: Confirm catalog item deletion
      operationId: postCatalogDeleteConfirmed
      tags:
        - Catalog UI
      requestBody:
        required: true
        content:
          application/x-www-form-urlencoded:
            schema:
              type: object
              required:
                - id
              properties:
                id:
                  type: integer
                  description: Identifier of the catalog item to remove.
                __RequestVerificationToken:
                  type: string
                  description: Anti-forgery verification token.
      responses:
        '302':
          description: Redirects to /Catalog after deletion.
          headers:
            Location:
              schema:
                type: string
                example: /Catalog

  /items/{catalogItemId}/pic:
    get:
      summary: Retrieve item picture file
      operationId: getItemPicture
      tags:
        - Catalog Media
      parameters:
        - name: catalogItemId
          in: path
          required: true
          description: Identifier of the catalog item.
          schema:
            type: integer
            minimum: 1
      responses:
        '200':
          description: Raw image binary stream.
          content:
            image/png:
              schema:
                type: string
                format: binary
            image/jpeg:
              schema:
                type: string
                format: binary
            image/gif:
              schema:
                type: string
                format: binary
            image/svg+xml:
              schema:
                type: string
                format: binary
            application/octet-stream:
              schema:
                type: string
                format: binary
        '400':
          description: Invalid catalog item identifier supplied (less than or equal to zero).
        '404':
          description: Image or catalog item not found.

  /api:
    get:
      summary: API health and discovery greeting
      operationId: getApiRoot
      tags:
        - System API
      responses:
        '200':
          description: Status message object.
          content:
            application/json:
              schema:
                type: object
                properties:
                  Message:
                    type: string
                    example: Hello World!

  /api/Brands:
    get:
      summary: List all catalog brands
      operationId: getCatalogBrands
      tags:
        - Brands API
      responses:
        '200':
          description: Array of catalog brands.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/CatalogBrand'

  /api/Brands/{id}:
    get:
      summary: Get brand by identifier
      operationId: getCatalogBrandById
      tags:
        - Brands API
      parameters:
        - name: id
          in: path
          required: true
          description: Unique brand identifier.
          schema:
            type: integer
      responses:
        '200':
          description: Matching catalog brand object.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CatalogBrand'
        '404':
          description: Brand with specified identifier not found.
    delete:
      summary: Delete brand (Mock)
      operationId: deleteCatalogBrand
      tags:
        - Brands API
      parameters:
        - name: id
          in: path
          required: true
          description: Unique brand identifier to delete.
          schema:
            type: integer
      responses:
        '200':
          description: Brand successfully deleted (demonstration stub).
        '404':
          description: Brand not found.

  /api/Files:
    get:
      summary: Export brands as binary serialized stream
      description: Returns brand DTO items serialized using .NET BinaryFormatter. Scheduled for replacement with JSON in .NET 10.
      operationId: getFilesSerialized
      tags:
        - Files API
      responses:
        '200':
          description: Serialized binary stream of brand DTOs.
          content:
            application/octet-stream:
              schema:
                type: string
                format: binary

components:
  schemas:
    CatalogBrand:
      type: object
      properties:
        id:
          type: integer
          example: 2
        brand:
          type: string
          example: .NET

    CatalogItemFormData:
      type: object
      required:
        - Name
        - Price
        - CatalogTypeId
        - CatalogBrandId
      properties:
        Id:
          type: integer
          description: Unique item identifier (0 for new items).
        Name:
          type: string
          maxLength: 50
          example: .NET Bot Black Hoodie
        Description:
          type: string
          nullable: true
          example: High quality hoodie with .NET bot logo
        Price:
          type: number
          format: decimal
          minimum: 0
          maximum: 1000000
          example: 19.50
        PictureFileName:
          type: string
          example: 1.png
        CatalogTypeId:
          type: integer
          example: 2
        CatalogBrandId:
          type: integer
          example: 2
        AvailableStock:
          type: integer
          minimum: 0
          maximum: 10000000
          example: 100
        RestockThreshold:
          type: integer
          minimum: 0
          maximum: 10000000
          example: 10
        MaxStockThreshold:
          type: integer
          minimum: 0
          maximum: 10000000
          example: 200
        OnReorder:
          type: boolean
          example: false
        __RequestVerificationToken:
          type: string
          description: Anti-forgery verification token.
```

[^1]: src/eShopLegacyMVC/eShopLegacyMVC.csproj: Project configuration - Target framework configured as net472 with ASP.NET MVC and Web API dependencies.
[^2]: eShopPorted/eShopPorted.csproj: Project configuration - Target framework net461 configured with Microsoft.AspNetCore 2.2 packages.
[^3]: src/eShopLegacyMVC/Controllers/CatalogController.cs: Index() - Loads paginated catalog items with pageSize and pageIndex parameters.
[^4]: eShopPorted/Controllers/CatalogController.cs: Index() - ASP.NET Core implementation of paginated catalog item list action.
[^5]: src/eShopLegacyMVC/Controllers/CatalogController.cs: Details() - Returns catalog item details view or BadRequest/HttpNotFound.
[^6]: eShopPorted/Controllers/CatalogController.cs: Details() - Returns catalog item details view or BadRequest/NotFound in ASP.NET Core.
[^7]: src/eShopLegacyMVC/Controllers/CatalogController.cs: Create() - GET action rendering item creation form with brand and type SelectLists.
[^8]: eShopPorted/Controllers/CatalogController.cs: Create() - ASP.NET Core GET action preparing SelectLists and empty CatalogItem view.
[^9]: src/eShopLegacyMVC/Controllers/CatalogController.cs: Create() - POST action processing model binding with ValidateAntiForgeryToken and saving new item.
[^10]: eShopPorted/Controllers/CatalogController.cs: Create() - ASP.NET Core POST action persisting bound CatalogItem entity.
[^11]: src/eShopLegacyMVC/Controllers/CatalogController.cs: Edit() - GET action retrieving catalog item and rendering edit form.
[^12]: eShopPorted/Controllers/CatalogController.cs: Edit() - ASP.NET Core GET action looking up catalog item for modification.
[^13]: src/eShopLegacyMVC/Controllers/CatalogController.cs: Edit() - POST action validating model state and updating catalog item entity.
[^14]: eShopPorted/Controllers/CatalogController.cs: Edit() - ASP.NET Core POST action executing item updates via ICatalogService.
[^15]: src/eShopLegacyMVC/Controllers/CatalogController.cs: Delete() - GET action rendering catalog item deletion confirmation page.
[^16]: eShopPorted/Controllers/CatalogController.cs: Delete() - ASP.NET Core GET action displaying item to be deleted.
[^17]: src/eShopLegacyMVC/Controllers/CatalogController.cs: DeleteConfirmed() - POST action deleting verified catalog item by ID.
[^18]: eShopPorted/Controllers/CatalogController.cs: DeleteConfirmed() - ASP.NET Core POST action removing item from repository.
[^19]: src/eShopLegacyMVC/Controllers/PicController.cs: Index() - Resolves file path using Server.MapPath and streams image binary with MIME type.
[^20]: eShopPorted/Controllers/PicController.cs: Index() - ASP.NET Core action returning image file byte buffer and content type.
[^21]: src/eShopLegacyMVC/Controllers/Api/CatalogController.cs: Index() - Attribute-routed GET endpoint returning JSON greeting.
[^22]: src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs: Get() - Web API 2 endpoint returning collection of CatalogBrand entities.
[^23]: eShopPorted/Controllers/Api/BrandsController.cs: Index() - ASP.NET Core API controller returning all catalog brands.
[^24]: src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs: Get(int id) - Web API 2 endpoint returning brand by ID or NotFound.
[^25]: src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs: Delete(int id) - Web API 2 endpoint demonstrating brand deletion.
[^26]: eShopPorted/Controllers/Api/BrandsController.cs: Delete(int id) - ASP.NET Core API controller demo deletion stub.
[^27]: src/eShopLegacyMVC/Controllers/WebApi/FilesController.cs: Get() - Serializes BrandDTO objects to binary stream using BinaryFormatter.
[^28]: eShopPorted/Controllers/Api/FilesController.cs: Index() - ASP.NET Core action returning BinaryFormatter serialized BrandDTO stream.
[^29]: eShopLegacy.Utilities/Serializing.cs: SerializeBinary() - Serializes input objects into a MemoryStream using BinaryFormatter.

## Assessment Findings
### Security and Compliance
##### Security Assessment

This assessment reviews the existing monolithic application across the legacy ASP.NET MVC codebase (`eShopLegacyMVC`), the intermediary port (`eShopPorted`), and shared libraries (`eShopLegacy.Utilities`). The target deployment is a single-service modernization to .NET 10 on Linux hosted on Cloud Run.

###### Vulnerability Summary Table

| Identifier | Severity | Category | Target Component | Impact |
| :--- | :--- | :--- | :--- | :--- |
| SEC-01 | Critical | Insecure Deserialization | `Serializing.cs` / `FilesController.cs` | Potential Remote Code Execution (RCE) via `BinaryFormatter` |
| SEC-02 | High | Broken Access Control | `CatalogController.cs`, `BrandsController.cs` | Unauthorized creation, modification, and deletion of catalog data |
| SEC-03 | High | Path Traversal | `PicController.cs`, `CatalogController.cs` | Arbitrary local file disclosure via untrusted file path concatenation |
| SEC-04 | Medium | Supply Chain & External Execution | `Convert-ToPackageReference.ps1` | Execution of unverified remote code via unauthenticated gist stylesheet |
| SEC-05 | Medium | End-of-Life Runtime & Dependencies | `.csproj` and `packages.config` files | Exposure to known CVEs across unsupported runtimes (.NET 4.6.1/4.7.2, ASP.NET Core 2.2) |
| SEC-06 | Low | Information Disclosure | `CatalogController.cs`, `_Layout.cshtml`, `Startup.cs` | Infrastructure fingerprinting via machine name, timestamp, and verbose stack traces |
| SEC-07 | Low | Unsafe SQL Construction | `CatalogDBInitializer.cs` | Risk of SQL injection via direct string interpolation into database execution routines |

---

###### Insecure Deserialization via `BinaryFormatter` (SEC-01)

####### Vulnerability Analysis
The utility library exposes binary serialization routines using the legacy .NET `BinaryFormatter` class [^1]. Both API controllers (`FilesController.cs` in `eShopLegacyMVC` and `eShopPorted`) invoke `SerializeBinary` [^2] [^3]. 

`BinaryFormatter` is inherently insecure for untrusted inputs. It serializes the object graph alongside full type metadata, allowing attackers who supply crafted binary payloads to instantiate arbitrary object graphs and trigger execution gadgets during deserialization [^1]. Microsoft has deprecated `BinaryFormatter` in modern .NET releases, and runtime support is disabled or completely removed in .NET 9 and .NET 10.

```csharp
// eShopLegacy.Utilities/Serializing.cs
public object DeserializeBinary(Stream stream)
{
    var binaryFormatter = new BinaryFormatter();
    stream.Seek(0, SeekOrigin.Begin);
    return binaryFormatter.Deserialize(stream); // CWE-502: Deserialization of Untrusted Data
}
```

####### Remediation Strategy
1. Deprecate and remove `BinaryFormatter` entirely.
2. Standardize on `System.Text.Json` or contract-based serializers like Protocol Buffers (`protobuf-net`) for binary payloads.
3. Transition endpoints returning files or serialized brand lists to return structured JSON payloads (`application/json`).

---

###### Broken Access Control on Data-Modifying Endpoints (SEC-02)

####### Vulnerability Analysis
The application lacks authentication and authorization filters across all controllers. State-modifying actions—such as `CatalogController.Create` [^4], `CatalogController.Edit` [^5], `CatalogController.DeleteConfirmed` [^6], and `BrandsController.Delete` [^7]—can be executed anonymously. 

Neither project configures authorization middleware or role-based access attributes (e.g., `[Authorize]`). In `launchSettings.json`, anonymous authentication is explicitly enabled while Windows authentication is disabled [^8].

```csharp
// eShopPorted/Controllers/CatalogController.cs
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult Create([Bind("Id,Name,Description,Price,PictureFileName,CatalogTypeId,CatalogBrandId,AvailableStock,RestockThreshold,MaxStockThreshold,OnReorder")] CatalogItem catalogItem)
{
    // Accessible anonymously; no role check
    if (ModelState.IsValid)
    {
        service.CreateCatalogItem(catalogItem);
        return RedirectToAction("Index");
    }
    // ...
}
```

####### Remediation Strategy
1. Integrate ASP.NET Core authentication middleware using OpenID Connect (OIDC) or JWT Bearer tokens.
2. Apply `[Authorize]` globally or at the controller level, requiring specific roles or policies for mutating actions (`Create`, `Edit`, `Delete`).
3. When deployed to Cloud Run, enforce ingress security and authenticate callers via Google Cloud Identity-Aware Proxy (IAP) or Cloud Run IAM invoke permissions.

---

###### Path Traversal in Image Serving (SEC-03)

####### Vulnerability Analysis
In `PicController.cs`, the `Index` action retrieves a `CatalogItem` by ID and reads the picture file directly from the filesystem using `System.IO.File.ReadAllBytes(path)` [^9] [^10]. The file name is obtained from `item.PictureFileName`, which is stored in the database. 

However, `PictureFileName` is bound directly from client input during create and edit requests in `CatalogController` [^4] [^5]. An attacker with access to edit or create catalog entries can submit directory traversal sequences (such as `../../../../etc/passwd` or `..\..\..\Windows\win.ini`) as the `PictureFileName`. When `PicController.Index` is called, it constructs a path outside the intended root directory, causing arbitrary file disclosure.

```csharp
// src/eShopLegacyMVC/Controllers/PicController.cs
var webRoot = Server.MapPath("~/Pics");
var path = Path.Combine(webRoot, item.PictureFileName); // Vulnerable to path manipulation if PictureFileName contains ../
var buffer = System.IO.File.ReadAllBytes(path);
return File(buffer, mimetype);
```

####### Remediation Strategy
1. Disallow arbitrary filenames by generating a deterministic identifier (e.g., `{catalogItemId}.png`).
2. Validate and sanitize inputs using `Path.GetFileName()` to strip directory sequences, and verify that the resolved path starts with the designated image directory via `Path.GetFullPath()`.
3. For cloud deployment on Cloud Run, store catalog images in a Google Cloud Storage bucket rather than on the local container filesystem.

---

###### External Code Execution in Migration Tooling (SEC-04)

####### Vulnerability Analysis
The repository includes a PowerShell automation script, `Convert-ToPackageReference.ps1`, designed to assist in migrating `packages.config` files to MSBuild `PackageReference` nodes [^11]. The script retrieves an XSL stylesheet directly from an unauthenticated, non-pinned GitHub Gist URL:

```powershell
#### Convert-ToPackageReference.ps1
Param ( [Parameter( Mandatory, ValueFromPipeline )][String] $inputUri,
        [String] $stylesheetUri = "https://gist.githubusercontent.com/a4099181/074a6c3dd524ea0d343382137492399c/raw/e2cd1bc88671bb1435569ba9ab1835994aa91725/Convert-ToPackageReference.xsl",
        [String] $resultsFile   = [System.IO.Path]::GetTempFileName() )
```

Loading and applying untrusted remote stylesheets via `XslCompiledTransform` creates a supply chain attack surface. If the external Gist account is modified or compromised, an attacker could manipulate build configurations or execute code during build-time conversions.

####### Remediation Strategy
1. Vendor the XSLT file directly within the source repository and load it locally.
2. Validate file integrity using SHA-256 hashes before loading transformation assets.

---

###### End-of-Life Runtime and Third-Party Dependencies (SEC-05)

####### Vulnerability Analysis
The projects target deprecated or end-of-life platforms:
- `eShopLegacyMVC.csproj` targets .NET Framework 4.7.2 with Entity Framework 6.2.0 [^12].
- `eShopPorted.csproj` targets `net461` using ASP.NET Core 2.2 and Entity Framework Core 2.2.6 [^13]. ASP.NET Core 2.2 reached end of life in December 2019 and no longer receives security updates.
- Vulnerable client-side dependencies:
  - `jQuery 3.3.1` / `jQuery 3.5.0` (susceptible to prototype pollution and regex denial-of-service in outdated sub-versions).
  - `Bootstrap 4.3.1` (known XSS vulnerabilities in tooltip and popover plugins).

####### Remediation Strategy
1. Modernize the unified project to .NET 10, running on Linux containers.
2. Upgrade Entity Framework Core to the matching .NET 10 release.
3. Update or replace client dependencies with actively maintained modern packages or integrate automated dependency scanning (e.g., Dependabot or Renovate).

---

###### Information Disclosure via Views and Error Handling (SEC-06)

####### Vulnerability Analysis
The legacy `_Layout.cshtml` view prints internal runtime diagnostics in the application footer:
- Displays `Environment.MachineName` and session initiation timestamp [^14].
- In `eShopPorted`, `_Layout.cshtml` outputs `Environment.MachineName` alongside `Startup.StartTime` [^15].

Exposing container hostnames or server machine names simplifies infrastructure reconnaissance. Furthermore, `Web.config` enables compilation debug mode (`compilation debug="true"`) [^16], which risks exposing stack traces and inner application details during unhandled exceptions.

####### Remediation Strategy
1. Remove infrastructure metadata (machine names, hostnames) from client-facing layouts.
2. Set production configurations to suppress stack traces, and enforce standard generic error pages.

---

###### Unsafe SQL Construction in Initializer (SEC-07)

####### Vulnerability Analysis
In `CatalogDBInitializer.cs`, database sequence IDs are retrieved using C# interpolated strings directly passed to Entity Framework raw SQL routines [^17]:

```csharp
// src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs
private static int GetSequenceIdFromSelectedDBSequence(CatalogDBContext context, string dBSequenceName)
{
    var rawQuery = context.Database.SqlQuery<Int64>($"SELECT NEXT VALUE FOR {dBSequenceName}");
    var sequenceId = (int)rawQuery.Single();
    return sequenceId;
}
```

Although `dBSequenceName` is currently passed from internal constants, executing string-interpolated dynamic SQL without parameterization or identifier validation is an insecure coding pattern that risks SQL injection if refactored to consume external input.

####### Remediation Strategy
1. Replace custom sequence generators and raw SQL calls with native database identity columns or standard EF Core value generation strategies (e.g., `UseHiLo`).
2. If dynamic identifiers are required, strictly validate against an allowlist of permitted table or sequence names.

---

##### Privacy Concerns

###### Assessment Against Privacy Regulations (GDPR, CCPA)

| Privacy Area | Current Codebase State | Compliance Risk | Recommended Action |
| :--- | :--- | :--- | :--- |
| **Personal Identifiable Information (PII)** | No user profiles, customer names, or addresses are stored. The domain model is strictly catalog data [^18]. | Low | Maintain catalog domain boundary. Ensure future customer-facing extensions include data classification. |
| **User Tracking & IP Collection** | Application Insights telemetry is configured with `ClientIpHeaderTelemetryInitializer` [^19]. Client IP addresses are logged. | Medium | Mask or truncate client IP addresses before transmitting telemetry to satisfy GDPR Article 4. |
| **Request Logging** | `WebRequestInfo` logs `RawUrl` and `UserAgent` strings [^20]. Query strings could inadvertently leak sensitive parameters. | Medium | Sanitize HTTP query parameters before logging to internal files or cloud monitoring tools. |
| **Cookie & Session Management** | Session state runs in-process (`InProc` in `Web.config`) [^16], storing machine details and timestamps [^21]. | Low | Eliminate cookie-based sticky sessions in favor of stateless container execution on Cloud Run. |

---

##### Compliance Evaluation

###### Standard Compliance Mappings

| Standard / Framework | Baseline Requirement | Observed Codebase Gap | Remediation for .NET 10 on Google Cloud |
| :--- | :--- | :--- | :--- |
| **SOC 2 Type II** (Security / Availability) | Enforce least privilege, access controls, and comprehensive audit logs. | Endpoints lack authentication [^4] [^7]. Application logs to local flat files (`myapp.log`) without integrity controls [^22]. | Enforce Cloud Identity / OIDC authentication. Direct structured JSON logs to standard output for ingestion by Google Cloud Logging. |
| **ISO/IEC 27001** (Asset Protection) | Protect system assets and prevent unauthorized configuration changes. | Plaintext connection strings and mock toggles in `appsettings.json` [^23] and `Web.config` [^16]. | Move connection strings and secrets to Google Cloud Secret Manager. Mount secrets as environment variables in Cloud Run. |
| **PCI DSS v4.0** | Secure transmission, input validation, and protection against unauthorized data access. | No data encryption in transit enforced at the application layer; plain HTTP enabled in launch settings [^8]. | Terminate TLS via Cloud Run default domains or Google Cloud Load Balancing. Enforce HTTPS redirection in .NET 10 middleware. |
| **NIST CSF** (Identify / Protect / Detect) | Continuous vulnerability management and patch maintenance. | Reliance on abandoned ASP.NET Core 2.2 and .NET 4.6.1/4.7.2 packages [^12] [^13]. | Migrate to supported LTS runtime (.NET 10). Establish container scanning in Google Cloud Artifact Registry. |

---

##### Threat Modeling

###### STRIDE Assessment for Monolithic Cloud Run Architecture

```
               [ User / Client ]
                       |
                       | (HTTPS)
                       v
         [ Cloud Run Ingress / WAF ]
         (Google Cloud Armor + TLS)
                       |
                       v
    +-------------------------------------+
    |      Cloud Run Service Container    |
    |  [.NET 10 Single-Service Monolith]  |
    |                                     |
    |  - Middleware: AuthN/AuthZ          |
    |  - Controllers: MVC & Web API       |
    |  - Logging: Google Cloud Logging    |
    +-------------------------------------+
           |                     |
   (Private Network)     (Google Cloud API)
           v                     v
   [ Cloud SQL Instance ] [ Cloud Storage ]
     (Catalog Database)     (Catalog Images)
```

| Threat Category | Threat Scenario | Current Handling | Target Mitigation on Cloud Run (.NET 10) |
| :--- | :--- | :--- | :--- |
| **Spoofing** | Attacker impersonates an administrator to manipulate catalog prices or delete items. | Unmitigated; all endpoints are open to anonymous access [^4] [^7]. | Require OIDC/OAuth2 authentication via Identity-Aware Proxy or Cloud Run IAM. |
| **Tampering** | Attacker tampers with the `PictureFileName` parameter to access unauthorized server files. | Unmitigated; controller passes raw filename to file loader [^5] [^9]. | Sanitize filename inputs; transition static asset storage to Google Cloud Storage. |
| **Repudiation** | An attacker creates, updates, or deletes catalog inventory without an audit trail. | Log entries record request URLs [^20], but lack identity context. | Implement structured audit logging capturing authenticated caller identity, timestamp, and action. |
| **Information Disclosure** | Attackers inspect internal server names and runtime timestamps from the layout footer. | Internal machine names and startup timestamps are printed directly to HTML [^14] [^15]. | Remove runtime debugging details from public views; suppress HTTP banner headers. |
| **Denial of Service** | Volumetric HTTP requests overwhelm the container instance or flood local file appenders. | Local log files use a rolling 10MB policy [^22]; application has no rate limiting. | Cloud Run automatic scaling; Cloud Armor rate limiting and DDoS protection. |
| **Elevation of Privilege** | Attacker exploits deserialization flaws in `BinaryFormatter` to run code as container user. | `BinaryFormatter` is used directly in `FilesController` [^2] [^3]. | Remove `BinaryFormatter` completely. Run container as an unprivileged non-root user. |

---

##### Risk Assessment and Prioritization

###### Risk Matrix

| Risk ID | Vulnerability / Issue | Likelihood | Impact | Priority | Target Remediation Stage |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **R-01** | Insecure Deserialization via `BinaryFormatter` | High | Critical | P1 (Immediate) | .NET 10 Migration |
| **R-02** | Unauthenticated Mutating Operations | High | High | P1 (Immediate) | Pre-Deployment / Middleware |
| **R-03** | Path Traversal via Image Fetching | Medium | High | P2 (High) | Code Refactoring |
| **R-04** | Deprecated Runtimes (.NET Framework / Core 2.2) | High | Medium | P2 (High) | .NET 10 Migration |
| **R-05** | Plaintext Secrets in Config Files | Medium | High | P2 (High) | Google Cloud Infrastructure Setup |
| **R-06** | Information Exposure in Layout Views | High | Low | P3 (Medium) | Code Cleanup |
| **R-07** | Local Rolling File Logging inside Container | High | Medium | P3 (Medium) | Containerization |

---

##### Google Cloud Integration Recommendations

Because the modernized system is a single monolithic service deployed to Cloud Run on Linux using .NET 10, Google Cloud managed security offerings should be integrated without introducing microservices:

```
+-----------------------------------------------------------------------------------+
|                                Google Cloud Platform                              |
|                                                                                   |
|  +--------------------+       +------------------------------------------------+  |
|  | Google Cloud Armor | ----> |                   Cloud Run                    |  |
|  | - DDoS Defense     |       |  +------------------------------------------+  |  |
|  | - OWASP Top 10 WAF |       |  |     .NET 10 Linux Container Monolith     |  |  |
|  +--------------------+       |  |  - Auth Middleware                       |  |  |
|                               |  |  - Strict Input Sanitization              |  |  |
|  +--------------------+       |  +------------------------------------------+  |  |
|  |  Cloud Secret Mgr  | ----> |          ^                          ^          |  |
|  | - DB Credentials   |       +----------|--------------------------|----------+  |
|  +--------------------+                  |                          |             |
|                                          v                          v             |
|                               +--------------------+     +---------------------+  |
|                               |  Cloud SQL Server  |     | Google Cloud Storage|  |
|                               |  - IAM DB Auth     |     | - Static Item Pics  |  |
|                               +--------------------+     +---------------------+  |
+-----------------------------------------------------------------------------------+
```

###### Recommended Google Cloud Security Services

| Service | Architectural Role | Security & Compliance Benefit |
| :--- | :--- | :--- |
| **Cloud Run Container Security** | Host the unified .NET 10 monolith in a secure, containerized sandbox on Linux. | Cloud Run provides built-in container isolation (gVisor sandboxing), automatic TLS termination, and scales down to zero when idle. |
| **Google Cloud Secret Manager** | Secure storage and injection of the database connection strings [^23]. | Eliminates credentials stored in source control or `appsettings.json`. Secrets are mounted directly into Cloud Run container environment variables. |
| **Google Cloud Armor** | Fronting Cloud Run via an External Application Load Balancer. | Protects against Layer 7 DoS/DDoS attacks, provides geographic filtering, and enforces predefined WAF rules for OWASP Top 10 mitigation. |
| **Cloud Identity and Access Management (IAM)** | Principle of least privilege for the Cloud Run runtime service account. | Restricts container permissions solely to necessary Google Cloud resources (e.g., read Secret Manager secrets, read/write to Cloud Storage). |
| **Google Cloud Security Command Center (SCC)** | Continuous threat detection and compliance monitoring. | Scans container images stored in Google Cloud Artifact Registry for operating system and library CVEs prior to Cloud Run deployment. |
| **Google Cloud Logging & Monitoring** | Replaces file-based `log4net` appenders [^22]. | Aggregates stdout/stderr logs from the .NET 10 console logger, providing immutable centralized storage and alert triggers for suspicious request patterns. |

---

##### Recommendations and Best Practices

###### Secure Coding Practices for .NET 10
1. **Eliminate Legacy Serializers**:
   Replace `Serializing.cs` [^1] with `System.Text.Json`. Ensure no polymorphic type handling is enabled without strict allowlists.
2. **Strict Model Binding and Sanitization**:
   Avoid binding raw database file paths. In `PicController` [^9] [^10], resolve images using strong integer lookups or hash-based object keys rather than string concatenation.
3. **Cross-Site Request Forgery (CSRF)**:
   Retain and verify `[ValidateAntiForgeryToken]` on all POST/PUT/DELETE actions [^4] [^5] [^6]. In ASP.NET Core (.NET 10), configure automatic antiforgery validation filters.
4. **Parameterized Data Access**:
   Ensure all Entity Framework operations utilize LINQ queries or parameterized expressions. Eliminate interpolated strings in `context.Database.ExecuteSqlRaw` or `FromSqlInterpolated`.

###### Cloud Run Container Hardening
1. **Non-Root Execution**:
   Configure the Dockerfile to run the .NET 10 application under an unprivileged user (e.g., `USER app` provided in the standard Microsoft .NET 10 chiseled Ubuntu or Alpine container images).
2. **Read-Only Root Filesystem**:
   Because container storage in Cloud Run is ephemeral and writing to local files [^22] is an anti-pattern in serverless environments, deploy the container with an in-memory temporary filesystem (`/tmp`) and direct all logging to standard output.
3. **Minimal Attack Surface**:
   Use multi-stage Docker builds to package only the published .NET 10 binaries and the stripped runtime, discarding build tools, PowerShell scripts [^11], and local CSV/ZIP database setup files [^24] from the production container.

---

###### Citations

[^1]: `eShopLegacy.Utilities/Serializing.cs`: `Serializing` class - Implements `BinaryFormatter` serialization and deserialization routines for generic objects.
[^2]: `eShopPorted/Controllers/Api/FilesController.cs`: `FilesController.Index()` - Invokes `Serializing.SerializeBinary` to return binary-serialized brand objects over HTTP.
[^3]: `src/eShopLegacyMVC/Controllers/WebApi/FilesController.cs`: `FilesController.Get()` - Serializes brand DTOs using `Serializing.SerializeBinary` and returns them in an `HttpResponseMessage`.
[^4]: `eShopPorted/Controllers/CatalogController.cs`: `CatalogController.Create(CatalogItem)` - HTTP POST endpoint creating a catalog item without authorization checks.
[^5]: `eShopPorted/Controllers/CatalogController.cs`: `CatalogController.Edit(CatalogItem)` - HTTP POST endpoint updating an existing catalog item, including `PictureFileName`, without access controls.
[^6]: `eShopPorted/Controllers/CatalogController.cs`: `CatalogController.DeleteConfirmed(int)` - HTTP POST action deleting a catalog item by ID without authentication or role verification.
[^7]: `eShopPorted/Controllers/Api/BrandsController.cs`: `BrandsController.Delete(int)` - HTTP DELETE endpoint allowing unauthenticated requests to target brand records.
[^8]: `eShopPorted/Properties/launchSettings.json`: `iisSettings` configuration - Configures `windowsAuthentication` to false and `anonymousAuthentication` to true.
[^9]: `eShopPorted/Controllers/PicController.cs`: `PicController.Index(int)` - Reads local file bytes using an unvalidated path composed of webRoot and `item.PictureFileName`.
[^10]: `src/eShopLegacyMVC/Controllers/PicController.cs`: `PicController.Index(int)` - Constructs local image file path using `Server.MapPath` and reads bytes directly via `System.IO.File.ReadAllBytes`.
[^11]: `Convert-ToPackageReference.ps1`: `Convert-ToPackageReference` function - Downloads and applies an XSL compiled transform from an external GitHub Gist URI without integrity validation.
[^12]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: Project target definition - Specifies legacy target framework `v4.7.2` and packages EntityFramework 6.2.0.
[^13]: `eShopPorted/eShopPorted.csproj`: Project target definition - Specifies deprecated target framework `net461` and references Microsoft.AspNetCore 2.2.0.
[^14]: `src/eShopLegacyMVC/Views/Shared/_Layout.cshtml`: Footer rendering logic - Outputs `Session["MachineName"]` and `Session["SessionStartTime"]` directly to client HTML.
[^15]: `eShopPorted/Views/Shared/_Layout.cshtml`: Footer rendering logic - Displays server `Environment.MachineName` and `Startup.StartTime` in layout footer.
[^16]: `src/eShopLegacyMVC/Web.config`: `<system.web>` section - Enables debug compilation mode (`debug="true"`) and configures `InProc` session state.
[^17]: `src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs`: `CatalogDBInitializer.GetSequenceIdFromSelectedDBSequence(CatalogDBContext, string)` - Executes interpolated raw SQL query against database sequence.
[^18]: `eShopPorted/Models/CatalogItem.cs`: `CatalogItem` class - Defines catalog product properties with no personal data fields.
[^19]: `src/eShopLegacyMVC/ApplicationInsights.config`: `TelemetryInitializers` configuration - Registers `ClientIpHeaderTelemetryInitializer` for client IP logging.
[^20]: `src/eShopLegacyMVC/Global.asax.cs`: `WebRequestInfo.ToString()` - Captures raw request URL and user agent into thread context for log outputs.
[^21]: `src/eShopLegacyMVC/Global.asax.cs`: `MvcApplication.Session_Start(Object, EventArgs)` - Stores machine name and timestamp into the HTTP session.
[^22]: `src/eShopLegacyMVC/log4Net.xml`: `<appender name="file">` - Configures local file system log writing to `logFiles\myapp.log` using `RollingFileAppender`.
[^23]: `eShopPorted/appsettings.json`: `ConnectionStrings` configuration - Stores unencrypted local database connection string in application configuration.
[^24]: `src/eShopLegacyMVC/Setup/CatalogItems.csv`: Setup CSV data - Bundles preconfigured database seed files within project source directory.

### Modernization Challenges
#### Current State Assessment & Migration Scope

The solution (`eShopLegacyMVC.sln`) comprises three projects displaying varying stages of architectural maturity and platform alignment [^1]:
1. `src/eShopLegacyMVC`: A legacy monolithic web application built on **ASP.NET MVC 5.2.7 / Web API 2** and **.NET Framework 4.7.2** [^2], using Entity Framework 6.2.0 [^3] and classic IIS/ASP.NET runtime components [^4].
2. `eShopPorted`: An intermediate migration artifact utilizing **ASP.NET Core 2.2** hosted on top of **.NET Framework 4.6.1 (`net461`)** and Entity Framework Core 2.2.6 [^5].
3. `eShopLegacy.Utilities`: A shared class library targeting **.NET Framework 4.6.1** that contains legacy binary serialization logic [^6].

The target modernization state is a single, unified service running on **.NET 10 on Linux**, deployable as a containerized workload on **Google Cloud Run**. The following sections provide an objective technical assessment of the existing challenges, technical debt, and architectural blockers encountered across this migration pathway.

---

#### Migration Challenges: ASP.NET to ASP.NET Core

Migrating from legacy ASP.NET (System.Web architecture) to ASP.NET Core requires addressing foundational differences in HTTP request lifecycle, pipeline hosting, configuration, and presentation libraries.

##### ASP.NET Feature Gaps and ASP.NET Core Equivalents

| Legacy ASP.NET Feature / API | Usage in Codebase | Supported in ASP.NET Core | ASP.NET Core Equivalent |
| :--- | :--- | :--- | :--- |
| `System.Web.Mvc.Controller` / `System.Web.Http.ApiController` | Split controllers across MVC and Web API namespaces (`CatalogController`, `BrandsController`, `FilesController`) [^7] [^8] [^9] | No (Unified in ASP.NET Core) | `Microsoft.AspNetCore.Mvc.ControllerBase` or `Controller` (unified routing & action results) |
| `System.Web.Optimization` (Bundling/Minification) | `BundleConfig.RegisterBundles` configuring Script/Style bundles [^10] | No | Static file serving (`UseStaticFiles()`), Client-side build tools (Vite/Webpack), or LibMan / BundlerMinifier |
| `HttpApplication` (`Global.asax.cs`) | Lifecycle management via `Application_Start`, `Session_Start`, `Application_BeginRequest` [^11] | No | `Program.cs` / Middleware Pipeline (`app.Use(...)`), `IHostedService` |
| `Server.MapPath()` | Physical file path resolution for catalog pictures in `PicController` [^12] | No | `IWebHostEnvironment.WebRootPath` / `ContentRootPath` |
| In-Proc `HttpSessionState` | Tracking `MachineName` and `SessionStartTime` in `Session_Start` [^13] | No | `Microsoft.AspNetCore.Session` with external caching / Distributed Cache |
| Request Filters & Global Handlers | `FilterConfig.RegisterGlobalFilters(new HandleErrorAttribute())` [^14] | No | Global exception handler middleware (`UseExceptionHandler`) or MVC Exception Filters |

##### Configuration (`Web.config`) Structure

The legacy application relies entirely on XML-based configuration files (`Web.config`, `Web.Debug.config`, `Web.Release.config`) [^15]:
- **`<connectionStrings>`**: Configures the database connection string named `CatalogDBContext` referencing local SQL Server LocalDB with Windows integrated authentication (`Server=(localdb)\MSSQLLocalDB; ...`) [^16].
- **`<appSettings>`**: Governs runtime behavior through keys such as `UseMockData` and `UseCustomizationData` [^17].
- **ASP.NET Core Discrepancy**: ASP.NET Core does not read `Web.config` appSettings or connectionStrings natively for application configuration. These must be represented using JSON configuration providers (`appsettings.json`), environment variables, or secret managers.

##### HTTP Handlers, Modules, and Pipeline Components

The legacy `Web.config` and `Global.asax.cs` register custom runtime extensions that rely heavily on `System.Web`:
- **`system.webServer/modules`**: Registers `TelemetryCorrelationHttpModule`, `ApplicationInsightsWebTracking`, and an asynchronous session state module: `Microsoft.AspNet.SessionState.SessionStateModuleAsync` [^18].
- **`system.webServer/handlers`**: Defines `ExtensionlessUrlHandler-Integrated-4.0` mapping all HTTP verbs to `System.Web.Handlers.TransferRequestHandler` [^19].
- **`Global.asax.cs` Request Lifecycle**:
  - `Application_Start()` orchestrates dependency injection container creation, Web API route mapping, global filter configuration, and database initializer execution [^20].
  - `Session_Start()` accesses `HttpContext.Current.Session` directly [^21].
  - `Application_BeginRequest()` injects diagnostic properties (`activityid`, `requestinfo`) into log4net's `LogicalThreadContext` [^22].
- **Blockers**: `IHttpHandler`, `IHttpModule`, and `Global.asax` lifecycle hooks do not exist in ASP.NET Core; all pipeline behavior must be converted into ASP.NET Core middleware components.

---

#### Migration Challenges: .NET Framework to .NET 10

Migrating all projects in the solution from legacy .NET Framework versions (4.6.1 and 4.7.2) to modern .NET 10 entails project file format modernization, library compatibility replacements, and managing obsolete framework primitives.

##### Library Dependencies and .NET 10 Compatibility

| Dependency / Component | Current Version | Project Context | Compatible with .NET 10? | Replacement / Alternative in .NET 10 |
| :--- | :--- | :--- | :--- | :--- |
| **EntityFramework** | 6.2.0 [^3] | `src/eShopLegacyMVC` | Partial / Deprecated (EF6 on .NET is maintenance-only) | **Entity Framework Core 10** |
| **Microsoft.EntityFrameworkCore** | 2.2.6 [^23] | `eShopPorted` | No (Out of support; targeting netstandard2.0 / .NET Core 2.2) | Upgrade to EF Core 10 |
| **Autofac.Mvc5** / **Autofac.WebApi2** | 4.0.2 / 6.0.1 [^24] | `src/eShopLegacyMVC` | No (Bound to legacy ASP.NET interfaces) | Native `Microsoft.Extensions.DependencyInjection` or `Autofac.Extensions.DependencyInjection` v10+ |
| **Microsoft.AspNet.Mvc** | 5.2.7 [^25] | `src/eShopLegacyMVC` | No | `Microsoft.AspNetCore.Mvc` |
| **Microsoft.AspNet.WebApi** | 5.2.7 [^26] | `src/eShopLegacyMVC` | No | Unified ASP.NET Core Web API Controllers |
| **BinaryFormatter** | .NET 4.6.1 BCL [^27] | `eShopLegacy.Utilities/Serializing.cs` | **Completely Removed** in .NET 9/.NET 10 (hard runtime errors) | `System.Text.Json` or `System.Text.Json.JsonSerializer` |
| **log4net** | 2.0.10 [^28] | `src/eShopLegacyMVC`, `eShopPorted` | Yes, but legacy configuration (`log4Net.xml`) does not natively bind to modern hosting logging | `Microsoft.Extensions.Logging` (Console provider / OpenTelemetry) |
| **Microsoft.ApplicationInsights.Web** | 2.9.1 [^29] | `src/eShopLegacyMVC` | No (IIS/ASP.NET specific telemetry modules) | `Microsoft.ApplicationInsights.AspNetCore` or OpenTelemetry .NET SDK |

##### Project File Format Analysis

1. **`src/eShopLegacyMVC/eShopLegacyMVC.csproj`**:
   - Uses verbose, legacy MSBuild XML with explicit project flavor GUIDs (`{349c5851-65df-11da-9384-00065b846f21}`) [^30].
   - Contains explicit `<Compile Include="...">` and `<Content Include="...">` item groups for every single C# file, view, CSS file, and font asset [^31].
   - Employs mixed dependency tracking: partially uses `packages.config` and partially inline `<PackageReference>` tags [^32] [^33].
   - Migration requires converting to a simplified SDK-style project file (`<Project Sdk="Microsoft.NET.Sdk.Web">`).
2. **`eShopLegacy.Utilities/eShopLegacy.Utilities.csproj`**:
   - Uses legacy non-SDK MSBuild format targeting `v4.6.1` [^34].
   - Requires conversion to `<Project Sdk="Microsoft.NET.Sdk">` with target framework `<TargetFramework>net10.0</TargetFramework>`.
3. **`eShopPorted/eShopPorted.csproj`**:
   - Uses SDK-style project format but explicitly locks `<TargetFramework>` to `net461` [^35].
   - References outdated ASP.NET Core 2.2 metapackages that cannot compile against modern .NET runtimes without comprehensive package upgrades [^36].

---

#### Migration Challenges: Windows to Linux

Deploying the application to Linux containers within Google Cloud Run introduces critical challenges involving Windows-specific operating system dependencies, file path semantics, and case sensitivity.

##### Operating System and Platform Dependencies

| Windows-Specific Technology / Pattern | Codebase Location | Linux Impact / Compatibility Blocker | Linux Alternative / Mitigation Requirement |
| :--- | :--- | :--- | :--- |
| **IIS Server In-Process Hosting** | `Web.config`, `global.asax` [^18] [^11] | IIS is not available on Linux. | Kestrel web server (`Microsoft.AspNetCore.Server.Kestrel`) |
| **SQL Server LocalDB Connection** | `Web.config`, `appsettings.json` [^16] [^37] | `(localdb)\MSSQLLocalDB` relies on Windows Named Pipes and OS-level instances; strictly unsupported on Linux. | Standard TCP-based SQL Server connection, or Google Cloud SQL for SQL Server over private IP/Cloud SQL Auth Proxy |
| **Hardcoded Windows Path Separators** | `CatalogDBInitializer.cs` [^38] | Hardcoded strings with backslashes (`Models\Infrastructure\dbo.catalog_hilo.Sequence.sql`) fail on Linux due to path delimiter divergence. | `Path.Combine(...)` or forward-slash normalized relative paths |
| **Case-Sensitive File System Navigation** | Static files (`wwwroot`, `Pics`), view names, SQL scripts [^39] | Windows NTFS is case-insensitive; Linux file systems (ext4, tmpfs) are case-sensitive. Mismatched casing (e.g., `Pics` vs `pics`, `log4Net.xml` vs `log4net.xml`) leads to runtime `FileNotFoundException`. | File and path normalization to consistent lowercase/matching casing |
| **`HostingEnvironment.ApplicationPhysicalPath`** | `CatalogDBInitializer.cs` [^40] | Relies on ASP.NET hosting APIs bound to Windows IIS worker process structures. | `AppContext.BaseDirectory` or `IHostEnvironment.ContentRootPath` |
| **Windows Performance Counters** | `ApplicationInsights.config` [^41] | Uses Win32/CLR process performance counters (`\Process(??APP_WIN32_PROC??)\Handle Count`). | EventCounters, .NET 10 Meters / OpenTelemetry metrics |

---

#### Technical Debt, Scalability, and Performance Bottlenecks

##### Legacy Practices & Technical Debt

- **Unsafe Binary Serialization**: `eShopLegacy.Utilities.Serializing` uses `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter` to serialize DTOs to streams [^27]. This class introduces critical remote code execution vulnerabilities when deserializing untrusted data and is completely blocked in modern .NET releases.
- **Inconsistent Partial Porting**: The existence of `eShopPorted` alongside `eShopLegacyMVC` demonstrates an incomplete porting effort [^1]. `eShopPorted` was abandoned on .NET Framework 4.6.1 with ASP.NET Core 2.2 [^35], creating codebase divergence and duplicate models/controllers [^42] [^43].
- **Thread Context Logging Coupling**: `Global.asax.cs` sets static correlation data via `LogicalThreadContext` [^22], which does not propagate reliably across asynchronous async/await state machines in modern .NET runtimes.

##### Scalability Issues

- **In-Memory and Local File State**:
  - `PicController` resolves picture assets directly from local disk storage (`Server.MapPath("~/Pics")` or relative file reads) [^12] [^44].
  - `CatalogDBInitializer` writes picture binaries to local disk at runtime by extracting a ZIP archive (`Setup/CatalogItems.zip`) into local application directories [^45].
  - **Horizontal Scaling Blocker**: Running multiple instances of the container on Google Cloud Run will result in inconsistencies or failures because local disk storage is ephemeral and instance-scoped.
- **Synchronous Database Operations**:
  - `CatalogService` and `CatalogController` execute database access synchronously (`ToList()`, `Find()`, `SaveChanges()`, `LongCount()`) rather than utilizing asynchronous execution (`ToListAsync()`, `SaveChangesAsync()`) [^46] [^47].
  - Under concurrent loads, synchronous blocking saturates thread pool workers, severely constraining vertical scalability and request throughput.
- **Client-Side Hi/Lo Key Generator Concurrency Contention**:
  - `CatalogItemHiLoGenerator` uses a local in-memory lock (`lock (sequenceLock)`) to increment IDs locally before issuing batch sequence commands to SQL Server [^48].
  - When scaled horizontally across multiple container instances, local locks fail to coordinate sequence bounds across instances unless sequence blocks are coordinated through atomic database sequences.

##### Performance Bottlenecks

- **File-to-Byte Buffer Reads on Every Image Request**:
  - `PicController.Index()` calls `System.IO.File.ReadAllBytes(path)` on every invocation [^49], loading binary files into the managed heap without caching or HTTP cache-control headers.
- **Heavy Database Initializer Overhead**:
  - `CatalogDBInitializer.Seed()` reads multiple CSV files (`CatalogTypes.csv`, `CatalogBrands.csv`, `CatalogItems.csv`), parses them via regular expressions, reads SQL files, and executes multiple database round-trips synchronously during application startup [^50].
  - In containerized environments like Google Cloud Run, cold start latency would be heavily degraded by this startup sequence.

---

#### Security Assessment of Legacy Practices

```
                                CRITICAL SECURITY RISKS
                                
  [ BinaryFormatter Usage ]                     [ IIS / SQL Server LocalDB ]
  ---------------------------------             ---------------------------------
  - Present in Serializing.cs                   - Windows-authenticated local strings
  - Insecure object graph deserialization       - Incompatible with container isolation
  - Vulnerable to Remote Code Execution (RCE)   - Hardcoded local database targets
  
  [ Outdated Dependency Chain ]                 [ Hardcoded Secret Vectors ]
  ---------------------------------             ---------------------------------
  - ASP.NET Core 2.2 (End-of-Life)              - Plaintext connection strings
  - Entity Framework 6.2.0 (Legacy)             - Static credentials in Web.config
  - Autofac 4.9.1 / Newtonsoft.Json 12.0.1      - Unsecured dev flags (UseMockData)
```

1. **Remote Code Execution via `BinaryFormatter`**:
   - `eShopLegacy.Utilities/Serializing.cs` implements `BinaryFormatter.Serialize` and `Deserialize` [^27].
   - `FilesController` invokes `SerializeBinary` [^51]. Deserialization of binary formatter streams allows arbitrary type instantiation, leading directly to Remote Code Execution (RCE).
2. **Unsupported, End-of-Life Runtime & Metapackages**:
   - `eShopPorted` references ASP.NET Core 2.2 metapackages [^36]. ASP.NET Core 2.2 reached End-of-Life in December 2019 and has unpatched, publicly documented CVEs.
   - `.NET Framework 4.6.1` is out of mainstream support, exposing the hosting environment to unpatched runtime-level vulnerabilities.
3. **Hardcoded Database Strings & Plaintext Assets**:
   - `Web.config` and `appsettings.json` store database connection strings in plaintext configuration blocks without secret management integration [^16] [^37].
4. **Missing Modern Transport & Header Protections**:
   - The application lacks HTTP Strict Transport Security (HSTS) enforcement, Content Security Policy (CSP) headers, and modern cross-site scripting/request forgery middleware integration throughout its API endpoints.

---

#### Cloud Readiness Assessment for Google Cloud Run

To function effectively on Google Cloud Run, workloads must conform to cloud-native container standards: stateless execution, container port binding, rapid initialization, and externalized state.

| Cloud-Native Principle | Current Codebase State | Alignment / Readiness Gap |
| :--- | :--- | :--- |
| **Stateless Execution** | Session state stored in-process (`mode="InProc"`) [^52]; catalog images written/read from local disk paths [^12] [^45]. | **Non-Compliant**. In-memory sessions and local file storage break when requests hit dynamic container replicas. |
| **Container Port Binding** | Hardcoded IIS Express port configurations (`52637`, `52429`) [^53] [^54]. | **Non-Compliant**. Cloud Run requires listening dynamically on the port specified by the `PORT` environment variable (default 8080). |
| **Fast Startup / Cold Starts** | `CatalogDBInitializer` executes database sequence scripts, reads CSV files, and unzips static pictures on boot [^50]. | **Poor**. Heavy disk I/O and synchronous database queries during boot significantly increase cold-start latency. |
| **Health Checks & Observability** | Uses legacy `log4net` rolling file appenders (`logFiles\myapp.log`) [^55] and legacy Application Insights modules [^56]. | **Non-Compliant**. File appenders fail or vanish in read-only/ephemeral container runtimes. Logging must output directly to `stdout`/`stderr` for Cloud Logging ingestion. |
| **Configuration Injection** | Rigid XML `Web.config` and local JSON files [^15] [^37]. | **Non-Compliant**. Requires 12-factor configuration support via environment variables and Secret Manager injection. |

---

#### Modernization Blockers & Priority Matrix

The modernization challenges are categorized and prioritized below based on their severity and impact on migrating to **.NET 10 on Linux on Google Cloud Run**:

| Priority | Modernization Blocker | Primary Affected Components | Technical Impact |
| :--- | :--- | :--- | :--- |
| **P0 - Critical** | **`BinaryFormatter` Incompatibility** | `eShopLegacy.Utilities/Serializing.cs` [^27], `FilesController` [^51] | .NET 10 permanently removes `BinaryFormatter`. Compilation and execution will fail immediately. |
| **P0 - Critical** | **System.Web / IIS Hosting Lock** | `eShopLegacyMVC` (`Global.asax.cs`, `Web.config`, MVC/WebApi controllers) [^2] [^11] [^15] | Prevents cross-platform execution on Linux and execution outside of Windows IIS. |
| **P0 - Critical** | **LocalDB & Windows SQL Semantics** | `Web.config`, `appsettings.json`, `CatalogDBContext` [^16] [^37] | Linux containers cannot attach to or resolve Windows SQL Server LocalDB instances. |
| **P1 - High** | **Local Disk & Ephemeral File Dependencies** | `PicController.cs` [^12], `CatalogDBInitializer.cs` [^45] | Breaks stateless container scaling on Google Cloud Run. |
| **P1 - High** | **Synchronous Thread-Blocking I/O** | `CatalogService.cs` [^46], `CatalogController.cs` [^47] | Causes thread pool starvation and poor request throughput under Cloud Run autoscaling. |
| **P1 - High** | **Legacy Logging and In-Proc Sessions** | `log4Net.xml` [^55], `Global.asax.cs` [^21] [^22], `Web.config` [^52] | File logging breaks in ephemeral containers; in-process session state cannot scale horizontally. |
| **P2 - Medium** | **Abandoned Partial Migration Divergence** | `eShopPorted` (.NET Framework 4.6.1 / ASP.NET Core 2.2) [^5] | Codebase redundancy and technical debt; must reconcile duplicate services into a single clean .NET 10 project. |
| **P2 - Medium** | **Heavy File-Based Database Initializer** | `CatalogDBInitializer.cs` [^50] | Adversely impacts container boot times and introduces cold start delays in serverless environments. |

---

#### Footnotes

[^1]: `eShopLegacyMVC.sln`: Project definitions - Solution references `eShopLegacyMVC`, `eShopPorted`, and `eShopLegacy.Utilities`.
[^2]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `<TargetFrameworkVersion>` - Specifies target framework as `v4.7.2`.
[^3]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `EntityFramework PackageReference` - References EntityFramework version 6.2.0.
[^4]: `src/eShopLegacyMVC/Web.config`: `<system.web>` and `<system.webServer>` - Configures ASP.NET 4.7.2 compilation, HTTP modules, and handlers.
[^5]: `eShopPorted/eShopPorted.csproj`: `<TargetFramework>` and EF Core dependencies - Targets `net461` while referencing `Microsoft.AspNetCore.Mvc` 2.2.0 and `Microsoft.EntityFrameworkCore` 2.2.6.
[^6]: `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj`: `<TargetFrameworkVersion>` - Defines library targeting `v4.6.1`.
[^7]: `src/eShopLegacyMVC/Controllers/CatalogController.cs`: `CatalogController` definition - Inherits from legacy `System.Web.Mvc.Controller`.
[^8]: `src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs`: `BrandsController` definition - Inherits from legacy `System.Web.Http.ApiController`.
[^9]: `src/eShopLegacyMVC/Controllers/WebApi/FilesController.cs`: `FilesController` definition - Inherits from legacy `System.Web.Http.ApiController`.
[^10]: `src/eShopLegacyMVC/App_Start/BundleConfig.cs`: `RegisterBundles()` - Implements legacy script and style bundling via `System.Web.Optimization`.
[^11]: `src/eShopLegacyMVC/Global.asax.cs`: `MvcApplication` class - Inherits from `System.Web.HttpApplication` handling classic ASP.NET lifecycle events.
[^12]: `src/eShopLegacyMVC/Controllers/PicController.cs`: `Index()` - Uses `Server.MapPath("~/Pics")` to resolve physical paths.
[^13]: `src/eShopLegacyMVC/Global.asax.cs`: `Session_Start()` - Accesses `HttpContext.Current.Session` directly.
[^14]: `src/eShopLegacyMVC/App_Start/FilterConfig.cs`: `RegisterGlobalFilters()` - Registers legacy `HandleErrorAttribute` filter.
[^15]: `src/eShopLegacyMVC/Web.config`: XML Root configuration - Defines .NET Framework configuration schema.
[^16]: `src/eShopLegacyMVC/Web.config`: `<connectionStrings>` - Defines `CatalogDBContext` using local Windows SQL Server LocalDB.
[^17]: `src/eShopLegacyMVC/Web.config`: `<appSettings>` - Stores `UseMockData` and `UseCustomizationData` key-values.
[^18]: `src/eShopLegacyMVC/Web.config`: `<system.webServer>/<modules>` - Configures IIS HTTP modules including asynchronous session state.
[^19]: `src/eShopLegacyMVC/Web.config`: `<system.webServer>/<handlers>` - Maps routes to `System.Web.Handlers.TransferRequestHandler`.
[^20]: `src/eShopLegacyMVC/Global.asax.cs`: `Application_Start()` - Configures Autofac, routes, bundles, and EF database initializers.
[^21]: `src/eShopLegacyMVC/Global.asax.cs`: `Session_Start()` - Direct manipulation of `HttpContext.Current.Session`.
[^22]: `src/eShopLegacyMVC/Global.asax.cs`: `Application_BeginRequest()` - Injects correlation identifiers into `LogicalThreadContext`.
[^23]: `eShopPorted/eShopPorted.csproj`: `Microsoft.EntityFrameworkCore` dependency - References version 2.2.6.
[^24]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: Autofac integrations - References `Autofac.Mvc5` (4.0.2) and `autofac.webapi2` (6.0.1).
[^25]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `Microsoft.AspNet.Mvc` - References legacy MVC version 5.2.7.
[^26]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `Microsoft.AspNet.WebApi` - References legacy Web API version 5.2.7.
[^27]: `eShopLegacy.Utilities/Serializing.cs`: `SerializeBinary()` and `DeserializeBinary()` - Employs `BinaryFormatter` for object graph serialization.
[^28]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `log4net` reference - References log4net version 2.0.10.
[^29]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: Application Insights references - References `Microsoft.ApplicationInsights.Web` version 2.9.1.
[^30]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `<ProjectTypeGuids>` - Contains legacy Visual Studio Web Application project GUIDs.
[^31]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `<Compile Include="...">` and `<Content Include="...">` - Explicitly tracks all project files.
[^32]: `src/eShopLegacyMVC/packages.config`: Package definitions - XML listing of installed NuGet packages.
[^33]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: Inline `<PackageReference>` - Hybrid project tracking containing PackageReference elements.
[^34]: `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj`: Project structure - Traditional non-SDK MSBuild project file.
[^35]: `eShopPorted/eShopPorted.csproj`: `<TargetFramework>` - Specifies `net461`.
[^36]: `eShopPorted/eShopPorted.csproj`: `<PackageReference Include="Microsoft.AspNetCore" Version="2.2.0" />` - Uses obsolete ASP.NET Core 2.2 package.
[^37]: `eShopPorted/appsettings.json`: `DefaultConnection` - Targets `(localdb)\\mssqllocaldb`.
[^38]: `src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs`: Path strings - Hardcodes `Models\Infrastructure\dbo.catalog_hilo.Sequence.sql` using Windows backslashes.
[^39]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: Content items - Defines files with mixed case names (`Pics`, `log4Net.xml`).
[^40]: `src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs`: `HostingEnvironment.ApplicationPhysicalPath` - Uses IIS physical path resolution.
[^41]: `src/eShopLegacyMVC/ApplicationInsights.config`: `PerformanceCollectorModule` - Configures Win32 and IIS CLR performance counters.
[^42]: `eShopPorted/Controllers/CatalogController.cs`: `CatalogController` - ASP.NET Core controller duplicating legacy `CatalogController.cs`.
[^43]: `eShopPorted/Models/CatalogItem.cs`: `CatalogItem` - Replicates legacy model class in ported namespace.
[^44]: `eShopPorted/Controllers/PicController.cs`: `Index()` - Reads file buffers directly from disk via `System.IO.File.ReadAllBytes`.
[^45]: `src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs`: `AddCatalogItemPictures()` - Unzips pictures into local physical directory.
[^46]: `src/eShopLegacyMVC/Services/CatalogService.cs`: `GetCatalogItemsPaginated()` - Synchronously calls `.LongCount()` and `.ToList()`.
[^47]: `src/eShopLegacyMVC/Controllers/CatalogController.cs`: `Index()` - Synchronously waits on service execution and view model preparation.
[^48]: `src/eShopLegacyMVC/Models/CatalogItemHiLoGenerator.cs`: `GetNextSequenceValue()` - Employs in-memory `lock (sequenceLock)` for Hi/Lo ID allocations.
[^49]: `src/eShopLegacyMVC/Controllers/PicController.cs`: `System.IO.File.ReadAllBytes()` - Synchronous read of image files on every HTTP request.
[^50]: `src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs`: `Seed()` - Sequentially executes scripts, parses CSV files, and unzips archives.
[^51]: `eShopPorted/Controllers/Api/FilesController.cs`: `Index()` - Serializes brand DTOs using `Serializing.SerializeBinary()`.
[^52]: `src/eShopLegacyMVC/Web.config`: `<sessionState mode="InProc" />` - Configures memory-bound session state.
[^53]: `eShopPorted/Properties/launchSettings.json`: `iisSettings/iisExpress` - Binds application to port 52637.
[^54]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `<IISUrl>` - Binds development server to port 52429.
[^55]: `src/eShopLegacyMVC/log4Net.xml`: `<appender name="file">` - Configures local file logging to `logFiles\myapp.log`.
[^56]: `src/eShopLegacyMVC/ApplicationInsights.config`: Telemetry modules - Employs classic IIS-bound telemetry handlers and tracking modules.

### Modernization Opportunities
#### Strategic Value of Cloud Modernization

The analyzed codebase represents an e-commerce catalog management system in a transitional state. It currently contains two primary implementations: the legacy ASP.NET MVC 5 application targeting .NET Framework 4.7.2 [^1], and a partially completed port (`eShopPorted`) targeting ASP.NET Core 2.2 running on top of the legacy .NET Framework 4.6.1 runtime [^2]. 

Continuing to run this application on Windows Virtual Machines or IIS imposes substantial operational challenges:
- **Windows Licensing and Compute Overhead:** Both projects currently rely on Windows-specific dependencies and .NET Framework libraries, requiring Windows Server licenses and preventing containerization on lightweight Linux infrastructure.
- **Statelessness and Scaling Constraints:** In-process session state tracking [^3] and local filesystem persistence for catalog product images [^4] restrict horizontal scalability, preventing instances from spinning down during periods of inactivity.
- **Unsupported and Insecure Runtime Dependencies:** The codebase utilizes legacy frameworks that have reached end-of-life (such as ASP.NET Core 2.2 and .NET Framework 4.6.1) along with obsolete, vulnerable mechanisms such as `BinaryFormatter` [^5].

Migrating directly to **.NET 10 on Linux** and deploying as a **single containerized service on Google Cloud Run** resolves these architectural limitations. Cloud Run provides a serverless, fully managed execution environment that eliminates operating system maintenance, supports automated request-based scaling (including scale-to-zero), and significantly lowers infrastructure and licensing costs.

---

#### Target Cloud Architecture on Google Cloud

In accordance with architectural requirements, the system will remain a **single unified monolith** without microservice decomposition or asynchronous message broker overhead. The target architecture leverages managed Google Cloud services to fulfill storage, database, security, and observability requirements.

```
                      +-----------------------------------+
                      |   Cloud Load Balancing & Armor    |
                      +-----------------+-----------------+
                                        | HTTPS
                                        v
                      +-----------------------------------+
                      |      Google Cloud Run Service     |
                      |   (.NET 10 Monolith on Linux)     |
                      +-------+-------------------+-------+
                              |                   |
            Direct Blob URLs /|                   | Private VPC
            Cloud Storage APIs|                   | Connection
                              v                   v
                    +-------------------+  +---------------------+
                    |   Cloud Storage   |  |   Cloud SQL for     |
                    | (Catalog Images)  |  |    SQL Server       |
                    +-------------------+  +---------------------+
```

##### Architectural Mapping

| Legacy Component | Source Code Implementation | Modernized Target (.NET 10 / Google Cloud) |
| :--- | :--- | :--- |
| **Compute / Host** | Windows IIS / IIS Express (`launchSettings.json` [^6], `eShopLegacyMVC.csproj` [^1]) | **Google Cloud Run** running a containerized .NET 10 Linux application. |
| **Relational Database** | LocalDB SQL Server (`Web.config` [^7], `appsettings.json` [^8]) | **Cloud SQL for SQL Server** accessed securely through a Serverless VPC Access connector. |
| **Binary/Media Assets** | Local disk storage (`PicController.cs` [^4], `Setup/CatalogItems.zip` [^9]) | **Google Cloud Storage** bucket serving public or CDN-cached catalog images. |
| **Logging & Diagnostics** | log4net `RollingFileAppender` [^10] & Application Insights [^11] | **Google Cloud Logging** and **Google Cloud Trace** via console standard out (stdout) and OpenTelemetry. |
| **Secrets & Config** | Plaintext `Web.config` [^7] and `appsettings.json` [^8] | **Google Secret Manager** integrated via environment variables or .NET configuration providers. |
| **Serialization** | `BinaryFormatter` in `Serializing.cs` [^5] | Modern JSON serialization via `System.Text.Json`. |

---

#### Service Integrations and Refactoring Requirements

##### Compute: Google Cloud Run on Linux

Deploying the modernized application to Cloud Run as a single service requires finishing the migration to modern ASP.NET Core (.NET 10) and packaging the application as a Linux-based OCI container.

- **Stateless Execution:** In the legacy application, session tracking stores the machine name and session start time in memory [^3]. To allow Cloud Run to scale from zero to multiple instances dynamically, in-memory session requirements must either be eliminated or replaced with client-side JWT cookies.
- **Port Binding and Health Checks:** ASP.NET Core in .NET 10 automatically respects the `PORT` environment variable injected by Cloud Run (defaulting to 8080). Native health checks configured in ASP.NET Core (`MapHealthChecks("/healthz")`) allow Cloud Run startup and liveness probes to monitor instance health without IIS process dependencies.
- **Dependency Injection Standardization:** The codebase currently uses Autofac (`ApplicationModule.cs` [^12], `Startup.cs` [^13]). While Autofac is supported in .NET Core, standardizing on the native `Microsoft.Extensions.DependencyInjection` in .NET 10 reduces cold-start times and overhead on Cloud Run.

##### Database: Cloud SQL for SQL Server

The application interacts with SQL Server through Entity Framework Core in `eShopPorted` [^14] and Entity Framework 6 in `eShopLegacyMVC` [^15].

- **Connection Management:** The legacy connection string targets `(localdb)\MSSQLLocalDB` with integrated security [^7]. For Cloud Run, this must transition to a standard SQL authentication connection string pointing to Cloud SQL via the Cloud SQL Auth Proxy or a private IP on a Virtual Private Cloud (VPC) network.
- **HiLo Sequence Generation:** The legacy system relies on custom SQL sequences (`catalog_hilo`, `catalog_brand_hilo`, `catalog_type_hilo`) with raw SQL queries to manage identity values [^16]. Because Cloud SQL for SQL Server supports standard SQL Server sequence objects, the EF Core mapping (`CatalogItemConfig.cs` [^17]) can utilize native `.UseHiLo()` sequences supported directly by the `Microsoft.EntityFrameworkCore.SqlServer` provider in .NET 10.
- **Database Seeding and Migrations:** `CatalogDBInitializer.cs` in the legacy application programmatically inspects physical application paths for CSV and ZIP files to seed database data on startup [^18]. In a containerized Cloud Run environment, seeding scripts should be executed as a controlled build-time migration or deployment job, rather than parsing local directories at runtime.

##### Storage: Google Cloud Storage for Catalog Media

Product pictures are currently loaded and served through controller actions that read directly from local disk files (`Server.MapPath("~/Pics")` in `PicController.cs` [^4]).

```csharp
// Current implementation in PicController.cs
var webRoot = Server.MapPath("~/Pics");
var path = Path.Combine(webRoot, item.PictureFileName);
var buffer = System.IO.File.ReadAllBytes(path);
return File(buffer, mimetype);
```

- **Ephemeral Storage Barrier:** Cloud Run instances have an in-memory, ephemeral filesystem. Writing or reading images from local disk leads to data loss upon instance recycling and memory bloat.
- **Cloud Storage Integration:** Images should be moved from `src/eShopLegacyMVC/Pics/` [^19] into a dedicated Google Cloud Storage bucket.
- **Asset Serving Strategy:** Rather than streaming file buffers through the .NET controller pipeline (which consumes Cloud Run CPU and memory), the `PictureUri` property [^20] can directly point to public Cloud Storage URLs or Cloud CDN endpoints, reducing web application load and lowering egress latency.

##### Security and Serialization Modernization

- **Elimination of `BinaryFormatter`:** The utility library `eShopLegacy.Utilities` contains a `Serializing` class that invokes `BinaryFormatter.Serialize()` and `BinaryFormatter.Deserialize()` [^5], exposed via `FilesController.cs` [^21]. `BinaryFormatter` is notoriously vulnerable to arbitrary code execution attacks and is disabled by default in modern .NET releases (and completely removed/blocked in .NET 9 and .NET 10). This must be refactored to `System.Text.Json` using strongly typed DTOs.
- **Google Secret Manager:** Database passwords and sensitive connection parameters must be extracted from `appsettings.json` [^8] and stored in Secret Manager, mounted directly as environment variables into Cloud Run.
- **Google Cloud Armor:** Deploying Google Cloud Armor at the external Cloud Load Balancer level protects the single Cloud Run entrypoint against layer 7 DDoS attacks, SQL injection attempts, and cross-site scripting (XSS), shielding MVC forms such as `Create.cshtml` [^22] and `Edit.cshtml` [^23].

---

#### Code Refactoring and Transformation Plan

The following table summarizes the mandatory refactoring actions to transition the codebase into a modern, production-grade .NET 10 single service:

| File / Component | Current Code Pattern | Target Modernization Refactoring |
| :--- | :--- | :--- |
| `eShopPorted.csproj` [^2] | `<TargetFramework>net461</TargetFramework>` with ASP.NET Core 2.2 references | Update to `<TargetFramework>net10.0</TargetFramework>`, upgrade all EF Core and ASP.NET Core packages to version 10.x. |
| `eShopLegacy.Utilities/Serializing.cs` [^5] | `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter` | Rewrite using `System.Text.Json.JsonSerializer` to prevent security vulnerabilities and ensure compatibility with .NET 10. |
| `eShopPorted/Controllers/PicController.cs` [^24] | Controller-based local file reads (`System.IO.File.ReadAllBytes`) | Deprecate local disk streaming; update `AddUriPlaceHolder()` [^20] to generate URLs pointing to Google Cloud Storage. |
| `src/eShopLegacyMVC/log4Net.xml` [^10] | `RollingFileAppender` writing to `logFiles\myapp.log` | Remove log4net file appender; configure standard ASP.NET Core Console Logger to write structured JSON to stdout for Cloud Logging. |
| `src/eShopLegacyMVC/Global.asax.cs` [^3] | `Session_Start` storing `MachineName` and `SessionStartTime` | Remove in-proc session dependencies; pass instance identity via standard Cloud Run metadata headers if needed. |
| `src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs` [^18] | Hardcoded CSV and ZIP file extraction from local filesystem path | Replace with EF Core migrations (`HasData` seed data in model builders or separate Cloud Run deployment jobs). |

---

#### Concrete Operational and Business Benefits

1. **Zero Operating System Maintenance:** By shifting from Windows Server IIS to Google Cloud Run, development teams eliminate Windows patch cycles, OS-level security remediation, and IIS configuration drift.
2. **Optimized Cost Model:** Cloud Run bills strictly per request down to the millisecond. Combined with scale-to-zero capabilities for non-production and off-peak environments, this drastically cuts resource expenditures compared to continually running Windows VMs.
3. **Faster Feature Deployment:** Packaging the unified monolith as a standard Linux container enables rapid CI/CD deployment pipelines using Google Cloud Build and Artifact Registry, allowing blue/green and canary traffic splitting natively without downtime.
4. **Enhanced Resilience:** Running in Cloud Run with a managed Cloud SQL backend provides built-in health checking, automated container restart, regional fault isolation, and managed database backups with minimal operational overhead.

[^1]: src/eShopLegacyMVC/eShopLegacyMVC.csproj: `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` - Specifies the legacy .NET Framework 4.7.2 target.
[^2]: eShopPorted/eShopPorted.csproj: `<TargetFramework>net461</TargetFramework>` - Shows ASP.NET Core 2.2 ported application still tied to .NET Framework 4.6.1.
[^3]: src/eShopLegacyMVC/Global.asax.cs: Session_Start() - Captures MachineName and start time into in-memory session.
[^4]: src/eShopLegacyMVC/Controllers/PicController.cs: Index() - Loads product image bytes from local disk using Server.MapPath.
[^5]: eShopLegacy.Utilities/Serializing.cs: SerializeBinary() - Implements binary serialization using BinaryFormatter.
[^6]: eShopPorted/Properties/launchSettings.json: iisSettings - Defines local Windows IIS Express deployment profiles.
[^7]: src/eShopLegacyMVC/Web.config: `<connectionStrings>` - Defines connection string for local MSSQLLocalDB instance.
[^8]: eShopPorted/appsettings.json: ConnectionStrings:DefaultConnection - Configures localdb SQL Server connection string.
[^9]: src/eShopLegacyMVC/Setup/CatalogItems.zip: Catalog item setup archive - Local ZIP archive containing catalog pictures.
[^10]: src/eShopLegacyMVC/log4Net.xml: `<appender name="file">` - Configures log4net RollingFileAppender targeting local disk log files.
[^11]: src/eShopLegacyMVC/ApplicationInsights.config: ApplicationInsights configuration - Configures legacy Windows Server Application Insights telemetry.
[^12]: src/eShopLegacyMVC/Modules/ApplicationModule.cs: Load() - Configures Autofac DI container registrations.
[^13]: eShopPorted/Startup.cs: ConfigureServices() - Implements Autofac container builder within ASP.NET Core startup.
[^14]: eShopPorted/Models/CatalogDBContext.cs: CatalogDBContext - EF Core database context implementation.
[^15]: src/eShopLegacyMVC/Models/CatalogDBContext.cs: CatalogDBContext - Entity Framework 6 DbContext implementation.
[^16]: src/eShopLegacyMVC/Models/CatalogItemHiLoGenerator.cs: GetNextSequenceValue() - Executes raw SQL queries against catalog_hilo database sequence.
[^17]: eShopPorted/Models/Config/CatalogItemConfig.cs: Configure() - Entity Framework Core entity configuration for catalog items.
[^18]: src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs: Seed() - Seeds database by parsing local filesystem CSV files and extracting ZIP archives.
[^19]: src/eShopLegacyMVC/Pics/1.png: Catalog product image - Static image file stored on the local filesystem.
[^20]: src/eShopLegacyMVC/Controllers/CatalogController.cs: AddUriPlaceHolder() - Generates image endpoint URLs for catalog items.
[^21]: src/eShopLegacyMVC/Controllers/WebApi/FilesController.cs: Get() - Invokes Serializing class to stream binary serialized brand data.
[^22]: src/eShopLegacyMVC/Views/Catalog/Create.cshtml: Catalog item creation view - Razor HTML form for creating new catalog items.
[^23]: src/eShopLegacyMVC/Views/Catalog/Edit.cshtml: Catalog item edit view - Razor HTML form for editing catalog items.
[^24]: eShopPorted/Controllers/PicController.cs: Index() - ASP.NET Core PicController action reading image bytes from disk.

## Cloud Migration
### Migration Overview
#### Migration Assessment and Architecture Overview

The codebase consists of two primary application implementations:
- `eShopLegacyMVC`: A legacy ASP.NET MVC 5 and Web API 2 project targeting .NET Framework 4.7.2 [^1], designed to run on Windows IIS [^2] and relying on Entity Framework 6 [^3].
- `eShopPorted`: A transitional ASP.NET Core 2.2 project currently targeting `.NETFramework,Version=v4.6.1` (`net461`) [^4], relying on Entity Framework Core 2.2.6 [^5].
- `eShopLegacy.Utilities`: A supporting class library compiled against .NET Framework 4.6.1 [^6].

To establish an enterprise-ready posture in Google Cloud, two broad migration paths are evaluated:
1. **As-Is (Rehost / Replatform)**: Retains the existing .NET Framework runtime and Windows dependencies with minimal modifications.
2. **Modernization (Refactor / Rearchitect)**: Elevates the codebase to .NET 10 on Linux as a single unified service, targeting modern container-first serverless execution.

---

#### Google Cloud Compute Targets

##### Compute Engine

Compute Engine provides scalable virtual machine (VM) infrastructure supporting Windows Server and Linux operating systems.

- **Migration Path**: 
  - *As-Is Scenario*: The application (`eShopLegacyMVC` or `eShopPorted` under .NET Framework) can be deployed directly to Compute Engine Windows Server VM instances running Internet Information Services (IIS). This requires configuring IIS application pools, deploying database drivers, and migrating web applications without code alterations.
  - *Modernized Scenario*: If upgraded to .NET 10, the application can run as a systemd service on Linux VM instances (e.g., Debian or Rocky Linux), eliminating Windows licensing overhead.
- **Further Deep Dive**: Refer to the separate section **"Shift to Compute Engine"** for an in-depth analysis of virtual machine provisioning, instance templates, and Windows licensing considerations.

##### Google Kubernetes Engine (GKE)

Google Kubernetes Engine provides an automated, managed container orchestration platform.

- **Migration Path**:
  - *As-Is Scenario*: Windows Server node pools in GKE can host Windows Server Core or Nano Server container images running the .NET Framework application. However, Windows container images feature large footprints and slower autoscaling responsiveness.
  - *Modernized Scenario*: Upgraded to .NET 10 on Linux, the service can be packaged into lightweight, secure container images based on Debian/Alpine minimal bases. The workload is managed via standard GKE deployments with Pod horizontal autoscaling.
- **Further Deep Dive**: Refer to the separate section **"Modernize to Kubernetes Engine"** for detailed architectural diagrams, pod configuration, and ingress management.

##### Cloud Run and Cloud Run Functions

Cloud Run provides a fully managed, stateless container runtime that automatically scales instances from zero to handle HTTP workloads.

- **Migration Path**:
  - *As-Is Scenario*: Cloud Run does not support Windows Server containers or full .NET Framework binaries. Therefore, migrating the legacy .NET Framework runtime directly to Cloud Run without code modifications is not viable.
  - *Modernized Scenario (Selected Primary Architecture)*: Modernizing the application to .NET 10 on Linux enables it to run directly on Cloud Run as a single, containerized monolithic service. Cloud Run fulfills the deployment goal by providing built-in TLS, dynamic request routing, concurrency handling, and automatic scale-to-zero capabilities with no infrastructure maintenance. Cloud Run Functions is unnecessary as the application operates as a single cohesive web service.
- **Further Deep Dive**: Refer to the separate section **"Modernize to Cloud Run"** for detailed container specifications, memory/CPU tuning, and concurrency configurations.

---

#### External Dependencies and Managed Services

The application interacts with several local and infrastructure-level external dependencies:

| Existing Dependency | Current Implementation | Google Cloud Strategy | Target Modernization Alternative |
| :--- | :--- | :--- | :--- |
| **Relational Database** | Microsoft SQL Server (LocalDB / SQL Express / Full SQL Server) [^7] | **Cloud SQL for SQL Server** | Provision Cloud SQL for SQL Server to retain existing EF / EF Core schemas without breaking T-SQL conventions. |
| **Catalog Image Storage** | Local File System (`~/Pics`, `wwwroot/Pics`) [^8] [^9] | **Google Cloud Storage (GCS)** | Offload static image assets to a Cloud Storage bucket, serving them either directly via public URLs or through Cloud CDN. |
| **Application Telemetry** | Microsoft Application Insights [^10] | **Google Cloud Observability** | Replace Application Insights with Google Cloud Logging and Google Cloud Monitoring using OpenTelemetry / Google Cloud .NET SDKs. |
| **Diagnostic Logging** | `log4net` file-based rolling logging (`myapp.log`) [^11] | **Google Cloud Logging** | Output structured JSON directly to `stdout`/`stderr`, captured automatically by Google Cloud Logging agents and Cloud Run runtime. |
| **Session Management** | In-Process ASP.NET Session State [^12] | **Memorystore for Redis** | Eliminate in-memory sticky sessions in favor of stateless authentication or externalized session caching via Memorystore for Redis. |
| **Object Serialization** | BinaryFormatter (`Serializing.cs`) [^13] | **Native JSON Serialization** | Replace insecure, deprecated .NET `BinaryFormatter` calls [^14] with `System.Text.Json`. |

---

#### Integration with Google Cloud Horizontals

Migrating to Google Cloud unlocks access to managed operational and infrastructure horizontals:

- **CI/CD Pipelines**: Automated container builds and continuous deployments are orchestrated using **Cloud Build**. Base container images are tagged, scanned for vulnerabilities, and stored in **Artifact Registry**.
- **Observability**: Standardized telemetry using **Google Cloud Logging**, **Google Cloud Monitoring**, and **Cloud Trace** replaces fragmented file logs and legacy Application Insights agents, providing centralized dashboards and alert policies.
- **Networking and Edge Protection**: Incoming web traffic routes through **Cloud Load Balancing** integrated with **Cloud Armor** for Web Application Firewall (WAF) defense and DDoS mitigation, with optional **Cloud CDN** caching for static catalog imagery.
- **Secrets Management**: Database connection credentials and application keys are decoupled from XML/JSON config files [^15] [^16] and retrieved dynamically using **Secret Manager**.
- **Internal Networking**: Database connectivity between Cloud Run and Cloud SQL is secured inside VPC boundaries using **Serverless VPC Access** or private service connect, avoiding public IP exposure.

---

#### Modernization Strategies

```
+---------------------------------------------------------------------------------------+
|                                MODERNIZATION STRATEGIES                                |
+---------------------------------------------------------------------------------------+
|  Strategy 1: Low-Effort (Lift and Shift)                                              |
|  [ eShopLegacyMVC / eShopPorted (net461/net472) ]                                     |
|                        │                                                              |
|                        ▼                                                              |
|      Google Cloud Compute Engine (Windows Server / IIS)                               |
|                        │                                                              |
|                        ▼                                                              |
|      Google Cloud SQL for SQL Server                                                  |
+---------------------------------------------------------------------------------------+
|  Strategy 2: High-Benefit (Modernize)                                                 |
|  [ Modernize to .NET 10 on Linux as Single Service ]                                 |
|                        │                                                              |
|                        ▼                                                              |
|            Google Cloud Run (Container)                                               |
|           ┌────────────┴────────────┐                                                 |
|           ▼                         ▼                                                 |
|  Cloud SQL for SQL Server     Google Cloud Storage (Catalog Media)                   |
+---------------------------------------------------------------------------------------+
```

##### Strategy 1: Low-Effort (Lift and Shift)

- **Target Architecture**: Compute Engine VM (Windows Server with IIS) connecting to Cloud SQL for SQL Server.
- **Effort Level**: Low.
- **Key Characteristics**: Preserves .NET Framework 4.7.2, Windows-specific dependencies, and in-process session state without code refactoring.

###### Migration Plan:
1. **Database Migration**: Export existing SQL schemas and data; import into a managed **Cloud SQL for SQL Server** instance.
2. **Infrastructure Provisioning**: Deploy Compute Engine Windows Server VM with IIS and .NET Framework 4.7.2 enabled. Configure VPC peering to the Cloud SQL instance.
3. **Application Deployment**: Update `Web.config` connection strings to point to Cloud SQL [^17]. Deploy IIS site bindings.
4. **Operations Setup**: Install Google Cloud Ops Agent on the VM to forward system metrics and `myapp.log` output to Google Cloud Logging.

---

##### Strategy 2: High-Benefit (Modernize to Cloud Run and .NET 10)

- **Target Architecture**: Single-service container deployed to **Cloud Run**, running **.NET 10** on **Linux**, backed by **Cloud SQL for SQL Server** and **Google Cloud Storage**.
- **Effort Level**: High initial effort; minimal ongoing operational overhead and total cost of ownership.
- **Key Characteristics**: Replaces obsolete .NET Framework APIs, migrates Entity Framework to EF Core, adopts native Linux containers, eliminates local disk dependencies, and preserves a single monolithic service architecture.

###### Migration Plan:
1. **Framework Modernization**:
   - Modernize the project structure to **.NET 10** targeting Linux (`net10.0`).
   - Standardize on `eShopPorted` code structures while consolidating shared logic from `eShopLegacy.Utilities` and `src/eShopLegacyMVC`.
   - Update Entity Framework Core to modern EF Core compatible with .NET 10.
   - Refactor `Serializing.cs` to remove `BinaryFormatter` [^14] in favor of `System.Text.Json`.
2. **Storage and State Decoupling**:
   - Refactor image loading logic in `PicController.cs` [^18] to stream or redirect catalog image assets from a private **Google Cloud Storage** bucket.
   - Remove reliance on ASP.NET in-process session variables [^19].
3. **Containerization & CI/CD**:
   - Author a multi-stage `Dockerfile` based on Microsoft's official .NET 10 runtime Linux image.
   - Configure **Cloud Build** triggers to compile, test, and publish images to **Artifact Registry**.
4. **Cloud Infrastructure Provisioning**:
   - Provision **Cloud SQL for SQL Server** with private IP connectivity.
   - Configure **Serverless VPC Access** connector.
   - Store SQL connection strings in **Secret Manager**.
5. **Deployment & Verification**:
   - Deploy the container image to **Cloud Run** with appropriate CPU/memory allocation and VPC egress rules.
   - Verify health checks, auto-scaling behavior, and Google Cloud Observability log ingestion.

---

[^1]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` - Specifies the target runtime for the legacy MVC project.
[^2]: `src/eShopLegacyMVC/Web.config`: `<system.webServer>` - Configures IIS modules, handlers, and ASP.NET runtime pipeline.
[^3]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `<PackageReference Include="EntityFramework" Version="6.2.0" />` - Uses EF6 for relational persistence.
[^4]: `eShopPorted/eShopPorted.csproj`: `<TargetFramework>net461</TargetFramework>` - Shows the ported ASP.NET Core project still tied to .NET Framework 4.6.1.
[^5]: `eShopPorted/eShopPorted.csproj`: `<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="2.2.6" />` - Specifies EF Core 2.2 SQL Server provider.
[^6]: `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj`: `<TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>` - Targets .NET Framework 4.6.1 for the shared utilities assembly.
[^7]: `eShopPorted/appsettings.json`: `"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=eShopPorted;..."` - Defines local SQL Server connection string.
[^8]: `src/eShopLegacyMVC/Controllers/PicController.cs`: `Index(int catalogItemId)` - Resolves physical local disk paths using `Server.MapPath("~/Pics")`.
[^9]: `eShopPorted/Controllers/PicController.cs`: `Index(int catalogItemId)` - Reads catalog item image bytes directly from local file disk path.
[^10]: `src/eShopLegacyMVC/ApplicationInsights.config`: `<ApplicationInsights>` - Configures telemetry initializers and performance counter collectors for Azure.
[^11]: `src/eShopLegacyMVC/log4Net.xml`: `<file value="logFiles\myapp.log" />` - Configures rolling file appender on local disk.
[^12]: `src/eShopLegacyMVC/Web.config`: `<sessionState mode="InProc" />` - Enforces stateful in-memory web session handling.
[^13]: `eShopLegacy.Utilities/Serializing.cs`: `SerializeBinary(object input)` - Uses obsolete `BinaryFormatter` for stream serialization.
[^14]: `eShopLegacy.Utilities/Serializing.cs`: `var binaryFormatter = new BinaryFormatter();` - Instantiates `BinaryFormatter`, which is obsolete and unsupported in modern .NET runtimes.
[^15]: `src/eShopLegacyMVC/Web.config`: `<connectionStrings>` - Stores cleartext SQL connection string within project configuration.
[^16]: `eShopPorted/appsettings.json`: `"ConnectionStrings"` - Stores environment connection settings in JSON deployment files.
[^17]: `src/eShopLegacyMVC/Web.Release.config`: `<connectionStrings>` - XML document transformation targets database configuration during release builds.
[^18]: `src/eShopLegacyMVC/Controllers/PicController.cs`: `System.IO.File.ReadAllBytes(path)` - Loads local picture files synchronously from the host filesystem.
[^19]: `src/eShopLegacyMVC/Global.asax.cs`: `Session_Start()` - Tracks machine execution metadata directly within server session variables.

### Shift to Compute Engine
#### Compute Engine Migration Assessment and Operating Model

The existing repository contains two implementations of the catalog application:
1. The legacy ASP.NET MVC 5 application (`eShopLegacyMVC`), which targets .NET Framework 4.7.2 and relies on IIS features, `System.Web`, ASP.NET Web API 2, and Entity Framework 6.2.0 [^1].
2. A partially migrated ASP.NET Core 2.2 application (`eShopPorted`), which targets `net461` on the .NET Framework CLR rather than .NET Core/.NET 10 [^2].

Because both active projects depend on the Windows-only .NET Framework runtime (`net472` and `net461`), migrating directly to Compute Engine with zero to minimal application code changes requires Windows Server instances running Internet Information Services (IIS).

However, retaining Windows Server VMs imposes ongoing Windows Server licensing costs, prevents native Linux execution, and acts as a barrier to container-based deployment on modern managed runtimes. In alignment with the modernization goal of transitioning to .NET 10 on Linux as a single unified service (without microservice fragmentation), Compute Engine serves either as:
- **Phase 1: Minimal-Effort Rehosting (IaaS Lift-and-Shift):** Deploying the application onto Windows Server Compute Engine instances running IIS to rapidly establish a Google Cloud footprint.
- **Phase 2: Linux Transition on Compute Engine:** Upgrading the single-service ASP.NET Core project to .NET 10, cross-compiling for Linux, and deploying directly onto Compute Engine Linux VMs (e.g., Debian or Ubuntu) with Kestrel, systemd, and Nginx before containerizing for Cloud Run.

##### Minimal Changes Rehosting Strategy (Windows Server on Compute Engine)

To achieve a migration with minimal code alterations:
- **Virtual Machine Configuration:** Compute Engine VMs are provisioned with Google Cloud Windows Server images (such as `windows-server-2022-dc-v20240110`).
- **Web Server Configuration:** IIS 10 is enabled via PowerShell DSC or startup scripts, configuring ASP.NET 4.7, HTTP redirection, and ASP.NET request handlers [^3].
- **Application Deployment:** The legacy compiled package (`eShopLegacyMVC`) is deployed to IIS under `Default Web Site` or an isolated application pool.
- **Data Source Transition:** The local LocalDB instance (`(localdb)\MSSQLLocalDB`) defined in `Web.config` is replaced with an external connection string pointing to a managed database [^4].

| Attribute | Lift-and-Shift (Windows Server) | Modernized (Linux / .NET 10) |
| :--- | :--- | :--- |
| **Compute Engine OS** | Windows Server 2019 / 2022 Datacenter | Debian 12 / Ubuntu 24.04 LTS / Container-Optimized OS |
| **Runtime Target** | .NET Framework 4.7.2 / IIS [^1] | .NET 10 Kestrel / systemd / Linux Container |
| **Code Changes Required** | Configuration changes only (`Web.config` connection strings) | Retarget framework, update EF Core, eliminate `BinaryFormatter` [^5] |
| **Licensing Overhead** | Windows Server OS licensing applies | Zero OS licensing fees (Open Source Linux) |
| **Cloud Run Readiness** | Not compatible (Windows dependency) | 100% compatible with Cloud Run and container runtimes |

---

#### External Dependencies and Google Cloud Managed Services

The application codebase depends on several platform and infrastructure components. The table below outlines how each dependency maps to Google Cloud native services or Compute Engine equivalents with minimal refactoring.

| Existing Dependency | Current Implementation | Google Cloud Strategy | Integration Approach |
| :--- | :--- | :--- | :--- |
| **Relational Database** | Microsoft SQL Server (LocalDB) via EF 6 / EF Core 2.2 [^4] [^6] | **Cloud SQL for SQL Server** | Provision Cloud SQL for SQL Server (Web or Standard edition). Cloud SQL natively supports T-SQL sequences like `catalog_hilo` [^7], schema objects, and SQL Server collation. Update the `CatalogDBContext` connection string without altering data-access logic. |
| **Static Product Images** | Local file system storage (`/Pics`) served via `PicController` [^8] [^9] | **Google Cloud Storage (GCS)** or **Compute Engine Persistent Disk** | For minimal changes, retain image assets on a dedicated Compute Engine Persistent Disk mounted to IIS. For target cloud alignment, offload static images to a private Google Cloud Storage bucket served via a Cloud CDN backend or private URLs. |
| **Application Telemetry** | Microsoft Application Insights [^10] | **Google Cloud Monitoring & Cloud Trace** | Replace `ApplicationInsights.config` and `Microsoft.ApplicationInsights` libraries with Google Cloud Logging and Monitoring .NET packages (`Google.Cloud.Diagnostics.AspNetCore` or `Google.Cloud.Diagnostics.Common`). |
| **Application Logging** | `log4net` rolling file appender targeting `logFiles\myapp.log` [^11] | **Cloud Logging** | For minimal changes, install the Google Cloud Ops Agent on the Compute Engine VM to tail `logFiles\myapp.log`. In .NET 10, re-route standard logging directly to stdout/stderr or Google Cloud Logging sink. |
| **Serialization** | `BinaryFormatter` in `eShopLegacy.Utilities` [^5] | **`System.Text.Json`** | `BinaryFormatter` has no cloud equivalent and presents security vulnerabilities; it is obsolete and unsupported in modern .NET runtimes. Replace binary streams with JSON serialization. |
| **Session State** | `InProc` session state storing host machine name and start time [^3] [^12] | **Stateless Execution** or **Memorystore for Redis** | The current session state stores ephemeral diagnostics [^12]. If maintaining a single VM instance, `InProc` functions as-is. For multiple load-balanced instances, remove the state or adopt Cloud Memorystore for Redis. |

##### Managed Database Transition: Cloud SQL for SQL Server

The legacy application relies on custom T-SQL sequence scripts (`dbo.catalog_hilo.Sequence.sql`, `dbo.catalog_brand_hilo.Sequence.sql`, and `dbo.catalog_type_hilo.Sequence.sql`) and SQL queries executing `SELECT NEXT VALUE FOR catalog_hilo` [^7] [^13]. 

Using **Cloud SQL for SQL Server** provides 100% T-SQL compatibility:
- Eliminates administrative database operations (automated backups, patch management, replication, and storage auto-expansion).
- Connects securely from the Compute Engine instance via private IP inside a Google Cloud Virtual Private Cloud (VPC) network, eliminating public IP exposure.
- Requires only updating the `connectionStrings` section in `Web.config` or `appsettings.json` [^4] [^6]:

```xml
<connectionStrings>
  <add name="CatalogDBContext" 
       connectionString="Server=10.x.x.x;Database=CatalogDb;User Id=catalog_app;Password=******;TrustServerCertificate=True;MultipleActiveResultSets=True;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

---

#### Leveraging Google Cloud Horizontals

Migrating to Compute Engine enables seamless integration with core Google Cloud foundational platform features without rearchitecting the monolith.

##### Networking and Security
- **Virtual Private Cloud (VPC):** Provision a dedicated VPC with private subnets for Compute Engine instances and Cloud SQL.
- **Cloud NAT:** Configured on the VPC router so that VMs without public external IP addresses can download packages, operating system security patches, and dependencies securely.
- **Google Cloud Armor & External HTTP(S) Load Balancing:** Place an External Application Load Balancer in front of the Compute Engine instances. Configure Google Cloud Armor security policies to protect against layer 7 denial-of-service (DDoS) and common web vulnerabilities (OWASP Top 10).
- **Google Cloud Managed SSL Certificates:** Terminate TLS at the load balancer automatically without needing to install and renew SSL certificates inside Windows IIS.

##### Logging, Diagnostics, and Monitoring
- **Google Cloud Ops Agent:** Installed on each Compute Engine VM (Windows or Linux) to collect performance metrics (CPU, memory, disk, network) and stream files matching `logFiles\myapp.log` directly into Cloud Logging [^11].
- **Cloud Monitoring Dashboards:** Set up alerting policies based on IIS request queues, ASP.NET error rates, system memory consumption, and HTTP 5xx responses.
- **Cloud Trace:** Correlate request lifecycles using correlation IDs already initialized by `ActivityIdHelper` in `Global.asax.cs` [^12].

##### CI/CD and Secret Management
- **Cloud Build:** Automate build processes. For minimal lift-and-shift, Cloud Build executes MSBuild steps in Windows container worker pools. For modern .NET 10 builds, standard Linux worker pools build and publish artifacts.
- **Secret Manager:** Store sensitive database connection strings, credentials, and API keys securely in Secret Manager rather than unencrypted configuration files (`Web.config` or `appsettings.json`). At startup, PowerShell scripts on Compute Engine retrieve secrets using VM service accounts authenticated via Instance Metadata.

---

#### Detailed Migration Plan to Compute Engine

This step-by-step implementation guide provides the fastest and lowest-risk execution path to rehost the application on Compute Engine.

##### Phase 1: Environment Provisioning and Networking Setup
1. **Configure VPC and Subnets:**
   - Establish a dedicated VPC (`eshop-vpc`) with a private subnet (`10.10.10.0/24`).
   - Configure a Cloud NAT gateway to allow outbound access for updates without assigning external public IPs to backend VM instances.
2. **Deploy Cloud SQL for SQL Server:**
   - Create a Cloud SQL for SQL Server instance (e.g., SQL Server 2019/2022 Standard) configured with a Private IP connected to `eshop-vpc`.
   - Execute the schema initialization and seeding scripts from `CatalogDBInitializer.cs` and the Hi-Lo sequence definition files (`dbo.catalog_hilo.Sequence.sql`, `dbo.catalog_brand_hilo.Sequence.sql`, `dbo.catalog_type_hilo.Sequence.sql`) [^7] [^13].
3. **Provision Compute Engine Instance:**
   - Deploy an `e2-standard-4` or `n2-standard-4` VM instance utilizing the `windows-server-2022-dc` public image family in the private subnet.
   - Attach a service account with minimal IAM roles (`roles/logging.logWriter`, `roles/monitoring.metricWriter`, `roles/secretmanager.secretAccessor`).

##### Phase 2: Host Preparation and Web Application Configuration
1. **Configure IIS via Startup Script:**
   - Use Compute Engine instance metadata to execute a PowerShell initialization script:
     ```powershell
     Install-WindowsFeature -name Web-Server, Web-App-Dev, Web-Asp-Net45, NET-Framework-45-ASPNET -IncludeManagementTools
     ```
2. **Install Observability Tools:**
   - Download and install the Google Cloud Ops Agent.
   - Configure `/etc/google-cloud-ops-agent/config.yaml` (or Windows equivalent path) to monitor the IIS application logs and `logFiles\myapp.log` [^11].
3. **Deploy Artifacts:**
   - Package the `eShopLegacyMVC` build outputs and static picture directory (`Pics/`) [^8].
   - Copy the assets to `C:\inetpub\eshop` on the Compute Engine instance.
   - Configure an IIS Application Pool set to .NET CLR Version `v4.0` in Integrated Pipeline mode.

##### Phase 3: Application Configuration and Validation
1. **Update Connection Configurations:**
   - Replace the database connection string in `Web.config` with the Cloud SQL private IP address and authorized credentials [^4].
   - Set application settings (`UseMockData = false`) to operate against the real database [^4].
2. **End-to-End Functional Verification:**
   - Verify health checks via the HTTP endpoint `/Catalog/Index` [^14].
   - Validate catalog listing pagination, item creation, and Hi-Lo key generation [^7] [^14].
   - Validate image rendering from `/items/{catalogItemId}/pic` [^9].
   - Verify that log statements generated by `log4net` appear within Cloud Logging Explorer [^11] [^14].

##### Phase 4: Traffic Routing and Cutover
1. **Configure External Load Balancing:**
   - Create an unmanaged Instance Group containing the Compute Engine VM.
   - Configure an External Application Load Balancer with an HTTP/HTTPS frontend, Google-managed SSL certificate, and backend service targeting IIS port 80.
   - Configure health check probes against `/Catalog/Index` [^14].
2. **DNS Cutover:**
   - Point DNS domain records to the external IP assigned to the Google Cloud Load Balancer.
   - Monitor request flow and latency metrics in Cloud Monitoring.

##### Phase 5: Modernization Runway (.NET 10 on Linux Validation)
1. **Modernize Application Monolith:**
   - Upgrade the project to .NET 10 within a single unified web project, replacing `BinaryFormatter` [^5] with `System.Text.Json` and updating Entity Framework to modern EF Core.
2. **Validate on Linux Compute Engine:**
   - Launch a Linux Compute Engine VM (Ubuntu 24.04 LTS or Container-Optimized OS).
   - Run the published .NET 10 single service via systemd or Docker container.
   - Validate performance and cross-platform compatibility without Windows licensing costs.
3. **Transition to Cloud Run:**
   - Containerize the validated .NET 10 application image.
   - Deploy directly to Google Cloud Run as a single managed container service, completing the migration to a fully serverless, auto-scaling Linux environment.

---

#### Footnotes

[^1]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` - Defines legacy .NET Framework 4.7.2 compilation for ASP.NET MVC project.
[^2]: `eShopPorted/eShopPorted.csproj`: `<TargetFramework>net461</TargetFramework>` - Defines .NET Framework 4.6.1 runtime target for ASP.NET Core project.
[^3]: `src/eShopLegacyMVC/Web.config`: `<system.webServer>` and `<modules>` - Configures IIS web server handlers, modules, and InProc session state.
[^4]: `src/eShopLegacyMVC/Web.config`: `<connectionStrings>` - Specifies default connection string to local SQL Server LocalDB.
[^5]: `eShopLegacy.Utilities/Serializing.cs`: `SerializeBinary() / DeserializeBinary()` - Legacy binary serialization using BinaryFormatter.
[^6]: `eShopPorted/appsettings.json`: `"DefaultConnection"` - Specifies ASP.NET Core connection string to local LocalDB instance.
[^7]: `src/eShopLegacyMVC/Models/CatalogItemHiLoGenerator.cs`: `GetNextSequenceValue()` - Queries `catalog_hilo` SQL Server sequence for Hi-Lo key generation.
[^8]: `src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs`: `AddCatalogItemPictures()` - Manages unpacking and serving local file system images from `/Pics`.
[^9]: `src/eShopLegacyMVC/Controllers/PicController.cs`: `Index()` - Reads image bytes directly from local server disk using `Server.MapPath`.
[^10]: `src/eShopLegacyMVC/ApplicationInsights.config`: `<ApplicationInsights>` - Configures Microsoft Application Insights telemetry collectors.
[^11]: `src/eShopLegacyMVC/log4Net.xml`: `<appender name="file">` - Configures log4net RollingFileAppender outputting to `logFiles\myapp.log`.
[^12]: `src/eShopLegacyMVC/Global.asax.cs`: `Application_BeginRequest() / Session_Start()` - Sets request-level activity identifiers and session tracking.
[^13]: `src/eShopLegacyMVC/Models/Infrastructure/dbo.catalog_hilo.Sequence.sql`: `CREATE SEQUENCE` - T-SQL script generating custom key sequence in SQL Server.
[^14]: `src/eShopLegacyMVC/Controllers/CatalogController.cs`: `Index()` - Primary MVC controller action rendering the paginated catalog table.

### Modernize to Kubernetes Engine
#### Container Modernization and Target Platform Architecture

The repository contains two main web implementations of the catalog management system: the original legacy application running on ASP.NET MVC 5 under .NET Framework 4.7.2[^1], and an intermediate migration iteration `eShopPorted` built with ASP.NET Core 2.2 targeting .NET Framework 4.6.1 (`net461`)[^2]. The solution also contains a shared class library `eShopLegacy.Utilities` targeting .NET Framework 4.6.1[^3].

Because both applications target the Windows-only .NET Framework runtime and rely on Windows-specific primitives—such as IIS-integrated request pipelines, Windows registry bindings, and Windows file paths[^4]—they cannot natively execute on Linux containers without completing runtime modernization. 

In accordance with architectural requirements, the target execution environment is **Google Cloud Run** running **.NET 10** on **Linux containers**, packaged and operated as a **single unified service** (monolith) rather than decomposed into distributed microservices.

##### Target Architecture Overview

Operating the application as a single container on Cloud Run on Linux provides numerous benefits over traditional virtual machines and heavy orchestration clusters:

*   **Serverless Scaling and Cost Efficiency**: Automatically scales down to zero when idle and scales out rapidly based on incoming HTTP request concurrency.
*   **Operating System Portability**: Migrating to Linux containers running .NET 10 eliminates Windows Server licensing fees and decreases container boot times.
*   **Operational Simplicity**: Cloud Run abstracts infrastructure, node pools, OS patching, and control plane management, allowing the engineering team to manage a single deployable artifact.
*   **Container Standards**: Although deployed to Cloud Run, packaging the application into an OCI-compliant container preserves complete portability to Google Kubernetes Engine (GKE) should future cluster-level networking or multi-tenant orchestrations become necessary.

---

#### External Dependencies and Google Cloud Managed Services

The application relies on several external components, configuration systems, and runtime libraries. Each dependency is evaluated below with its corresponding Google Cloud migration strategy.

| Dependency / Component | Current Implementation | Google Cloud Strategy | Migration Approach |
| :--- | :--- | :--- | :--- |
| **Relational Database** | Microsoft SQL Server (`(localdb)\MSSQLLocalDB`)[^5] | **Google Cloud SQL for SQL Server** | Provision a managed Cloud SQL for SQL Server instance (Web or Standard Edition). Connect securely via Cloud SQL Auth Proxy or Serverless VPC Access connector using private IP. |
| **Static Assets / Images** | Local disk storage (`~/Pics/*.png`, `wwwroot/Pics`)[^4][^6] | **Google Cloud Storage (GCS)** | Offload physical image files to a Cloud Storage bucket. Serve images directly through Cloud CDN/Storage or stream from Cloud Storage via the application service layer. |
| **Telemetry & APM** | Azure Application Insights (`ApplicationInsights.config`)[^7] | **Google Cloud Monitoring & Cloud Trace** | Remove `Microsoft.ApplicationInsights` packages[^8]. Utilize OpenTelemetry .NET SDK with Google Cloud Trace and Cloud Monitoring exporters. |
| **Application Logging** | Log4Net rolling file appender (`logFiles\myapp.log`)[^9] | **Google Cloud Logging** | Replace Log4Net or redirect `ILogger` output directly to standard output (`stdout`/`stderr`) using structured JSON format. Cloud Run natively ingests container stdout directly into Cloud Logging. |
| **Binary Serialization** | Insecure .NET `BinaryFormatter` (`Serializing.cs`)[^10] | Standard JSON / System.Text.Json | Replace `BinaryFormatter` with standard `System.Text.Json` serialization in `FilesController`[^11] to comply with security best practices and .NET 10 runtime requirements. |
| **Configuration & Secrets** | `Web.config` appSettings / `appsettings.json`[^5][^12] | **Secret Manager & Cloud Run Environment Variables** | Migrate non-sensitive configuration values to Cloud Run environment variables and store connection strings and credentials in **Google Secret Manager**, mounted directly as runtime secrets. |

---

#### Monolithic Service Architecture on Cloud Run

Per system constraints, the application will **not** be fragmented into microservices. Retaining a modular, single-service monolithic architecture avoids unnecessary distributed system complexity, inter-service network latency, data consistency synchronizations (e.g., distributed transactions or sagas), and overhead associated with managing multiple deployment pipelines.

##### Service Packaging

All application functionalities—including the server-side rendered MVC catalog views, the internal RESTful API endpoints (`BrandsController`, `FilesController`), and image handling—will remain within the consolidated ASP.NET Core (.NET 10) host[^11][^13][^14]:

```
+---------------------------------------------------------------------------------+
|                                 Google Cloud                                    |
|                                                                                 |
|  +--------------------+         +--------------------------------------------+  |
|  |   Cloud Storage    |         |        Cloud Run (Single Monolith)         |  |
|  | (Product Pictures) |         |                                            |  |
|  +---------^----------+         |  +--------------------------------------+  |  |
|            |                    |  |   ASP.NET Core Web Host (.NET 10)    |  |  |
|            | (Direct/Proxied)   |  |                                      |  |  |
|            |                    |  |  * Razor MVC Views (/Catalog)        |  |  |
|            |                    |  |  * REST Controllers (/api/brands)    |  |  |
|            |                    |  |  * Static File Middleware            |  |  |
|            |                    |  |  * EF Core 10 DbContext Layer        |  |  |
|            |                    |  +------------------+-------------------+  |  |
|            |                    +---------------------|----------------------+  |
|            |                                          |                         |
|            |                                          | (Private VPC Network)   |
|            |                                          v                         |
|            |                    +--------------------------------------------+  |
|            +------------------->|          Cloud SQL for SQL Server          |  |
|                                 +--------------------------------------------+  |
+---------------------------------------------------------------------------------+
```

##### Network Architecture and Security

1. **Ingress Control**: Cloud Run ingress can be configured to accept public traffic via Google Cloud External Application Load Balancers (with Cloud Armor integration for DDoS/WAF protection) or direct HTTPS URLs.
2. **Private Egress**: The Cloud Run service connects to an internal Google Cloud Virtual Private Cloud (VPC) using a Serverless VPC Access connector or Direct VPC egress.
3. **Database Isolation**: The Cloud SQL for SQL Server instance resides on a private subnet within the VPC and does not expose a public IP address, ensuring secure, low-latency database connectivity from the application container.

---

#### Google Cloud Horizontals Integration

##### CI/CD with Cloud Build and Artifact Registry
*   **Build Pipeline**: Use **Google Cloud Build** to execute continuous integration tests, build the multi-stage Linux Docker image on .NET 10, and publish container artifacts.
*   **Image Repository**: Store build containers in **Google Cloud Artifact Registry**, enabling automated vulnerability scanning on base images and project layers.
*   **Continuous Deployment**: Configure Cloud Build triggers or Cloud Deploy to deploy validated container images into Cloud Run staging and production revisions with zero downtime.

##### Observability: Google Cloud Logging, Cloud Monitoring, and Cloud Trace
*   **Logging**: The container writes structured JSON logs directly to the standard console output. Cloud Run captures this output and forwards it automatically to **Google Cloud Logging**. Log correlation tags (`trace_id`, `span_id`) enable end-to-end distributed tracing across request lifecycles.
*   **Monitoring**: Real-time metrics such as request count, latency (p50, p95, p99), memory utilization, CPU allocation, and container instances are captured out of the box in **Cloud Monitoring**. Custom alert policies can be established for error spikes or database connection exhaustion.
*   **Health Probes**: Configure Cloud Run HTTP startup and liveness probes targeting ASP.NET Core Health Check endpoints (`/healthz`) to verify container initialization and database responsiveness prior to routing traffic.

##### Configuration and Secret Management
*   **Secret Manager**: Store sensitive configuration strings (such as `DefaultConnection` credentials)[^5][^12] in Google Secret Manager. Cloud Run mounts these secrets into the runtime container as secure environment variables or volume mounts, preventing secrets from leaking into container images or repository source control.

---

#### Detailed Migration Plan

The modernization roadmap executes in structured phases to ensure stability, maintain backward compatibility, and transition from Windows .NET Framework to Linux .NET 10 on Cloud Run.

##### Phase 1: Codebase Remediation and .NET 10 Upgrade
1. **Target Modern .NET 10**: Consolidate the solution around `eShopPorted` and upgrade the project file from `net461`[^2] to `net10.0`.
2. **Eliminate Insecure Primitives**: Remove the `eShopLegacy.Utilities` dependency containing `BinaryFormatter`[^3][^10]. Refactor `FilesController` to return native JSON data streams (`System.Text.Json`)[^11].
3. **ORM Modernization**: Upgrade Entity Framework Core references from version 2.2[^2] to EF Core 10. Update model configuration classes (`CatalogItemConfig.cs`, `CatalogBrandConfig.cs`)[^15][^16] to align with modern EF Core conventions.
4. **Standardize Logging**: Replace Log4Net configuration[^9] with the built-in `Microsoft.Extensions.Logging` abstraction, adding a console provider formatted for Google Cloud Logging JSON ingestion.

##### Phase 2: Linux Containerization
1. **Develop Linux Dockerfile**: Create an optimized, multi-stage Dockerfile using Debian-based or Alpine-based .NET 10 runtime base images.
   ```dockerfile
   # Build Stage
   FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
   WORKDIR /src
   COPY . .
   RUN dotnet publish eShopPorted/eShopPorted.csproj -c Release -o /app/publish

   # Runtime Stage
   FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
   WORKDIR /app
   COPY --from=build /app/publish .
   ENV ASPNETCORE_URLS=http://+:8080
   EXPOSE 8080
   ENTRYPOINT ["dotnet", "eShopPorted.dll"]
   ```
2. **Validate on Linux**: Verify application initialization and SQL connectivity locally using Linux Docker containers.

##### Phase 3: Infrastructure Setup on Google Cloud
1. **Network Configuration**: Configure a Google Cloud VPC with private IP range allocations. Create a Serverless VPC Access connector for serverless-to-VPC routing.
2. **Database Provisioning**: Provision a Google Cloud SQL for SQL Server instance with private IP access enabled.
3. **Schema and Data Migration**: Apply database schema definitions and seed data (using EF Core migrations or SQL scripts)[^17][^18] to Cloud SQL.
4. **Storage Creation**: Create a Google Cloud Storage bucket to store product catalog assets (`Pics`)[^6], applying necessary IAM permissions or Cloud CDN integration.
5. **Secrets Configuration**: Register database connection strings and sensitive application settings in Secret Manager.

##### Phase 4: Cloud Run Deployment and Cutover
1. **Build and Push**: Trigger Cloud Build to compile the application and publish the resulting image to Artifact Registry.
2. **Deploy Service**: Deploy the container to Cloud Run with the following baseline settings:
   * Execution environment: Linux container
   * Port: `8080`
   * Concurrency: `80` requests per instance
   * Minimum instances: `0` (or `1` for warm start critical latency)
   * Maximum instances: scaled according to expected peak load
   * Secrets: Injected from Secret Manager
   * VPC connector: Bound for private Cloud SQL connectivity
3. **End-to-End Testing**: Execute functional and load testing against the Cloud Run service URL to validate catalog browsing, pagination, item creation, and database persistence.
4. **DNS and Cutover**: Update custom domain mapping, attach managed SSL certificates in Cloud Run or Google Cloud Load Balancing, and complete production traffic cutover.

---

#### References

[^1]: `src/eShopLegacyMVC/eShopLegacyMVC.csproj`: `TargetFrameworkVersion v4.7.2` - Original legacy ASP.NET MVC project targeting .NET Framework 4.7.2.
[^2]: `eShopPorted/eShopPorted.csproj`: `TargetFramework net461` - Ported ASP.NET Core 2.2 project compiled against .NET Framework 4.6.1.
[^3]: `eShopLegacy.Utilities/eShopLegacy.Utilities.csproj`: `TargetFrameworkVersion v4.6.1` - Shared utility class library project targeting .NET Framework 4.6.1.
[^4]: `src/eShopLegacyMVC/Controllers/PicController.cs`: `Server.MapPath("~/Pics")` - Resolves physical local Windows disk paths for catalog item image files.
[^5]: `src/eShopLegacyMVC/Web.config`: `CatalogDBContext` - SQL Server connection string configured for LocalDB database instance.
[^6]: `eShopPorted/Controllers/PicController.cs`: `Index(int catalogItemId)` - Action retrieving catalog pictures from local file system paths.
[^7]: `src/eShopLegacyMVC/ApplicationInsights.config`: Root configuration - Configures Microsoft Azure Application Insights telemetry modules and sinks.
[^8]: `src/eShopLegacyMVC/packages.config`: `Microsoft.ApplicationInsights` - Packages managing legacy Application Insights tracking.
[^9]: `src/eShopLegacyMVC/log4Net.xml`: `RollingFileAppender` - Log4Net file appender configuring local disk log rolling (`logFiles\myapp.log`).
[^10]: `eShopLegacy.Utilities/Serializing.cs`: `SerializeBinary(object input)` - Binary serialization implementation utilizing `BinaryFormatter`.
[^11]: `eShopPorted/Controllers/Api/FilesController.cs`: `Index()` - REST controller action calling `Serializing.SerializeBinary`.
[^12]: `eShopPorted/appsettings.json`: `DefaultConnection` - Connection string configuration for ported local SQL database.
[^13]: `eShopPorted/Controllers/CatalogController.cs`: `CatalogController` - Razor MVC controller providing views for listing, editing, creating, and deleting items.
[^14]: `eShopPorted/Controllers/Api/BrandsController.cs`: `BrandsController` - ASP.NET Core API controller providing endpoints for brand data.
[^15]: `eShopPorted/Models/Config/CatalogItemConfig.cs`: `CatalogItemConfig` - Entity Framework Core entity mapping configuration for `CatalogItem`.
[^16]: `eShopPorted/Models/Config/CatalogBrandConfig.cs`: `CatalogBrandConfig` - Entity Framework Core entity mapping configuration for `CatalogBrand`.
[^17]: `eShopPorted/Migrations/20201130044339_Initial.cs`: `Up(MigrationBuilder migrationBuilder)` - Entity Framework Core schema creation migration.
[^18]: `src/eShopLegacyMVC/Models/Infrastructure/dbo.catalog_hilo.Sequence.sql`: SQL Sequence script - Creates SQL Server sequence for catalog item HiLo identity generation.

### Modernize to Cloud Run
#### Modernization Strategy: Deploying to Cloud Run

Migrating the eShop catalog application from a legacy .NET Framework Windows architecture to a unified, containerized service running on .NET 10 on Linux using Cloud Run yields significant benefits in infrastructure management, compute utilization, portability, and operational overhead. 

The current solution consists of:
- A legacy ASP.NET 4.7.2 MVC monolithic application (`eShopLegacyMVC`) relying on System.Web, IIS, and Windows-specific features [^1].
- An early-stage port (`eShopPorted`) using ASP.NET Core 2.2, which still targets .NET Framework 4.6.1 (`net461`) [^2].
- A shared class library (`eShopLegacy.Utilities`) targeting .NET Framework 4.6.1 [^3].

In accordance with architectural requirements to maintain operational simplicity, the system will not be decomposed into multiple microservices or event-driven functions. Instead, it will be modernized as a **single, unified service** deployed to Cloud Run. Moving to .NET 10 on Linux enables containerization within standard Open Container Initiative (OCI) images and eliminates Windows Server licensing requirements while unlocking Cloud Run capabilities such as automatic horizontal scaling (including scale-to-zero), automatic TLS termination, and built-in health checking.

```
       [ Client Request ]
               │
               ▼
   [ Cloud Run Service (Linux) ]
   ┌──────────────────────────────────────────┐
   │ ASP.NET Core (.NET 10) Single Container  │
   │  - MVC UI & Razor Views                  │
   │  - REST APIs (/api/brands, /api/catalog) │
   │  - Cloud Storage Static Media Client     │
   └──────────────────────────────────────────┘
               │
      (Private VPC Egress)
               ▼
   [ Cloud SQL for SQL Server / PostgreSQL ]
```

---

#### External Dependencies and Google Cloud Mapping

The legacy application relies on several infrastructure components and libraries that require adaptation for a cloud-native Linux container environment.

| Dependency / Component | Current Implementation | Google Cloud Strategy | Modernization Approach |
| :--- | :--- | :--- | :--- |
| **Relational Database** | Microsoft SQL Server (LocalDB in development) [^4] | **Cloud SQL for SQL Server** (or **Cloud SQL for PostgreSQL**) | Connect via Cloud SQL Auth Proxy or Serverless VPC Access with Private IP. Migrate schema using Entity Framework Core migrations [^5]. |
| **Media / Picture Storage** | Local file system (`Server.MapPath("~/Pics")` or local disk paths) [^6] | **Cloud Storage** | Offload catalog images to a Cloud Storage bucket, serving files via public bucket URLs or signed URLs rather than reading physical files from disk [^7]. |
| **Session Management** | In-process IIS session state (`InProc`) [^8] | **Stateless Design** or **Memorystore for Redis** | Eliminate `InProc` session state. Cloud Run instances are stateless and ephemeral; user sessions should either be avoided or stored in Memorystore for Redis if state persistence across instances is mandatory. |
| **Telemetry & Metrics** | Azure Application Insights (`Microsoft.AI.Web`) [^9] | **Cloud Monitoring** & **Cloud Trace** | Replace `ApplicationInsights` telemetry initializers and configuration with Google Cloud OpenTelemetry .NET SDK or Google Cloud Trace libraries [^10]. |
| **Application Logging** | `log4net` file-based rolling logger writing to `logFiles\myapp.log` [^11] | **Cloud Logging** | Redirect all log outputs to standard output (`stdout`/`stderr`) using `Microsoft.Extensions.Logging` or a console-targeted log4net/Serilog provider, which Cloud Run automatically ingests into Cloud Logging. |
| **Object Serialization** | `BinaryFormatter` in `eShopLegacy.Utilities` [^12] | **System.Text.Json** | `BinaryFormatter` is deprecated and disabled by default in modern .NET due to remote code execution vulnerabilities; replace with standard JSON serialization [^13]. |
| **Secrets & Configuration** | `Web.config` appSettings and connectionStrings [^14] | **Secret Manager** & Cloud Run Environment Variables | Store connection strings and credentials securely in Secret Manager and mount them directly as environment variables into Cloud Run. |

---

#### Unified Service Architecture

Rather than fragmenting the application into separate microservices or distributed Cloud Run Functions, the application will remain a single deployment unit on Cloud Run.

##### Rationale for Single-Service Architecture
- **Low Operational Complexity:** Eliminates distributed transaction challenges, network latency between internal service calls, API version coordination, and cross-service communication failures.
- **Efficient Cloud Run Concurrency:** A single Cloud Run instance running .NET 10 can handle up to 1,000 concurrent requests per container instance, allowing high throughput with minimal resource footprints.
- **Unified Domain Logic:** The catalog querying, admin maintenance, image serving, and API endpoints share the same Entity Framework Core model and business context (`CatalogItem`, `CatalogBrand`, `CatalogType`) [^15].

##### Networking and Connectivity
- **Ingress:** Configured to receive internal and external HTTPS traffic directly from the internet or through an external Application Load Balancer with Cloud Armor for Web Application Firewall (WAF) protection.
- **Egress to Data Tier:** Direct VPC egress or a Serverless VPC Access connector routes database traffic privately to Cloud SQL inside a Virtual Private Cloud (VPC), ensuring database credentials and data are not exposed to the public internet [^16].

---

#### Evaluation of Cloud Run Functions vs. Cloud Run Service

While Google Cloud provides Cloud Run Functions (formerly Cloud Functions) for event-driven logic, a monolithic Cloud Run container service is optimal for this application:

| Feature / Consideration | Cloud Run (Recommended) | Cloud Run Functions | Evaluation for eShop |
| :--- | :--- | :--- | :--- |
| **Execution Model** | Persistent HTTP server with high request concurrency | Single-purpose event/HTTP invocations | Cloud Run accommodates MVC view rendering, static resources, and API calls within a single runtime. |
| **Routing & Middleware** | Native ASP.NET Core routing and dependency injection pipeline | Requires routing logic per function or multiple function deployments | A unified routing setup simplifies the `CatalogController` and API endpoints without deploying multiple disparate artifacts [^17]. |
| **Cold Start Mitigation** | Configurable minimum instances (`--min-instances`) | Per-function cold starts during scale-out | Cloud Run ensures rapid responses across UI and API endpoints by retaining warmed container instances. |
| **Database Connection Pooling** | Shared ADO.NET / EF Core DbContext connection pool per container instance | Potential connection exhaustion across hundreds of function instances | Cloud Run preserves stable database connection counts against Cloud SQL [^18]. |

---

#### Utilization of Google Cloud Platform Horizontals

```
┌────────────────────────────────────────────────────────────────────────┐
│                        Google Cloud Platform                           │
│                                                                        │
│   [ Cloud Build ] ──▶ [ Artifact Registry ] ──▶ [ Cloud Run (.NET 10) ]│
│                                                         │              │
│   ┌─────────────────────────────────────────────────────┼──────────┐   │
│   │ Networking & Storage                                │          │   │
│   │   ├── Cloud Storage (Pics & Assets) ◀───────────────┘          │   │
│   │   ├── Secret Manager (Connection Strings & Config) ────────────┤   │
│   │   └── Direct VPC Egress ──▶ [ Cloud SQL for SQL Server ]       │   │
│   └────────────────────────────────────────────────────────────────┘   │
│   ┌────────────────────────────────────────────────────────────────┐   │
│   │ Observability                                                  │   │
│   │   ├── Cloud Logging (Structured Console Output)                │   │
│   │   └── Cloud Monitoring (Container Metrics & Latency)           │   │
│   └────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────┘
```

##### 1. Build and Deployment (CI/CD)
- **Artifact Registry:** Stores versioned, hardened Linux OCI container images compiled for `.NET 10`.
- **Cloud Build:** Automates test execution, `dotnet publish -c Release`, container image creation via multi-stage Dockerfiles, and seamless blue/green deployments to Cloud Run using revisions.

##### 2. Observability (Logging and Monitoring)
- **Cloud Logging:** The legacy rolling file appender [^11] is replaced by console-directed structured JSON logging. Cloud Run captures all `stdout` output and indexes logs in real time with trace correlation.
- **Cloud Monitoring:** Tracks Cloud Run container instance counts, CPU/memory utilization, request latency, and HTTP status code rates with custom alerting policies.

##### 3. Static Media and Asset Storage
- Catalog product images currently kept in local paths (`Pics/1.png`, `Pics/2.png`, etc.) [^19] are placed in a dedicated Cloud Storage bucket. The `PictureUri` property on `CatalogItem` can be modified to return the Cloud Storage public endpoint or a custom domain, offloading static file serving from application compute [^20].

##### 4. Configuration and Security
- **Secret Manager:** Database passwords and external service tokens are retrieved at runtime or injected via Cloud Run secret volume mounts or environment variables, avoiding static strings in `appsettings.json` or `Web.config` [^4] [^14].
- **Workload Identity:** Cloud Run services run under a designated IAM Service Account with minimal privileges (Cloud SQL Client, Secret Manager Secret Accessor, Storage Object Viewer).

---

#### Detailed Migration Plan

The modernization path follows a phased approach to migrate from the Windows-based legacy framework to a containerized .NET 10 service running on Cloud Run.

##### Phase 1: Target .NET 10 and Linux Porting
1. **Target .NET 10 Framework:** Update the project to target .NET 10.
2. **Eliminate Windows & Legacy System Dependencies:**
   - Remove obsolete System.Web packages, `HttpApplication` life cycle events (`Global.asax.cs`) [^21], and `BundleConfig` / `WebGrease` [^22].
   - Replace `BinaryFormatter` calls in `eShopLegacy.Utilities/Serializing.cs` with `System.Text.Json` [^12].
   - Remove IIS handlers, ISAPI modules, and `System.Web.Optimization` dependencies from `Web.config` [^14] and `Views/Web.config` [^23].
3. **Consolidate Controller and Dependency Pipelines:**
   - Standardize all endpoints under ASP.NET Core controllers (combining MVC and Web API functionality into `Microsoft.AspNetCore.Mvc`) [^24].
   - Replace Autofac custom module initialization (`ApplicationModule`) [^25] with native .NET Core `IServiceCollection` registration.
4. **ORM and Data Layer Modernization:**
   - Replace Entity Framework 6 (`DbContext`) [^26] with Entity Framework Core configured for SQL Server or PostgreSQL [^27].
   - Replace Hi-Lo sequence generation SQL scripts (`dbo.catalog_hilo.Sequence.sql`) [^28] with native EF Core sequence mapping (`UseHiLo`).

##### Phase 2: Containerization for Linux
1. **Develop Multi-Stage Dockerfile:**
   - Base stage using `mcr.microsoft.com/dotnet/aspnet:10.0` (Linux Alpine or Debian).
   - Build stage using `mcr.microsoft.com/dotnet/sdk:10.0`.
   - Ensure non-root user execution (`USER app`) for container security.
2. **Update Image and Asset Retrieval:**
   - Upload media assets from `src/eShopLegacyMVC/Pics` [^19] to a Google Cloud Storage bucket.
   - Refactor `PicController.cs` to redirect requests to Cloud Storage URLs or configure direct media links in `CatalogController.AddUriPlaceHolder` [^7] [^20].
3. **Configure Console Logging:**
   - Replace the `log4net.Appender.RollingFileAppender` [^11] with standard ASP.NET Core Console logging formatted for Cloud Logging.

##### Phase 3: Google Cloud Infrastructure Provisioning
1. **Database Setup:**
   - Deploy an instance of **Cloud SQL for SQL Server** (or PostgreSQL).
   - Configure private IP connectivity within the Virtual Private Cloud (VPC).
2. **Secrets Storage:**
   - Provision connection strings and credentials in **Secret Manager**.
3. **Media Storage:**
   - Create a Google **Cloud Storage** bucket configured with Uniform Bucket-Level Access.
4. **Networking:**
   - Configure Cloud Run Direct VPC egress or a Serverless VPC Access connector to permit secure communication between the Cloud Run container and Cloud SQL private IP.

##### Phase 4: CI/CD Pipeline and Deployment
1. **Setup Build Automation:**
   - Configure a `cloudbuild.yaml` file in Cloud Build to trigger on repository commits.
   - Build container image and push to **Artifact Registry**.
2. **Deploy to Cloud Run:**
   - Deploy the container to Cloud Run with:
     - Memory allocation: 512MiB to 1GiB.
     - CPU allocation: 1 vCPU.
     - Execution environment: Second Generation (recommended for Linux system calls and networking performance).
     - Minimum instances: 1 (to eliminate cold starts for web users).
     - Autoscaling limits: Maximum instances based on expected traffic.
     - Bound service account granting access to Secret Manager and Cloud SQL.

##### Phase 5: Verification, Testing, and Cutover
1. **Functional Testing:** Execute end-to-end user workflows: pagination, catalog creation, editing, deleting, and image loading.
2. **Load & Concurrency Validation:** Verify that Cloud Run correctly handles concurrent database sessions through EF Core without connection starvation.
3. **Traffic Cutover:** Map custom domain name to the Cloud Run service, verify SSL/TLS certificate provisioning, and route production traffic.

---

[^1]: src/eShopLegacyMVC/eShopLegacyMVC.csproj: `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` - Specifies target framework as legacy .NET Framework 4.7.2.
[^2]: eShopPorted/eShopPorted.csproj: `<TargetFramework>net461</TargetFramework>` - Specifies that the ported ASP.NET Core project still targets .NET Framework 4.6.1.
[^3]: eShopLegacy.Utilities/eShopLegacy.Utilities.csproj: `<TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>` - Defines legacy framework version for utilities library.
[^4]: eShopPorted/appsettings.json: `ConnectionStrings:DefaultConnection` - Defines connection string for local SQL Server LocalDB database.
[^5]: eShopPorted/Models/CatalogDBContext.cs: CatalogDBContext - EF Core database context implementation for the ported catalog service.
[^6]: src/eShopLegacyMVC/Controllers/PicController.cs: Index() - Resolves local directory path with Server.MapPath and reads raw image bytes from local disk.
[^7]: eShopPorted/Controllers/PicController.cs: Index() - Reads file from disk buffer to return images; should be replaced by Cloud Storage URLs.
[^8]: src/eShopLegacyMVC/Web.config: `<sessionState mode="InProc" />` - Configures in-process session state unsuitable for multi-instance cloud deployments.
[^9]: src/eShopLegacyMVC/ApplicationInsights.config: `<ApplicationInsights>` - Configures Azure Application Insights modules and telemetry initializers.
[^10]: src/eShopLegacyMVC/eShopLegacyMVC.csproj: `<PackageReference Include="Microsoft.ApplicationInsights"...` - Application Insights dependency to be retired in favor of Cloud Monitoring.
[^11]: src/eShopLegacyMVC/log4Net.xml: `<appender name="file" ...>` - Configures log4net file appender writing to local disk.
[^12]: eShopLegacy.Utilities/Serializing.cs: SerializeBinary() - Serializes objects using deprecated BinaryFormatter.
[^13]: eShopPorted/Controllers/Api/FilesController.cs: Index() - Uses BinaryFormatter through the Serializing utility class.
[^14]: src/eShopLegacyMVC/Web.config: `<configuration>` - Main legacy web configuration file containing system.web and system.webServer configuration.
[^15]: eShopPorted/Models/CatalogItem.cs: CatalogItem - Core catalog entity definition shared by UI and API controllers.
[^16]: eShopPorted/Startup.cs: ConfigureServices() - Registers CatalogDBContext with SQL Server options via connection string.
[^17]: eShopPorted/Controllers/CatalogController.cs: CatalogController - MVC controller managing catalog views, creation, editing, and deletion.
[^18]: eShopPorted/Services/CatalogService.cs: CatalogService - Service implementing database access operations using CatalogDBContext.
[^19]: src/eShopLegacyMVC/Pics/1.png: Picture Asset - Example product image currently stored on the local file system.
[^20]: src/eShopLegacyMVC/Controllers/CatalogController.cs: AddUriPlaceHolder() - Generates local route-based picture URLs for catalog items.
[^21]: src/eShopLegacyMVC/Global.asax.cs: MvcApplication - Inherits HttpApplication to manage ASP.NET application start and request lifecycle.
[^22]: src/eShopLegacyMVC/App_Start/BundleConfig.cs: RegisterBundles() - Legacy asset bundling using WebGrease and System.Web.Optimization.
[^23]: src/eShopLegacyMVC/Views/Web.config: `<system.webServer>` - Web configuration preventing direct access to Razor views under IIS.
[^24]: eShopPorted/Controllers/Api/BrandsController.cs: BrandsController - API controller leveraging ASP.NET Core ControllerBase.
[^25]: src/eShopLegacyMVC/Modules/ApplicationModule.cs: Load() - Autofac dependency injection configuration module.
[^26]: src/eShopLegacyMVC/Models/CatalogDBContext.cs: CatalogDBContext - EF6 database context implementation targeting .NET Framework.
[^27]: eShopPorted/Models/Config/CatalogItemConfig.cs: CatalogItemConfig - EF Core entity configuration implementing IEntityTypeConfiguration.
[^28]: src/eShopLegacyMVC/Models/Infrastructure/dbo.catalog_hilo.Sequence.sql: CREATE SEQUENCE - Raw SQL script creating database sequence for HiLo generator.

