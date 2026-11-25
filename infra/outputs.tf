output "cloud_run_service_url" {
  description = "HTTPS URL of the deployed Cloud Run service."
  value       = google_cloud_run_v2_service.app.uri
}

output "migration_job_name" {
  description = "Name of the Cloud Run job that executes the EF Core migration bundle (/app/efbundle)."
  value       = google_cloud_run_v2_job.migrate.name
}

output "artifact_registry_image_uri" {
  description = "Target container image URI in Artifact Registry."
  value       = local.image_uri
}

output "catalog_images_bucket_name" {
  description = "Cloud Storage bucket hosting catalog product images."
  value       = google_storage_bucket.catalog_pics.name
}

output "cloud_sql_instance_connection_name" {
  description = "Connection name of the Cloud SQL for SQL Server instance."
  value       = google_sql_database_instance.sqlserver.connection_name
}

output "cloud_sql_private_ip" {
  description = "Private IPv4 address of the Cloud SQL for SQL Server instance."
  value       = google_sql_database_instance.sqlserver.private_ip_address
}

output "db_connection_secret_id" {
  description = "Secret Manager secret ID storing the SQL Server connection string."
  value       = google_secret_manager_secret.db_connection.secret_id
}
