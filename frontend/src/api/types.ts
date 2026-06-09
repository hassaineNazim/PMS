export type MealPlan = 'RoomOnly' | 'BedAndBreakfast' | 'HalfBoard' | 'FullBoard';
export type PaymentMethod = 'Cash' | 'CIB' | 'Edahabia' | 'BankTransfer' | 'Cheque' | 'Other';
export type PaymentType = 'Deposit' | 'Balance' | 'Refund';
export type ChargeCategory = 'MiniBar' | 'Restaurant' | 'RoomService' | 'Laundry' | 'Telephone' | 'Spa' | 'Other';
export type HousekeepingStatus = 'Clean' | 'Dirty' | 'InProgress' | 'Inspected';
export type CashSessionStatus = 'Open' | 'Closed';

export const MEAL_PLAN_LABELS: Record<MealPlan, string> = {
  RoomOnly: 'Logement seul',
  BedAndBreakfast: 'Petit-déjeuner',
  HalfBoard: 'Demi-pension',
  FullBoard: 'Pension complète',
};

export type RoomStatus = 'Available' | 'Occupied' | 'Dirty' | 'OutOfService';
export type RoomType = 'Single' | 'Double' | 'Twin' | 'Suite' | 'Deluxe';
export type ReservationStatus = 'Confirmed' | 'CheckedIn' | 'CheckedOut' | 'Cancelled' | 'NoShow';
export type InvoiceStatus = 'Draft' | 'Pending' | 'Paid' | 'Cancelled' | 'Refunded';
export type UserRole = 'Admin' | 'Manager' | 'Receptionist' | 'Housekeeping';
export type StaffRole = 'Manager' | 'Receptionist' | 'Housekeeper' | 'Maintenance' | 'Security' | 'Other';
export type StaffStatus = 'Active' | 'Inactive' | 'OnLeave';

export interface UserDto {
  id: string;
  tenantId: string;
  email: string;
  fullName: string;
  role: UserRole;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserDto;
}

export interface RoomDto {
  id: string;
  number: string;
  type: RoomType;
  status: RoomStatus;
  floor?: number | null;
  capacity: number;
  pricePerNight: number;
  description?: string | null;
}

export interface GuestDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  language: string;
  nationality?: string | null;
  documentType?: string | null;
  documentNumber?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ReservationDto {
  id: string;
  guestId: string;
  guestName: string;
  roomId: string;
  roomNumber: string;
  roomType: RoomType;
  checkIn: string;
  checkOut: string;
  nights: number;
  status: ReservationStatus;
  adults: number;
  children: number;
  mealPlan: MealPlan;
  mealPlanTotal: number;
  roomTotal: number;
  totalAmount: number;
  notes?: string | null;
  accompanyingGuests?: string | null;
}

export interface AvailableRoomDto {
  roomId: string;
  number: string;
  type: RoomType;
  capacity: number;
  pricePerNight: number;
  nights: number;
  estimatedTotal: number;
}

export interface InvoiceDto {
  id: string;
  number: string;
  reservationId: string;
  guestId: string;
  guestName: string;
  roomId: string;
  roomNumber: string;
  checkIn: string;
  checkOut: string;
  nights: number;
  pricePerNight: number;
  roomSubtotal: number;
  mealPlanSubtotal: number;
  extrasSubtotal: number;
  subtotal: number;
  taxRate: number;
  taxAmount: number;
  stampDuty: number;
  total: number;
  amountPaid: number;
  balanceDue: number;
  currency: string;
  status: InvoiceStatus;
  createdAt: string;
}

export interface ChargeDto {
  id: string;
  reservationId: string;
  category: ChargeCategory;
  label: string;
  quantity: number;
  unitPrice: number;
  total: number;
  postedAt: string;
}

export interface PaymentLineDto {
  id: string;
  amount: number;
  method: PaymentMethod;
  type: PaymentType;
  stampDuty: number;
  reference?: string | null;
  paidAt: string;
}

export interface FolioDto {
  reservationId: string;
  guestName: string;
  roomNumber: string;
  mealPlan: MealPlan;
  roomSubtotal: number;
  mealPlanSubtotal: number;
  extrasSubtotal: number;
  subtotal: number;
  taxRate: number;
  taxAmount: number;
  stampDuty: number;
  total: number;
  amountPaid: number;
  balanceDue: number;
  currency: string;
  charges: ChargeDto[];
  payments: PaymentLineDto[];
}

export interface CashSessionDto {
  id: string;
  userName: string;
  openedAt: string;
  closedAt?: string | null;
  openingFloat: number;
  cashMovements: number;
  expectedCash: number;
  countedCash?: number | null;
  discrepancy?: number | null;
  status: CashSessionStatus;
  notes?: string | null;
}

export interface RatePeriodDto {
  id: string;
  name: string;
  roomType?: RoomType | null;
  startDate: string;
  endDate: string;
  pricePerNight: number;
  priority: number;
}

export interface HousekeepingRoomDto {
  roomId: string;
  number: string;
  floor?: number | null;
  status: RoomStatus;
  housekeepingStatus: HousekeepingStatus;
  assignedHousekeeperId?: string | null;
  assignedHousekeeperName?: string | null;
}

export interface MainCouranteEntryDto {
  date: string;
  movement: string;
  guestName: string;
  roomNumber: string;
  checkIn: string;
  checkOut: string;
  status: ReservationStatus;
}

export interface TenantSettingsDto {
  name: string;
  legalName: string;
  address?: string | null;
  city?: string | null;
  country?: string | null;
  phone?: string | null;
  contactEmail?: string | null;
  currency: string;
  defaultTaxRate: number;
  taxId?: string | null;
  statId?: string | null;
  tradeRegister?: string | null;
  taxArticle?: string | null;
  fiscalStampEnabled: boolean;
  fiscalStampRate: number;
  fiscalStampMinimum: number;
  breakfastSupplement: number;
  halfBoardSupplement: number;
  fullBoardSupplement: number;
}

export interface StaffDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  role: StaffRole;
  department?: string | null;
  hireDate: string;
  status: StaffStatus;
}

export interface CheckInResult {
  reservationId: string;
  guestName: string;
  roomNumber: string;
  checkOut: string;
  invoiceId: string;
  invoiceNumber: string;
  invoiceTotal: number;
  displayNotified: boolean;
  displayProvider?: string | null;
  displayError?: string | null;
}

export interface TimeSeriesPoint { date: string; value: number; }

export interface DashboardStatsDto {
  rooms: { total: number; available: number; occupied: number; dirty: number; outOfService: number; occupancyRate: number };
  reservations: { total: number; confirmed: number; checkedIn: number; checkedOut: number; cancelled: number };
  revenue: { total: number; thisMonth: number; lastMonth: number; monthGrowth: number; averageInvoice: number; invoiceCount: number };
  guests: { total: number };
  charts: {
    reservationsByDay: TimeSeriesPoint[];
    revenueByDay: TimeSeriesPoint[];
    occupancyByDay: TimeSeriesPoint[];
    roomsByStatus: { available: number; occupied: number; dirty: number; outOfService: number };
    reservationsByStatus: { confirmed: number; checkedIn: number; checkedOut: number; cancelled: number };
  };
}
