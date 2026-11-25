variable "project_id" {
  description = "Google Cloud project ID."
  type        = string
  default     = "chintan-sandbox"
}

variable "region" {
  description = "Google Cloud region for Cloud Run, Artifact Registry, Cloud SQL, and Cloud Storage."
  type        = string
  default     = "australia-southeast1"
}

variable "service_name" {
  description = "Base name for the Cloud Run service and supporting infrastructure resources."
  type        = string
  default     = "eshop-catalog"
}

variable "image_tag" {
  description = "Container image tag in Artifact Registry."
  type        = string
  default     = "latest"
}

variable "sql_database_version" {
  description = "Cloud SQL for SQL Server version/edition."
  type        = string
  default     = "SQLSERVER_2022_EXPRESS"
}

variable "sql_tier" {
  description = "Cloud SQL custom machine tier (SQL Server requires at least 1 vCPU and 3840 MB RAM)."
  type        = string
  default     = "db-custom-1-3840"
}

variable "sql_availability_type" {
  description = "Cloud SQL availability type (ZONAL or REGIONAL)."
  type        = string
  default     = "ZONAL"

  validation {
    condition     = contains(["ZONAL", "REGIONAL"], var.sql_availability_type)
    error_message = "sql_availability_type must be either 'ZONAL' or 'REGIONAL'."
  }
}

variable "deletion_protection" {
  description = "Whether to enable deletion protection on stateful Cloud SQL, Cloud Run, and GCS resources."
  type        = bool
  default     = true
}

variable "min_instances" {
  description = "Minimum number of Cloud Run service instances."
  type        = number
  default     = 0
}

variable "max_instances" {
  description = "Maximum number of Cloud Run service instances."
  type        = number
  default     = 10
}

variable "invoker_members" {
  description = "IAM principals granted roles/run.invoker on the Cloud Run service (e.g., user:alice@example.com)."
  type        = list(string)
  default     = []

  validation {
    condition = alltrue([
      for member in var.invoker_members :
      !contains(["allUsers", "allAuthenticatedUsers"], trimspace(member))
    ])
    error_message = "invoker_members must not contain 'allUsers' or 'allAuthenticatedUsers' because CloudRunIamAuthenticationHandler trusts Cloud Run IAM for mutating endpoints."
  }
}
