namespace Pms.Domain.Enums;

public enum RoomStatus
{
    Available = 0,
    Occupied = 1,
    Dirty = 2,
    OutOfService = 3
}

public enum RoomType
{
    Single = 0,
    Double = 1,
    Twin = 2,
    Suite = 3,
    Deluxe = 4
}

public enum ReservationStatus
{
    Confirmed = 0,
    CheckedIn = 1,
    CheckedOut = 2,
    Cancelled = 3,
    NoShow = 4
}

public enum InvoiceStatus
{
    Draft = 0,
    Pending = 1,
    Paid = 2,
    Cancelled = 3,
    Refunded = 4
}

/// <summary>Application login roles (RBAC).</summary>
public enum UserRole
{
    Admin = 0,
    Manager = 1,
    Receptionist = 2,
    Housekeeping = 3
}

/// <summary>HR job role for staff records (distinct from login <see cref="UserRole"/>).</summary>
public enum StaffRole
{
    Manager = 0,
    Receptionist = 1,
    Housekeeper = 2,
    Maintenance = 3,
    Security = 4,
    Other = 5
}

public enum StaffStatus
{
    Active = 0,
    Inactive = 1,
    OnLeave = 2
}

/// <summary>Commercial plan attached to a tenant license.</summary>
public enum LicensePlan
{
    Trial = 0,
    Standard = 1,
    Professional = 2,
    Enterprise = 3
}

/// <summary>Board / meal plan (formule de pension) attached to a reservation.</summary>
public enum MealPlan
{
    RoomOnly = 0,        // Logement seul
    BedAndBreakfast = 1, // Petit-déjeuner inclus
    HalfBoard = 2,       // Demi-pension (petit-déj + 1 repas)
    FullBoard = 3        // Pension complète (petit-déj + 2 repas)
}

/// <summary>Payment method. Cash dominates in Algeria; CIB/Edahabia are local cards.</summary>
public enum PaymentMethod
{
    Cash = 0,        // Espèces
    CIB = 1,         // Carte CIB
    Edahabia = 2,    // Carte Edahabia (Algérie Poste)
    BankTransfer = 3,// Virement
    Cheque = 4,
    Other = 5
}

public enum PaymentType
{
    Deposit = 0,  // Acompte / arrhes
    Balance = 1,  // Solde
    Refund = 2    // Remboursement
}

/// <summary>Category of an additional charge posted to a guest folio (mini POS).</summary>
public enum ChargeCategory
{
    MiniBar = 0,
    Restaurant = 1,
    RoomService = 2,
    Laundry = 3,
    Telephone = 4,
    Spa = 5,
    Other = 6
}

/// <summary>Housekeeping state of a room, independent from its commercial status.</summary>
public enum HousekeepingStatus
{
    Clean = 0,
    Dirty = 1,
    InProgress = 2,
    Inspected = 3
}

public enum CashSessionStatus
{
    Open = 0,
    Closed = 1
}
