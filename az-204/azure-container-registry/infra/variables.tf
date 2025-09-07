variable "rg_name" {
  type        = string
  default     = "demo-rg"
  description = "Resource group name"
}

variable "location" {
  type        = string
  default     = "East US"
  description = "Azure region"
}


variable "subscription_id" {
  type        = string
  description = "Subscription to deploy to"
  default     = null
}
