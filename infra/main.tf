locals {
  required_apis = toset([
    "artifactregistry.googleapis.com",
    "compute.googleapis.com",
    "run.googleapis.com",
    "secretmanager.googleapis.com",
    "servicenetworking.googleapis.com",
    "sqladmin.googleapis.com",
    "storage.googleapis.com",
  ])

  image_uri = "${var.region}-docker.pkg.dev/${var.project_id}/${google_artifact_registry_repository.app.repository_id}/${var.service_name}:${var.image_tag}"
}

resource "google_project_service" "apis" {
  for_each = local.required_apis

  project            = var.project_id
  service            = each.value
  disable_on_destroy = false
}

# ---------------------------------------------------------------------------
# VPC & Private Services Access for Cloud SQL
# ---------------------------------------------------------------------------

resource "google_compute_network" "main" {
  name                    = "${var.service_name}-vpc"
  auto_create_subnetworks = false

  depends_on = [google_project_service.apis["compute.googleapis.com"]]
}

resource "google_compute_subnetwork" "cloudrun" {
  name                     = "${var.service_name}-subnet"
  ip_cidr_range            = "10.10.0.0/24"
  region                   = var.region
  network                  = google_compute_network.main.id
  private_ip_google_access = true
}

resource "google_compute_global_address" "private_ip_range" {
  name          = "${var.service_name}-sql-psa"
  purpose       = "VPC_PEERING"
  address_type  = "INTERNAL"
  prefix_length = 16
  network       = google_compute_network.main.id
}

resource "google_service_networking_connection" "private_vpc_connection" {
  network                 = google_compute_network.main.id
  service                 = "servicenetworking.googleapis.com"
  reserved_peering_ranges = [google_compute_global_address.private_ip_range.name]

  depends_on = [google_project_service.apis["servicenetworking.googleapis.com"]]
}

# ---------------------------------------------------------------------------
# Artifact Registry
# ---------------------------------------------------------------------------

resource "google_artifact_registry_repository" "app" {
  location      = var.region
  repository_id = var.service_name
  description   = "Linux .NET 10 container images for ${var.service_name}"
  format        = "DOCKER"

  depends_on = [google_project_service.apis["artifactregistry.googleapis.com"]]
}

# ---------------------------------------------------------------------------
# Cloud Storage Bucket for Catalog Images
# ---------------------------------------------------------------------------

resource "random_id" "bucket_suffix" {
  byte_length = 4
}

resource "google_storage_bucket" "catalog_pics" {
  name                        = "${var.project_id}-${var.service_name}-pics-${random_id.bucket_suffix.hex}"
  location                    = var.region
  uniform_bucket_level_access = true
  public_access_prevention    = "enforced"
  force_destroy               = !var.deletion_protection

  versioning {
    enabled = true
  }

  depends_on = [google_project_service.apis["storage.googleapis.com"]]
}

resource "google_storage_bucket_object" "catalog_pics" {
  for_each = fileset("${path.module}/../eShopLegacyMVCSolution/eShopPorted/wwwroot/Pics", "*.png")

  name         = each.value
  bucket       = google_storage_bucket.catalog_pics.name
  source       = "${path.module}/../eShopLegacyMVCSolution/eShopPorted/wwwroot/Pics/${each.value}"
  content_type = "image/png"
}

# ---------------------------------------------------------------------------
# Cloud SQL for SQL Server & Generated Passwords
# ---------------------------------------------------------------------------

resource "random_password" "sql_root" {
  length           = 24
  min_upper        = 2
  min_lower        = 2
  min_numeric      = 2
  min_special      = 2
  override_special = "!@#%^&*-_=+"
}

resource "random_password" "sql_app" {
  length           = 24
  min_upper        = 2
  min_lower        = 2
  min_numeric      = 2
  min_special      = 2
  override_special = "!@#%^&*-_=+"
}

resource "google_sql_database_instance" "sqlserver" {
  name                = "${var.service_name}-sql"
  region              = var.region
  database_version    = var.sql_database_version
  root_password       = random_password.sql_root.result
  deletion_protection = var.deletion_protection

  settings {
    tier              = var.sql_tier
    availability_type = var.sql_availability_type

    ip_configuration {
      ipv4_enabled    = false
      private_network = google_compute_network.main.id
    }

    backup_configuration {
      enabled = true
    }
  }

  depends_on = [
    google_project_service.apis["sqladmin.googleapis.com"],
    google_service_networking_connection.private_vpc_connection,
  ]
}

resource "google_sql_database" "catalog" {
  name     = "CatalogDb"
  instance = google_sql_database_instance.sqlserver.name
}

resource "google_sql_user" "app" {
  name     = "eshop_app"
  instance = google_sql_database_instance.sqlserver.name
  password = random_password.sql_app.result
}

# ---------------------------------------------------------------------------
# Secret Manager (Database Connection String)
# ---------------------------------------------------------------------------

resource "google_secret_manager_secret" "db_connection" {
  secret_id = "${var.service_name}-db-connection"

  replication {
    auto {}
  }

  depends_on = [google_project_service.apis["secretmanager.googleapis.com"]]
}

resource "google_secret_manager_secret_version" "db_connection" {
  secret      = google_secret_manager_secret.db_connection.id
  secret_data = "Server=tcp:${google_sql_database_instance.sqlserver.private_ip_address},1433;Database=${google_sql_database.catalog.name};User Id=${google_sql_user.app.name};Password=${random_password.sql_app.result};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
}

