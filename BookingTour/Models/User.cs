using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDelete { get; set; }

    public int? DefaultRoleId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Role? DefaultRole { get; set; }

    public virtual ICollection<UserRole> UserRoleAssignedByNavigations { get; set; } = new List<UserRole>();

    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();
}
