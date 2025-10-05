using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace webapi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Avatar { get; set; }
        public string? Cover { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime Dob { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Background { get; set; }

    }

    public class AspNetUserDetailsModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? Background { get; set; }
        public DateTime? Dob { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Cover { get; set; }
        public string? Avatar { get; set; }
        public List<string>? UserRoles { get; set; }
    }

    public class AspNetUserAddress
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Postcode { get; set; }
    }

    public class AspNetUserWeightHistory
    {
        public string Id { get; set; }
        public int? WeightLb { get; set; }
        public DateTime? CreatedAt  { get; set; }
        public string AspNetUserId { get; set; }
    }

    public class Genders
    {
        public string Id { get; set; }
        public string Gender { get; set; }
    }

    public class UserDob
    {
        public DateTime Dob { get; set; }
    }
}
