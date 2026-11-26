export type Role = "Admin" | "Staff" | "User"

export type AssetStatus = "Available" | "OnLoan" | "Maintenance" | "Retired"

export type AssetCondition = "Good" | "MinorDamage" | "MajorDamage"

export type LoanStatus =
  | "Pending"
  | "Approved"
  | "Rejected"
  | "OnLoan"
  | "Returned"
  | "Overdue"

export type MaintenanceStatus = "Reported" | "InProgress" | "Done" | "Cancelled"

export interface ApiEnvelope<T> {
  success: boolean
  data: T
  message: string | null
  errors: Record<string, string[]> | null
}

export interface Page<T> {
  items: T[]
  totalItems: number
  totalPages: number
  page: number
  pageSize: number
}

export interface ListQuery {
  search?: string
  page?: number
  pageSize?: number
  sortBy?: string
  sortDir?: "asc" | "desc"
}

export interface UserProfile {
  id: string
  fullName: string
  email: string
  role: Role
  department: string
  phone: string
  isActive: boolean
}

export interface LoginResponse {
  accessToken: string
  tokenType: string
  expiresAt: string
  user: UserProfile
}

export interface Asset {
  id: string
  assetCode: string
  name: string
  categoryId: string
  categoryName: string
  locationId: string
  locationName: string
  vendorId: string | null
  vendorName: string | null
  serialNumber: string | null
  purchaseDate: string
  purchasePrice: number
  condition: AssetCondition
  status: AssetStatus
  photoUrl: string | null
  notes: string | null
}

export interface RecentLoan {
  id: string
  borrowerId: string
  requestedAt: string
  loanDate: string
  dueDate: string
  returnDate: string | null
  status: LoanStatus
  purpose: string
}

export interface AssetDetail {
  asset: Asset
  mostRecentLoan: RecentLoan | null
}

export interface Loan {
  id: string
  assetId: string
  assetCode: string
  assetName: string
  borrowerId: string
  borrowerName: string
  requestedAt: string
  loanDate: string
  dueDate: string
  returnDate: string | null
  status: LoanStatus
  purpose: string
  approvedById: string | null
  rejectionReason: string | null
  returnCondition: AssetCondition | null
}

export interface MaintenanceTicket {
  id: string
  assetId: string
  assetCode: string
  assetName: string
  reportedById: string
  reportedAt: string
  description: string
  status: MaintenanceStatus
  cost: number | null
  startedAt: string
  finishedAt: string
  vendorId: string | null
}

export interface DashboardSummary {
  totalAssets: number
  availableAssets: number
  onLoanAssets: number
  maintenanceAssets: number
  retiredAssets: number
  activeLoans: number
  overdueLoans: number
  activeMaintenances: number
}

export interface CategoryCount {
  categoryId: string
  categoryName: string
  assetCount: number
}

export interface Category {
  id: string
  name: string
  description: string
}

export interface Location {
  id: string
  name: string
  building: string
  floor: string
  description: string
}

export interface Vendor {
  id: string
  name: string
  contactPerson: string
  email: string
  phone: string
  address: string
}

export interface AdminUser extends UserProfile {
  createdAt: string
}

export interface AuditLog {
  id: string
  userId: string | null
  entityName: string
  entityId: string
  action: string
  oldValues: string | null
  newValues: string | null
  createdAt: string
}