# ---------------------------------------------------------------------------
# Dedicated Cloud Run Service Account & Least-Privilege IAM
# ---------------------------------------------------------------------------

resource "google_service_account" "cloudrun" {
  account_id   = "${var.service_name}-sa"
  display_name = "Cloud Run runtime and migration service account for ${var.service_name}"
}

resource "google_secret_manager_secret_iam_member" "cloudrun_db_secret" {
  secret_id = google_secret_manager_secret.db_connection.id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.cloudrun.email}"
}

resource "google_storage_bucket_iam_member" "cloudrun_pics_viewer" {
  bucket = google_storage_bucket.catalog_pics.name
  role   = "roles/storage.objectViewer"
  member = "serviceAccount:${google_service_account.cloudrun.email}"
}

resource "google_project_iam_member" "cloudrun_sql_client" {
  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${google_service_account.cloudrun.email}"
}

resource "google_project_iam_member" "cloudrun_log_writer" {
  project = var.project_id
  role    = "roles/logging.logWriter"
  member  = "serviceAccount:${google_service_account.cloudrun.email}"
}

resource "google_project_iam_member" "cloudrun_metric_writer" {
  project = var.project_id
  role    = "roles/monitoring.metricWriter"
  member  = "serviceAccount:${google_service_account.cloudrun.email}"
}

# ---------------------------------------------------------------------------
# Cloud Run Job: EF Core Migration Bundle (/app/efbundle)
# ---------------------------------------------------------------------------

resource "google_cloud_run_v2_job" "migrate" {
  name                = "${var.service_name}-migrate"
  location            = var.region
  deletion_protection = var.deletion_protection

  template {
    task_count = 1

    template {
      service_account       = google_service_account.cloudrun.email
      execution_environment = "EXECUTION_ENVIRONMENT_GEN2"
      max_retries           = 1
      timeout               = "300s"

      vpc_access {
        network_interfaces {
          network    = google_compute_network.main.name
          subnetwork = google_compute_subnetwork.cloudrun.name
        }
        egress = "PRIVATE_RANGES_ONLY"
      }

      containers {
        image   = local.image_uri
        command = ["/app/efbundle"]

        resources {
          limits = {
            cpu    = "1"
            memory = "512Mi"
          }
        }

        env {
          name = "ConnectionStrings__DefaultConnection"
          value_source {
            secret_key_ref {
              secret  = google_secret_manager_secret.db_connection.secret_id
              version = "latest"
            }
          }
        }
      }
    }
  }

  depends_on = [
    google_project_service.apis["run.googleapis.com"],
    google_secret_manager_secret_version.db_connection,
    google_secret_manager_secret_iam_member.cloudrun_db_secret,
  ]
}

# ---------------------------------------------------------------------------
# Cloud Run Service: Unified .NET 10 Catalog Monolith
# ---------------------------------------------------------------------------

resource "google_cloud_run_v2_service" "app" {
  name                = var.service_name
  location            = var.region
  ingress             = "INGRESS_TRAFFIC_ALL"
  deletion_protection = var.deletion_protection

  template {
    service_account                  = google_service_account.cloudrun.email
    execution_environment            = "EXECUTION_ENVIRONMENT_GEN2"
    max_instance_request_concurrency = 80
    timeout                          = "60s"

    scaling {
      min_instance_count = var.min_instances
      max_instance_count = var.max_instances
    }

    vpc_access {
      network_interfaces {
        network    = google_compute_network.main.name
        subnetwork = google_compute_subnetwork.cloudrun.name
      }
      egress = "PRIVATE_RANGES_ONLY"
    }

    containers {
      image = local.image_uri

      ports {
        container_port = 8080
      }

      resources {
        limits = {
          cpu    = "1"
          memory = "512Mi"
        }
        cpu_idle          = true
        startup_cpu_boost = true
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "UseMockData"
        value = "false"
      }

      env {
        name  = "Database__ApplyMigrationsOnStartup"
        value = "false"
      }

      env {
        name  = "Authentication__TrustCloudRunIam"
        value = "true"
      }

      env {
        name  = "Storage__BucketName"
        value = google_storage_bucket.catalog_pics.name
      }

      env {
        name = "ConnectionStrings__DefaultConnection"
        value_source {
          secret_key_ref {
            secret  = google_secret_manager_secret.db_connection.secret_id
            version = "latest"
          }
        }
      }

      startup_probe {
        http_get {
          path = "/healthz"
          port = 8080
        }
        initial_delay_seconds = 2
        period_seconds        = 5
        timeout_seconds       = 3
        failure_threshold     = 10
      }

      liveness_probe {
        http_get {
          path = "/healthz"
          port = 8080
        }
        period_seconds    = 30
        timeout_seconds   = 5
        failure_threshold = 3
      }
    }
  }

  depends_on = [
    google_project_service.apis["run.googleapis.com"],
    google_secret_manager_secret_version.db_connection,
    google_secret_manager_secret_iam_member.cloudrun_db_secret,
    google_storage_bucket_iam_member.cloudrun_pics_viewer,
  ]
}

resource "google_cloud_run_v2_service_iam_member" "invokers" {
  for_each = toset(var.invoker_members)

  project  = google_cloud_run_v2_service.app.project
  location = google_cloud_run_v2_service.app.location
  name     = google_cloud_run_v2_service.app.name
  role     = "roles/run.invoker"
  member   = each.value
}
