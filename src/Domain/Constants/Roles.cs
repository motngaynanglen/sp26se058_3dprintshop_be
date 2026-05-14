namespace sp26se058_3dprintshop_be.Domain.Constants;

public abstract class Roles
{
    public const string ADMIN = nameof(ADMIN);
    public const string MANAGER = nameof(MANAGER);
    public const string STAFF = nameof(STAFF);
    public const string CUSTOMER = nameof(CUSTOMER);
    public const string GUEST = nameof(GUEST);

    public const string Authenticated = MANAGER + "," + STAFF + "," + CUSTOMER;
    public const string SystemAdmin = ADMIN;
    public const string Internal = MANAGER + "," + STAFF;
    public const string StaffOrManager = STAFF + "," + MANAGER;
    public const string CustomerStaffManager = CUSTOMER + "," + STAFF + "," + MANAGER;
}
